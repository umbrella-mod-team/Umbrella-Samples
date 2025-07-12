using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using EmuVR.InputManager;
using System.Collections;
using System.IO;

namespace WIGUx.Modules.id3MotionSim
{
    public class id3MotionSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        [Header("Object Settings")]
        private Transform XObject; // Reference to the main X object
        private Transform YObject; // Reference to the main Y object
        private Transform ZObject; // Reference to the main Z object
        private Transform ControllerX; // Reference to the main animated controller (wheel)
        private Transform ControllerY; // Reference to the main animated controller (wheel)
        private Transform ControllerZ; // Reference to the main animated controller (wheel)
        private Transform WheelObject; // Reference to the throttle mirroring object
        private Transform ShifterObject; // Reference to the animated controller (shifter)
        private Transform GasObject; // Reference to the throttle mirroring object
        private Transform BrakeObject; // Reference to the throttle mirroring object
        private Transform StartObject; // Reference to the throttle mirroring object
        private GameObject vrCam;    // Reference to the VR Camera  
        private GameObject cockpitCam;    // Reference to the cockpit camera
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
        public float primaryThumbstickRotationMultiplier = 80f; // Multiplier for primary thumbstick rotation intensity
        public float secondaryThumbstickRotationMultiplier = 2f; // Multiplier for secondary thumbstick rotation intensity
        public float triggerRotationMultiplier = 2f; // Multiplier for trigger rotation intensity
        private float adjustSpeed = 1.0f;  // Adjust this adjustment speed as needed a lower number will lead to smaller adustments
        private readonly float rotationSmoothness = 5f;  //sets the smoothness of the rotation
        private readonly float thumbstickVelocity = 25f;  // Velocity for keyboard input
        private readonly float keyboardVelocityX = 25f;  // Velocity for keyboard input
        private readonly float keyboardVelocityY = 25f;  // Velocity for keyboard input
        private readonly float keyboardVelocityZ = 25f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityX = 80.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityY = 80.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityZ = 80.5f;  // Velocity for keyboard input
        private readonly float centeringVelocityX = 25f;  // Velocity for centering rotation
        private readonly float centeringVelocityY = 25f;  // Velocity for centering rotation
        private readonly float centeringVelocityZ = 25f;  // Velocity for centering rotation
        private readonly float centeringControllerVelocityX = 80.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float centeringControllerVelocityY = 80.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float centeringControllerVelocityZ = 80.5f;  // Velocity for centering rotation (stick or wheel)

        [Header("Rotation Tracking")]        // Sets up the rotation varibles and sets them to 0 
        private float currentRotationX = 0f;  // Current rotation for X-axis
        private float currentRotationY = 0f;  // Current rotation for Y-axis
        private float currentRotationZ = 0f;  // Current rotation for Z-axis
        private float currentControllerRotationX = 0f;  // Current rotation for X-axis (stick or wheel)
        private float currentControllerRotationY = 0f;  // Current rotation for Y-axis (stick or wheel)
        private float currentControllerRotationZ = 0f;  // Current rotation for Z-axis (stick or wheel)

        [Header("Rotation Limits")]        // Rotation Limits for Keyboard inputs

        private float rotationLimitX = 16f;  // Rotation limit for X-axis
        private float rotationLimitY = 30f;  // Rotation limit for Y-axis
        private float rotationLimitZ = 40f;  // Rotation limit for Z-axis
        private float controllerrotationLimitX = 80f;  // Rotation limit for X-axis (stick or wheel)
        private float controllerrotationLimitY = 0f;  // Rotation limit for Y-axis (stick or wheel)
        private float controllerrotationLimitZ = 80f;  // Rotation limit for Z-axis (stick or wheel)

        [Header("Position Settings")]     // Initial positions setup
        private Vector3 XStartPosition;  // Initial X position for resetting
        private Vector3 YStartPosition;  // Initial Y positions and rotations for resetting
        private Vector3 ZStartPosition;  // Initial Z positions and rotations for resetting
        private Vector3 ControllerXStartPosition; // Initial controller positions and rotations for resetting
        private Vector3 ControllerYStartPosition; // Initial controller positions and rotations for resetting
        private Vector3 ControllerZStartPosition; // Initial controller positions and rotations for resetting
        private Vector3 WheelStartPosition; // Initial Wheel positions for resetting
        private Vector3 ShifterStartPosition; // Initial Shifter positions for resetting
        private Vector3 GasStartPosition;  // Initial gas positions for resetting
        private Vector3 BrakeStartPosition;  // Initial brake positions for resetting
        private Vector3 playerCameraStartPosition;  // Initial Player Camera positions and rotations for resetting
        private Vector3 playerVRSetupStartPosition;  // Initial PlayerVR positions and rotations for resetting
        private Vector3 cockpitCamStartPosition;  // Initial cockpitCam positions and rotations for resetting
        private Vector3 vrCamStartPosition;    // Initial vrCam positionsfor resetting

        [Header("Rotation Settings")]     // Initial rotations setup
        private Quaternion XStartRotation;  // Initial X rotation for resetting
        private Quaternion YStartRotation;  // Initial Y rotation for resetting
        private Quaternion ZStartRotation;  // Initial Z positions for resetting
        private Quaternion ControllerXStartRotation; // Initial controlller X positions and rotations for resetting
        private Quaternion ControllerYStartRotation; // Initial controlller Y positions and rotations for resetting
        private Quaternion ControllerZStartRotation; // Initial controlller Z positions and rotations for resetting
        private Quaternion WheelStartRotation;  // Initial Wheel rotation for resetting
        private Quaternion ShifterStartRotation;  // Initial Shifter rotation for resetting
        private Quaternion GasStartRotation;  // Initial gas rotation for resetting
        private Quaternion BrakeStartRotation;  // Initial brake rotation for resetting
        private Quaternion playerCameraStartRotation;  // Initial Player Camera rotation for resetting
        private Quaternion playerVRSetupStartRotation;  // Initial PlayerVR rotation for resetting
        private Quaternion cockpitCamStartRotation;  // Initial cockpitCam rotation for resetting
        private Quaternion vrCamStartRotation;      // Initial VRCam rotation for resetting

        [Header("Lights and Emissives")]     // Setup Emissive and Lights
        public Light turbolight1;
        public Light turbolight2;
        private Transform Turbo1Object;
        private Transform Turbo2Object;
        private Transform HazardlObject;
        private Renderer[] AttractEmissiveObjects;
        private Transform HazardrObject;
        private Renderer[] HazzardEmissiveObjects;
        public float FlashDuration = 0.01f;
        public float FlashDelay = 0.05f;
        public float DangerDuration = 0.3f;
        public float DangerDelay = 0.3f;
        private Coroutine AttractCoroutine;
        private Coroutine DangerCoroutine; // Coroutine variable to control the strobe flashing
        public float lightDuration = 0.35f; // Duration during which the lights will be on
        private Light[] lights;        //array of lights

        [Header("Collider Triggers")]
        [SerializeField] private Collider cockpitCollider;

        [Header("Rom Check")]
        private bool inFocusMode = false;  // Flag to track focus mode state
        private GameSystem gameSystem;  // Cached GameSystem for this cabinet.
        private string insertedGameName = string.Empty;
        private string controlledGameName = string.Empty;
        private string filePath;
        private string configPath;
        private Dictionary<GameObject, Transform> originalParents = new Dictionary<GameObject, Transform>();  // Dictionary to store original parents of objects

        void Start()
        {
            CheckInsertedGameName();
            CheckControlledGameName();
            filePath = $"./Emulators/MAME/outputs/{insertedGameName}.txt";
            configPath = $"./Emulators/MAME/inputs/{insertedGameName}.ini";
            //WriteLampConfig(filePath);
            gameSystem = GetComponent<GameSystem>();
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
                logger.Debug("No VR Devices found. No SteamVR or OVR present)");
            }
            // Find X object in hierarchy
            XObject = transform.Find("X");
            if (XObject != null)
            {
                logger.Debug($"{gameObject.name} X object found.");
                XStartPosition = XObject.position;
                XStartRotation = XObject.rotation;

                // Find Y object under X
                YObject = XObject.Find("Y");
                if (YObject != null)
                {
                    logger.Debug($"{gameObject.name} Y object found.");
                    YStartPosition = YObject.position;
                    YStartRotation = YObject.rotation;

                    // Find Z object under Y
                    ZObject = YObject.Find("Z");
                    if (ZObject != null)
                    {
                        logger.Debug($"{gameObject.name} Z object found.");
                        ZStartPosition = ZObject.position;
                        ZStartRotation = ZObject.rotation;

                        // Find cockpit camera
                        GameObject cockpitCam = ZObject.Find("eyes/cockpitcam")?.gameObject;
                        if (cockpitCam != null)
                        {
                            logger.Debug($"{gameObject.name} Cockpitcam object found.");
                            cockpitCamStartPosition = cockpitCam.transform.position;
                            cockpitCamStartRotation = cockpitCam.transform.rotation;
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
                            vrCamStartPosition = vrCam.transform.position;
                            vrCamStartRotation = vrCam.transform.rotation;
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
                        // Find Shifter
                        ShifterObject = ZObject.Find("Shifter");
                        if (ShifterObject != null)
                        {
                            logger.Debug($"{gameObject.name} Shifter object found.");
                            // Store initial position and rotation of the Shifter
                            ShifterStartPosition = ShifterObject.position;
                            ShifterStartRotation = ShifterObject.rotation;
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
                            GasStartPosition = GasObject.position;
                            GasStartRotation = GasObject.rotation;
                        }
                        else
                        {
                            logger.Debug($"{ZObject.name} Gas object not found.");
                        }


                        // Find Brake under Z
                        BrakeObject = ZObject.Find("Brake");
                        if (BrakeObject != null)
                        {
                            logger.Debug($"{gameObject.name} Brake object found.");
                            BrakeStartPosition = BrakeObject.position;
                            BrakeStartRotation = BrakeObject.rotation;
                        }
                        else
                        {
                            logger.Debug($"{gameObject.name} Brake object not found.");
                        }

                        // Find ControllerX under Z
                        ControllerX = ZObject.Find("ControllerX");
                        if (ControllerX != null)
                        {
                            logger.Debug($"{gameObject.name} ControllerX object found.");
                            ControllerXStartPosition = ControllerX.position;
                            ControllerXStartRotation = ControllerX.rotation;

                            // Find ControllerY under ControllerX
                            ControllerY = ControllerX.Find("ControllerY");
                            if (ControllerY != null)
                            {
                                logger.Debug($"{gameObject.name} ControllerY object found.");
                                ControllerYStartPosition = ControllerY.position;
                                ControllerYStartRotation = ControllerY.rotation;

                                // Find ControllerZ under ControllerY
                                ControllerZ = ControllerY.Find("ControllerZ");
                                if (ControllerZ != null)
                                {
                                    logger.Debug($"{gameObject.name} ControllerZ object found.");
                                    ControllerZStartPosition = ControllerZ.position;
                                    ControllerZStartRotation = ControllerZ.rotation;
                                }

                                else
                                {
                                    logger.Debug($"{gameObject.name} ControllerZ object not found.");
                                }
                            }
                            else
                            {
                                logger.Debug($"{gameObject.name} ControllerY object not found.");
                            }
                        }
                        else
                        {
                            logger.Debug($"{gameObject.name} ControllerX object not found.");
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
            // Find cockpit camera
            cockpitCam = ZObject.Find("eyes/cockpitcam")?.gameObject;
            if (cockpitCam != null)
            {
                logger.Debug($"{gameObject.name} Cockpitcam object found.");
                cockpitCamStartPosition = cockpitCam.transform.position;
                cockpitCamStartRotation = cockpitCam.transform.rotation;
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
                vrCamStartPosition = vrCam.transform.position;
                vrCamStartRotation = vrCam.transform.rotation;
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
                else
                {
                    logger.Debug($"{gameObject.name} Start object not found.");
                }


                // Find Wheel under Z
                WheelObject = ZObject.Find("Wheel");
                if (WheelObject != null)
                {
                    logger.Debug($"{gameObject.name} Wheel object found.");
                    WheelStartPosition = WheelObject.position;
                    WheelStartRotation = WheelObject.rotation;
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
                    ShifterStartPosition = ShifterObject.position;
                    ShifterStartRotation = ShifterObject.rotation;
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
                    GasStartPosition = GasObject.position;
                    GasStartRotation = GasObject.rotation;
                }
                else
                {
                    logger.Debug($"{ZObject.name} Gas object not found.");
                }
                // Find Brake under Z
                BrakeObject = ZObject.Find("Brake");
                if (BrakeObject != null)
                {
                    logger.Debug($"{gameObject.name} Brake object found.");
                    BrakeStartPosition = BrakeObject.position;
                    BrakeStartRotation = BrakeObject.rotation;
                }
                else
                {
                    logger.Debug($"{gameObject.name} Brake object not found.");
                }
                // Attempt to find cockpitCollider by name if not assigned in Inspector
                if (cockpitCollider == null)
                {
                    const string colliderName = "Cockpit"; // Change to your cockpit collider GameObject's name
                    GameObject go = GameObject.Find(colliderName);
                    if (go != null)
                    {
                        cockpitCollider = go.GetComponent<Collider>();
                        if (cockpitCollider != null)
                            logger.Info($"{gameObject.name} cockpitCollider found by name: {cockpitCollider.name}");
                        else
                            logger.Error($"{gameObject.name} {colliderName} found but has no Collider component!");
                    }
                    else
                    {
                        logger.Error($"{gameObject.name} No GameObject named {colliderName} found in scene.");
                    }
                }
                if (cockpitCollider != null)
                {
                    logger.Debug($"{gameObject.name} Using cockpitCollider: {cockpitCollider.name}");
                    if (!cockpitCollider.isTrigger)
                        logger.Debug($"{gameObject.name} cockpitCollider is not set as a trigger!");
                }
                HazzardEmissiveObjects = new Renderer[1];
                HazzardEmissiveObjects[0] = transform.Find("Hazard")?.GetComponent<Renderer>();
                StartAttractPattern();
            }

            void Update()
            {
                bool inputDetected = false;  // Initialize for centering for keyboard input
                bool throttleDetected = false;// Initialize for centering for keyboard input
                CheckInsertedGameName();
                CheckControlledGameName();

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
                    HandleInput(ref inputDetected, ref throttleDetected); // Pass by reference
                }
            }

            void StartFocusMode()
            {
                logger.Info("Compatible Rom Dectected, Starting Inital D Ver. 3 ...");
                logger.Info("Initial D Cycraft Motion Sim starting... Rolling in the 90s");
                logger.Info("GAS GAS GAS!...");
                cockpitCam.transform.position = cockpitCamStartPosition; // new hotness
                cockpitCam.transform.rotation = cockpitCamStartRotation; // new hotness
                StopCurrentPatterns();
                //  strobeCoroutine = StartCoroutine(FlashStrobes());
                // StopCoroutine(strobeCoroutine);
                //  StartDangerPattern();
                // Set objects as children of cockpit cam for focus mode

                if (cockpitCam != null)
                {
                    if (playerCamera != null)
                    {
                        // Store initial position, rotation, and scale of PlayerCamera
                        playerCameraStartPosition = playerCamera.transform.position;
                        playerCameraStartRotation = playerCamera.transform.rotation;
                        SaveOriginalParent(playerCamera);  // Save original parent of PlayerCamera

                        // Set PlayerCamera as child of cockpit cam and maintain scale
                        playerCamera.transform.SetParent(cockpitCam.transform, false);

                        playerCamera.transform.localRotation = Quaternion.identity;
                        logger.Info("PlayerCamera set as child of CockpitCam.");
                    }

                    if (playerVRSetup != null)
                    {
                        // Store initial position, rotation, and scale of PlayerVRSetup
                        playerVRSetupStartPosition = playerVRSetup.transform.position;
                        playerVRSetupStartRotation = playerVRSetup.transform.rotation;

                        SaveOriginalParent(playerVRSetup);  // Save original parent of PlayerVRSetup

                        // Set PlayerVRSetup as child of cockpit cam and maintain scale
                        playerVRSetup.transform.SetParent(cockpitCam.transform, false);
                        playerVRSetup.transform.localRotation = Quaternion.identity;
                        logger.Info("PlayerVRSetup.PlayerRig set as child of CockpitCam.");
                    }
                }
                else
                {
                    logger.Error("CockpitCam object not found under Z!");
                }
                inFocusMode = true;  // Set focus mode flag
            }

            void EndFocusMode()
            {
                logger.Info("Exiting Focus Mode...");
                // Restore original parents of objects
                RestoreOriginalParent(playerCamera, "PlayerCamera");
                RestoreOriginalParent(playerVRSetup, "PlayerVRSetup.PlayerRig");
                StartAttractPattern();

                // Reset X to initial positions and rotations
                if (XObject != null)
                {
                    XObject.position = XStartPosition;
                    XObject.rotation = XStartRotation;
                }

                // Reset Y object to initial position and rotation
                if (YObject != null)
                {
                    YObject.position = YStartPosition;
                    YObject.rotation = YStartRotation;
                }
                // Reset Z object to initial position and rotation
                if (ZObject != null)
                {
                    ZObject.position = ZStartPosition;
                    ZObject.rotation = ZStartRotation;
                }

                // Reset cockpit cam to initial position and rotation
                if (cockpitCam != null)
                {
                    cockpitCam.transform.position = cockpitCamStartPosition;
                    cockpitCam.transform.rotation = cockpitCamStartRotation;
                }
                logger.Info("Resetting Positions");
                ResetPositions();

                inFocusMode = false;  // Clear focus mode flag
            }

            void HandleInput(ref bool inputDetected, ref bool throttleDetected) // Pass by reference
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

                // Ximput controller input
                if (XInput.IsConnected)
                {
                    primaryThumbstick = XInput.Get(XInput.Axis.LThumbstick);
                    Vector2 xboxPrimaryThumbstick = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                    Vector2 xboxSecondaryThumbstick = new Vector2(Input.GetAxis("RightStickHorizontal"), Input.GetAxis("RightStickVertical"));

                    // Combine VR and Xbox inputs
                    primaryThumbstick += xboxPrimaryThumbstick;
                    secondaryThumbstick += xboxSecondaryThumbstick;

                    // Get the trigger axis values
                    // Detect input from Xbox triggers

                    if (XInput.Get(XInput.Button.LIndexTrigger))
                    {
                        float rotateY = primaryThumbstickRotationMultiplier * Time.deltaTime;

                        if (currentRotationY - rotateY > -rotationLimitY)
                        {
                            YObject.Rotate(0, rotateY, 0);
                            currentRotationY -= rotateY;
                            throttleDetected = true;
                        }
                    }
                    if (XInput.Get(XInput.Button.RIndexTrigger))
                    {
                        float rotateY = keyboardVelocityX * Time.deltaTime;

                        if (currentRotationY + rotateY < rotationLimitY)
                        {
                            YObject.Rotate(0, -rotateY, 0);
                            currentRotationY += rotateY;
                            throttleDetected = true;
                        }
                    }
                    // Handle RB button press for plunger position
                    if (XInput.GetDown(XInput.Button.RShoulder) || Input.GetKeyDown(KeyCode.JoystickButton5))
                    {

                        inputDetected = true;
                    }

                    // Reset position on RB button release
                    if (XInput.GetUp(XInput.Button.RShoulder) || Input.GetKeyUp(KeyCode.JoystickButton5))
                    {

                    }

                    // Handle LB button press for plunger position
                    if (XInput.GetDown(XInput.Button.LShoulder) || Input.GetKeyDown(KeyCode.JoystickButton4))
                    {
                        inputDetected = true;
                    }

                    // Reset position on LB button release
                    if (XInput.GetUp(XInput.Button.LShoulder) || Input.GetKeyUp(KeyCode.JoystickButton4))
                    {

                    }
                }

                // Fire2 (positive Z rotation)
                if (Input.GetButton("Fire2") || XInput.Get(XInput.Button.B))
                {
                    float rotateY = keyboardVelocityX * Time.deltaTime;

                    if (currentRotationY + rotateY < rotationLimitY)
                    {
                        YObject.Rotate(0, -rotateY, 0);
                        currentRotationY += rotateY;
                        // inputDetected = true;
                    }
                }

                // Fire3
                // Handle positive X rotation when "Fire3" is pressed
                if (Input.GetButton("Fire3") || XInput.Get(XInput.Button.X))
                {
                    float rotateX = keyboardVelocityX * Time.deltaTime;
                    // Positive X rotation
                    if (currentRotationX + rotateX > -rotationLimitX)
                    {
                        XObject.Rotate(rotateX, 0, 0);
                        currentRotationX -= rotateX;
                        //   inputDetected = true;
                    }
                }

                // Fire3
                if (Input.GetButtonDown("Fire3") || XInput.GetDown(XInput.Button.X))
                {
                    // Set lights to bright
                    //ToggleTurboEmissive(true);
                    //ToggleBrightness1(true);
                    //ToggleBrightness2(true);
                    //ToggleLight1(true);
                    //ToggleLight2(true);
                    inputDetected = true;
                }
                // Reset position on button release
                if (Input.GetButtonUp("Fire3") || XInput.GetUp(XInput.Button.X))
                {
                    //ToggleTurboEmissive(false);
                    //ToggleBrightness1(false);
                    //ToggleBrightness2(false);
                    //ToggleLight1(false);
                    //ToggleLight2(false);
                    inputDetected = true;
                }
                // Jump
                if (Input.GetButtonDown("Jump") || XInput.GetDown(XInput.Button.Y))
                {
                    // Set lights to bright
                    //ToggleTurboEmissive(true);
                    //ToggleBrightness1(true);
                    //ToggleBrightness2(true);
                    //ToggleLight1(true);
                    //ToggleLight2(true);
                    inputDetected = true;
                }
                // Reset position on button release
                if (Input.GetButtonUp("Jump") || XInput.GetDown(XInput.Button.Y))
                {
                    //ToggleTurboEmissive(false);
                    //ToggleBrightness1(false);
                    //ToggleBrightness2(false);
                    //ToggleLight1(false);
                    //ToggleLight2(false);
                }
                // Fire1 (negative Z rotation)
                if (Input.GetButton("Fire1") || XInput.Get(XInput.Button.A))
                {
                    float rotateY = primaryThumbstickRotationMultiplier * Time.deltaTime;

                    if (currentRotationY - rotateY > -rotationLimitY)
                    {
                        YObject.Rotate(0, rotateY, 0);
                        currentRotationY -= rotateY;
                        inputDetected = true;
                    }
                }
                // Handle Z rotation for XObject and ControllerZ (Down Arrow or primaryThumbstick.y < 0)
                // Thumbstick direction: Left
                if ((Input.GetKey(KeyCode.LeftArrow) || primaryThumbstick.x < 0))
                {
                    if (currentRotationZ > -rotationLimitZ)
                    {
                        float rotateZ = (Input.GetKey(KeyCode.LeftArrow) ? keyboardVelocityZ : -primaryThumbstick.x * primaryThumbstickRotationMultiplier) * Time.deltaTime;
                        ZObject.Rotate(0, 0, rotateZ);
                        currentRotationZ -= rotateZ;
                        inputDetected = true;
                    }
                    if (currentControllerRotationZ > -controllerrotationLimitZ)
                    {
                        float controllerRotateZ = (Input.GetKey(KeyCode.LeftArrow) ? keyboardControllerVelocityZ : -primaryThumbstick.x * primaryThumbstickRotationMultiplier) * Time.deltaTime;
                        ControllerZ.Rotate(0, 0, controllerRotateZ);
                        currentControllerRotationZ -= controllerRotateZ;
                        inputDetected = true;
                    }
                }

                // Handle Z rotation for XObject and ControllerZ (Up Arrow or primaryThumbstick.y > 0)
                // Thumbstick direction: right
                if ((Input.GetKey(KeyCode.RightArrow) || primaryThumbstick.x > 0))
                {
                    if (currentRotationZ < rotationLimitZ)
                    {
                        float rotateZ = (Input.GetKey(KeyCode.RightArrow) ? keyboardVelocityZ : primaryThumbstick.x * primaryThumbstickRotationMultiplier) * Time.deltaTime;
                        ZObject.Rotate(0, 0, -rotateZ);
                        currentRotationZ += rotateZ;
                        inputDetected = true;
                    }
                    if (currentControllerRotationZ < controllerrotationLimitZ)
                    {
                        float controllerRotateZ = (Input.GetKey(KeyCode.RightArrow) ? keyboardControllerVelocityZ : primaryThumbstick.x * primaryThumbstickRotationMultiplier) * Time.deltaTime;
                        ControllerZ.Rotate(0, 0, -controllerRotateZ);
                        currentControllerRotationZ += controllerRotateZ;
                        inputDetected = true;
                    }
                }
                // Center the rotation if no input is detected (i think this is redundant)

                if (!inputDetected)
                {
                    CenterRotation();
                }
                if (!throttleDetected)
                {
                    CenterThrottle();
                }
            }

            void ResetPositions()
            {
                cockpitCam.transform.position = cockpitCamStartPosition;
                cockpitCam.transform.rotation = cockpitCamStartRotation;
                playerVRSetup.transform.position = playerVRSetupStartPosition;
                playerVRSetup.transform.rotation = playerVRSetupStartRotation;
                //playerVRSetup.transform.localScale = new Vector3(1f, 1f, 1f);
                playerCamera.transform.position = playerCameraStartPosition;
                playerCamera.transform.rotation = playerCameraStartRotation;
                //playerCamera.transform.localScale = new Vector3(1f, 1f, 1f);
                // Reset rotation allowances and current rotation values
                currentRotationX = 0f;
                currentRotationY = 0f;
                currentRotationZ = 0f;
            }
            void CenterThrottle()
            {
                // Center X-axis
                if (currentRotationX > 0)
                {
                    float unrotateX = Mathf.Min(centeringVelocityX * Time.deltaTime, currentRotationX);
                    XObject.Rotate(unrotateX, 0, 0);
                    currentRotationX -= unrotateX;
                }
                else if (currentRotationX < 0)
                {
                    float unrotateX = Mathf.Min(centeringVelocityX * Time.deltaTime, -currentRotationX);
                    XObject.Rotate(-unrotateX, 0, 0);
                    currentRotationX += unrotateX;
                }

                // Center Y-axis
                if (currentRotationY > 0)
                {
                    float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, currentRotationY);
                    YObject.Rotate(0, unrotateY, 0);
                    currentRotationY -= unrotateY;
                }
                else if (currentRotationY < 0)
                {
                    float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, -currentRotationY);
                    YObject.Rotate(0, -unrotateY, 0);
                    currentRotationY += unrotateY;
                }
            }

            void CenterRotation()
            {
                // Center Z-axis
                if (currentRotationZ > 0)
                {
                    float unrotateZ = Mathf.Min(centeringVelocityZ * Time.deltaTime, currentRotationZ);
                    ZObject.Rotate(0, 0, unrotateZ);
                    currentRotationZ -= unrotateZ;
                }
                else if (currentRotationZ < 0)
                {
                    float unrotateZ = Mathf.Min(centeringVelocityZ * Time.deltaTime, -currentRotationZ);
                    ZObject.Rotate(0, 0, -unrotateZ);
                    currentRotationZ += unrotateZ;
                }
                //Centering for contoller

                // Center Y-axis Controller rotation
                if (currentControllerRotationY > 0)
                {
                    float unrotateY = Mathf.Min(centeringControllerVelocityY * Time.deltaTime, currentControllerRotationY);
                    ControllerY.Rotate(0, unrotateY, 0);   // Rotating to reduce the rotation
                    currentControllerRotationY -= unrotateY;    // Reducing the positive rotation
                }
                else if (currentControllerRotationY < 0)
                {
                    float unrotateY = Mathf.Min(centeringControllerVelocityY * Time.deltaTime, -currentControllerRotationY);
                    ControllerY.Rotate(0, -unrotateY, 0);  // Rotating to reduce the rotation
                    currentControllerRotationY += unrotateY;    // Reducing the negative rotation
                }

                // Center X-Axis Controller rotation
                if (currentControllerRotationX > 0)
                {
                    float unrotateX = Mathf.Min(centeringControllerVelocityX * Time.deltaTime, currentControllerRotationX);
                    ControllerX.Rotate(unrotateX, 0, 0);   // Rotating to reduce the rotation
                    currentControllerRotationX -= unrotateX;    // Reducing the positive rotation
                }
                else if (currentControllerRotationX < 0)
                {
                    float unrotateX = Mathf.Min(centeringControllerVelocityX * Time.deltaTime, -currentControllerRotationX);
                    ControllerX.Rotate(-unrotateX, 0, 0);   // Rotating to reduce the rotation
                    currentControllerRotationX += unrotateX;    // Reducing the positive rotation
                }

                // Center Z-axis Controller rotation
                if (currentControllerRotationZ > 0)
                {
                    float unrotateZ = Mathf.Min(centeringControllerVelocityZ * Time.deltaTime, currentControllerRotationZ);
                    ControllerZ.Rotate(0, 0, unrotateZ);   // Rotating to reduce the rotation
                    currentControllerRotationZ -= unrotateZ;    // Reducing the positive rotation
                }
                else if (currentControllerRotationZ < 0)
                {
                    float unrotateZ = Mathf.Min(centeringControllerVelocityZ * Time.deltaTime, -currentControllerRotationZ);
                    ControllerZ.Rotate(0, 0, -unrotateZ);   // Rotating to reduce the rotation
                    currentControllerRotationZ += unrotateZ;    // Reducing the positive rotation
                }
            }

            // Check if object is found and log appropriate message
            void CheckObject(GameObject obj, string name)
            {
                if (obj == null)
                {
                    logger.Error($"{name} not found!");
                }
                else
                {
                    logger.Info($"{name} found.");
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
                    logger.Info($"{name} restored to original parent.");
                }
            }

            // Unset parent of object and log appropriate message
            void UnsetParentObject(GameObject obj, string name)
            {
                if (obj != null)
                {
                    obj.transform.SetParent(null);
                    logger.Info($"{name} unset from parent.");
                }
            }

            void HandleTransformAdjustment()
            {
                // Choose target camera: use vrCam if available, otherwise fallback to cockpitCam
                var cam = vrCam != null ? vrCam : cockpitCam;

                if (cam != null)
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
                    vrCamStartRotation = vrCam.transform.rotation;
                }
                else if (cockpitCam != null)
                {
                    cockpitCamStartPosition = cockpitCam.transform.localPosition;
                    cockpitCamStartRotation = cockpitCam.transform.rotation;
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

        public void TurnAllOff()
        {
            foreach (var renderer in HazzardEmissiveObjects)
            {
                ToggleEmissiveRenderer(renderer, false);
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

                    // logger.Info($"{targetObject.name} emissive state set to {(isActive ? "ON" : "OFF")}.");
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

        void ToggleLight(Light targetLight, bool isActive) // Toggle light dynamically
        {
            if (targetLight != null)
            {
                targetLight.enabled = isActive;
                // logger.Debug($"{gameObject.name} {targetLight.name} light turned {(isActive ? "on" : "off")}.");
            }
            else
            {
                // logger.Debug($"{gameObject.name}{targetLight.name} light component is not assigned.");
            }
        }

        // Method to log missing objects
        void LogMissingObject(Renderer[] emissiveObjects, string arrayName)
        {
            for (int i = 0; i < emissiveObjects.Length; i++)
            {
                if (emissiveObjects[i] == null)
                {
                    logger.Debug($"{arrayName} object at index {i} not found under ControllerZ.");
                }
            }
        }

        IEnumerator AttractPattern()
        {
            while (true)
            {
                ToggleEmissive(HazzardEmissiveObjects[0], true);
                //   ToggleEmissive(HazzardEmissiveObjects[1], false);
                yield return new WaitForSeconds(FlashDuration);

                ToggleEmissive(HazzardEmissiveObjects[0], false);
                //    ToggleEmissive(HazzardEmissiveObjects[1], true);
                yield return new WaitForSeconds(FlashDuration);
            }
        }

        IEnumerator DangerPattern()
        {
            while (true)
            {
                ToggleEmissive(HazzardEmissiveObjects[0], true);
                //    ToggleEmissive(HazzardEmissiveObjects[1], true);
                yield return new WaitForSeconds(DangerDuration);

                ToggleEmissive(HazzardEmissiveObjects[0], false);
                //   ToggleEmissive(hazzardEmissiveObjects[1], false);
                yield return new WaitForSeconds(DangerDelay);
            }
        }

        void ToggleEmissive(Renderer renderer, bool isOn)
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

            // Start the attract pattern for front, left, and right
            AttractCoroutine = StartCoroutine(AttractPattern());
        }

        public void StartDangerPattern()
        {
            // Stop any currently running coroutines
            StopCurrentPatterns();

            // Start the danger pattern for front, left, and right
            DangerCoroutine = StartCoroutine(DangerPattern());
        }

        private void StopCurrentPatterns()
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
    }
}
