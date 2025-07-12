using UnityEngine;
using System.Collections.Generic;
using System.IO;
using WIGU;
using System;
using WIGUx.Modules.MameHookModule;
using System.Reflection;

namespace WIGUx.Modules.chasehqSim
{
    public class chasehqSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        [Header("Object Settings")]
        private Transform WheelObject; // Reference to shifter
        private Transform ShifterObject; // Reference to shifter
        private Transform GasObject; // Reference to the throttle mirroring object
        private Transform BrakeObject; // Reference to the throttle mirroring object
        private Transform Light1Object; // Reference to the left light
        private Transform Light2Object; // Reference to the right light

        [Header("Input Settings")]
        public string primaryThumbstickHorizontal = "Horizontal"; // Input axis for primary thumbstick horizontal
        public string primaryThumbstickVertical = "Vertical"; // Input axis for primary thumbstick vertical
        public string secondaryThumbstickHorizontal = "RightStickHorizontal"; // Input axis for secondary thumbstick horizontal
        public string secondaryThumbstickVertical = "RightStickVertical"; // Input axis for secondary thumbstick forward/backward
        public string leftTrigger = "LIndexTrigger";
        public string rightTrigger = "RIndexTrigger";

        [Header("Velocity Multiplier Settings")]        // Speeds for the animation of the in game flight stick or wheel
        private float primaryThumbstickRotationMultiplier = 50f; // Multiplier for primary thumbstick rotation intensity
        private float secondaryThumbstickRotationMultiplier = 25f; // Multiplier for secondary thumbstick rotation intensity
        private float triggerRotationMultiplier = 20f; // Multiplier for trigger rotation intensity
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
        Dictionary<string, int> lastLampStates = new Dictionary<string, int>
    {
        { "genout5", 0 }, { "genout6", 0 }
    };
        private Light genout5_light;
        private Light genout6_light;
        private Light startlight;
        private Light[] lights;

        [Header("Timers and States")]  // Store last states and timers
        private bool isHigh = true; //set the flashing flag
        private bool inFocusMode = false;  // Flag to track focus mode state
        private GameSystemState systemState;

        [Header("Rom Check")]
        private GameSystem gameSystem;  // Cached GameSystem for this cabinet.
        private string insertedGameName = string.Empty;
        private string controlledGameName = string.Empty;
        private string configPath;

        void Start()
        {
            CheckInsertedGameName();
            CheckControlledGameName();
            configPath = $"./Emulators/MAME/inputs/{insertedGameName}.ini";
            gameSystem = GetComponent<GameSystem>();
            InitializeLights();
            InitializeObjects();
            // Initialize lamp states
            if (genout5_light) ToggleLight(genout5_light, false);
            if (genout6_light) ToggleLight(genout6_light, false);
        }
        void Update()
        {
            bool inputDetected = false;  // Initialize for centering
            bool throttleDetected = false;// Initialize for centering
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
                MapThumbsticks(ref inputDetected, ref throttleDetected);
            }
        }

        void StartFocusMode()
        {
            logger.Debug($"{gameObject.name} Starting Focus Mode...");
            // StopCurrentPatterns();
            logger.Debug("Chase HQ Module starting...");
            logger.Debug("Be on the Lookout!...");
            inFocusMode = true;  // Set focus mode flag
        }

        void EndFocusMode()
        {
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
            if (genout5_light) ToggleLight(genout5_light, false);
            if (genout6_light) ToggleLight(genout6_light, false);

            inFocusMode = false;  // Clear focus mode flag
        }

        public void ReadData()
        {
            // 1) Your original “zeroed” lamp list:
            var currentLampStates = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        { "genout5", 0 }, { "genout6", 0 }
    };
            // 2) Reflectively fetch the lamp list (falling back if needed)
            IEnumerable<string> lampList = null;
            var hookType = Type.GetType(
                "WIGUx.Modules.MameHookModule.MameHookController, WIGUx.Modules.MameHookModule"
            );
            if (hookType != null)
            {
                var lampProp = hookType.GetProperty(
                    "currentLampState",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
                );
                lampList = lampProp?.GetValue(null) as IEnumerable<string>;
            }
            if (lampList == null)
                lampList = MameHookController.currentLampState;

            // 3) Parse into your state dictionary
            if (lampList != null)
            {
                foreach (var entry in lampList)
                {
                    var parts = entry.Split('|');
                    if (parts.Length != 2) continue;

                    string lamp = parts[0].Trim();
                    if (currentLampStates.ContainsKey(lamp)
                        && int.TryParse(parts[1].Trim(), out int value))
                    {
                        currentLampStates[lamp] = value;
                    }
                }
            }

            // 4) Dispatch only those lamps to your existing logic
            foreach (var kv in currentLampStates)
            {
                // matches: void ProcessLampState(string lampKey, Dictionary<string,int> currentStates)
                ProcessLampState(kv.Key, currentLampStates);
            }

        }

        // 🔹 Helper function for safe lamp processing
        void ProcessLampState(string lampKey, Dictionary<string, int> currentStates)
        {
            if (!lastLampStates.ContainsKey(lampKey))
            {
                lastLampStates[lampKey] = 0;
                logger.Debug($"{gameObject.name} Added missing key '{lampKey}' to lastLampStates.");
            }

            if (currentStates.TryGetValue(lampKey, out int newValue))
            {
                if (lastLampStates[lampKey] != newValue)
                {
                    lastLampStates[lampKey] = newValue;

                    // Call the corresponding function dynamically
                    switch (lampKey)
                    {
                        case "genout5":
                            ProcessLamp0(newValue);
                            break;
                        case "genout6":
                            ProcessLamp1(newValue);
                            break;
                        default:
                            logger.Debug($"{gameObject.name} No processing function for '{lampKey}'");
                            break;
                    }
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} Lamp key '{lampKey}' not found in current states.");
            }
        }

        // Individual function for lamp0
        void ProcessLamp0(int state)
        {
            logger.Debug($"{gameObject.name} genout6 updated: {state}");
            // Update lights
            if (genout5_light) ToggleLight(genout5_light, state == 1);
            // Update emissive material
            // if (Fire1Object) ToggleEmissive(Fire1Object.gameObject, state == 1);
        }
        // Individual function for lamp1
        void ProcessLamp1(int state)
        {
            logger.Debug($"{gameObject.name} genout5 updated: {state}");
            // Update lights
            if (genout6_light) ToggleLight(genout6_light, state == 1);
            // Update emissive material
            // if (Fire2Object) ToggleEmissive(Fire2Object.gameObject, state == 1);

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
            }
            // Calculate a new rotation from the thumbstick input.
            // shift button pressed
            if (XInput.GetDown(XInput.Button.Y))
            {
                if (!isHigh)
                {
                    ShifterObject.Rotate(0, 0, 45f);
                    isHigh = true;
                }
                else
                {
                    ShifterObject.Rotate(0, 0, -45f);
                    isHigh = false;
                }
                inputDetected = true; 
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



        // Check if object is found and log appropriate message
        void CheckObject(GameObject obj, string name)
        {
            if (obj == null)
            {
                logger.Debug($"{gameObject.name} {name} not found!");
            }
            else
            {
                logger.Debug($"{gameObject.name} {name} found.");
            }
        }
        void InitializeObjects()
        {
            ShifterObject = transform.Find("Shifter");
            if (ShifterObject != null)
            {
                logger.Debug($"{gameObject.name} Shifter object found.");
                ShifterStartPosition = ShifterObject.transform.localPosition;
                ShifterStartRotation = ShifterObject.transform.localRotation;
            }
            else
            {
                logger.Debug($"{gameObject.name} Shifter object not found.");
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
                logger.Debug($"{gameObject.name} Wheel object not found.");
            }
            GasObject = transform.Find("Gas");
            if (GasObject != null)
            {
                logger.Debug($"{gameObject.name} Gas object found.");
                GasStartPosition = GasObject.transform.localPosition;
                GasStartRotation = GasObject.transform.localRotation;
            }
            else
            {
                logger.Debug($"{gameObject.name} Gas object not found.");
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
                logger.Debug($"{gameObject.name} Brake object not found.");
            }
        }
        void InitializeLights()
        {
            // Gets all Light components in the target object and its children
            Light[] lights = transform.GetComponentsInChildren<Light>();

            // Log the names of the objects containing the Light components and filter out unwanted lights
            foreach (Light light in lights)
            {
                if (light.gameObject.name == "genout5")
                {
                    genout5_light = light;
                    logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                }
                else if (light.gameObject.name == "genout6")
                {
                    genout6_light = light;
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
