using UnityEngine;
using WIGU;
using System.IO;

namespace WIGUx.Modules.arcadeControllerMotionSim
{
    public class arcadeControllerSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        // === Velocity / multipliers ===
        private readonly float keyboardControllerVelocityX = 150.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityY = 150.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityZ = 150.5f;  // Velocity for keyboard input
        private readonly float vrControllerVelocity = 200.5f;        // Velocity for VR/Controller input

        private float primaryThumbstickRotationMultiplier = 10f;   // Left stick rotation intensity
        private float secondaryThumbstickRotationMultiplier = 10f;  // Right stick rotation intensity
        private const float THUMBSTICK_DEADZONE = 0.13f;            // Deadzone from template

        // === Sticks (rotate these objects directly) ===
        private Transform p1Stick;    // P1 Left  : P1Stick/p1Stick
        private Transform p1Stick2;   // P1 Right : P1Stick/p1Stick2
        private Transform p2Stick;    // P2 Left  : P2Stick/p2Stick
        private Transform p2Stick2;   // P2 Right : P2Stick/p2Stick2

        private Quaternion p1StickStartRot;
        private Quaternion p1Stick2StartRot;
        private Quaternion p2StickStartRot;
        private Quaternion p2Stick2StartRot;

        // === Button movement limits ===
        private float p1positionLimitstart = 0.003f;  // P1 Start
        private float p1positionLimit1 = 0.003f;      // P1 Button1 (X)
        private float p1positionLimit2 = 0.003f;      // P1 Button2 (B)
        private float p1positionLimit3 = 0.003f;      // P1 Button3 (A)
        private float p1positionLimit4 = 0.003f;      // P1 Button4 (Y)
        private float p1positionLimit5 = 0.003f;      // P1 Button5 (LB)
        private float p1positionLimit6 = 0.003f;      // P1 Button6 (RB)

        private float p2positionLimitstart = 0.003f;  // P2 Start
        private float p2positionLimit1 = 0.003f;      // P2 Button1 (X)
        private float p2positionLimit2 = 0.003f;      // P2 Button2 (B)
        private float p2positionLimit3 = 0.003f;      // P2 Button3 (A)
        private float p2positionLimit4 = 0.003f;      // P2 Button4 (Y)
        private float p2positionLimit5 = 0.003f;      // P2 Button5 (LB)
        private float p2positionLimit6 = 0.003f;      // P2 Button6 (RB)

        // === Current button travel trackers ===
        private float p1currentStartButtonPosition = 0f;
        private float p1currentButton1Position = 0f;
        private float p1currentButton2Position = 0f;
        private float p1currentButton3Position = 0f;
        private float p1currentButton4Position = 0f;
        private float p1currentButton5Position = 0f;
        private float p1currentButton6Position = 0f;

        private float p2currentStartButtonPosition = 0f;
        private float p2currentButton1Position = 0f;
        private float p2currentButton2Position = 0f;
        private float p2currentButton3Position = 0f;
        private float p2currentButton4Position = 0f;
        private float p2currentButton5Position = 0f;
        private float p2currentButton6Position = 0f;

        // === Button transforms ===
        private Transform p1StartObject;  // P1/p1Start
        private Transform p1Button1Object; // P1/p1Button1 (X)
        private Transform p1Button2Object; // P1/p1Button2 (B)
        private Transform p1Button3Object; // P1/p1Button3 (A)
        private Transform p1Button4Object; // P1/p1Button4 (Y)
        private Transform p1Button5Object; // P1/p1Button5 (LB)
        private Transform p1Button6Object; // P1/p1Button6 (RB)

        private Transform p2StartObject;   // P2/p2Start
        private Transform p2Button1Object; // P2/p2Button1 (X)
        private Transform p2Button2Object; // P2/p2Button2 (B)
        private Transform p2Button3Object; // P2/p2Button3 (A)
        private Transform p2Button4Object; // P2/p2Button4 (Y)
        private Transform p2Button5Object; // P2/p2Button5 (LB)
        private Transform p2Button6Object; // P2/p2Button6 (RB)

        // === Cached start locals (buttons) ===
        private Vector3 p1StartObjectStartPosition; private Quaternion p1StartObjectStartRotation;
        private Vector3 p1Button1ObjectStartPosition; private Quaternion p1Button1ObjectStartRotation;
        private Vector3 p1Button2ObjectStartPosition; private Quaternion p1Button2ObjectStartRotation;
        private Vector3 p1Button3ObjectStartPosition; private Quaternion p1Button3ObjectStartRotation;
        private Vector3 p1Button4ObjectStartPosition; private Quaternion p1Button4ObjectStartRotation;
        private Vector3 p1Button5ObjectStartPosition; private Quaternion p1Button5ObjectStartRotation;
        private Vector3 p1Button6ObjectStartPosition; private Quaternion p1Button6ObjectStartRotation;

        private Vector3 p2StartObjectStartPosition; private Quaternion p2StartObjectStartRotation;
        private Vector3 p2Button1ObjectStartPosition; private Quaternion p2Button1ObjectStartRotation;
        private Vector3 p2Button2ObjectStartPosition; private Quaternion p2Button2ObjectStartRotation;
        private Vector3 p2Button3ObjectStartPosition; private Quaternion p2Button3ObjectStartRotation;
        private Vector3 p2Button4ObjectStartPosition; private Quaternion p2Button4ObjectStartRotation;
        private Vector3 p2Button5ObjectStartPosition; private Quaternion p2Button5ObjectStartRotation;
        private Vector3 p2Button6ObjectStartPosition; private Quaternion p2Button6ObjectStartRotation;

        // === GameSystem linkage / focus gating ===
        private GameSystem gameSystem;  // Cached GameSystem for this cabinet
        private bool inFocusMode = false;
        private string controlledGameName = string.Empty;
        private string insertedGameName = string.Empty;

        void Start()
        {
            gameSystem = GetComponent<GameSystem>();
            ShowInsertedGameName();
            ShowControlledGameName();
            InitializeObjects();
        }

        void Update()
        {
            if (insertedGameName == controlledGameName && !inFocusMode)
            {
                StartFocusMode();
            }

            if (GameSystem.ControlledSystem == null && inFocusMode)
            {
                EndFocusMode();
            }

            if (inFocusMode)
            {
                MapThumbsticks();
                MapButtons();
            }
        }

        void StartFocusMode()
        {
            logger.Debug("focus starting...");

            // Reset sticks to start rotations
            if (p1Stick) p1Stick.localRotation = p1StickStartRot;
            if (p1Stick2) p1Stick2.localRotation = p1Stick2StartRot;
            if (p2Stick) p2Stick.localRotation = p2StickStartRot;
            if (p2Stick2) p2Stick2.localRotation = p2Stick2StartRot;

            // Reset P1 buttons
            if (p1StartObject) { p1StartObject.localPosition = p1StartObjectStartPosition; p1StartObject.localRotation = p1StartObjectStartRotation; }
            if (p1Button1Object) { p1Button1Object.localPosition = p1Button1ObjectStartPosition; p1Button1Object.localRotation = p1Button1ObjectStartRotation; }
            if (p1Button2Object) { p1Button2Object.localPosition = p1Button2ObjectStartPosition; p1Button2Object.localRotation = p1Button2ObjectStartRotation; }
            if (p1Button3Object) { p1Button3Object.localPosition = p1Button3ObjectStartPosition; p1Button3Object.localRotation = p1Button3ObjectStartRotation; }
            if (p1Button4Object) { p1Button4Object.localPosition = p1Button4ObjectStartPosition; p1Button4Object.localRotation = p1Button4ObjectStartRotation; }
            if (p1Button5Object) { p1Button5Object.localPosition = p1Button5ObjectStartPosition; p1Button5Object.localRotation = p1Button5ObjectStartRotation; }
            if (p1Button6Object) { p1Button6Object.localPosition = p1Button6ObjectStartPosition; p1Button6Object.localRotation = p1Button6ObjectStartRotation; }

            // Reset P2 buttons
            if (p2StartObject) { p2StartObject.localPosition = p2StartObjectStartPosition; p2StartObject.localRotation = p2StartObjectStartRotation; }
            if (p2Button1Object) { p2Button1Object.localPosition = p2Button1ObjectStartPosition; p2Button1Object.localRotation = p2Button1ObjectStartRotation; }
            if (p2Button2Object) { p2Button2Object.localPosition = p2Button2ObjectStartPosition; p2Button2Object.localRotation = p2Button2ObjectStartRotation; }
            if (p2Button3Object) { p2Button3Object.localPosition = p2Button3ObjectStartPosition; p2Button3Object.localRotation = p2Button3ObjectStartRotation; }
            if (p2Button4Object) { p2Button4Object.localPosition = p2Button4ObjectStartPosition; p2Button4Object.localRotation = p2Button4ObjectStartRotation; }
            if (p2Button5Object) { p2Button5Object.localPosition = p2Button5ObjectStartPosition; p2Button5Object.localRotation = p2Button5ObjectStartRotation; }
            if (p2Button6Object) { p2Button6Object.localPosition = p2Button6ObjectStartPosition; p2Button6Object.localRotation = p2Button6ObjectStartRotation; }

            // Zero trackers
            p1currentStartButtonPosition = p1currentButton1Position = p1currentButton2Position = p1currentButton3Position = p1currentButton4Position = p1currentButton5Position = p1currentButton6Position = 0f;
            p2currentStartButtonPosition = p2currentButton1Position = p2currentButton2Position = p2currentButton3Position = p2currentButton4Position = p2currentButton5Position = p2currentButton6Position = 0f;

            inFocusMode = true;
        }

        void EndFocusMode()
        {
            logger.Debug("Exiting Focus Mode...");
            inFocusMode = false;
        }

        private Vector2 ApplyDeadzone(Vector2 v)
        {
            if (Mathf.Abs(v.x) < THUMBSTICK_DEADZONE) v.x = 0f;
            if (Mathf.Abs(v.y) < THUMBSTICK_DEADZONE) v.y = 0f;
            return v;
        }

        private void MapThumbsticks()
        {
            if (!inFocusMode) return;

            Vector2 left = Vector2.zero;
            Vector2 right = Vector2.zero;

            // Oculus
            if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.Oculus)
            {
                left = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
                right = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
            }
            // OpenVR
            else if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.OpenVR)
            {
                var lc = SteamVRInput.GetController(HandType.Left);
                var rc = SteamVRInput.GetController(HandType.Right);
                if (lc != null) left = lc.GetAxis();
                if (rc != null) right = rc.GetAxis();
            }

            // XInput (adds; also acts as fallback when not in VR)
            if (XInput.IsConnected)
            {
                left += XInput.Get(XInput.Axis.LThumbstick);
                right += XInput.Get(XInput.Axis.RThumbstick);

                // Optional Unity axes as extra fallback
                left += new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                right += new Vector2(Input.GetAxis("RightStickHorizontal"), Input.GetAxis("RightStickVertical"));
            }

            left = ApplyDeadzone(left);
            right = ApplyDeadzone(right);

            // Rotate sticks (null-safe)
            if (p1Stick)
            {
                var rot = Quaternion.Euler(left.y * primaryThumbstickRotationMultiplier, 0f, -left.x * primaryThumbstickRotationMultiplier);
                p1Stick.localRotation = p1StickStartRot * rot;
            }
            if (p1Stick2)
            {
                var rot = Quaternion.Euler(right.y * secondaryThumbstickRotationMultiplier, 0f, -right.x * secondaryThumbstickRotationMultiplier);
                p1Stick2.localRotation = p1Stick2StartRot * rot;
            }
            if (p2Stick)
            {
                var rot = Quaternion.Euler(left.y * primaryThumbstickRotationMultiplier, 0f, -left.x * primaryThumbstickRotationMultiplier);
                p2Stick.localRotation = p2StickStartRot * rot;
            }
            if (p2Stick2)
            {
                var rot = Quaternion.Euler(right.y * secondaryThumbstickRotationMultiplier, 0f, -right.x * secondaryThumbstickRotationMultiplier);
                p2Stick2.localRotation = p2Stick2StartRot * rot;
            }
        }

        private void MapButtons()
        {
            if (!inFocusMode) return;

            // === Helper locals ===
            float vKey = (Input.GetKey(KeyCode.X) ? keyboardControllerVelocityX : vrControllerVelocity) * Time.deltaTime;
            Vector3 down1 = new Vector3(0f, -p1positionLimit1, 0f);
            Vector3 down2 = new Vector3(0f, -p1positionLimit2, 0f);
            Vector3 down3 = new Vector3(0f, -p1positionLimit3, 0f);
            Vector3 down4 = new Vector3(0f, -p1positionLimit4, 0f);
            Vector3 down5 = new Vector3(0f, -p1positionLimit5, 0f);
            Vector3 down6 = new Vector3(0f, -p1positionLimit6, 0f);
            Vector3 downS1 = new Vector3(0f, -p1positionLimitstart, 0f);
            Vector3 downS2 = new Vector3(0f, -p2positionLimitstart, 0f);

            // ===================
            // START (Both players)
            // ===================
            bool startDown = XInput.GetDown(XInput.Button.Start) || OVRInput.GetDown(OVRInput.Button.Start) || Input.GetKeyDown(KeyCode.JoystickButton7);
            bool startUp = XInput.GetUp(XInput.Button.Start) || OVRInput.GetUp(OVRInput.Button.Start) || Input.GetKeyUp(KeyCode.JoystickButton7);

            if (startDown)
            {
                if (p1StartObject && p1currentStartButtonPosition < p1positionLimitstart)
                { p1StartObject.localPosition += downS1; p1currentStartButtonPosition += vKey; }
                if (p2StartObject && p2currentStartButtonPosition < p2positionLimitstart)
                { p2StartObject.localPosition += downS2; p2currentStartButtonPosition += vKey; }
            }
            if (startUp)
            {
                if (p1StartObject) { p1StartObject.localPosition = p1StartObjectStartPosition; p1currentStartButtonPosition = 0f; }
                if (p2StartObject) { p2StartObject.localPosition = p2StartObjectStartPosition; p2currentStartButtonPosition = 0f; }
            }

            // ===================
            // P1: X (Button1)
            // ===================
            bool p1_b1_down = XInput.GetDown(XInput.Button.X) || OVRInput.GetDown(OVRInput.Button.Three) || SteamVRInput.GetDown(SteamVRInput.TouchButton.X);
            bool p1_b1_up = XInput.GetUp(XInput.Button.X) || OVRInput.GetUp(OVRInput.Button.Three) || SteamVRInput.GetUp(SteamVRInput.TouchButton.X);
            if (p1_b1_down && p1Button1Object && p1currentButton1Position < p1positionLimit1)
            { p1Button1Object.localPosition += down1; p1currentButton1Position += vKey; }
            if (p1_b1_up && p1Button1Object)
            { p1Button1Object.localPosition = p1Button1ObjectStartPosition; p1currentButton1Position = 0f; }

            // P1: B (Button2)
            bool p1_b2_down = XInput.GetDown(XInput.Button.B) || OVRInput.GetDown(OVRInput.Button.Two) || SteamVRInput.GetDown(SteamVRInput.TouchButton.B);
            bool p1_b2_up = XInput.GetUp(XInput.Button.B) || OVRInput.GetUp(OVRInput.Button.Two) || SteamVRInput.GetUp(SteamVRInput.TouchButton.B);
            if (p1_b2_down && p1Button2Object && p1currentButton2Position < p1positionLimit2)
            { p1Button2Object.localPosition += down2; p1currentButton2Position += vKey; }
            if (p1_b2_up && p1Button2Object)
            { p1Button2Object.localPosition = p1Button2ObjectStartPosition; p1currentButton2Position = 0f; }

            // P1: A (Button3)
            bool p1_b3_down = XInput.GetDown(XInput.Button.A) || OVRInput.GetDown(OVRInput.Button.One) || SteamVRInput.GetDown(SteamVRInput.TouchButton.A);
            bool p1_b3_up = XInput.GetUp(XInput.Button.A) || OVRInput.GetUp(OVRInput.Button.One) || SteamVRInput.GetUp(SteamVRInput.TouchButton.A);
            if (p1_b3_down && p1Button3Object && p1currentButton3Position < p1positionLimit3)
            { p1Button3Object.localPosition += down3; p1currentButton3Position += vKey; }
            if (p1_b3_up && p1Button3Object)
            { p1Button3Object.localPosition = p1Button3ObjectStartPosition; p1currentButton3Position = 0f; }

            // P1: Y (Button4)
            bool p1_b4_down = XInput.GetDown(XInput.Button.Y) || OVRInput.GetDown(OVRInput.Button.Four) || SteamVRInput.GetDown(SteamVRInput.TouchButton.Y);
            bool p1_b4_up = XInput.GetUp(XInput.Button.Y) || OVRInput.GetUp(OVRInput.Button.Four) || SteamVRInput.GetUp(SteamVRInput.TouchButton.Y);
            if (p1_b4_down && p1Button4Object && p1currentButton4Position < p1positionLimit4)
            { p1Button4Object.localPosition += down4; p1currentButton4Position += vKey; }
            if (p1_b4_up && p1Button4Object)
            { p1Button4Object.localPosition = p1Button4ObjectStartPosition; p1currentButton4Position = 0f; }

            // P1: LB (Button5)
            bool p1_b5_down = XInput.GetDown(XInput.Button.LShoulder) || OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger) > 0.5f || SteamVRInput.GetDown(SteamVRInput.TouchButton.LGrip);
            bool p1_b5_up = XInput.GetUp(XInput.Button.LShoulder) || OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger) <= 0.5f || SteamVRInput.GetUp(SteamVRInput.TouchButton.LGrip);
            if (p1_b5_down && p1Button5Object && p1currentButton5Position < p1positionLimit5)
            { p1Button5Object.localPosition += down5; p1currentButton5Position += vKey; } // ✅ fix: increment 5, not 6
            if (p1_b5_up && p1Button5Object)
            { p1Button5Object.localPosition = p1Button5ObjectStartPosition; p1currentButton5Position = 0f; }

            // P1: RB (Button6)
            bool p1_b6_down = XInput.GetDown(XInput.Button.RShoulder) || OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger) > 0.5f || SteamVRInput.GetDown(SteamVRInput.TouchButton.RGrip);
            bool p1_b6_up = XInput.GetUp(XInput.Button.RShoulder) || OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger) <= 0.5f || SteamVRInput.GetUp(SteamVRInput.TouchButton.RGrip);
            if (p1_b6_down && p1Button6Object && p1currentButton6Position < p1positionLimit6)
            { p1Button6Object.localPosition += down6; p1currentButton6Position += vKey; }
            if (p1_b6_up && p1Button6Object)
            { p1Button6Object.localPosition = p1Button6ObjectStartPosition; p1currentButton6Position = 0f; }

            // ===================
            // P2 mirrors P1
            // ===================
            bool p2_b1_down = XInput.GetDown(XInput.Button.X) || OVRInput.GetDown(OVRInput.Button.Three) || SteamVRInput.GetDown(SteamVRInput.TouchButton.X);
            bool p2_b1_up = XInput.GetUp(XInput.Button.X) || OVRInput.GetUp(OVRInput.Button.Three) || SteamVRInput.GetUp(SteamVRInput.TouchButton.X);
            if (p2_b1_down && p2Button1Object && p2currentButton1Position < p2positionLimit1)
            { p2Button1Object.localPosition += new Vector3(0f, -p2positionLimit1, 0f); p2currentButton1Position += vKey; }
            if (p2_b1_up && p2Button1Object)
            { p2Button1Object.localPosition = p2Button1ObjectStartPosition; p2currentButton1Position = 0f; }

            bool p2_b2_down = XInput.GetDown(XInput.Button.B) || OVRInput.GetDown(OVRInput.Button.Two) || SteamVRInput.GetDown(SteamVRInput.TouchButton.B);
            bool p2_b2_up = XInput.GetUp(XInput.Button.B) || OVRInput.GetUp(OVRInput.Button.Two) || SteamVRInput.GetUp(SteamVRInput.TouchButton.B);
            if (p2_b2_down && p2Button2Object && p2currentButton2Position < p2positionLimit2)
            { p2Button2Object.localPosition += new Vector3(0f, -p2positionLimit2, 0f); p2currentButton2Position += vKey; }
            if (p2_b2_up && p2Button2Object)
            { p2Button2Object.localPosition = p2Button2ObjectStartPosition; p2currentButton2Position = 0f; }

            bool p2_b3_down = XInput.GetDown(XInput.Button.A) || OVRInput.GetDown(OVRInput.Button.One) || SteamVRInput.GetDown(SteamVRInput.TouchButton.A);
            bool p2_b3_up = XInput.GetUp(XInput.Button.A) || OVRInput.GetUp(OVRInput.Button.One) || SteamVRInput.GetUp(SteamVRInput.TouchButton.A);
            if (p2_b3_down && p2Button3Object && p2currentButton3Position < p2positionLimit3)
            { p2Button3Object.localPosition += new Vector3(0f, -p2positionLimit3, 0f); p2currentButton3Position += vKey; }
            if (p2_b3_up && p2Button3Object)
            { p2Button3Object.localPosition = p2Button3ObjectStartPosition; p2currentButton3Position = 0f; }

            bool p2_b4_down = XInput.GetDown(XInput.Button.Y) || OVRInput.GetDown(OVRInput.Button.Four) || SteamVRInput.GetDown(SteamVRInput.TouchButton.Y);
            bool p2_b4_up = XInput.GetUp(XInput.Button.Y) || OVRInput.GetUp(OVRInput.Button.Four) || SteamVRInput.GetUp(SteamVRInput.TouchButton.Y);
            if (p2_b4_down && p2Button4Object && p2currentButton4Position < p2positionLimit4)
            { p2Button4Object.localPosition += new Vector3(0f, -p2positionLimit4, 0f); p2currentButton4Position += vKey; }
            if (p2_b4_up && p2Button4Object)
            { p2Button4Object.localPosition = p2Button4ObjectStartPosition; p2currentButton4Position = 0f; }

            bool p2_b5_down = XInput.GetDown(XInput.Button.LShoulder) || OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger) > 0.5f || SteamVRInput.GetDown(SteamVRInput.TouchButton.LGrip);
            bool p2_b5_up = XInput.GetUp(XInput.Button.LShoulder) || OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger) <= 0.5f || SteamVRInput.GetUp(SteamVRInput.TouchButton.LGrip);
            if (p2_b5_down && p2Button5Object && p2currentButton5Position < p2positionLimit5)
            { p2Button5Object.localPosition += new Vector3(0f, -p2positionLimit5, 0f); p2currentButton5Position += vKey; }
            if (p2_b5_up && p2Button5Object)
            { p2Button5Object.localPosition = p2Button5ObjectStartPosition; p2currentButton5Position = 0f; }

            bool p2_b6_down = XInput.GetDown(XInput.Button.RShoulder) || OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger) > 0.5f || SteamVRInput.GetDown(SteamVRInput.TouchButton.RGrip);
            bool p2_b6_up = XInput.GetUp(XInput.Button.RShoulder) || OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger) <= 0.5f || SteamVRInput.GetUp(SteamVRInput.TouchButton.RGrip);
            if (p2_b6_down && p2Button6Object && p2currentButton6Position < p2positionLimit6)
            { p2Button6Object.localPosition += new Vector3(0f, -p2positionLimit6, 0f); p2currentButton6Position += vKey; }
            if (p2_b6_up && p2Button6Object)
            { p2Button6Object.localPosition = p2Button6ObjectStartPosition; p2currentButton6Position = 0f; }
        }

        private void ShowInsertedGameName()
        {
            if (gameSystem != null && gameSystem.Game != null && !string.IsNullOrEmpty(gameSystem.Game.path))
            {
                insertedGameName = FileNameHelper.GetFileName(gameSystem.Game.path);
                logger.Debug("[" + gameObject.name + "] Inserted Game is: " + insertedGameName);
            }
            else
            {
                logger.Debug("[" + gameObject.name + "] Game system or its game path is null.");
                insertedGameName = string.Empty;
            }
        }

        private void ShowControlledGameName()
        {
            if (GameSystem.ControlledSystem != null && GameSystem.ControlledSystem.Game != null && !string.IsNullOrEmpty(GameSystem.ControlledSystem.Game.path))
            {
                controlledGameName = FileNameHelper.GetFileName(GameSystem.ControlledSystem.Game.path);
                logger.Debug("[" + gameObject.name + "] Current Controlled Game is: " + controlledGameName);
            }
            else
            {
                logger.Debug("[" + gameObject.name + "] Controlled Game System or its game path is null.");
                controlledGameName = string.Empty;
            }
        }

        public static class FileNameHelper
        {
            public static string GetFileName(string filePath)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string FileName = System.Text.RegularExpressions.Regex.Replace(fileName, "[\\/:*?\"<>|]", "_");
                return FileName;
            }
        }

        void InitializeObjects()
        {
            // === Sticks ===
            p1Stick = transform.Find("P1Stick/p1Stick");
            p1Stick2 = transform.Find("P1Stick/p1Stick2");
            p2Stick = transform.Find("P2Stick/p2Stick");
            p2Stick2 = transform.Find("P2Stick/p2Stick2");

            if (p1Stick) { p1StickStartRot = p1Stick.localRotation; logger.Debug("p1Stick found."); }
            else { logger.Debug("p1Stick not found."); }
            if (p1Stick2) { p1Stick2StartRot = p1Stick2.localRotation; logger.Debug("p1Stick2 found."); }
            else { logger.Debug("p1Stick2 not found."); }
            if (p2Stick) { p2StickStartRot = p2Stick.localRotation; logger.Debug("p2Stick found."); }
            else { logger.Debug("p2Stick not found."); }
            if (p2Stick2) { p2Stick2StartRot = p2Stick2.localRotation; logger.Debug("p2Stick2 found."); }
            else { logger.Debug("p2Stick2 not found."); }

            // === P1 Buttons ===
            p1StartObject = transform.Find("P1/p1Start");
            p1Button1Object = transform.Find("P1/p1Button1");
            p1Button2Object = transform.Find("P1/p1Button2");
            p1Button3Object = transform.Find("P1/p1Button3");
            p1Button4Object = transform.Find("P1/p1Button4");
            p1Button5Object = transform.Find("P1/p1Button5");
            p1Button6Object = transform.Find("P1/p1Button6");

            if (p1StartObject) { logger.Debug("p1StartObject found."); p1StartObjectStartPosition = p1StartObject.localPosition; p1StartObjectStartRotation = p1StartObject.localRotation; }
            if (p1Button1Object) { logger.Debug("p1Button1Object found."); p1Button1ObjectStartPosition = p1Button1Object.localPosition; p1Button1ObjectStartRotation = p1Button1Object.localRotation; }
            if (p1Button2Object) { logger.Debug("p1Button2Object found."); p1Button2ObjectStartPosition = p1Button2Object.localPosition; p1Button2ObjectStartRotation = p1Button2Object.localRotation; }
            if (p1Button3Object) { logger.Debug("p1Button3Object found."); p1Button3ObjectStartPosition = p1Button3Object.localPosition; p1Button3ObjectStartRotation = p1Button3Object.localRotation; }
            if (p1Button4Object) { logger.Debug("p1Button4Object found."); p1Button4ObjectStartPosition = p1Button4Object.localPosition; p1Button4ObjectStartRotation = p1Button4Object.localRotation; }
            if (p1Button5Object) { logger.Debug("p1Button5Object found."); p1Button5ObjectStartPosition = p1Button5Object.localPosition; p1Button5ObjectStartRotation = p1Button5Object.localRotation; }
            if (p1Button6Object) { logger.Debug("p1Button6Object found."); p1Button6ObjectStartPosition = p1Button6Object.localPosition; p1Button6ObjectStartRotation = p1Button6Object.localRotation; }

            // === P2 Buttons ===
            p2StartObject = transform.Find("P2/p2Start");
            p2Button1Object = transform.Find("P2/p2Button1");
            p2Button2Object = transform.Find("P2/p2Button2");
            p2Button3Object = transform.Find("P2/p2Button3");
            p2Button4Object = transform.Find("P2/p2Button4");
            p2Button5Object = transform.Find("P2/p2Button5");
            p2Button6Object = transform.Find("P2/p2Button6");

            if (p2StartObject) { logger.Debug("p2StartObject found."); p2StartObjectStartPosition = p2StartObject.localPosition; p2StartObjectStartRotation = p2StartObject.localRotation; }
            if (p2Button1Object) { logger.Debug("p2Button1Object found."); p2Button1ObjectStartPosition = p2Button1Object.localPosition; p2Button1ObjectStartRotation = p2Button1Object.localRotation; }
            if (p2Button2Object) { logger.Debug("p2Button2Object found."); p2Button2ObjectStartPosition = p2Button2Object.localPosition; p2Button2ObjectStartRotation = p2Button2Object.localRotation; }
            if (p2Button3Object) { logger.Debug("p2Button3Object found."); p2Button3ObjectStartPosition = p2Button3Object.localPosition; p2Button3ObjectStartRotation = p2Button3Object.localRotation; }
            if (p2Button4Object) { logger.Debug("p2Button4Object found."); p2Button4ObjectStartPosition = p2Button4Object.localPosition; p2Button4ObjectStartRotation = p2Button4Object.localRotation; }
            if (p2Button5Object) { logger.Debug("p2Button5Object found."); p2Button5ObjectStartPosition = p2Button5Object.localPosition; p2Button5ObjectStartRotation = p2Button5Object.localRotation; }
            if (p2Button6Object) { logger.Debug("p2Button6Object found."); p2Button6ObjectStartPosition = p2Button6Object.localPosition; p2Button6ObjectStartRotation = p2Button6Object.localRotation; }
        }
    }
}
