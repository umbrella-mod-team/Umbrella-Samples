using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using WIGUx.Modules.MameHookModule;

namespace WIGUx.Modules.crusinblastSimModule
{
    public class crusinblastSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        [Header("Object Settings")]
        private Transform WheelObject; // Reference to the throttle mirroring object
        private Transform ShifterObject; // Reference to the throttle mirroring object
        private Transform GasObject; // Reference to the throttle mirroring object
        private Transform BrakeObject; // Reference to the throttle mirroring object
        private Transform StartObject; // Reference to the start button object
        private Transform BrakeLight1Object; // Reference to the Fire left light emissive
        private Transform BrakeLight2Object; // Reference to the Fire left light emissive
        private Transform RearLightObject; // Reference to the Fire right light emissive
        private Transform Danger1Object; // Reference to the Danger1 light emissive
        private Transform Danger2Object; // Reference to the Danger2 light emissive
        private Transform Danger3Object; // Reference to the Danger1 light emissive
        private Transform Danger4Object; // Reference to the Danger2 light emissive

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
        private Light brake1light;
        private Light brake2light;
        private Light danger1light;
        private Light danger2light;
        private Light danger3light;
        private Light danger4light;
        private float lightDuration = 0.35f;
        private float attractFlashDuration = 0.7f;
        private float attractFlashDelay = 0.7f;
        private float dangerFlashDuration = 0.3f;
        private float dangerFlashDelay = 0.3f;
        private Coroutine dangerCoroutine; // Coroutine variable to control the focused danger mode
        private Coroutine attractCoroutine; // Coroutine variable to control the attract mode
        private Light[] lights;        //array of lights
        public Material wheelLightsMat;
        private float speed = 1f; // moves per second

        [Header("Timers and States")]  // Store last states and timers
        private bool isflashing = false; //set the flashing lights flag
        private bool isHigh = false; //set the high gear flag
        private GameSystemState systemState;

        Dictionary<string, int> lastLampStates = new Dictionary<string, int>
             {
               { "lamp0", 0 }, { "lamp1", 0 }, { "lamp2", 0 }, { "lamp3", 0 }
             };

        [Header("Collider Triggers")]
        [SerializeField] private Collider cockpitCollider;

        [Header("Rom Check")]
        private bool inFocusMode = false;  // Flag to track focus mode state
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
            InitializeLights();
            InitializeObjects();
            gameSystem = GetComponent<GameSystem>();
            if (brake1light) ToggleLight(brake1light, false);
            if (brake2light) ToggleLight(brake2light, false);
          //  if (StartObject) ToggleEmissive(StartObject.gameObject, false);
            if (BrakeLight1Object) ToggleEmissive(BrakeLight1Object.gameObject, false);
            if (BrakeLight2Object) ToggleEmissive(BrakeLight2Object.gameObject, false);
            if (RearLightObject) ToggleEmissive(RearLightObject.gameObject, false);
            if (Danger1Object) ToggleEmissive(Danger1Object.gameObject, false);
            if (Danger2Object) ToggleEmissive(Danger2Object.gameObject, false);
            if (Danger3Object) ToggleEmissive(Danger3Object.gameObject, false);
            if (Danger4Object) ToggleEmissive(Danger4Object.gameObject, false);
            if (danger1light) ToggleLight(danger1light, false);
            if (danger2light) ToggleLight(danger2light, false);
            if (danger3light) ToggleLight(danger3light, false);
            if (danger4light) ToggleLight(danger4light, false);
            StartAttractPattern();
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
                MapButtons(ref inputDetected, ref throttleDetected);
                // Animate the slice angle for your Custom/CruisnWheel shader
                float angle = Mathf.Repeat(720f - (Time.time * speed * 720f), 720f);
                wheelLightsMat.SetFloat("_SliceAngle", angle);
            }
            if (!inFocusMode)
            {
                // Animate the slice angle for your Custom/CruisnWheel shader
                float angle = Mathf.Repeat(360f - (Time.time * speed * 360f), 360f);
                wheelLightsMat.SetFloat("_SliceAngle", angle);
            }
        }

        void StartFocusMode()
        {
            logger.Debug($"{gameObject.name} Starting Focus Mode...");
            if (brake1light) ToggleLight(brake1light, false);
            if (brake2light) ToggleLight(brake2light, false);
         //   if (StartObject) ToggleEmissive(StartObject.gameObject, false);
            if (BrakeLight1Object) ToggleEmissive(BrakeLight1Object.gameObject, false);
            if (BrakeLight2Object) ToggleEmissive(BrakeLight2Object.gameObject, false);
            if (RearLightObject) ToggleEmissive(RearLightObject.gameObject, true);
            if (Danger1Object) ToggleEmissive(Danger1Object.gameObject, false);
            if (Danger2Object) ToggleEmissive(Danger2Object.gameObject, false);
            if (Danger3Object) ToggleEmissive(Danger3Object.gameObject, false);
            if (Danger4Object) ToggleEmissive(Danger4Object.gameObject, false);
            if (danger1light) ToggleLight(danger1light, false);
            if (danger2light) ToggleLight(danger2light, false);
            if (danger3light) ToggleLight(danger3light, false);
            if (danger4light) ToggleLight(danger4light, false);
            StartDangerPattern();
            inFocusMode = true;  // Set focus mode flag
        }

        void EndFocusMode()
        {
            logger.Debug($"{gameObject.name} Exiting Focus Mode...");
            if (brake1light) ToggleLight(brake1light, false);
            if (brake2light) ToggleLight(brake2light, false);
           // if (StartObject) ToggleEmissive(StartObject.gameObject, true);
            if (BrakeLight1Object) ToggleEmissive(BrakeLight1Object.gameObject, false);
            if (BrakeLight2Object) ToggleEmissive(BrakeLight2Object.gameObject, false);
            if (RearLightObject) ToggleEmissive(RearLightObject.gameObject, false);
            if (Danger1Object) ToggleEmissive(Danger1Object.gameObject, false);
            if (Danger2Object) ToggleEmissive(Danger2Object.gameObject, false);
            if (Danger3Object) ToggleEmissive(Danger3Object.gameObject, false);
            if (Danger4Object) ToggleEmissive(Danger4Object.gameObject, false);
            if (danger1light) ToggleLight(danger1light, false);
            if (danger2light) ToggleLight(danger2light, false);
            if (danger3light) ToggleLight(danger3light, false);
            if (danger4light) ToggleLight(danger4light, false);
            StartAttractPattern();
            ResetPositions();
            inFocusMode = false;  // Clear focus mode flag
        }
        void ReadData()
        {
            // This method is a placeholder for reading data from the MAME hook.
            // You can implement your logic here to read and process the data.
            // For example, you might want to read lamp states or other game-specific data.
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

            // Reset rotation allowances and current rotation values
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

        private void MapButtons(ref bool inputDetected, ref bool throttleDetected) // Pass by reference
        {
            if (!inFocusMode) return;

            if (Input.GetButtonDown("Fire2") || XInput.GetDown(XInput.Button.B))    // press Fire3 turn on lights
            {
                if (brake1light) ToggleLight(brake1light, true);
                if (brake2light) ToggleLight(brake2light, true);
                if (BrakeLight1Object) ToggleEmissive(BrakeLight1Object.gameObject, true);
                if (BrakeLight2Object) ToggleEmissive(BrakeLight2Object.gameObject, true);
            }

            if (Input.GetButtonUp("Fire2") || XInput.GetUp(XInput.Button.B))     // release Fire3 turn off lights
            {
                if (brake1light) ToggleLight(brake1light, false);
                if (brake2light) ToggleLight(brake2light, false);
                if (BrakeLight1Object) ToggleEmissive(BrakeLight1Object.gameObject, false);
                if (BrakeLight2Object) ToggleEmissive(BrakeLight2Object.gameObject, false);
            }
            /*
            if (XInput.GetDown(XInput.Button.Y))
            {
                if (!isHigh)
                {
                    // Shift if not in high
                    ShifterObject.Rotate(0, 0, 45f);
                    isHigh = true;
                }
                else
                {
                    ShifterObject.Rotate(0, 0, -45f);
                    isHigh = false;
                }
            }
            */
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
        /*
public void ReadData()
{
    // 1) Your original “zeroed” lamp list:
    var currentLampStates = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
{
{ "lamp0", 0 }, { "lamp1", 0 }, { "lamp2", 0 }, { "lamp3", 0 }
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
                case "lamp2":
                    ProcessLamp2(newValue);
                    break;
                case "lamp3":
                    ProcessLamp3(newValue);
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
// Individual function for lamp0
void ProcessLamp0(int state)
{
    logger.Debug($"Lamp 0 updated: {state}");

    // Update lights
    if (locklight) ToggleLight(locklight, state == 1);
    // Update emissive material
    if (Fire2Object) ToggleEmissive(Fire2Object.gameObject, state == 1);
}
// Individual function for lamp1
void ProcessLamp1(int state)
{
    logger.Debug($"Lamp 1 updated: {state}");

    // Update lights

    // Update emissive material

}
// Individual function for lamp2
void ProcessLamp2(int state)
{
    logger.Debug($"Lamp 2 updated: {state}");

    // Update lights

    if (firelight1) ToggleLight(firelight1, state == 1);
    if (firelight2) ToggleLight(firelight2, state == 1);
    // Update emissive material
    if (Fire1Object) ToggleEmissive(Fire1Object.gameObject, state == 1);
}

// Individual function for lamp3
void ProcessLamp3(int state)
{
    logger.Debug($"Lamp 3 updated: {state}");

    // Update lights
    if (startlight) ToggleLight(startlight, state == 1);
    // Update emissive material
    if (StartObject) ToggleEmissive(StartObject.gameObject, state == 1);
}
*/


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

        void SaveOriginalParent(GameObject obj)
        {
            if (obj != null && !originalParents.ContainsKey(obj))
            {
                if (obj.transform.parent != null)
                {
                    originalParents[obj] = obj.transform.parent;
                }
                else
                {
                    originalParents[obj] = null; // Explicitly store that this was in the root
                    logger.Debug($"{gameObject.name} Object {obj.name} was in the root and has no parent.");
                }
            }
        }

        void RestoreOriginalParent(GameObject obj, string name)
        {
            if (obj == null)
            {
                logger.Error($"{gameObject.name} RestoreOriginalParent: {name} is NULL!");
                return;
            }

            if (!originalParents.ContainsKey(obj))
            {
                logger.Error($"{gameObject.name} RestoreOriginalParent: No original parent found for {name}");
                return;
            }

            Transform originalParent = originalParents[obj];

            // If the original parent was NULL, place the object back in the root
            if (originalParent == null)
            {
                obj.transform.SetParent(null, true);  // Moves it back to the root
                logger.Debug($"{gameObject.name} {name} restored to root.");
            }
            else
            {
                obj.transform.SetParent(originalParent, false);
                logger.Debug($"{gameObject.name} {name} restored to original parent: {originalParent.name}");
            }
        }

        void DisableEmission(Renderer[] emissiveObjects)        // Method to disable emission
        {
            foreach (var renderer in emissiveObjects)
            {
                if (renderer != null)
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
                else
                {
                    logger.Debug($"{gameObject.name} Renderer component not found on one of the emissive objects.");
                }
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

        void LogMissingObject(Renderer[] emissiveObjects, string arrayName)      // Method to log missing objects
        {
            for (int i = 0; i < emissiveObjects.Length; i++)
            {
                if (emissiveObjects[i] == null)
                {
                    logger.Debug($"{gameObject.name} {arrayName} object at index {i} not found under ControllerZ.");
                }
            }
        }
        IEnumerator AttractPattern()  //Pattern For Attract Mode
        {
			yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 1f));
			while (true)
            {
                //  if (StartObject) ToggleEmissive(StartObjectToggleEmissive, false);
                if (BrakeLight1Object) ToggleEmissive(BrakeLight1Object.gameObject, false);
                if (BrakeLight2Object) ToggleEmissive(BrakeLight2Object.gameObject, false);
                if (brake1light) ToggleLight(brake1light, false);
                if (brake2light) ToggleLight(brake2light, false);
                if (Danger1Object) ToggleEmissive(Danger1Object.gameObject, false);
                if (danger1light) ToggleLight(danger1light, false);
                if (Danger2Object) ToggleEmissive(Danger2Object.gameObject, false);
                if (danger2light) ToggleLight(danger2light, false);
                if (Danger3Object) ToggleEmissive(Danger3Object.gameObject, false);
                if (danger3light) ToggleLight(danger3light, false);
                if (Danger4Object) ToggleEmissive(Danger4Object.gameObject, false);
                if (danger4light) ToggleLight(danger4light, false);
                yield return new WaitForSeconds(attractFlashDuration);
                //   if (StartObject) ToggleEmissive(StartObject.gameObject, true);
                if (BrakeLight1Object) ToggleEmissive(BrakeLight1Object.gameObject, true);
                if (BrakeLight2Object) ToggleEmissive(BrakeLight2Object.gameObject, true);
                if (brake1light) ToggleLight(brake1light, true);
                if (brake2light) ToggleLight(brake2light, true);
                if (Danger1Object) ToggleEmissive(Danger1Object.gameObject, true);
                if (danger1light) ToggleLight(danger1light, true);
                if (Danger2Object) ToggleEmissive(Danger2Object.gameObject, true);
                if (danger2light) ToggleLight(danger2light, true);
                if (Danger3Object) ToggleEmissive(Danger3Object.gameObject, true);
                if (danger3light) ToggleLight(danger3light, true);
                if (Danger4Object) ToggleEmissive(Danger4Object.gameObject, true);
                if (danger4light) ToggleLight(danger4light, true);
                yield return new WaitForSeconds(attractFlashDelay);
            }
        }
        IEnumerator DangerPattern() //Pattern For Focused Danger Mode
        {
            while (true)
            {
                if (Danger1Object) ToggleEmissive(Danger1Object.gameObject, false);
                if (danger1light) ToggleLight(danger1light, false);
                if (Danger2Object) ToggleEmissive(Danger2Object.gameObject, true);
                if (danger2light) ToggleLight(danger2light, true);
                if (Danger3Object) ToggleEmissive(Danger3Object.gameObject, false);
                if (danger3light) ToggleLight(danger3light, false);
                if (Danger4Object) ToggleEmissive(Danger4Object.gameObject, true);
                if (danger4light) ToggleLight(danger4light, true);
                yield return new WaitForSeconds(dangerFlashDuration);
                if (Danger1Object) ToggleEmissive(Danger1Object.gameObject, true);
                if (danger1light) ToggleLight(danger1light, true);
                if (Danger2Object) ToggleEmissive(Danger2Object.gameObject, false);
                if (danger2light) ToggleLight(danger2light, false);
                if (Danger3Object) ToggleEmissive(Danger3Object.gameObject, true);
                if (danger3light) ToggleLight(danger3light, true);
                if (Danger4Object) ToggleEmissive(Danger4Object.gameObject, false);
                if (danger4light) ToggleLight(danger4light, false);
                yield return new WaitForSeconds(dangerFlashDelay);
            }
        }
        public void StartAttractPattern()
        {
            // Stop any currently running coroutines
            StopCurrentPatterns();
            attractCoroutine = StartCoroutine(AttractPattern());
            if (BrakeLight1Object) ToggleEmissive(BrakeLight1Object.gameObject, false);
            if (BrakeLight2Object) ToggleEmissive(BrakeLight2Object.gameObject, false);
            if (brake1light) ToggleLight(brake1light, false);
            if (brake2light) ToggleLight(brake2light, false);
            //if (StartObject) ToggleEmissive(StartObject.gameObject, true);
        }
        public void StartDangerPattern()
        {
            // Stop any currently running coroutines
            StopCurrentPatterns();
            dangerCoroutine = StartCoroutine(DangerPattern());
            if (BrakeLight1Object) ToggleEmissive(BrakeLight1Object.gameObject, false);
            if (BrakeLight2Object) ToggleEmissive(BrakeLight2Object.gameObject, false);
            if (brake1light) ToggleLight(brake1light, false);
            if (brake2light) ToggleLight(brake2light, false);
         //   if (StartObject) ToggleEmissive(StartObject.gameObject, false);
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
            Light[] lights = transform.GetComponentsInChildren<Light>(true);

            foreach (Light light in lights)
            {
                switch (light.gameObject.name)
                {
                    case "brake1light":
                        brake1light = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    case "brake2light":
                        brake2light = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    case "danger1light":
                        danger1light = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    case "danger2light":
                        danger2light = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    case "danger3light":
                        danger3light = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    case "danger4light":
                        danger4light = light;
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

            StartObject = transform.Find("Start");
            if (StartObject != null)
            {
                logger.Debug($"{gameObject.name} Start object found.");
                // Ensure the Start object is initially off
                Renderer renderer = StartObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} Start object not found.");
            }

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
            // Find "segalight" under the root of this GameObject
            Transform segalightObject = transform.Find("segalight");
            if (segalightObject == null)
            {
                logger.Debug("Could not find segalight object!");
                return;
            }

            Renderer segalightRenderer = segalightObject.GetComponent<Renderer>();
            if (segalightRenderer == null)
            {
                logger.Debug("segalight object does not have a Renderer!");
                return;
            }
            wheelLightsMat = segalightRenderer.material;
            if (wheelLightsMat == null)
            {
                logger.Debug("segalight Renderer does not have a material assigned!");
                return;
            }
            // Find RearLightObject
            RearLightObject = transform.Find("RearLight");
            if (RearLightObject != null)
            {
                logger.Debug($"{gameObject.name} RearLight object found.");
                // Ensure the RearLight object is initially off
                Renderer renderer = RearLightObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} RearLight object not found.");
            }
            // Find danger1Object
            Danger1Object = transform.Find("DangerLight1");
            if (Danger1Object != null)
            {
                logger.Debug($"{gameObject.name} DangerLight1 object found.");
                // Ensure the DangerLight1 object is initially off
                Renderer renderer = Danger1Object.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} DangerLight1 object not found.");
            }
            // Find danger2Object
            Danger2Object = transform.Find("DangerLight2");
            if (Danger2Object != null)
            {
                logger.Debug($"{gameObject.name} DangerLight2 object found.");
                // Ensure the DangerLight2 object is initially off
                Renderer renderer = Danger2Object.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} DangerLight2 object not found.");
            }
            // Find danger3Object
            Danger3Object = transform.Find("DangerLight3");
            if (Danger3Object != null)
            {
                logger.Debug($"{gameObject.name} DangerLight3 object found.");
                // Ensure the DangerLight3 object is initially off
                Renderer renderer = Danger3Object.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} DangerLight3 object not found.");
            }

            // Find danger2Object
            Danger4Object = transform.Find("DangerLight4");
            if (Danger4Object != null)
            {
                logger.Debug($"{gameObject.name} DangerLight4 object found.");
                // Ensure the DangerLight4 object is initially off
                Renderer renderer = Danger4Object.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} DangerLight4 object not found.");
            }
            // Find fireObject object
            BrakeLight1Object = transform.Find("BrakeLight1");
            if (BrakeLight1Object != null)
            {
                logger.Debug($"{gameObject.name} BrakeLight1 object found.");
                // Ensure the brake light object is initially off
                Renderer renderer = BrakeLight1Object.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} BrakeLight1 object not found.");
            }
            // Find fireObject object under Z
            BrakeLight2Object = transform.Find("BrakeLight2");
            if (BrakeLight2Object != null)
            {
                logger.Debug($"{gameObject.name} BrakeLight2 object found.");
                // Ensure the brake light object is initially off
                Renderer renderer = BrakeLight2Object.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} BrakeLight2 object not found.");
            }

        }

    }
}

