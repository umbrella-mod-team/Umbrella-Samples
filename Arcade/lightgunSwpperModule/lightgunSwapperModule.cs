using MelonLoader.ICSharpCode.SharpZipLib.GZip;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using WIGU;


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
        private MonoBehaviour lightgunControllerRaw; // used for reflection
        private GameSystemState systemState;
        private bool isVRActive = false;
        private GameObject playerCamera;   // Reference to the Player Camera
        private GameObject playerVRSetup;   // Reference to the VR Camera
        private GameObject aimLine;
        private Rect currentCaptureRect;
        private IntPtr currentCaptureHwnd = IntPtr.Zero;
        private Vector2 lastMousePos;
        private const float RECT_CHECK_INTERVAL = 1.0f;
        private float rectCheckTimer = 0f;
        private bool isMouseDown = false;
        private bool isMiddleDown = false;
        private bool isRightDown = false;
        private bool isXButton1Down = false;
        private bool isXButton2Down = false;
        private List<string> winFileLines = new List<string>();
        private IntPtr currentHwnd = IntPtr.Zero;
        private string lastTopLine = null;
        private string lastWinFilePath = null;
        private LightgunController lightgunController;
        private GameObject gunObject;
        private GameObject[] gunClones;
        private GameObject[] triggerClones;
        private GameObject[] originalArms;
        private Transform[] triggers;
        private bool isInitialized;
        private bool isLeftHand;
        private HandType? lastResolvedHand = null;   // tracks last trigger pressed
        private MonoBehaviour leftController;
        private MonoBehaviour rightController;
        // --- Controller transform snapshots (Aim/Sight/Pivot only, no Grip) ---
        private struct ControllerTransformSnapshot
        {
            public Vector3 pos;
            public Quaternion rot;
            public bool valid;
        }

        private ControllerTransformSnapshot[] savedAim;
        private ControllerTransformSnapshot[] savedSight;
        private ControllerTransformSnapshot[] savedPivot;
        private bool savedControllerDefaults = false;

        [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")] private static extern bool SetCursorPos(int X, int Y);
        [DllImport("user32.dll")] private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, IntPtr dwExtraInfo);

        private RECT GetWindowBounds(IntPtr hwnd)
        {
            RECT rect;
            if (GetWindowRect(hwnd, out rect))
                return rect;
            return new RECT();
        }

        public struct RECT
        {
            public int Left, Top, Right, Bottom;
            public int width => Right - Left;
            public int height => Bottom - Top;
            public int x => Left;
            public int y => Top;
        }

        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;
        private const uint MOUSEEVENTF_HWHEEL = 0x01000;
        private const uint MOUSEEVENTF_XDOWN = 0x0080;
        private const uint MOUSEEVENTF_XUP = 0x0100;
        private const int XBUTTON1 = 0x0001; // Mouse 4 (Back)
        private const int XBUTTON2 = 0x0002; // Mouse 5 (Forward)

        void Start()
        {
            CheckInsertedGameName();
            CheckControlledGameName();
            gameSystem = GetComponent<GameSystem>();
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
            isVRActive = (playerVRSetup != null);
            if (inFocusMode) // only while in focus
            {
                var controllers = Resources.FindObjectsOfTypeAll<MonoBehaviour>()
                    .Where(mb => mb.isActiveAndEnabled && mb.GetType().Name == "LightgunController")
                    .ToList();

                foreach (var ctrl in controllers)
                {
                    var handField = ctrl.GetType().GetField("handType", BindingFlags.NonPublic | BindingFlags.Instance);
                    var handValue = handField?.GetValue(ctrl);

                    if (handValue is HandType ht)
                    {
                        if (ht == HandType.Left) leftController = ctrl;
                        else if (ht == HandType.Right) rightController = ctrl;
                        // ignore anything else (like Head slot)
                    }
                }
            }

            //gunObject = FindChild(gameObject, "Gun");
            gunObject = transform.Find("Gun")?.gameObject;
            if (this.gunObject != null)
                logger.Debug($"[LightGun] Custom Gun model found: {this.gunObject.name}");
            else
                logger.Error("[LightGun] Custom Gun model NOT found in cabinet.");
            this.gunClones = new GameObject[3];
            this.triggerClones = new GameObject[3];
            this.originalArms = new GameObject[3];
            this.triggers = new Transform[3];
            this.originalArms[0] = GameObject.Find("Head/LightgunController");
            this.originalArms[1] = GameObject.Find("Hands/HandRight/r_hand_skeletal_lowres/hands:hands_geom/LightgunController");
            this.originalArms[2] = GameObject.Find("Hands/HandLeft/l_hand_skeletal_lowres/hands:hands_geom/LightgunController");

            flashlight = GetComponentInChildren<Flashlight>();
            if (flashlight?.lightObject != null)
                flashlight.lightObject.SetActive(true);
        }

        // 1) Update(): use existing focus/core gates, call TryComputeCursorXY → HandleMouseInjection
        private void Update()
        {
            CheckInsertedGameName();
            CheckControlledGameName();
            // Track trigger use even outside focus so we know last hand
            if (IsTriggerPressed(HandType.Left))
            {
                if (lastResolvedHand != HandType.Left)
                {
                    lastResolvedHand = HandType.Left;
                    if (gunClones[2] == null)   // index 2 = left hand
                        StartCoroutine(SpawnCloneOnSwap(HandType.Left));
                }
            }
            else if (IsTriggerPressed(HandType.Right))
            {
                if (lastResolvedHand != HandType.Right)
                {
                    lastResolvedHand = HandType.Right;
                    if (gunClones[1] == null)   // index 1 = right hand
                        StartCoroutine(SpawnCloneOnSwap(HandType.Right));
                }
            }
            // === Focus enter/exit ===
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

            // capture†core only path
            if (inFocusMode && TryGetLightgunController())
            {
                // Refresh active controller
                var targetField = lightgunControllerRaw.GetType().GetField("attachedTarget", BindingFlags.NonPublic | BindingFlags.Instance);
                var attachedTarget = targetField?.GetValue(lightgunControllerRaw) as LightgunTarget;

                bool triggerPressed = IsTriggerPressed(HandType.Left) || IsTriggerPressed(HandType.Right);

                // If RetroArch core is available, honor it; otherwise still inject mouse
                bool allowMouse = false;
                if (attachedTarget?.retroarch != null)
                {
                    if (attachedTarget.retroarch.isRunning &&
                        attachedTarget.retroarch.game?.core == "wgc_libretro")
                    {
                        allowMouse = true;
                    }
                }
                else
                {
                    // No retroarch target found → fallback for desktop/capture
                    allowMouse = true;
                }

                if (allowMouse)
                {
                    int x, y;
                    bool aimHit = TryComputeCursorXY(out x, out y);
                    if (aimHit)
                    {
                        HandleMouseInjection(x, y, triggerPressed);
                    }
                    else
                    {
                        //    logger.Debug("[LightGun] no hit (raycast MISS)");

                        if (triggerPressed)
                        {
                            // Off-screen fire = reload
                            mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, IntPtr.Zero);
                            mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, IntPtr.Zero);
                            logger.Debug("[LightGun] Middle Mouse Click (reload/offscreen)");

                            if (isMouseDown)
                            {
                                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);
                                isMouseDown = false;
                            }
                        }
                    }
                }
                /*
if (aimLine == null)
{
  aimLine = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
  Destroy(aimLine.GetComponent<Collider>());
  aimLine.GetComponent<Renderer>().material.color = Color.red;
  aimLine.SetActive(false);
}

      rectCheckTimer += Time.unscaledDeltaTime;
      if (rectCheckTimer >= RECT_CHECK_INTERVAL)
      {
          rectCheckTimer = 0f;
          TryRefreshRectAndHwnd();
      }
       */
            }
        }
        // 2) SaveControllerTransforms: store Aim/Sight/Pivot of both controllers for later restoration
        private void SaveControllerTransforms()
        {
            if (originalArms == null) return;

            int n = originalArms.Length;
            savedAim = new ControllerTransformSnapshot[n];
            savedSight = new ControllerTransformSnapshot[n];
            savedPivot = new ControllerTransformSnapshot[n];

            for (int i = 0; i < n; ++i)
            {
                var arm = originalArms[i];
                if (arm == null) continue;

                var aimT = arm.transform.Find("Aim");
                if (aimT != null)
                {
                    savedAim[i].pos = aimT.localPosition;
                    savedAim[i].rot = aimT.localRotation;
                    savedAim[i].valid = true;
                }

                var sightT = arm.transform.Find("Sight") ?? arm.transform.Find("SightGrip") ?? arm.transform.Find("sight");
                if (sightT != null)
                {
                    savedSight[i].pos = sightT.localPosition;
                    savedSight[i].rot = sightT.localRotation;
                    savedSight[i].valid = true;
                }

                var pivotT = arm.transform.Find("Pivot") ?? arm.transform.Find("spinPivot") ?? arm.transform.Find("pivot");
                if (pivotT != null)
                {
                    savedPivot[i].pos = pivotT.localPosition;
                    savedPivot[i].rot = pivotT.localRotation;
                    savedPivot[i].valid = true;
                }
            }

            savedControllerDefaults = true;
            logger.Debug("[LightGun] Aim/Sight/Pivot transforms saved (local).");
        }



        // 3) TryComputeCursorXY — mirrors stock LightgunController raycast parameters
        private bool TryComputeCursorXY(out int x, out int y)
        {
            x = 0; y = 0;
            if (lightgunControllerRaw == null) return false;

            var aimField = lightgunControllerRaw.GetType().GetField("aim", BindingFlags.NonPublic | BindingFlags.Instance);
            Transform aimTransform = aimField?.GetValue(lightgunControllerRaw) as Transform ?? lightgunControllerRaw.transform;

            Vector3 origin = aimTransform.position;
            Vector3 direction = aimTransform.TransformDirection(Vector3.forward);

            //  logger.Debug($"[AIM] origin: ({origin.x:F6}, {origin.y:F6}, {origin.z:F6}) direction: ({direction.x:F6}, {direction.y:F6}, {direction.z:F6})");

            if (aimLine != null)
            {
                aimLine.SetActive(true);
                Vector3 mid = origin + direction * 15f;
                aimLine.transform.position = mid;
                aimLine.transform.up = direction.normalized;
                aimLine.transform.localScale = new Vector3(0.01f, 15f, 0.01f);
            }

            UnityEngine.RaycastHit hit;
            int mask = (int)LayerDatabase.Instance.LightGunTarget;
            bool ok = Physics.Raycast(origin, direction, out hit, 30f, mask, QueryTriggerInteraction.Collide);
            if (!ok)
            {
                //   logger?.Debug("[AIM] Raycast MISS");
                return false;
            }

            var targetField = lightgunControllerRaw.GetType().GetField("attachedTarget", BindingFlags.NonPublic | BindingFlags.Instance);
            var attachedTarget = targetField?.GetValue(lightgunControllerRaw) as LightgunTarget;
            var sc = attachedTarget?.screenController;

            float screenX, screenY;

            if (sc != null)
            {
                Vector2 uv = new Vector2(hit.textureCoord.x, 1f - hit.textureCoord.y);
                uv = (uv - Vector2.one * 0.5f) * sc.UVScale + Vector2.one * 0.5f;
                screenX = uv.x;
                screenY = uv.y;
            }
            else
            {
                var sp = Camera.main.WorldToScreenPoint(hit.point);
                screenX = sp.x / Screen.width;
                screenY = 1f - (sp.y / Screen.height);
            }

            screenX = Mathf.Clamp01(screenX);
            screenY = Mathf.Clamp01(screenY);

            if (currentCaptureRect.width > 0 && currentCaptureRect.height > 0)
            {
                x = Mathf.RoundToInt(currentCaptureRect.x + screenX * currentCaptureRect.width);
                y = Mathf.RoundToInt(currentCaptureRect.y + screenY * currentCaptureRect.height);
            }
            else
            {
                x = Mathf.RoundToInt(screenX * Screen.width);
                y = Mathf.RoundToInt(screenY * Screen.height);
            }
            // logger?.Debug($"coordance: {x},{y}");
            return true;
        }
        private bool IsThumbstickClick()
        {
            if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.Oculus)
            {
                return OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch)
                    || OVRInput.Get(OVRInput.Button.SecondaryThumbstick, OVRInput.Controller.RTouch);
            }
            else if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.OpenVR)
            {
                return SteamVRInput.GetDown(SteamVRInput.TouchButton.LThumbstick)
                    || SteamVRInput.GetDown(SteamVRInput.TouchButton.RThumbstick);
            }
            return false;
        }

        private bool IsTopButton()
        {
            if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.Oculus)
            {
                return OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.RTouch)
                    || OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.LTouch);
            }
            else if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.OpenVR)
            {
                return SteamVRInput.GetDown(SteamVRInput.TouchButton.Y)    // Left controller top
                    || SteamVRInput.GetDown(SteamVRInput.TouchButton.A);   // Right controller top
            }
            return false;
        }

        private bool IsBottomButton()
        {
            if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.Oculus)
            {
                return OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.RTouch)
                    || OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.LTouch);
            }
            else if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.OpenVR)
            {
                return SteamVRInput.GetDown(SteamVRInput.TouchButton.A)
                    || SteamVRInput.GetDown(SteamVRInput.TouchButton.B);
            }
            return false;
        }

        private void HandleMouseInjection(int x, int y, bool triggerDown)
        {
            SetCursorPos(x, y);
            // Force Windows to notice the cursor moved (fix for some fullscreen games)
            mouse_event(MOUSEEVENTF_MOVE, 0, 0, 0, IntPtr.Zero);

            // === PRIMARY FIRE (Left Click) ===
            if (triggerDown && !isMouseDown)
            {
                mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)x, (uint)y, 0, IntPtr.Zero);
                isMouseDown = true;
                logger.Debug($"[LightGun] Left Mouse Click sent at coordance: {x},{y}");
            }
            else if (!triggerDown && isMouseDown)
            {
                mouse_event(MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, IntPtr.Zero);
                isMouseDown = false;
                logger.Debug("[LightGun] Left Mouse Click released");
            }

            // === VR Button Emulation ===
            var lastActiveField = typeof(LightgunController).GetField("LastActiveGun", BindingFlags.Public | BindingFlags.Static);
            var activeHandObj = lastActiveField?.GetValue(null);
            HandType activeHand = activeHandObj is HandType ht ? ht : HandType.Left;

            bool topLeftDown = false, topLeftUp = false;
            bool topRightDown = false, topRightUp = false;
            bool botLeftDown = false, botLeftUp = false;
            bool botRightDown = false, botRightUp = false;
            bool stickLeftDown = false, stickLeftUp = false;
            bool stickRightDown = false, stickRightUp = false;

            if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.Oculus)
            {
                topLeftDown = OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch);
                topLeftUp = OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.LTouch);
                topRightDown = OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch);
                topRightUp = OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.RTouch);

                botLeftDown = OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch);
                botLeftUp = OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.LTouch);
                botRightDown = OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch);
                botRightUp = OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch);

                stickLeftDown = OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);
                stickLeftUp = OVRInput.GetUp(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);
                stickRightDown = OVRInput.GetDown(OVRInput.Button.SecondaryThumbstick, OVRInput.Controller.RTouch);
                stickRightUp = OVRInput.GetUp(OVRInput.Button.SecondaryThumbstick, OVRInput.Controller.RTouch);
            }
            else if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.OpenVR)
            {
                topLeftDown = SteamVRInput.GetDown(SteamVRInput.TouchButton.Y);
                topLeftUp = SteamVRInput.GetUp(SteamVRInput.TouchButton.Y);
                topRightDown = SteamVRInput.GetDown(SteamVRInput.TouchButton.X);
                topRightUp = SteamVRInput.GetUp(SteamVRInput.TouchButton.X);

                botLeftDown = SteamVRInput.GetDown(SteamVRInput.TouchButton.B);
                botLeftUp = SteamVRInput.GetUp(SteamVRInput.TouchButton.B);
                botRightDown = SteamVRInput.GetDown(SteamVRInput.TouchButton.A);
                botRightUp = SteamVRInput.GetUp(SteamVRInput.TouchButton.A);

                stickLeftDown = SteamVRInput.GetDown(SteamVRInput.TouchButton.LThumbstick);
                stickLeftUp = SteamVRInput.GetUp(SteamVRInput.TouchButton.LThumbstick);
                stickRightDown = SteamVRInput.GetDown(SteamVRInput.TouchButton.RThumbstick);
                stickRightUp = SteamVRInput.GetUp(SteamVRInput.TouchButton.RThumbstick);
            }
            // LEFT controller → Middle Mouse
            if (botLeftDown) { mouse_event(MOUSEEVENTF_MIDDLEDOWN, (uint)x, (uint)y, 0, IntPtr.Zero); isMiddleDown = true; }
            if (botLeftUp) { mouse_event(MOUSEEVENTF_MIDDLEUP, (uint)x, (uint)y, 0, IntPtr.Zero); isMiddleDown = false; }

            // RIGHT controller → Right Mouse
            if (botRightDown) { mouse_event(MOUSEEVENTF_RIGHTDOWN, (uint)x, (uint)y, 0, IntPtr.Zero); isRightDown = true; }
            if (botRightUp) { mouse_event(MOUSEEVENTF_RIGHTUP, (uint)x, (uint)y, 0, IntPtr.Zero); isRightDown = false; }


            // LEFT controller → Right Mouse
            if (topLeftDown) { mouse_event(MOUSEEVENTF_RIGHTDOWN, (uint)x, (uint)y, 0, IntPtr.Zero); isRightDown = true; }
            if (topLeftUp) { mouse_event(MOUSEEVENTF_RIGHTUP, (uint)x, (uint)y, 0, IntPtr.Zero); isRightDown = false; }

            // RIGHT controller → Middle Mouse
            if (topRightDown) { mouse_event(MOUSEEVENTF_MIDDLEDOWN, (uint)x, (uint)y, 0, IntPtr.Zero); isMiddleDown = true; }
            if (topRightUp) { mouse_event(MOUSEEVENTF_MIDDLEUP, (uint)x, (uint)y, 0, IntPtr.Zero); isMiddleDown = false; }


            // === Stick Clicks → XBUTTON1/XBUTTON2 ===
            if (stickLeftDown)
                mouse_event(MOUSEEVENTF_XDOWN, (uint)x, (uint)y, XBUTTON1, IntPtr.Zero);
            if (stickLeftUp)
                mouse_event(MOUSEEVENTF_XUP, (uint)x, (uint)y, XBUTTON1, IntPtr.Zero);

            if (stickRightDown)
                mouse_event(MOUSEEVENTF_XDOWN, (uint)x, (uint)y, XBUTTON2, IntPtr.Zero);
            if (stickRightUp)
                mouse_event(MOUSEEVENTF_XUP, (uint)x, (uint)y, XBUTTON2, IntPtr.Zero);

            EmitMouseWheelFromThumbstick();
        }

        private void EmitMouseWheelFromThumbstick()
        {
            const float DEADZONE = 0.45f;  // tweak if needed
            const int WHEEL_DELTA = 120;

            Vector2 leftStick = SteamVRInput.GetAnalog(HandType.Left);

            // === Mouse Wheel Vertical ===
            if (Mathf.Abs(leftStick.y) >= DEADZONE)
            {
                int scrollAmount = (int)(WHEEL_DELTA * Mathf.Sign(leftStick.y));
                mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)scrollAmount, IntPtr.Zero);
                //  logger.Debug($"[LightGun] Mouse Wheel {(scrollAmount > 0 ? "Up" : "Down")} via Left Stick Y: {leftStick.y:F2}");
            }

            // === Mouse Wheel Horizontal ===
            if (Mathf.Abs(leftStick.x) >= DEADZONE)
            {
                int hScrollAmount = (int)(WHEEL_DELTA * Mathf.Sign(leftStick.x));
                mouse_event(MOUSEEVENTF_HWHEEL, 0, 0, (uint)hScrollAmount, IntPtr.Zero);
                // logger.Debug($"[LightGun] Mouse Wheel Horizontal {(hScrollAmount > 0 ? "Right" : "Left")} via Left Stick X: {leftStick.x:F2}");
            }
        }


        public static bool IsTriggerPressed(HandType handType)
        {
            float t = 0f;

            if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.Oculus)
            {
                t = (handType == HandType.Left)
                    ? OVRInput.Get(OVRInput.RawAxis1D.LIndexTrigger, OVRInput.Controller.Touch)
                    : OVRInput.Get(OVRInput.RawAxis1D.RIndexTrigger, OVRInput.Controller.Touch);
            }
            else if (SteamVRInput.TouchMode)
            {
                t = SteamVRInput.GetAxis(SteamVRInput.TouchAxis.Trigger, handType);
            }
            else if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.OpenVR)
            {
                t = SteamVRInput.GetTriggerValue(handType);
            }
            else
            {
                // fallback for mouse/gamepad/keyboard
                if (XInput.Get(XInput.Button.A) || XInput.Get(XInput.Button.RIndexTrigger) ||
                    Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0))
                {
                    t = 1f;
                }
            }

            return t > 0.3f;
        }
        private void ParseWinFile()
        {
            if (GameSystem.ControlledSystem != null && GameSystem.ControlledSystem.Game != null && !string.IsNullOrEmpty(GameSystem.ControlledSystem.Game.path))
            {
                string emuVRRoot = Path.Combine(Application.dataPath, ".."); // Gets the EmuVR base directory
                string rawRelativePath = GameSystem.ControlledSystem?.Game?.path;

                if (!string.IsNullOrEmpty(rawRelativePath))
                {
                    string fullWinPath = Path.Combine(emuVRRoot, rawRelativePath);

                    if (File.Exists(fullWinPath))
                    {
                        lastWinFilePath = fullWinPath;

                        winFileLines = File.ReadAllLines(fullWinPath).ToList();
                        lastTopLine = winFileLines.FirstOrDefault();

                        logger.Info($"[LightGun] Parsed .win file: {fullWinPath}");
                        foreach (var line in winFileLines)
                            logger.Info(" .win entry: " + line);
                    }
                    else
                    {
                        logger.Warning($"[LightGun] .win file NOT FOUND: {fullWinPath}");
                    }
                }
                else
                {
                    logger.Warning("[LightGun] No .win file path provided from GameSystem.");
                }

            }
        }

        private void SetWindowCaptureRectFromWinFile(string winFilePath)
        {
            if (!File.Exists(winFilePath)) return;


            string[] lines = File.ReadAllLines(winFilePath);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;


                // Skip launcher or second windows if any are marked
                if (line.Contains("<") || line.StartsWith("#")) continue;


                string trimmed = line.Trim();


                // Try to find by window title
                IntPtr hwnd = FindWindow(null, trimmed);
                if (hwnd == IntPtr.Zero)
                {
                    // Try by class name (rare fallback)
                    hwnd = FindWindow(trimmed, null);
                }


                if (hwnd != IntPtr.Zero)
                {
                    RECT r;
                    if (GetWindowRect(hwnd, out r))
                    {
                        currentCaptureRect = new Rect(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
                        currentCaptureHwnd = hwnd;
                        SetForegroundWindow(hwnd); // bring to front
                        logger?.Debug($"[CAPTURE] Acquired window '{trimmed}' at rect: {currentCaptureRect}");
                        break; // First matching window found
                    }
                }
            }
        }

        private void TryRefreshRectAndHwnd()
        {
            if (winFileLines == null || winFileLines.Count == 0)
                return;


            foreach (var line in winFileLines)
            {
                string exe = Path.GetFileNameWithoutExtension(line).ToLowerInvariant();
                string titleFragment = line.Contains(".exe") ? line.Substring(line.IndexOf(".exe") + 4).Trim() : line;
                if (TryFindMatchingWindow(exe, titleFragment, out var hwnd))
                {
                    if (hwnd != currentHwnd || !GetWindowRect(hwnd, out RECT r) || RectFromRECT(r) != currentCaptureRect)
                    {
                        currentHwnd = hwnd;
                        GetWindowRect(hwnd, out r);
                        currentCaptureRect = RectFromRECT(r);
                        SetForegroundWindow(hwnd);
                        logger.Info($"[LightGun] Foreground window updated: {line}");
                        logger.Info($"[LightGun] Capture target rect: {currentCaptureRect}");
                    }
                    return;
                }
            }


            // No match found - fallback to full desktop
            currentHwnd = IntPtr.Zero;
            currentCaptureRect = new Rect(0, 0, Display.main.systemWidth, Display.main.systemHeight);
            logger.Warning("[LightGun] No matching window found from .win; falling back to full desktop");
            logger.Info($"[LightGun] Capture target rect (desktop fallback): {currentCaptureRect}");
        }

        private static Rect RectFromRECT(RECT r)
        {
            return new Rect(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
        }


        private bool TryFindMatchingWindow(string exeName, string titleFragment, out IntPtr hwnd)
        {
            hwnd = IntPtr.Zero;
            foreach (var proc in Process.GetProcessesByName(exeName))
            {
                try
                {
                    if (proc.MainWindowHandle != IntPtr.Zero &&
                    (string.IsNullOrEmpty(titleFragment) || proc.MainWindowTitle.Contains(titleFragment)))
                    {
                        logger.Info($"[LightGun] Matched window: {proc.ProcessName} | Title: {proc.MainWindowTitle}");
                        hwnd = proc.MainWindowHandle;
                        return true;
                    }
                }
                catch { }
            }
            return false;
        }
        private bool TryGetLightgunController()
        {
            var lastActiveField = typeof(LightgunController).GetField("LastActiveGun", BindingFlags.Public | BindingFlags.Static);

            // 1. Update on trigger press
            if (IsTriggerPressed(HandType.Left) && leftController != null)
            {
                if (lastResolvedHand != HandType.Left)
                {
                    logger.Debug("[LightGun] Active hand switched → LEFT");
                    lastResolvedHand = HandType.Left;
                    lastActiveField?.SetValue(null, HandType.Left);
                }
                lightgunControllerRaw = leftController;
                return true;
            }
            if (IsTriggerPressed(HandType.Right) && rightController != null)
            {
                if (lastResolvedHand != HandType.Right)
                {
                    logger.Debug("[LightGun] Active hand switched → RIGHT");
                    lastResolvedHand = HandType.Right;
                    lastActiveField?.SetValue(null, HandType.Right);
                }
                lightgunControllerRaw = rightController;
                return true;
            }

            // 2. Fallback: use LastActiveGun and controller scan
            var activeHand = lastActiveField?.GetValue(null);
            var controllers = Resources.FindObjectsOfTypeAll<MonoBehaviour>()
                .Where(mb => mb.GetType().Name == "LightgunController");
            foreach (var ctrl in controllers)
            {
                var handField = ctrl.GetType().GetField("handType", BindingFlags.NonPublic | BindingFlags.Instance);
                var handValue = handField?.GetValue(ctrl);
                if (activeHand != null && handValue != null && handValue.Equals(activeHand))
                {
                    lightgunControllerRaw = ctrl;
                    return true;
                }
            }

            // 3. Fallback: just take first controller if nothing else
            var fallback = controllers.FirstOrDefault();
            lightgunControllerRaw = fallback;
            return fallback != null;
        }

        private IEnumerator SpawnCloneOnSwap(HandType newHand)
        {
            // Let LightgunController.LastActiveGun update internally
            yield return null;

            // 🔒 Gate: only run while focused and with a valid prefab + arms
            if (!inFocusMode || gunObject == null || originalArms == null || gunClones == null)
                yield break;

            int index = (newHand == HandType.Left) ? 2 : 1;
            if (index < 0 || index >= originalArms.Length || originalArms[index] == null)
                yield break;

            // Don’t re-clone if already present
            if (gunClones[index] != null)
                yield break;

            // (Optional) extra safety: abort if focus ended between yield and now
            if (!inFocusMode) yield break;

            var clone = Instantiate(gunObject);
            if (clone == null) yield break; // double safety in case prefab got cleared
            clone.name = "Gun";
            clone.SetActive(true);

            logger.Debug($"[LightGun] Instantiated clone on swap for index {index}");

            // ====== Grip ======
            Transform controllerGrip = originalArms[index].transform.Find("Grip");
            Transform gunGrip = clone.transform.Find("Grip");

            // ====== Trigger ======
            Transform controllerTrigger = originalArms[index].transform.Find("Trigger");
            Transform gunTrigger = clone.transform.Find("Trigger");

            if (controllerGrip != null && gunGrip != null)
            {
                logger.Debug($"[LightGun] Aligning Grip → Grip for index {index}");

                // Align pos/rot
                clone.transform.position = controllerGrip.position;
                clone.transform.rotation = controllerGrip.rotation;

                // Preserve visual scale relative to controller parent
                Vector3 cabinetWS = gunObject.transform.lossyScale;
                Vector3 parentWS = originalArms[index].transform.lossyScale;
                Vector3 finalLocalScale = new Vector3(
                    parentWS.x == 0f ? 1f : cabinetWS.x / parentWS.x,
                    parentWS.y == 0f ? 1f : cabinetWS.y / parentWS.y,
                    parentWS.z == 0f ? 1f : cabinetWS.z / parentWS.z
                );

                clone.transform.SetParent(originalArms[index].transform, true);
                clone.transform.localScale = finalLocalScale;

                logger.Debug($"[LightGun] Clone aligned at pos={clone.transform.position}, rot={clone.transform.rotation.eulerAngles}, cabinetWS={cabinetWS}, parentWS={parentWS}, final localScale={finalLocalScale}, lossyScale={clone.transform.lossyScale}");

                /*
                // ====== Aim ======
                Transform controllerAim = originalArms[index].transform.Find("Aim");
                Transform gunAim = clone.transform.Find("Aim");
                if (controllerAim != null && gunAim != null)
                {
                    controllerAim.localPosition = gunAim.localPosition;
                    controllerAim.localRotation = gunAim.localRotation;
                    logger.Debug($"[LightGun] Aim aligned → pos={controllerAim.localPosition}, rot={controllerAim.localRotation.eulerAngles}");
                }

                // ====== Sight ======
                Transform controllerSight = originalArms[index].transform.Find("Sight")
                                         ?? originalArms[index].transform.Find("SightGrip")
                                         ?? originalArms[index].transform.Find("sight");
                Transform gunSight = clone.transform.Find("Sight")
                                         ?? clone.transform.Find("SightGrip")
                                         ?? clone.transform.Find("sight");
                if (controllerSight != null && gunSight != null)
                {
                    controllerSight.localPosition = gunSight.localPosition;
                    controllerSight.localRotation = gunSight.localRotation;
                    logger.Debug($"[LightGun] Sight aligned → pos={controllerSight.localPosition}, rot={controllerSight.localRotation.eulerAngles}");
                }

                // ====== Pivot ======
                Transform controllerPivot = originalArms[index].transform.Find("Pivot")
                                         ?? originalArms[index].transform.Find("spinPivot")
                                         ?? originalArms[index].transform.Find("pivot");
                Transform gunPivot = clone.transform.Find("Pivot")
                                         ?? clone.transform.Find("spinPivot")
                                         ?? clone.transform.Find("pivot");
                if (controllerPivot != null && gunPivot != null)
                {
                    controllerPivot.localPosition = gunPivot.localPosition;
                    controllerPivot.localRotation = gunPivot.localRotation;
                    logger.Debug($"[LightGun] Pivot aligned → pos={controllerPivot.localPosition}, rot={controllerPivot.localRotation.eulerAngles}");
                }
                */
            }
            else
            {
                logger.Warning($"[LightGun] Missing Grip for index {index}: controllerGrip={(controllerGrip != null)}, gunGrip={(gunGrip != null)}");
            }

            if (controllerTrigger != null && gunTrigger != null)
            {
                gunTrigger.SetParent(controllerTrigger, true);
                triggerClones[index] = gunTrigger.gameObject;
                logger.Debug($"[LightGun] Linked Trigger → Trigger for index {index}");
            }
            else
            {
                logger.Warning($"[LightGun] Missing Trigger for index {index}: controllerTrigger={(controllerTrigger != null)}, gunTrigger={(gunTrigger != null)}");
            }

            gunClones[index] = clone;

            // Hide original arm mesh if present
            var mr = originalArms[index].GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;

            logger.Debug($"[LightGun] Clone spawned for {newHand} hand after swap.");
        }

        private void ResetGuns()
        {
           // RestoreControllerTransforms();   // ensure Aim/Sight/Pivot go back to defaults

            if (this.isInitialized)
            {
                for (int index = 0; index < 3; ++index)
                {
                    if (this.originalArms[index] != null && this.originalArms[index].name != "LightgunController")
                        this.originalArms[index].GetComponent<MeshRenderer>().enabled = true;
                    if (this.triggers[index] != null)
                        this.triggers[index].GetComponent<MeshRenderer>().enabled = true;
                    if (this.gunClones[index] != null)
                        Destroy(this.gunClones[index]);
                    if (this.triggerClones[index] != null)
                        Destroy(this.triggerClones[index]);
                }
            }
            this.gunObject.SetActive(true);
            this.originalArms[0] = GameObject.Find("Head/LightgunController");
            this.originalArms[1] = GameObject.Find("Hands/HandRight/r_hand_skeletal_lowres/hands:hands_geom/LightgunController");
            this.originalArms[2] = GameObject.Find("Hands/HandLeft/l_hand_skeletal_lowres/hands:hands_geom/LightgunController");
            this.isLeftHand = false;
        }


        private IEnumerator AttachGun()
        {
            yield return new WaitForSeconds(0.20f);
            this.LoadGuns();
            this.CloneGuns();
            this.HideOriginals();
        }

        private void LoadGuns()
        {
            for (int index = 0; index < 3; ++index)
            {
                if (this.originalArms[index] != null && this.originalArms[index].name == "LightgunController")
                {
                    GameObject gameObject = FindChild(this.originalArms[index], "Aim");
                    if (gameObject != null && gameObject.transform.parent != null)
                    {
                        this.originalArms[index] = gameObject.transform.parent.gameObject;
                        this.triggers[index] = this.originalArms[index].transform.Find("Trigger");
                    }
                }
            }
        }
        private void CloneGuns()
        {
            for (int index = 0; index < 3; ++index)
            {
                if (originalArms[index] == null || gunClones[index] != null || originalArms[index].name == "LightgunController")
                    continue;

                // Instantiate the custom gun prefab
                gunClones[index] = Instantiate<GameObject>(gunObject);
                gunClones[index].name = "Gun";
                gunClones[index].SetActive(true);

                logger.Debug($"[LightGun] Instantiated clone for index {index}");

                // ====== Grip ======
                Transform controllerGrip = originalArms[index].transform.Find("Grip");
                Transform gunGrip = gunClones[index].transform.Find("Grip");

                // ====== Trigger ======
                Transform controllerTrigger = originalArms[index].transform.Find("Trigger");
                Transform gunTrigger = gunClones[index].transform.Find("Trigger");

                if (controllerGrip != null && gunGrip != null)
                {
                    logger.Debug($"[LightGun] Aligning Grip → Grip for index {index}");

                    // Align pos/rot
                    gunClones[index].transform.position = controllerGrip.position;
                    gunClones[index].transform.rotation = controllerGrip.rotation;

                    // --- Scale preservation ---
                    Vector3 cabinetWS = gunObject.transform.lossyScale;
                    Vector3 parentWS = originalArms[index].transform.lossyScale;
                    Vector3 finalLocalScale = new Vector3(
                        parentWS.x == 0f ? 1f : cabinetWS.x / parentWS.x,
                        parentWS.y == 0f ? 1f : cabinetWS.y / parentWS.y,
                        parentWS.z == 0f ? 1f : cabinetWS.z / parentWS.z
                    );

                    gunClones[index].transform.SetParent(originalArms[index].transform, true);
                    gunClones[index].transform.localScale = finalLocalScale;

                    logger.Debug($"[LightGun] Clone aligned at pos={gunClones[index].transform.position}, rot={gunClones[index].transform.rotation.eulerAngles}, cabinetWS={cabinetWS}, parentWS={parentWS}, final localScale={finalLocalScale}, lossyScale={gunClones[index].transform.lossyScale}");
                }
                else
                {
                    logger.Warning($"[LightGun] Missing Grip for index {index}: controllerGrip={(controllerGrip != null)}, gunGrip={(gunGrip != null)}");
                }

                if (controllerTrigger != null && gunTrigger != null)
                {
                    gunTrigger.SetParent(controllerTrigger, true);
                    triggerClones[index] = gunTrigger.gameObject;
                    logger.Debug($"[LightGun] Linked Trigger → Trigger for index {index}");
                }
                else
                {
                    logger.Warning($"[LightGun] Missing Trigger for index {index}: controllerTrigger={(controllerTrigger != null)}, gunTrigger={(gunTrigger != null)}");
                }

                /*
                // ====== Aim ======
                Transform controllerAim = originalArms[index].transform.Find("Aim");
                Transform gunAim = gunClones[index].transform.Find("Aim");
                if (controllerAim != null && gunAim != null)
                {
                    controllerAim.localPosition = gunAim.localPosition;
                    controllerAim.localRotation = gunAim.localRotation;
                    logger.Debug($"[LightGun] Aim aligned → pos={controllerAim.localPosition}, rot={controllerAim.localRotation.eulerAngles}");
                }

                // ====== Sight ======
                Transform controllerSight = originalArms[index].transform.Find("Sight")
                                         ?? originalArms[index].transform.Find("SightGrip")
                                         ?? originalArms[index].transform.Find("sight");
                Transform gunSight = gunClones[index].transform.Find("Sight")
                                         ?? gunClones[index].transform.Find("SightGrip")
                                         ?? gunClones[index].transform.Find("sight");
                if (controllerSight != null && gunSight != null)
                {
                    controllerSight.localPosition = gunSight.localPosition;
                    controllerSight.localRotation = gunSight.localRotation;
                    logger.Debug($"[LightGun] Sight aligned → pos={controllerSight.localPosition}, rot={controllerSight.localRotation.eulerAngles}");
                }

                // ====== Pivot ======
                Transform controllerPivot = originalArms[index].transform.Find("Pivot")
                                         ?? originalArms[index].transform.Find("spinPivot")
                                         ?? originalArms[index].transform.Find("pivot");
                Transform gunPivot = gunClones[index].transform.Find("Pivot")
                                         ?? gunClones[index].transform.Find("spinPivot")
                                         ?? gunClones[index].transform.Find("pivot");
                if (controllerPivot != null && gunPivot != null)
                {
                    controllerPivot.localPosition = gunPivot.localPosition;
                    controllerPivot.localRotation = gunPivot.localRotation;
                    logger.Debug($"[LightGun] Pivot aligned → pos={controllerPivot.localPosition}, rot={controllerPivot.localRotation.eulerAngles}");
                }
                */

                // Hide the cabinet gun once a clone is made
                gunObject.SetActive(false);

                logger.Debug($"[LightGun] Clone fully set up for index {index}");
            }
        }


        private void HideOriginals()
        {
            for (int index = 0; index < 3; ++index)
            {
                if (this.originalArms[index] != null && this.originalArms[index].name != "LightgunController")
                    this.originalArms[index].GetComponent<MeshRenderer>().enabled = false;
                if (this.triggers[index] != null)
                {
                    this.triggers[index].GetComponent<MeshRenderer>().enabled = false;
                    if (this.triggerClones[index] != null)
                        this.triggerClones[index].transform.SetParent(this.triggers[index]);
                }
            }
        }

        private static GameObject FindChild(GameObject parent, string childName)
        {
            Transform childTransform = parent.transform.Find(childName);
            if (childTransform != null)
                return childTransform.gameObject;
            Stack<Transform> transformStack = new Stack<Transform>();
            transformStack.Push(parent.transform);
            while (transformStack.Count > 0)
            {
                foreach (Transform subChild in transformStack.Pop())
                {
                    if (subChild.name == childName)
                        return subChild.gameObject;
                    transformStack.Push(subChild);
                }
            }
            return null;
        }

        private async Task TrackActiveWindowFromWinFile()
        {
            string romPath = GameSystem.ControlledSystem?.Game?.path;
            if (string.IsNullOrEmpty(romPath))
                return;

            string winFile = Path.ChangeExtension(romPath, ".win");
            if (!File.Exists(winFile))
                return;

            string[] windowEntries = File.ReadAllLines(winFile);
            IntPtr hwnd = IntPtr.Zero;
            string activeWindowName = null;

            while (inFocusMode)
            {
                foreach (string entry in windowEntries)
                {
                    string trimmed = entry.Trim();
                    if (string.IsNullOrEmpty(trimmed))
                        continue;

                    hwnd = FindWindow(null, trimmed);
                    if (hwnd != IntPtr.Zero)
                    {
                        // Bring to front if changed
                        if (activeWindowName != trimmed)
                        {
                            activeWindowName = trimmed;
                            BringWindowToFront(hwnd);
                            logger.Debug($"[LightGun] Brought window to front: {activeWindowName}");
                        }

                        // Update rect only for top priority window
                        if (trimmed == windowEntries[0].Trim())
                        {
                            RECT r = GetWindowBounds(hwnd);
                            Rect newRect = new Rect(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
                            if (newRect != currentCaptureRect)
                            {
                                currentCaptureRect = newRect;
                                logger.Debug($"[LightGun] Capture rect updated: {newRect}");
                            }
                        }

                        break; // Only use the first match per tick
                    }
                }

                await Task.Delay(1500); // poll every 1.5s
            }
        }
        private void BringWindowToFront(IntPtr hwnd)
        {
            SetForegroundWindow(hwnd);

            // === Force cursor visible and unlocked ===
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            PlayerController.SetLockedCursor(false, temp: true);

            logger?.Debug("[LightGun] Cursor unlocked after bringing window to front.");
        }

        private string GetPrimaryCaptureWindowName(string winFilePath)
        {
            if (!File.Exists(winFilePath))
                return null;

            var lines = File.ReadAllLines(winFilePath);
            foreach (var line in lines)
            {
                string l = line.ToLowerInvariant();
                if (!l.Contains("desktop") && !l.Contains("monitor") && !string.IsNullOrWhiteSpace(line))
                {
                    return line.Trim();
                }
            }
            return null;
        }
        private void HandleModelSwap()
        {
            if (!inFocusMode) return;

            // Reset originals if already initialized
            if (isInitialized)
            {
                ResetGuns();
            }

            // Start coroutine to attach new guns (clone + hide originals)
            StartCoroutine(AttachGun());
            isInitialized = true;

            logger.Debug("[LightGun] Swapped in replacement guns (clone + hide originals).");
        }
        private void StartFocusMode()
        {
            if (inFocusMode) return;

            logger.Debug("Starting Focus Mode. Grab the Gun!");
            inFocusMode = true;
            // Normal path: swap now
            TryGetLightgunController();
            // Hide flashlight (cabinet gun)
            flashlight?.lightObject?.SetActive(false);
           // SaveControllerTransforms();
            try { Cursor.visible = true; } catch { }
            try { PlayerController.SetLockedCursor(false, temp: true); } catch { }

            // Use last trigger pressed to set active hand, skip blind fallback
            var lastActiveField = typeof(LightgunController).GetField("LastActiveGun", BindingFlags.Public | BindingFlags.Static);
            if (lastResolvedHand.HasValue)
            {
                lastActiveField?.SetValue(null, lastResolvedHand.Value);
                logger.Debug($"[LightGun] Focus start using last resolved hand → {lastResolvedHand.Value}");
            }

            TryRefreshRectAndHwnd();
            HandleModelSwap();
            Cursor.visible = true;
            PlayerController.SetLockedCursor(false, temp: true);
            logger.Debug("[LightGun] Cursor unlocked via PlayerController.");

            if (GameSystem.ControlledSystem?.Game?.core == "wgc_libretro")
                ParseWinFile();

            TryRefreshRectAndHwnd();
            string romPath = GameSystem.ControlledSystem?.Game?.path;
            if (!string.IsNullOrEmpty(romPath))
                _ = TrackActiveWindowFromWinFile();
        }

        private void EndFocusMode()
        {
            if (!inFocusMode) return;

            logger.Debug("Ending Focus Mode.");
            inFocusMode = false;
           // RestoreControllerTransforms();
            ResetGuns();
            isInitialized = false;
            // Cursor/UI cleanup
            flashlight?.lightObject?.SetActive(true);
            try { Cursor.visible = true; } catch { }
            try { PlayerController.SetLockedCursor(true); } catch { }
            logger.Debug("[LightGun] Cursor relocked via PlayerController.");
        }
        private void RestoreControllerTransforms()
        {
            if (!savedControllerDefaults || originalArms == null) return;

            int n = originalArms.Length;
            for (int i = 0; i < n; ++i)
            {
                var arm = originalArms[i];
                if (arm == null) continue;

                var aimT = arm.transform.Find("Aim");
                if (aimT != null && savedAim[i].valid)
                {
                    aimT.localPosition = savedAim[i].pos;
                    aimT.localRotation = savedAim[i].rot;
                    logger.Debug($"[LightGun] Aim restored → pos={aimT.localPosition}, rot={aimT.localRotation.eulerAngles}");
                }

                var sightT = arm.transform.Find("Sight") ?? arm.transform.Find("SightGrip") ?? arm.transform.Find("sight");
                if (sightT != null && savedSight[i].valid)
                {
                    sightT.localPosition = savedSight[i].pos;
                    sightT.localRotation = savedSight[i].rot;
                    logger.Debug($"[LightGun] Sight restored → pos={sightT.localPosition}, rot={sightT.localRotation.eulerAngles}");
                }

                var pivotT = arm.transform.Find("Pivot") ?? arm.transform.Find("spinPivot") ?? arm.transform.Find("pivot");
                if (pivotT != null && savedPivot[i].valid)
                {
                    pivotT.localPosition = savedPivot[i].pos;
                    pivotT.localRotation = savedPivot[i].rot;
                    logger.Debug($"[LightGun] Pivot restored → pos={pivotT.localPosition}, rot={pivotT.localRotation.eulerAngles}");
                }
            }

            savedControllerDefaults = false;
        }

        void CheckObject(GameObject obj, string name)     // Check if object is found and log appropriate message
        {
            if (obj == null)
            {
                logger.Error($"{gameObject.name} {name} not found!");
            }
            else
            {
              //  logger.Debug($"{gameObject.name} {name} found.");
            }
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
            // Extracts the file name without the extension and replaces invalid file characters with underscores.
            public static string GetFileName(string filePath)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string FileName = System.Text.RegularExpressions.Regex.Replace(fileName, "[\\/:*?\"<>|]", "_");
                return FileName;
            }
        }
    }
}
/*
 
        private IEnumerator WaitForReturnThenSwap()
        {
            while (moveToCabinetInProgress) yield return null;
            // If user unfocused during the wait, don’t swap anymore.
            if (!inFocusMode) yield break;
            TryGetLightgunController();
            HandleModelSwap();
        }


private IEnumerator MoveGunToHandRoutine(Transform fromGrip, Transform toGrip, GameObject gunInstance)
        {
            if (gunInstance == null || fromGrip == null || toGrip == null)
            {
                logger.Error("[LightGun] MoveGunToHandRoutine: missing arguments.");
                yield break;
            }

            moveToHandInProgress = true;

            // Ensure the traveling instance is visible during the animation
            ForceRendererState(gunInstance, true);

            // initial world-space pose at cabinet
            var t = gunInstance.transform;
            gunInstance.SetActive(true);
            t.SetParent(null, true); // world space

            // match cabinet world scale
            Vector3 cabinetWorldScale = (ugcGun != null) ? ugcGun.lossyScale : Vector3.one;
            t.localScale = cabinetWorldScale;

            // align via instance Grip if present
            Transform instanceGrip = t.Find("Grip");
            if (instanceGrip != null)
            {
                Quaternion deltaRot = fromGrip.rotation * Quaternion.Inverse(instanceGrip.rotation);
                t.rotation = deltaRot * t.rotation;
                Vector3 deltaPos = fromGrip.position - instanceGrip.position;
                t.position += deltaPos;
            }
            else
            {
                t.position = fromGrip.position;
                t.rotation = fromGrip.rotation;
            }

            logger.Debug($"[LightGun] Move start: pos={t.position}, rot={t.rotation.eulerAngles}");

            // compute end pose snapping instance Grip to controller Grip
            Vector3 endPos; Quaternion endRot;
            if (instanceGrip != null)
            {
                Quaternion deltaRot = toGrip.rotation * Quaternion.Inverse(instanceGrip.rotation);
                endRot = deltaRot * t.rotation;
                Vector3 deltaPos = toGrip.position - instanceGrip.position;
                endPos = t.position + deltaPos;
            }
            else
            {
                endPos = toGrip.position;
                endRot = toGrip.rotation;
            }

            // animate
            const float posSpeed = 8.0f;
            const float angSpeed = 540.0f;
            const float posEps = 0.001f;
            const float angEps = 0.25f;

            while (Vector3.Distance(t.position, endPos) > posEps || Quaternion.Angle(t.rotation, endRot) > angEps)
            {
                t.position = Vector3.MoveTowards(t.position, endPos, posSpeed * Time.deltaTime);
                t.rotation = Quaternion.RotateTowards(t.rotation, endRot, angSpeed * Time.deltaTime);
                yield return null;
            }

            // snap, parent under the controller Grip, and preserve world scale
            t.SetPositionAndRotation(endPos, endRot);
            logger.Debug($"[LightGun] Pre-parent final pose: pos={t.position}, rot={t.rotation.eulerAngles}");

            Vector3 worldScale = t.lossyScale;
            t.SetParent(toGrip, true);
            t.localScale = worldScale;

            logger.Debug($"[LightGun] Parent applied. worldScale={t.lossyScale}, localScale={t.localScale}");

            // Ensure cabinet + default controller gun stay hidden once the custom is locked in
            if (ugcGun != null)
            {
                var rendsUGC = ugcGun.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < rendsUGC.Length; i++) rendsUGC[i].enabled = false;
            }
            if (defaultGunInstance != null)
            {
                var rendsDef = defaultGunInstance.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < rendsDef.Length; i++) rendsDef[i].enabled = false;
            }

            // visible instance pointer (dual-instance still uses this for "active" reference)
            replacementGunInstance = gunInstance;

            moveToHandInProgress = false;
            activeMoveCoroutine = null;

            // If focus ended during travel, queue the return now (we never cancel forward moves)
            if (!inFocusMode && !moveToCabinetInProgress)
            {
                activeMoveCoroutine = StartCoroutine(MoveGunBackToCabinetRoutine());
            }
        }

        private IEnumerator MoveGunBackToCabinetRoutine()
        {
            moveToCabinetInProgress = true;

            // NEVER cancel a forward move; wait for it to finish
            while (moveToHandInProgress) yield return null;

            // Choose a source instance:
            // Prefer the visible one; if both hidden (e.g., pre-visibility), fall back sanely.
            GameObject src = null;

            if (leftGunInstance != null)
            {
                var r = leftGunInstance.GetComponentsInChildren<Renderer>(true);
                if (r.Length > 0 && r[0].enabled) src = leftGunInstance;
            }
            if (src == null && rightGunInstance != null)
            {
                var r = rightGunInstance.GetComponentsInChildren<Renderer>(true);
                if (r.Length > 0 && r[0].enabled) src = rightGunInstance;
            }
            if (src == null && replacementGunInstance != null) src = replacementGunInstance;
            if (src == null && leftGunInstance != null) src = leftGunInstance;
            if (src == null && rightGunInstance != null) src = rightGunInstance;

            if (src == null || cabinetGripTransform == null)
            {
                moveToCabinetInProgress = false;
                yield break;
            }

            // Ensure only the returning instance is visible during the flight back
            if (src == leftGunInstance && rightGunInstance != null)
                ForceRendererState(rightGunInstance, false);
            if (src == rightGunInstance && leftGunInstance != null)
                ForceRendererState(leftGunInstance, false);
            ForceRendererState(src, true); // animate the one the player sees

            // Detach and animate back to cabinet
            var t = src.transform;
            t.SetParent(null, true);

            Vector3 startPos = t.position; Quaternion startRot = t.rotation;
            Vector3 endPos = cabinetGripTransform.position; Quaternion endRot = cabinetGripTransform.rotation;

            logger.Debug($"[LightGun] Return start: pos={startPos}, rot={startRot.eulerAngles} → cab pos={endPos}, rot={endRot.eulerAngles}");

            const float posSpeed = 8.0f;
            const float angSpeed = 540.0f;
            const float posEps = 0.001f;
            const float angEps = 0.25f;

            while (Vector3.Distance(t.position, endPos) > posEps || Quaternion.Angle(t.rotation, endRot) > angEps)
            {
                t.position = Vector3.MoveTowards(t.position, endPos, posSpeed * Time.deltaTime);
                t.rotation = Quaternion.RotateTowards(t.rotation, endRot, angSpeed * Time.deltaTime);
                yield return null;
            }

            t.SetPositionAndRotation(endPos, endRot);
            logger.Debug($"[LightGun] Return complete at cabinet: pos={t.position}, rot={t.rotation.eulerAngles}");

            // Restore cabinet UGC + default controller gun
            if (ugcGun != null)
            {
                var rendsUGC = ugcGun.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < rendsUGC.Length; i++) rendsUGC[i].enabled = true;
            }
            if (defaultGunInstance != null)
            {
                var rendDef = defaultGunInstance.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < rendDef.Length; i++) rendDef[i].enabled = true;
                if (!defaultGunInstance.activeSelf) defaultGunInstance.SetActive(true);
            }

            // Destroy both custom instances and clear state
            if (leftGunInstance != null) Destroy(leftGunInstance);
            if (rightGunInstance != null) Destroy(rightGunInstance);
            leftGunInstance = rightGunInstance = null;
            replacementGunInstance = null;

            // Clear pointers so nothing stale lingers
            gripTransform = null;
            cabinetGripTransform = null;

            isModelReplaced = false;
            moveToCabinetInProgress = false;
            activeMoveCoroutine = null;

            logger.Debug("[LightGun] Returned replacement guns, restored cabinet model, and cleared swap state.");
        }
*/