using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System;
using System.Runtime.InteropServices;
using Button = UnityEngine.UI.Button;
using WIGU;

namespace WIGUx.Modules.aburnerMotionSim
{
    public class aburnerMotionSimController : MonoBehaviour
    {

        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        [Header("Object Settings")]

        private Transform StartObject; // Reference to the start button object
        private Transform StickObject; // Reference to the stick mirroring object
        private Transform ThrottleObject; // Reference to the left stick mirroring object
        private Transform XObject; // Reference to the main X object
        private Transform YObject; // Reference to the main Y object
        private Transform ZObject; // Reference to the main Z object
        private Transform ControllerX; // Reference to the main animated Controller (wheel)
        private Transform ControllerY; // Reference to the main animated Controller (wheel)
        private Transform ControllerZ; // Reference to the main animated Controller (wheel)
        private Transform Fire1Object; // Reference to the fire left light
        private Transform Fire2Object; // Reference to the fire right light
        private Transform Danger1Object; // Reference to the danger1 light
        private Transform Danger2Object; // Reference to the danger2 light
        private GameObject cockpitCam;    // Reference to the cockpit camera
        private GameObject vrCam;    // Reference to the cockpit camera
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
        private readonly float rotationSmoothness = 5f;  //sets the smoothness of the rotation
        private readonly float thumbstickVelocity = 30f;  // Velocity for Thumbsticks
        private readonly float thumbstickVelocityX = 25f; // Velocity for Thumbsticks
        private readonly float thumbstickVelocityY = 25f; // Velocity for Thumbsticks
        private readonly float thumbstickVelocityZ = 25f; // Velocity for Thumbsticks
        private readonly float keyboardVelocityX = 45f;  // Velocity for keyboard input
        private readonly float keyboardVelocityY = 45f;  // Velocity for keyboard input
        private readonly float keyboardVelocityZ = 45f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityX = 150.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityY = 150.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityZ = 150.5f;  // Velocity for keyboard input
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
        private float rotationLimitX = 15f;  // Rotation limit for X-axis
        private float rotationLimitY = 0f;  // Rotation limit for Y-axis
        private float rotationLimitZ = 25f;  // Rotation limit for Z-axis
        private float ControllerrotationLimitX = 0f;  // Rotation limit for X-axis (stick or wheel)
        private float ControllerrotationLimitY = 0f;  // Rotation limit for Y-axis (stick or wheel)
        private float ControllerrotationLimitZ = 0f;  // Rotation limit for Z-axis (stick or wheel)

        [Header("Position Settings")]     // Initial positions setup
        private Vector3 XStartPosition;  // Initial X position for resetting
        private Vector3 YStartPosition;  // Initial Y positions for resetting
        private Vector3 ZStartPosition;  // Initial Z positions for resetting
        private Vector3 StickStartPosition; // Initial Throttle positions for resetting
        private Vector3 ThrottleStartPosition; // Initial Throttle positions for resetting
        private Vector3 ControllerXStartPosition; // Initial controller positions for resetting
        private Vector3 ControllerYStartPosition; // Initial controller positions for resetting
        private Vector3 ControllerZStartPosition; // Initial controller positions for resetting
        private Vector3 playerCameraStartPosition;  // Initial Player Camera positions for resetting
        private Vector3 playerVRSetupStartPosition;  // Initial PlayerVR positions for resetting
        private Vector3 cockpitCamStartPosition;  // Initial cockpitCam positionsfor resetting
        private Vector3 vrCamStartPosition;    // Initial vrCam positionsfor resetting


        [Header("Rotation Settings")]     // Initial rotations setup
        private Quaternion XStartRotation;  // Initial X rotation for resetting
        private Quaternion YStartRotation;  // Initial Y rotation for resetting
        private Quaternion ZStartRotation;  // Initial Z rotation for resetting
        private Quaternion ControllerXStartRotation; // Initial controlller X rotations for resetting
        private Quaternion ControllerYStartRotation; // Initial controlller Y rotations for resetting
        private Quaternion ControllerZStartRotation; // Initial controlller Z rotations for resetting
        private Quaternion StickStartRotation;  // Initial Stick rotation for resetting
        private Quaternion ThrottleStartRotation;  // Initial Throttle rotation for resetting
        private Quaternion playerCameraStartRotation;  // Initial Player Camera rotation for resetting
        private Quaternion playerVRSetupStartRotation;  // Initial PlayerVR rotation for resetting
        private Quaternion cockpitCamStartRotation;  // Initial cockpitCam rotation for resetting
        private Quaternion vrCamStartRotation;      // Initial VRCam rotation for resetting

        [Header("Lights and Emissives")]     // Setup Emissive and Lights
        Dictionary<string, int> lastLampStates = new Dictionary<string, int>
             {
               { "lamp0", 0 }, { "lamp1", 0 }, { "lamp2", 0 }, { "lamp3", 0 }
             };
        private Light firelight1;
        private Light firelight2;
        private Light locklight;
        private Light startlight;
        private Color emissiveOnColor = Color.white;    // Emissive colors
        private Color emissiveOffColor = Color.black;

        [Header("Timers and States")]  // Store last states and timers
        private float lightDuration = 0.35f;
        private float attractFlashDuration = 0.7f;
        private float attractFlashDelay = 0.7f;
        private float dangerFlashDuration = 0.3f;
        private float dangerFlashDelay = 0.3f;
        private bool isHigh = false; //set the high gear flag

        private Coroutine dangerCoroutine; // Coroutine variable to control the focused danger mode
        private Coroutine attractCoroutine; // Coroutine variable to control the attract mode
        private Light[] lights;        //array of lights

        [Header("Collider Triggers")]
        [SerializeField] private Collider cockpitCollider;

        [Header("Rom Stuff")]     // Check for compatible titles
        private GameSystem gameSystem;  // Cached GameSystem for this cabinet.
        private string insertedGameName = string.Empty;
        private string controlledGameName = string.Empty;
        private string filePath;
        private string configPath;
        private Dictionary<GameObject, Transform> originalParents = new Dictionary<GameObject, Transform>();  // Dictionary to store original parents of objects
       
        [Header("Control UI")]
        public GameObject bindingUI;  // UI panel
        public Button saveButton;     // UI savebutton
        public Button rebindButton;   // UI rebind button
        public Transform bindingsContainer; // UI container for binding rows
        public GameObject bindingEntryPrefab;// prefab for each binding row
        private string selectedBinding;  // currently selected action
        private bool waitingForInput = false;// indicates if user is rebinding   
        public Slider sensitivitySlider;       // optional slider for sensitivity
        public Text sensitivityValueText;      // optional text for numeric sensitivity
        private bool inFocusMode = false;  // Flag to track focus mode state
        private Dictionary<string, InputBinding> Controls; // main dictionary for input
        private Dictionary<string, GameObject> bindingEntries = new Dictionary<string, GameObject>();

        void Start()
        {
            //WriteLampConfig(filePath);
            CheckInsertedGameName();
            CheckControlledGameName();
            filePath = $"./Emulators/MAME/outputs/{insertedGameName}.txt";
            configPath = $"./Emulators/MAME/inputs/{insertedGameName}.ini";


            Debug.Log($"Checking INI file at: {configPath}");
            if (File.Exists(configPath))
            {
                logger.Debug($"{gameObject.name} INI file FOUND!");
            }
            else
            {
                logger.Error("INI file is NOT found at this path!: {ConfigPath}");
            }
            CabinetControlModule.SetActiveConfig(configPath);
            LoadConfig();
            if (bindingUI == null)
            {
                logger.Debug($"{gameObject.name} CRITICAL: bindingUI is NULL! Make sure it is assigned in the Unity Inspector.");
            }
            else
            {
                bindingUI.SetActive(false);
            }
            //  Check if `bindingsContainer` is null
            if (bindingsContainer == null)
            {
                logger.Warning("CRITICAL: bindingsContainer is NULL! Assign it in the Unity Inspector.");
            }
            if (saveButton != null)
                saveButton.onClick.AddListener(SaveConfig);
            else
                logger.Error("saveButton is not assigned!");

            if (rebindButton != null)
                rebindButton.onClick.AddListener(StartRebinding);
            else
                logger.Error("rebindButton is not assigned!");

            PopulateBindingsList();
            WriteLampConfig(filePath);
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
                Debug.LogWarning("No VR Devices found. No SteamVR or OVR present)");
            }

            // Find X object in hierarchy
            XObject = transform.Find("X");
            if (XObject != null)
            {
                logger.Debug($"{gameObject.name} X object found.");
                XStartPosition = XObject.position;
                XStartRotation = XObject.rotation;
            }
            else
            {
                logger.Debug($"{gameObject.name} X object not found.");
            }
            // Find y object under X
            YObject = XObject.Find("Y");
            if (YObject != null)
            {
                logger.Debug($"{gameObject.name} Y object found.");
                YStartPosition = YObject.position;
                YStartRotation = YObject.rotation;
            }
            else
            {
                logger.Debug($"{gameObject.name} Y object not found.");
            }
            // Find Z object under Y
            ZObject = YObject.Find("Z");
            if (ZObject != null)
            {
                logger.Debug($"{gameObject.name} Z object found.");
                ZStartPosition = ZObject.position;
                ZStartRotation = ZObject.rotation;
            }
            else
            {
                logger.Debug($"{gameObject.name} Z object not found.");
            }
            // Find vrCam under seatMotor
            vrCam = ZObject.Find("eyes/vrcam")?.gameObject;
            if (vrCam != null)
            {
                logger.Debug($"{gameObject.name} vrCam object found.");

                // Store initial position and rotation of  vrCam
                vrCamStartPosition = vrCam.transform.localPosition;
                vrCamStartRotation = vrCam.transform.rotation;
            }
            else
            {
                logger.Error("vrcam object not found under seatMotor!");
            }
            // Find cockpit camera under cockpit
            cockpitCam = ZObject.Find("eyes/cockpitcam")?.gameObject;
            if (cockpitCam != null)
            {
                logger.Debug($"{gameObject.name} Cockpitcam object found.");
                cockpitCamStartPosition = cockpitCam.transform.position;
                cockpitCamStartRotation = cockpitCam.transform.rotation;
            }
            else
            {
                logger.Debug($"{gameObject.name} Cockpitcam object not found.");
            }
            // Find ControllerX under Z
            ControllerX = ZObject.Find("ControllerX");
            if (ControllerX != null)
            {
                logger.Debug($"{gameObject.name} ControllerX object found.");
                ControllerXStartPosition = ControllerX.position;
                ControllerXStartRotation = ControllerX.rotation;
            }
            else
            {
                logger.Debug($"{gameObject.name} ControllerX object not found.");
            }
            // Find ControllerY under ControllerX
            ControllerY = ControllerX.Find("ControllerY");
            if (ControllerY != null)
            {
                logger.Debug($"{gameObject.name} ControllerY object found.");
                ControllerYStartPosition = ControllerY.position;
                ControllerYStartRotation = ControllerY.rotation;
            }
            else
            {
                logger.Debug($"{gameObject.name} ControllerY object not found.");
            }
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
            // Find Throttle under Y
            ThrottleObject = ZObject.Find("Throttle");
            if (ThrottleObject != null)
            {
                logger.Debug($"{gameObject.name} Throttle object found.");
                ThrottleStartPosition = ThrottleObject.position;
                ThrottleStartRotation = ThrottleObject.rotation;
            }
            else
            {
                logger.Debug($"{gameObject.name} Throttle object not found.");
            }
            // Find Stick under Z
            StickObject = ControllerZ.Find("Stick");
            if (StickObject != null)
            {
                logger.Debug($"{gameObject.name} Stick object found.");
                // Store initial position and rotation of the Stick
                StickStartPosition = StickObject.position;
                StickStartRotation = StickObject.rotation;
            }
            else
            {
                logger.Debug($"{gameObject.name} Stick object not found.");
            }
            // Find startObject object under Y
            StartObject = XObject.Find("Start");
            if (StartObject != null)
            {
                logger.Debug($"{gameObject.name} Start object found.");
                // Ensure the start object is initially off
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
            /*
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
            // Find Shifter under Z
            ShifterObject = transform.Find("Shifter");
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
                        // Find Gas under Z
            GasObject = transform.Find("Gas");
            if (GasObject != null)
            {
                logger.Debug($"{gameObject.name} Gas object found.");
                GasStartPosition = GasObject.position;
                GasStartRotation = GasObject.rotation;
            }
            else
            {
                logger.Debug($"{gameObject.name} Gas object not found.");
            }
            // Find Brake under Z
            BrakeObject = transform.Find("Brake");
            if (BrakeObject != null)
            {
                logger.Debug($"{gameObject.name} Brake object found.");
                // Store initial position and rotation of the Brake
                BrakeStartPosition = BrakeObject.position;
                BrakeStartRotation = BrakeObject.rotation;
            }
            else
            {
                logger.Debug($"{gameObject.name} Brake object not found.");
            }
            // Find Danger2Object object under Z
            Danger2Object = XObject.Find("Danger2");
            if (Danger2Object != null)
            {
                logger.Debug($"{gameObject.name} Danger2 object found.");
                // Ensure the Danger object is initially off
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
            */
            // Find Fire1Object object under X
            Fire1Object = XObject.Find("Fire1");
            if (Fire1Object != null)
            {
                logger.Debug($"{gameObject.name} Fire1 object found.");
                // Ensure the fire1 object is initially off
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
            // Find Fire2Object object under X
            Fire2Object = XObject.Find("Fire2");
            if (Fire2Object != null)
            {
                logger.Debug($"{gameObject.name} Fire2 object found.");
                // Ensure the fire2 object is initially off
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
                else if (light.gameObject.name == "startlight")
                {
                    startlight = light;
                    logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                }
                else if (light.gameObject.name == "locklight")
                {
                    locklight = light;
                    logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                }
                else
                {
                    logger.Debug($"{gameObject.name} Excluded Light found in object: " + light.gameObject.name);
                }
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
                        logger.Info($"cockpitCollider found by name: {cockpitCollider.name}");
                    else
                        logger.Error($"GameObject '{colliderName}' found but has no Collider component!");
                }
                else
                {
                    logger.Error($"No GameObject named '{colliderName}' found in scene.");
                }
            }
            if (cockpitCollider != null)
            {
                logger.Debug($"Using cockpitCollider: {cockpitCollider.name}");
                if (!cockpitCollider.isTrigger)
                    logger.Debug($"{gameObject.name} cockpitCollider is not set as a trigger!");
            }
            //  StartAttractPattern();
            //  ToggleLight(firelight1, false);
            // ToggleLight(firelight2, false);
            //  ToggleEmissive(fire1Object.gameObject, false);
            //  ToggleEmissive(fire2Object.gameObject, false);
            //   ToggleEmissive(danger1Object.gameObject, false);
            //  ToggleEmissive(danger2Object.gameObject, false);
        }

        void Update()
        {

            bool inputDetected = false;  // Initialize for centering for keyboard input
            bool throttleDetected = false;// Initialize for centering for keyboard input
            ReadData();
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
                ApplyInputs();
                HandleTransformAdjustment();
                //   MapThumbsticks(ref inputDetected, ref throttleDetected);
                //    MapButtons(ref inputDetected, ref throttleDetected);

                if (Input.GetKeyDown(KeyCode.B) && Input.GetKey(KeyCode.LeftControl))
                {
                    CabinetControlModule.SetActiveConfig(configPath);
                    bindingUI.SetActive(!bindingUI.activeSelf);
                }
            }
        }

        void WriteLampConfig(string filePath)
        {
            // no changes needed, we keep your original logic
            string content = "lamp0 = 0\n" +
                             "lamp1 = 0\n" +
                             "lamp2 = 0\n" +
                             "lamp3 = 0\n";
            try
            {
                File.WriteAllText(filePath, content);
                Debug.Log("File written to: " + filePath);
            }
            catch (IOException e)
            {
                Debug.LogError("File write failed: " + e.Message);
            }
        }

        void StartFocusMode()
        {
            logger.Info($"{gameObject.name} Compatible Rom Dectected, Powering Up Motors...");
            logger.Info($"{gameObject.name} Sega After Burner Motion Sim starting...");
            logger.Info($"{gameObject.name} GET READY!!...");
            //  ToggleEmissive(fire1Object.gameObject, false);
            //  ToggleEmissive(fire2Object.gameObject, false);
            //   StopCurrentPatterns();
            //  ToggleEmissive(startObject.gameObject, true);
            //StartDangerPattern();
            if (cockpitCam != null)
            {
              cockpitCam.transform.localPosition = cockpitCamStartPosition;
              cockpitCam.transform.rotation = cockpitCamStartRotation;
            }
            if (vrCam != null)
            {
                vrCam.transform.localPosition = vrCamStartPosition;
                vrCam.transform.rotation = vrCamStartRotation;
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
                logger.Debug($"{gameObject.name} Containment check - bounds.Contains: {boundsContains}, ClosestPoint==pos: {inside}");
            }

            if (cockpitCollider != null && inside)
            {
                if (playerVRSetup == null)
                {
                    // Parent and apply offset to PlayerCamera
                    SaveOriginalParent(playerCamera);
                    playerCamera.transform.SetParent(cockpitCam.transform, true);
                    logger.Debug($"{gameObject.name} PlayerCamera set as child of CockpitCam.");
                }
                if (playerVRSetup != null)
                {
                    // Parent and apply offset to PlayerVRSetup
                    SaveOriginalParent(playerVRSetup);
                    playerVRSetup.transform.SetParent(vrCam.transform, true);
                    logger.Debug($"{gameObject.name} PlayerVRSetup.PlayerRig set as child of VR Camera.");
                    logger.Info($"{gameObject.name} Dont Get Dizzy!");
                }
            }
            else
            {
                logger.Info($"{gameObject.name} Player is not aboard the ride, Starting Without the player aboard.");
            }

            inFocusMode = true;
        }

        void EndFocusMode()
        {
            logger.Info($"{gameObject.name} Exiting Focus Mode...");
            RestoreOriginalParent(playerCamera, "PlayerCamera");
            RestoreOriginalParent(playerVRSetup, "PlayerVRSetup.PlayerRig");
            ResetPositions();

            //    ToggleLight(firelight1, false);
            //    ToggleLight(firelight2, false);
            //    ToggleEmissive(fire1Object.gameObject, false);
            //    ToggleEmissive(fire2Object.gameObject, false);
            //    StartAttractPattern();
            inFocusMode = false;
        }
        void ResetPositions()      // Reset objects and cockpit cam to initial position and rotation
        {
            bool inside = false; // Check containment
            logger.Info($"{gameObject.name} Resetting Positions");
            // Reset X to initial positions and rotations
            if (XObject != null)
            {
                XObject.transform.position = XStartPosition;
                XObject.transform.rotation = XStartRotation;
            }

            // Reset Y object to initial position and rotation
            if (YObject != null)
            {
                YObject.transform.position = YStartPosition;
                YObject.transform.rotation = YStartRotation;
            }
            // Reset z object to initial position and rotation
            if (ZObject != null)
            {
                ZObject.transform.position = ZStartPosition;
                ZObject.transform.rotation = ZStartRotation;
            }
            if (StickObject != null)
            {
                StickObject.transform.position = StickStartPosition;
                StickObject.transform.rotation = StickStartRotation;
            }
            if (ThrottleObject != null)
            {
                ThrottleObject.transform.position = ThrottleStartPosition;
                ThrottleObject.transform.rotation = ThrottleStartRotation;
            }
            /*
            if (LStickObject != null)
            {
                LStickObject.position = LStickStartPosition;
                LStickObject.rotation = LStickStartRotation;
            }
            if (RStickObject != null)
            {
                rStickObject.position = RStickStartPosition;
                rStickObject.rotation = RStickStartRotation;
            }
            if (WheelObject != null)
            {
                WheelObject.position = WheelStartPosition;
                WheelObject.rotation = WheelStartRotation;
            }
            if (ShifterObject != null)
            {
                ShifterObject.position = ShifterStartPosition;
                ShifterObject.rotation = ShifterStartRotation;
            }
              if (GasObject != null)
            {
                GasObject.position = GasStartPosition;
                GasObject.rotation = GasStartRotation;
            }
            if (BrakeObject != null)
            {
                BrakeObject.position = BrakeStartPosition;
                BrakeObject.rotation = BrakeStartRotation;
            }
            */

            if (cockpitCollider != null && inside)
            {
                if (cockpitCam != null)
                {
                    cockpitCam.transform.position = cockpitCamStartPosition;
                    cockpitCam.transform.rotation = cockpitCamStartRotation;
                }
                if (vrCam != null)
                {
                    vrCam.transform.position = vrCamStartPosition;
                    vrCam.transform.rotation = vrCamStartRotation;
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
                logger.Info($"{gameObject.name} Player was not aboard the ride, skipping reset.");
            }

            currentRotationX = 0f;
            currentRotationY = 0f;
            currentRotationZ = 0f;
            currentControllerRotationX = 0f;
            currentControllerRotationY = 0f;
            currentControllerRotationZ = 0f;
        }
        void ReadData()
        {
            if (!File.Exists(filePath))
            {
                logger.Warning($"{gameObject.name} aburner2 outputs file not found: {filePath}");
                return;
            }

            string data;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader sr = new StreamReader(fs))
            {
                data = sr.ReadToEnd();
            }

            // Split file into lines
            string[] lines = data.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, int> currentLampStates = new Dictionary<string, int>
    {
        { "lamp0", 0 }, { "lamp1", 0 }, { "lamp2", 0 }, { "lamp3", 0 }
    };

            foreach (string line in lines)
            {
                string[] parts = line.Split(new[] { '=' }, 2);
                if (parts.Length == 2)
                {
                    string key = parts[0].Trim();
                    if (int.TryParse(parts[1].Trim(), out int value))
                    {
                        currentLampStates[key] = value;
                    }
                }
            }

            // 🔹 Ensure lastLampStates is initialized
            if (lastLampStates == null)
            {
                lastLampStates = new Dictionary<string, int>
        {
            { "lamp0", 0 }, { "lamp1", 0 }, { "lamp2", 0 }, { "lamp3", 0 }
        };
                Debug.LogWarning("lastLampStates was NULL. Re-initialized.");
            }

            // 🔹 Check and process each lamp safely
            ProcessLampState("lamp0", currentLampStates);
            ProcessLampState("lamp1", currentLampStates);
            ProcessLampState("lamp2", currentLampStates);
            ProcessLampState("lamp3", currentLampStates);
        }

        // 🔹 Helper function for safe lamp processing
        void ProcessLampState(string lampKey, Dictionary<string, int> currentStates)
        {
            if (!lastLampStates.ContainsKey(lampKey))
            {
                lastLampStates[lampKey] = 0;
                Debug.LogWarning($"Added missing key '{lampKey}' to lastLampStates.");
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
                            Debug.LogWarning($"No processing function for '{lampKey}'");
                            break;
                    }
                }
            }
            else
            {
                Debug.LogError($"Lamp key '{lampKey}' not found in current states.");
            }
        }

        // Individual function for lamp0
        void ProcessLamp0(int state)
            {
                Debug.Log($"Lamp 0 updated: {state}");

                // Update lights
                if (locklight) locklight.enabled = (state == 1);
                // Update emissive material
                if (Fire2Object) SetEmissive(Fire2Object, state == 1);
            }
            // Individual function for lamp1
            void ProcessLamp1(int state)
            {
                Debug.Log($"Lamp 1 updated: {state}");

                // Update lights

                // Update emissive material

            }
            // Individual function for lamp2
            void ProcessLamp2(int state)
            {
                Debug.Log($"Lamp 2 updated: {state}");

                // Update lights

                if (firelight1) firelight1.enabled = (state == 1);
                if (firelight2) firelight2.enabled = (state == 1);
                // Update emissive material
                if (Fire1Object) SetEmissive(Fire1Object, state == 1);
            }

            // Individual function for lamp3
            void ProcessLamp3(int state)
            {
                Debug.Log($"Lamp 3 updated: {state}");

                // Update lights
                if (startlight) startlight.enabled = (state == 1);
                // Update emissive material
                if (StartObject) SetEmissive(StartObject, state == 1);
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


        void CheckObject(GameObject obj, string name)     // Check if object is found and log appropriate message
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
                        logger.Info($"Object {obj.name} was in the root and has no parent.");
                    }
                }
            }

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

                // If the original parent was NULL, place the object back in the root
                if (originalParent == null)
                {
                    obj.transform.SetParent(null, true);  // Moves it back to the root
                    logger.Debug($"{name} restored to root.");
                }
                else
                {
                    obj.transform.SetParent(originalParent, false);
                    logger.Debug($"{name} restored to original parent: {originalParent.name}");
                }
            }

        void CenterThrottle()
        {
            if (currentRotationX > 0)            // Center X-axis
            {
                float unrotateX = Mathf.Min(centeringVelocityX * Time.deltaTime, currentRotationX);
                YObject.Rotate(unrotateX, 0, 0);
                currentRotationX -= unrotateX;
            }
            else if (currentRotationX < 0)      // Center X-axis
            {
                float unrotateX = Mathf.Min(centeringVelocityX * Time.deltaTime, -currentRotationX);
                YObject.Rotate(-unrotateX, 0, 0);
                currentRotationX += unrotateX;
            }
            if (currentRotationY > 0)            // Center Y-axis
            {
                float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, currentRotationY);
                YObject.Rotate(0, unrotateY, 0);
                currentRotationY -= unrotateY;
            }
            else if (currentRotationY < 0)      // Center Y-axis
            {
                float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, -currentRotationY);
                YObject.Rotate(0, -unrotateY, 0);
                currentRotationY += unrotateY;
            }
        }
        void CenterRotation()
        {
            // Center primary object's X-axis rotation
            if (currentRotationX > 0)
            {
                float unrotateX = Mathf.Min(centeringVelocityX * Time.deltaTime, currentRotationX);
                YObject.Rotate(unrotateX, 0, 0);
                currentRotationX -= unrotateX;
            }
            else if (currentRotationX < 0)
            {
                float unrotateX = Mathf.Min(centeringVelocityX * Time.deltaTime, -currentRotationX);
                YObject.Rotate(-unrotateX, 0, 0);
                currentRotationX += unrotateX;
            }
            // Center primary object's Y-axis rotation
            if (currentRotationY > 0)
            {
                float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, currentRotationY);
                XObject.Rotate(0, unrotateY, 0);
                currentRotationY -= unrotateY;
            }
            else if (currentRotationY < 0)
            {
                float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, -currentRotationY);
                XObject.Rotate(0, -unrotateY, 0);
                currentRotationY += unrotateY;
            }
            // Center primary object's Z-axis rotation
            if (currentRotationZ > 0)
            {
                float unrotateZ = Mathf.Min(centeringVelocityZ * Time.deltaTime, currentRotationZ);
                XObject.Rotate(0, 0, unrotateZ);
                currentRotationZ -= unrotateZ;
            }
            else if (currentRotationZ < 0)
            {
                float unrotateZ = Mathf.Min(centeringVelocityZ * Time.deltaTime, -currentRotationZ);
                XObject.Rotate(0, 0, -unrotateZ);
                currentRotationZ += unrotateZ;
            }

            // Center Controller object's X-axis rotation
            if (currentControllerRotationX > 0)
            {
                float unrotateX = Mathf.Min(centeringControllerVelocityX * Time.deltaTime, currentControllerRotationX);
                ControllerX.Rotate(unrotateX, 0, 0);
                currentControllerRotationX -= unrotateX;
            }
            else if (currentControllerRotationX < 0)
            {
                float unrotateX = Mathf.Min(centeringControllerVelocityX * Time.deltaTime, -currentControllerRotationX);
                ControllerX.Rotate(-unrotateX, 0, 0);
                currentControllerRotationX += unrotateX;
            }
            // Center Controller object's Y-axis rotation
            if (currentControllerRotationY > 0)
            {
                float unrotateY = Mathf.Min(centeringControllerVelocityY * Time.deltaTime, currentControllerRotationY);
                ControllerY.Rotate(0, unrotateY, 0);
                currentControllerRotationY -= unrotateY;
            }
            else if (currentControllerRotationY < 0)
            {
                float unrotateY = Mathf.Min(centeringControllerVelocityY * Time.deltaTime, -currentControllerRotationY);
                ControllerY.Rotate(0, -unrotateY, 0);
                currentControllerRotationY += unrotateY;
            }
            // Center Controller object's Z-axis rotation
            if (currentControllerRotationZ > 0)
            {
                float unrotateZ = Mathf.Min(centeringControllerVelocityZ * Time.deltaTime, currentControllerRotationZ);
                ControllerZ.Rotate(0, 0, unrotateZ);
                currentControllerRotationZ -= unrotateZ;
            }
            else if (currentControllerRotationZ < 0)
            {
                float unrotateZ = Mathf.Min(centeringControllerVelocityZ * Time.deltaTime, -currentControllerRotationZ);
                ControllerZ.Rotate(0, 0, -unrotateZ);
                currentControllerRotationZ += unrotateZ;
            }
        }

        // Method to enable or disable emissive based on the Transform reference
        void SetEmissive(Transform emissiveTransform, bool enable)
            {
                if (emissiveTransform != null)
                {
                    Renderer renderer = emissiveTransform.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        // If the renderer is disabled, enable it
                        if (!renderer.enabled)
                        {
                            renderer.enabled = true;
                            logger.Info($"{emissiveTransform.name} Renderer was disabled, now enabled.");
                        }

                        // Enable or disable the emission keyword based on the flag
                        if (enable)
                        {
                            renderer.material.EnableKeyword("_EMISSION");
                            logger.Info($"{emissiveTransform.name} emission enabled.");
                        }
                        else
                        {
                            renderer.material.DisableKeyword("_EMISSION");
                            logger.Info($"{emissiveTransform.name} emission disabled.");
                        }
                    }
                    else
                    {
                        logger.Warning($"{emissiveTransform.name} does not have a Renderer component.");
                    }
                }
                else
                {
                    logger.Warning($"Emissive object {emissiveTransform?.name} not found.");
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

                        //    logger.Info($"{targetObject.name} emissive state set to {(isActive ? "ON" : "OFF")} with color {emissionColor} and intensity {intensity}.");
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

            void ToggleLight(Light targetLight, bool isActive) // Toggle light dynamically
            {
                if (targetLight != null)
                {
                    targetLight.enabled = isActive;
                    // logger.Info($"{targetLight.name} light turned {(isActive ? "on" : "off")}.");
                }
                else
                {
                    // logger.Debug($"{targetLight.name} light component is not assigned.");
                }
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

            IEnumerator attractPattern()  //Pattern For Attract Mode
            {
                while (true)
                {
                    SetEmissive(StartObject, false);
                    SetEmissive(Fire1Object, false);
                    SetEmissive(Fire2Object, false);
                    yield return new WaitForSeconds(attractFlashDuration);
                    SetEmissive(StartObject, true);
                    yield return new WaitForSeconds(attractFlashDuration);
                }
            }
            IEnumerator dangerPattern() //Pattern For Focused Danger Mode
            {
                while (true)
                {
                    SetEmissive(StartObject, true);
                    SetEmissive(Fire1Object, false);
                    SetEmissive(Fire2Object, false);
                    //  ToggleEmissive(danger1Object.gameObject, true);
                    //   ToggleEmissive(danger2Object.gameObject, false);
                    //    yield return new WaitForSeconds(dangerFlashDelay);
                    //    ToggleEmissive(danger1Object.gameObject, false);
                    //   ToggleEmissive(danger2Object.gameObject, true);
                    //   yield return new WaitForSeconds(dangerFlashDelay);
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

        //---------------------------------------------------------------------
        // INI LOAD
        //---------------------------------------------------------------------
        void LoadConfig()
        {
            Debug.Log($"LOAD CONFIG - Checking INI file at: {configPath}");

            //  Ensure Controls is initialized before doing anything
            if (Controls == null)
            {
                Controls = new Dictionary<string, InputBinding>();
            }

            //  If the file is missing, create defaults
            if (!File.Exists(configPath))
            {
                Debug.LogError("LOAD CONFIG - INI file not found! Creating default values.");

                Controls["ThrottleUp"] = new InputBinding();
                Controls["ThrottleDown"] = new InputBinding();
                Controls["StickLeft"] = new InputBinding();
                Controls["StickRight"] = new InputBinding();
                Controls["StickForward"] = new InputBinding();
                Controls["StickBackward"] = new InputBinding();
                //Controls["StickZLeft"] = new InputBinding();
                //Controls["StickZRight"] = new InputBinding();

                SaveConfig();
                return;
            }

            //  Log file contents before parsing
            Debug.Log($"LOAD CONFIG - INI File Found! Reading from: {configPath}");
            string[] lines = File.ReadAllLines(configPath);

            if (lines.Length == 0)
            {
                Debug.LogError("INI file is EMPTY. Using defaults.");
                return;
            }

            foreach (string line in lines)
            {
                Debug.Log($"INI LINE READ: {line}"); // Log each line being processed

                if (string.IsNullOrWhiteSpace(line) || !line.Contains("="))
                {
                    Debug.LogWarning($"Skipping invalid or empty INI line: {line}");
                    continue;
                }

                string[] parts = line.Split('=');
                if (parts.Length != 2) // Ensure exactly two parts
                {
                    Debug.LogError($"Malformed INI line (missing '='?): {line}");
                    continue;
                }

                string leftSide = parts[0].Trim();
                string rightSide = parts[1].Trim();

                string[] keyParts = leftSide.Split('.');
                if (keyParts.Length != 2) // Ensure action.prop format
                {
                    Debug.LogError($"Malformed INI key (missing dot '.'): {leftSide}");
                    continue;
                }

                string action = keyParts[0];  // e.g. "StickLeft"
                string prop = keyParts[1];    // e.g. "Keyboard"

                if (!Controls.ContainsKey(action))
                {
                    Controls[action] = new InputBinding();
                }

                InputBinding binding = Controls[action];

                switch (prop)
                {
                    case "Keyboard":
                        binding.Keyboard = rightSide;
                        break;
                    case "Mouse":
                        binding.Mouse = rightSide;
                        break;
                    case "XInput":
                        binding.XInput = rightSide;
                        break;
                    case "DInput":
                        binding.DInput = rightSide;
                        break;
                    case "VR":
                        binding.VR = rightSide;
                        break;
                    case "Sensitivity":
                        if (float.TryParse(rightSide, out float s))
                        {
                            binding.Sensitivity = s;
                        }
                        else
                        {
                            Debug.LogError($"Failed to parse Sensitivity value: {rightSide}");
                        }
                        break;
                    default:
                        Debug.LogWarning($"Unknown INI property: {prop}");
                        break;
                }
            }

            Debug.Log("Successfully loaded INI from " + configPath);
        }

        //---------------------------------------------------------------------
        // INI SAVE
        //---------------------------------------------------------------------
        void SaveConfig()
        {
            using (var writer = new StreamWriter(configPath))
            {
                foreach (var kv in Controls)
                {
                    string action = kv.Key;
                    InputBinding b = kv.Value;

                    writer.WriteLine($"{action}.Keyboard={b.Keyboard}");
                    writer.WriteLine($"{action}.Mouse={b.Mouse}");
                    writer.WriteLine($"{action}.XInput={b.XInput}");
                    writer.WriteLine($"{action}.DInput={b.DInput}");
                    writer.WriteLine($"{action}.VR={b.VR}");
                    writer.WriteLine($"{action}.Sensitivity={b.Sensitivity}");
                }
            }
            Debug.Log("Saved INI to " + configPath);
        }

        //---------------------------------------------------------------------
        // UI METHODS
        //---------------------------------------------------------------------
        void PopulateBindingsList()
        {
            // replaced 'bindingsContainer_DYN' with 'bindingsContainer', etc.
            if (bindingsContainer == null || bindingEntryPrefab == null) return;

            foreach (Transform child in bindingsContainer)
            {
                Destroy(child.gameObject);
            }
            bindingEntries.Clear();

            foreach (var kv in Controls)
            {
                GameObject entry = Instantiate(bindingEntryPrefab, bindingsContainer);
                entry.transform.Find("BindingName").GetComponent<Text>().text = kv.Key;
                entry.transform.Find("BindingValue").GetComponent<Text>().text = GetBindingString(kv.Value);

                // If there's a slider for sensitivity:
                Slider slider = entry.transform.Find("SensitivitySlider")?.GetComponent<Slider>();
                if (slider)
                {
                    slider.value = kv.Value.Sensitivity;
                    slider.onValueChanged.AddListener((val) => { kv.Value.Sensitivity = val; });
                }

                // Clicking on the row selects this binding
                UnityEngine.UI.Button btn = entry.GetComponent<UnityEngine.UI.Button>();
                if (btn)
                {
                    btn.onClick.AddListener(() => SelectBinding(kv.Key));
                }

                bindingEntries[kv.Key] = entry;
            }
        }

        void ApplyInputs()
        {
            Vector3 stickMovement = new Vector3(
                GetAxisValue("StickLeft", "StickRight"),
                GetAxisValue("StickForward", "StickBackward"),
                GetAxisValue("StickZLeft", "StickZRight")
            );
            Quaternion stickRotation = Quaternion.Euler(-stickMovement.y * primaryThumbstickRotationMultiplier, 0, -stickMovement.x * primaryThumbstickRotationMultiplier);
            StickObject.localRotation = stickRotation;

            float throttleValue = GetThrottleValue("ThrottleUp", "ThrottleDown");
            ThrottleObject.localRotation = Quaternion.Euler(0, 0, throttleValue * triggerRotationMultiplier);

            // X
            float newRotationX = currentRotationX + stickMovement.x * Time.deltaTime * thumbstickVelocityX;
            newRotationX = Mathf.Clamp(newRotationX, -rotationLimitX, rotationLimitX);
            float rotateX = newRotationX - currentRotationX;
            XObject.Rotate(0, rotateX, 0);
            currentRotationX = newRotationX;

            /*
            // Y
            float newRotationY = currentRotationY + stickMovement.y * Time.deltaTime * thumbstickVelocityY;
            newRotationY = Mathf.Clamp(newRotationY, -rotationLimitY, rotationLimitY);
            float rotateY = newRotationY - currentRotationY;
            YObject.Rotate(rotateY, 0, 0);
            currentRotationY = newRotationY;
            */

            // Z
            float newRotationZ = currentRotationZ + stickMovement.y * Time.deltaTime * thumbstickVelocityZ;
            newRotationZ = Mathf.Clamp(newRotationZ, -rotationLimitZ, rotationLimitZ);
            float rotateZ = newRotationZ - currentRotationZ;
            ZObject.Rotate(0, 0, rotateZ);
            currentRotationZ = newRotationZ;
        }

        float GetThrottleValue(string upAction, string downAction)
        {
            float upValue = IsInputActive(upAction) ? 1f : 0f;
            float downValue = IsInputActive(downAction) ? 1f : 0f;
            return (upValue - downValue) * Controls[upAction].Sensitivity;
        }

        float GetAxisValue(string negativeAction, string positiveAction)
        {
            float value = 0;
            if (IsInputActive(negativeAction)) value -= 1;
            if (IsInputActive(positiveAction)) value += 1;
            return value * Controls[negativeAction].Sensitivity;
        }

        void SelectBinding(string bindingKey)
        {
            selectedBinding = bindingKey;
            // If you have a slider for sensitivity:
            // sensitivitySlider.value = controls[selectedBinding].Sensitivity;
            // sensitivityValueText.text = sensitivitySlider.value.ToString("F1");
        }

        void StartRebinding()
        {
            // This references a 'selectedBindingDyn'? 
            // If you want the same logic, replace with 'selectedBinding'
            if (string.IsNullOrEmpty(selectedBinding)) return;
            waitingForInput = true;
            Debug.Log("Press a key to bind: " + selectedBinding);
        }

        void UpdateSensitivity(float value)
        {
            if (!string.IsNullOrEmpty(selectedBinding))
            {
                Controls[selectedBinding].Sensitivity = value;
                // if you want to reflect in UI:
                // sensitivityValueText.text = value.ToString("F1");
            }
        }

        string GetBindingString(InputBinding b)
        {
            return string.Format("Keyboard: {0}, Mouse: {1}, XInput: {2}, DInput: {3}, VR: {4}, Sensitivity: {5:F1}",
                b.Keyboard, b.Mouse, b.XInput, b.DInput, b.VR, b.Sensitivity);
        }

        bool IsDirectInputButtonPressed(string action)
        {
            return DirectInputControlModule.IsButtonPressed(action);
        }

        float GetDirectInputAnalogValue(string action)
        {
            return DirectInputControlModule.GetAnalogValue(action);
        }

        public bool IsInputActive(string action)
        {
            if (!Controls.ContainsKey(action)) return false;
            InputBinding bind = Controls[action];

            // Keyboard Input
            if (!string.IsNullOrEmpty(bind.Keyboard) && Input.GetKey(bind.Keyboard))
                return true;

            // Mouse Input (Buttons)
            if (!string.IsNullOrEmpty(bind.Mouse))
            {
                if (bind.Mouse == "Mouse0" && Input.GetMouseButton(0)) return true;
                if (bind.Mouse == "Mouse1" && Input.GetMouseButton(1)) return true;
                if (bind.Mouse == "Mouse2" && Input.GetMouseButton(2)) return true;
            }

            // XInput Buttons
            if (!string.IsNullOrEmpty(bind.XInput)
                && Enum.TryParse(bind.XInput, out XInput.Button xButton)
                && XInput.Get(xButton))
                return true;

            // XInput Triggers (Analog)
            if (bind.XInput == "LeftTrigger" && XInput.Get(XInput.Trigger.LIndexTrigger) > 0.1f) return true;
            if (bind.XInput == "RightTrigger" && XInput.Get(XInput.Trigger.RIndexTrigger) > 0.1f) return true;

            if (!string.IsNullOrEmpty(bind.DInput) && IsDirectInputButtonPressed(action))
                return true;

            if (bind.DInput == "ThrottleUp" && GetDirectInputAnalogValue(bind.DInput) > 0.1f) return true;
            if (bind.DInput == "ThrottleDown" && GetDirectInputAnalogValue(bind.DInput) < -0.1f) return true;

            // VR Triggers (Throttle & Buttons)
            if (!string.IsNullOrEmpty(bind.VR))
            {
                if (bind.VR == "RightTrigger" && OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger) > 0.1f) return true;
                if (bind.VR == "LeftTrigger" && OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger) > 0.1f) return true;
                if (OVRInput.Get(OVRInput.Button.One)) return true;
                if (OVRInput.Get(OVRInput.Button.Two)) return true;
                if (OVRInput.Get(OVRInput.Button.Three)) return true;
                if (OVRInput.Get(OVRInput.Button.Four)) return true;
            }

            return false;
        }
    }


    public static class DirectInputControlModule
    {
        private static string activeConfigPath;
        private static Dictionary<string, InputBinding> directInputBindings;

        [DllImport("user32.dll")]
        private static extern uint GetRawInputData(
            IntPtr hRawInput,
            uint uiCommand,
            out RAWINPUT pData,
            ref uint pcbSize,
            uint cbSizeHeader
        );

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUT
        {
            public RAWINPUTHEADER header;
            public RAWMOUSE mouse;
            public RAWINPUTHID hid;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWMOUSE
        {
            public ushort usFlags;
            public uint ulButtons;
            public uint ulRawButtons;
            public int lLastX;
            public int lLastY;
            public uint ulExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTHID
        {
            public uint dwSizeHid;
            public uint dwCount;
            public byte bRawData;
        }

        public static void Initialize(string iniFile)
        {
            activeConfigPath = Path.Combine(Application.persistentDataPath, "Emulators/MAME/inputs/", iniFile);
            LoadDinputConfig();
        }

        public static void LoadDinputConfig()
        {
            directInputBindings = new Dictionary<string, InputBinding>();
            if (!File.Exists(activeConfigPath))
            {
                Debug.Log("No INI found for DirectInput, using defaults...");
                return;
            }

            string[] lines = File.ReadAllLines(activeConfigPath);
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || !line.Contains("="))
                    continue;

                string[] parts = line.Split('=');
                if (parts.Length < 2) continue;

                string key = parts[0].Trim();
                string value = parts[1].Trim();
                string[] keyParts = key.Split('.');
                if (keyParts.Length < 2) continue;

                string action = keyParts[0];
                string prop = keyParts[1];

                if (!directInputBindings.ContainsKey(action))
                    directInputBindings[action] = new InputBinding();

                InputBinding binding = directInputBindings[action];
                switch (prop)
                {
                    case "Mouse": binding.Mouse = value; break;
                    case "DInput": binding.DInput = value; break;
                    case "Sensitivity":
                        if (float.TryParse(value, out float s))
                            binding.Sensitivity = s;
                        break;
                }
            }
        }

        public static float GetAnalogValue(string action)
        {
            if (!GetDInputRawInput(out RAWINPUT rawInput))
                return 0f;

            if (!directInputBindings.ContainsKey(action))
                return 0f;

            string mappedInput = directInputBindings[action].DInput;

            float rawValue = 0f;

            switch (mappedInput)
            {
                case "StickX": rawValue = rawInput.hid.bRawData; break;
                case "StickY": rawValue = rawInput.hid.bRawData; break;
                case "wheel": rawValue = rawInput.hid.bRawData; break;
                case "Pedal": rawValue = rawInput.hid.bRawData; break;
                case "Throttle": rawValue = rawInput.hid.bRawData; break;
                case "Rudder": rawValue = rawInput.hid.bRawData; break;
                case "Dial": rawValue = rawInput.hid.bRawData; break;
                case "Spinner": rawValue = rawInput.hid.bRawData; break;
                default:
                    Debug.LogWarning($"DirectInputControlModule: Unknown input type '{mappedInput}' for action '{action}'.");
                    return 0f;
            }

            return ApplyDeadzone(rawValue, action);
        }

        private static float ApplyDeadzone(float value, string action)
        {
            if (!directInputBindings.ContainsKey(action))
                return value;

            float sensitivity = directInputBindings[action].Sensitivity;
            return value * sensitivity;
        }

        public static bool GetDInputRawInput(out RAWINPUT rawInput)
        {
            uint size = (uint)Marshal.SizeOf(typeof(RAWINPUT));
            rawInput = new RAWINPUT();
            if (GetRawInputData(IntPtr.Zero, 0x10000003, out rawInput, ref size, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) == uint.MaxValue)
            {
                return false;
            }
            return true;
        }

        public static bool IsButtonPressed(string action)
        {
            if (!directInputBindings.ContainsKey(action)) return false;

            string mappedInput = directInputBindings[action].DInput;
            if (string.IsNullOrEmpty(mappedInput)) return false;

            return CheckRawButtonPress(mappedInput);
        }

        private static bool CheckRawButtonPress(string input)
        {
            if (GetDInputRawInput(out RAWINPUT rawInput))
            {
                uint buttonState = rawInput.mouse.ulButtons;
                if (input == "Button1" && (buttonState & 0x0001) != 0) return true;
                if (input == "Button2" && (buttonState & 0x0002) != 0) return true;
                if (input == "Button3" && (buttonState & 0x0004) != 0) return true;
                if (input == "Button4" && (buttonState & 0x0008) != 0) return true;
            }
            return false;
        }
    }

    public class CabinetControlModule : MonoBehaviour
    {
        private static string activeConfigPath;
        private static Dictionary<string, InputBinding> controlBindings;


        [Header("Control UI")]
        private string selectedBinding;
        private bool waitingForInput = false;

        void Start()
        {
            Debug.Log("CabinetControlModule is starting.");

            if (!IsConfigLoaded)  // ✅ Prevents multiple loads
            {
                LoadControlConfig();
            }
            else
            {
                Debug.Log("Config already loaded, skipping redundant load.");
            }
            if (controlBindings == null)
            {
                Debug.LogError("CRITICAL: controlBindings is NULL in Start()!");
                return;
            }
        }

        public static void SetActiveConfig(string iniPath)
        {
            if (string.IsNullOrEmpty(iniPath))
            {
                Debug.LogError("SetActiveConfig received a null or empty INI file.");
                return;
            }

            activeConfigPath = iniPath;
            Debug.Log($"CabinetControlModule: Active config set to {activeConfigPath}");

            if (!File.Exists(activeConfigPath))
            {
                Debug.LogWarning("INI file does not exist, creating a default one.");
                File.WriteAllText(activeConfigPath, "# Default INI\n");
            }
        }

        public static bool IsConfigLoaded { get; private set; } = false;

        public static void LoadControlConfig()
        {
            if (IsConfigLoaded)
            {
                Debug.Log("INI already loaded, skipping duplicate load.");
                return;
            }

            controlBindings = new Dictionary<string, InputBinding>();

            if (!File.Exists(activeConfigPath))
            {
                Debug.LogError("No INI found, using defaults.");
                return;
            }

            Debug.Log($"Loading INI file from: {activeConfigPath}");
            string[] lines = File.ReadAllLines(activeConfigPath);

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || !line.Contains("="))
                    continue;

                string[] parts = line.Split('=');
                if (parts.Length < 2) continue;

                string key = parts[0].Trim();
                string value = parts[1].Trim();
                string[] keyParts = key.Split('.');
                if (keyParts.Length < 2) continue;

                string action = keyParts[0];
                string prop = keyParts[1];

                if (!controlBindings.ContainsKey(action))
                    controlBindings[action] = new InputBinding();

                InputBinding binding = controlBindings[action];
                switch (prop)
                {
                    case "Keyboard": binding.Keyboard = value; break;
                    case "Mouse": binding.Mouse = value; break;
                    case "XInput": binding.XInput = value; break;
                    case "DInput": binding.DInput = value; break;
                    case "VR": binding.VR = value; break;
                    case "Sensitivity":
                        if (float.TryParse(value, out float s))
                            binding.Sensitivity = s;
                        break;
                }
            }

            IsConfigLoaded = true;  // ✅ Mark as loaded to prevent future duplicate calls
            Debug.Log("Successfully loaded INI.");
        }


        public static void SaveConfig()
        {
            using (var writer = new StreamWriter(activeConfigPath))
            {
                foreach (var kv in controlBindings)
                {
                    writer.WriteLine($"{kv.Key}.Keyboard={kv.Value.Keyboard}");
                    writer.WriteLine($"{kv.Key}.Mouse={kv.Value.Mouse}");
                    writer.WriteLine($"{kv.Key}.XInput={kv.Value.XInput}");
                    writer.WriteLine($"{kv.Key}.DInput={kv.Value.DInput}");
                    writer.WriteLine($"{kv.Key}.VR={kv.Value.VR}");
                    writer.WriteLine($"{kv.Key}.Sensitivity={kv.Value.Sensitivity}");
                }
            }
        }


        void SelectBinding(string bindingKey)
        {
            selectedBinding = bindingKey;
        }

        void StartRebinding()
        {
            // Ensure the active INI is set before opening the binding menu
            CabinetControlModule.SetActiveConfig(activeConfigPath);

            if (string.IsNullOrEmpty(selectedBinding)) return;
            waitingForInput = true;
        }

        string GetBindingString(InputBinding b)
        {
            return string.Format("Keyboard: {0}, Mouse: {1}, XInput: {2}, DInput: {3}, VR: {4}, Sensitivity: {5:F1}",
                b.Keyboard, b.Mouse, b.XInput, b.DInput, b.VR, b.Sensitivity);
        }
    }

    [Serializable]
    public class InputBinding
    {
        public string Keyboard;
        public string Mouse;
        public string XInput;
        public string DInput;
        public string VR;
        public float Sensitivity = 1.0f;
    }
}
