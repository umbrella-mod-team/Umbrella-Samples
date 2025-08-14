using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR;
using WIGU;
using WIGUx.Modules.MameHookModule;

namespace WIGUx.Modules.midwayRacingSimModule
{
    public class midwayRacingSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        [Header("Object Settings")]
        private Transform WheelObject; // Reference to the Wheel mirroring object
        private Transform ShifterObject; // Reference to the Shifter mirroring object
        private Transform GasObject; // Reference to the Gas mirroring object
        private Transform BrakeObject; // Reference to the Throttle mirroring object

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
        private float WheelRotationDegrees = 100f; // Degrees for wheel rotation, adjust as needed
        private readonly float rotationSmoothness = 5f;  //sets the smoothness of the rotation
        private readonly float thumbstickVelocity = 50f;  // Velocity for keyboard input

        [Header("Rotation Tracking")]        // Sets up the rotation varibles and sets them to 0 

        [Header("Rotation Limits")]        // Rotation Limits 
        [SerializeField] float minRotationX = -15f;
        [SerializeField] float maxRotationX = 15f;
        [SerializeField] float minRotationY = -15f;
        [SerializeField] float maxRotationY = 15f;
        [SerializeField] float minRotationZ = -15f;
        [SerializeField] float maxRotationZ = 15f;

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
        private Transform Lamp0Object; 
        private Transform Lamp1Object;
        private Transform Lamp2Object; 
        private Transform Lamp3Object;
        private Transform Lamp4Object;
        private Transform Lamp5Object;
        private Transform Lamp6Object; 
        private Transform Lamp7Object;
        private Transform Lamp8Object;
        private Transform Brakelight1Object;
        private Transform Brakelight2Object;
        public Light lamp0_light;
        public Light lamp1_light;
        public Light lamp2_light;
        public Light lamp3_light;
        public Light lamp4_light;
        public Light lamp5_light;
        public Light lamp6_light;
        public Light lamp7_light;
        public Light lamp8_light;
        public Light brake1_light;
        public Light brake2_light;
        private float lightDuration = 0.35f;
        private float attractFlashDuration = 0.7f;
        private float attractFlashDelay = 0.7f;
        private float dangerFlashDuration = 0.3f;
        private float dangerFlashDelay = 0.3f;
        private Coroutine dangerCoroutine; // Coroutine variable to control the focused danger mode
        private Coroutine attractCoroutine; // Coroutine variable to control the attract mode
        private Light[] lights;        //array of lights
        Dictionary<string, int> lastLampStates = new Dictionary<string, int>
             {
               { "lamp0", 0 }, { "lamp1", 0 }, { "lamp2", 0 }, { "lamp3", 0 }, { "lamp4", 0 }, { "lamp5", 0 }, { "lamp6", 0 }, { "lamp7", 0 }
             };
        [Header("Timers and States")]  // Store last states and timers
        private bool isFlashing = false; //set the flashing lights flag
        private bool isHigh = false; //set the high gear flag
        private bool inFocusMode = false;  // Flag to track focus mode state
        private bool isCenteringRotation = false; // Flag to track centering rotation state
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
            if (lamp5_light) ToggleLight(lamp5_light, false);
            if (lamp6_light) ToggleLight(lamp6_light, false);
            if (lamp7_light) ToggleLight(lamp7_light, false);
            if (lamp8_light) ToggleLight(lamp8_light, false);
            //if (brake_light1) ToggleLight(brake_light1, false);
            // if (brake_light2) ToggleLight(brake_light2, false);
            if (Lamp0Object) ToggleEmissive(Lamp0Object.gameObject, false);
            if (Lamp8Object) ToggleEmissive(Lamp8Object.gameObject, false);
            if (Lamp1Object) ToggleEmissive(Lamp1Object.gameObject, false);
            if (Lamp2Object) ToggleEmissive(Lamp2Object.gameObject, false);
            if (Lamp3Object) ToggleEmissive(Lamp3Object.gameObject, false);
            if (Lamp4Object) ToggleEmissive(Lamp4Object.gameObject, false);
            if (Lamp5Object) ToggleEmissive(Lamp5Object.gameObject, false);
            if (Lamp6Object) ToggleEmissive(Lamp6Object.gameObject, false);
            if (Lamp7Object) ToggleEmissive(Lamp7Object.gameObject, false);
            if (Lamp8Object) ToggleEmissive(Lamp8Object.gameObject, false);
           // StartAttractPattern();
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
                MapThumbsticks();
                MapButtons();
            }
        }


        void StartFocusMode()
        {
            logger.Debug($"{gameObject.name} Starting Focus Mode...");
            StopCurrentPatterns();
            inFocusMode = true;  // Set focus mode flag           
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
        }
        private const float THUMBSTICK_DEADZONE = 0.13f; // Adjust as needed

        private Vector2 ApplyDeadzone(Vector2 input, float deadzone)
        {
            input.x = Mathf.Abs(input.x) < deadzone ? 0f : input.x;
            input.y = Mathf.Abs(input.y) < deadzone ? 0f : input.y;
            return input;
        }
        private void MapThumbsticks()
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

        private void MapButtons()
        {
            if (!inFocusMode) return;
            /*
            // LeftTrigger
            if (
                XInput.GetDown(XInput.Button.LIndexTrigger)
                || OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger)
                || SteamVRInput.GetDown(SteamVRInput.TouchButton.LTrigger)
            )
            {
                if (brake_light1) ToggleLight(brake_light1, true);
                if (brake_light2) ToggleLight(brake_light2, true);
                if (BrakelightsObject) ToggleEmissive(BrakelightsObject.gameObject, true);
                if (Brakelight1Object) ToggleEmissive(Brakelight1Object.gameObject, true);
                if (Brakelight2Object) ToggleEmissive(Brakelight2Object.gameObject, true);
            }

            // Reset position on button release
            if (
                XInput.GetUp(XInput.Button.LIndexTrigger)
                || OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger)
                || SteamVRInput.GetUp(SteamVRInput.TouchButton.LTrigger)
            )
            {
                if (brake_light1) ToggleLight(brake_light1, false);
                if (brake_light2) ToggleLight(brake_light2, false);
                if (BrakelightsObject) ToggleEmissive(BrakelightsObject.gameObject, false);
                if (Brakelight1Object) ToggleEmissive(Brakelight1Object.gameObject, false);
                if (Brakelight2Object) ToggleEmissive(Brakelight2Object.gameObject, false);
            }

            /*
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
            */
        }

        void CenterRotation()
        {
            isCenteringRotation = true;
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

        void ReadData()
        {
            // 1) Your original “zeroed” lamp list:
            var currentLampStates = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
             {
               { "lamp0", 0 }, { "lamp1", 0 }, { "lamp2", 0 }, { "lamp3", 0 }, { "lamp4", 0 }, { "lamp5", 0 }, { "lamp6", 0 }, { "lamp7", 0 }
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
                            ProcessLamp0(newValue);
                            break;
                        case "lamp1":
                            ProcessLamp1(newValue);
                            break;
                        case "lamp2":
                            ProcessLamp2(newValue);
                            break;
                        case "lamp3":
                            ProcessLamp3(newValue);
                            break;
                        case "lamp4":
                            ProcessLamp4(newValue);
                            break;
                        case "lamp5":
                            ProcessLamp5(newValue);
                            break;
                        case "lamp6":
                            ProcessLamp6(newValue);
                            break;
                        case "lamp7":
                            ProcessLamp7(newValue);
                            break;
                        case "lamp8":
                            ProcessLamp8(newValue);
                            break;
                        default:
                            logger.Debug($"No processing function for '{lampKey}'");
                            break;
                    }
                }
            }
            else
            {
                logger.Error($"{gameObject.name} Lamp key '{lampKey}' not found in current states.");
            }
        }

        // Individual function for lamp0
        void ProcessLamp0(int state)
        {
            logger.Debug($"Lamp 0 updated: {state}");

            // Update lights
            if (lamp0_light) ToggleLight(lamp0_light, state == 1);
            // Update emissive material
            if (Lamp0Object) ToggleEmissive(Lamp0Object.gameObject, state == 1);


        }
        // Individual function for lamp1
        void ProcessLamp1(int state)
        {
            logger.Debug($"Lamp 1 updated: {state}");
            // Update lights
            if (lamp1_light) ToggleLight(lamp1_light, state == 1);
            // Update emissive material
            if (Lamp1Object) ToggleEmissive(Lamp1Object.gameObject, state == 1);
        }
        // Individual function for lamp2
        void ProcessLamp2(int state)
        {
            logger.Debug($"Lamp 2 updated: {state}");

            // Update lights
            if (lamp2_light) ToggleLight(lamp2_light, state == 1);
            // Update emissive material
            if (Lamp2Object) ToggleEmissive(Lamp2Object.gameObject, state == 1);

        }

        // Individual function for lamp3
        void ProcessLamp3(int state)
        {
            logger.Debug($"Lamp 3 updated: {state}");

            // Update lights
            if (lamp3_light) ToggleLight(lamp3_light, state == 1);
            // Update emissive material
            if (Lamp3Object) ToggleEmissive(Lamp3Object.gameObject, state == 1);
        }

        // Individual function for lamp4
        void ProcessLamp4(int state)
        {
            logger.Debug($"Lamp 4 updated: {state}");

            // Update lights
            if (lamp4_light) ToggleLight(lamp4_light, state == 1);
            // Update emissive material
            if (Lamp4Object) ToggleEmissive(Lamp4Object.gameObject, state == 1);


        }

        // Individual function for lamp5
        void ProcessLamp5(int state)
        {
            logger.Debug($"Lamp 5 updated: {state}");

            // Update lights
            if (lamp5_light) ToggleLight(lamp5_light, state == 1);
            // Update emissive material
            if (Lamp5Object) ToggleEmissive(Lamp5Object.gameObject, state == 1);
        }

        // Individual function for lamp36
        void ProcessLamp6(int state)
        {
            logger.Debug($"Lamp 6 updated: {state}");

            // Update lights
            if (lamp6_light) ToggleLight(lamp6_light, state == 1);
            // Update emissive material
            if (Lamp6Object) ToggleEmissive(Lamp6Object.gameObject, state == 1);


        }

        // Individual function for lamp7
        void ProcessLamp7(int state)
        {
            logger.Debug($"Lamp 7 updated: {state}");

            // Update lights
            if (lamp7_light) ToggleLight(lamp7_light, state == 1);

            // Update emissive material
            if (Lamp7Object) ToggleEmissive(Lamp7Object.gameObject, state == 1);

        }

        // Individual function for lamp8
        void ProcessLamp8(int state)
        {
            logger.Debug($"Lamp 8 updated: {state}");

            // Update lights
            if (lamp8_light) ToggleLight(lamp8_light, state == 1);
            // Update emissive material
            if (Lamp8Object) ToggleEmissive(Lamp8Object.gameObject, state == 1);
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

        IEnumerator attractPattern()  //Pattern For Attract Mode
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 1f));
            while (true)
            {

                yield return new WaitForSeconds(attractFlashDuration);

                yield return new WaitForSeconds(attractFlashDelay);
            }
        }

        IEnumerator dangerPattern() //Pattern For Focused Danger Mode
        {
            while (true)
            {

                yield return new WaitForSeconds(dangerFlashDuration);

                yield return new WaitForSeconds(dangerFlashDelay);
            }
        }
        public void StartAttractPattern()
        {
            // Stop any currently running coroutines
            StopCurrentPatterns();
            attractCoroutine = StartCoroutine(attractPattern());
        }
        public void StartDangerPattern()
        {
            // Stop any currently running coroutines
            StopCurrentPatterns();
            dangerCoroutine = StartCoroutine(dangerPattern());
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
        }
        void InitializeLights()
        {
            // Gets all Light components in the target object and its children
            Light[] Lights = transform.GetComponentsInChildren<Light>();

            // Log the names of the objects containing the Light components and filter out unwanted lights
            foreach (Light light in Lights)
            {
                if (light.gameObject.name == "lamp_0")
                {
                    lamp0_light = light;
                    logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                }
                else if (light.gameObject.name == "lamp_1")
                {
                    lamp1_light = light;
                    logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                }
                else if (light.gameObject.name == "lamp_2")
                {
                    lamp2_light = light;
                    logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                }
                else if (light.gameObject.name == "lamp_3")
                {
                    lamp3_light = light;
                    logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                }
                else if (light.gameObject.name == "lamp_4")
                {
                    lamp4_light = light;
                    logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                }
                else if (light.gameObject.name == "lamp_5")
                {
                    lamp5_light = light;
                    logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                }
                else if (light.gameObject.name == "lamp_6")
                {
                    lamp6_light = light;
                    logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                }
                else if (light.gameObject.name == "lamp_7")
                {
                    lamp7_light = light;
                    logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                }
                else if (light.gameObject.name == "lamp_8")
                {
                    lamp8_light = light;
                    logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                }
                /*
                if (light.gameObject.name == "brakelight1")
                {
                    brake_light1 = light;
                    logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                }
                if (light.gameObject.name == "brakelight2")
                {
                    brake_light2 = light;
                    logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                }
                */
                else
                {
                    logger.Debug($"{gameObject.name} Excluded Light found in object: " + light.gameObject.name);
                }
            }
        }
        void InitializeObjects()
        {

            // Find Lamp0Object object under Z
            Lamp0Object = transform.Find("Lamp_0");
            if (Lamp0Object != null)
            {
                logger.Debug($"{gameObject.name} Lamp0 object found.");

                Renderer renderer = Lamp0Object.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} Lamp0 object not found.");
            }
            // Find Lamp0Object object under Z
            Lamp1Object = transform.Find("Lamp_1");
            if (Lamp1Object != null)
            {
                logger.Debug($"{gameObject.name} Lamp1 object found.");

                Renderer renderer = Lamp1Object.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} Lamp1 object not found.");
            }
            // Find Lamp2Object object under Z
            Lamp2Object = transform.Find("Lamp_2");
            if (Lamp2Object != null)
            {
                logger.Debug($"{gameObject.name} Lamp2 object found.");

                Renderer renderer = Lamp2Object.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} Lamp2 object not found.");
            }
            // Find Lamp3Object object under Z
            Lamp3Object = transform.Find("Lamp_3");
            if (Lamp3Object != null)
            {
                logger.Debug($"{gameObject.name} Lamp3 object found.");

                Renderer renderer = Lamp3Object.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} Lamp3 object not found.");
            }
            // Find Lamp4Object object under Z
            Lamp4Object = transform.Find("Lamp_4");
            if (Lamp4Object != null)
            {
                logger.Debug($"{gameObject.name} Lamp4 object found.");

                Renderer renderer = Lamp4Object.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} Lamp4 object not found.");
            }
            // Find Lamp5Object object under Z
            Lamp5Object = transform.Find("Lamp_5");
            if (Lamp5Object != null)
            {
                logger.Debug($"{gameObject.name} Lamp5 object found.");

                Renderer renderer = Lamp5Object.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} Lamp5 object not found.");
            }
            // Find Lamp6Object object under Z
            Lamp6Object = transform.Find("Lamp_6");
            if (Lamp6Object != null)
            {
                logger.Debug($"{gameObject.name} Lamp6 object found.");

                Renderer renderer = Lamp6Object.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} Lamp6 object not found.");
            }
            // Find Lamp0Object object under Z
            Lamp7Object = transform.Find("Lamp_7");
            if (Lamp7Object != null)
            {
                logger.Debug($"{gameObject.name} Lamp7 object found.");

                Renderer renderer = Lamp7Object.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} Lamp7 object not found.");
            }
            // Find Lamp8Object object under Z
            Lamp8Object = transform.Find("Lamp_8");
            if (Lamp8Object != null)
            {
                logger.Debug($"{gameObject.name} Lamp8 object found.");

                Renderer renderer = Lamp8Object.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} Lamp8 object not found.");
            }

            // Find Wheel under Z
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
            /*
            // Find Brakelights
            BrakelightsObject = transform.Find("Brakelights");
            if (BrakelightsObject != null)
            {
                logger.Debug($"{gameObject.name} Brakelights object found.");
            }
            else
            {
                //   logger.Debug($"{gameObject.name} Brakelights object not found.");
            }
            // Find Brakelight1
            Brakelight1Object = transform.Find("Brakelight1");
            if (Brakelight1Object != null)
            {
                logger.Debug($"{gameObject.name} Brakelight1 object found.");
            }
            else
            {
                // logger.Debug($"{gameObject.name} Brakelight1 object not found.");
            }
            // Find Brakelight2
            Brakelight2Object = transform.Find("Brakelight2");
            if (Brakelight2Object != null)
            {
                logger.Debug($"{gameObject.name} Brakelight2 object found.");
            }
            else
            {
                //   logger.Debug($"{gameObject.name} Brakelight2 object not found.");
            }
            */

        }
    }
}

