using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using EmuVR.InputManager;
using System.Collections;

namespace WIGUx.Modules.id3MotionSim
{
    public class id3MotionSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        private readonly float keyboardVelocityX = 4f;  // Velocity for keyboard input
        private readonly float keyboardVelocityY = 20f;  // Velocity for keyboard input
        private readonly float keyboardVelocityZ = 20f;  // Velocity for keyboard input
        private readonly float vrVelocity = 25f;        // Velocity for VR controller input

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

        private Transform id3XObject; // Reference to the main X object
        private Transform id3YObject; // Reference to the main Y object
        private Transform id3ZObject; // Reference to the main Z object
        private GameObject cockpitCam;    // Reference to the cockpit camera

        // Initial positions and rotations for resetting
        private Vector3 id3XStartPosition;
        private Quaternion id3XStartRotation;
        private Vector3 id3YStartPosition;
        private Quaternion id3YStartRotation;
        private Vector3 id3ZStartPosition;
        private Quaternion id3ZStartRotation;
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
        private float controllerrotationLimitZ = 135f;  // Rotation limit for Z-axis (stick or wheel)

        private float currentControllerRotationX = 0f;  // Current rotation for X-axis (stick or wheel)
        private float currentControllerRotationY = 0f;  // Current rotation for Y-axis (stick or wheel)
        private float currentControllerRotationZ = 0f;  // Current rotation for Z-axis (stick or wheel)

        private readonly float centeringControllerVelocityX = 350.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float centeringControllerVelocityY = 350.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float centeringControllerVelocityZ = 350.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float rotationSmoothness = 5f;  //sets the smoothness of the rotation
        private Transform id3ControllerX; // Reference to the main animated controller (wheel)
        private Vector3 id3ControllerXStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion id3ControllerXStartRotation; // Initial controlller positions and rotations for resetting
        private Transform id3ControllerY; // Reference to the main animated controller (wheel)
        private Vector3 id3ControllerYStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion id3ControllerYStartRotation; // Initial controlller positions and rotations for resetting
        private Transform id3ControllerZ; // Reference to the main animated controller (wheel)
        private Vector3 id3ControllerZStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion id3ControllerZStartRotation; // Initial controlller positions and rotations for resetting

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
        private Transform id3turbo1Object;
        private Transform id3turbo2Object;
        private Transform id3hazardlObject;
        private Transform id3hazardrObject;
        public float brightIntensity = 5.0f; // Set the brightness intensity level
        public float dimIntensity = 1.0f;    // Set the dim intensity level
        public string fire1Button = "Fire1"; // Name of the fire button
        public string fire2Button = "Fire2"; // Name of the fire button 
        public string fire3Button = "Fire3"; // Name of the fire button 
        public string JumpButton = "Jump"; // Name of the fire button 
        public Light id3turbolight1;
        public Light id3turbolight2;
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

        private readonly string[] compatibleGames = { "initdv3" };

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

            // Find id3X object in hierarchy
            id3XObject = transform.Find("id3X");
            if (id3XObject != null)
            {
                logger.Info("id3X object found.");
                id3XStartPosition = id3XObject.position;
                id3XStartRotation = id3XObject.rotation;

                // Find id3Y object under id3X
                id3YObject = id3XObject.Find("id3Y");
                if (id3YObject != null)
                {
                    logger.Info("id3Y object found.");
                    id3YStartPosition = id3YObject.position;
                    id3YStartRotation = id3YObject.rotation;

                    // Find id3Z object under id3Y
                    id3ZObject = id3YObject.Find("id3Z");
                    if (id3ZObject != null)
                    {
                        logger.Info("id3Z object found.");
                        id3ZStartPosition = id3ZObject.position;
                        id3ZStartRotation = id3ZObject.rotation;

                        // Find id3ControllerX under id3Z
                        id3ControllerX = id3ZObject.Find("id3ControllerX");
                        if (id3ControllerX != null)
                        {
                            logger.Info("id3ControllerX object found.");
                            // Store initial position and rotation of the stick
                            id3ControllerXStartPosition = id3ControllerX.transform.position; // these could cause the controller to mess up
                            id3ControllerXStartRotation = id3ControllerX.transform.rotation;

                            // Find id3ControllerY under id3ControllerX
                            id3ControllerY = id3ControllerX.Find("id3ControllerY");
                            if (id3ControllerY != null)
                            {
                                logger.Info("id3ControllerY object found.");
                                // Store initial position and rotation of the stick
                                id3ControllerYStartPosition = id3ControllerY.transform.position;
                                id3ControllerYStartRotation = id3ControllerY.transform.rotation;

                                // Find id3ControllerZ under id3ControllerY
                                id3ControllerZ = id3ControllerY.Find("id3ControllerZ");
                                if (id3ControllerZ != null)
                                {
                                    logger.Info("id3ControllerZ object found.");
                                    // Store initial position and rotation of the stick
                                    id3ControllerZStartPosition = id3ControllerZ.transform.position;
                                    id3ControllerZStartRotation = id3ControllerZ.transform.rotation;

                                    // Find id3turbo1Object object under id3ControllerZ
                                    id3turbo1Object = id3ControllerZ.Find("id3turbo1");
                                    if (id3turbo1Object != null)
                                    {
                                        logger.Info("id3turbo1 object found.");
                                        // Ensure the id3taillight1 object is initially off
                                        Renderer renderer = id3turbo1Object.GetComponent<Renderer>();
                                        if (renderer != null)
                                        {
                                            renderer.material.DisableKeyword("_EMISSION");
                                        }
                                        else
                                        {
                                            logger.Debug("Renderer component is not found on id3turbo1 object.");
                                        }
                                    }
                                    else
                                    {
                                        logger.Debug("id3turbo1 object not found under id3ControllerZ.");
                                    }

                                    // Find id3turbo2 object under id3Z
                                    id3turbo2Object = id3ControllerZ.Find("id3turbo2");
                                    if (id3turbo2Object != null)
                                    {
                                        logger.Info("id3turbo2 object found.");
                                        // Ensure the id3turbolight2 object is initially off
                                        Renderer renderer = id3turbo2Object.GetComponent<Renderer>();
                                        if (renderer != null)
                                        {
                                            renderer.material.DisableKeyword("_EMISSION");
                                        }
                                        else
                                        {
                                            logger.Debug("Renderer component is not found on id3turbo2 object.");
                                        }
                                    }
                                    else
                                    {
                                        logger.Debug("id3turbo2 object not found under id3ControllerZ.");
                                    }
                                }
                                else
                                {
                                    logger.Error("id3ControllerZ object not found under id3ControllerY!");
                                }
                            }
                            else
                            {
                                logger.Error("id3ControllerY object not found under id3ControllerX!");
                            }
                        }
                        else
                        {
                            logger.Error("id3ControllerX object not found under id3Z!");
                        }
                        /*
                        // Gets all Light components in the target object and its children
                        Light[] allLights = id3ControllerZ.GetComponentsInChildren<Light>();

                        // Log the names of the objects containing the Light components and filter out unwanted lights
                        foreach (Light light in allLights)
                        {
                            if (light.gameObject.name == "id3turbolight1")
                            {
                                id3turbolight1 = light;
                                logger.Info("Included Light found in object: " + light.gameObject.name);
                            }
                            else if (light.gameObject.name == "id3turbolight2")
                            {
                                id3turbolight2 = light;
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
                        */
                        // Find cockpit camera under cockpit
                        cockpitCam = id3ZObject.Find("eyes/cockpitcam")?.gameObject;
                        if (cockpitCam != null)
                        {
                            logger.Info("Cockpitcam object found.");

                            // Store initial position and rotation of cockpit cam
                            cockpitCamStartPosition = cockpitCam.transform.position;
                            cockpitCamStartRotation = cockpitCam.transform.rotation;
                        }
                        else
                        {
                            logger.Error("Cockpitcam object not found under id3Z!");
                        }
                    }
                    else
                    {
                        logger.Error("id3Z object not found under id3Y!");
                    }
                }
                else
                {
                    logger.Error("id3Y object not found under id3X!");
                }
            }
            else
            {
                logger.Error("id3X object not found!");
            }
            hazzardEmissiveObjects = new Renderer[1];
            //leftEmissiveObjects = new Renderer[1];
            //rightEmissiveObjects = new Renderer[1];

            hazzardEmissiveObjects[0] = transform.Find("id3hazard")?.GetComponent<Renderer>();
         //   hazzardEmissiveObjects[1] = id3ZObject.Find("id3hazardr")?.GetComponent<Renderer>();

            //leftEmissiveObjects[0] = id3ZObject.Find("left1")?.GetComponent<Renderer>();
            //leftEmissiveObjects[1] = id3ZObject.Find("left2")?.GetComponent<Renderer>();
           // leftEmissiveObjects[2] = id3ZObject.Find("left3")?.GetComponent<Renderer>();

            //rightEmissiveObjects[0] = id3ZObject.Find("right1")?.GetComponent<Renderer>();
            //rightEmissiveObjects[1] = id3ZObject.Find("right2")?.GetComponent<Renderer>();
            //rightEmissiveObjects[2] = id3ZObject.Find("right3")?.GetComponent<Renderer>();
            // Set lights to start

            StartAttractPattern();
            //ToggleLight1(false);
            //ToggleLight2(false);
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
                logger.Error("CockpitCam object not found under id3Z!");
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

            // Reset id3X to initial positions and rotations
            if (id3XObject != null)
            {
                id3XObject.position = id3XStartPosition;
                id3XObject.rotation = id3XStartRotation;
            }

            // Reset id3Y object to initial position and rotation
            if (id3YObject != null)
            {
                id3YObject.position = id3YStartPosition;
                id3YObject.rotation = id3YStartRotation;
            }
            // Reset id3Z object to initial position and rotation
            if (id3ZObject != null)
            {
                id3ZObject.position = id3ZStartPosition;
                id3ZObject.rotation = id3ZStartRotation;
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
                if (XInput.Get(XInput.Button.LIndexTrigger))
                {
                    float rotateY = vrVelocity * Time.deltaTime;

                    if (currentRotationY - rotateY > -rotationLimitY)
                    {
                        id3YObject.Rotate(0, rotateY, 0);
                        currentRotationY -= rotateY;
                        inputDetected = true;
                    }
                }
                if (XInput.Get(XInput.Button.RIndexTrigger))
                {
                    float rotateY = keyboardVelocityX * Time.deltaTime;

                    if (currentRotationY + rotateY < rotationLimitY)
                    {
                        id3YObject.Rotate(0, -rotateY, 0);
                        currentRotationY += rotateY;
                        inputDetected = true;
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
                    id3YObject.Rotate(0, -rotateY, 0);
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
                    id3XObject.Rotate(rotateX, 0, 0);
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
                float rotateY = vrVelocity * Time.deltaTime;

                if (currentRotationY - rotateY > -rotationLimitY)
                {
                    id3YObject.Rotate(0, rotateY, 0);
                    currentRotationY -= rotateY;
                    inputDetected = true;
                }
            }

            /*
            // Handle X rotation for id3YObject and id3ControllerX (Down Arrow or primaryThumbstick.y > 0)
            // Thumbstick direction: down
            if ((Input.GetKey(KeyCode.DownArrow) || primaryThumbstick.y > 0))
            {
                if (currentRotationX > -rotationLimitX)
                {
                    float rotateX = (Input.GetKey(KeyCode.DownArrow) ? keyboardVelocityX : primaryThumbstick.y * vrVelocity) * Time.deltaTime;
                    id3XObject.Rotate(rotateX, 0, 0);
                    currentRotationX -= rotateX;
                    inputDetected = true;
                }
                if (currentControllerRotationX > -controllerrotationLimitX)
                {
                    float controllerRotateX = (Input.GetKey(KeyCode.DownArrow) ? keyboardControllerVelocityX : primaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                    id3ControllerX.Rotate(controllerRotateX, 0, 0);
                    currentControllerRotationX -= controllerRotateX;
                    inputDetected = true;
                }
            }

            // Handle X rotation for id3YObject and id3ControllerX (Up Arrow or primaryThumbstick.y < 0)
            // Thumbstick direction: up
            if ((Input.GetKey(KeyCode.UpArrow) || primaryThumbstick.y < 0))
            {
                if (currentRotationX < rotationLimitX)
                {
                    float rotateX = (Input.GetKey(KeyCode.UpArrow) ? keyboardVelocityX : -primaryThumbstick.y * vrVelocity) * Time.deltaTime;
                    id3XObject.Rotate(-rotateX, 0, 0);
                    currentRotationX += rotateX;
                    inputDetected = true;
                }
                if (currentControllerRotationX < controllerrotationLimitX)
                {
                    float controllerRotateX = (Input.GetKey(KeyCode.UpArrow) ? keyboardControllerVelocityX : -primaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                    id3ControllerX.Rotate(-controllerRotateX, 0, 0);
                    currentControllerRotationX += controllerRotateX;
                    inputDetected = true;
                }
            }
            */
            // Handle Z rotation for id3XObject and id3ControllerZ (Down Arrow or primaryThumbstick.y < 0)
            // Thumbstick direction: Left
            if ((Input.GetKey(KeyCode.LeftArrow) || primaryThumbstick.x < 0))
            {
                if (currentRotationZ > -rotationLimitZ)
                {
                    float rotateZ = (Input.GetKey(KeyCode.LeftArrow) ? keyboardVelocityZ : -primaryThumbstick.x * vrVelocity) * Time.deltaTime;
                    id3ZObject.Rotate(0, 0, rotateZ);
                    currentRotationZ -= rotateZ;
                    inputDetected = true;
                }
                if (currentControllerRotationZ > -controllerrotationLimitZ)
                {
                    float controllerRotateZ = (Input.GetKey(KeyCode.LeftArrow) ? keyboardControllerVelocityZ : -primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                    id3ControllerZ.Rotate(0, 0, controllerRotateZ);
                    currentControllerRotationZ -= controllerRotateZ;
                    inputDetected = true;
                }
            }

            // Handle Z rotation for id3XObject and id3ControllerZ (Up Arrow or primaryThumbstick.y > 0)
            // Thumbstick direction: right
            if ((Input.GetKey(KeyCode.RightArrow) || primaryThumbstick.x > 0))
            {
                if (currentRotationZ < rotationLimitZ)
                {
                    float rotateZ = (Input.GetKey(KeyCode.RightArrow) ? keyboardVelocityZ : primaryThumbstick.x * vrVelocity) * Time.deltaTime;
                    id3ZObject.Rotate(0, 0, -rotateZ);
                    currentRotationZ += rotateZ;
                    inputDetected = true;
                }
                if (currentControllerRotationZ < controllerrotationLimitZ)
                {
                    float controllerRotateZ = (Input.GetKey(KeyCode.RightArrow) ? keyboardControllerVelocityZ : primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                    id3ControllerZ.Rotate(0, 0, -controllerRotateZ);
                    currentControllerRotationZ += controllerRotateZ;
                    inputDetected = true;
                }
            }
            /*
            // Handle left rotation (Thumbstick left)
            if (primaryThumbstick.x < 0 && currentRotationY < rotationLimitY) // Note the change in condition
            {
                float rotateY = primaryThumbstick.x * vrVelocity * Time.deltaTime;
                id3YObject.Rotate(0, rotateY, 0);  // Rotate Y in the opposite direction
                currentRotationY -= rotateY;  // Update current rotation (subtracting because the direction is swapped)
                inputDetected = true;
            }

            // Handle right rotation (Thumbstick right)
            if (primaryThumbstick.x > 0 && currentRotationY > -rotationLimitY) // Note the change in condition
            {
                float rotateY = primaryThumbstick.x * vrVelocity * Time.deltaTime;
                id3YObject.Rotate(0, rotateY, 0);  // Rotate Y in the opposite direction
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
                id3XObject.Rotate(unrotateX, 0, 0);
                currentRotationX -= unrotateX;
            }
            else if (currentRotationX < 0)
            {
                float unrotateX = Mathf.Min(centeringVelocityX * Time.deltaTime, -currentRotationX);
                id3XObject.Rotate(-unrotateX, 0, 0);
                currentRotationX += unrotateX;
            }

            // Center Y-axis
            if (currentRotationY > 0)
            {
                float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, currentRotationY);
                id3YObject.Rotate(0, unrotateY, 0);
                currentRotationY -= unrotateY;
            }
            else if (currentRotationY < 0)
            {
                float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, -currentRotationY);
                id3YObject.Rotate(0, -unrotateY, 0);
                currentRotationY += unrotateY;
            }

            // Center Z-axis
            if (currentRotationZ > 0)
            {
                float unrotateZ = Mathf.Min(centeringVelocityZ * Time.deltaTime, currentRotationZ);
                id3ZObject.Rotate(0, 0, unrotateZ);
                currentRotationZ -= unrotateZ;
            }
            else if (currentRotationZ < 0)
            {
                float unrotateZ = Mathf.Min(centeringVelocityZ * Time.deltaTime, -currentRotationZ);
                id3ZObject.Rotate(0, 0, -unrotateZ);
                currentRotationZ += unrotateZ;
            }
            //Centering for contoller

            // Center Y-axis Controller rotation
            if (currentControllerRotationY > 0)
            {
                float unrotateY = Mathf.Min(centeringControllerVelocityY * Time.deltaTime, currentControllerRotationY);
                id3ControllerY.Rotate(0, unrotateY, 0);   // Rotating to reduce the rotation
                currentControllerRotationY -= unrotateY;    // Reducing the positive rotation
            }
            else if (currentControllerRotationY < 0)
            {
                float unrotateY = Mathf.Min(centeringControllerVelocityY * Time.deltaTime, -currentControllerRotationY);
                id3ControllerY.Rotate(0, -unrotateY, 0);  // Rotating to reduce the rotation
                currentControllerRotationY += unrotateY;    // Reducing the negative rotation
            }

            // Center X-Axis Controller rotation
            if (currentControllerRotationX > 0)
            {
                float unrotateX = Mathf.Min(centeringControllerVelocityX * Time.deltaTime, currentControllerRotationX);
                id3ControllerX.Rotate(unrotateX, 0, 0);   // Rotating to reduce the rotation
                currentControllerRotationX -= unrotateX;    // Reducing the positive rotation
            }
            else if (currentControllerRotationX < 0)
            {
                float unrotateX = Mathf.Min(centeringControllerVelocityX * Time.deltaTime, -currentControllerRotationX);
                id3ControllerX.Rotate(-unrotateX, 0, 0);   // Rotating to reduce the rotation
                currentControllerRotationX += unrotateX;    // Reducing the positive rotation
            }

            // Center Z-axis Controller rotation
            if (currentControllerRotationZ > 0)
            {
                float unrotateZ = Mathf.Min(centeringControllerVelocityZ * Time.deltaTime, currentControllerRotationZ);
                id3ControllerZ.Rotate(0, 0, unrotateZ);   // Rotating to reduce the rotation
                currentControllerRotationZ -= unrotateZ;    // Reducing the positive rotation
            }
            else if (currentControllerRotationZ < 0)
            {
                float unrotateZ = Mathf.Min(centeringControllerVelocityZ * Time.deltaTime, -currentControllerRotationZ);
                id3ControllerZ.Rotate(0, 0, -unrotateZ);   // Rotating to reduce the rotation
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
            if (id3turbolight1 != null)
            {
                id3turbolight1.intensity = isBright ? brightIntensity : dimIntensity;
                // logger.Info($"{id3turbolight1.name} light set to {(isBright ? "bright" : "dim")}.");
            }
            else
            {
                logger.Debug("id3turbolight1 light component is not found.");
            }
        }

        // Method to change the brightness of fire2 light
        void ToggleBrightness2(bool isBright)
        {
            if (id3turbolight2 != null)
            {
                id3turbolight2.intensity = isBright ? brightIntensity : dimIntensity;
                // logger.Info($"{id3turbolight2.name} light set to {(isBright ? "bright" : "dim")}.");
            }
            else
            {
                logger.Debug("id3turbolight2 light component is not found.");
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
        /*
        // Method to toggle the id3turbolight1 light
        void ToggleLight1(bool isActive)
        {
            if (id3turbolight1 != null)
            {
                id3turbolight1.enabled = isActive;
                //          logger.Info($"{id3turbolight1.name} light turned {(isActive ? "on" : "off")}.");
            }
            else
            {
                logger.Debug("id3turbolight1 light component is not found.");
            }
        }

        // Method to toggle the id3turbolight1 light
        void ToggleLight2(bool isActive)
        {
            if (id3turbolight2 != null)
            {
                id3turbolight2.enabled = isActive;
                //       logger.Info($"{id3turbolight2.name} light turned {(isActive ? "on" : "off")}.");
            }
            else
            {
                logger.Debug("id3turbolight2 light component is not found.");
            }
        }
        */
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
            if (id3turbo1Object != null)
            {
                Renderer renderer = id3turbo1Object.GetComponent<Renderer>();
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
                    //    logger.Info($"id3turbo1Object object emission turned {(isActive ? "on" : "off")}.");
                }
                else
                {
                    logger.Debug("Renderer component is not found on id3turbo1Object object.");
                }
            }
            else
            {
                logger.Debug("id3turbo1Object object is not assigned.");
            }
            if (id3turbo2Object != null)
            {
                Renderer renderer = id3turbo2Object.GetComponent<Renderer>();
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
                    //           logger.Info($"id3turbo2Object object emission turned {(isActive ? "on" : "off")}.");
                }
                else
                {
                    logger.Debug("Renderer component is not found on id3turbo2Object object.");
                }
            }
            else
            {
                logger.Debug("id3turbo2Object object is not assigned.");
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
                    logger.Debug($"{arrayName} object at index {i} not found under id3ControllerZ.");
                }
            }
        }

        IEnumerator id3FrontAttractPattern()
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

        IEnumerator id3FrontHazzardPattern()
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
            frontCoroutine = StartCoroutine(id3FrontHazzardPattern());
         //   leftCoroutine = StartCoroutine(SideAttractPattern(leftEmissiveObjects));
         //   rightCoroutine = StartCoroutine(SideAttractPattern(rightEmissiveObjects));
        }

        public void StartDangerPattern()
        {
            // Stop any currently running coroutines
            StopCurrentPatterns();

            // Start the danger pattern for front, left, and right
            frontCoroutine = StartCoroutine(id3FrontAttractPattern());
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
