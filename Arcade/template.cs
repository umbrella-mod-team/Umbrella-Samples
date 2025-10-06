using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using EmuVR.InputManager;
using System.Collections;
using System.IO;
using System;
using WIGUx.Modules.MameHookModule;
using UnityEditor;
using System.Reflection;

namespace WIGUx.Modules.MotionSimTemplate
{
    public class TemplateMotionSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        [Header("Object Settings")]
        private Transform WheelObject; // Reference to the Wheel mirroring object
        private Transform ShifterObject; // Reference to the Shifter mirroring object
        private Transform GasObject; // Reference to the Gas mirroring object
        private Transform BrakeObject; // Reference to the Throttle mirroring object
        private Transform StartObject; // Reference to the Start button object
        private Transform HandlebarObject; // Reference to the Handlebar mirroring object
        private Transform LStickObject; // Reference to the Left Stick mirroring object
        private Transform RStickObject; // Reference to the Right Stick mirroring object
        private Transform StickObject; // Reference to the Stick mirroring object
        private Transform ThrottleObject; // Reference to the left stick mirroring object
        private Transform XObject; // Reference to the main X object
        private Transform YObject; // Reference to the main Y object
        private Transform ZObject; // Reference to the main Z object
        private Transform Fire1Object; // Reference to the Fire left light emissive
        private Transform Fire2Object; // Reference to the Fire right light emissive
        private Transform Danger1Object; // Reference to the Danger1 light emissive
        private Transform Danger2Object; // Reference to the Danger2 light emissive
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

        [Header("Velocity Multiplier Settings")]        // Speeds for the animation of the in game flight stick or wheel
        private float primaryThumbstickRotationMultiplier = 10f; // Multiplier for primary thumbstick rotation intensity
        private float secondaryThumbstickRotationMultiplier = 25f; // Multiplier for secondary thumbstick rotation intensity
        private float triggerRotationMultiplier = 20f; // Multiplier for trigger rotation intensity
        private float adjustSpeed = 1.0f;  // Adjust this adjustment speed as needed a lower number will lead to smaller adustments
        private readonly float rotationSmoothness = 5f;  //sets the smoothness of the rotation
        private float HandlebarRotationDegrees = 15f; // Degrees for Handlebar rotation, adjust as needed
        private float WheelRotationDegrees = 100f; // Degrees for wheel rotation, adjust as needed
        private float StickXRotationDegrees = 10f; // Degrees for Stick rotation, adjust as needed
        private float StickYRotationDegrees = 10f; // Degrees for Stick rotation, adjust as needed
        private readonly float thumbstickVelocity = 50f;  // Velocity for keyboard input
        private readonly float centeringVelocityX = 25f;  // Velocity for centering rotation
        private readonly float centeringVelocityY = 25f;  // Velocity for centering rotation
        private readonly float centeringVelocityZ = 25f;  // Velocity for centering rotation

        [Header("Rotation Tracking")]        // Sets up the rotation varibles and sets them to 0 
        private float currentRotationX = 0f;  // Current rotation for X-axis
        private float currentRotationY = 0f;  // Current rotation for Y-axis
        private float currentRotationZ = 0f;  // Current rotation for Z-axis

        [Header("Rotation Limits")]        // Rotation Limits 
        [SerializeField] float minRotationX = -15f;
        [SerializeField] float maxRotationX = 15f;
        [SerializeField] float minRotationY = -15f;
        [SerializeField] float maxRotationY = 15f;
        [SerializeField] float minRotationZ = -15f;
        [SerializeField] float maxRotationZ = 15f;

        [Header("Position Settings")]     // Initial positions setup
        private Vector3 XStartPosition;  // Initial X position for resetting
        private Vector3 YStartPosition;  // Initial Y positions for resetting
        private Vector3 ZStartPosition;  // Initial Z positions for resetting
        private Vector3 HandlebarStartPosition; // Initial Handlebar positions for resetting
        private Vector3 WheelStartPosition; // Initial Wheel positions for resetting
        private Vector3 ShifterStartPosition; // Initial Shifter positions for resetting
        private Vector3 GasStartPosition;  // Initial gas positions for resetting
        private Vector3 BrakeStartPosition;  // Initial brake positions for resetting
        private Vector3 LStickStartPosition; // Initial Left Stick positions for resetting
        private Vector3 RStickStartPosition; // Initial Right positions for resetting
        private Vector3 StickStartPosition; // Initial Throttle positions for resetting
        private Vector3 ThrottleStartPosition; // Initial Throttle positions for resetting
        private Vector3 playerCameraStartPosition;  // Initial Player Camera positions for resetting
        private Vector3 playerVRSetupStartPosition;  // Initial PlayerVR positions for resetting
        private Vector3 cockpitCamStartPosition;  // Initial cockpitCam positionsfor resetting
        private Vector3 vrCamStartPosition;    // Initial vrCam positionsfor resetting
        private Vector3 playerCameraInitialWorldScale; // Initial Player Camera world scale for resetting
        private Vector3 playerVRSetupInitialWorldScale; // Initial PlayerVR world scale for resetting

        [Header("Rotation Settings")]     // Initial rotations setup
        private Quaternion XStartRotation;  // Initial X rotation for resetting
        private Quaternion YStartRotation;  // Initial Y rotation for resetting
        private Quaternion ZStartRotation;  // Initial Z rotation for resetting
        private Quaternion HandlebarStartRotation;  // Initial Handlebar rotation for resetting
        private Quaternion WheelStartRotation;  // Initial Wheel rotation for resetting
        private Quaternion ShifterStartRotation;  // Initial Shifter rotation for resetting
        private Quaternion GasStartRotation;  // Initial gas rotation for resetting
        private Quaternion BrakeStartRotation;  // Initial brake rotation for resetting
        private Quaternion LStickStartRotation;  // Initial Left Stick rotation for resetting
        private Quaternion RStickStartRotation;  // Initial Right Stick rotation for resetting
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
        Dictionary<string, int> lastLampStates = new Dictionary<string, int>
             {
               { "lamp0", 0 }, { "lamp1", 0 }, { "lamp2", 0 }, { "lamp3", 0 }
             };
        [Header("Timers and States")]  // Store last states and timers
        private bool isFlashing = false; //set the flashing lights flag
        private bool isHigh = false; //set the high gear flag
        private bool inFocusMode = false;  // Flag to track focus mode state
        private bool isCenteringRotation = false; // Flag to track centering rotation state
        private bool isRiding = false; // Set riding state to true
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
            if (firelight1) ToggleLight(firelight1, false);
            if (firelight2) ToggleLight(firelight2, false);
            if (StartObject) ToggleEmissive(StartObject.gameObject, false);
            if (Hazard) ToggleEmissive(Hazard.gameObject, false);
            if (Fire2Object) ToggleEmissive(Fire2Object.gameObject, false);
            StartAttractPattern();
            
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
            if (systemState != null && systemState.IsOn)
            {
                StopCurrentPatterns();
            }
            if (isCenteringRotation && !inputDetected)
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
            bool inputDetected = false;

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
                HandleTransformAdjustment();
            }
        }

        void StartFocusMode()
        {
            logger.Debug($"{gameObject.name} Starting Focus Mode...");
            //      logger.Debug($"[CHECK] playerCamera: {(playerCamera == null ? "NULL" : playerCamera.name)}  cockpitCam: {(cockpitCam == null ? "NULL" : cockpitCam.name)}");
            //      logger.Debug($"[CHECK] playerCamera.activeSelf: {(playerCamera != null ? playerCamera.activeSelf.ToString() : "n/a")}  cockpitCam.activeSelf: {(cockpitCam != null ? cockpitCam.gameObject.activeSelf.ToString() : "n/a")}");
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
                // logger.Debug($"{gameObject.name} Containment check - bounds.Contains: {boundsContains}, ClosestPoint==pos: {inside}");
            }

            if (cockpitCollider != null && inside)
            {
                if (playerVRSetup == null)
                {
                    // Parent and apply offset to PlayerCamera
                    SaveOriginalParent(playerCamera);
                    Vector3 worldPos = playerCamera.transform.position;
                    Quaternion worldRot = playerCamera.transform.rotation;
                    playerCameraInitialWorldScale = playerCamera.transform.lossyScale;
                    playerCamera.transform.SetParent(cockpitCam.transform, false);
                    playerCamera.transform.position = worldPos;
                    playerCamera.transform.rotation = worldRot;
                    NormalizeWorldScale(playerCamera, cockpitCam.transform);
                    StartCoroutine(AlignPlayerHeadToCamera(playerCamera, cockpitCam.transform));
                    logger.Debug($"{gameObject.name} Player is aboard and strapped in.");
                    isRiding = true; // Set riding state to true
                }
                if (playerVRSetup != null)
                {
                    // Parent and apply offset to PlayerVRSetup
                    SaveOriginalParent(playerVRSetup);
                    Vector3 worldPos = playerVRSetup.transform.position;
                    Quaternion worldRot = playerVRSetup.transform.rotation;
                    playerVRSetupInitialWorldScale = playerVRSetup.transform.lossyScale;
                    playerVRSetup.transform.SetParent(vrCam.transform, false);
                    playerVRSetup.transform.position = worldPos;
                    playerVRSetup.transform.rotation = worldRot;
                    NormalizeWorldScale(playerVRSetup, vrCam.transform);
                    StartCoroutine(AlignPlayerHeadToCamera(playerVRSetup, vrCam.transform));
                    logger.Debug($"{gameObject.name} VR Player is aboard and strapped in!");
                    logger.Debug($"{gameObject.name} Motion Sim starting...");
                    logger.Debug($"{gameObject.name} GET READY!!...");
                    isRiding = true; // Set riding state to true
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} Player is not aboard the ride, Starting Without the player aboard.");
            }

            inFocusMode = true;
        }

        void EndFocusMode()
        {
            logger.Debug($"{gameObject.name} Exiting Focus Mode...");
            RestoreOriginalParent(playerCamera, "PlayerCamera");
            RestoreOriginalParent(playerVRSetup, "PlayerVRSetup.PlayerRig");
            StartAttractPattern();
            // if (Fire1Object) ToggleEmissive(Fire1Object.gameObject, false);
            // if (Fire2Object) ToggleEmissive(Fire2Object.gameObject, false);
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

            if (LStickObject != null)
            {
                LStickObject.localPosition = LStickStartPosition;
                LStickObject.localRotation = LStickStartRotation;
            }
            if (RStickObject != null)
            {
                RStickObject.localPosition = RStickStartPosition;
                RStickObject.localRotation = RStickStartRotation;
            }
            if (HandlebarObject != null)
            {
                HandlebarObject.localPosition = HandlebarStartPosition;
                HandlebarObject.localRotation = HandlebarStartRotation;
            }
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
                }
            }
            if (!inputDetected)
            {
                CenterRotation();    // Center the rotation if no input is detected
            }
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
			// Map primary thumbstick to Handlebar
			if (HandlebarObject)
			{
				Quaternion primaryRotation = Quaternion.Euler(
					0f,
					primaryThumbstick.x * HandlebarRotationDegrees,
					0f

				);
				HandlebarObject.localRotation = HandlebarStartRotation * primaryRotation;
				if (Mathf.Abs(primaryThumbstick.x) > 0.01f) // Only set if Handlebar is being turned
					inputDetected = true;
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
			}

			// Map triggers to throttle rotation on X-axis
			if (ThrottleObject)
			{
				// Triggers for throttle rotation on X-axis
				Quaternion triggerRotation = Quaternion.Euler(
					(RIndexTrigger - LIndexTrigger) * triggerRotationMultiplier, // X-axis
					0f,
					0f
				);
				ThrottleObject.localRotation = ThrottleStartRotation * triggerRotation;
			}
            // Map primary thumbstick to Left Stick
            if (LStickObject)
            {
                // Rotation applied on top of starting rotation
                Quaternion primaryRotation = Quaternion.Euler(
                    primaryThumbstick.y * StickYRotationDegrees,
                    0f,
                    -primaryThumbstick.x * StickXRotationDegrees
                );
                LStickObject.localRotation = LStickStartRotation * primaryRotation;
            }
            // Map primary thumbstick to Right Stick
            if (RStickObject)
            {
                // Rotation applied on top of starting rotation
                Quaternion secondaryRotation = Quaternion.Euler(
                    secondaryThumbstick.y * StickYRotationDegrees,
                    0f,
                    -secondaryThumbstick.x * StickXRotationDegrees
                );
                RStickObject.localRotation = RStickStartRotation * secondaryRotation;
            }
            // Analog R trigger → X-axis rotation (e.g., rotate backward)
            if (RIndexTrigger > 0.01f)
			{
				float rotateX = primaryThumbstickRotationMultiplier * Time.deltaTime;
				float targetRotation = Mathf.Clamp(currentRotationX + rotateX, minRotationX, maxRotationX);
				float appliedRotateX = targetRotation - currentRotationX;
				if (Mathf.Abs(appliedRotateX) > 0.0001f)
				{
					XObject.localRotation *= Quaternion.Euler(-appliedRotateX, 0f, 0f);
					currentRotationX = targetRotation;
				}
			}

			// Analog L trigger → X-axis rotation (e.g., rotate forward)
			if (LIndexTrigger > 0.01f)
			{
				float rotateX = primaryThumbstickRotationMultiplier * Time.deltaTime;
				float targetRotation = Mathf.Clamp(currentRotationX - rotateX, minRotationX, maxRotationX);
				float appliedRotateX = currentRotationX - targetRotation;
				if (Mathf.Abs(appliedRotateX) > 0.0001f)
				{
					XObject.localRotation *= Quaternion.Euler(appliedRotateX, 0f, 0f);
					currentRotationX = targetRotation;
				}
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
                }
            }
            if (!inputDetected)
            {
                CenterRotation();    // Center the rotation if no input is detected
            }
        }

        private void MapButtons(ref bool inputDetected) // Pass by reference
        {
            if (!inFocusMode) return;

            // Fire3/X button pressed (turn on lights)
            if (XInput.GetDown(XInput.Button.X)
                || OVRInput.GetDown(OVRInput.Button.One) // Oculus "A"/"X" (use .One for canonical blue X left)
                || SteamVRInput.GetDown(SteamVRInput.TouchButton.X)
            )
            {
                ChangeColorEmissive(Fire1Object.gameObject, Color.red, 10.0f, true);
                ChangeColorEmissive(Fire2Object.gameObject, Color.red, 10.0f, true);
                if (firelight1) ToggleLight(firelight1, true);
                if (firelight2) ToggleLight(firelight2, true);
            }

            // Fire3/X button released (turn off lights)
            if (XInput.GetUp(XInput.Button.X)
                || OVRInput.GetUp(OVRInput.Button.One)
                || SteamVRInput.GetUp(SteamVRInput.TouchButton.X)
            )
            {
                ChangeColorEmissive(Fire1Object.gameObject, Color.white, 2.0f, true);
                ChangeColorEmissive(Fire2Object.gameObject, Color.white, 2.0f, true);
                if (firelight1) ToggleLight(firelight1, false);
                if (firelight2) ToggleLight(firelight2, false);
            }

            // Left Trigger pressed (brake lights/emissives on)
            if (
                XInput.GetDown(XInput.Button.LIndexTrigger)
                || OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger)
                || SteamVRInput.GetDown(SteamVRInput.TouchButton.LTrigger)
            )
            {
                if (brakelight1) ToggleLight(brakelight1, true);
                if (brakelight2) ToggleLight(brakelight2, true);
                if (StartObject) ToggleEmissive(StartObject.gameObject, true);
                if (Brakelight1) ToggleEmissive(Brakelight1.gameObject, true);
                if (Brakelight2) ToggleEmissive(Brakelight2.gameObject, true);
                if (Brakelight3) ToggleEmissive(Brakelight3.gameObject, true);
            }

            // Left Trigger released (brake lights/emissives off)
            if (
                XInput.GetUp(XInput.Button.LIndexTrigger)
                || OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger)
                || SteamVRInput.GetUp(SteamVRInput.TouchButton.LTrigger)
            )
            {
                if (brakelight1) ToggleLight(brakelight1, false);
                if (brakelight2) ToggleLight(brakelight2, false);
                if (StartObject) ToggleEmissive(StartObject.gameObject, false);
                if (Brakelight1) ToggleEmissive(Brakelight1.gameObject, false);
                if (Brakelight2) ToggleEmissive(Brakelight2.gameObject, false);
                if (Brakelight3) ToggleEmissive(Brakelight3.gameObject, false);
            }

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
        }

        void CenterRotation()
        {
            isCenteringRotation = true;
        }

        void HandleTransformAdjustment()
        {
            if (!inFocusMode) return;

            bool cockpitCamMoved = false;
            bool vrCamMoved = false;

            // Move BOTH cameras if isRiding is true
            if (isRiding)
            {
                // Desktop camera (cockpitCam)
                if (cockpitCam != null)
                {
                    if (Input.GetKey(KeyCode.Home))
                    {
                        cockpitCam.transform.localPosition += Vector3.forward * adjustSpeed * Time.deltaTime;
                        cockpitCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.End))
                    {
                        cockpitCam.transform.localPosition += Vector3.back * adjustSpeed * Time.deltaTime;
                        cockpitCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.UpArrow))
                    {
                        cockpitCam.transform.localPosition += Vector3.up * adjustSpeed * Time.deltaTime;
                        cockpitCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.DownArrow))
                    {
                        cockpitCam.transform.localPosition += Vector3.down * adjustSpeed * Time.deltaTime;
                        cockpitCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.LeftArrow))
                    {
                        cockpitCam.transform.localPosition += Vector3.left * adjustSpeed * Time.deltaTime;
                        cockpitCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.RightArrow))
                    {
                        cockpitCam.transform.localPosition += Vector3.right * adjustSpeed * Time.deltaTime;
                        cockpitCamMoved = true;
                    }
                    if (Input.GetKeyDown(KeyCode.Backspace))
                    {
                        cockpitCam.transform.Rotate(0, 90, 0);
                        cockpitCamMoved = true;
                    }
                }

                // VR camera (vrCam)
                if (vrCam != null)
                {
                    if (Input.GetKey(KeyCode.Home))
                    {
                        vrCam.transform.localPosition += Vector3.forward * adjustSpeed * Time.deltaTime;
                        vrCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.End))
                    {
                        vrCam.transform.localPosition += Vector3.back * adjustSpeed * Time.deltaTime;
                        vrCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.UpArrow))
                    {
                        vrCam.transform.localPosition += Vector3.up * adjustSpeed * Time.deltaTime;
                        vrCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.DownArrow))
                    {
                        vrCam.transform.localPosition += Vector3.down * adjustSpeed * Time.deltaTime;
                        vrCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.LeftArrow))
                    {
                        vrCam.transform.localPosition += Vector3.left * adjustSpeed * Time.deltaTime;
                        vrCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.RightArrow))
                    {
                        vrCam.transform.localPosition += Vector3.right * adjustSpeed * Time.deltaTime;
                        vrCamMoved = true;
                    }
                    if (Input.GetKeyDown(KeyCode.Backspace))
                    {
                        vrCam.transform.Rotate(0, 90, 0);
                        vrCamMoved = true;
                    }
                }
            }

            // Save and log **only if there was a change**
            if (vrCam != null && vrCamMoved)
            {
                vrCamStartPosition = vrCam.transform.localPosition;
                vrCamStartRotation = vrCam.transform.localRotation;
                Debug.Log($"{gameObject.name}vrCam localPosition: " + vrCam.transform.localPosition.ToString("F4"));
            }
            if (cockpitCam != null && cockpitCamMoved)
            {
                cockpitCamStartPosition = cockpitCam.transform.localPosition;
                cockpitCamStartRotation = cockpitCam.transform.localRotation;
                Debug.Log($"{gameObject.name} cockpitCam localPosition: " + cockpitCam.transform.localPosition.ToString("F4"));
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

        void ReadData()
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
                        case "lamp1":
                            ProcessLamp1(newValue);
                            break;
                        case "lamp2":
                            ProcessLamp2(newValue);
                            break;
                        case "lamp3":
                            ProcessLamp3(newValue);
                            break;
                        default:
                            logger.Debug($"{gameObject.name} No processing function for '{lampKey}'");
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
            logger.Debug($"{gameObject.name} Lamp 0 updated: {state}");

            // Update lights
            if (locklight) ToggleLight(locklight, state == 1);
            // Update emissive material
            if (Fire2Object) ToggleEmissive(Fire2Object.gameObject, state == 1);
        }
        // Individual function for lamp1
        void ProcessLamp1(int state)
        {
            logger.Debug($"{gameObject.name} Lamp 1 updated: {state}");

            // Update lights

            // Update emissive material

        }
        // Individual function for lamp2
        void ProcessLamp2(int state)
        {
            logger.Debug($"{gameObject.name} Lamp 2 updated: {state}");

            // Update lights

            if (firelight1) ToggleLight firelight1 = (state == 1);
            if (firelight2) ToggleLight firelight2 = (state == 1);
            // Update emissive material
            if (Fire1Object) ToggleEmissive(Fire1Object.gameObject, state == 1);
        }

        // Individual function for lamp3
        void ProcessLamp3(int state)
        {
            logger.Debug($"{gameObject.name} Lamp 3 updated: {state}");

            // Update lights
            if (startlight) ToggleLight startlight = (state == 1);
            // Update emissive material
            if (StartObject) ToggleEmissive(StartObject.gameObject, state == 1);
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

        void NormalizeWorldScale(GameObject obj, Transform newParent)
        {
            if (obj == null || newParent == null) return;

            Vector3 parentWorldScale = newParent.lossyScale;
            Vector3 targetWorldScale = Vector3.one;

            // Use the correct initial world scale for each object
            if (obj == playerCamera)
                targetWorldScale = playerCameraInitialWorldScale;
            else if (obj == playerVRSetup)
                targetWorldScale = playerVRSetupInitialWorldScale;
            else
                return; // Do nothing for unknown objects

            obj.transform.localScale = new Vector3(
                targetWorldScale.x / parentWorldScale.x,
                targetWorldScale.y / parentWorldScale.y,
                targetWorldScale.z / parentWorldScale.z
            );
        }
        // === ALIGNMENT (VR + DESKTOP, unified, delayed) ===
        // Waits a short delay for parenting to settle, then moves vrcam or cockpitcam so the player head/camera
        // aligns with the "eyes" anchor height.
        IEnumerator AlignPlayerHeadToCamera(GameObject player, Transform cameraTransform)
        {
            if (player == null || cameraTransform == null)
            {
                logger.Debug($"{gameObject.name} [Align] skipped — player or cameraTransform null");
                yield break;
            }

            // small delay to let parenting finish
            yield return new WaitForSeconds(0.25f);

            // "eyes" anchor is the parent of vrcam/cockpitcam, so use cameraTransform.parent
            Transform eyesAnchor = cameraTransform.parent;
            if (eyesAnchor == null)
            {
                logger.Debug($"{gameObject.name} [Align] no 'eyes' anchor found (cameraTransform has no parent).");
                yield break;
            }

            // locate VR or desktop head inside the current camera branch, not globally
            Transform head =
                cameraTransform.Find("[SteamVRCameraRig]/Camera (eye)/Head") ??
                cameraTransform.Find("OVRCameraRig/TrackingSpace/CenterEyeAnchor") ??
                cameraTransform.Find("Camera (Main)") ??
                cameraTransform.Find("Camera (eye)") ??
                cameraTransform.Find("Camera");

            // desktop fallback: use cached PlayerCamera if available
            if (head == null && playerCamera != null)
                head = playerCamera.transform;

            if (head == null)
            {
                logger.Debug($"{gameObject.name} [Align] ❌ could not find head or player camera under {cameraTransform.name}.");
                yield break;
            }

            // compute world offset between player's head and eyes anchor
            Vector3 offset = head.position - eyesAnchor.position;

            // move the camera anchor (vrcam or cockpitcam) so head/camera lines up with eyes
            cameraTransform.position -= offset;

            string mode;
            if (head.name.Contains("CenterEyeAnchor"))
                mode = "OVR";
            else if (head.name.Contains("PlayerCamera") || head.name.Contains("Camera (Main)"))
                mode = "Desktop";
            else
                mode = "SteamVR";

            logger.Debug($"{gameObject.name} [Align] ✅ adjusted {cameraTransform.name} by {-offset.y:F3} for {mode}");
        }


        // Save original parent of object in dictionary
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

        // Restore original parent of object and log appropriate message
        void RestoreOriginalParent(GameObject obj, string name)
        {
            if (obj == null)
            {
                logger.Error($"RestoreOriginalParent: {name} is NULL!");
                return;
            }

            if (!originalParents.ContainsKey(obj))
            {
                logger.Warning($"RestoreOriginalParent: No original parent found for {name}");
                return;
            }

            Transform originalParent = originalParents[obj];

            Vector3 worldPos = obj.transform.position;
            Quaternion worldRot = obj.transform.rotation;

            // If the original parent was NULL, place the object back in the root
            if (originalParent == null)
            {
                obj.transform.SetParent(null, false);  // Moves it back to the root
                obj.transform.position = worldPos;
                obj.transform.rotation = worldRot;

                // When no parent, just set localScale to initial world scale
                if (obj == playerCamera)
                    obj.transform.localScale = playerCameraInitialWorldScale;
                else if (obj == playerVRSetup)
                    obj.transform.localScale = playerVRSetupInitialWorldScale;

                logger.Debug($"{name} restored to root.");
            }
            else
            {
                obj.transform.SetParent(originalParent, false);
                obj.transform.position = worldPos;
                obj.transform.rotation = worldRot;
                NormalizeWorldScale(obj, originalParent);

                logger.Debug($"{name} restored to original parent: {originalParent.name}");
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
                if (BrakelightsObject) ToggleEmissive(BrakelightsObject.gameObject, false);
                if (brake_light1) ToggleLight(brake_light1, false);
                if (brake_light2) ToggleLight(brake_light2, false);
                yield return new WaitForSeconds(attractFlashDuration);
                if (BrakelightsObject) ToggleEmissive(BrakelightsObject.gameObject, true);
                if (brake_light1) ToggleLight(brake_light1, true);
                if (brake_light2) ToggleLight(brake_light2, true);
                yield return new WaitForSeconds(attractFlashDelay);
            }
        }

        IEnumerator dangerPattern() //Pattern For Focused Danger Mode
        {
            while (true)
            {
                if (BrakelightsObject) ToggleEmissive(BrakelightsObject.gameObject, false);
                if (brake_light1) ToggleLight(brake_light1, false);
                if (brake_light2) ToggleLight(brake_light2, false);
                yield return new WaitForSeconds(dangerFlashDuration);
                if (BrakelightsObject) ToggleEmissive(BrakelightsObject.gameObject, true);
                if (brake_light1) ToggleLight(brake_light1, true);
                if (brake_light2) ToggleLight(brake_light2, true);
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
                if (light.gameObject.name == "brakelight1")
                {
                    brake_light1 = light;
                    logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                }
                else if (light.gameObject.name == "brakelight2")
                {
                    brake_light2 = light;
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
                        // Find fireObject object under Z
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
                        // Find fire2Object object under Z
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

                        // Find LStick 
                        LStickObject = transform.Find("LStick");
                        if (LStickObject != null)
                        {
                            logger.Debug($"{gameObject.name} LStick object found.");
                            // Store initial position and rotation of the Left stick
                            LStickStartPosition = LStickObject.localPosition;
                            LStickStartRotation = LStickObject.localRotation;
                        }
                        else
                        {
                            logger.Debug($"{gameObject.name} LStick object not found.");
                        }

                        // Find RStick 
                        RStickObject = transform.Find("RStick");
                        if (RStickObject != null)
                        {
                            logger.Debug($"{gameObject.name} RStick object found.");
                            // Store initial position and rotation of the Right stick
                            RStickStartPosition = RStickObject.localPosition;
                            RStickStartRotation = RStickObject.localRotation;
                        }
                        else
                        {
                            logger.Debug($"{gameObject.name} RStick object not found.");
                        }
                        // Find Handlebar object under Z
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
                        // Find Wheel under Z
                        WheelObject = ZObject.Find("Wheel");
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
                        ShifterObject = ZObject.Find("Shifter");
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
                        GasObject = ZObject.Find("Gas");
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


                        // Find Brake under Z
                        BrakeObject = ZObject.Find("Brake");
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