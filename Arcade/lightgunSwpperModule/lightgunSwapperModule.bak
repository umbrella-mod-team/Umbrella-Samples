using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using Valve.VR;
using WIGU;
using static ServerInfo;


namespace WIGUx.Modules.lightgunSwapperModule
{
    public class UGCLightgunSwapper : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        private GameSystem gameSystem;
        private Flashlight flashlight;
        private bool inFocusMode = false;
        private string insertedGameName = string.Empty;
        private string controlledGameName = string.Empty;
        private bool isModelReplaced = false;
        private Lightgun ugcLightgunPrefab;
        private MonoBehaviour lightgunController;
        private GameObject defaultGunInstance;
        private GameObject replacementGunInstance;
        private GameSystemState systemState; //systemstate

#if UNITY_STANDALONE_WIN
                        // Move the cursor via SendInput
                        SendAbsoluteMouseMove(x, y);
                        // Map VR trigger to left mouse
                        if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.Oculus)
                        {
                            float trigger = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger);
                            if (trigger > 0.5f && !isMouseDown)
                            {
                                SendMouseDown();
                                isMouseDown = true;
                            }
                            else if (trigger <= 0.5f && isMouseDown)
                            {
                                SendMouseUp();
                                isMouseDown = false;
                            }
                        }
                        else if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.OpenVR)
                        {
                            var lastActiveField = typeof(LightgunController).GetField("LastActiveGun", BindingFlags.Public | BindingFlags.Static);
                            var activeHandObj = lastActiveField?.GetValue(null);
                            HandType activeHand = activeHandObj is HandType ht ? ht : HandType.Left;
                            var steam = SteamVRInput.GetController(activeHand);
                            if (steam.GetPressDown(EVRButtonId.k_EButton_SteamVR_Trigger))
                                SendMouseDown();
                            if (steam.GetPressUp(EVRButtonId.k_EButton_SteamVR_Trigger))
                                SendMouseUp();
                        }
#endif

        void Start()
        {
            CheckInsertedGameName();
            CheckControlledGameName();
            gameSystem = GetComponent<GameSystem>();
            flashlight = GetComponentInChildren<Flashlight>();
            if (flashlight?.lightObject != null)
                flashlight.lightObject.SetActive(true);

            // Locate UGC lightgun prefab in children
            ugcLightgunPrefab = GetComponentInChildren<Lightgun>(true);
            if (ugcLightgunPrefab != null)
                ugcLightgunPrefab.gameObject.SetActive(false);
        }

        void Update()
        {
            CheckInsertedGameName();
            CheckControlledGameName();
            // Enter/exit focus
            if (!inFocusMode && !string.IsNullOrEmpty(insertedGameName)
                && !string.IsNullOrEmpty(controlledGameName)
                && insertedGameName == controlledGameName)
            {
                StartFocusMode();
            }
            if (GameSystem.ControlledSystem == null && inFocusMode)
            {
                EndFocusMode();
            }

            // Keep flashlight state
            if (!inFocusMode && flashlight?.lightObject != null && !flashlight.lightObject.activeSelf)
                flashlight.lightObject.SetActive(true);

            // Reset controller on hand switch
            if (lightgunController != null)
            {
                var handField = lightgunController.GetType().GetField("handType", BindingFlags.NonPublic | BindingFlags.Instance);
                var lastActiveField = typeof(LightgunController).GetField("LastActiveGun", BindingFlags.Public | BindingFlags.Static);
                if (handField != null && lastActiveField != null)
                {
                    var currentHand = handField.GetValue(lightgunController);
                    var lastActive = lastActiveField.GetValue(null);
                    if (!currentHand.Equals(lastActive))
                    {
                        lightgunController = null;
                        isModelReplaced = false;
                    }
                }
            }
            /*
            // Aim override and mouse emulation
            if (inFocusMode && TryGetLightgunController())
            {
                var targetField = lightgunController.GetType().GetField("attachedTarget", BindingFlags.NonPublic | BindingFlags.Instance);
                var attachedTarget = targetField?.GetValue(lightgunController) as LightgunTarget;
                if (attachedTarget?.retroarch.isRunning == true
                    && attachedTarget.retroarch.game.core == "wgc_libretro")
                {
                    // Keep OS cursor unlocked while capture core is active
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    PlayerController.SetLockedCursor(false, temp: true);
                    // Keep OS cursor unlocked while capture core is active
                    PlayerController.SetLockedCursor(false, temp: true);
                    var aimField = lightgunController.GetType().GetField("aim", BindingFlags.NonPublic | BindingFlags.Instance);
                    Transform aimTransform = aimField?.GetValue(lightgunController) as Transform
                        ?? lightgunController.transform;

                    Vector3 origin = aimTransform.localPosition;
                    Vector3 direction = aimTransform.TransformDirection(Vector3.forward);
                    if (Physics.Raycast(origin, direction, out var hit, 30f,
                        (int)LayerDatabase.Instance.LightGunTarget, QueryTriggerInteraction.Collide))
                    {
                        var sc = attachedTarget.screenController;
                        int x, y;
                        if (sc != null)
                        {
                            Vector2 uv = new Vector2(hit.textureCoord.x, 1f - hit.textureCoord.y);
                            uv = (uv - Vector2.one * 0.5f) * sc.UVScale + Vector2.one * 0.5f;
                            x = Mathf.RoundToInt(uv.x * Screen.width);
                            y = Mathf.RoundToInt(uv.y * Screen.height);
                        }
                        else
                        {
                            var screenPos = Camera.main.WorldToScreenPoint(hit.point);
                            x = Mathf.RoundToInt(screenPos.x);
                            y = Mathf.RoundToInt(Screen.height - screenPos.y);
                        }
                        logger.Debug($"[Debug] Aim Cursor: ({x},{y})");
#if UNITY_STANDALONE_WIN
                        SetCursorPos(x, y);
                        // Determine active hand
                        var lastActiveField = typeof(LightgunController).GetField("LastActiveGun", BindingFlags.Public | BindingFlags.Static);
                        var activeHandObj = lastActiveField?.GetValue(null);
                        HandType activeHand = activeHandObj is HandType ht ? ht : HandType.Left;

                        if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.Oculus)
                        {
                            var axis = activeHand == HandType.Right
                                ? OVRInput.Axis1D.SecondaryIndexTrigger
                                : OVRInput.Axis1D.PrimaryIndexTrigger;
                            float trigger = OVRInput.Get(axis);
                            if (trigger > 0.5f && !isMouseDown)
                            {
                                mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)x, (uint)y, 0, System.IntPtr.Zero);
                                isMouseDown = true;
                            }
                            else if (trigger <= 0.5f && isMouseDown)
                            {
                                mouse_event(MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, System.IntPtr.Zero);
                                isMouseDown = false;
                            }
                        }
                        else if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.OpenVR)
                        {
                            var steam = SteamVRInput.GetController(activeHand);
                            if (steam.GetPressDown(EVRButtonId.k_EButton_SteamVR_Trigger))
                                mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)x, (uint)y, 0, System.IntPtr.Zero);
                            if (steam.GetPressUp(EVRButtonId.k_EButton_SteamVR_Trigger))
                                mouse_event(MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, System.IntPtr.Zero);
                        }
#endif
                    }
                }
            }

           HandleModelSwap();
            */
        }

        private bool TryGetLightgunController()
        {
            var lastActiveField = typeof(LightgunController).GetField("LastActiveGun", BindingFlags.Public | BindingFlags.Static);
            var activeHand = lastActiveField?.GetValue(null);
            var controllers = Resources.FindObjectsOfTypeAll<MonoBehaviour>().Where(mb => mb.GetType().Name == "LightgunController");
            foreach (var ctrl in controllers)
            {
                var handField = ctrl.GetType().GetField("handType", BindingFlags.NonPublic | BindingFlags.Instance);
                var handValue = handField?.GetValue(ctrl);
                if (activeHand != null && handValue != null && handValue.Equals(activeHand))
                {
                    lightgunController = ctrl;
                    return true;
                }
            }
            return false;
        }

        private void HandleModelSwap()
        {
            if (!inFocusMode || isModelReplaced) return;

            if (ugcLightgunPrefab == null)
            {
                logger.Error("UGC Lightgun prefab not found! Cannot swap model.");
                return;
            }
            if (!TryGetLightgunController())
            {
                logger.Error("Could not find active LightgunController.");
                return;
            }

            var field = lightgunController.GetType().GetField("lightgun", BindingFlags.NonPublic | BindingFlags.Instance);
            var currentGun = field?.GetValue(lightgunController) as MonoBehaviour;
            if (currentGun == null)
            {
                logger.Error("Could not find current gun object on controller.");
                return;
            }

            if (currentGun.gameObject.activeInHierarchy)
            {
                // CLEANUP: Remove any existing replacement gun and restore default renderers.
                if (replacementGunInstance != null)
                {
                    Destroy(replacementGunInstance);
                    replacementGunInstance = null;
                }
                if (defaultGunInstance != null)
                {
                    var oldRenderers = defaultGunInstance.GetComponentsInChildren<Renderer>(true);
                    foreach (var r in oldRenderers)
                        r.enabled = true;
                }

                defaultGunInstance = currentGun.gameObject;

                // Hide the original model only (NOT the scripts):
                var modelRenderers = defaultGunInstance.GetComponentsInChildren<Renderer>();
                if (modelRenderers == null || modelRenderers.Length == 0)
                {
                    logger.Warning("No Renderer components found on original gun to hide!");
                }
                else
                {
                    foreach (var r in modelRenderers)
                        r.enabled = false;
                }

                // Instantiate your replacement model (visual only)
                replacementGunInstance = Instantiate(ugcLightgunPrefab.gameObject, lightgunController.transform);
                replacementGunInstance.transform.localPosition = defaultGunInstance.transform.localPosition;
                replacementGunInstance.transform.localRotation = defaultGunInstance.transform.localRotation;
                replacementGunInstance.transform.localScale = defaultGunInstance.transform.localScale;
                replacementGunInstance.SetActive(true);

                logger.Debug("Swapped in replacement gun model successfully.");

                // Mark as replaced so it doesn't repeat every frame:
                isModelReplaced = true;
            }
        }


        private void StartFocusMode()
        {
            logger.Debug("Starting Focus Mode. Grab the Gun!");
            inFocusMode = true;
            flashlight?.lightObject?.SetActive(false);
            // Ensure OS cursor remains unlocked during capture
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            PlayerController.SetLockedCursor(false, temp: true);
        }

        private void EndFocusMode()
        {
            logger.Debug("Ending Focus Mode.");
            inFocusMode = false;

            // Destroy replacement and reset all renderers
            if (replacementGunInstance != null)
            {
                Destroy(replacementGunInstance);
                replacementGunInstance = null;
            }
            if (defaultGunInstance != null)
            {
                var modelRenderers = defaultGunInstance.GetComponentsInChildren<Renderer>(true);
                foreach (var r in modelRenderers)
                    r.enabled = true;
                defaultGunInstance.SetActive(true);
            }
            isModelReplaced = false;
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

        // Helper class to extract and sanitize file names.
        public static class FileNameHelper
        {
            public static string GetFileName(string filePath)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                return System.Text.RegularExpressions.Regex.Replace(fileName, "[\\/:*?\"<>|]", "_");
            }
        }
        public static class KeyEmulator
        {
            // Virtual key codes for Q and E
            const byte VK_Q = 0x51;
            const byte VK_E = 0x45;
            const uint KEYEVENTF_KEYDOWN = 0x0000;
            const uint KEYEVENTF_KEYUP = 0x0002;

            [DllImport("user32.dll")]
            static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

            public static void SendQandEKeypress()
            {
                // Send Q down
                keybd_event(VK_Q, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                // Send E down
                keybd_event(VK_E, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);

                // Send Q up
                keybd_event(VK_Q, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                // Send E up
                keybd_event(VK_E, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
        }
    }
}
