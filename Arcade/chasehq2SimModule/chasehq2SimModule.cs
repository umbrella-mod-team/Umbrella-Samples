using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;
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

        //sexy new combined input handler
        void MapThumbsticks(ref bool inputDetected)
        {
            if (!inFocusMode) return;

            Vector2 primaryThumbstick = Vector2.zero;
            Vector2 secondaryThumbstick = Vector2.zero;

            // VR Controller input
            if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.Oculus)
            {
                primaryThumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
                secondaryThumbstick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
                Vector2 ovrPrimaryThumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
                Vector2 ovrSecondaryThumbstick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
                float ovrPrimaryIndexTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger);
                float ovrSecondaryIndexTrigger = OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger);
                float ovrPrimaryHandTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger);
                float ovrSecondaryHandTrigger = OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger);
            }
            else if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.OpenVR)
            {
                var leftController = SteamVRInput.GetController(HandType.Left);
                var rightController = SteamVRInput.GetController(HandType.Right);
                primaryThumbstick = leftController.GetAxis();
                secondaryThumbstick = rightController.GetAxis();

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
                if (Mathf.Abs(RIndexTrigger) > 0.01f)
                    inputDetected = true; 
 isCenteringRotation = false;                // Only set if trigger is being pressed
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
                if (Mathf.Abs(LIndexTrigger) > 0.01f)
                    inputDetected = true; 
 isCenteringRotation = false;            // Only set if trigger is being pressed
            }
        }
        private void MapButtons(ref bool inputDetected)
        {
            if (!inFocusMode) return;
            // Check if the primary thumbstick is pressed
            if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick))
            {
                logger.Debug("OVR Primary thumbstick pressed");
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
 isCenteringRotation = false;
                }
            }

            // Check if the secondary index trigger on the left Controller is pressed
            if (OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger))
            {
                logger.Debug("OVR Secondary index trigger pressed");
            }

            // Check if the secondary hand trigger on the righttroller is pressed
            if (OVRInput.Get(OVRInput.Button.SecondaryHandTrigger))
            {
                if (SteamVRInput.GetDown(SteamVRInput.Button.RGrip))
                {
                    logger.Debug("RGrip pressed");
                    if (!isHigh)
                    {
                        // Start the flashing if not already flashing
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

            // Check if the secondary thumbstick is pressed
            if (OVRInput.GetDown(OVRInput.Button.SecondaryThumbstick))
            {
                logger.Debug("OVR Secondary thumbstick pressed");
            }
            if (SteamVRInput.GetDown(SteamVRInput.Button.RGrip))
            {
                logger.Debug("RGrip pressed");
                if (!isHigh)
                {
                    // Start the flashing if not already flashing
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

            // Check if the primary index trigger on the right Controller is pressed
            if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger))
            {
                //    logger.Debug("OVR Primary index trigger pressed");
            }

            // Thunbstick button pressed
            if (XInput.GetDown(XInput.Button.LThumbstick))
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
 isCenteringRotation = false;
            }
            // shift button pressed
            if (XInput.GetDown(XInput.Button.Y))
            {
                if (!isHigh)
                {
                    // Start the flashing if not already flashing
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
