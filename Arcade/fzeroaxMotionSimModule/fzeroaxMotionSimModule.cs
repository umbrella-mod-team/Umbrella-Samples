using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using EmuVR.InputManager;
using System.Collections;

namespace WIGUx.Modules.fzeroaxMotionSim
{
    public class fzeroaxMotionSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        private readonly float keyboardVelocityX = 2f;  // Velocity for keyboard input
        private readonly float keyboardVelocityY = 50f;  // Velocity for keyboard input
        private readonly float keyboardVelocityZ = 50f;  // Velocity for keyboard input
        private readonly float vrVelocity = 50f;        // Velocity for VR controller input

        private readonly float centeringVelocityX = 50f;  // Velocity for centering rotation
        private readonly float centeringVelocityY = 50f;  // Velocity for centering rotation
        private readonly float centeringVelocityZ = 50f;  // Velocity for centering rotation

        private float adjustSpeed = 1.0f;  // Adjust this adjustment speed as needed a lower number will lead to smaller adustments

        private float rotationLimitX = 16f;  // Rotation limit for X-axis
        private float rotationLimitY = 30f;  // Rotation limit for Y-axis
        private float rotationLimitZ = 40f;  // Rotation limit for Z-axis

        private float currentRotationX = 0f;  // Current rotation for X-axis
        private float currentRotationY = 0f;  // Current rotation for Y-axis
        private float currentRotationZ = 0f;  // Current rotation for Z-axis

        private Transform fzeroaxXObject; // Reference to the main X object
        private Transform fzeroaxYObject; // Reference to the main Y object
        private Transform fzeroaxZObject; // Reference to the main Z object
        private GameObject cockpitCam;    // Reference to the cockpit camera

        // Initial positions and rotations for resetting
        private Vector3 fzeroaxXStartPosition;
        private Quaternion fzeroaxXStartRotation;
        private Vector3 fzeroaxYStartPosition;
        private Quaternion fzeroaxYStartRotation;
        private Vector3 fzeroaxZStartPosition;
        private Quaternion fzeroaxZStartRotation;
        private Vector3 cockpitCamStartPosition;
        private Quaternion cockpitCamStartRotation;

        // Controller animation 
        // Speeds for the animation of the in game flight stick or wheel
        private readonly float keyboardControllerVelocityX = 350.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityY = 350.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityZ = 350.5f;  // Velocity for keyboard input
        private readonly float vrControllerVelocity = 350.5f;        // Velocity for VR controller input

        private float controllerrotationLimitX = 0f;  // Rotation limit for X-axis (stick or wheel)
        private float controllerrotationLimitY = 0f;  // Rotation limit for Y-axis (stick or wheel)
        private float controllerrotationLimitZ = 55f;  // Rotation limit for Z-axis (stick or wheel)

        private float currentControllerRotationX = 0f;  // Current rotation for X-axis (stick or wheel)
        private float currentControllerRotationY = 0f;  // Current rotation for Y-axis (stick or wheel)
        private float currentControllerRotationZ = 0f;  // Current rotation for Z-axis (stick or wheel)

        private readonly float centeringControllerVelocityX = 150.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float centeringControllerVelocityY = 150.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float centeringControllerVelocityZ = 150.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float rotationSmoothness = 5f;  //sets the smoothness of the rotation
        private Transform fzeroaxControllerX; // Reference to the main animated controller (wheel)
        private Vector3 fzeroaxControllerXStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion fzeroaxControllerXStartRotation; // Initial controlller positions and rotations for resetting
        private Transform fzeroaxControllerY; // Reference to the main animated controller (wheel)
        private Vector3 fzeroaxControllerYStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion fzeroaxControllerYStartRotation; // Initial controlller positions and rotations for resetting
        private Transform fzeroaxControllerZ; // Reference to the main animated controller (wheel)
        private Vector3 fzeroaxControllerZStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion fzeroaxControllerZStartRotation; // Initial controlller positions and rotations for resetting

        // Initial positions and rotations for VR setup
        private Vector3 playerCameraStartPosition;
        private Quaternion playerCameraStartRotation;
        private Vector3 playerVRSetupStartPosition;
        private Quaternion playerVRSetupStartRotation;
        private Vector3 playerCameraStartScale;
        private Vector3 playerVRSetupStartScale;

        // GameObject references for PlayerCamera and VR setup
        private GameObject playerCamera;
        private GameObject playerVRSetup;


        //Lights and Emissives
        private Transform fzeroaxturbo1Object;
        private Transform fzeroaxturbo2Object;
        private Transform fzeroaxhazardlObject;
        private Transform fzeroaxhazardrObject;
        public float brightIntensity = 5.0f; // Set the brightness intensity level
        public float dimIntensity = 1.0f;    // Set the dim intensity level
        public string fire1Button = "Fire1"; // Name of the fire button
        public string fire2Button = "Fire2"; // Name of the fire button 
        public string fire3Button = "Fire3"; // Name of the fire button 
        public string JumpButton = "Jump"; // Name of the fire button 
        public Light fzeroaxturbolight1;
        public Light fzeroaxturbolight2;
        public Light strobe1_light;
        public Light strobe2_light;
        public Light strobe3_light;
        public Light strobe4_light;
        private float flashDuration = 0.01f;
        private float flashInterval = 0.05f;
        private Renderer[] hazzardEmissiveObjects;
        private Renderer[] leftEmissiveObjects;
        private Renderer[] rightEmissiveObjects;
        public float frontFlashDuration = 0.7f;
        public float frontFlashDelay = 0.7f;
        public float sideFlashDuration = 0.5f;
        public float sideFlashDelay = 0.5f;
        public float frontDangerDuration = 0.7f;
        public float frontDangerDelay = 0.7f;
        public float sideDangerDuration = 0.55f;
        public float sideDangerDelay = 0.55f;
        private Coroutine frontCoroutine;
        private Coroutine leftCoroutine;
        private Coroutine rightCoroutine;
        public float lightDuration = 0.35f; // Duration during which the lights will be on
        private bool inFocusMode = false;  // Flag to track focus mode state
        private bool areStrobesOn = false; // track strobe lights
        private Coroutine hazardsCoroutine; // Coroutine variable to control the strobe flashing
        //array of lights
        private Light[] lights;

        // Array of strobe lights
        private List<Light> strobeLights = new List<Light>();

        private readonly string[] compatibleGames = { "Zero AX" };

        private Dictionary<GameObject, Transform> originalParents = new Dictionary<GameObject, Transform>();  // Dictionary to store original parents of objects

        void Start()
        {
            // Find references to PlayerCamera and VR setup objects
            playerCamera = PlayerVRSetup.PlayerCamera.gameObject;
            playerVRSetup = PlayerVRSetup.PlayerRig.gameObject;

            // Check if objects are found
            CheckObject(playerCamera, "PlayerCamera");
            CheckObject(playerVRSetup, "PlayerVRSetup.PlayerRig");

            GameObject cameraObject = GameObject.Find("OVRCameraRig");

            // Find gfpce2X object in hierarchy
            fzeroaxXObject = transform.Find("fzeroaxX");
            if (fzeroaxXObject != null)
            {
                logger.Info("fzeroaxX object found.");
                fzeroaxXStartPosition = fzeroaxXObject.position;
                fzeroaxXStartRotation = fzeroaxXObject.rotation;

                // Find fzeroaxY object under fzeroaxX
                fzeroaxYObject = fzeroaxXObject.Find("fzeroaxY");
                if (fzeroaxYObject != null)
                {
                    logger.Info("fzeroaxY object found.");
                    fzeroaxYStartPosition = fzeroaxYObject.position;
                    fzeroaxYStartRotation = fzeroaxYObject.rotation;

                    // Find fzeroaxZ object under fzeroaxY
                    fzeroaxZObject = fzeroaxYObject.Find("fzeroaxZ");
                    if (fzeroaxZObject != null)
                    {
                        logger.Info("fzeroaxZ object found.");
                        fzeroaxZStartPosition = fzeroaxZObject.position;
                        fzeroaxZStartRotation = fzeroaxZObject.rotation;

                        // Find fzeroaxControllerX under fzeroaxZ
                        fzeroaxControllerX = fzeroaxZObject.Find("fzeroaxControllerX");
                        if (fzeroaxControllerX != null)
                        {
                            logger.Info("fzeroaxControllerX object found.");
                            // Store initial position and rotation of the stick
                            fzeroaxControllerXStartPosition = fzeroaxControllerX.transform.position; // these could cause the controller to mess up
                            fzeroaxControllerXStartRotation = fzeroaxControllerX.transform.rotation;

                            // Find fzeroaxControllerY under fzeroaxControllerX
                            fzeroaxControllerY = fzeroaxControllerX.Find("fzeroaxControllerY");
                            if (fzeroaxControllerY != null)
                            {
                                logger.Info("fzeroaxControllerY object found.");
                                // Store initial position and rotation of the stick
                                fzeroaxControllerYStartPosition = fzeroaxControllerY.transform.position;
                                fzeroaxControllerYStartRotation = fzeroaxControllerY.transform.rotation;

                                // Find fzeroaxControllerZ under fzeroaxControllerY
                                fzeroaxControllerZ = fzeroaxControllerY.Find("fzeroaxControllerZ");
                                if (fzeroaxControllerZ != null)
                                {
                                    logger.Info("fzeroaxControllerZ object found.");
                                    // Store initial position and rotation of the stick
                                    fzeroaxControllerZStartPosition = fzeroaxControllerZ.transform.position;
                                    fzeroaxControllerZStartRotation = fzeroaxControllerZ.transform.rotation;

                                    // Find fzeroaxturbo1Object object under fzeroaxControllerZ
                                    fzeroaxturbo1Object = fzeroaxControllerZ.Find("fzeroaxturbo1");
                                    if (fzeroaxturbo1Object != null)
                                    {
                                        logger.Info("fzeroaxturbo1 object found.");
                                        // Ensure the fzeroaxtaillight1 object is initially off
                                        Renderer renderer = fzeroaxturbo1Object.GetComponent<Renderer>();
                                        if (renderer != null)
                                        {
                                            renderer.material.DisableKeyword("_EMISSION");
                                        }
                                        else
                                        {
                                            logger.Debug("Renderer component is not found on fzeroaxturbo1 object.");
                                        }
                                    }
                                    else
                                    {
                                        logger.Debug("fzeroaxturbo1 object not found under fzeroaxControllerZ.");
                                    }

                                    // Find fzeroaxturbo2 object under fzeroaxZ
                                    fzeroaxturbo2Object = fzeroaxControllerZ.Find("fzeroaxturbo2");
                                    if (fzeroaxturbo2Object != null)
                                    {
                                        logger.Info("fzeroaxturbo2 object found.");
                                        // Ensure the fzeroaxturbolight2 object is initially off
                                        Renderer renderer = fzeroaxturbo2Object.GetComponent<Renderer>();
                                        if (renderer != null)
                                        {
                                            renderer.material.DisableKeyword("_EMISSION");
                                        }
                                        else
                                        {
                                            logger.Debug("Renderer component is not found on fzeroaxturbo2 object.");
                                        }
                                    }
                                    else
                                    {
                                        logger.Debug("fzeroaxturbo2 object not found under fzeroaxControllerZ.");
                                    }
                                }
                                else
                                {
                                    logger.Error("fzeroaxControllerZ object not found under fzeroaxControllerY!");
                                }
                            }
                            else
                            {
                                logger.Error("fzeroaxControllerY object not found under fzeroaxControllerX!");
                            }
                        }
                        else
                        {
                            logger.Error("fzeroaxControllerX object not found under fzeroaxZ!");
                        }
                        // Gets all Light components in the target object and its children
                        Light[] allLights = fzeroaxControllerZ.GetComponentsInChildren<Light>();

                        // Log the names of the objects containing the Light components and filter out unwanted lights
                        foreach (Light light in allLights)
                        {
                            if (light.gameObject.name == "fzeroaxturbolight1")
                            {
                                fzeroaxturbolight1 = light;
                                logger.Info("Included Light found in object: " + light.gameObject.name);
                            }
                            else if (light.gameObject.name == "fzeroaxturbolight2")
                            {
                                fzeroaxturbolight2 = light;
                                logger.Info("Included Light found in object: " + light.gameObject.name);
                            }
                            else if (light.gameObject.name == "strobe1_light")
                            {
                                strobe1_light = light;
                                logger.Info("Included Light found in object: " + light.gameObject.name);
                            }
                            else if (light.gameObject.name == "strobe2_light")
                            {
                                strobe2_light = light;
                                logger.Info("Included Light found in object: " + light.gameObject.name);
                            }
                            else if (light.gameObject.name == "strobe3_light")
                            {
                                strobe3_light = light;
                                logger.Info("Included Light found in object: " + light.gameObject.name);
                            }
                            else if (light.gameObject.name == "strobe4_light")
                            {
                                strobe4_light = light;
                                logger.Info("Included Light found in object: " + light.gameObject.name);
                            }
                            else
                            {
                                logger.Info("Excluded Light found in object: " + light.gameObject.name);
                            }
                        }
                        // Find cockpit camera under cockpit
                        cockpitCam = fzeroaxZObject.Find("eyes/cockpitcam")?.gameObject;
                        if (cockpitCam != null)
                        {
                            logger.Info("Cockpitcam object found.");

                            // Store initial position and rotation of cockpit cam
                            cockpitCamStartPosition = cockpitCam.transform.position;
                            cockpitCamStartRotation = cockpitCam.transform.rotation;
                        }
                        else
                        {
                            logger.Error("Cockpitcam object not found under fzeroaxZ!");
                        }
                    }
                    else
                    {
                        logger.Error("fzeroaxZ object not found under fzeroaxY!");
                    }
                }
                else
                {
                    logger.Error("fzeroaxY object not found under fzeroaxX!");
                }
            }
            else
            {
                logger.Error("fzeroaxX object not found!");
            }
            hazzardEmissiveObjects = new Renderer[1];
            //leftEmissiveObjects = new Renderer[1];
            //rightEmissiveObjects = new Renderer[1];

            hazzardEmissiveObjects[0] = transform.Find("fzeroaxhazard")?.GetComponent<Renderer>();
         //   hazzardEmissiveObjects[1] = fzeroaxZObject.Find("fzeroaxhazardr")?.GetComponent<Renderer>();

            //leftEmissiveObjects[0] = fzeroaxZObject.Find("left1")?.GetComponent<Renderer>();
            //leftEmissiveObjects[1] = fzeroaxZObject.Find("left2")?.GetComponent<Renderer>();
           // leftEmissiveObjects[2] = fzeroaxZObject.Find("left3")?.GetComponent<Renderer>();

            //rightEmissiveObjects[0] = fzeroaxZObject.Find("right1")?.GetComponent<Renderer>();
            //rightEmissiveObjects[1] = fzeroaxZObject.Find("right2")?.GetComponent<Renderer>();
            //rightEmissiveObjects[2] = fzeroaxZObject.Find("right3")?.GetComponent<Renderer>();
            // Set lights to start

            StartAttractPattern();
            ToggleLight1(false);
            ToggleLight2(false);
            //ToggleBrightness1(false);
            //ToggleBrightness2(false);
        }

        void Update()
        {
            bool inputDetected = false; // Initialize at the beginning of the Update method
            if (Input.GetKeyDown(KeyCode.Y))
            {
                logger.Info("Resetting Positions");
                ResetPositions();
            }
                /*
                // Check if the "L" key is pressed
                if (Input.GetKeyDown(KeyCode.L))
                {
                    if (areStrobesOn)
                    {
                        // Strobe lights are currently on, stop the coroutine to turn them off
                        logger.Info("Stopping strobes");
                        StopCoroutine(strobeCoroutine);
                        strobeCoroutine = null;
                        TurnAllOff(); // Turn all emissives off
                        TurnOffAllStrobes();
                    }
                    else
                    {
                        // Strobe lights are currently off, start the coroutine to flash them
                        logger.Info("Starting strobes");
                        strobeCoroutine = StartCoroutine(FlashStrobes());
                    }

                    // Toggle the strobe state
                    areStrobesOn = !areStrobesOn;
                }
                */
                if (GameSystem.ControlledSystem != null && !inFocusMode)
            {
                string controlledSystemGamePathString = GameSystem.ControlledSystem.Game.path != null ? GameSystem.ControlledSystem.Game.path.ToString() : null;
                bool containsString = false;

                foreach (var gameString in compatibleGames)
                {
                    if (controlledSystemGamePathString != null && controlledSystemGamePathString.Contains(gameString))
                    {
                        containsString = true;
                        break;
                    }
                }

                if (containsString)
                {
                    StartFocusMode();
                }
            }

            if (GameSystem.ControlledSystem == null && inFocusMode)
            {
                EndFocusMode();
            }
            if (inFocusMode)
            {
                HandleTransformAdjustment();
                HandleInput(ref inputDetected);  // Pass by reference
            }
        }

        void StartFocusMode()
        {
            string controlledSystemGamePathString = GameSystem.ControlledSystem.Game.path != null ? GameSystem.ControlledSystem.Game.path.ToString() : null;
            logger.Info($"Controlled System Game path String: {controlledSystemGamePathString}");
            logger.Info("Compatible Rom Dectected, Starting F-Zero AX...");
            logger.Info("F-Zero AX Motion Sim starting... O.K. Captain Falcon, You Ready?");
            logger.Info("Here We Go!...");
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
                    playerCameraStartScale = playerCamera.transform.localScale; // Store initial scale
                    SaveOriginalParent(playerCamera);  // Save original parent of PlayerCamera

                    // Set PlayerCamera as child of cockpit cam and maintain scale
                    playerCamera.transform.SetParent(cockpitCam.transform, false);
                    playerCamera.transform.localScale = playerCameraStartScale;  // Reapply initial scale
                    playerCamera.transform.localRotation = Quaternion.identity;
                    logger.Info("PlayerCamera set as child of CockpitCam.");
                }

                if (playerVRSetup != null)
                {
                    // Store initial position, rotation, and scale of PlayerVRSetup
                    playerVRSetupStartPosition = playerVRSetup.transform.position;
                    playerVRSetupStartRotation = playerVRSetup.transform.rotation;
                    playerVRSetupStartScale = playerVRSetup.transform.localScale; // Store initial scale
                    SaveOriginalParent(playerVRSetup);  // Save original parent of PlayerVRSetup

                    // Set PlayerVRSetup as child of cockpit cam and maintain scale
                    playerVRSetup.transform.SetParent(cockpitCam.transform, false);
                    playerVRSetup.transform.localScale = playerVRSetupStartScale;
                    playerVRSetup.transform.localRotation = Quaternion.identity;
                    logger.Info("PlayerVRSetup.PlayerRig set as child of CockpitCam.");
                }
            }
            else
            {
                logger.Error("CockpitCam object not found under fzeroaxZ!");
            }
            inFocusMode = true;  // Set focus mode flag
        }

        void EndFocusMode()
        {
            logger.Info("Exiting Focus Mode...");
            // Restore original parents of objects
            RestoreOriginalParent(playerCamera, "PlayerCamera");
            RestoreOriginalParent(playerVRSetup, "PlayerVRSetup.PlayerRig");
            // StopCoroutine(strobeCoroutine);
            // TurnOffAllStrobes();
            StartAttractPattern();

            // Reset fzeroaxX to initial positions and rotations
            if (fzeroaxXObject != null)
            {
                fzeroaxXObject.position = fzeroaxXStartPosition;
                fzeroaxXObject.rotation = fzeroaxXStartRotation;
            }

            // Reset fzeroaxY object to initial position and rotation
            if (fzeroaxYObject != null)
            {
                fzeroaxYObject.position = fzeroaxYStartPosition;
                fzeroaxYObject.rotation = fzeroaxYStartRotation;
            }
            // Reset fzeroaxZ object to initial position and rotation
            if (fzeroaxZObject != null)
            {
                fzeroaxZObject.position = fzeroaxZStartPosition;
                fzeroaxZObject.rotation = fzeroaxZStartRotation;
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

        void HandleInput(ref bool inputDetected)
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

                /*
                // Check if the A button on the right controller is pressed
                if (OVRInput.GetDown(OVRInput.Button.One))
                {
                    Debug.Log("OVR A button pressed");
                }

                // Check if the B button on the right controller is pressed
                if (OVRInput.GetDown(OVRInput.Button.Two))
                {
                    Debug.Log("OVR B button pressed");
                }

                // Check if the X button on the left controller is pressed
                if (OVRInput.GetDown(OVRInput.Button.Three))
                {
                    Debug.Log("OVR X button pressed");
                }

                // Check if the Y button on the left controller is pressed
                if (OVRInput.GetDown(OVRInput.Button.Four))
                {
                    Debug.Log("OVR Y button pressed");
                }

                // Check if the primary index trigger on the right controller is pressed
                if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger))
                {
                    Debug.Log("OVR Primary index trigger pressed");
                }

                // Check if the secondary index trigger on the left controller is pressed
                if (OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger))
                {
                    Debug.Log("OVR Secondary index trigger pressed");
                }

                // Check if the primary hand trigger on the right controller is pressed
                if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger))
                {
                    Debug.Log("OVR Primary hand trigger pressed");
                }

                // Check if the secondary hand trigger on the left controller is pressed
                if (OVRInput.Get(OVRInput.Button.SecondaryHandTrigger))
                {
                    Debug.Log("OVR Secondary hand trigger pressed");
                }

                // Check if the primary thumbstick is pressed
                if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick))
                {
                    Debug.Log("OVR Primary thumbstick pressed");
                }

                // Check if the secondary thumbstick is pressed
                if (OVRInput.GetDown(OVRInput.Button.SecondaryThumbstick))
                {
                    Debug.Log("OVR Secondary thumbstick pressed");
                }
                */
            
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

                // Handle RT press (assuming RT is mapped to a button in your XInput class)
                if (XInput.GetDown(XInput.Button.RIndexTrigger))
                {
                    inputDetected = true;
                }

                // Reset position on RT release
                if (XInput.GetUp(XInput.Button.RIndexTrigger))
                {

                }

                // LeftTrigger
                if (XInput.GetDown(XInput.Button.LIndexTrigger))
                {
                    inputDetected = true;
                }

                // Reset position on button release
                if (XInput.GetUp(XInput.Button.LIndexTrigger))
                {

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
                    fzeroaxYObject.Rotate(0, -rotateY, 0);
                    currentRotationY += rotateY;
                    inputDetected = true;
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
                    fzeroaxXObject.Rotate(rotateX, 0, 0);
                    currentRotationX -= rotateX;
                    inputDetected = true;
                }
            }

            // Fire3
            if (Input.GetButtonDown("Fire3") || XInput.GetDown(XInput.Button.X))
            {
                // Set lights to bright
                ToggleTurboEmissive(true);
                //ToggleBrightness1(true);
                //ToggleBrightness2(true);
                ToggleLight1(true);
                ToggleLight2(true);
                inputDetected = true;
            }
            // Reset position on button release
            if (Input.GetButtonUp("Fire3") || XInput.GetUp(XInput.Button.X))
            {
                ToggleTurboEmissive(false);
                //ToggleBrightness1(false);
                //ToggleBrightness2(false);
                ToggleLight1(false);
                ToggleLight2(false);
            }
            // Jump
            if (Input.GetButtonDown("Jump") || XInput.GetDown(XInput.Button.Y))
            {
                // Set lights to bright
                ToggleTurboEmissive(true);
                //ToggleBrightness1(true);
                //ToggleBrightness2(true);
                ToggleLight1(true);
                ToggleLight2(true);
                inputDetected = true;
            }
            // Reset position on button release
            if (Input.GetButtonUp("Jump") || XInput.GetDown(XInput.Button.Y))
            {
                ToggleTurboEmissive(false);
                //ToggleBrightness1(false);
                //ToggleBrightness2(false);
                ToggleLight1(false);
                ToggleLight2(false);
            }
            // Fire1 (negative Z rotation)
            if (Input.GetButton("Fire1") || XInput.Get(XInput.Button.A))
            {
                float rotateY = vrVelocity * Time.deltaTime;

                if (currentRotationY - rotateY > -rotationLimitY)
                {
                    fzeroaxYObject.Rotate(0, rotateY, 0);
                    currentRotationY -= rotateY;
                    inputDetected = true;
                }
            }

            /*
            // Handle X rotation for fzeroaxYObject and fzeroaxControllerX (Down Arrow or primaryThumbstick.y > 0)
            // Thumbstick direction: down
            if ((Input.GetKey(KeyCode.DownArrow) || primaryThumbstick.y > 0))
            {
                if (currentRotationX > -rotationLimitX)
                {
                    float rotateX = (Input.GetKey(KeyCode.DownArrow) ? keyboardVelocityX : primaryThumbstick.y * vrVelocity) * Time.deltaTime;
                    fzeroaxXObject.Rotate(rotateX, 0, 0);
                    currentRotationX -= rotateX;
                    inputDetected = true;
                }
                if (currentControllerRotationX > -controllerrotationLimitX)
                {
                    float controllerRotateX = (Input.GetKey(KeyCode.DownArrow) ? keyboardControllerVelocityX : primaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                    fzeroaxControllerX.Rotate(controllerRotateX, 0, 0);
                    currentControllerRotationX -= controllerRotateX;
                    inputDetected = true;
                }
            }

            // Handle X rotation for fzeroaxYObject and fzeroaxControllerX (Up Arrow or primaryThumbstick.y < 0)
            // Thumbstick direction: up
            if ((Input.GetKey(KeyCode.UpArrow) || primaryThumbstick.y < 0))
            {
                if (currentRotationX < rotationLimitX)
                {
                    float rotateX = (Input.GetKey(KeyCode.UpArrow) ? keyboardVelocityX : -primaryThumbstick.y * vrVelocity) * Time.deltaTime;
                    fzeroaxXObject.Rotate(-rotateX, 0, 0);
                    currentRotationX += rotateX;
                    inputDetected = true;
                }
                if (currentControllerRotationX < controllerrotationLimitX)
                {
                    float controllerRotateX = (Input.GetKey(KeyCode.UpArrow) ? keyboardControllerVelocityX : -primaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                    fzeroaxControllerX.Rotate(-controllerRotateX, 0, 0);
                    currentControllerRotationX += controllerRotateX;
                    inputDetected = true;
                }
            }
            */
            // Handle Z rotation for fzeroaxXObject and fzeroaxControllerZ (Down Arrow or primaryThumbstick.y < 0)
            // Thumbstick direction: Left
            if ((Input.GetKey(KeyCode.LeftArrow) || primaryThumbstick.x < 0))
            {
                if (currentRotationZ > -rotationLimitZ)
                {
                    float rotateZ = (Input.GetKey(KeyCode.LeftArrow) ? keyboardVelocityZ : -primaryThumbstick.x * vrVelocity) * Time.deltaTime;
                    fzeroaxZObject.Rotate(0, 0, rotateZ);
                    currentRotationZ -= rotateZ;
                    inputDetected = true;
                }
                if (currentControllerRotationZ > -controllerrotationLimitZ)
                {
                    float controllerRotateZ = (Input.GetKey(KeyCode.LeftArrow) ? keyboardControllerVelocityZ : -primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                    fzeroaxControllerZ.Rotate(0, 0, controllerRotateZ);
                    currentControllerRotationZ -= controllerRotateZ;
                    inputDetected = true;
                }
            }

            // Handle Z rotation for fzeroaxXObject and fzeroaxControllerZ (Up Arrow or primaryThumbstick.y > 0)
            // Thumbstick direction: right
            if ((Input.GetKey(KeyCode.RightArrow) || primaryThumbstick.x > 0))
            {
                if (currentRotationZ < rotationLimitZ)
                {
                    float rotateZ = (Input.GetKey(KeyCode.RightArrow) ? keyboardVelocityZ : primaryThumbstick.x * vrVelocity) * Time.deltaTime;
                    fzeroaxZObject.Rotate(0, 0, -rotateZ);
                    currentRotationZ += rotateZ;
                    inputDetected = true;
                }
                if (currentControllerRotationZ < controllerrotationLimitZ)
                {
                    float controllerRotateZ = (Input.GetKey(KeyCode.RightArrow) ? keyboardControllerVelocityZ : primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                    fzeroaxControllerZ.Rotate(0, 0, -controllerRotateZ);
                    currentControllerRotationZ += controllerRotateZ;
                    inputDetected = true;
                }
            }
            /*
            // Handle left rotation (Thumbstick left)
            if (primaryThumbstick.x < 0 && currentRotationY < rotationLimitY) // Note the change in condition
            {
                float rotateY = primaryThumbstick.x * vrVelocity * Time.deltaTime;
                fzeroaxYObject.Rotate(0, rotateY, 0);  // Rotate Y in the opposite direction
                currentRotationY -= rotateY;  // Update current rotation (subtracting because the direction is swapped)
                inputDetected = true;
            }

            // Handle right rotation (Thumbstick right)
            if (primaryThumbstick.x > 0 && currentRotationY > -rotationLimitY) // Note the change in condition
            {
                float rotateY = primaryThumbstick.x * vrVelocity * Time.deltaTime;
                fzeroaxYObject.Rotate(0, rotateY, 0);  // Rotate Y in the opposite direction
                currentRotationY -= rotateY;  // Update current rotation (subtracting because the direction is swapped)
                inputDetected = true;
            }
            */

            // Center the rotation if no input is detected (i think this is redundant)

            if (!inputDetected)
            {
                CenterRotation();
            }
        }

        void ResetPositions()
        {
            cockpitCam.transform.position = cockpitCamStartPosition;
            cockpitCam.transform.rotation = cockpitCamStartRotation;
            playerVRSetup.transform.position = playerVRSetupStartPosition;
            playerVRSetup.transform.rotation = playerVRSetupStartRotation;
            playerVRSetup.transform.localScale = playerVRSetupStartScale;
            //playerVRSetup.transform.localScale = new Vector3(1f, 1f, 1f);
            playerCamera.transform.position = playerCameraStartPosition;
            playerCamera.transform.rotation = playerCameraStartRotation;
            //playerCamera.transform.localScale = new Vector3(1f, 1f, 1f);
            playerCamera.transform.localScale = playerCameraStartScale;
            // Reset rotation allowances and current rotation values
            currentRotationX = 0f;
            currentRotationY = 0f;
            currentRotationZ = 0f;
        }

        void CenterRotation()
        {
            // Center X-axis
            if (currentRotationX > 0)
            {
                float unrotateX = Mathf.Min(centeringVelocityX * Time.deltaTime, currentRotationX);
                fzeroaxXObject.Rotate(unrotateX, 0, 0);
                currentRotationX -= unrotateX;
            }
            else if (currentRotationX < 0)
            {
                float unrotateX = Mathf.Min(centeringVelocityX * Time.deltaTime, -currentRotationX);
                fzeroaxXObject.Rotate(-unrotateX, 0, 0);
                currentRotationX += unrotateX;
            }

            // Center Y-axis
            if (currentRotationY > 0)
            {
                float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, currentRotationY);
                fzeroaxYObject.Rotate(0, unrotateY, 0);
                currentRotationY -= unrotateY;
            }
            else if (currentRotationY < 0)
            {
                float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, -currentRotationY);
                fzeroaxYObject.Rotate(0, -unrotateY, 0);
                currentRotationY += unrotateY;
            }

            // Center Z-axis
            if (currentRotationZ > 0)
            {
                float unrotateZ = Mathf.Min(centeringVelocityZ * Time.deltaTime, currentRotationZ);
                fzeroaxZObject.Rotate(0, 0, unrotateZ);
                currentRotationZ -= unrotateZ;
            }
            else if (currentRotationZ < 0)
            {
                float unrotateZ = Mathf.Min(centeringVelocityZ * Time.deltaTime, -currentRotationZ);
                fzeroaxZObject.Rotate(0, 0, -unrotateZ);
                currentRotationZ += unrotateZ;
            }
            //Centering for contoller

            // Center Y-axis Controller rotation
            if (currentControllerRotationY > 0)
            {
                float unrotateY = Mathf.Min(centeringControllerVelocityY * Time.deltaTime, currentControllerRotationY);
                fzeroaxControllerY.Rotate(0, unrotateY, 0);   // Rotating to reduce the rotation
                currentControllerRotationY -= unrotateY;    // Reducing the positive rotation
            }
            else if (currentControllerRotationY < 0)
            {
                float unrotateY = Mathf.Min(centeringControllerVelocityY * Time.deltaTime, -currentControllerRotationY);
                fzeroaxControllerY.Rotate(0, -unrotateY, 0);  // Rotating to reduce the rotation
                currentControllerRotationY += unrotateY;    // Reducing the negative rotation
            }

            // Center X-Axis Controller rotation
            if (currentControllerRotationX > 0)
            {
                float unrotateX = Mathf.Min(centeringControllerVelocityX * Time.deltaTime, currentControllerRotationX);
                fzeroaxControllerX.Rotate(unrotateX, 0, 0);   // Rotating to reduce the rotation
                currentControllerRotationX -= unrotateX;    // Reducing the positive rotation
            }
            else if (currentControllerRotationX < 0)
            {
                float unrotateX = Mathf.Min(centeringControllerVelocityX * Time.deltaTime, -currentControllerRotationX);
                fzeroaxControllerX.Rotate(-unrotateX, 0, 0);   // Rotating to reduce the rotation
                currentControllerRotationX += unrotateX;    // Reducing the positive rotation
            }

            // Center Z-axis Controller rotation
            if (currentControllerRotationZ > 0)
            {
                float unrotateZ = Mathf.Min(centeringControllerVelocityZ * Time.deltaTime, currentControllerRotationZ);
                fzeroaxControllerZ.Rotate(0, 0, unrotateZ);   // Rotating to reduce the rotation
                currentControllerRotationZ -= unrotateZ;    // Reducing the positive rotation
            }
            else if (currentControllerRotationZ < 0)
            {
                float unrotateZ = Mathf.Min(centeringControllerVelocityZ * Time.deltaTime, -currentControllerRotationZ);
                fzeroaxControllerZ.Rotate(0, 0, -unrotateZ);   // Rotating to reduce the rotation
                currentControllerRotationZ += unrotateZ;    // Reducing the positive rotation
            }
        }

        void HandleTransformAdjustment()
        {
            // Handle position adjustments
            if (Input.GetKey(KeyCode.Home))
            {
                // Move forward
                cockpitCam.transform.position += cockpitCam.transform.forward * adjustSpeed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.End))
            {
                // Move backward
                cockpitCam.transform.position -= cockpitCam.transform.forward * adjustSpeed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.PageUp))
            {
                // Move up
                cockpitCam.transform.position += cockpitCam.transform.up * adjustSpeed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.Insert))
            {
                // Move down
                cockpitCam.transform.position -= cockpitCam.transform.up * adjustSpeed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.Delete))
            {
                // Move left
                cockpitCam.transform.position -= cockpitCam.transform.right * adjustSpeed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.PageDown))
            {
                // Move right
                cockpitCam.transform.position += cockpitCam.transform.right * adjustSpeed * Time.deltaTime;
            }

            // Handle rotation with Backspace key
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                cockpitCam.transform.Rotate(0, 90, 0);
            }

            // Save the new position and rotation
            cockpitCamStartPosition = cockpitCam.transform.position;
            cockpitCamStartRotation = cockpitCam.transform.rotation;
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
        /*
        // Method to change the brightness of the lights
        void ToggleBrightness(bool isBright)
        {
            for (int i = 0; i < lights.Length; i++)
            {
                lights[i].intensity = isBright ? brightIntensity : dimIntensity;
            }

            logger.Info($"Lights set to {(isBright ? "bright" : "dim")}.");
        }

        // Method to change the brightness of fire1 light
        void ToggleBrightness1(bool isBright)
        {
            if (fzeroaxturbolight1 != null)
            {
                fzeroaxturbolight1.intensity = isBright ? brightIntensity : dimIntensity;
                // logger.Info($"{fzeroaxturbolight1.name} light set to {(isBright ? "bright" : "dim")}.");
            }
            else
            {
                logger.Debug("fzeroaxturbolight1 light component is not found.");
            }
        }

        // Method to change the brightness of fire2 light
        void ToggleBrightness2(bool isBright)
        {
            if (fzeroaxturbolight2 != null)
            {
                fzeroaxturbolight2.intensity = isBright ? brightIntensity : dimIntensity;
                // logger.Info($"{fzeroaxturbolight2.name} light set to {(isBright ? "bright" : "dim")}.");
            }
            else
            {
                logger.Debug("fzeroaxturbolight2 light component is not found.");
            }
        }
        */
        // Method to toggle the lights
        void ToggleLights(bool isActive)
        {
            for (int i = 0; i < lights.Length; i++)
            {
                lights[i].enabled = isActive;
            }

            logger.Info($"Lights turned {(isActive ? "on" : "off")}.");
        }

        // Method to toggle the fzeroaxturbolight1 light
        void ToggleLight1(bool isActive)
        {
            if (fzeroaxturbolight1 != null)
            {
                fzeroaxturbolight1.enabled = isActive;
                //          logger.Info($"{fzeroaxturbolight1.name} light turned {(isActive ? "on" : "off")}.");
            }
            else
            {
                logger.Debug("fzeroaxturbolight1 light component is not found.");
            }
        }

        // Method to toggle the fzeroaxturbolight1 light
        void ToggleLight2(bool isActive)
        {
            if (fzeroaxturbolight2 != null)
            {
                fzeroaxturbolight2.enabled = isActive;
                //       logger.Info($"{fzeroaxturbolight2.name} light turned {(isActive ? "on" : "off")}.");
            }
            else
            {
                logger.Debug("fzeroaxturbolight2 light component is not found.");
            }
        }

        IEnumerator FlashStrobes()
        {
            while (true)
            {
                // Choose a random strobe light to flash
                int randomIndex = Random.Range(0, 4);
                Light strobeLight = null;

                switch (randomIndex)
                {
                    case 0:
                        strobeLight = strobe1_light;
                        break;
                    case 1:
                        strobeLight = strobe2_light;
                        break;
                    case 2:
                        strobeLight = strobe3_light;
                        break;
                    case 3:
                        strobeLight = strobe4_light;
                        break;
                }

                // Turn on the chosen strobe light
                ToggleStrobeLight(strobeLight, true);

                // Wait for the flash duration
                yield return new WaitForSeconds(flashDuration);

                // Turn off the chosen strobe light
                ToggleStrobeLight(strobeLight, false);

                // Wait for the next flash interval
                yield return new WaitForSeconds(flashInterval - flashDuration);
            }
        }

        void ToggleStrobeLight(Light strobeLight, bool isActive)
        {
            if (strobeLight != null)
            {
                strobeLight.enabled = isActive;
                // logger.Info($"{strobeLight.name} light turned {(isActive ? "on" : "off")}.");
            }
            else
            {
                logger.Debug($"{strobeLight?.name} light component is not found.");
            }
        }
        void TurnOffAllStrobes()
        {
            ToggleStrobeLight(strobe1_light, false);
            ToggleStrobeLight(strobe2_light, false);
            ToggleStrobeLight(strobe3_light, false);
            ToggleStrobeLight(strobe4_light, false);
        }

        void ToggleStrobe1(bool isActive)
        {
            ToggleStrobeLight(strobe1_light, isActive);
        }

        void ToggleStrobe2(bool isActive)
        {
            ToggleStrobeLight(strobe2_light, isActive);
        }

        void ToggleStrobe3(bool isActive)
        {
            ToggleStrobeLight(strobe3_light, isActive);
        }

        void ToggleStrobe4(bool isActive)
        {
            ToggleStrobeLight(strobe4_light, isActive);
        }
        // Method to toggle the fireemissive object
        void ToggleTurboEmissive(bool isActive)
        {
            if (fzeroaxturbo1Object != null)
            {
                Renderer renderer = fzeroaxturbo1Object.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (isActive)
                    {
                        renderer.material.EnableKeyword("_EMISSION");
                    }
                    else
                    {
                        renderer.material.DisableKeyword("_EMISSION");
                    }
                    //    logger.Info($"fzeroaxturbo1Object object emission turned {(isActive ? "on" : "off")}.");
                }
                else
                {
                    logger.Debug("Renderer component is not found on fzeroaxturbo1Object object.");
                }
            }
            else
            {
                logger.Debug("fzeroaxturbo1Object object is not assigned.");
            }
            if (fzeroaxturbo2Object != null)
            {
                Renderer renderer = fzeroaxturbo2Object.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (isActive)
                    {
                        renderer.material.EnableKeyword("_EMISSION");
                    }
                    else
                    {
                        renderer.material.DisableKeyword("_EMISSION");
                    }
                    //           logger.Info($"fzeroaxturbo2Object object emission turned {(isActive ? "on" : "off")}.");
                }
                else
                {
                    logger.Debug("Renderer component is not found on fzeroaxturbo2Object object.");
                }
            }
            else
            {
                logger.Debug("fzeroaxturbo2Object object is not assigned.");
            }
        }
        // Method to disable emission
        void DisableEmission(Renderer[] emissiveObjects)
        {
            foreach (var renderer in emissiveObjects)
            {
                if (renderer != null)
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
                else
                {
                    logger.Debug("Renderer component not found on one of the emissive objects.");
                }
            }
        }

        // Method to log missing objects
        void LogMissingObject(Renderer[] emissiveObjects, string arrayName)
        {
            for (int i = 0; i < emissiveObjects.Length; i++)
            {
                if (emissiveObjects[i] == null)
                {
                    logger.Debug($"{arrayName} object at index {i} not found under fzeroaxControllerZ.");
                }
            }
        }

        IEnumerator fzeroaxFrontAttractPattern()
        {
            while (true)
            {
                ToggleEmissive(hazzardEmissiveObjects[0], true);
             //   ToggleEmissive(hazzardEmissiveObjects[1], false);
                yield return new WaitForSeconds(frontFlashDuration);

                ToggleEmissive(hazzardEmissiveObjects[0], false);
            //    ToggleEmissive(hazzardEmissiveObjects[1], true);
                yield return new WaitForSeconds(frontFlashDuration);
            }
        }

        IEnumerator SideAttractPattern(Renderer[] emissiveObjects)
        {
            while (true)
            {
                ToggleEmissive(emissiveObjects[0], true);
                ToggleEmissive(emissiveObjects[1], false);
                ToggleEmissive(emissiveObjects[2], false);
                yield return new WaitForSeconds(sideFlashDuration);

                ToggleEmissive(emissiveObjects[0], false);
                ToggleEmissive(emissiveObjects[1], true);
                ToggleEmissive(emissiveObjects[2], false);
                yield return new WaitForSeconds(sideFlashDuration);

                ToggleEmissive(emissiveObjects[0], false);
                ToggleEmissive(emissiveObjects[1], false);
                ToggleEmissive(emissiveObjects[2], true);
                yield return new WaitForSeconds(sideFlashDuration);
            }
        }

        IEnumerator fzeroaxFrontHazzardPattern()
        {
            while (true)
            {
                ToggleEmissive(hazzardEmissiveObjects[0], true);
            //    ToggleEmissive(hazzardEmissiveObjects[1], true);
                yield return new WaitForSeconds(frontDangerDuration);

                ToggleEmissive(hazzardEmissiveObjects[0], false);
             //   ToggleEmissive(hazzardEmissiveObjects[1], false);
                yield return new WaitForSeconds(frontDangerDelay);
            }
        }

        IEnumerator SideDangerPattern(Renderer[] emissiveObjects)
        {
            while (true)
            {
                ToggleEmissive(emissiveObjects[0], true);
                ToggleEmissive(emissiveObjects[1], false);
                ToggleEmissive(emissiveObjects[2], true);
                yield return new WaitForSeconds(sideDangerDuration);

                ToggleEmissive(emissiveObjects[0], false);
                ToggleEmissive(emissiveObjects[1], true);
                ToggleEmissive(emissiveObjects[2], false);
                yield return new WaitForSeconds(sideDangerDelay);
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

        public void TurnAllOff()
        {
            foreach (var renderer in hazzardEmissiveObjects)
            {
                ToggleEmissive(renderer, false);
            }

            foreach (var renderer in leftEmissiveObjects)
            {
                ToggleEmissive(renderer, false);
            }

            foreach (var renderer in rightEmissiveObjects)
            {
                ToggleEmissive(renderer, false);
            }
        }

        public void StartAttractPattern()
        {
            // Stop any currently running coroutines
            StopCurrentPatterns();

            // Start the attract pattern for front, left, and right
            frontCoroutine = StartCoroutine(fzeroaxFrontHazzardPattern());
         //   leftCoroutine = StartCoroutine(SideAttractPattern(leftEmissiveObjects));
         //   rightCoroutine = StartCoroutine(SideAttractPattern(rightEmissiveObjects));
        }

        public void StartDangerPattern()
        {
            // Stop any currently running coroutines
            StopCurrentPatterns();

            // Start the danger pattern for front, left, and right
            frontCoroutine = StartCoroutine(fzeroaxFrontAttractPattern());
          //  leftCoroutine = StartCoroutine(SideDangerPattern(leftEmissiveObjects));
          //  rightCoroutine = StartCoroutine(SideDangerPattern(rightEmissiveObjects));
        }

        private void StopCurrentPatterns()
        {
            if (frontCoroutine != null)
            {
                StopCoroutine(frontCoroutine);
                frontCoroutine = null;
            }

            if (leftCoroutine != null)
            {
                StopCoroutine(leftCoroutine);
                leftCoroutine = null;
            }

            if (rightCoroutine != null)
            {
                StopCoroutine(rightCoroutine);
                rightCoroutine = null;
            }
        }

        // Method to check if VR input is active
        bool VRInputActive()
        {
            // Assuming you have methods to check VR input
            return GetPrimaryThumbstick() != Vector2.zero || GetSecondaryThumbstick() != Vector2.zero;
        }

        // Placeholder methods to get VR thumbstick input (to be implemented with actual VR input handling)
        Vector2 GetPrimaryThumbstick()
        {
            // Implement VR primary thumbstick input handling here
            return Vector2.zero;
        }

        Vector2 GetSecondaryThumbstick()
        {
            // Implement VR secondary thumbstick input handling here
            return Vector2.zero;
        }
    }
}
