using UnityEngine;
using WIGU;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Xml.Linq;

namespace WIGUx.Modules.abcMotionSim
{
    public class abcMotionSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        [Header("Object Settings")]
        private Transform StartObject; // Reference to the start button object
        private Transform StickObject; // Reference to the stick mirroring object
        private Transform ThrottleObject; // Reference to the left stick mirroring object
        private Transform XObject; // Reference to the main X object
        private Transform YObject; // Reference to the main Y object
        private Transform ZObject; // Reference to the main Z object
        private Transform Fire1Object; // Reference to the fire left light
        private Transform Fire2Object; // Reference to the fire right light
        private Transform Thruster1Object; // Reference to the thruster1 light
        private Transform Thruster2Object; // Reference to the thruster2 light
        private Transform Danger1Object; // Reference to the danger1 light
        private Transform Danger2Object; // Reference to the danger2 light
        private GameObject cockpitCam;    // Reference to the cockpit camera
        private GameObject vrCam;    // Reference to the vr camera 
        private GameObject playerCamera;   // Reference to the player camera
        private GameObject playerVRSetup;   // Reference to the player 

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
        private float StickXRotationDegrees = 10f; // Degrees for Stick rotation, adjust as needed
        private float StickYRotationDegrees = 10f; // Degrees for Stick rotation, adjust as needed
        private readonly float rotationSmoothness = 5f;  //sets the smoothness of the rotation
        private readonly float thumbstickVelocity = 45f;  // Velocity for keyboard input
        private readonly float centeringVelocityX = 25f;  // Velocity for centering rotation
        private readonly float centeringVelocityY = 25f;  // Velocity for centering rotation
        private readonly float centeringVelocityZ = 25f;  // Velocity for centering rotation

        [Header("Rotation Tracking")]        // Sets up the rotation varibles and sets them to 0 
        private float currentRotationX = 0f;  // Current rotation for X-axis
        private float currentRotationY = 0f;  // Current rotation for Y-axis
        private float currentRotationZ = 0f;  // Current rotation for Z-axis

        [Header("Rotation Limits")]        // Rotation Limits 
        [SerializeField] float minRotationX = -25f;
        [SerializeField] float maxRotationX = 25f;
        [SerializeField] float minRotationY = 0f;
        [SerializeField] float maxRotationY = 0f;
        [SerializeField] float minRotationZ = 0f;
        [SerializeField] float maxRotationZ = 0f;

        [Header("Position Settings")]     // Initial positions setup
        private Vector3 XStartPosition;  // Initial X position for resetting
        private Vector3 YStartPosition;  // Initial Y positions for resetting
        private Vector3 ZStartPosition;  // Initial Z positions for resetting
        private Vector3 StickStartPosition; // Initial Throttle positions for resetting
        private Vector3 ThrottleStartPosition; // Initial Throttle positions for resetting
        private Vector3 playerCameraStartPosition;  // Initial Player Camera positions for resetting
        private Vector3 playerVRSetupStartPosition;  // Initial PlayerVR positions for resetting
        private Vector3 cockpitCamStartPosition;  // Initial cockpitCam positionsfor resetting
        private Vector3 vrCamStartPosition;    // Initial vrCam positionsfor resetting

        [Header("Rotation Settings")]     // Initial rotations setup
        private Quaternion XStartRotation;  // Initial X rotation for resetting
        private Quaternion YStartRotation;  // Initial Y rotation for resetting
        private Quaternion ZStartRotation;  // Initial Z rotation for resetting
        private Quaternion StickStartRotation;  // Initial Stick rotation for resetting
        private Quaternion ThrottleStartRotation;  // Initial Throttle rotation for resetting
        private Quaternion playerCameraStartRotation;  // Initial Player Camera rotation for resetting
        private Quaternion playerVRSetupStartRotation;  // Initial PlayerVR rotation for resetting
        private Quaternion cockpitCamStartRotation;  // Initial cockpitCam rotation for resetting
        private Quaternion vrCamStartRotation;      // Initial VRCam rotation for resetting

        [Header("Lights and Emissives")]     // Setup Emissive and Lights
        private Light firelight1;
        private Light firelight2;
        private float lightDuration = 0.35f;
        private float attractFlashDuration = 0.7f;
        private float attractFlashDelay = 0.7f;
        private float dangerFlashDuration = 0.3f;
        private float dangerFlashDelay = 0.3f;
        private Coroutine dangerCoroutine; // Coroutine variable to control the focused danger mode
        private Coroutine attractCoroutine; // Coroutine variable to control the attract mode
        private Light[] lights;        //array of lights
        Dictionary<Transform, bool> lastEmissiveStates = new Dictionary<Transform, bool>();// Track last known emissive states to prevent unnecessary updates

        [Header("Collider Triggers")]
        [SerializeField] private Collider cockpitCollider;

        [Header("Timers and States")]  // Store last states and timers
        private bool inFocusMode = false;  // Flag to track focus mode state
        private bool isCenteringRotation = false; // Flag to track centering rotation state
        private bool isRiding = false; // Set riding state to false
        private GameSystemState systemState;

        [Header("Rom Check")]     // Check for compatible titles
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
            StartAttractPattern();
            if (firelight1) ToggleLight(firelight1, false);
            if (firelight2) ToggleLight(firelight2, false);
            if (Fire1Object) ToggleEmissive(Fire1Object.gameObject, false);
            if (Fire2Object) ToggleEmissive(Fire2Object.gameObject, false);
            if (Danger1Object) ToggleEmissive(Danger1Object.gameObject, false);
            if (Danger2Object) ToggleEmissive(Danger2Object.gameObject, false);
            if (Thruster1Object) ToggleEmissive(Thruster1Object.gameObject, false);
            if (Thruster2Object) ToggleEmissive(Thruster2Object.gameObject, false);
        }

        void Update()
        {
			// ReadData();
			bool inputDetected = false;  // Initialize for centering
			bool throttleDetected = false;// Initialize for centering
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
            CheckInsertedGameName();
            CheckControlledGameName();
        }
        void WriteLampConfig(string filePath)
        {
            string content = "start_lamp = 0\n" +
                             "danger_lamp = 0\n" +
                             "crash_lamp = 0\n" +
                             "estop_lamp = 0\n";

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
            if (Fire1Object) ToggleEmissive(Fire1Object.gameObject, false);
            if (Fire2Object) ToggleEmissive(Fire2Object.gameObject, false);
            if (Thruster1Object) ToggleEmissive(Thruster1Object.gameObject, true);
            if (Thruster2Object) ToggleEmissive(Thruster2Object.gameObject, true);
            logger.Debug($"{gameObject.name} Starting Focus Mode...");
            StopCurrentPatterns();
            StartDangerPattern();

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
                // logger.Debug($"{gameObject.name} Containment check - bounds.Contains: {boundsContains}, ClosestPoint==pos: {inside}");
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
                    logger.Debug($"{gameObject.name} Sega After Burner Climax Motion Sim starting...");
                    logger.Debug($"{gameObject.name} GET READY, AGAIN!!!...");
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
            StartAttractPattern();
            if (firelight1) ToggleLight(firelight1, false);
            if (firelight2) ToggleLight(firelight2, false);
            if (Fire1Object) ToggleEmissive(Fire1Object.gameObject, false);
            if (Fire2Object) ToggleEmissive(Fire2Object.gameObject, false);
            if (Thruster1Object) ToggleEmissive(Thruster1Object.gameObject, false);
            if (Thruster2Object) ToggleEmissive(Thruster2Object.gameObject, false);
            if (Danger1Object) ToggleEmissive(Danger1Object.gameObject, false);
            if (Danger2Object) ToggleEmissive(Danger2Object.gameObject, false);
            ResetPositions();
            inFocusMode = false;  // Clear focus mode flag
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
            if (StickObject != null)
            {
                StickObject.localPosition = StickStartPosition;
                StickObject.localRotation = StickStartRotation;
            }
            if (ThrottleObject != null)
            {
                ThrottleObject.localPosition = ThrottleStartPosition;
                ThrottleObject.localRotation = ThrottleStartRotation;
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

        private void MapThumbsticks(ref bool inputDetected, ref bool throttleDetected)
        {
            if (!inFocusMode) return;

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
            // Xinput controller input
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
			// Map thumbstick for Stick 
			if (StickObject)
			{
				// Rotation applied on top of starting rotation
				Quaternion primaryRotation = Quaternion.Euler(
					primaryThumbstick.y * StickYRotationDegrees,
					0f,
					-primaryThumbstick.x * StickXRotationDegrees
				);
				StickObject.localRotation = StickStartRotation * primaryRotation;
				if (Mathf.Abs(primaryThumbstick.x) > 0.01f || Mathf.Abs(primaryThumbstick.y) > 0.01f)
					inputDetected = true; 
 isCenteringRotation = false; // Set if thumbstick moved
			}

			// Map secondary thumbstick to right stick rotation on X-axis
			if (ThrottleObject)
			{
				// Triggers for throttle rotation on X-axis (not Z unless your axis is set up that way)
				float LIndexTrigger = XInput.Get(XInput.Trigger.LIndexTrigger);
				float RIndexTrigger = XInput.Get(XInput.Trigger.RIndexTrigger);

				// This is the rotation you want to "add" to the starting rotation
				Quaternion triggerRotation = Quaternion.Euler(
					(LIndexTrigger - RIndexTrigger) * triggerRotationMultiplier, // X-axis
					0f,
					0f
				);
				// Apply it relative to the starting rotation
				ThrottleObject.localRotation = ThrottleStartRotation * triggerRotation;
				if (Mathf.Abs(LIndexTrigger) > 0.01f || Mathf.Abs(RIndexTrigger) > 0.01f)
					inputDetected = true; 
 isCenteringRotation = false; // Set if either trigger is pressed
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
                float inputValue = -primaryThumbstick.x * thumbstickVelocity * Time.deltaTime;
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

        private void MapButtons(ref bool inputDetected, ref bool throttleDetected) // Pass by reference
        {
            if (!inFocusMode) return;
   
            if (Input.GetButtonDown("Fire3") || XInput.GetDown(XInput.Button.X))    // press Fire3 turn on lights
            {
                ChangeColorEmissive(Fire1Object.gameObject, Color.red, 10.0f, true);
                ChangeColorEmissive(Fire2Object.gameObject, Color.red, 10.0f, true);
                if (firelight1) ToggleLight(firelight1, true);
                if (firelight2) ToggleLight(firelight2, true);
                inputDetected = true; 
 isCenteringRotation = false;
            }

            if (Input.GetButtonUp("Fire3") || XInput.GetUp(XInput.Button.X))     // release Fire3 turn off lights
            {
                if (Fire1Object) ToggleEmissive(Fire1Object.gameObject, false);
                if (Fire2Object) ToggleEmissive(Fire2Object.gameObject, false);
                if (firelight1) ToggleLight(firelight1, false);
                if (firelight2) ToggleLight(firelight2, false);
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
                logger.Warning($"{gameObject.name} RestoreOriginalParent: No original parent found for {name}");
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

                    // logger.Debug($"{gameObject.name} {targetObject.name} emissive state set to {(isActive ? "ON" : "OFF")}.");
                }
                else
                {
                    logger.Debug($"{gameObject.name} Renderer component not found on {targetObject.name}.");
                }
            }
            else
            {
                logger.Debug($"{targetObject.name} emissive object is not assigned.");
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
                logger.Debug($"{gameObject.name} Target emissive object is not assigned.");
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
                    logger.Debug($"{arrayName} object at index {i} not found under ControllerZ.");
                }
            }
        }

        IEnumerator AttractPattern()  //Pattern For Attract Mode
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 1f));
            while (true)
            {
                if (StartObject) ToggleEmissive(StartObject.gameObject, false);
                yield return new WaitForSeconds(attractFlashDuration);
                if (StartObject) ToggleEmissive(StartObject.gameObject, true);
                yield return new WaitForSeconds(attractFlashDelay);
            }
        }
        IEnumerator DangerPattern() //Pattern For Focused Danger Mode
        {
            while (true)
            {
                if (StartObject) ToggleEmissive(StartObject.gameObject, true);
                if (Fire1Object) ToggleEmissive(Fire1Object.gameObject, false);
                if (Fire2Object) ToggleEmissive(Fire2Object.gameObject, false);
                if (Danger1Object) ToggleEmissive(Danger1Object.gameObject, true);
                if (Danger2Object) ToggleEmissive(Danger2Object.gameObject, false);
                yield return new WaitForSeconds(dangerFlashDuration);
                if (Danger1Object) ToggleEmissive(Danger1Object.gameObject, false);
                if (Danger2Object) ToggleEmissive(Danger2Object.gameObject, true);
                yield return new WaitForSeconds(dangerFlashDelay);
            }
        }

        void ToggleEmissive(Renderer renderer, bool isOn)  // Method to toggle emssive textures
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

        public void StartAttractPattern()
        {
            // Stop any currently running coroutines
            StopCurrentPatterns();
            attractCoroutine = StartCoroutine(AttractPattern());
        }
        public void StartDangerPattern()
        {
            // Stop any currently running coroutines
            StopCurrentPatterns();
            dangerCoroutine = StartCoroutine(DangerPattern());
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

            // Log the names of the objects containing the Light components and filter out unwanted lights
            foreach (Light light in lights)
            {
                if (light.gameObject.name == "fire1light")
                {
                    firelight1 = light;
                    logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                }
                else if (light.gameObject.name == "fire2light")
                {
                    firelight2 = light;
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
                        GameObject cockpitCam = ZObject.Find("eyes/cockpitcam")?.gameObject;
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
                        GameObject vrCam = ZObject.Find("eyes/vrcam")?.gameObject;
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

                        // Find Throttle under Z
                        ThrottleObject = ZObject.Find("Throttle");
                        if (ThrottleObject != null)
                        {
                            logger.Debug($"{gameObject.name} Throttle object found.");
                            ThrottleStartPosition = ThrottleObject.localPosition;
                            ThrottleStartRotation = ThrottleObject.localRotation;
                        }
                        else
                        {
                            logger.Debug($"{gameObject.name} Throttle object not found.");
                        }

                        // Find Stick under Z
                        StickObject = ZObject.Find("Stick");
                        if (StickObject != null)
                        {
                            logger.Debug($"{gameObject.name} Stick object found.");
                            // Store initial position and rotation of the stick
                            StickStartPosition = StickObject.localPosition;
                            StickStartRotation = StickObject.localRotation;
                        }
                        else
                        {
                            logger.Debug($"{gameObject.name} Stick object not found.");
                        }
                        // Find Danger1Object object under Z
                        Danger1Object = ZObject.Find("Danger1");
                        if (Danger1Object != null)
                        {
                            logger.Debug($"{gameObject.name} Danger1 object found.");
                            // Ensure the Danger1 object is initially off
                            Renderer renderer = Danger1Object.GetComponent<Renderer>();
                            if (renderer != null)
                            {
                                renderer.material.DisableKeyword("_EMISSION");
                            }
                        }
                        else
                        {
                            logger.Debug($"{gameObject.name} Danger1 object not found.");
                        }
                        // Find Danger2Object object under Z
                        Danger2Object = ZObject.Find("Danger2");
                        if (Danger2Object != null)
                        {
                            logger.Debug($"{gameObject.name} Danger2 object found.");
                            // Ensure the Danger2 object is initially off
                            Renderer renderer = Danger2Object.GetComponent<Renderer>();
                            if (renderer != null)
                            {
                                renderer.material.DisableKeyword("_EMISSION");
                            }
                        }
                        else
                        {
                            logger.Debug($"{gameObject.name} Danger2 object not found.");
                        }
                        // Find Thruster1Object object under Z
                        Thruster1Object = ZObject.Find("Thruster1");
                        if (Thruster1Object != null)
                        {
                            logger.Debug($"{gameObject.name} Thruster1 object found.");
                            // Ensure the Thruster1 object is initially off
                            Renderer renderer = Thruster1Object.GetComponent<Renderer>();
                            if (renderer != null)
                            {
                                renderer.material.DisableKeyword("_EMISSION");
                            }
                        }
                        else
                        {
                            logger.Debug($"{gameObject.name} Thruster1 object not found.");
                        }
                        // Find Thruster2Object object under Z
                        Thruster2Object = ZObject.Find("Thruster2");
                        if (Thruster2Object != null)
                        {
                            logger.Debug($"{gameObject.name} Thruster2 object found.");
                            // Ensure the thruster2 object is initially off
                            Renderer renderer = Thruster2Object.GetComponent<Renderer>();
                            if (renderer != null)
                            {
                                renderer.material.DisableKeyword("_EMISSION");
                            }
                        }
                        else
                        {
                            logger.Debug($"{gameObject.name} Thruster2 object not found.");
                        }
                        // Find fire1Object object under Z
                        Fire1Object = ZObject.Find("Fire1");
                        if (Fire1Object != null)
                        {
                            logger.Debug($"{gameObject.name} Fire1 object found.");
                            // Ensure the Fire1 object is initially off
                            Renderer renderer = Fire1Object.GetComponent<Renderer>();
                            if (renderer != null)
                            {
                                renderer.material.DisableKeyword("_EMISSION");
                            }
                        }
                        else
                        {
                            logger.Debug($"{gameObject.name} Fire1 object not found.");
                        }
                        // Find FireObject object under Z
                        Fire2Object = ZObject.Find("Fire2");
                        if (Fire2Object != null)
                        {
                            logger.Debug($"{gameObject.name} Fire2 object found.");
                            // Ensure the Fire2 object is initially off
                            Renderer renderer = Fire2Object.GetComponent<Renderer>();
                            if (renderer != null)
                            {
                                renderer.material.DisableKeyword("_EMISSION");
                            }
                        }
                        else
                        {
                            logger.Debug($"{gameObject.name} Fire2 object not found.");
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