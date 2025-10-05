using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using WIGU;

namespace WIGUx.Modules.camcorderModule
{
    public class camcorderController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        private Camera spectatorCamera;
        private RenderTexture outputTexture;   // full-res RT
        private Renderer screenRenderer;
        private RawImage overlayImage;
        private GameObject overlayCanvas;
        private bool overlayActive = false;
        private GameObject playerCamera;   // Reference to the player camera
        private GameObject playerVRSetup;   // Reference to the player
        private GameObject currentAvatarRoot;
        private string screenMeshName = "screen_obj_5";

        // Hardcoded SpectatorOnly layer index
        private const int SpectatorLayer = 27;

        // ===== keep references to attached avatar body/head =====
        private GameObject ugcBodyRef;
        private GameObject ugcHeadRef;

        // === BODY FOLLOW (height + rotation) ===
        private Transform vrHead;             // PlayerVRSetup.PlayerCamera.transform
        private float _lastLoggedYaw = float.NaN;

        [Serializable]
        private class AppConfigData
        {
            public string SelectedAvatarFolder;
            public string SelectedAvatarFile;
        }

        void Start()
        {
            // Find references to PlayerCamera and VR setup objects
            playerCamera = PlayerVRSetup.PlayerCamera.gameObject;

            // Find and assign the whole VR rig try SteamVR first, then Oculus
            playerVRSetup = GameObject.Find("Player/[SteamVRCameraRig]");
            // If not found, try to find the Oculus VR rig
            if (playerVRSetup == null)
            {
                playerVRSetup = GameObject.Find("OVRCameraRig");
            }

            // Check if objects are found
            CheckObject(playerCamera, "PlayerCamera");
            if (playerVRSetup != null)
            {
                CheckObject(playerVRSetup, playerVRSetup.name); // will print either [SteamVRCameraRig] or OVRCameraRig
            }
            else
            {
                logger.Debug($"{gameObject.name} No VR Devices found. No SteamVR or OVR present)");
            }
            // === Resolve spectator camera ===
            spectatorCamera = GetComponentInChildren<Camera>(true);
            if (spectatorCamera == null)
            {
                logger.Error("SpectatorCamera not found under Professional camera!");
                return;
            }

            // === Create hi-res RT ===
            AllocateRenderTexture();
            spectatorCamera.targetTexture = outputTexture;
            spectatorCamera.enabled = true;
            logger.Debug($"Created hi-res desktop RT {outputTexture.width}x{outputTexture.height}");

            // === Cabinet screen hookup ===
            var screen = GameObject.Find(screenMeshName);
            if (screen != null)
            {
                screenRenderer = screen.GetComponent<Renderer>();
                if (screenRenderer != null && screenRenderer.sharedMaterial != null)
                {
                    screenRenderer.sharedMaterial.SetTexture("_EmissionMap", outputTexture);
                    screenRenderer.sharedMaterial.EnableKeyword("_EMISSION");
                    logger.Debug($"Linked {screenMeshName} emission map to {outputTexture.name}");
                }
            }
            else
            {
                logger.Error($"No object named {screenMeshName} found!");
            }

            // === Load avatar selection from Launcher.config.json ===
            AppConfigData cfg = null;
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WIGUx", "Launcher.config.json");
            if (File.Exists(configPath))
            {
                try
                {
                    var json = File.ReadAllText(configPath);
                    var jobj = JObject.Parse(json);

                    cfg = new AppConfigData
                    {
                        SelectedAvatarFolder = (string)jobj["SelectedAvatarFolder"],
                        SelectedAvatarFile = (string)jobj["SelectedAvatarFile"]
                    };

                    logger.Debug($"[Camcorder] Avatar from config: {cfg.SelectedAvatarFolder}/{cfg.SelectedAvatarFile}");
                }
                catch (Exception ex)
                {
                    logger.Error("[Camcorder] Failed to parse Launcher.config.json: " + ex.Message);
                }
            }

            if (cfg != null && !string.IsNullOrEmpty(cfg.SelectedAvatarFile))
            {
                GameObject avatarRoot = null;
                GameObject prefab = null;

                string wantedId = Path.GetFileNameWithoutExtension(cfg.SelectedAvatarFile).ToLowerInvariant();

                // 🔎 Search already loaded bundles for a matching prefab
                foreach (var ab in AssetBundle.GetAllLoadedAssetBundles())
                {
                    // only look inside the correct bundle
                    if (!ab.name.Equals(wantedId, StringComparison.OrdinalIgnoreCase))
                        continue;

                    foreach (string assetName in ab.GetAllAssetNames())
                    {
                        logger.Debug($"[Camcorder] Checking asset {assetName} in bundle {ab.name}");

                        if (assetName.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                        {
                            prefab = ab.LoadAsset<GameObject>(assetName);
                            if (prefab != null)
                            {
                                logger.Debug($"[Camcorder] Loaded avatar prefab {assetName} from bundle {ab.name}");
                                break;
                            }
                        }
                    }
                    if (prefab != null) break;
                }

                // 🚨 If prefab found, instantiate
                if (prefab != null)
                {
                    // ✅ Clear any existing avatar first
                    if (currentAvatarRoot != null)
                    {
                        Destroy(currentAvatarRoot);
                        currentAvatarRoot = null;
                        ugcBodyRef = null;
                        ugcHeadRef = null;
                        logger.Debug("[Camcorder] Destroyed previous avatar before spawning new one");
                    }

                    avatarRoot = Instantiate(prefab);
                    avatarRoot.name = prefab.name;
                    currentAvatarRoot = avatarRoot;  // track the latest
                    logger.Debug($"[Camcorder] Spawned avatar prefab {avatarRoot.name}");
                    // Delay hiding until Body/Head have been reparented, so we only hide leftover static meshes
                    StartCoroutine(HideAvatarRootAfterAttach(avatarRoot));
                    // Debug: list children so we know Body/Head exist
                    //   foreach (Transform child in avatarRoot.transform)
                    //   logger.Debug($"[Camcorder] Avatar root child: {child.name}");

                    // 🚫 Strip components that would make the avatar look like a prop to LevelSerializer
                    foreach (var sel in avatarRoot.GetComponentsInChildren<Selectable>(true))
                        Destroy(sel);

                    foreach (var comp in avatarRoot.GetComponentsInChildren<CompoundObject>(true))
                        Destroy(comp);

                    foreach (var gm in avatarRoot.GetComponentsInChildren<GameMedium>(true))
                        Destroy(gm);

                    foreach (var sys in avatarRoot.GetComponentsInChildren<GameSystem>(true))
                        Destroy(sys);
                }

                // === Attach Body/Head if we have them ===
                if (avatarRoot != null && playerVRSetup != null) // 🚫 only attach if VR rig exists
                {
                    var ugcBody = avatarRoot.transform.Find("Body");
                    var ugcHead = avatarRoot.transform.Find("Head");

                    var playerBody = GameObject.Find("Player/Body");
                    GameObject playerHead = null;

                    if (playerVRSetup.name.Contains("SteamVRCameraRig"))
                    {
                        playerHead = GameObject.Find("Player/[SteamVRCameraRig]/Camera (eye)/Head");
                    }
                    else if (playerVRSetup.name.Contains("OVRCameraRig"))
                    {
                        playerHead = GameObject.Find("OVRCameraRig/TrackingSpace/CenterEyeAnchor");
                    }

                    if (ugcBody != null && playerBody != null)
                    {
                        PreserveAndReparent(ugcBody.gameObject, playerBody);
                        ugcBodyRef = ugcBody.gameObject;
                        logger.Debug("[Camcorder] Avatar Body attached to Player/Body");
                    }
                    else
                    {
                        if (ugcBody == null) logger.Error("[Camcorder] Avatar prefab missing 'Body'");
                        if (playerBody == null) logger.Error("[Camcorder] Could not find Player/Body in scene");
                    }

                    if (ugcHead != null && playerHead != null)
                    {
                        PreserveAndReparent(ugcHead.gameObject, playerHead);
                        ugcHeadRef = ugcHead.gameObject;
                        logger.Debug("[Camcorder] Avatar Head attached to Player Head");
                    }
                    else
                    {
                        if (ugcHead == null) logger.Error("[Camcorder] Avatar prefab missing 'Head'");
                        if (playerHead == null) logger.Error("[Camcorder] Could not find a VR Head attachment point");
                    }
                }
                else
                {
                    logger.Debug("[Camcorder] Skipping avatar attach — no VR rig detected.");
                }
                // 🧹 Final safety pass — hide any extra static Body under Player/Body (the frozen duplicate)
                {
                    var playerBodyAnchor = GameObject.Find("Player/Body");
                    if (playerBodyAnchor != null && ugcBodyRef != null)
                    {
                        foreach (Transform child in playerBodyAnchor.transform)
                        {
                            // find duplicate "Body" object that isn't the working ugcBodyRef
                            if (child.name.Equals("Body", StringComparison.OrdinalIgnoreCase) &&
                                child.gameObject != ugcBodyRef)
                            {
                                logger.Debug($"[Camcorder] Hiding extra static Body '{child.name}' under Player/Body");
                                foreach (var rend in child.GetComponentsInChildren<Renderer>(true))
                                    rend.enabled = false; // hide duplicate renderers only
                            }
                        }
                    }
                }
            }


            // === LAYER HANDLING ===
            spectatorCamera.cullingMask |= (1 << SpectatorLayer);

            if (PlayerVRSetup.PlayerCamera != null)
            {
                PlayerVRSetup.PlayerCamera.cullingMask &= ~(1 << SpectatorLayer);
                logger.Debug("Removed SpectatorOnly layer from VR camera");
            }

            var field = typeof(PlayerVRSetup).GetField("desktopCamera", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null && PlayerVRSetup.Instance != null)
            {
                var desktopCam = field.GetValue(PlayerVRSetup.Instance) as Camera;
                if (desktopCam != null)
                {
                    desktopCam.cullingMask &= ~(1 << SpectatorLayer);
                    logger.Debug("Removed SpectatorOnly layer from desktop camera");
                }
            }

            logger.Debug($"Using SpectatorOnly layer {SpectatorLayer}");

            // Cache VR head
            if (PlayerVRSetup.PlayerCamera != null)
                vrHead = PlayerVRSetup.PlayerCamera.transform;
        }

        void Update()
        {
            // Reallocate RT if resolution changes
            if (outputTexture != null &&
                (outputTexture.width != Screen.width || outputTexture.height != Screen.height))
            {
                AllocateRenderTexture();
                spectatorCamera.targetTexture = outputTexture;

                if (screenRenderer != null && screenRenderer.sharedMaterial != null)
                {
                    screenRenderer.sharedMaterial.SetTexture("_EmissionMap", outputTexture);
                    screenRenderer.sharedMaterial.EnableKeyword("_EMISSION");
                }

                if (overlayImage != null)
                    overlayImage.texture = outputTexture;

                logger.Debug($"Resized outputTexture to {outputTexture.width}x{outputTexture.height}");
            }

            // === FOLLOW PLAYER: BODY HEIGHT + ROTATION ===
            if (ugcBodyRef != null)
            {
                // re-cache VR head if needed
                if (vrHead == null && playerVRSetup != null)
                {
                    GameObject headGO = null;

                    if (playerVRSetup.name.Contains("SteamVRCameraRig"))
                    {
                        headGO = GameObject.Find("Player/[SteamVRCameraRig]/Camera (eye)/Head");
                    }
                    else if (playerVRSetup.name.Contains("OVRCameraRig"))
                    {
                        headGO = GameObject.Find("OVRCameraRig/TrackingSpace/CenterEyeAnchor");
                    }

                    if (headGO != null)
                        vrHead = headGO.transform;
                }


                // HEIGHT
                if (vrHead != null)
                {
                    var parent = ugcBodyRef.transform.parent;
                    if (parent != null)
                    {
                        Vector3 localHeadPos = parent.InverseTransformPoint(vrHead.position);
                        var lp = ugcBodyRef.transform.localPosition;
                        lp.y = localHeadPos.y;
                        ugcBodyRef.transform.localPosition = lp;
                    }
                }

                // ROTATION
                var pre = PlayerController.preTransform;
                if (pre != null)
                {
                    float yaw = pre.eulerAngles.y;

                    var le = ugcBodyRef.transform.localEulerAngles;
                    le.y = yaw;
                    ugcBodyRef.transform.localEulerAngles = le;

                    if (!Mathf.Approximately(_lastLoggedYaw, yaw))
                    {
                        _lastLoggedYaw = yaw;
                    }
                }
                else
                {
                    logger.Debug("[Camcorder] PlayerController.preTransform not ready yet.");
                }
            }

            // Toggle overlay with Shift+PageUp
            if (Input.GetKeyDown(KeyCode.PageUp) &&
                (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
            {
                overlayActive = !overlayActive;

                if (overlayActive)
                    CreateOverlay();
                else
                    DestroyOverlay();
            }
        }

        // === Allocate RenderTexture ===
        private void AllocateRenderTexture()
        {
            if (outputTexture != null)
            {
                spectatorCamera.targetTexture = null;
                outputTexture.Release();
                Destroy(outputTexture);
            }

            outputTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Default);
            outputTexture.name = "CamcorderRT";
            outputTexture.Create();

            logger.Debug($"Allocated full-res RenderTexture {outputTexture.width}x{outputTexture.height}");
        }

        // === Overlay ===
        private void CreateOverlay()
        {
            if (overlayCanvas != null || outputTexture == null) return;

            overlayCanvas = new GameObject("SpectatorOverlayCanvas");
            var canvas = overlayCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            var scaler = overlayCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Screen.width, Screen.height);

            overlayCanvas.AddComponent<GraphicRaycaster>();

            var overlayGO = new GameObject("SpectatorOverlayImage");
            overlayGO.transform.SetParent(overlayCanvas.transform, false);

            overlayImage = overlayGO.AddComponent<RawImage>();
            overlayImage.texture = outputTexture;

            var rt = overlayImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            logger.Debug("Overlay ON — showing CamcorderRT fullscreen on desktop");
        }

        private void DestroyOverlay()
        {
            if (overlayCanvas != null)
                Destroy(overlayCanvas);

            overlayCanvas = null;
            overlayImage = null;

            logger.Debug("Overlay OFF — restored desktop");
        }

        // === Preserve lossy scale but snap to parent's origin ===
        private void PreserveAndReparent(GameObject ugcObj, GameObject targetParent)
        {
            Vector3 worldScale = ugcObj.transform.lossyScale;

            ugcObj.transform.SetParent(targetParent.transform, false);

            ugcObj.transform.localPosition = Vector3.zero;
            ugcObj.transform.localRotation = Quaternion.identity;

            Vector3 parentScale = targetParent.transform.lossyScale;
            ugcObj.transform.localScale = new Vector3(
                worldScale.x / parentScale.x,
                worldScale.y / parentScale.y,
                worldScale.z / parentScale.z
            );

            logger.Debug($"[Camcorder] Attached {ugcObj.name} to {targetParent.name}, " +
                         $"localPos={ugcObj.transform.localPosition}, localScale={ugcObj.transform.localScale}");

            SetLayerRecursively(ugcObj, SpectatorLayer);
            logger.Debug($"[Camcorder] Preserved + reparented {ugcObj.name} under {targetParent.name}, localScale={ugcObj.transform.localScale}");
        }
        private System.Collections.IEnumerator HideAvatarRootAfterAttach(GameObject avatarRoot)
        {
            yield return new WaitForSeconds(0.5f); // wait so body/head are reparented first

            if (avatarRoot == null) yield break;

            foreach (Transform child in avatarRoot.transform)
            {
                foreach (var renderer in child.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.enabled = false;
                }
                logger.Debug($"[Camcorder] Hid leftover static object '{child.name}' under avatarRoot");
            }
        }
  
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
                SetLayerRecursively(child.gameObject, layer);
        }
        void CheckObject(GameObject obj, string name)     // Check if object is found and log appropriate message
        {
            if (obj == null)
            {
                logger.Error($"{gameObject.name} {name} not found!");
            }
            else
            {
                logger.Debug($"{gameObject.name} {name} found.");
            }
        }
    }
}