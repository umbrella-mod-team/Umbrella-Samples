using UnityEngine;
using System.Runtime.InteropServices;
using Mirror;
using WIGU;
using System;

namespace WIGUx.Modules.LightgunCaptureController
{
    /// <summary>
    /// This module forces lightgun input to be active when either the configuration conditions are met
    /// (Game.lightgun nonzero and Game.core equals "wgc_libretro") or when the user presses the F key to force-enable.
    /// It loads the lightgun model (or custom UGC if needed), applies the same grip snapping and gun alignment offset,
    /// then performs raycasting from the gun's aim transform to compute normalized coordinates (using SCALE_CENTER
    /// and the ScreenController's UVScale from LightgunTarget), and finally converts these coordinates into OS mouse events.
    /// VR trigger input is mapped to OS mouse clicks.
    /// </summary>
    public class CaptureEnabledLightgunModule : NetworkBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        #region Configuration (Read from GameSystem)
        private int configLightgun = 0;
        private string configCore = "";
        #endregion

        [Header("Lightgun Model Settings")]
        [Tooltip("ID of the lightgun model to load (or for custom UGC if lightgun==9).")]
        public int lightgunId = 1;
        [Tooltip("Transform used for snapping the lightgun model (grip position).")]
        public Transform grip;
        [Tooltip("Optional transform for additional offset.")]
        public Transform sightGrip;

        [Header("VR & Input Settings")]
        [Tooltip("Reference to the HandController (for dual-hand support).")]
        public HandController handController;
        [Tooltip("Layer mask for the cabinet screen target (must match LightgunTarget layer).")]
        public LayerMask targetLayer;
        [Tooltip("Maximum raycast distance for aiming.")]
        public float maxRayDistance = 30f;

        [Header("Debug Settings")]
        [Tooltip("Enable debug logging.")]
        public bool debugMode = true;
        [Tooltip("Press this key to force-enable the capture module (for debugging).")]
        public KeyCode forceEnableKey = KeyCode.F;

        // Constants from the original system.
        private static readonly Vector2 RELOAD_UV = new Vector2(-2f, -2f);
        private static readonly Vector2 SCALE_CENTER = new Vector2(0.5f, 0.5f);

        // Private references for the lightgun model and its aim transform.
        private Lightgun lightgun;
        private Transform gunAim; // Expected to be lightgun.aim

        // For tracking VR trigger state.
        private bool previousTriggerState = false;
        // For accumulating spin input.
        private float currentSpinAngle = 0f;

#if UNITY_STANDALONE_WIN
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);
        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP   = 0x0004;
#endif

        void Start()
        {
            // Read configuration from the active GameSystem.
            ReadConfig();
            if (debugMode)
                Debug.Log("[CaptureModule] Config read: lightgun = " + configLightgun + ", core = " + configCore);

            // Only activate normally if config conditions are met.
            if (configLightgun != 0 && configCore.Equals("wgc_libretro", StringComparison.OrdinalIgnoreCase))
            {
                if (debugMode)
                    Debug.Log("[CaptureModule] Config conditions met. Enabling lightgun input.");
                SetupGun();
            }
            else
            {
                if (debugMode)
                    Debug.Log("[CaptureModule] Config conditions not met. Module is disabled normally.");
                // But allow force enabling via key.
                enabled = true;
            }
        }

        void Update()
        {
            // If the force-enable key is pressed, override configuration.
            if (Input.GetKeyDown(forceEnableKey))
            {
                if (debugMode)
                    Debug.Log("[CaptureModule] Force-enable key pressed. Overriding configuration.");
                // Override config for debugging purposes.
                configLightgun = 1;
                configCore = "wgc_libretro";
                if (lightgun == null)
                    SetupGun();
            }

            // Only process if configuration is met (either naturally or via force enable).
            if (configLightgun == 0 || !configCore.Equals("wgc_libretro", StringComparison.OrdinalIgnoreCase))
                return;

            if (lightgun == null)
            {
                if (debugMode)
                    Debug.LogWarning("[CaptureModule] Lightgun model missing. Attempting to load.");
                SetupGun();
                return;
            }

            if (gunAim != null && debugMode)
            {
                Debug.Log("[CaptureModule] GunAim Position: " + gunAim.position + " | Forward: " + gunAim.forward);
            }

            // --- VR Input Mapping (Dual-Hand) ---
            bool isLeft = handController != null && handController.HandType == HandType.Left;
            float triggerValue = 0f;
            bool triggerPressed = false;
            bool spinActive = false;

            if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.Oculus)
            {
                triggerValue = OVRInput.Get(isLeft ? OVRInput.RawAxis1D.LIndexTrigger : OVRInput.RawAxis1D.RIndexTrigger, OVRInput.Controller.Touch);
                triggerPressed = OVRInput.Get(isLeft ? OVRInput.RawButton.LHandTrigger : OVRInput.RawButton.RHandTrigger, OVRInput.Controller.Touch);
                spinActive = OVRInput.Get(isLeft ? OVRInput.RawButton.LThumbstick : OVRInput.RawButton.RThumbstick, OVRInput.Controller.Touch);
            }
            else if (SteamVRInput.TouchMode)
            {
                triggerValue = SteamVRInput.GetAxis(SteamVRInput.TouchAxis.Trigger, isLeft ? HandType.Left : HandType.Right);
                triggerPressed = SteamVRInput.Get(isLeft ? SteamVRInput.TouchButton.LGrip : SteamVRInput.TouchButton.RGrip);
                spinActive = SteamVRInput.Get(isLeft ? SteamVRInput.TouchButton.LDpadUp : SteamVRInput.TouchButton.RDpadUp);
            }
            else if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.OpenVR)
            {
                triggerValue = SteamVRInput.GetTriggerValue(isLeft ? HandType.Left : HandType.Right);
                triggerPressed = SteamVRInput.Get(isLeft ? SteamVRInput.Button.LGrip : SteamVRInput.Button.RGrip);
                spinActive = SteamVRInput.Get(isLeft ? SteamVRInput.Button.LPadUp : SteamVRInput.Button.RPadUp);
            }
            if (debugMode)
            {
                Debug.Log("[CaptureModule] Hand: " + (isLeft ? "Left" : "Right") +
                          " | Trigger value: " + triggerValue +
                          ", Trigger pressed: " + triggerPressed +
                          " | Spin active: " + spinActive);
            }
            if (spinActive)
            {
                SpinGun(2f); // Spin by 2° per frame; adjust as needed.
            }

            // --- Aiming: Cast a ray from gunAim ---
            if (gunAim == null)
            {
                if (debugMode)
                    Debug.LogWarning("[CaptureModule] Gun aim transform is null.");
                return;
            }
            Ray ray = new Ray(gunAim.position, gunAim.forward);
            Debug.DrawRay(gunAim.position, gunAim.forward * maxRayDistance, Color.red);
            if (debugMode)
                Debug.Log("[CaptureModule] Raycast origin: " + gunAim.position + " | Direction: " + gunAim.forward);

            Vector2 rawAim = Vector2.zero;
            bool validHit = false;
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo, maxRayDistance, targetLayer))
            {
                rawAim = new Vector2(hitInfo.textureCoord.x, 1f - hitInfo.textureCoord.y);
                validHit = true;
                if (debugMode)
                    Debug.Log("[CaptureModule] Raycast hit: " + hitInfo.collider.name +
                              " | TextureCoord: " + hitInfo.textureCoord +
                              " | RawAim: " + rawAim);
            }
            else
            {
                rawAim = RELOAD_UV;
                if (debugMode)
                    Debug.Log("[CaptureModule] Raycast did not hit; using RELOAD_UV: " + RELOAD_UV);
            }

            // --- Remap Raw Aim ---
            Vector2 uvScale = Vector2.one;
            if (validHit)
            {
                LightgunTarget target = hitInfo.transform.parent.GetComponent<LightgunTarget>();
                if (target != null && target.screenController != null)
                {
                    uvScale = target.screenController.UVScale;
                    if (debugMode)
                        Debug.Log("[CaptureModule] Using UVScale from ScreenController: " + uvScale);
                }
            }
            Vector2 scaledAim = (rawAim - SCALE_CENTER) * uvScale + SCALE_CENTER;
            if (debugMode)
                Debug.Log("[CaptureModule] Scaled aim: " + scaledAim);

            // --- Convert to OS Screen Coordinates ---
            int screenX = (int)(scaledAim.x * Screen.width);
            int screenY = (int)((1f - scaledAim.y) * Screen.height);
            if (debugMode)
                Debug.Log("[CaptureModule] Mapped screen coordinates: (" + screenX + ", " + screenY + ")");

#if UNITY_STANDALONE_WIN
            if (validHit)
            {
                if (debugMode)
                    Debug.Log("[CaptureModule] Setting OS cursor position to: (" + screenX + ", " + screenY + ")");
                SetCursorPos(screenX, screenY);
            }
#endif

            // --- Trigger Processing ---
#if UNITY_STANDALONE_WIN
            if (triggerPressed && !previousTriggerState)
            {
                if (debugMode)
                    Debug.Log("[CaptureModule] Trigger pressed: simulating mouse down.");
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            }
            else if (!triggerPressed && previousTriggerState)
            {
                if (debugMode)
                    Debug.Log("[CaptureModule] Trigger released: simulating mouse up.");
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            }
#endif
            previousTriggerState = triggerPressed;
        }

        /// <summary>
        /// Reads configuration values from the active GameSystem's Game property.
        /// </summary>
        private void ReadConfig()
        {
            GameSystem gs = FindObjectOfType<GameSystem>();
            if (gs != null && gs.Game != null)
            {
                configLightgun = gs.Game.lightgun; // nonzero enables input (e.g. 4, 9, etc.)
                configCore = gs.Game.core;
            }
            else if (debugMode)
            {
                Debug.LogWarning("[CaptureModule] Could not find GameSystem or its Game property; using defaults.");
            }
        }

        /// <summary>
        /// Loads the lightgun model using Lightgun.Load and applies grip snapping and gun alignment offset.
        /// If configLightgun equals 9, custom UGC logic can be inserted.
        /// </summary>
        void SetupGun()
        {
            if (lightgun != null)
                Destroy(lightgun.gameObject);

            if (configLightgun == 9)
            {
                if (debugMode)
                    Debug.Log("[CaptureModule] Custom UGC mode detected (lightgun==9). Loading custom UGC lightgun.");
                // TODO: Insert custom UGC loading logic here.
                lightgun = Lightgun.Load(lightgunId, this.transform); // Placeholder.
            }
            else
            {
                if (debugMode)
                    Debug.Log("[CaptureModule] Loading standard lightgun model.");
                lightgun = Lightgun.Load(lightgunId, this.transform);
            }

            if (lightgun == null)
            {
                Debug.LogError("[CaptureModule] Failed to load lightgun model with id: " + lightgunId);
                return;
            }
            lightgun.gameObject.SetActive(true);
            gunAim = lightgun.aim;
            if (gunAim == null)
            {
                Debug.LogError("[CaptureModule] Gun aim transform is missing in the loaded model.");
                return;
            }
            if (debugMode)
                Debug.Log("[CaptureModule] Lightgun model loaded successfully.");
            ResetPosition();
        }

        /// <summary>
        /// Positions the lightgun model by snapping the grip and applying the gun alignment offset from Settings.
        /// </summary>
        void ResetPosition()
        {
            if (lightgun.transform.parent != this.transform)
                lightgun.transform.parent = this.transform;
            if (grip != null)
            {
                lightgun.SnapGrip(grip);
                if (debugMode)
                    Debug.Log("[CaptureModule] SnapGrip applied using grip transform.");
            }
            else if (debugMode)
            {
                Debug.LogWarning("[CaptureModule] Grip transform is not assigned.");
            }
            if (lightgun.pivot != null)
            {
                if (debugMode)
                    Debug.Log("[CaptureModule] Applying gun alignment offset: " + Settings.GunAlignment);
                lightgun.transform.RotateAround(lightgun.pivot.position,
                    -lightgun.pivot.right, Settings.GunAlignment);
            }
            else if (debugMode)
            {
                Debug.LogWarning("[CaptureModule] Lightgun pivot is not assigned.");
            }
        }

        /// <summary>
        /// Rotates the lightgun model about its pivot in response to spin input.
        /// </summary>
        public void SpinGun(float spinDelta)
        {
            if (lightgun == null || lightgun.pivot == null)
                return;
            currentSpinAngle += spinDelta;
            if (debugMode)
                Debug.Log("[CaptureModule] Spinning gun by " + spinDelta + "°; total spin: " + currentSpinAngle);
            lightgun.transform.RotateAround(lightgun.pivot.position, Vector3.up, spinDelta);
        }
    }
}
