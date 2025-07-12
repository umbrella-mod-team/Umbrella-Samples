using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using System.Collections;
using static XInput;
using Valve.VR;
using static SteamVR_Utils;
using System.IO;
using System;
using WIGUx.Modules.MameHookModule;
using System.Reflection;

namespace WIGUx.Modules.spyhuntSim
{
    public class spyhuntSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        [Header("Object Settings")]
        private Transform WheelObject; // Reference to shifter
        private Transform ShifterObject; // Reference to shifter
        private Transform GasObject; // Reference to the throttle mirroring object

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
        private float WheelRotationDegrees = 50f; // Degrees for wheel rotation, adjust as needed
        private readonly float rotationSmoothness = 5f;  //sets the smoothness of the rotation
        private readonly float thumbstickVelocity = 30f;  // Velocity for keyboard input

        [Header("Rotation Tracking")]        // Sets up the rotation varibles and sets them to 0 
        private float currentRotationX = 0f;  // Current rotation for X-axis
        private float currentRotationY = 0f;  // Current rotation for Y-axis
        private float currentRotationZ = 0f;  // Current rotation for Z-axis

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
        private Transform lamp0Object; // Reference to the light
        private Transform lamp1Object; // Reference to the light
        private Transform lamp2Object; // Reference to the light
        private Transform lamp3Object; // Reference to the light
        private Transform lamp4Object; // Reference to the light
        private Transform weaponObject; // Reference to the light
        private Light lamp0_light;
        private Light lamp1_light;
        private Light lamp2_light;
        private Light lamp3_light;
        private Light lamp4_light;
        private Light weapon_light;
        private Light[] lights;
        Dictionary<string, int> lastLampStates = new Dictionary<string, int>
         {
        { "lamp0", 0 }, { "lamp1", 0 }, { "lamp2", 0 }, { "lamp3", 0 }, { "lamp4", 0 }
        };

        [Header("Timers and States")]  // Store last states and timers
        private bool isFlashing = false; //set the flashing lights flag
        private bool isHigh = false; //set the high gear flag
        private bool inFocusMode = false;  // Flag to track focus mode state
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
            InitializeLights();
            InitializeObjects();
            if (lamp0_light) ToggleLight(lamp0_light, false);
            if (lamp1_light) ToggleLight(lamp1_light, false);
            if (lamp2_light) ToggleLight(lamp2_light, false);
            if (lamp3_light) ToggleLight(lamp3_light, false);
            if (lamp4_light) ToggleLight(lamp4_light, false);
            if (weapon_light) ToggleLight(weapon_light, false);
            if (weaponObject) ToggleEmissive(weaponObject.gameObject, false);
            if (lamp0Object) ToggleEmissive(lamp0Object.gameObject, false);
            if (lamp1Object) ToggleEmissive(lamp1Object.gameObject, false);
            if (lamp2Object) ToggleEmissive(lamp2Object.gameObject, false);
            if (lamp3Object) ToggleEmissive(lamp3Object.gameObject, false);
            if (lamp4Object) ToggleEmissive(lamp4Object.gameObject, false);
        }
        void Update()
        {
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
            bool inputDetected = false;
            bool throttleDetected = false;
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
                MapButtons(ref inputDetected, ref throttleDetected);
            }
        }


        void StartFocusMode()
        {
            logger.Debug(" Spy Hunter Module starting...");
            logger.Debug("Deploying Weapons Van!...");
            inFocusMode = true;  // Set focus mode flag
        }

        void EndFocusMode()
        {
            if (WheelObject != null)
            {
                WheelObject.localPosition = WheelStartPosition;
                WheelObject.localRotation = WheelStartRotation;
            }
            if (GasObject != null)
            {
                GasObject.localPosition = GasStartPosition;
                GasObject.localRotation = GasStartRotation;
            }
            if (ShifterObject != null)
            {
                ShifterObject.localPosition = ShifterStartPosition;
                ShifterObject.localRotation = ShifterStartRotation;
            }
            if (lamp0_light) ToggleLight(lamp0_light, false);
            if (lamp1_light) ToggleLight(lamp1_light, false);
            if (lamp2_light) ToggleLight(lamp2_light, false);
            if (lamp3_light) ToggleLight(lamp3_light, false);
            if (lamp4_light) ToggleLight(lamp4_light, false);
            if (weapon_light) ToggleLight(weapon_light, false);
            if (weaponObject) ToggleEmissive(weaponObject.gameObject, false);
            if (lamp0Object) ToggleEmissive(lamp0Object.gameObject, false);
            if (lamp1Object) ToggleEmissive(lamp1Object.gameObject, false);
            if (lamp2Object) ToggleEmissive(lamp2Object.gameObject, false);
            if (lamp3Object) ToggleEmissive(lamp3Object.gameObject, false);
            if (lamp4Object) ToggleEmissive(lamp4Object.gameObject, false);
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
            // Reset rotation allowances and current rotation values
            currentRotationX = 0f;
            currentRotationY = 0f;
            currentRotationZ = 0f;
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
            // 1) Your original “zeroed” lamp list:
            var currentLampStates = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        { "lamp0", 0 }, { "lamp1", 0 }, { "lamp2", 0 }, { "lamp3", 0 }, { "lamp4", 0 }
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
                logger.Debug($"Added missing key '{lampKey}' to lastLampStates.");
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
                            Proocesslamp0(newValue);
                            break;
                        case "lamp1":
                            Proocesslamp1(newValue);
                            break;
                        case "lamp2":
                            Proocesslamp2(newValue);
                            break;
                        case "lamp3":
                            Proocesslamp3(newValue);
                            break;
                        case "lamp4":
                            Processlamp4(newValue);
                            break;
                        default:
                            logger.Debug($"No processing function for '{lampKey}'");
                            break;
                    }
                }
            }
            else
            {
                logger.Debug($"Lamp key '{lampKey}' not found in current states.");
            }
        }

        // Individual function for lamp0
        void Proocesslamp0(int state)
        {
            logger.Debug($"lamp0 updated: {state}");

            // Update lights
            if (lamp0_light) ToggleLight(lamp0_light, state == 1);
            // Update emissive material
            if (lamp0Object) ToggleEmissive(lamp0Object.gameObject, state == 1);
        }
        // Individual function for lamp1
        void Proocesslamp1(int state)
        {
            logger.Debug($"lamp1 updated: {state}");

            // Update lights
            if (lamp1_light) ToggleLight(lamp1_light, state == 1);
            // Update emissive material
            if (lamp1Object) ToggleEmissive(lamp1Object.gameObject, state == 1);
        }

        // Individual function for lamp2
        void Proocesslamp2(int state)
        {
            logger.Debug($"lamp2 updated: {state}");

            // Update lights
            if (lamp2_light) ToggleLight(lamp2_light, state == 1);
            // Update emissive material
            if (lamp2Object) ToggleEmissive(lamp2Object.gameObject, state == 1);
        }
        // Individual function for lamp3
        void Proocesslamp3(int state)
        {
            logger.Debug($"lamp3 updated: {state}");

            // Update lights
               if (lamp3_light) ToggleLight(lamp3_light, state == 1);
            // Update emissive material
            if (lamp3Object) ToggleEmissive(lamp3Object.gameObject, state == 1);
        }

        // Individual function for lamp4
        void Processlamp4(int state)
        {
            logger.Debug($"lamp4 updated: {state}");

            // Update lights
            if (lamp4_light) ToggleLight(lamp4_light, state == 1);
            if (weapon_light) ToggleLight(weapon_light, state == 1);
            // Update emissive material
            if (lamp4Object) ToggleEmissive(lamp4Object.gameObject, state == 1);
            if (weaponObject) ToggleEmissive(weaponObject.gameObject, state == 1);
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
        }

        private void MapButtons(ref bool inputDetected, ref bool throttleDetected) // Pass by reference
        {
            if (!inFocusMode) return;

            // shift button pressed
            if (XInput.GetDown(XInput.Button.B))
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

                    //    logger.Debug($"{targetObject.name} emissive state set to {(isActive ? "ON" : "OFF")} with color {emissionColor} and intensity {intensity}.");
                }
                else
                {
                    //    logger.Debug($"Renderer component not found on {targetObject.name}.");
                }
            }
            else
            {
                logger.Debug("Target emissive object is not assigned.");
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

        void InitializeLights()
        {
            // Gets all Light components in the target object and its children
            Light[] lights = transform.GetComponentsInChildren<Light>();

            // Log the names of the objects containing the Light components and filter out unwanted lights
            foreach (Light light in lights)
            {
                if (light.gameObject.name == "lamp0_light")
                {
                    lamp0_light = light;
                    logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                }
                else if (light.gameObject.name == "lamp1_light")
                {
                    lamp1_light = light;
                    logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                }
                else if (light.gameObject.name == "lamp2_light")
                {
                    lamp2_light = light;
                    logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                }
                else if (light.gameObject.name == "lamp3_light")
                {
                    lamp3_light = light;
                    logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                }
                else if (light.gameObject.name == "lamp4_light")
                {
                    lamp4_light = light;
                    logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                }
                else if (light.gameObject.name == "weapon_light")
                {
                    weapon_light = light;
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
            WheelObject = transform.Find("Wheel");
            if (WheelObject != null)
            {
                logger.Debug("Wheel object found.");
                WheelStartPosition = WheelObject.transform.localPosition;
                WheelStartRotation = WheelObject.transform.localRotation;
            }
            else
            {
                logger.Error("Wheel object not found!!");
            }
            ShifterObject = transform.Find("Shifter");
            if (ShifterObject != null)
            {
                logger.Debug("Shifter object found.");
                ShifterStartPosition = ShifterObject.transform.localPosition;
                ShifterStartRotation = ShifterObject.transform.localRotation;
            }
            else
            {
                logger.Error("Shifter object not found!!");
            }
            GasObject = transform.Find("Gas");
            if (GasObject != null)
            {
                logger.Debug("Gas object found.");
                GasStartPosition = GasObject.transform.localPosition;
                GasStartRotation = GasObject.transform.localRotation;
            }
            // Manually check every single emissive object
            lamp0Object = transform.Find("Emissives/lamp0");
            if (lamp0Object != null) logger.Debug("lamp0 found.");
            else logger.Warning("lamp0 not found.");

            lamp1Object = transform.Find("Emissives/lamp1");
            if (lamp1Object != null) logger.Debug("lamp1 found.");
            else logger.Warning("lamp1 not found.");

            lamp2Object = transform.Find("Emissives/lamp2");
            if (lamp2Object != null) logger.Debug("lamp2 found.");
            else logger.Warning("lamp2 not found.");

            lamp3Object = transform.Find("Emissives/lamp3");
            if (lamp3Object != null) logger.Debug("lamp3 found.");
            else logger.Warning("lamp3 not found.");

            lamp4Object = transform.Find("Emissives/lamp4");
            if (lamp4Object != null) logger.Debug("lamp4 found.");
            else logger.Warning("lamp4 not found.");

            weaponObject = WheelObject.Find("weapon");
            if (weaponObject != null) logger.Debug("weapon van lamp found.");
            else logger.Warning("weapon lamp not found.");

        }
    }
}
