using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using WIGUx.Modules.MameHookModule;

namespace WIGUx.Modules.manxttMotionSim
{
    public class manxttMotionSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        [Header("Object Settings")]
        private Transform HandlebarObject; // Reference to the Wheel mirroring object
        private Transform StartObject; // Reference to the start button object
        private Transform XObject; // Reference to the main X object
        private Transform YObject; // Reference to the main Y object
        private Transform ZObject; // Reference to the main Z object
        private Transform Taillight1Object; // Reference to the Fire left light emissive
        private Transform Taillight2Object; // Reference to the Fire right light emissive
        private GameObject cockpitCam;    // Reference to the Desktop Camera
        private GameObject vrCam;    // Reference to the VR Camera  
        private GameObject playerCamera;   // Reference to the Player Camera
        private GameObject playerVRSetup;   // Reference to the VR Camera

        [Header("Input Settings")]
        public string primaryThumbstickHorizontal = "Horizontal"; // Input axis for primary thumbstick horizontal
        public string primaryThumbstickVertical = "Vertical"; // Input axis for primary thumbstick vertical
        public string secondaryThumbstickHorizontal = "RightStickHorizontal"; // Input axis for secondary thumbstick horizontal
        public string secondaryThumbstickVertical = "RightStickVertical"; // Input axis for secondary thumbstick forward/backward
        public string leftTrigger = "LIndexTrigger";
        public string rightTrigger = "RIndexTrigger";

        [Header("Velocity Multiplier Settings")]        // Speeds for the animation of the in game flight stick or Handlebar
        private float primaryThumbstickRotationMultiplier = 10f; // Multiplier for primary thumbstick rotation intensity
        private float secondaryThumbstickRotationMultiplier = 25f; // Multiplier for secondary thumbstick rotation intensity
        private float triggerRotationMultiplier = 20f; // Multiplier for trigger rotation intensity
        private float adjustSpeed = 1.0f;  // Adjust this adjustment speed as needed a lower number will lead to smaller adustments
        private float HandlebarRotationDegrees = 30f; // Degrees for Stick rotation, adjust as needed
        private readonly float rotationSmoothness = 5f;  //sets the smoothness of the rotation
        private readonly float thumbstickVelocity = 50f;  // Velocity for keyboard input
        private readonly float centeringVelocityX = 55.5f;  // Velocity for centering rotation
        private readonly float centeringVelocityY = 55.5f;  // Velocity for centering rotation
        private readonly float centeringVelocityZ = 55.5f;  // Velocity for centering rotation

        [Header("Rotation Tracking")]        // Sets up the rotation varibles and sets them to 0 
        private float currentRotationX = 0f;  // Current rotation for X-axis
        private float currentRotationY = 0f;  // Current rotation for Y-axis
        private float currentRotationZ = 0f;  // Current rotation for Z-axis

        [Header("Rotation Limits")]        // Rotation Limits 
        [SerializeField] float minRotationX = 0f;
        [SerializeField] float maxRotationX = 0f;
        [SerializeField] float minRotationY = -10f;
        [SerializeField] float maxRotationY = 10f;
        [SerializeField] float minRotationZ = -30f;
        [SerializeField] float maxRotationZ = 30f;

        [Header("Position Settings")]     // Initial positions setup
        private Vector3 XStartPosition;  // Initial X position for resetting
        private Vector3 YStartPosition;  // Initial Y positions for resetting
        private Vector3 ZStartPosition;  // Initial Z positions for resetting
        private Vector3 HandlebarStartPosition; // Initial Handlebar positions for resetting
        private Vector3 playerCameraStartPosition;  // Initial Player Camera positions for resetting
        private Vector3 playerVRSetupStartPosition;  // Initial PlayerVR positions for resetting
        private Vector3 cockpitCamStartPosition;  // Initial cockpitCam positionsfor resetting
        private Vector3 vrCamStartPosition;    // Initial vrCam positionsfor resetting

        [Header("Rotation Settings")]     // Initial rotations setup
        private Quaternion XStartRotation;  // Initial X rotation for resetting
        private Quaternion YStartRotation;  // Initial Y rotation for resetting
        private Quaternion ZStartRotation;  // Initial Z rotation for resetting
        private Quaternion HandlebarStartRotation;  // Initial Handlebar rotation for resetting
        private Quaternion playerCameraStartRotation;  // Initial Player Camera rotation for resetting
        private Quaternion playerVRSetupStartRotation;  // Initial PlayerVR rotation for resetting
        private Quaternion cockpitCamStartRotation;  // Initial cockpitCam rotation for resetting
        private Quaternion vrCamStartRotation;      // Initial VRCam rotation for resetting

        [Header("Lights and Emissives")]     // Setup Emissive and Lights
        public Light brakelight1;
        public Light brakelight2;
        private Transform hazardlObject;
        private Transform hazardrObject;
        private float flashDuration = 0.01f;
        private float flashInterval = 0.05f;
        private Renderer[] hazzardEmissiveObjects;
        private Coroutine hazardsCoroutine; // Coroutine variable to control the strobe flashing
        private float lightDuration = 0.35f;
        private float attractFlashDuration = 0.7f;
        private float attractFlashDelay = 0.7f;
        private float dangerFlashDuration = 0.3f;
        private float dangerFlashDelay = 0.3f;
        private Coroutine DangerCoroutine; // Coroutine variable to control the focused danger mode
        private Coroutine AttractCoroutine; // Coroutine variable to control the attract mode
        private Light[] lights;        //array of lights
        Dictionary<string, int> lastLampStates = new Dictionary<string, int>
             {
               { "lamp0", 0 }, { "lamp1", 0 }, { "lamp2", 0 }, { "lamp3", 0 }
             };

        [Header("Timers and States")]  // Store last states and timers
        private bool inFocusMode = false;  // Flag to track focus mode state
        private bool isCenteringRotation = false; // Flag to track centering rotation state
        private bool isRiding = false; // Set riding state to false
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
            if (brakelight1) ToggleLight(brakelight1, false);
            if (brakelight2) ToggleLight(brakelight2, false);
            if (Taillight1Object) ToggleEmissive(Taillight1Object.gameObject, false);
            if (Taillight2Object) ToggleEmissive(Taillight2Object.gameObject, false);
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
            if (isCenteringRotation && !throttleDetected && !inputDetected)
            {
                bool centeredX = false, centeredY = false, centeredZ = false;

                // X axis
                float angleX = Quaternion.Angle(XObject.localRotation, XStartRotation);
                if (angleX > 0.01f)
                {
                    XObject.localRotation = Quaternion.RotateTowards(
                        XObject.localRotation,
                        XStartRotation,
                        centeringVelocityX * Time.deltaTime);
                    currentRotationX = Mathf.MoveTowards(
                        currentRotationX, 0f, centeringVelocityX * Time.deltaTime);
                }
                else
                {
                    XObject.localRotation = XStartRotation;
                    currentRotationX = 0f;
                    centeredX = true;
                }

                // Y axis
                float angleY = Quaternion.Angle(YObject.localRotation, YStartRotation);
                if (angleY > 0.01f)
                {
                    YObject.localRotation = Quaternion.RotateTowards(
                        YObject.localRotation,
                        YStartRotation,
                        centeringVelocityY * Time.deltaTime);
                    currentRotationY = Mathf.MoveTowards(
                        currentRotationY, 0f, centeringVelocityY * Time.deltaTime);
                }
                else
                {
                    YObject.localRotation = YStartRotation;
                    currentRotationY = 0f;
                    centeredY = true;
                }

                // Z axis
                float angleZ = Quaternion.Angle(ZObject.localRotation, ZStartRotation);
                if (angleZ > 0.01f)
                {
                    ZObject.localRotation = Quaternion.RotateTowards(
                        ZObject.localRotation,
                        ZStartRotation,
                        centeringVelocityZ * Time.deltaTime);
                    currentRotationZ = Mathf.MoveTowards(
                        currentRotationZ, 0f, centeringVelocityZ * Time.deltaTime);
                }
                else
                {
                    ZObject.localRotation = ZStartRotation;
                    currentRotationZ = 0f;
                    centeredZ = true;
                }

                if (centeredX && centeredY && centeredZ)
                {
                    isCenteringRotation = false;
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
                HandleTransformAdjustment();
            }
        }

        void ReadData()
        {
            //no data for this yet

        }
        void StartFocusMode()
        {
            logger.Debug($"{gameObject.name} Starting Focus Mode...");
            StopCurrentPatterns();
            if (cockpitCam != null)
            {
                cockpitCam.transform.localPosition = cockpitCamStartPosition;
                cockpitCam.transform.localRotation = cockpitCamStartRotation;
            }
            if (vrCam != null)
            {
                vrCam.transform.localPosition = vrCamStartPosition;
                vrCam.transform.localRotation = vrCamStartRotation;
            }
            if (playerCamera != null)
            {
                playerCameraStartPosition = playerCamera.transform.position;
                playerCameraStartRotation = playerCamera.transform.rotation;
            }

            if (playerVRSetup != null)
            {
                playerVRSetupStartPosition = playerVRSetup.transform.position;
                playerVRSetupStartRotation = playerVRSetup.transform.rotation;
            }
            // Check containment
            bool inside = false;
            if (cockpitCollider != null)
            {
                Vector3 camPos = playerCamera.transform.position;
                bool boundsContains = cockpitCollider.bounds.Contains(camPos);
                Vector3 closest = cockpitCollider.ClosestPoint(camPos);
                inside = (closest == camPos);
                //  logger.Debug($"Containment check - bounds.Contains: {boundsContains}, ClosestPoint==pos: {inside}");
            }

            if (cockpitCollider != null && inside)
            {
                if (playerVRSetup == null)
                {
                    // Parent and apply offset to PlayerCamera
                    SaveOriginalParent(playerCamera);
                    playerCamera.transform.SetParent(cockpitCam.transform, true);
                    logger.Debug($"{gameObject.name} Player is aboard and strapped in.");
                    isRiding = true; // Set riding state to true
                }
                if (playerVRSetup != null)
                {
                    // Parent and apply offset to PlayerVRSetup
                    SaveOriginalParent(playerVRSetup);
                    playerVRSetup.transform.SetParent(vrCam.transform, true);
                    logger.Debug($"{gameObject.name} VR Player is aboard and strapped in!");
                    logger.Debug("Manx TT Super Bike Sim starting...Feel real power between your legs!");
                    logger.Debug("Dont Fall Off!...");
                    isRiding = true; // Set riding state to true
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} Player is not aboard the ride, Starting Without the Player aboard.");
            }
            inFocusMode = true;  // Set focus mode flag
        }


        void EndFocusMode()
        {
            logger.Debug($"{gameObject.name} Exiting Focus Mode...");
            RestoreOriginalParent(playerCamera, "PlayerCamera");
            RestoreOriginalParent(playerVRSetup, "PlayerVRSetup.PlayerRig");
            //StartAttractPattern();
            ResetPositions();
            inFocusMode = false;  // Clear focus mode flag
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

            // X ROTATION (Pitch, up/down on stick, XObject)
            if (primaryThumbstick.y != 0f)
            {
                float inputValue = -primaryThumbstick.y * thumbstickVelocity * Time.deltaTime;
                float targetX = Mathf.Clamp(currentRotationX + inputValue, minRotationX, maxRotationX);
                float rotateX = targetX - currentRotationX;
                if (Mathf.Abs(rotateX) > 0.0001f)
                {
                    XObject.Rotate(rotateX, 0f, 0f);
                    currentRotationX = targetX;
                    inputDetected = true; 
 isCenteringRotation = false;
                }
            }

            // Y ROTATION (Yaw, left/right on stick, YObject)
            if (primaryThumbstick.x != 0f)
            {
                float inputValue = primaryThumbstick.x * thumbstickVelocity * Time.deltaTime;
                float targetY = Mathf.Clamp(currentRotationY + inputValue, minRotationY, maxRotationY);
                float rotateY = targetY - currentRotationY;
                if (Mathf.Abs(rotateY) > 0.0001f)
                {
                    YObject.Rotate(0f, rotateY, 0f);
                    currentRotationY = targetY;
                    inputDetected = true; 
 isCenteringRotation = false;
                }
            }

            // Z ROTATION (Roll, e.g., left/right on primary stick, ZObject)
            if (primaryThumbstick.x != 0f)
            {
                float inputValue = primaryThumbstick.x * thumbstickVelocity * Time.deltaTime;
                float targetZ = Mathf.Clamp(currentRotationZ + inputValue, minRotationZ, maxRotationZ);
                float rotateZ = targetZ - currentRotationZ;
                if (Mathf.Abs(rotateZ) > 0.0001f)
                {
                    ZObject.Rotate(0f, 0f, rotateZ);
                    currentRotationZ = targetZ;
                    inputDetected = true; 
 isCenteringRotation = false;
                }
            }
            if (!inputDetected)
            {
                CenterRotation();    // Center the rotation if no input is detected
            }
            if (!throttleDetected)
            {
                CenterThrottle();    // Center the rotation if no throttle input is detected
            }
        }

        private void MapButtons(ref bool inputDetected, ref bool throttleDetected)
        {
            if (!inFocusMode) return;

            // Fire2 - brake light & emissive on/off (example)
            if (Input.GetButtonDown("Fire2"))
            {
                if (brakelight1) ToggleLight(brakelight1, true);
                if (brakelight2) ToggleLight(brakelight2, true);
                if (Taillight1Object) ToggleEmissive(Taillight1Object.gameObject, true);
                if (Taillight2Object) ToggleEmissive(Taillight2Object.gameObject, true);
                inputDetected = true; 
 isCenteringRotation = false;
            }
            if (Input.GetButtonUp("Fire2"))
            {
                if (brakelight1) ToggleLight(brakelight1, false);
                if (brakelight2) ToggleLight(brakelight2, false);
                if (Taillight1Object) ToggleEmissive(Taillight1Object.gameObject, false);
                if (Taillight2Object) ToggleEmissive(Taillight2Object.gameObject, false);
                inputDetected = true; 
 isCenteringRotation = false;
            }
        }

        void ResetPositions()      // Reset objects and cockpit cam to initial position and rotation
        {
            logger.Debug($"{gameObject.name} Resetting Positions");

            // Reset X to initial positions and rotations
            if (XObject != null)
            {
                XObject.localPosition = XStartPosition;
                XObject.localRotation = XStartRotation;
            }

            // Reset Y object to initial position and rotation
            if (YObject != null)
            {
                YObject.localPosition = YStartPosition;
                YObject.localRotation = YStartRotation;
            }
            // Reset Z object to initial position and rotation
            if (ZObject != null)
            {
                ZObject.localPosition = ZStartPosition;
                ZObject.localRotation = ZStartRotation;
            }
            if (HandlebarObject != null)
            {
                HandlebarObject.localPosition = HandlebarStartPosition;
                HandlebarObject.localRotation = HandlebarStartRotation;
            }
            if (isRiding == true)
            {
                if (cockpitCam != null)
                {
                    cockpitCam.transform.localPosition = cockpitCamStartPosition;
                    cockpitCam.transform.localRotation = cockpitCamStartRotation;
                }
                if (vrCam != null)
                {
                    vrCam.transform.localPosition = vrCamStartPosition;
                    vrCam.transform.localRotation = vrCamStartRotation;
                }
                if (playerVRSetup != null)
                {
                    playerVRSetup.transform.position = playerVRSetupStartPosition;
                    playerVRSetup.transform.rotation = playerVRSetupStartRotation;
                }
                if (playerCamera != null)
                {
                    playerCamera.transform.position = playerCameraStartPosition;
                    playerCamera.transform.rotation = playerCameraStartRotation;
                }
                isRiding = false; // Set riding state to false
            }
            else
            {
                logger.Debug($"{gameObject.name} Player was not aboard the ride, skipping reset.");
            }

            // Reset rotation allowances and current rotation values
            currentRotationX = 0f;
            currentRotationY = 0f;
            currentRotationZ = 0f;
        }
        void CenterRotation()
        {
            isCenteringRotation = true;
        }
        void CenterThrottle()
        {
            isCenteringRotation = true;
        }

		void HandleTransformAdjustment()
		{
			if (!inFocusMode) return;
			// Choose target camera: use vrCam if available, otherwise fallback to cockpitCam
			var cam = vrCam != null ? vrCam : cockpitCam;

			if (cam != null && isRiding)
			{
                // Handle position adjustments
                if (Input.GetKey(KeyCode.Home))
                {
                    // Move forward
                    cam.transform.localPosition += cam.transform.forward * adjustSpeed * Time.deltaTime;
                }
                if (Input.GetKey(KeyCode.End))
                {
                    // Move backward
                    cam.transform.localPosition -= cam.transform.forward * adjustSpeed * Time.deltaTime;
                }
                if (Input.GetKey(KeyCode.UpArrow))
                {
                    // Move up
                    cam.transform.localPosition += cam.transform.up * adjustSpeed * Time.deltaTime;
                }
                if (Input.GetKey(KeyCode.DownArrow))
                {
                    // Move down
                    cam.transform.localPosition -= cam.transform.up * adjustSpeed * Time.deltaTime;
                }
                if (Input.GetKey(KeyCode.LeftArrow))
                {
                    // Move left
                    cam.transform.localPosition -= cam.transform.right * adjustSpeed * Time.deltaTime;
                }
                if (Input.GetKey(KeyCode.RightArrow))
                {
                    // Move right
                    cam.transform.localPosition += cam.transform.right * adjustSpeed * Time.deltaTime;
                }

                // Handle rotation with Backspace key
                if (Input.GetKeyDown(KeyCode.Backspace))
                {
                    cam.transform.Rotate(0, 90, 0);
                }
            }

            // Save the new position and rotation
            if (vrCam != null)
            {
                vrCamStartPosition = vrCam.transform.localPosition;
                vrCamStartRotation = vrCam.transform.localRotation;
            }
            else if (cockpitCam != null)
            {
                cockpitCamStartPosition = cockpitCam.transform.localPosition;
                cockpitCamStartRotation = cockpitCam.transform.localRotation;
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

        // Save original parent of object in dictionary
        void SaveOriginalParent(GameObject obj)
        {
            if (obj != null && !originalParents.ContainsKey(obj))
            {
                originalParents[obj] = obj.transform.parent;
            }
        }

        // Restore original parent of object and log appropriate message
        void RestoreOriginalParent(GameObject obj, string name)
        {
            if (obj != null && originalParents.ContainsKey(obj))
            {
                obj.transform.SetParent(originalParents[obj]);
                logger.Debug($"{gameObject.name} {name} restored to original parent.");
            }
        }
        void StartAttractPattern()
        {
            // Stop any currently running coroutines
            StopCurrentPatterns();
            AttractCoroutine = StartCoroutine(AttractPattern());
        }
        void StartDangerPattern()
        {
            // Stop any currently running coroutines
            StopCurrentPatterns();
            DangerCoroutine = StartCoroutine(DangerPattern());
        }
        void StopCurrentPatterns()
        {
            if (AttractCoroutine != null)
            {
                StopCoroutine(AttractCoroutine);
                AttractCoroutine = null;
            }
            if (DangerCoroutine != null)
            {
                StopCoroutine(DangerCoroutine);
                DangerCoroutine = null;
            }
        }
        IEnumerator AttractPattern()  //Pattern For Attract Mode
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 1f));
            while (true)
            {
                if (brakelight1) ToggleLight(brakelight1, true);
                if (brakelight2) ToggleLight(brakelight2, true);
                if (Taillight1Object) ToggleEmissive(Taillight1Object.gameObject, true);
                if (Taillight2Object) ToggleEmissive(Taillight2Object.gameObject, true);
                yield return new WaitForSeconds(attractFlashDuration);
                if (brakelight1) ToggleLight(brakelight1, false);
                if (brakelight2) ToggleLight(brakelight2, false);
                if (Taillight1Object) ToggleEmissive(Taillight1Object.gameObject, false);
                if (Taillight2Object) ToggleEmissive(Taillight2Object.gameObject, false);
                yield return new WaitForSeconds(attractFlashDelay);
            }
        }
        IEnumerator DangerPattern() //Pattern For Focused Danger Mode
        {
            while (true)
            {
                if (brakelight1) ToggleLight(brakelight1, true);
                if (brakelight2) ToggleLight(brakelight2, true);
                if (Taillight1Object) ToggleEmissive(Taillight1Object.gameObject, true);
                if (Taillight2Object) ToggleEmissive(Taillight2Object.gameObject, true);
                yield return new WaitForSeconds(dangerFlashDuration);
                if (brakelight1) ToggleLight(brakelight1, false);
                if (brakelight2) ToggleLight(brakelight2, false);
                if (Taillight1Object) ToggleEmissive(Taillight1Object.gameObject, false);
                if (Taillight2Object) ToggleEmissive(Taillight2Object.gameObject, false);
                yield return new WaitForSeconds(dangerFlashDelay);
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
                        brakelight1 = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    case "brake2light":
                        brakelight2 = light;
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
            // Find references to PlayerCamera and VR setup objects
            playerCamera = PlayerVRSetup.PlayerCamera.gameObject;

            // Find and assign the whole VR rig try SteamVR first, then Oculus
            playerVRSetup = GameObject.Find("Player/[SteamVRCameraRig]");
            // If not found, try to find the Oculus VR rig
            if (playerVRSetup == null)
            {
                playerVRSetup = GameObject.Find("OVRCameraRig");
            }

            // Check if objects are found
            CheckObject(playerCamera, "PlayerCamera");
            if (playerVRSetup != null)
            {
                CheckObject(playerVRSetup, playerVRSetup.name); // will print either [SteamVRCameraRig] or OVRCameraRig
            }
            else
            {
                logger.Debug($"{gameObject.name} No VR Devices found. No SteamVR or OVR present)");
            }

            // Find X object in hierarchy
            XObject = transform.Find("X");
            if (XObject != null)
            {
                logger.Debug($"{gameObject.name} X object found.");
                XStartPosition = XObject.localPosition;
                XStartRotation = XObject.localRotation;

                // Find Y object under X
                YObject = XObject.Find("Y");
                if (YObject != null)
                {
                    logger.Debug($"{gameObject.name} Y object found.");
                    YStartPosition = YObject.localPosition;
                    YStartRotation = YObject.localRotation;

                    // Find Z object under Y
                    ZObject = YObject.Find("Z");
                    if (ZObject != null)
                    {
                        logger.Debug($"{gameObject.name} Z object found.");
                        ZStartPosition = ZObject.localPosition;
                        ZStartRotation = ZObject.localRotation;

                        // Find cockpit camera
                        cockpitCam = ZObject.Find("eyes/cockpitcam")?.gameObject;
                        if (cockpitCam != null)
                        {
                            logger.Debug($"{gameObject.name} Cockpitcam object found.");
                            cockpitCamStartPosition = cockpitCam.transform.localPosition;
                            cockpitCamStartRotation = cockpitCam.transform.localRotation;
                        }
                        else
                        {
                            logger.Debug($"{gameObject.name} cockpitCam object not found.");
                        }

                        // Find vr camera
                        vrCam = ZObject.Find("eyes/vrcam")?.gameObject;
                        if (vrCam != null)
                        {
                            logger.Debug($"{gameObject.name} vrCam object found.");
                            vrCamStartPosition = vrCam.transform.localPosition;
                            vrCamStartRotation = vrCam.transform.localRotation;
                        }
                        else
                        {
                            logger.Debug($"{gameObject.name} vrCam object not found.");
                        }
                        // Find StartObject object under Z
                        StartObject = ZObject.Find("Start");
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
                        // Find Handlebar under Z
                        HandlebarObject = ZObject.Find("Handlebar");
                        if (HandlebarObject != null)
                        {
                            logger.Debug($"{gameObject.name} Handlebar object found.");
                            HandlebarStartPosition = HandlebarObject.localPosition;
                            HandlebarStartRotation = HandlebarObject.localRotation;
                        }
                        else
                        {
                            logger.Debug($"{gameObject.name} Handlebar object not found.");
                        }
                        // Find Taillight1Object object under Z
                        Taillight1Object = ZObject.Find("Taillight1");
                        if (Taillight1Object != null)
                        {
                            logger.Debug($"{gameObject.name} Taillight1 object found.");
                            // Ensure the Taillight1 object is initially off
                            Renderer renderer = Taillight1Object.GetComponent<Renderer>();
                            if (renderer != null)
                            {
                                renderer.material.DisableKeyword("_EMISSION");
                            }
                        }
                        else
                        {
                            logger.Debug($"{gameObject.name} Taillight1 object not found.");
                        }
                        // Find Taillight2Object object under Z
                        Taillight2Object = ZObject.Find("Taillight2");
                        if (Taillight2Object != null)
                        {
                            logger.Debug($"{gameObject.name} Taillight2 object found.");
                            // Ensure the Fire2 object is initially off
                            Renderer renderer = Taillight2Object.GetComponent<Renderer>();
                            if (renderer != null)
                            {
                                renderer.material.DisableKeyword("_EMISSION");
                            }
                        }
                        else
                        {
                            logger.Debug($"{gameObject.name} Taillight2 object not found.");
                        }
                    }
                    else
                    {
                        logger.Debug($"{gameObject.name} Z object not found.");
                    }
                }
                else
                {
                    logger.Debug($"{gameObject.name} Y object not found.");
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} X object not found.");
            }

            // Attempt to find cockpitCollider by name
            if (cockpitCollider == null)
            {
                Collider[] colliders = GetComponentsInChildren<Collider>(true); // true = include inactive
                foreach (var col in colliders)
                {
                    if (col.gameObject.name == "Cockpit")
                    {
                        cockpitCollider = col;
                        logger.Debug($"{gameObject.name} cockpitCollider found in children: {cockpitCollider.name}");
                        break;
                    }
                }
            }
        }
    }
}