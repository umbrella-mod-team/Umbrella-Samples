using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
// using WIGUx.Modules.MameHookModule;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;
using WIGU;

namespace WIGUx.Modules.rracerfSim
{
    public class rracerfSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        [Header("Object Settings")]
        private Transform WheelObject; // Reference to the Handlebar mirroring object
        private Transform ShifterObject; // Reference to the Shifter mirroring object
        private Transform GasObject; // Reference to the Gas mirroring object
        private Transform BrakeObject; // Reference to the Throttle mirroring object
        private Transform StartObject; // Reference to the Start button object

        [Header("Input Settings")]
        public string primaryThumbstickHorizontal = "Horizontal"; // Input axis for primary thumbstick horizontal
        public string primaryThumbstickVertical = "Vertical"; // Input axis for primary thumbstick vertical
        public string secondaryThumbstickHorizontal = "RightStickHorizontal"; // Input axis for secondary thumbstick horizontal
        public string secondaryThumbstickVertical = "RightStickVertical"; // Input axis for secondary thumbstick forward/backward
        public string leftTrigger = "LIndexTrigger";
        public string rightTrigger = "RIndexTrigger";

        [Header("Velocity Multiplier Settings")]        // Speeds for the animation of the in game flight stick or wheel
        private float primaryThumbstickRotationMultiplier = 10f; // Multiplier for primary thumbstick rotation intensity
        private float secondaryThumbstickRotationMultiplier = 25f; // Multiplier for secondary thumbstick rotation intensity
        private float triggerRotationMultiplier = 20f; // Multiplier for trigger rotation intensity
        private float adjustSpeed = 1.0f;  // Adjust this adjustment speed as needed a lower number will lead to smaller adustments
        private readonly float rotationSmoothness = 5f;  //sets the smoothness of the rotation
        private float WheelRotationDegrees = 100f; // Degrees for wheel rotation, adjust as needed
        private readonly float thumbstickVelocity = 50f;  // Velocity for keyboard input
        private readonly float centeringVelocityX = 25f;  // Velocity for centering rotation
        private readonly float centeringVelocityY = 25f;  // Velocity for centering rotation
        private readonly float centeringVelocityZ = 25f;  // Velocity for centering rotation

        [Header("Rotation Tracking")]        // Sets up the rotation varibles and sets them to 0 
        private float currentRotationX = 0f;  // Current rotation for X-axis
        private float currentRotationY = 0f;  // Current rotation for Y-axis
        private float currentRotationZ = 0f;  // Current rotation for Z-axis

        [Header("Position Settings")]     // Initial positions setup

        private Vector3 WheelStartPosition; // Initial Wheel positions for resetting
        private Vector3 ShifterStartPosition; // Initial Shifter positions for resetting
        private Vector3 GasStartPosition;  // Initial gas positions for resetting
        private Vector3 BrakeStartPosition;  // Initial brake positions for resetting        

        [Header("Rotation Settings")]     // Initial rotations setup
        private Quaternion WheelStartRotation;  // Initial Wheel rotation for resetting
        private Quaternion ShifterStartRotation;  // Initial Shifter rotation for resetting
        private Quaternion GasStartRotation;  // Initial gas rotation for resetting
        private Quaternion BrakeStartRotation;  // Initial brake rotation for resetting


        [Header("Lights and Emissives")]     // Setup Emissive and Lights
        private Transform rrlightObject;
        public Renderer[] rrleftEmissiveObjects;
        public Renderer[] rrrightEmissiveObjects;
        private Coroutine rrleftCoroutine;
        private Coroutine rrrightCoroutine;
        private float sideFlashDuration = 0.3f;
        private float sideFlashDelay = 0.05f;
        private float sideDangerDuration = 0.1f;
        private float sideDangerDelay = 0.2f;
        private float attractFlashDuration = 0.7f;
        private float attractFlashDelay = 0.7f;
        private float dangerFlashDuration = 0.3f;
        private float dangerFlashDelay = 0.3f;
        private Coroutine dangerCoroutine; // Coroutine variable to control the focused danger mode
        private Coroutine attractCoroutine; // Coroutine variable to control the attract mode
        private Transform BrakelightsObject;
        public Light brake_light1;
        public Light brake_light2;
        public float lightDuration = 0.35f; // Duration during which the lights will be on
        private Light[] lights;        //array of lights
        Dictionary<string, int> lastLampStates = new Dictionary<string, int>
             {
               { "lamp0", 0 }, { "lamp1", 0 }, { "lamp2", 0 }, { "lamp3", 0 }
             };
        [Header("Timers and States")]  // Store last states and timers
        private bool isFlashing = false; //set the flashing lights flag
        private bool isHigh = false; //set the high gear flag
        private bool inFocusMode = false;  // Flag to track focus mode state
        private bool isCenteringRotation = false; // Flag to track centering rotation state
        private bool isRiding = false; // Set riding state to true
        private GameSystemState systemState; //systemstate

        [Header("Collider Triggers")]
        [SerializeField] private Collider cockpitCollider;

        [Header("Rom Check")]
        private GameSystem gameSystem;  // Cached GameSystem for this cabinet.
        private string insertedGameName = string.Empty;
        private string controlledGameName = string.Empty;
        private string configPath;
        private Dictionary<GameObject, Transform> originalParents = new Dictionary<GameObject, Transform>();  // Dictionary to store original parents of objects


        void Start()
        {
            CheckInsertedGameName();
            CheckControlledGameName();
            configPath = $"./Emulators/MAME/inputs/{insertedGameName}.ini";
            gameSystem = GetComponent<GameSystem>();
            InitializeEmissiveArrays();
            InitializeLights();
            InitializeObjects();
            if (brake_light1) ToggleLight(brake_light1, false);
            if (brake_light2) ToggleLight(brake_light2, false);
            if (StartObject) ToggleEmissive(StartObject.gameObject, false);
            StartAttractPattern();
        }

        void Update()
        {
            bool inputDetected = false;  // Initialize for centering

            CheckInsertedGameName();
            CheckControlledGameName();
            /*
            if (WIGUx.Modules.MameHookModule.MameHookController.ActiveRomsList != null)
            {
                foreach (var rom in WIGUx.Modules.MameHookModule.MameHookController.ActiveRomsList)
                {
                    if (rom == insertedGameName)
                        ReadData();
                }
            }

            if (systemState != null && systemState.IsOn)
            {
                StopCurrentPatterns();
            } 
            */
            // Enter focus when names match
            if (!string.IsNullOrEmpty(insertedGameName)
                && !string.IsNullOrEmpty(controlledGameName)
                && insertedGameName == controlledGameName
                && !inFocusMode)
            {
                StartFocusMode();
            }
            if (GameSystem.ControlledSystem == null && inFocusMode)
            {
                EndFocusMode();
            }
            if (inFocusMode)
            {
                MapThumbsticks(ref inputDetected);
                MapButtons(ref inputDetected);

            }
        }


        void StartFocusMode()
        {
            logger.Debug($"{gameObject.name} Starting Focus Mode...");
            StopCurrentPatterns();

        }

        void EndFocusMode()
        {
            logger.Debug($"{gameObject.name} Exiting Focus Mode...");
            StartAttractPattern();

            ResetPositions();
            inFocusMode = false;  // Clear focus mode flag
        }

        void ResetPositions()      // Reset objects and cockpit cam to initial position and rotation
        {
            logger.Debug($"{gameObject.name} Resetting Positions");

            if (WheelObject != null)
            {
                WheelObject.localPosition = WheelStartPosition;
                WheelObject.localRotation = WheelStartRotation;
            }
            if (ShifterObject != null)
            {
                ShifterObject.localPosition = ShifterStartPosition;
                ShifterObject.localRotation = ShifterStartRotation;
            }
            if (GasObject != null)
            {
                GasObject.localPosition = GasStartPosition;
                GasObject.localRotation = GasStartRotation;
            }
            if (BrakeObject != null)
            {
                BrakeObject.localPosition = BrakeStartPosition;
                BrakeObject.localRotation = BrakeStartRotation;
            }
            // Reset rotation allowances and current rotation values
            currentRotationX = 0f;
            currentRotationY = 0f;
            currentRotationZ = 0f;
        }
        private const float THUMBSTICK_DEADZONE = 0.13f; // Adjust as needed

        private Vector2 ApplyDeadzone(Vector2 input, float deadzone)
        {
            input.x = Mathf.Abs(input.x) < deadzone ? 0f : input.x;
            input.y = Mathf.Abs(input.y) < deadzone ? 0f : input.y;
            return input;
        }
        private void MapThumbsticks(ref bool inputDetected)
        {
            if (!inFocusMode) return;

            Vector2 primaryThumbstick = Vector2.zero;
            Vector2 secondaryThumbstick = Vector2.zero;
            float LIndexTrigger = 0f, RIndexTrigger = 0f;
            float primaryHandTrigger = 0f, secondaryHandTrigger = 0f;

            // === INPUT SELECTION WITH DEADZONE ===
            // OVR CONTROLLERS (adds to VR input if both are present)
            if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.Oculus)
            {
                primaryThumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
                secondaryThumbstick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

                LIndexTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger);
                RIndexTrigger = OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger);
                primaryHandTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger);
                secondaryHandTrigger = OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger);

                primaryThumbstick = ApplyDeadzone(primaryThumbstick, THUMBSTICK_DEADZONE);
                secondaryThumbstick = ApplyDeadzone(secondaryThumbstick, THUMBSTICK_DEADZONE);
            }

            // STEAMVR CONTROLLERS (adds to VR input if both are present)
            if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.OpenVR)
            {
                var leftController = SteamVRInput.GetController(HandType.Left);
                var rightController = SteamVRInput.GetController(HandType.Right);
                if (leftController != null) primaryThumbstick += leftController.GetAxis();
                if (rightController != null) secondaryThumbstick += rightController.GetAxis();

                LIndexTrigger = Mathf.Max(LIndexTrigger, SteamVRInput.GetTriggerValue(HandType.Left));
                RIndexTrigger = Mathf.Max(RIndexTrigger, SteamVRInput.GetTriggerValue(HandType.Right));

                primaryThumbstick = ApplyDeadzone(primaryThumbstick, THUMBSTICK_DEADZONE);
                secondaryThumbstick = ApplyDeadzone(secondaryThumbstick, THUMBSTICK_DEADZONE);
            }

            // XBOX CONTROLLER (adds to VR input if both are present)
            if (XInput.IsConnected)
            {
                primaryThumbstick += XInput.Get(XInput.Axis.LThumbstick);
                secondaryThumbstick += XInput.Get(XInput.Axis.RThumbstick);

                // Optionally use Unity Input axes as backup:
                primaryThumbstick += new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                secondaryThumbstick += new Vector2(Input.GetAxis("RightStickHorizontal"), Input.GetAxis("RightStickVertical"));

                LIndexTrigger = Mathf.Max(LIndexTrigger, XInput.Get(XInput.Trigger.LIndexTrigger));
                RIndexTrigger = Mathf.Max(RIndexTrigger, XInput.Get(XInput.Trigger.RIndexTrigger));

                primaryThumbstick = ApplyDeadzone(primaryThumbstick, THUMBSTICK_DEADZONE);
                secondaryThumbstick = ApplyDeadzone(secondaryThumbstick, THUMBSTICK_DEADZONE);
            }

            // Map primary thumbstick to wheel
            if (WheelObject)
			{
				Quaternion primaryRotation = Quaternion.Euler(
					0f,
					0f,
					-primaryThumbstick.x * WheelRotationDegrees
				);
				WheelObject.localRotation = WheelStartRotation * primaryRotation;
				if (Mathf.Abs(primaryThumbstick.x) > 0.01f) // Only set if wheel is being turned
					inputDetected = true;
			}

			// Map triggers for gas and brake rotation on X-axis
			if (GasObject)
			{
				Quaternion gasRotation = Quaternion.Euler(
					RIndexTrigger * triggerRotationMultiplier,
					0f,
					0f
				);
				GasObject.localRotation = GasStartRotation * gasRotation;
			}
			if (BrakeObject)
			{
				Quaternion brakeRotation = Quaternion.Euler(
					LIndexTrigger * triggerRotationMultiplier,
					0f,
					0f
				);
				BrakeObject.localRotation = BrakeStartRotation * brakeRotation;
			}
		}

        private void MapButtons(ref bool inputDetected) // Pass by reference
        {
            if (!inFocusMode) return;

            // LeftTrigger (Brake) - toggle lights and emissive
            if (
                (XInput.IsConnected && XInput.GetDown(XInput.Button.LIndexTrigger))
                || OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger)
                || SteamVRInput.GetDown(SteamVRInput.TouchButton.LTrigger)
            )
            {
                if (brake_light1) ToggleLight(brake_light1, true);
                if (brake_light2) ToggleLight(brake_light2, true);
                if (BrakelightsObject) ToggleEmissive(BrakelightsObject.gameObject, true);
            }

            if (
                (XInput.IsConnected && XInput.GetUp(XInput.Button.LIndexTrigger))
                || OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger)
                || SteamVRInput.GetUp(SteamVRInput.TouchButton.LTrigger)
            )
            {
                if (brake_light1) ToggleLight(brake_light1, false);
                if (brake_light2) ToggleLight(brake_light2, false);
                if (BrakelightsObject) ToggleEmissive(BrakelightsObject.gameObject, false);
            }

        bool shifterPressed =
               XInput.GetDown(XInput.Button.Y)
               || OVRInput.GetDown(OVRInput.Button.Two)   // Oculus Y (left controller)
               || SteamVRInput.GetDown(SteamVRInput.TouchButton.Y);

            if (shifterPressed)
            {
                if (!isHigh)
                {
                    ShifterObject.Rotate(-15f, 0, 0);
                    isHigh = true;
                }
                else
                {
                    ShifterObject.Rotate(15f, 0, 0);
                    isHigh = false;
                }
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
        // Initialize the emissive arrays with the appropriate objects
        private void InitializeEmissiveArrays()
        {
            // Initialize left and right arrays from rrlightObject
            rrleftEmissiveObjects = new Renderer[15];
            rrrightEmissiveObjects = new Renderer[15];
            rrlightObject = transform.Find("rrlight");
            if (rrlightObject != null)
            {
                // Left side
                rrleftEmissiveObjects[0] = rrlightObject.Find("lllight1")?.GetComponent<Renderer>();
                rrleftEmissiveObjects[1] = rrlightObject.Find("lllight2")?.GetComponent<Renderer>();
                rrleftEmissiveObjects[2] = rrlightObject.Find("lllight3")?.GetComponent<Renderer>();
                rrleftEmissiveObjects[3] = rrlightObject.Find("lllight4")?.GetComponent<Renderer>();
                rrleftEmissiveObjects[4] = rrlightObject.Find("lllight5")?.GetComponent<Renderer>();
                rrleftEmissiveObjects[5] = rrlightObject.Find("lllight6")?.GetComponent<Renderer>();
                rrleftEmissiveObjects[6] = rrlightObject.Find("lllight7")?.GetComponent<Renderer>();
                rrleftEmissiveObjects[7] = rrlightObject.Find("lllight8")?.GetComponent<Renderer>();
                rrleftEmissiveObjects[8] = rrlightObject.Find("lllight9")?.GetComponent<Renderer>();
                rrleftEmissiveObjects[9] = rrlightObject.Find("lllight10")?.GetComponent<Renderer>();
                rrleftEmissiveObjects[10] = rrlightObject.Find("lllight11")?.GetComponent<Renderer>();
                rrleftEmissiveObjects[11] = rrlightObject.Find("lllight12")?.GetComponent<Renderer>();
                rrleftEmissiveObjects[12] = rrlightObject.Find("lllight13")?.GetComponent<Renderer>();
                rrleftEmissiveObjects[13] = rrlightObject.Find("lllight14")?.GetComponent<Renderer>();
                rrleftEmissiveObjects[14] = rrlightObject.Find("lllight15")?.GetComponent<Renderer>();

                // Right side
                rrrightEmissiveObjects[0] = rrlightObject.Find("rllight1")?.GetComponent<Renderer>();
                rrrightEmissiveObjects[1] = rrlightObject.Find("rllight2")?.GetComponent<Renderer>();
                rrrightEmissiveObjects[2] = rrlightObject.Find("rllight3")?.GetComponent<Renderer>();
                rrrightEmissiveObjects[3] = rrlightObject.Find("rllight4")?.GetComponent<Renderer>();
                rrrightEmissiveObjects[4] = rrlightObject.Find("rllight5")?.GetComponent<Renderer>();
                rrrightEmissiveObjects[5] = rrlightObject.Find("rllight6")?.GetComponent<Renderer>();
                rrrightEmissiveObjects[6] = rrlightObject.Find("rllight7")?.GetComponent<Renderer>();
                rrrightEmissiveObjects[7] = rrlightObject.Find("rllight8")?.GetComponent<Renderer>();
                rrrightEmissiveObjects[8] = rrlightObject.Find("rllight9")?.GetComponent<Renderer>();
                rrrightEmissiveObjects[9] = rrlightObject.Find("rllight10")?.GetComponent<Renderer>();
                rrrightEmissiveObjects[10] = rrlightObject.Find("rllight11")?.GetComponent<Renderer>();
                rrrightEmissiveObjects[11] = rrlightObject.Find("rllight12")?.GetComponent<Renderer>();
                rrrightEmissiveObjects[12] = rrlightObject.Find("rllight13")?.GetComponent<Renderer>();
                rrrightEmissiveObjects[13] = rrlightObject.Find("rllight14")?.GetComponent<Renderer>();
                rrrightEmissiveObjects[14] = rrlightObject.Find("rllight15")?.GetComponent<Renderer>();
            }
            LogMissingObject(rrleftEmissiveObjects, "rrleftEmissiveObjects");
            LogMissingObject(rrrightEmissiveObjects, "rrrightEmissiveObjects");
        }


        // Method to disable emission
        void DisableEmission(Renderer[] emissiveObjects)
        {
            foreach (var renderer in emissiveObjects)
            {
                if (renderer != null)
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
                else
                {
                    Debug.Log("Renderer component not found on one of the emissive objects.");
                }
            }
        }

        // Method to log missing objects
        void LogMissingObject(Renderer[] emissiveObjects, string arrayName)
        {
            for (int i = 0; i < emissiveObjects.Length; i++)
            {
                if (emissiveObjects[i] == null)
                {
                    //    logger.Error($"{arrayName} object at index {i} not found.");
                }
            }
        }
        // Attract pattern for the side
        IEnumerator SideAttractPattern(Renderer[] emissiveObjects)
        {
            int totalLights = emissiveObjects.Length;
            int frameCount = 4; // Since the pattern repeats every 4 frames

            while (true)
            {
                for (int frame = 0; frame < frameCount; frame++)
                {
                    // Turn all lights on
                    for (int i = 0; i < totalLights; i++)
                    {
                        ToggleEmissiveRenderer(emissiveObjects[i], true);
                    }

                    // Turn off specific lights based on the current frame
                    for (int i = frame; i < totalLights; i += frameCount)
                    {
                        ToggleEmissiveRenderer(emissiveObjects[i], false);
                    }

                    // Wait before moving to the next frame
                    yield return new WaitForSeconds(sideFlashDuration);
                }

                // Add a small delay between cycles
                yield return new WaitForSeconds(sideFlashDelay);
            }
        }
        // Danger pattern for the sides
        IEnumerator SideDangerPattern(Renderer[] emissiveObjects)
        {
            while (true)
            {
                // Flash even-numbered lights in each group
                for (int group = 1; group < 3; group += 2) // This iterates over the second light in each group (index 1, 4, 7, 10, 13)
                {
                    for (int i = group; i < emissiveObjects.Length; i += 3)
                    {
                        ToggleEmissiveRenderer(emissiveObjects[i], true);
                    }
                }
                yield return new WaitForSeconds(sideDangerDuration);

                // Turn off even-numbered lights
                for (int group = 1; group < 3; group += 2)
                {
                    for (int i = group; i < emissiveObjects.Length; i += 3)
                    {
                        ToggleEmissiveRenderer(emissiveObjects[i], false);
                    }
                }

                // Flash odd-numbered lights in each group
                for (int group = 0; group < 3; group += 2) // This iterates over the first and third lights in each group (index 0, 3, 6, 9, 12)
                {
                    for (int i = group; i < emissiveObjects.Length; i += 3)
                    {
                        ToggleEmissiveRenderer(emissiveObjects[i], true);
                    }
                }
                yield return new WaitForSeconds(sideDangerDuration);

                // Turn off odd-numbered lights
                for (int group = 0; group < 3; group += 2)
                {
                    for (int i = group; i < emissiveObjects.Length; i += 3)
                    {
                        ToggleEmissiveRenderer(emissiveObjects[i], false);
                    }
                }

                yield return new WaitForSeconds(sideDangerDelay);
            }
        }


        void ToggleEmissiveRenderer(Renderer renderer, bool isOn)
        {
            if (isOn)
            {
                renderer.material.EnableKeyword("_EMISSION");
            }
            else
            {
                renderer.material.DisableKeyword("_EMISSION");
            }
        }

        void ToggleEmissive(GameObject targetObject, bool isActive)
        {
            if (targetObject != null)
            {
                Renderer renderer = targetObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material material = renderer.material;

                    if (isActive)
                    {
                        material.EnableKeyword("_EMISSION");
                    }
                    else
                    {
                        material.DisableKeyword("_EMISSION");
                    }

                    // logger.Debug($"{targetObject.name} emissive state set to {(isActive ? "ON" : "OFF")}.");
                }
                else
                {
                    logger.Debug($"{gameObject.name} Renderer component not found on {targetObject.name}.");
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} {targetObject.name} emissive object is not assigned.");
            }
        }

        void ToggleLight(Light targetLight, bool isActive)
        {
            if (targetLight == null) return;

            // Ensure the GameObject itself is active
            if (targetLight.gameObject.activeSelf != isActive)
                targetLight.gameObject.SetActive(isActive);

            // Then toggle the component
            targetLight.enabled = isActive;
        }

        // Method to toggle all in the array
        void ToggleAll(Renderer[] emissiveObjects, bool isOn)
        {
            foreach (var renderer in emissiveObjects)
            {
                ToggleEmissiveRenderer(renderer, isOn);
            }
        }

        public void TurnAllOff()
        {
            //   ToggleAll(frontEmissiveObjects, false);
            ToggleAll(rrleftEmissiveObjects, false);
            ToggleAll(rrrightEmissiveObjects, false);
        }

        public void StartAttractPattern()
        {
            StopCurrentPatterns();
            attractCoroutine = StartCoroutine(attractPattern());
            rrleftCoroutine = StartCoroutine(SideAttractPattern(rrleftEmissiveObjects));
            rrrightCoroutine = StartCoroutine(SideAttractPattern(rrrightEmissiveObjects));
        }

        public void StartDangerPattern()
        {
            StopCurrentPatterns();
            dangerCoroutine = StartCoroutine(dangerPattern());
            rrleftCoroutine = StartCoroutine(SideDangerPattern(rrleftEmissiveObjects));
            rrrightCoroutine = StartCoroutine(SideDangerPattern(rrrightEmissiveObjects));
        }

        IEnumerator attractPattern()  //Pattern For Attract Mode
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 1f));
            while (true)
            {
                if (BrakelightsObject) ToggleEmissive(BrakelightsObject.gameObject, false);
                if (brake_light1) ToggleLight(brake_light1, false);
                if (brake_light2) ToggleLight(brake_light2, false);
                yield return new WaitForSeconds(attractFlashDuration);
                if (BrakelightsObject) ToggleEmissive(BrakelightsObject.gameObject, true);
                if (brake_light1) ToggleLight(brake_light1, true);
                if (brake_light2) ToggleLight(brake_light2, true);
                yield return new WaitForSeconds(attractFlashDelay);
            }
        }

        IEnumerator dangerPattern() //Pattern For Focused Danger Mode
        {
            while (true)
            {
                if (BrakelightsObject) ToggleEmissive(BrakelightsObject.gameObject, false);
                if (brake_light1) ToggleLight(brake_light1, false);
                if (brake_light2) ToggleLight(brake_light2, false);
                yield return new WaitForSeconds(dangerFlashDuration);
                if (BrakelightsObject) ToggleEmissive(BrakelightsObject.gameObject, true);
                if (brake_light1) ToggleLight(brake_light1, true);
                if (brake_light2) ToggleLight(brake_light2, true);
                yield return new WaitForSeconds(dangerFlashDelay);
            }
        }

        private void StopCurrentPatterns()
        {
            if (attractCoroutine != null)
            {
                StopCoroutine(attractCoroutine);
                attractCoroutine = null;
            }
            if (dangerCoroutine != null)
            {
                StopCoroutine(dangerCoroutine);
                dangerCoroutine = null;
            }
            if (rrleftCoroutine != null)
            {
                StopCoroutine(rrleftCoroutine);
                rrleftCoroutine = null;
            }

            if (rrrightCoroutine != null)
            {
                StopCoroutine(rrrightCoroutine);
                rrrightCoroutine = null;
            }
        }

        // Check if object is found and log appropriate message
        void CheckObject(GameObject obj, string name)
        {
            if (obj == null)
            {
                logger.Error($"{name} not found!");
            }
            else
            {
                logger.Info($"{name} found.");
            }
        }

        void ChangeColorEmissive(GameObject targetObject, Color emissionColor, float intensity, bool isActive)
        {
            if (targetObject != null)
            {
                Renderer renderer = targetObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material material = renderer.material;

                    if (isActive)
                    {
                        material.EnableKeyword("_EMISSION");
                        material.SetColor("_EmissionColor", emissionColor * intensity);
                    }
                    else
                    {
                        material.DisableKeyword("_EMISSION");
                    }

                    //    logger.Debug($"{gameObject.name} {targetObject.name} emissive state set to {(isActive ? "ON" : "OFF")} with color {emissionColor} and intensity {intensity}.");
                }
                else
                {
                    //    logger.Debug($"{gameObject.name} Renderer component not found on {targetObject.name}.");
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} Target emissive object is not assigned.");
            }
        }
        void InitializeLights()
        {
            // Gets all Light components in the target object and its children
            Light[] Lights = transform.GetComponentsInChildren<Light>();

            // Log the names of the objects containing the Light components and filter out unwanted lights
            foreach (Light light in Lights)
            {
                if (light.gameObject.name == "brakelight1")
                {
                    brake_light1 = light;
                    logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                }
                else if (light.gameObject.name == "brakelight2")
                {
                    brake_light2 = light;
                    logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                }
                else
                {
                    logger.Debug($"{gameObject.name} Excluded Light found in object: " + light.gameObject.name);
                }
            }
        }
        void InitializeObjects()
        {

            // Find Wheel
            WheelObject = transform.Find("Wheel");
            if (WheelObject != null)
            {
                logger.Debug($"{gameObject.name} Wheel object found.");
                WheelStartPosition = WheelObject.localPosition;
                WheelStartRotation = WheelObject.localRotation;
            }
            else
            {
                logger.Debug($"{gameObject.name} Wheel object not found.");
            }

            // Find Shifter
            ShifterObject = transform.Find("Shifter");
            if (ShifterObject != null)
            {
                logger.Debug($"{gameObject.name} Shifter object found.");
                // Store initial position and rotation of the Shifter
                ShifterStartPosition = ShifterObject.localPosition;
                ShifterStartRotation = ShifterObject.localRotation;
            }
            else
            {
                logger.Debug($"{gameObject.name} Shifter object not found.");
            }

            // Find Gas
            GasObject = transform.Find("Gas");
            if (GasObject != null)
            {
                logger.Debug($"{gameObject.name} Gas object found.");
                GasStartPosition = GasObject.localPosition;
                GasStartRotation = GasObject.localRotation;
            }
            else
            {
                logger.Debug($"{gameObject.name} Gas object not found.");
            }


            // Find Brake
            BrakeObject = transform.Find("Brake");
            if (BrakeObject != null)
            {
                logger.Debug($"{gameObject.name} Brake object found.");
                BrakeStartPosition = BrakeObject.localPosition;
                BrakeStartRotation = BrakeObject.localRotation;
            }
            else
            {
                logger.Debug($"{gameObject.name} Brake object not found.");
            }
            // Find Brakelights
            BrakelightsObject = transform.Find("Brakelights");
            if (BrakelightsObject != null)
            {
                logger.Debug($"{gameObject.name} Brakelights object found.");
                BrakeStartPosition = BrakeObject.localPosition;
                BrakeStartRotation = BrakeObject.localRotation;
            }
            else
            {
                logger.Debug($"{gameObject.name} Brakelights object not found.");
            }
        }
    }
}
