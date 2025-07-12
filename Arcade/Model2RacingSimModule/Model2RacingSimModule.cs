using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Text.RegularExpressions;
using WIGU;

namespace WIGUx.Modules.Model2RacingSim
{
    public class Model2SimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        [Header("Object Settings")]
        private Transform WheelObject; // Reference to the throttle mirroring object
        private Transform ShifterObject; // Reference to the throttle mirroring object
        private Transform GasObject; // Reference to the gas mirroring object
        private Transform BrakeObject; // Reference to the brake mirroring object

        [Header("Input Settings")]
        public string primaryThumbstickHorizontal = "Horizontal"; // Input axis for primary thumbstick horizontal
        public string primaryThumbstickVertical = "Vertical"; // Input axis for primary thumbstick vertical
        public string secondaryThumbstickHorizontal = "RightStickHorizontal"; // Input axis for secondary thumbstick horizontal
        public string secondaryThumbstickVertical = "RightStickVertical"; // Input axis for secondary thumbstick forward/backward
        public string leftTrigger = "LIndexTrigger";
        public string rightTrigger = "RIndexTrigger";

        [Header("Velocity Multiplier Settings")]        // Speeds for the animation of the in game flight stick or wheel
        private float primaryThumbstickRotationMultiplier = 100f; // Multiplier for primary thumbstick rotation intensity
        private float secondaryThumbstickRotationMultiplier = 25f; // Multiplier for secondary thumbstick rotation intensity
        private float triggerRotationMultiplier = 25f; // Multiplier for trigger rotation intensity
        private float adjustSpeed = 1.0f;  // Adjust this adjustment speed as needed a lower number will lead to smaller adustments
        private float WheelRotationDegrees = 100f; // Degrees for wheel rotation, adjust as needed
        private readonly float rotationSmoothness = 5f;  //sets the smoothness of the rotation
        private readonly float thumbstickVelocity = 30f;  // Velocity for keyboard input

        [Header("Position Settings")]     // Initial positions setup
        private Vector3 WheelStartPosition;  // Initial wheel positions for resetting
        private Vector3 ShifterStartPosition;  // Initial shifter positions for resetting
        private Vector3 GasStartPosition;  // Initial gas positions for resetting
        private Vector3 BrakeStartPosition;  // Initial brake positions for resetting

        [Header("Rotation Settings")]     // Initial rotations setup
        private Quaternion WheelStartRotation;  // Initial wheel rotations for resetting
        private Quaternion ShifterStartRotation;  // Initial shifter rotations for resetting
        private Quaternion GasStartRotation;  // Initial Gas rotations for resetting
        private Quaternion BrakeStartRotation;  // Initial Brake rotations for resetting

        [Header("Lights and Emissives")]     // Setup Emissive and Lights
        private Light RaceLeaderLight;
        private Light ShiftLight;
        private bool lastShiftLightState = false;
        bool lastStartButtonState = false;
        private Transform VRRedEmissiveObject;
        private Transform VRBlueEmissiveObject;
        private Transform VRYellowEmissiveObject;
        private Transform VRGreenEmissiveObject;
        private Transform StartButtonEmissiveObject;
        private Transform[] gearEmissives;
        private Light[] lights;
        private float targetEmissiveIntensity = 0f;
        private float emissiveLerpSpeed = 5f;

        [Header("Timers and States")]  // Store last states and timers
        bool flashState = (Time.frameCount / 30) % 2 == 0; // Flashes every 30 frames
        private bool inFocusMode = false;  // Flag to track focus mode state
        private GameSystemState systemState; //systemstate

        [Header("Rom Check")]
        private GameSystem gameSystem;  // Cached GameSystem for this cabinet.
        private string insertedGameName = string.Empty;
        private string controlledGameName = string.Empty;
        private string filePath;
        private string configPath;
        // Track last known emissive states to prevent unnecessary updates
        Dictionary<Transform, bool> lastEmissiveStates = new Dictionary<Transform, bool>();

        void Start()
        {
            CheckInsertedGameName();
            CheckControlledGameName();
            gameSystem = GetComponent<GameSystem>();
            InitializeObjects();
            InitializeLights();
            string filePath = $"./Emulators/MAME/outputs/{insertedGameName}.txt";
            string configPath = $"./Emulators/MAME/inputs/{insertedGameName}.ini";
            DisableAllEmissives();
        }

        void Update()
        {
            ReadData();
            bool inputDetected = false;  // Initialize for centering
            bool throttleDetected = false;// Initialize for centering
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
                MapThumbsticks(ref inputDetected, ref throttleDetected);
            }
        }
        void WriteLampConfig(string filePath)
        {
            string content = "0,0,0,0,0,0,0,0,0\n";

            try
            {
                File.WriteAllText(filePath, content);
                logger.Debug($"{gameObject.name} File written to: " + filePath);
            }
            catch (IOException e)
            {
                logger.Debug($"{gameObject.name} File write failed: " + e.Message);
            }
        }

        void StartFocusMode()
        {
            logger.Debug($"{gameObject.name} Starting Focus Mode...");
            inFocusMode = true;  // Set focus mode flag
        }

        void EndFocusMode()
        {
            logger.Debug($"{gameObject.name} Exiting Focus Mode...");
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
        }
        private const float THUMBSTICK_DEADZONE = 0.13f; // Adjust as needed

        private Vector2 ApplyDeadzone(Vector2 input, float deadzone)
        {
            input.x = Mathf.Abs(input.x) < deadzone ? 0f : input.x;
            input.y = Mathf.Abs(input.y) < deadzone ? 0f : input.y;
            return input;
        }
        private void MapThumbsticks(ref bool inputDetected, ref bool throttleDetected)
        {
            if (!inFocusMode) return;

            Vector2 primaryThumbstick = Vector2.zero;
            Vector2 secondaryThumbstick = Vector2.zero;

            // Declare variables for triggers or extra inputs
            float primaryIndexTrigger = 0f, secondaryIndexTrigger = 0f;
            float primaryHandTrigger = 0f, secondaryHandTrigger = 0f;
            float xboxLIndexTrigger = 0f, xboxRIndexTrigger = 0f;

            // === INPUT SELECTION WITH DEADZONE ===
            // VR CONTROLLERS
            if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.Oculus)
            {
                primaryThumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
                secondaryThumbstick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

                // Oculus-specific inputs
                primaryIndexTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger);
                secondaryIndexTrigger = OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger);
                primaryHandTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger);
                secondaryHandTrigger = OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger);

                // Apply deadzone
                primaryThumbstick = ApplyDeadzone(primaryThumbstick, THUMBSTICK_DEADZONE);
                secondaryThumbstick = ApplyDeadzone(secondaryThumbstick, THUMBSTICK_DEADZONE);

                // --- Your oculus-specific mapping logic goes here, using the above values ---
            }
            else if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.OpenVR)
            {
                var leftController = SteamVRInput.GetController(HandType.Left);
                var rightController = SteamVRInput.GetController(HandType.Right);
                primaryThumbstick = leftController.GetAxis();
                secondaryThumbstick = rightController.GetAxis();

                // If you need extra OpenVR/SteamVR inputs, grab them here.

                // Apply deadzone
                primaryThumbstick = ApplyDeadzone(primaryThumbstick, THUMBSTICK_DEADZONE);
                secondaryThumbstick = ApplyDeadzone(secondaryThumbstick, THUMBSTICK_DEADZONE);

                // --- Your OpenVR-specific mapping logic goes here ---
            }
            // XBOX CONTROLLER (only if NOT in VR)
            else if (XInput.IsConnected)
            {
                primaryThumbstick = XInput.Get(XInput.Axis.LThumbstick);
                secondaryThumbstick = XInput.Get(XInput.Axis.RThumbstick);
                xboxLIndexTrigger = XInput.Get(XInput.Trigger.LIndexTrigger);
                xboxRIndexTrigger = XInput.Get(XInput.Trigger.RIndexTrigger);

                // Optionally use Unity Input axes as backup:
                // primaryThumbstick   = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                // secondaryThumbstick = new Vector2(Input.GetAxis("RightStickHorizontal"), Input.GetAxis("RightStickVertical"));

                // Apply deadzone
                primaryThumbstick = ApplyDeadzone(primaryThumbstick, THUMBSTICK_DEADZONE);
                secondaryThumbstick = ApplyDeadzone(secondaryThumbstick, THUMBSTICK_DEADZONE);

                // --- Your Xbox-specific mapping logic goes here, using xboxLIndexTrigger etc. ---
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
 isCenteringRotation = false;
            }

            // Map triggers for gas and brake rotation on X-axis
            if (GasObject)
            {
                float RIndexTrigger = XInput.Get(XInput.Trigger.RIndexTrigger);
                Quaternion gasRotation = Quaternion.Euler(
                    RIndexTrigger * triggerRotationMultiplier,
                    0f,
                    0f
                );
                GasObject.localRotation = GasStartRotation * gasRotation;
                if (Mathf.Abs(RIndexTrigger) > 0.01f) // Only set if trigger is being pressed
                    throttleDetected = true; 
 isCenteringRotation = false;
            }
            if (BrakeObject)
            {
                float LIndexTrigger = XInput.Get(XInput.Trigger.LIndexTrigger);
                Quaternion brakeRotation = Quaternion.Euler(
                    LIndexTrigger * triggerRotationMultiplier,
                    0f,
                    0f
                );
                BrakeObject.localRotation = BrakeStartRotation * brakeRotation;
                if (Mathf.Abs(LIndexTrigger) > 0.01f) // Only set if trigger is being pressed
                    throttleDetected = true; 
 isCenteringRotation = false;
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

        void ReadData()
        {
            if (!File.Exists(filePath))
            {
                logger.Debug($"{gameObject.name} outputs file not found: {filePath}");
                return;
            }

            string data;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader sr = new StreamReader(fs))
            {
                data = sr.ReadToEnd();
            }

            string[] values = data.Split(',');

            if (values.Length < 9)
            {
                logger.Debug($"{gameObject.name} Unexpected file format. Expected at least 9 values, got {values.Length}");
                return;
            }

            // Parse numerical game values
            int gameState = int.TryParse(values[0], out int gameStateVal) ? gameStateVal : -1;
            int vrView = int.TryParse(values[1], out int vrViewVal) ? vrViewVal : -1;
            int gear = int.TryParse(values[2], out int gearVal) ? gearVal : -1;
            int kph = int.TryParse(values[3], out int kphVal) ? kphVal : -1;
            int mph = Mathf.RoundToInt(kph * 0.621371f); // Convert KPH to MPH
            int rpm = int.TryParse(values[4], out int rpmVal) ? rpmVal : -1;
            int position = int.TryParse(values[5], out int positionVal) ? positionVal : -1;
            int timeLeft = int.TryParse(values[6], out int timeLeftVal) ? timeLeftVal : -1;
            int mpPosition = int.TryParse(values[7], out int mpPositionVal) ? mpPositionVal : -1;
            int numPlayers = int.TryParse(values[8], out int numPlayersVal) ? numPlayersVal : -1;
            // logger.Debug($"Current Gear: {gear}");
            // Determine emissive states
            bool vrRedOn = vrView == 1;
            bool vrBlueOn = vrView == 2;
            bool vrGreenOn = vrView == 3;
            bool vrYellowOn = vrView == 0;
            bool startButtonOn = gameState == 22;
            bool startButtonflash = gameState == 3;

            // Prevent redundant updates by checking last known state
            UpdateEmissiveState(VRRedEmissiveObject, vrRedOn);
            UpdateEmissiveState(VRBlueEmissiveObject, vrBlueOn);
            UpdateEmissiveState(VRYellowEmissiveObject, vrYellowOn);
            UpdateEmissiveState(VRGreenEmissiveObject, vrGreenOn);
            UpdateEmissiveState(StartButtonEmissiveObject, startButtonOn);

            bool flashState = (Time.frameCount % 50) < 30; // Flashes every 50 frames (30 ON, 20 OFF)

            if (startButtonflash && flashState != lastStartButtonState)
            {
                SetEmissive(StartButtonEmissiveObject, flashState);
                lastStartButtonState = flashState; // Store the last state to prevent redundant updates
            }
            UpdateShiftLight(rpm);
            // Reference to the  shifter object
            if (ShifterObject != null)
            {
                Quaternion gearRotation = Quaternion.identity; // Default rotation

                switch (gear)
                {
                    case 0:
                        gearRotation = Quaternion.Euler(0f, 30f, 30f); // Forward 20°, Left 20°
                        break;
                    case 1:
                        gearRotation = Quaternion.Euler(0f, -30f, 30f); // Down 20°, Left 20°
                        break;
                    case 2:
                        gearRotation = Quaternion.Euler(0f, -30f, -30f); // Forward 20°, Right 20°
                        break;
                    case 3:
                        gearRotation = Quaternion.Euler(0f, 30f, -30f); // Back 20°, Right 20°
                        break;
                    default:
                        gearRotation = Quaternion.identity; // No rotation
                        break;
                }

                ShifterObject.transform.localRotation = gearRotation;
            }
            // Update gear emissive indicators only if necessary
            /* if (gearEmissives != null)
            {
                for (int i = 0; i < gearEmissives.Length; i++)
                {
                    bool shouldEnable = (i + 1) == gear;
                    UpdateEmissiveState(gearEmissives[i], shouldEnable);
                }
            } */
        }
        void UpdateShiftLight(int rpm)
        {
            bool shiftLightOn = rpm > 7000;

            // Only update if the state has changed
            if (shiftLightOn != lastShiftLightState)
            {
                if (ShiftLight != null)
                {
                    ShiftLight.enabled = shiftLightOn;
                    logger.Debug($"{gameObject.name} Shift Light is now {(shiftLightOn ? "ON" : "OFF")}");
                }
                else
                {
                    logger.Debug($"{gameObject.name} Shift Light is missing!");
                }

                // Store the new state
                lastShiftLightState = shiftLightOn;
            }
        }
       
        // 🔹 Helper function to update emissives **only when state changes**
        void UpdateEmissiveState(Transform emissiveObject, bool newState)
        {
            if (emissiveObject == null) return;

            bool lastState = lastEmissiveStates.ContainsKey(emissiveObject) ? lastEmissiveStates[emissiveObject] : !newState;
            if (lastState == newState) return; // No change, skip updating

            SetEmissive(emissiveObject, newState);
            lastEmissiveStates[emissiveObject] = newState;
        }

        // Update gear emissive indicators
        /* if (gearEmissives != null)
        {
            for (int i = 0; i < gearEmissives.Length; i++)
            {
                SetEmissive(gearEmissives[i], (i + 1) == gear);
            }
        } */

        void InitializeLights()
        {

            // Gets all Light components in the target object and its children
            Light[] Lights = transform.GetComponentsInChildren<Light>();

            // Log the names of the objects containing the Light components and filter out unwanted lights
            foreach (Light light in Lights)
            {
                if (light.gameObject.name == "RaceLeaderLight")
                {
                    RaceLeaderLight = light;
                    logger.Debug($"{gameObject.name} RaceLeaderLight found in object: " + light.gameObject.name);
                }
                else if (light.gameObject.name == "ShiftLight")
                {
                    ShiftLight = light;
                    logger.Debug($"{gameObject.name} ShiftLight found in object: " + light.gameObject.name);
                }
                else
                {
                    logger.Debug($"{gameObject.name} Excluded Light found in object: " + light.gameObject.name);
                }
            }
        }

        void InitializeObjects()
        {
            ShifterObject = transform.Find("Shifter");
            if (ShifterObject != null)
            {
                logger.Debug($"{gameObject.name}Shifter object found.");
                ShifterStartPosition = ShifterObject.transform.localPosition;
                ShifterStartRotation = ShifterObject.transform.localRotation;
            }
            else
            {
                logger.Debug($"{gameObject.name} Shifter object not found.!");
            }
            WheelObject = transform.Find("Wheel");
            if (WheelObject != null)
            {
                logger.Debug($"{gameObject.name} Wheel object found.");
                WheelStartPosition = WheelObject.transform.localPosition;
                WheelStartRotation = WheelObject.transform.localRotation;
            }
            else
            {
                logger.Debug($"{gameObject.name} Wheel object not found.!");
            }
            GasObject = transform.Find("Gas");
            if (GasObject != null)
            {
                logger.Debug($"{gameObject.name}Gas object found.");
                GasStartPosition = GasObject.transform.localPosition;
                GasStartRotation = GasObject.transform.localRotation;
            }
            else
            {
                logger.Debug($"{gameObject.name} Gas object not found.!");
            }
            BrakeObject = transform.Find("Brake");
            if (BrakeObject != null)
            {
                logger.Debug($"{gameObject.name} Brake object found.");
                BrakeStartPosition = BrakeObject.transform.localPosition;
                BrakeStartRotation = BrakeObject.transform.localRotation;
            }
            else
            {
                logger.Debug($"{gameObject.name} Brake object not found.!");
            }
            // Find startButtonEmissiveObject object 
            StartButtonEmissiveObject = transform.Find("Start");
            if (StartButtonEmissiveObject != null)
            {
                logger.Debug($"{gameObject.name} StartButtonEmissive object found.");
                // Ensure the startButtonEmissive object is initially off
                Renderer renderer = transform.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} StartButtonEmissive object not found.");
            }
            // Find VRRedEmissiveObject object 
            VRRedEmissiveObject = transform.Find("VRRed");
            if (VRRedEmissiveObject != null)
            {
                logger.Debug($"{gameObject.name} VRRedEmissive object found.");
                // Ensure the vrRedEmissive object is initially off
                Renderer renderer = transform.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} VRRedEmissive object not found.");
            }
            // Find VRBlueEmissiveObject
            VRBlueEmissiveObject = transform.Find("VRBlue");
            if (VRBlueEmissiveObject != null)
            {
                logger.Debug($"{gameObject.name} VRBlueEmissive object found.");
                // Ensure the VRBlueEmissive object is initially off
                Renderer renderer = transform.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} VRBlueEmissive object not found.");
            }
            // Find VRGreenEmissiveObject object
            VRGreenEmissiveObject = transform.Find("VRGreen");
            if (VRGreenEmissiveObject != null)
            {
                logger.Debug($"{gameObject.name} VRGreenEmissive object found.");
                // Ensure the VRGreenEmissive object is initially off
                Renderer renderer = transform.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} VRGreenEmissive object not found.");
            }
            // Find VRYellowEmissiveObject object
            VRYellowEmissiveObject = transform.Find("VRYellow");
            if (VRYellowEmissiveObject != null)
            {
                logger.Debug($"{gameObject.name} VRYellowEmissive object found.");
                // Ensure the VRYellowEmissiveObject object is initially off
                Renderer renderer = transform.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} VRYellowEmissive object not found.");
            }
            string missingObjects = "";
            if (RaceLeaderLight == null) missingObjects += "Race Leader Light, ";
            if (ShiftLight == null) missingObjects += "Shift Light, ";
            if (VRRedEmissiveObject == null) missingObjects += "VR Red Emissive, ";
            if (VRBlueEmissiveObject == null) missingObjects += "VR Blue Emissive, ";
            if (VRYellowEmissiveObject == null) missingObjects += "VR Yellow Emissive, ";
            if (VRGreenEmissiveObject == null) missingObjects += "VR Green Emissive, ";
            if (StartButtonEmissiveObject == null) missingObjects += "Start Button Emissive, ";
            //    if (shiftLightEmissiveObject == null) missingObjects += "Shift Light Emissive, ";
            //    if (rpmEmissivObjectObject == null) missingObjects += "RPM Emissive, ";
            //    if (speedEmissiveObject == null) missingObjects += "Speed Emissive, ";
            //    if (timeLeftEmissiveObject == null) missingObjects += "Time Left Emissive, ";
            //    if (positionEmissiveObject == null) missingObjects += "Position Emissive, ";
            //    if (gearEmissives == null || gearEmissives.Length == 0) missingObjects += "Gear Emissives, ";

            if (!string.IsNullOrEmpty(missingObjects))
            {
                logger.Debug($"{gameObject.name} Missing objects: " + missingObjects.TrimEnd(',', ' '));
            }
            else
            {
                logger.Debug($"{gameObject.name} All required objects found.");
            }
        }
           // Method to enable or disable emissive based on the Transform reference
        void SetEmissive(Transform emissiveTransform, bool enable)
        {
            if (emissiveTransform != null)
            {
                Renderer renderer = emissiveTransform.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // If the renderer is disabled, enable it
                    if (!renderer.enabled)
                    {
                        renderer.enabled = true;
                        logger.Debug($"{gameObject.name} {emissiveTransform.name} Renderer was disabled, now enabled.");
                    }

                    // Enable or disable the emission keyword based on the flag
                    if (enable)
                    {
                        renderer.material.EnableKeyword("_EMISSION");
                        //  logger.Debug($"{gameObject.name} {emissiveTransform.name} emission enabled.");
                    }
                    else
                    {
                        renderer.material.DisableKeyword("_EMISSION");
                        //    logger.Debug($"{gameObject.name} {emissiveTransform.name} emission disabled.");
                    }
                }
                else
                {
                    logger.Debug($"{gameObject.name} {emissiveTransform.name} does not have a Renderer component.");
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} Emissive object {emissiveTransform?.name} not found.");
            }
        }
        void DisableAllEmissives()
        {
            logger.Debug($"{gameObject.name} Disabling all emissives at startup...");

            // List of all  emissive GameObjects
            Transform[] emissiveObjects = new Transform[]
            {
        VRRedEmissiveObject, VRBlueEmissiveObject, VRYellowEmissiveObject, VRGreenEmissiveObject,
        StartButtonEmissiveObject // Add more if necessary
            };

            // Loop through each emissive object and disable its emission
            foreach (var emissiveObject in emissiveObjects)
            {
                if (emissiveObject != null)
                {
                    SetEmissive(emissiveObject, false);
                }
                else
                {
                    logger.Debug($"{gameObject.name} Emissive object not found.");
                }
            }

            // Disable additional lights
            if (RaceLeaderLight != null)
                RaceLeaderLight.enabled = false;
            else
                logger.Debug($"{gameObject.name} Race Leader Light is missing!");

            if (ShiftLight != null)
                ShiftLight.enabled = false;
            else
                logger.Debug($"{gameObject.name} Shift Light is missing!");
        }
    }
}
