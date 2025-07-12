using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using EmuVR.InputManager;
using System.Collections;
using static XInput;
using Valve.VR;
using static SteamVR_Utils;
using System.IO;
using System;
using WIGUx.Modules.MameHookModule;
using System.Reflection;

namespace WIGUx.Modules.orunnersSimModule
{
    public class orunnersSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        [Header("Object Settings")]
        private Transform WheelObject; // Reference to shifter
        private Transform ShifterObject; // Reference to shifter
        private Transform GasObject; // Reference to the throttle mirroring object
        private Transform BrakeObject; // Reference to the throttle mirroring object
        private Transform MA_DJ_Music_lampObject; // Reference to the left light
        private Transform MA_backnext_lampObject; // Reference to the right light
        private Transform MA_Check_Point_lampObject; // Reference to the left light
        private Transform MA_Race_Leader_lampObject; // Reference to the right light
        private Transform MB_DJ_Music_lampObject; // Reference to the left light
        private Transform MB_backnext_lampObject; // Reference to the right light
        private Transform MB_Check_Point_lampObject; // Reference to the left light
        private Transform MB_Race_Leader_lampObject; // Reference to the right light

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
        private Light p1_light;
        private Light p2_light;
        private Light startlight;
        private Light[] lights;
        Dictionary<string, int> lastLampStates = new Dictionary<string, int>
    {
        { "MA_DJ_Music_lamp", 0 }, { "MA_<<_>>_lamp", 0 }, { "MA_Check_Point_lamp", 0 }, { "MA_Race_Leader_lamp", 0 }, { "MB_DJ_Music_lamp", 0 }, { "MB_<<_>>_lamp", 0 }, { "MB_Check_Point_lamp", 0 }, { "MB_Race_Leader_lamp", 0 }
    };
        private Color emissiveOnColor = Color.white;        // Emissive colors
        private Color emissiveOffColor = Color.black;        // Emissive colors

        [Header("Timers and States")]  // Store last states and timers
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
            DisableAllEmissives();
            // Initialize lamp states
            if (p1_light) ToggleLight(p1_light, false);
            if (p2_light) ToggleLight(p2_light, false);
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
            bool inputDetected = false;  // Initialize for centering 
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
            // StopCurrentPatterns();
            logger.Debug("Outrunners Module starting...");
            logger.Debug("Pick a tune!...");
            inFocusMode = true;  // Set focus mode flag
        }

        void EndFocusMode()
        {
            logger.Debug($"{gameObject.name} Exiting Focus Mode...");
            //StartAttractPattern();
            // if (Fire1Object) ToggleEmissive(Fire1Object.gameObject, false);
            // if (Fire2Object) ToggleEmissive(Fire2Object.gameObject, false);
            ResetPositions();
            inFocusMode = false;  // Clear focus mode flag
        }

        void ResetPositions()      // Reset objects and cockpit cam to initial position and rotation
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
        }
        void ReadData()
        {
            // 1) Your original “zeroed” lamp list:
            var currentLampStates = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
             { "MA_DJ_Music_lamp", 0 }, { "MA_<<_>>_lamp", 0 }, { "MA_Check_Point_lamp", 0 }, { "MA_Race_Leader_lamp", 0 }, { "MB_DJ_Music_lamp", 0 }, { "MB_<<_>>_lamp", 0 }, { "MB_Check_Point_lamp", 0 }, { "MB_Race_Leader_lamp", 0 }
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


            // 🔹 Ensure `lastLampStates` is initialized
            if (lastLampStates == null)
            {
                lastLampStates = new Dictionary<string, int>
        {
            { "MA_DJ_Music_lamp", 0 }, { "MA_<<_>>_lamp", 0 }, { "MA_Check_Point_lamp", 0 }, { "MA_Race_Leader_lamp", 0 }, { "MB_DJ_Music_lamp", 0 }, { "MB_<<_>>_lamp", 0 }, { "MB_Check_Point_lamp", 0 }, { "MB_Race_Leader_lamp", 0 }
        };
                logger.Debug("lastLampStates was NULL. Re-initialized.");
            }

            // 🔹 Process lamps safely
            ProcessLampState("MA_DJ_Music_lamp", currentLampStates);
            ProcessLampState("MA_<<_>>_lamp", currentLampStates);
            ProcessLampState("MA_Check_Point_lamp", currentLampStates);
            ProcessLampState("MA_Race_Leader_lamp", currentLampStates);
            ProcessLampState("MB_DJ_Music_lamp", currentLampStates);
            ProcessLampState("MB_<<_>>_lamp", currentLampStates);
            ProcessLampState("MB_Check_Point_lamp", currentLampStates);
            ProcessLampState("MB_Race_Leader_lamp", currentLampStates);
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
                        case "MA_DJ_Music_lamp":
                            ProocessMA_DJ_Music_lamp(newValue);
                            break;
                        case "MA_<<_>>_lamp":
                            ProcessMA_backnext_lamp(newValue);
                            break;
                        case "MA_Check_Point_lamp":
                            ProcessMA_Check_Point_lamp(newValue);
                            break;
                        case "MA_Race_Leader_lamp":
                            ProcessMA_Race_Leader_lamp(newValue);
                            break;
                        case "MB_DJ_Music_lamp":
                            ProcessMB_DJ_Music_lamp(newValue);
                            break;
                        case "MB_<<_>>_lamp":
                            ProcessMB_backnext_lamp(newValue);
                            break;
                        case "MB_Check_Point_lamp":
                            ProcessMB_Check_Point_lamp(newValue);
                            break;
                        case "MB_Race_Leader_lamp":
                            ProcesMB_Race_Leader_lamp(newValue);
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

        // Individual function for MA_DJ_Music_lamp
        void ProocessMA_DJ_Music_lamp(int state)
        {
            logger.Debug($"MA_DJ_Music_lamp updated: {state}");

            // Update lights
            // if (1_light) ToggleLight(1_light, state == 1);
            // Update emissive material
            if (MA_DJ_Music_lampObject) ToggleEmissive(MA_DJ_Music_lampObject.gameObject, state == 1);
        }
        // Individual function for MA_backnext_lamp
        void ProcessMA_backnext_lamp(int state)
        {
            logger.Debug($"MA_backnext_lamp updated: {state}");

            // Update lights
            // if (2_light) ToggleLight(2_light, state == 1);
            // Update emissive material
            if (MA_backnext_lampObject) ToggleEmissive(MA_backnext_lampObject.gameObject, state == 1);
        }
        // Individual function for MA_DJ_Music_lamp
        void ProcessMA_Check_Point_lamp(int state)
        {
            logger.Debug($"MA_Check_Point_lamp updated: {state}");

            // Update lights
            // if (1_light) ToggleLight(1_light, state == 1);
            // Update emissive material
            if (MA_Check_Point_lampObject) ToggleEmissive(MA_Check_Point_lampObject.gameObject, state == 1);
        }
        // Individual function for MA_backnext_lamp
        void ProcessMA_Race_Leader_lamp(int state)
        {
            logger.Debug($"MA_Race_Leader_lamp updated: {state}");

            // Update lights
            // if (2_light) ToggleLight(2_light, state == 1);
            // Update emissive material
            if (MA_Race_Leader_lampObject) ToggleEmissive(MA_Race_Leader_lampObject.gameObject, state == 1);
        }

        // Individual function for MB_DJ_Music_lamp
        void ProcessMB_DJ_Music_lamp(int state)
        {
            logger.Debug($"MB_DJ_Music_lamp updated: {state}");

            // Update lights
            // if (1_light) ToggleLight(1_light, state == 1);
            // Update emissive material
            if (MB_DJ_Music_lampObject) ToggleEmissive(MB_DJ_Music_lampObject.gameObject, state == 1);
        }
        // Individual function for MB_backnext_lamp
        void ProcessMB_backnext_lamp(int state)
        {
            logger.Debug($"MB_backnext_lamp updated: {state}");

            // Update lights
            // if (2_light) ToggleLight(2_light, state == 1);
            // Update emissive material
            if (MB_backnext_lampObject) ToggleEmissive(MB_backnext_lampObject.gameObject, state == 1);
        }

        // Individual function for MB_Check_Point_lamp
        void ProcessMB_Check_Point_lamp(int state)
        {
            logger.Debug($"MB_Check_Point_lam updated: {state}");

            // Update lights
            // if (1_light) ToggleLight(1_light, state == 1);
            // Update emissive material
            if (MB_Check_Point_lampObject) ToggleEmissive(MB_Check_Point_lampObject.gameObject, state == 1);
        }
        // Individual function for MA_backnext_lamp
        void ProcesMB_Race_Leader_lamp(int state)
        {
            logger.Debug($"MB_Race_Leader_lamp updated: {state}");

            // Update lights
            // if (2_light) ToggleLight(2_light, state == 1);
            // Update emissive material
            if (MB_Race_Leader_lampObject) ToggleEmissive(MB_Race_Leader_lampObject.gameObject, state == 1);
        }

        private void MapThumbsticks(ref bool inputDetected)
        {
            if (!inFocusMode) return;
            if (WheelObject == null) return;

            Vector2 primaryThumbstick = Vector2.zero;
            Vector2 secondaryThumbstick = Vector2.zero;

            // VR controller input
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
            // Ximput controller input
            if (XInput.IsConnected)
            {
                primaryThumbstick = XInput.Get(XInput.Axis.LThumbstick);
                secondaryThumbstick = XInput.Get(XInput.Axis.RThumbstick);
                Vector2 xboxPrimaryThumbstick = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                Vector2 xboxSecondaryThumbstick = new Vector2(Input.GetAxis("RightStickHorizontal"), Input.GetAxis("RightStickVertical"));
                float LIndexTrigger = XInput.Get(XInput.Trigger.LIndexTrigger);
                float RIndexTrigger = XInput.Get(XInput.Trigger.RIndexTrigger);
                // Combine VR and Xbox inputs
                primaryThumbstick += xboxPrimaryThumbstick;
                secondaryThumbstick += xboxSecondaryThumbstick;
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
        }
        private void MapButtons(ref bool inputDetected)
        {
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

        void ToggleLight(Light targetLight, bool isActive)
        {
            if (targetLight == null) return;

            // Ensure the GameObject itself is active
            if (targetLight.gameObject.activeSelf != isActive)
                targetLight.gameObject.SetActive(isActive);

            // Then toggle the component
            targetLight.enabled = isActive;
        }
        void DisableAllEmissives()
        {
            logger.Debug("Disabling all emissives at startup...");

            // List of all emissive GameObjects
            Transform[] emissiveObjects = new Transform[]
            {
            MA_DJ_Music_lampObject, MA_backnext_lampObject, MA_Check_Point_lampObject, MA_Race_Leader_lampObject, MB_DJ_Music_lampObject, MB_backnext_lampObject, MB_Check_Point_lampObject, MB_Race_Leader_lampObject
            };

            // Loop through each emissive object and disable its emission
            foreach (var emissiveObject in emissiveObjects)
            {
                if (emissiveObject != null)
                {
                    Renderer renderer = emissiveObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.DisableKeyword("_EMISSION");
                        logger.Debug($"{emissiveObject.name} emission disabled.");
                    }
                }
                else
                {
                    logger.Debug($"Emissive object {emissiveObject?.name} not found.");
                }
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
        void InitializeLights()
        {
            // Gets all Light components in the target object and its children
            Light[] lights = transform.GetComponentsInChildren<Light>(true);

            foreach (Light light in lights)
            {
                switch (light.gameObject.name)
                {
                    case "1_light":
                        p1_light = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    case "2_light":
                        p2_light = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    default:
                        logger.Debug($"{gameObject.name} Excluded Light found in object: " + light.gameObject.name);
                        break;
                }
            }
        }

        void InitializeObjects()
        {
            ShifterObject = transform.Find("Shifter");
            if (ShifterObject != null)
            {
                logger.Debug("Shifter object found.");
                ShifterStartPosition = ShifterObject.transform.localPosition;
                ShifterStartRotation = ShifterObject.transform.localRotation;
            }
            WheelObject = transform.Find("Wheel");
            if (WheelObject != null)
            {
                logger.Debug("Wheel object found.");
                WheelStartPosition = WheelObject.transform.localPosition;
                WheelStartRotation = WheelObject.transform.localRotation;
            }
            GasObject = transform.Find("Gas");
            if (GasObject != null)
            {
                logger.Debug("Gas object found.");
                GasStartPosition = GasObject.transform.localPosition;
                GasStartRotation = GasObject.transform.localRotation;
            }
            BrakeObject = transform.Find("Brake");
            if (BrakeObject != null)
            {
                logger.Debug("Brake object found.");
                BrakeStartPosition = BrakeObject.transform.localPosition;
                BrakeStartRotation = BrakeObject.transform.localRotation;
            }

            // Manually check every single emissive object
            MA_DJ_Music_lampObject = transform.Find("Emissives/MA_DJ_Music_lamp");
            if (MA_DJ_Music_lampObject != null) logger.Debug("MA_DJ_Music_lamp found.");
            else logger.Warning("MA_DJ_Music_lamp not found.");

            MA_backnext_lampObject = transform.Find("Emissives/MA_<<_>>_lamp");
            if (MA_backnext_lampObject != null) logger.Debug("MA_<<_>>_lamp found.");
            else logger.Warning("MA_<<_>>_lamp not found.");

            MA_Check_Point_lampObject = transform.Find("Emissives/MA_Check_Point_lamp");
            if (MA_Check_Point_lampObject != null) logger.Debug("MA_Check_Point_lamp found.");
            else logger.Warning("MA_Check_Point_lamp not found.");

            MA_Race_Leader_lampObject = transform.Find("Emissives/MA_Race_Leader_lamp");
            if (MA_Race_Leader_lampObject != null) logger.Debug("MA_Race_Leader_lamp found.");
            else logger.Warning("MA_Race_Leader_lamp not found.");

            MB_DJ_Music_lampObject = transform.Find("Emissives/MB_DJ_Music_lamp");
            if (MB_DJ_Music_lampObject != null) logger.Debug("MB_DJ_Music_lamp found.");
            else logger.Warning("MB_DJ_Music_lamp not found.");

            MB_backnext_lampObject = transform.Find("Emissives/MB_<<_>>_lamp");
            if (MB_backnext_lampObject != null) logger.Debug("MB_<<_>>_lamp found.");
            else logger.Warning("MB_<<_>>_lamp not found.");

            MB_Check_Point_lampObject = transform.Find("Emissives/MB_Check_Point_lamp");
            if (MB_Check_Point_lampObject != null) logger.Debug("MB_Check_Point_lamp found.");
            else logger.Warning("MB_Check_Point_lamp not found.");

            MB_Race_Leader_lampObject = transform.Find("Emissives/MB_Race_Leader_lamp");
            if (MB_Race_Leader_lampObject != null) logger.Debug("MB_Race_Leader_lamp found.");
            else logger.Warning("MB_Race_Leader_lamp not found.");
        }
    }
}
