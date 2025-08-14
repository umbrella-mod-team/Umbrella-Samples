using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using WIGU;
using WIGUx.Modules.MameHookModule;

namespace WIGUx.Modules.chasehq2Sim
{
    public class chasehq2SimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        [Header("Object Settings")]
        private Transform StartObject; // Reference to the start button object
        private Transform GasObject; // Reference to the throttle mirroring object
        private Transform BrakeObject; // Reference to the throttle mirroring object
        private Transform WheelObject; // Reference to the throttle mirroring object
        private Transform ShifterObject; // Reference to the throttle mirroring object

        [Header("Input Settings")]
        public string primaryThumbstickHorizontal = "Horizontal"; // Input axis for primary thumbstick horizontal
        public string primaryThumbstickVertical = "Vertical"; // Input axis for primary thumbstick vertical
        public string secondaryThumbstickHorizontal = "RightStickHorizontal"; // Input axis for secondary thumbstick horizontal
        public string secondaryThumbstickVertical = "RightStickVertical"; // Input axis for secondary thumbstick forward/backward
        public string leftTrigger = "LIndexTrigger";
        public string rightTrigger = "RIndexTrigger";

        [Header("Velocity Multiplier Settings")]        // Speeds for the animation of the in game flight stick or wheel
        private float primaryThumbstickRotationMultiplier = 120f; // Multiplier for primary thumbstick rotation intensity
        private float secondaryThumbstickRotationMultiplier = 25f; // Multiplier for secondary thumbstick rotation intensity
        private float triggerRotationMultiplier = 20f; // Multiplier for trigger rotation intensity
        private float adjustSpeed = 1.0f;  // Adjust this adjustment speed as needed a lower number will lead to smaller adustments
        private float WheelRotationDegrees = 100f; // Degrees for wheel rotation, adjust as needed
        private readonly float rotationSmoothness = 5f;  //sets the smoothness of the rotation
        private readonly float thumbstickVelocity = 50f;  // Velocity for keyboard input

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
        Dictionary<string, int> lastLampStates = new Dictionary<string, int>
    {
        { "led0", 0 }, { "led1", 0 }
    };
        private Light[] lights;        //array of lights
        private Light chasehq21_light;
        private Light chasehq22_light;
        private Light chasehq23_light;
        private Light chasehq24_light;
        private float flashDuration = 0.15f;
        private float flashInterval = 0.05f;
        private float lightDuration = 0.25f; // Duration during which the lights will be on
        private Transform lightsObject;
        private Coroutine chasehq2Coroutine; // Coroutine variable to control the strobe flashing
        private bool isFlashing = false; //set the flashing lights flag
        private GameSystemState systemState;

        [Header("Rom Check")]
        private GameSystem gameSystem;  // Cached GameSystem for this cabinet.
        private string insertedGameName = string.Empty;
        private string controlledGameName = string.Empty;
        private string configPath;
        private bool isHigh = false; //set the high gear flag
        private bool inFocusMode = false;  // Flag to track focus mode state
        private Dictionary<GameObject, Transform> originalParents = new Dictionary<GameObject, Transform>();  // Dictionary to store original parents of objects
                                                                                                              // Public property to access the Game instance
        void Start()
        {
            CheckInsertedGameName();
            CheckControlledGameName();
            configPath = $"./Emulators/MAME/inputs/{insertedGameName}.ini";
            gameSystem = GetComponent<GameSystem>();
            InitializeLights();
            InitializeObjects();
            if (chasehq21_light) ToggleLight(chasehq21_light, false);
            if (chasehq22_light) ToggleLight(chasehq22_light, false);
            if (chasehq23_light) ToggleLight(chasehq23_light, false);
            if (chasehq24_light) ToggleLight(chasehq24_light, false);
            //    StartAttractPattern();


        }

        void Update()
        {
            bool inputDetected = false;  // Initialize for centering
            CheckInsertedGameName();
            CheckControlledGameName();
            if (WIGUx.Modules.MameHookModule.MameHookController.ActiveRomsList != null)
            {
                foreach (var rom in WIGUx.Modules.MameHookModule.MameHookController.ActiveRomsList)
                {
                    if (rom == insertedGameName)
                        ReadData();
                }
            }
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
        void WriteLampConfig(string filePath)
        {
            string content = "led0 = 0\n" +
                             "led1 = 0\n";

            try
            {
                File.WriteAllText(filePath, content);
                logger.Debug("File written to: " + filePath);
            }
            catch (IOException e)
            {
                logger.Debug("File write failed: " + e.Message);
            }
        }




        void StartFocusMode()
        {
            logger.Debug($"{gameObject.name} Starting Focus Mode...");
            // StopCurrentPatterns();
            logger.Debug("Chase HQ 2 Module starting...");
            logger.Debug("Be on the Lookout!...");
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
            inFocusMode = true;  // Set focus mode flag
        }

        void EndFocusMode()
        {
            // Reset Controllers to initial positions and rotations

            StopCoroutine(chasehq2Coroutine);
            if (chasehq21_light) ToggleLight(chasehq21_light, false);
            if (chasehq22_light) ToggleLight(chasehq22_light, false);
            if (chasehq23_light) ToggleLight(chasehq23_light, false);
            if (chasehq24_light) ToggleLight(chasehq24_light, false);
            inFocusMode = false;  // Clear focus mode flag
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
                // Rotation applied on top of starting rotation
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


        private void MapButtons(ref bool inputDetected)
        {
            if (!inFocusMode) return;
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

            if (
     XInput.GetDown(XInput.Button.LThumbstick)
     || OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick)
     || SteamVRInput.GetDown(SteamVRInput.TouchButton.LThumbstick)
 )
            {
                if (!isFlashing)
                {
                    // Start the flashing if not already flashing
                    chasehq2Coroutine = StartCoroutine(FlashLights());
                    isFlashing = true;
                }
                else
                {
                    // Stop the flashing if it's currently active
                    StopCoroutine(chasehq2Coroutine);
                    if (chasehq21_light) ToggleLight(chasehq21_light, false);
                    if (chasehq22_light) ToggleLight(chasehq22_light, false);
                    if (chasehq23_light) ToggleLight(chasehq23_light, false);
                    if (chasehq24_light) ToggleLight(chasehq24_light, false);
                    chasehq2Coroutine = null;
                    isFlashing = false;
                }
                inputDetected = true;
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
        void ReadData()
        {
            //readdata
        }

        // 🔹 Helper function for safe lamp processing
        void ProcessLampState(string lampKey, Dictionary<string, int> currentStates)
        {
            if (!lastLampStates.ContainsKey(lampKey))
            {
                lastLampStates[lampKey] = 0;
                logger.Error($"{gameObject.name} Added missing key '{lampKey}' to lastLampStates.");
            }

            if (currentStates.TryGetValue(lampKey, out int newValue))
            {
                if (lastLampStates[lampKey] != newValue)
                {
                    lastLampStates[lampKey] = newValue;

                    // Call the corresponding function dynamically
                    switch (lampKey)
                    {
                        case "lamp0":
                            // ProcessLamp0(newValue);
                            break;
                        case "lamp2":
                            //   ProcessLamp2(newValue);
                            break;
                        case "lamp3":
                            //  ProcessLamp3(newValue);
                            break;
                        default:
                            logger.Warning($"No processing function for '{lampKey}'");
                            break;
                    }
                }
            }
            else
            {
                logger.Error($"{gameObject.name} Lamp key '{lampKey}' not found in current states.");
            }
        }

        // Check if object is found and log appropriate message
        void CheckObject(GameObject obj, string name)
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



        IEnumerator FlashLights()
        {
            int currentIndex = 0; // Start with the first light in the array

            while (true)
            {
                // Check if the light is not null
                if (lights != null)
                {
                    // Log the chosen light
                    // logger.Debug($"Flashing {light.name}");

                    // Turn on the chosen light
                    //     if (lights) ToggleLight(lights, true);

                    // Wait for the flash duration
                    yield return new WaitForSeconds(flashDuration);

                    // Turn off the chosen light
                    //   if (lights) ToggleLight(lights, false);

                    // Wait for the next flash interval
                    yield return new WaitForSeconds(flashInterval - flashDuration);
                }
                else
                {
                    logger.Debug($"{lights} is null.");
                }

                // Move to the next light in the array
                currentIndex = (currentIndex + 1) % lights.Length;
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
            // Find Brake under Z
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
            lightsObject = transform.Find("lights");
            if (lightsObject != null)
            {
                logger.Debug("lightsObject found.");
            }
            else
            {
                logger.Error("lightsObject object not found!");
                return; // Early exit if lightsObject is not found
            }
        }
        void InitializeLights()
        {
            // Gets all Light components in the target object and its children
            Light[] lights = transform.GetComponentsInChildren<Light>();

            // Log the names of the objects containing the Light components and filter out unwanted lights
            foreach (Light light in lights)
            {
                if (light.gameObject.name == "chasehq21_light")
                {
                    chasehq21_light = light;
                    logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                }
                else if (light.gameObject.name == "chasehq22_light")

                {
                    chasehq22_light = light;
                    logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                }
                else if (light.gameObject.name == "chasehq23_light")

                {
                    chasehq23_light = light;
                    logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                }
                else if (light.gameObject.name == "chasehq24_light")

                {
                    chasehq24_light = light;
                    logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                }
                else
                {
                    logger.Debug($"{gameObject.name} Excluded Light found in object: " + light.gameObject.name);
                }
            }
        }

    }
}
