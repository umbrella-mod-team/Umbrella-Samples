using System;
using System.IO;
using UnityEngine;
using WIGU;
using System.Collections.Generic;
using UnityEngine.Video;
using System.Linq;
using System.Collections;
using Newtonsoft.Json.Linq;
using System.Text;

namespace WIGUx.Modules.attractmodeModule
{
    public class attractmodeController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        [Header("Lights and Emissives")]
        private Transform topEmissiveObject;
        private Transform sideEmissiveObject;
        private Transform bottomEmissiveObject;
        private Transform segalightObject;
        private Transform billsEmissiveObject;

        [Header("Screen Stuff")]
        private GameObject playerCamera;   // Reference to the player camera
        private Transform playerCameraTransform;
        private GameObject playerVRSetup;   // Reference to the player 
        private UnityEngine.Video.VideoPlayer ugcVideoPlayer;
        private RenderTexture attractRenderTexture;
        private ScreenController screenController;
        private ScreenReceiver screenReceiver;
        private int realheight;
        private int realwidth;
        private bool isAttractMode = false;
        private bool isTate = false;
        private AudioSource audioSource;
        private GameSystemState systemState;
        private bool isSyncedScreenMesh = false;
        public VideoPlayer videoPlayer;
        public GameObject emissiveLight; // whatever lights up
        private bool attractModeAutoStart = false; // Default is off, can be set from config
        private bool videoLoaded = false;
        private float videoDistanceThreshold; // attractmode distance to play video
        private float attractFadeStart; // attractmode audio fade distance start
        private float attractFadeEnd;  // attractmode audio fade distance end
        private float attractVolume;  //attract mode volume
        private int attractRenderWidth = 320; //  0 (auto)
        private int attractRenderHeight = 240; //  0 (auto)
        private KeyCode attractModeHotkey = KeyCode.Insert; // Default is Insert, can be set from config
        private Dictionary<GameObject, GameObject> attractScreens = new Dictionary<GameObject, GameObject>();
        [Header("Cabinet Settings")]
        private Retroarch retroarchInstance;
        private GameSystem gameSystem;
        private float currentHue;
        private float hueCycleSpeed = 0.5f;
        private string insertedGameName = string.Empty;
        private string controlledGameName = string.Empty;
        private string videoUrl;
        private JObject saveJson;

        void Awake()
        {
            systemState = GetComponent<GameSystemState>();
        }

        void Start()
        {
            gameSystem = GetComponent<GameSystem>();
            screenController = GetComponentInParent<ScreenController>() ?? FindObjectOfType<ScreenController>();
            retroarchInstance = FindObjectOfType<Retroarch>();
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
                //  logger.Debug($"{gameObject.name} No VR Devices found. No SteamVR or OVR present)");
            }
            CheckForMissingObjects();
            LoadAttractSettingsFromSaveAndConfig();
            if (attractModeAutoStart && !isAttractMode)
            {
                if (isSyncedScreenMesh)
                    StartAttractMode();
                else
                    StartCoroutine(DelayedStartAttractMode());
            }
            currentHue = UnityEngine.Random.Range(0f, 1f);
            // Find the ScreenReceiver if present
            if (screenController != null)
                screenReceiver = screenController.GetComponent<ScreenReceiver>();
        }
        void Update()
        {
            CheckInsertedGameName();
            CheckControlledGameName();

            if (systemState != null && systemState.IsOn && isAttractMode == true)
            {
                StopAttractMode();
            }

            // Enable/Disable attract mode with Insert for demo purposes
            if (Input.GetKeyDown(attractModeHotkey))
            {
                if (!isAttractMode)
                {
                    if (isSyncedScreenMesh)
                        StartAttractMode();
                    else
                        StartCoroutine(DelayedStartAttractMode());
                }
                else
                {
                    StopAttractMode();
                }
            }

            // --- Always turn ON visuals in attract mode ---
            if (isAttractMode && systemState != null)
            {
                systemState.SetPowerLight(true);
                var animation = systemState.GetComponent<Animation>();
                if (systemState.onAnimation != null && animation != null)
                    animation.Play(systemState.onAnimation.name);
            }

            // --- Handle Attract Mode video (distance, streaming logic) ---
            if (isAttractMode && playerCamera != null)
            {
                playerCameraTransform = playerCamera.transform;

                Vector3 toObj = transform.position - playerCameraTransform.position;
                float rawDist = toObj.magnitude;
                float forwardDot = Vector3.Dot(playerCameraTransform.forward, toObj.normalized);
                float biasForward = 0.0f;
                float dist = rawDist * (1f - biasForward * forwardDot);

                // Use settings loaded from config/save (set in StartAttractMode)
                // float videoDistanceThreshold, attractFadeStart, attractFadeEnd, attractVolume
                float fadeLength = attractFadeEnd - attractFadeStart;

                // --- Dynamic video streaming: Create or destroy video player based on range ---
                if (!isSyncedScreenMesh)
                {
                    // Create the VideoPlayer if entering range and it's not already created
                    if (ugcVideoPlayer == null && dist <= videoDistanceThreshold)
                    {
                        ugcVideoPlayer = gameObject.AddComponent<VideoPlayer>();
                        ugcVideoPlayer.playOnAwake = false;
                        ugcVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
                        ugcVideoPlayer.source = UnityEngine.Video.VideoSource.Url;
                        ugcVideoPlayer.url = videoUrl; // should be set up in StartAttractMode()
                        ugcVideoPlayer.isLooping = true;
                        if (audioSource != null)
                        {
                            ugcVideoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                            ugcVideoPlayer.SetTargetAudioSource(0, audioSource);
                            audioSource.volume = attractVolume;
                        }
                        ugcVideoPlayer.errorReceived += (vp, msg) => logger.Error("[AttractMode] VideoPlayer ERROR: " + msg);
                        ugcVideoPlayer.prepareCompleted += OnVideoPrepared;
                        ugcVideoPlayer.Prepare();
                    }
                    // Destroy/unload the VideoPlayer if leaving range
                    else if (ugcVideoPlayer != null && dist > videoDistanceThreshold)
                    {
                        ugcVideoPlayer.Stop();
                        Destroy(ugcVideoPlayer);
                        ugcVideoPlayer = null;
                    }

                    // Only do play/pause/mute if the video is loaded (ugcVideoPlayer != null)
                    if (ugcVideoPlayer != null)
                    {
                        if (dist <= videoDistanceThreshold)
                        {
                            if (!ugcVideoPlayer.isPlaying)
                            {
                                ugcVideoPlayer.Play();
                                if (audioSource != null)
                                {
                                    audioSource.mute = false;
                                }
                            }
                        }
                        else
                        {
                            if (ugcVideoPlayer.isPlaying)
                            {
                                ugcVideoPlayer.Stop();
                                if (audioSource != null)
                                {
                                    audioSource.mute = true;
                                }
                            }
                        }
                    }
                }
                else // SyncedScreenMesh: let video play, only mute audio by range
                {
                    if (audioSource != null)
                    {
                        audioSource.mute = (dist > videoDistanceThreshold);
                    }
                }

                // --- Audio fade logic (applies if audioSource exists and video is playing) ---
                if (audioSource != null && ugcVideoPlayer != null)
                {
                    float fade;
                    if (dist <= attractFadeStart)
                        fade = 1f;
                    else if (dist >= attractFadeEnd)
                        fade = 0f;
                    else
                        fade = 1f - ((dist - attractFadeStart) / fadeLength);

                    audioSource.volume = fade * attractVolume; // max volume at closest
                    audioSource.mute = (fade < 0.01f) || !ugcVideoPlayer.isPlaying;

                    // Flush audio buffer if muted or video is paused to prevent overflow
                    if ((fade < 0.01f || !ugcVideoPlayer.isPlaying) && audioSource.isPlaying)
                    {
                        audioSource.Stop();
                        if (ugcVideoPlayer != null)
                            ugcVideoPlayer.audioOutputMode = VideoAudioOutputMode.None;
                    }

                    // Reattach AudioSource and restart audio only when back in range and video is playing
                    if ((fade >= 0.01f && ugcVideoPlayer.isPlaying) && !audioSource.isPlaying)
                    {
                        if (ugcVideoPlayer != null)
                        {
                            ugcVideoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                            ugcVideoPlayer.SetTargetAudioSource(0, audioSource);
                        }
                        audioSource.Play();
                    }
                }
            }

            CycleEmissiveColors();
        }


        private IEnumerator DelayedStartAttractMode()
        {
            float delay = UnityEngine.Random.Range(0.1f, 5f);
            yield return new WaitForSeconds(delay);
            StartAttractMode();
        }

        private void CheckInsertedGameName()
        {
            if (gameSystem != null && gameSystem.Game != null && !string.IsNullOrEmpty(gameSystem.Game.path))
                insertedGameName = FileNameHelper.GetFileName(gameSystem.Game.path);
            else
                insertedGameName = string.Empty;
        }

        private void CheckControlledGameName()
        {
            if (GameSystem.ControlledSystem != null && GameSystem.ControlledSystem.Game != null
                && !string.IsNullOrEmpty(GameSystem.ControlledSystem.Game.path))
                controlledGameName = FileNameHelper.GetFileName(GameSystem.ControlledSystem.Game.path);
            else
                controlledGameName = string.Empty;
        }
        string GetEmuVRRoot()
        {
            var dataPath = Application.dataPath;
            var root = Path.GetDirectoryName(dataPath);
            return root;
        }
        private static string GetArcadeCfgPath()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot, "wigux", "arcade.cfg");
        }

        private void StartAttractMode()
        {
            logger.Debug($"{gameObject.name} Starting Attract Mode...");
            if (isAttractMode)
            {
                logger.Debug("Attract mode already running.");
                return;
            }
            isAttractMode = true;

            if (screenController == null || screenController.screens.Count == 0)
            {
                logger.Debug($"{gameObject.name} No screen controller or no screens found!");
                isAttractMode = false;
                return;
            }

            // --- CHECK VIDEO EXISTS BEFORE ANY SWAP ---
            string folderAndName = CheckGameName();
            if (string.IsNullOrEmpty(folderAndName))
            {
                isAttractMode = false;
                return;
            }
            string emuvrRoot = GetEmuVRRoot();
            string videoFilePath = Path.Combine(emuvrRoot, "Custom", "Videos", folderAndName + ".mp4");
            videoUrl = "file:///" + videoFilePath.Replace("\\", "/"); // <-- assign to field!
            logger.Debug($"[Attractmode][Debug] Using video URL: {videoUrl}");

            // --- ATTRACT SCREEN CLONE: Instantiate clones, hide originals ---
            foreach (var origScreen in screenController.screens)
            {
                if (!attractScreens.ContainsKey(origScreen))
                {
                    GameObject clone = Instantiate(origScreen, origScreen.transform.parent);
                    clone.name = origScreen.name + "_Attract";
                    if (origScreen.name == "screen_mesh_9")
                    {
                        isSyncedScreenMesh = true;
                        logger.Debug($"{gameObject.name} Syncing screens for screen_mesh_9.");
                    }
                    clone.SetActive(true);
                    attractScreens[origScreen] = clone;

                    if (audioSource == null && attractScreens.Count == 1)
                    {
                        audioSource = clone.GetComponent<AudioSource>();
                        if (audioSource == null)
                        {
                            audioSource = clone.AddComponent<AudioSource>();
                            audioSource.playOnAwake = false;
                            audioSource.spatialBlend = 0.0f;
                        }
                    }
                    var rend = clone.GetComponent<Renderer>();
                    if (rend) rend.enabled = true;
                    clone.layer = origScreen.layer;
                    var receiver = clone.GetComponent<ScreenReceiver>();
                    if (receiver) Destroy(receiver);
                    origScreen.SetActive(false);
                }
            }

            // Clean up any previous player/texture
            if (ugcVideoPlayer != null)
            {
                ugcVideoPlayer.Stop();
                Destroy(ugcVideoPlayer);
                ugcVideoPlayer = null;
            }
            if (attractRenderTexture != null)
            {
                attractRenderTexture.Release();
                Destroy(attractRenderTexture);
                attractRenderTexture = null;
            }

            // --- DO NOT CREATE THE VIDEOPLAYER HERE! ---
            // VideoPlayer will be created in Update(), when player is in range.

            // Visuals/animation ON always in attract mode (optional if you want extra safety)
            if (systemState != null)
            {
                systemState.SetPowerLight(true);
                var animation = systemState.GetComponent<Animation>();
                if (systemState.onAnimation != null && animation != null)
                    animation.Play(systemState.onAnimation.name);
            }
        }


        private void OnVideoPrepared(UnityEngine.Video.VideoPlayer source)
        {
            realwidth = Mathf.Max(2, (int)source.width);
            realheight = Mathf.Max(2, (int)source.height);
            logger.Debug($"[AttractMode] Video prepared:320x240 real size = {realwidth}x{realheight}");
            // Check if this is a TATE (portrait) video
            isTate = realheight > realwidth;
            int texWidth = attractRenderWidth > 0 ? attractRenderWidth : realwidth;
            int texHeight = attractRenderHeight > 0 ? attractRenderHeight : realheight;
            logger.Debug($"[AttractMode] Video prepared, render size: {texWidth}x{texHeight} (video={realwidth}x{realheight})");
            attractRenderTexture = new RenderTexture(texWidth, texHeight, 0);

            attractRenderTexture.Create();
            ugcVideoPlayer.targetTexture = attractRenderTexture;

            var HologramObj = transform.Find("Hologram");
            if (HologramObj == null)
                logger.Debug($"{gameObject.name}: [DEBUG] Hologram cache not found under cab root.");

            else
                logger.Debug($"{gameObject.name}: [DEBUG] Found Hologram cache: {HologramObj.name}");

            var TateObj = transform.Find("Tate");
            if (TateObj == null)
                logger.Debug($"{gameObject.name}: [DEBUG] Tate cache not found under cab root.");
            else
                logger.Debug($"{gameObject.name}: [DEBUG] Found Tate cache: {TateObj.name}");

            var rootHologramObj = transform.Find("Hologram");
            var rootTateObj = transform.Find("Tate");

            foreach (var pair in attractScreens)
            {
                var clone = pair.Value;
                if (clone == null) continue;
                var rend = clone.GetComponent<Renderer>();
                if (rend == null) continue;

                // --- Hologram: Assign shader from root cache ---
                if (rootHologramObj != null)
                {
                    var holoRend = rootHologramObj.GetComponent<Renderer>();
                    if (holoRend != null && holoRend.sharedMaterial != null)
                    {
                        rend.material.shader = holoRend.sharedMaterial.shader;
                        rend.material.SetTexture("_EmissionMap", attractRenderTexture);
                        rend.material.SetColor("_EmissionColor", Color.white);
                        rend.material.EnableKeyword("_EMISSION");
                        logger.Debug($"[DEBUG] Assigned Hologram shader '{holoRend.sharedMaterial.shader.name}' to {clone.name}");
                        continue;
                    }
                }

                // --- Tate: Assign shader from root cache ---
                if (isTate && rootTateObj != null)
                {
                    var tateRend = rootTateObj.GetComponent<Renderer>();
                    if (tateRend != null && tateRend.sharedMaterial != null)
                    {
                        rend.material.shader = tateRend.sharedMaterial.shader;
                        rend.material.SetTexture("_EmissionMap", attractRenderTexture);
                        rend.material.SetColor("_EmissionColor", Color.white);
                        rend.material.EnableKeyword("_EMISSION");
                        logger.Debug($"[DEBUG] Assigned Tate shader '{tateRend.sharedMaterial.shader.name}' to {clone.name}");
                        continue;
                    }
                }

                // --- Standard fallback ---
                rend.material.shader = Shader.Find("Standard");
                rend.material.mainTexture = attractRenderTexture;
                rend.material.SetTexture("_EmissionMap", attractRenderTexture);
                rend.material.SetColor("_EmissionColor", Color.white);
                rend.material.EnableKeyword("_EMISSION");
                logger.Debug($"[DEBUG] Assigned Standard shader to {clone.name}");
            }

            ugcVideoPlayer.aspectRatio = UnityEngine.Video.VideoAspectRatio.Stretch;

            logger.Debug("[AttractMode] ugcVideoPlayer.targetTexture: " + ugcVideoPlayer.targetTexture.GetInstanceID());
            logger.Debug("[AttractMode] attractRenderTexture: " + attractRenderTexture.GetInstanceID());
            if (systemState != null)
            {
                systemState.SetPowerLight(true);
                // Play ON animation if available
                var animation = systemState.GetComponent<Animation>();
                if (systemState.onAnimation != null && animation != null)
                    animation.Play(systemState.onAnimation.name);
            }
            ugcVideoPlayer.Play();
            ugcVideoPlayer.prepareCompleted -= OnVideoPrepared;
        }

        private void StopAttractMode()
        {
            logger.Debug($"{gameObject.name} Stopping Attract Mode...");
            if (!isAttractMode) return;
            isAttractMode = false;

            // --- ATTRACT SCREEN CLONE: destroy clones and unhide originals ---
            foreach (var pair in attractScreens)
            {
                if (pair.Value != null)
                    Destroy(pair.Value); // Destroy clone
                if (pair.Key != null)
                    pair.Key.SetActive(true); // Unhide original
            }
            attractScreens.Clear();

            // Cleanup VideoPlayer and RenderTexture
            if (ugcVideoPlayer != null)
            {
                ugcVideoPlayer.Stop();
                Destroy(ugcVideoPlayer);
                ugcVideoPlayer = null;
            }
            if (attractRenderTexture != null)
            {
                attractRenderTexture.Release();
                Destroy(attractRenderTexture);
                attractRenderTexture = null;
            }
            // --- Power Light/Emissive: OFF unless real system ON ---
            if (systemState != null && !systemState.IsOn)
            {
                systemState.SetPowerLight(false);
                // Play Off animation if available
                var animation = systemState.GetComponent<Animation>();
                if (systemState.offAnimation != null && animation != null)
                    animation.Play(systemState.offAnimation.name);
            }
        }
        public void LoadAttractSettingsFromSaveAndConfig()
        {
            float defaultVideoDistanceThreshold = 5f;
            float defaultAttractFadeStart = 1.5f;
            float defaultAttractFadeEnd = 3.0f;
            float defaultAttractVolume = 0.35f;
            int defaultAttractRenderWidth = 0;
            int defaultAttractRenderHeight = 0;

            // Assign defaults first
            videoDistanceThreshold = defaultVideoDistanceThreshold;
            attractFadeStart = defaultAttractFadeStart;
            attractFadeEnd = defaultAttractFadeEnd;
            attractRenderWidth = defaultAttractRenderWidth;
            attractRenderHeight = defaultAttractRenderHeight;
            attractVolume = defaultAttractVolume;
            attractModeAutoStart = false;
            attractModeHotkey = KeyCode.Insert;

            int slot = Settings.AutoLoadLevel;
            string slotSection = $"Slot{slot}";
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string cfgPath = Path.Combine(projectRoot, "wigux", "arcade.cfg");
            logger.Debug($"[AttractMode] Looking for cfg at: {cfgPath}");
            var ini = new IniFile(cfgPath);

            // --- Load all values from slot section in config ---
            videoDistanceThreshold = ini.GetFloat(slotSection, "videoDistanceThreshold", defaultVideoDistanceThreshold);
            attractFadeStart = ini.GetFloat(slotSection, "attractFadeStart", defaultAttractFadeStart);
            attractFadeEnd = ini.GetFloat(slotSection, "attractFadeEnd", defaultAttractFadeEnd);
            attractRenderWidth = (int)ini.GetFloat(slotSection, "attractRenderWidth", defaultAttractRenderWidth);
            attractRenderHeight = (int)ini.GetFloat(slotSection, "attractRenderHeight", defaultAttractRenderHeight);
            attractVolume = ini.GetFloat(slotSection, "attractVolume", defaultAttractVolume);

            // --- Per-slot autostart and hotkey ---
            attractModeAutoStart = ini.GetString(slotSection, "attractModeAutoStart", "false").ToLower() == "true";

            string hotkeyString = ini.GetString(slotSection, "attractModeHotkey", "Insert");
            if (System.Enum.TryParse<KeyCode>(hotkeyString, out var parsedKey))
                attractModeHotkey = parsedKey;

            logger.Debug($"[AttractMode] Slot {slot} values from arcade.cfg:");
            logger.Debug($"   videoDistanceThreshold = {videoDistanceThreshold}");
            logger.Debug($"   attractFadeStart = {attractFadeStart}");
            logger.Debug($"   attractFadeEnd = {attractFadeEnd}");
            logger.Debug($"   attractVolume = {attractVolume}");
            logger.Debug($"   attractRenderWidth = {attractRenderWidth}");
            logger.Debug($"   attractRenderHeight = {attractRenderHeight}");
            logger.Debug($"   attractModeAutoStart = {attractModeAutoStart}");
            logger.Debug($"   attractModeHotkey = {attractModeHotkey}");

            // --- Override volume from save JSON (per-cabinet), if present ---
            try
            {
                string slotPath = LevelSerializer.GetSlotPath(slot);
                if (File.Exists(slotPath))
                {
                    JObject saveJson = JObject.Parse(File.ReadAllText(slotPath, Encoding.UTF8));
                    logger.Debug($"[AttractMode] loaded save JSON for Slot{slot}");

                    string ugcId = gameObject.name.Replace("(Clone)", "");
                    JArray systems = saveJson["systems"] as JArray;
                    if (systems != null)
                    {
                        foreach (JObject system in systems)
                        {
                            if ((string)system["id"] == ugcId)
                            {
                                int? embeddedScreen = system["embedded_screen"]?.Value<int>();
                                if (embeddedScreen != null)
                                {
                                    JArray objects = saveJson["objects"] as JArray;
                                    if (objects != null)
                                    {
                                        foreach (JObject obj in objects)
                                        {
                                            if ((int?)obj["embedded_screen"] == embeddedScreen)
                                            {
                                                float? vol = obj["volume"]?.Value<float>();
                                                if (vol != null)
                                                {
                                                    attractVolume = vol.Value;
                                                    logger.Debug($"[AttractMode][SAVE JSON OVERRIDE] Attract volume overridden by save JSON: {attractVolume}");
                                                }
                                                break;
                                            }
                                        }
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                logger.Warning($"[AttractMode] Could not load save JSON for slot {slot}: {ex.Message}");
            }

            logger.Debug($"[AttractMode] Slot {slot} settings: videoDistanceThreshold={videoDistanceThreshold}, attractFadeStart={attractFadeStart}, attractFadeEnd={attractFadeEnd}, attractVolume={attractVolume}, attractRenderWidth={attractRenderWidth}, attractRenderHeight={attractRenderHeight}, attractModeAutoStart={attractModeAutoStart}, attractModeHotkey={attractModeHotkey}");
        }
        private string CheckGameName()
        {
            if (gameSystem?.Game == null)
                return string.Empty;

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string videosRoot = Path.Combine(projectRoot, "Custom", "Videos");

            // --- 1. Get System Folder from Game PATH (right after \Games\) ---
            string systemFolder = null;
            string path = gameSystem.Game.path.Replace('\\', '/'); // normalize
            int gamesIdx = path.IndexOf("/Games/", StringComparison.OrdinalIgnoreCase);
            if (gamesIdx >= 0)
            {
                int afterGames = gamesIdx + 7; // length of "/Games/"
                int nextSlash = path.IndexOf('/', afterGames);
                if (nextSlash > afterGames)
                    systemFolder = path.Substring(afterGames, nextSlash - afterGames);
                else
                    systemFolder = path.Substring(afterGames);
            }
            else
            {
                // fallback: try using GameMedium.name first part
                if (gameSystem.GameMedium != null && !string.IsNullOrEmpty(gameSystem.GameMedium.name))
                {
                    // Take only the first segment before '/'
                    var parts = gameSystem.GameMedium.name.Split('/');
                    systemFolder = FileNameHelper.Sanitize(parts[0].Trim());
                }
                else
                {
                    systemFolder = FileNameHelper.Sanitize(gameSystem.systemName);
                }
            }

            // --- 2. Extract candidate names ---
            string fileName = FileNameHelper.GetFileName(gameSystem.Game.path); // "mk4"

            // If GameMedium.name has a second part, that's the full title
            string fullTitle = null;
            if (gameSystem.GameMedium != null && !string.IsNullOrEmpty(gameSystem.GameMedium.name))
            {
                var parts = gameSystem.GameMedium.name.Split('/');
                if (parts.Length > 1)
                    fullTitle = FileNameHelper.Sanitize(parts[1].Trim());
            }
            if (string.IsNullOrEmpty(fullTitle))
                fullTitle = FileNameHelper.Sanitize(gameSystem.Game.name);

            // Stripped title
            string stripped = System.Text.RegularExpressions.Regex.Replace(
                fullTitle, @"\s*\(.*?\)", "");
            string shortTitle = FileNameHelper.Sanitize(stripped);

            string[] names = { fileName, fullTitle, shortTitle };

            // --- 3. Search only in the systemFolder extracted ---
            string[] folders = { systemFolder };

            foreach (var folder in folders)
            {
                if (string.IsNullOrEmpty(folder))
                    continue;

                foreach (var name in names)
                {
                    if (string.IsNullOrEmpty(name))
                        continue;

                    string candidate = Path.Combine(videosRoot, folder, name + ".mp4");
                    if (File.Exists(candidate))
                    {
                        logger.Debug($"[Attractmode][Debug] Found attract video: {candidate}");
                        // return rel-path (relative to Custom/Videos/)
                        return Path.Combine(folder, name);
                    }
                }
            }

            logger.Debug($"[Attractmode][Debug] No .mp4 found for '{fileName}' ({fullTitle}/{shortTitle}) in folder: {systemFolder}");
            return string.Empty;
        }

        private void CycleEmissiveColors()
        {
            currentHue += hueCycleSpeed * Time.deltaTime;
            if (currentHue > 1f) currentHue -= 1f;
            Color emissiveColor = Color.HSVToRGB(currentHue, 1f, 1f);
            Transform[] emissives = { topEmissiveObject, sideEmissiveObject, bottomEmissiveObject, billsEmissiveObject, segalightObject };
            foreach (var obj in emissives)
            {
                if (obj != null)
                {
                    var rend = obj.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        rend.material.SetColor("_EmissionColor", emissiveColor);
                        rend.material.EnableKeyword("_EMISSION");
                    }
                }
            }
        }

        void CheckForMissingObjects()
        {
            topEmissiveObject = transform.Find("topEmissive");
            sideEmissiveObject = transform.Find("sideEmissive");
            bottomEmissiveObject = transform.Find("bottomEmissive");
            segalightObject = transform.Find("segalight");
            billsEmissiveObject = transform.Find("coindoor/billsEmissive");
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
        public static class FileNameHelper
        {
            public static string GetFileName(string filePath)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                return System.Text.RegularExpressions.Regex.Replace(fileName, "[\\/:*?\"<>|]", "_");
            }
            public static string Sanitize(string s)
            {
                return System.Text.RegularExpressions.Regex.Replace(s, "[\\\\/:*?\"<>|]", "_");
            }
        }
        // INI parser for arcade.cfg
        public class IniFile
        {
            private Dictionary<string, Dictionary<string, string>> data = new Dictionary<string, Dictionary<string, string>>();

            public IniFile(string path)
            {
                if (!File.Exists(path))
                    return;

                string currentSection = "";
                foreach (var line in File.ReadAllLines(path))
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";")) continue;

                    if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                    {
                        currentSection = trimmed.Substring(1, trimmed.Length - 2);
                        if (!data.ContainsKey(currentSection))
                            data[currentSection] = new Dictionary<string, string>();
                    }
                    else if (trimmed.Contains('='))
                    {
                        var split = trimmed.Split(new[] { '=' }, 2);
                        if (!string.IsNullOrEmpty(currentSection))
                            data[currentSection][split[0].Trim()] = split[1].Trim();
                    }
                }
            }

            public float GetFloat(string section, string key, float defaultValue)
            {
                if (data.TryGetValue(section, out var dict) && dict.TryGetValue(key, out var value) && float.TryParse(value, out var result))
                    return result;
                return defaultValue;
            }

            public string GetString(string section, string key, string defaultValue)
            {
                if (data.TryGetValue(section, out var dict) && dict.TryGetValue(key, out var value))
                    return value;
                return defaultValue;
            }
        }

    }
}