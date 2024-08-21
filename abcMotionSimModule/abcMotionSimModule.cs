using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using EmuVR.InputManager;
using System.Collections;

namespace WIGUx.Modules.abcMotionSim
{
    public class abcMotionSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        private readonly float keyboardVelocityX = 35.5f;  // Velocity for keyboard input
        private readonly float keyboardVelocityY = 35.5f;  // Velocity for keyboard input
        private readonly float keyboardVelocityZ = 35.5f;  // Velocity for keyboard input
        private readonly float vrVelocity = 35.5f;        // Velocity for VR controller input

        private readonly float centeringVelocityX = 20.5f;  // Velocity for centering rotation
        private readonly float centeringVelocityY = 20.5f;  // Velocity for centering rotation
        private readonly float centeringVelocityZ = 20.5f;  // Velocity for centering rotation

        private float adjustSpeed = 1.0f;  // Adjust this adjustment speed as needed a lower number will lead to smaller adustments

        private float rotationLimitX = 25f;  // Rotation limit for X-axis
        private float rotationLimitY = 0f;  // Rotation limit for Y-axis
        private float rotationLimitZ = 0f;  // Rotation limit for Z-axis

        private float currentRotationX = 0f;  // Current rotation for X-axis
        private float currentRotationY = 0f;  // Current rotation for Y-axis
        private float currentRotationZ = 0f;  // Current rotation for Z-axis

        // Controller animation 
        // Speeds for the animation of the in game flight stick or wheel
        private readonly float keyboardControllerVelocityX = 150.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityY = 150.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityZ = 150.5f;  // Velocity for keyboard input
        private readonly float vrControllerVelocity = 150.5f;        // Velocity for VR controller input

        private float controllerrotationLimitX = 12f;  // Rotation limit for X-axis (stick or wheel)
        private float controllerrotationLimitY = 0f;  // Rotation limit for Y-axis (stick or wheel)
        private float controllerrotationLimitZ = 12f;  // Rotation limit for Z-axis (stick or wheel)

        private float currentControllerRotationX = 0f;  // Current rotation for X-axis (stick or wheel)
        private float currentControllerRotationY = 0f;  // Current rotation for Y-axis (stick or wheel)
        private float currentControllerRotationZ = 0f;  // Current rotation for Z-axis (stick or wheel)

        private readonly float centeringControllerVelocityX = 50.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float centeringControllerVelocityY = 50.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float centeringControllerVelocityZ = 50.5f;  // Velocity for centering rotation (stick or wheel)
        
        //throttle stuff
        private readonly float throttlecenteringVelocity = 0.3f;  // Velocity for centering rotation (throttle)
        private readonly float throttleVelocity = 0.3f;  // Velocity for throttle movement
        private float abcthrottleLimit = 3f;  // position limit for throttle
        private float currentthrottlePosition = 0f;  // Current throttle position value
        private Quaternion abcthrottleStartRotation;  //shouldnt need this
        private Vector3 abcthrottleStartPosition; // Initial throttle positions and rotations for resetting
        private Transform abcthrottleObject; // Reference to the Throttle

        //controllers
        private Transform abcControllerX; // Reference to the main animated controller (wheel)
        private Vector3 abcControllerXStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion abcControllerXStartRotation; // Initial controlller positions and rotations for resetting
        private Transform abcControllerY; // Reference to the main animated controller (wheel)
        private Vector3 abcControllerYStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion abcControllerYStartRotation; // Initial controlller positions and rotations for resetting
        private Transform abcControllerZ; // Reference to the main animated controller (wheel)
        private Vector3 abcControllerZStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion abcControllerZStartRotation; // Initial controlller positions and rotations for resetting
     
        //cockpit
        private Transform abcXObject; // Reference to the main X object
        private Transform abcYObject; // Reference to the main Y object
        private Transform abcZObject; // Reference to the main Z object
        private GameObject cockpitCam;    // Reference to the cockpit camera

        // Initial positions and rotations for resetting
        private Vector3 abcXStartPosition;
        private Quaternion abcXStartRotation;
        private Vector3 abcYStartPosition;
        private Quaternion abcYStartRotation;
        private Vector3 abcZStartPosition;
        private Quaternion abcZStartRotation;
        private Vector3 cockpitCamStartPosition;
        private Quaternion cockpitCamStartRotation;

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
        private GameObject Hands;

        // Variable to store the game path
        public GameObject gameObject2;
        private string gamePath;
        //lights
        private Transform fireemissive1Object;
        private Transform fireemissive2Object;
        public Light fire1_light;
        public Light fire2_light;
        public float lightDuration = 0.35f; // Duration during which the lights will be on
                                            //      public float fireLightIntensity = 3.0f; // Intensity of the light when it is on
                                            //        public Color fireLightColor = Color.red; // Color of the light when it is on
        private Light[] lights;

        private bool inFocusMode = false;  // Flag to track focus mode state

        private readonly string[] compatibleGames = { "After Burner Climax.win", "After Burner - Climax.win" };

        private Dictionary<GameObject, Transform> originalParents = new Dictionary<GameObject, Transform>();  // Dictionary to store original parents of objects

        void Start()
        {
            // Find references to PlayerCamera and VR setup objects
            playerCamera = PlayerVRSetup.PlayerCamera.gameObject;
            playerVRSetup = PlayerVRSetup.PlayerRig.gameObject;
            // Check if objects are found
            CheckObject(playerCamera, "PlayerCamera");

            // Find the GameObject named "Hands"
            GameObject handsObject = GameObject.Find("Hands");
            // Check the object and log the result
            CheckObject(handsObject, "Hands");

            // Find abcX object in hierarchy
            abcXObject = transform.Find("abcX");
            if (abcXObject != null)
            {
                logger.Info("abcX object found.");
                abcXStartPosition = abcXObject.position;
                abcXStartRotation = abcXObject.rotation;

                // Find abcY object under abcX
                abcYObject = abcXObject.Find("abcY");
                if (abcYObject != null)
                {
                    logger.Info("abcY object found.");
                    abcYStartPosition = abcYObject.position;
                    abcYStartRotation = abcYObject.rotation;

                    // Find abcZ object under abcY
                    abcZObject = abcYObject.Find("abcZ");
                    if (abcZObject != null)
                    {
                        logger.Info("abcZ object found.");
                        abcZStartPosition = abcZObject.position;
                        abcZStartRotation = abcZObject.rotation;

                        // Find fireemissive object under abcZ
                        fireemissive1Object = abcZObject.Find("fireemissive1");
                        if (fireemissive1Object != null)
                        {
                            logger.Info("fireemissive1 object found.");
                            // Ensure the fireemissive1 object is initially off
                            Renderer renderer = fireemissive1Object.GetComponent<Renderer>();
                            if (renderer != null)
                            {
                                renderer.material.DisableKeyword("_EMISSION");
                            }
                            else
                            {
                                logger.Debug("Renderer component is not found on fireemissive1 object.");
                            }
                        }
                        else
                        {
                            logger.Debug("fireemissive1 object not found under abcZ.");
                        }

                        // Find fireemissive object under abcZ
                        fireemissive2Object = abcZObject.Find("fireemissive2");
                        if (fireemissive2Object != null)
                        {
                            logger.Info("fireemissive2 object found.");
                            // Ensure the fireemissive2 object is initially off
                            Renderer renderer = fireemissive2Object.GetComponent<Renderer>();
                            if (renderer != null)
                            {
                                renderer.material.DisableKeyword("_EMISSION");
                            }
                            else
                            {
                                logger.Debug("Renderer component is not found on fireemissive2 object.");
                            }
                        }
                        else
                        {
                            logger.Debug("fireemissive2 object not found under abcZ.");
                        }
                        // Find fire1_light object
                        fire1_light = abcZObject.Find("fire1_light").GetComponent<Light>();
                        if (fire1_light != null)
                        {
                            logger.Info("fire1_light object found.");
                        }
                        else
                        {
                            logger.Debug("fire1_light object not found.");
                        }

                        // Find fire2_light object
                        fire2_light = abcZObject.Find("fire2_light").GetComponent<Light>();
                        if (fire2_light != null)
                        {
                            logger.Info("fire2_light object found.");
                        }
                        else
                        {
                            logger.Debug("fire2_light object not found.");
                        }

                        // Find plunger4 object under button4
                        abcthrottleObject = abcZObject.Find("throttle");
                        if (abcthrottleObject != null)
                        {
                            logger.Info("abcthrottleObject found.");
                            abcthrottleStartPosition = abcthrottleObject.position;
                            abcthrottleStartRotation = abcthrottleObject.rotation;
                        }
                        else
                        {
                            logger.Error("abcthrottleObject object not found under abcZObject!");
                        }

                        // Find abcControllerX under abcZ
                        abcControllerX = abcZObject.Find("abcControllerX");
                        if (abcControllerX != null)
                        {
                            logger.Info("abcControllerX object found.");
                            // Store initial position and rotation of the stick
                            abcControllerXStartPosition = abcControllerX.transform.position; // these could cause the controller to mess up
                            abcControllerXStartRotation = abcControllerX.transform.rotation;

                            // Find abcControllerY under abcControllerX
                            abcControllerY = abcControllerX.Find("abcControllerY");
                            if (abcControllerY != null)
                            {
                                logger.Info("abcControllerY object found.");
                                // Store initial position and rotation of the stick
                                abcControllerYStartPosition = abcControllerY.transform.position;
                                abcControllerYStartRotation = abcControllerY.transform.rotation;

                                // Find abcControllerZ under abcControllerY
                                abcControllerZ = abcControllerY.Find("abcControllerZ");
                                if (abcControllerZ != null)
                                {
                                    logger.Info("abcControllerZ object found.");
                                    // Store initial position and rotation of the stick
                                    abcControllerZStartPosition = abcControllerZ.transform.position;
                                    abcControllerZStartRotation = abcControllerZ.transform.rotation;
                                }
                                else
                                {
                                    logger.Error("abcControllerZ object not found under abcControllerY!");
                                }
                            }
                            else
                            {
                                logger.Error("abcControllerY object not found under abcControllerX!");
                            }
                        }
                        else
                        {
                            logger.Error("abcControllerX object not found under abcZ!");
                        }

                        // Find cockpit camera under abcZ
                        cockpitCam = abcZObject.Find("eyes/cockpitcam")?.gameObject;
                        if (cockpitCam != null)
                        {
                            logger.Info("Cockpitcam object found.");

                            // Store initial position and rotation of cockpit cam
                            cockpitCamStartPosition = cockpitCam.transform.position;
                            cockpitCamStartRotation = cockpitCam.transform.rotation;
                        }
                        else
                        {
                            logger.Error("Cockpitcam object not found under abcZ!");
                        }
                    }
                    else
                    {
                        logger.Error("abcZ object not found under abcY!");
                    }
                }
                else
                {
                    logger.Error("abcY object not found under abcX!");
                }
            }
            else
            {
                logger.Error("abcX object not found!");
            }
        }

        void Update()
        {
            bool inputDetected = false;  // Initialize at the beginning of the Update method

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

                // Fire2
                if (Input.GetButtonDown("Fire2"))
                {
                    ToggleFireEmissive1(true);
                    ToggleFireEmissive2(true);
                    ToggleLight1(true);
                    ToggleLight2(true);
                    inputDetected = true;
                }

                // Reset position on button release
                if (Input.GetButtonUp("Fire2"))
                {
                    ToggleFireEmissive1(false);
                    ToggleFireEmissive2(false);
                    ToggleLight1(false);
                    ToggleLight2(false);
                    inputDetected = true;
                }

                // Fire3
                if (Input.GetButtonDown("Fire3"))
                {
                    ToggleFireEmissive1(true);
                    ToggleFireEmissive2(true);
                    ToggleLight1(true);
                    ToggleLight2(true);
                    inputDetected = true;
                }

                // Reset position on button release
                if (Input.GetButtonUp("Fire3"))
                {
                    ToggleFireEmissive1(false);
                    ToggleFireEmissive2(false);
                    ToggleLight1(false);
                    ToggleLight2(false);
                    inputDetected = true;
                }

                // Jump
                if (Input.GetButtonDown("Jump"))
                {
                    inputDetected = true;
                }

                // Reset position on button release
                if (Input.GetButtonUp("Jump"))
                {

                    inputDetected = true;
                }

            }
        }

        void StartFocusMode()
        {
            string controlledSystemGamePathString = GameSystem.ControlledSystem.Game.path != null ? GameSystem.ControlledSystem.Game.path.ToString() : null;
            logger.Info($"Controlled System Game path String: {controlledSystemGamePathString}");
            logger.Info("Compatible Rom Dectected, Greetings...");
            logger.Info("Sega After Burner Climax Motion Sim starting...");
            logger.Info("GET READY, AGAIN!!!...");
            /*
            if (handsObject != null)
            {
                handsObject.SetActive(false); // Disable the object
            }
            */
            // Set objects as children of cockpit cam for focus mode
            if (cockpitCam != null)
            {
                cockpitCam.transform.position = cockpitCamStartPosition; // new hotness
                cockpitCam.transform.rotation = cockpitCamStartRotation; // new hotness
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
                logger.Error("CockpitCam object not found under abcZ!");
            }

            // Reset rotation allowances and current rotation values
            currentRotationX = 0f;
            currentRotationY = 0f;
            currentRotationZ = 0f;
            currentControllerRotationX = 0f;
            currentControllerRotationY = 0f;
            currentControllerRotationZ = 0f;
            currentControllerRotationZ = 0f;

            inFocusMode = true;  // Set focus mode flag
        }

        void EndFocusMode()
        {
            logger.Info("Exiting Focus Mode...");
            // Restore original parents of objects
            RestoreOriginalParent(playerCamera, "PlayerCamera");
            RestoreOriginalParent(playerVRSetup, "PlayerVRSetup.PlayerRig");



            // Reset abcX to initial positions and rotations
            if (abcXObject != null)
            {
                abcXObject.position = abcXStartPosition;
                abcXObject.rotation = abcXStartRotation;
            }

            // Reset abcY object to initial position and rotation
            if (abcYObject != null)
            {
                abcYObject.position = abcYStartPosition;
                abcYObject.rotation = abcYStartRotation;
            }
            // Reset abcZ object to initial position and rotation
            if (abcZObject != null)
            {
                abcZObject.position = abcZStartPosition;
                abcZObject.rotation = abcZStartRotation;
            }
            // Reset abcthrottle object to initial position and rotation
            if (abcthrottleObject != null)
            {
                abcthrottleObject.position = abcthrottleStartPosition;
                abcthrottleObject.rotation = abcthrottleStartRotation;
            }
            // Reset abccontrollerX object to initial position and rotation
            if (abcControllerX != null)
            {
                // abcController.position = abcControllerStartPosition;
                abcControllerX.rotation = abcControllerXStartRotation;
            }

            // Reset abccontrollerY object to initial position and rotation
            if (abcControllerY != null)
            {
                // abcControllerY.position = abcControllerStartPosition; 
                abcControllerY.rotation = abcControllerYStartRotation;
            }

            // Reset abccontrollerZ object to initial position and rotation
            if (abcControllerZ != null)
            {
                // abcControllerY.position = abcControllerStartPosition;
                abcControllerZ.rotation = abcControllerZStartRotation;
            }

            // Reset cockpit cam to initial position and rotation
            if (cockpitCam != null)
            {
                cockpitCam.transform.position = cockpitCamStartPosition;
                cockpitCam.transform.rotation = cockpitCamStartRotation;
            }

            // Reset rotation allowances and current rotation values
            currentRotationX = 0f;
            currentRotationY = 0f;
            currentRotationZ = 0f;
            currentControllerRotationX = 0f;
            currentControllerRotationY = 0f;
            currentControllerRotationZ = 0f;

            logger.Info("Resetting Positions");
            ResetPositions();

            inFocusMode = false;  // Clear focus mode flag
        }

        //sexy new combined input handler
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

                // Handle X press
                if (XInput.GetDown(XInput.Button.X))
                {
                    ToggleFireEmissive1(true);
                    ToggleFireEmissive2(true);
                    ToggleLight1(true);
                    ToggleLight2(true);
                    inputDetected = true;
                }

                // Reset on X release
                if (XInput.GetUp(XInput.Button.X))
                {
                    ToggleFireEmissive1(false);
                    ToggleFireEmissive2(false);
                    ToggleLight1(false);
                    ToggleLight2(false);
                    inputDetected = true;
                }

                // Handle LT press and throttle movement
                if (XInput.GetDown(XInput.Button.LIndexTrigger) && currentthrottlePosition > -abcthrottleLimit) // position limit for throttle
                {
                    abcthrottleObject.Translate(-0.03f, 0, 0); // Using Translate method to move the object
                    inputDetected = true;
                }

                // Handle LT press and throttle movement
                if (XInput.GetUp(XInput.Button.LIndexTrigger) && currentthrottlePosition > -abcthrottleLimit) // position limit for throttle
                {
                    abcthrottleObject.Translate(0.03f, 0, 0);
                    inputDetected = true;
                }
                // Handle RT press and throttle movement
                if (XInput.GetDown(XInput.Button.RIndexTrigger) && currentthrottlePosition < abcthrottleLimit) // position limit for throttle
                {
                    abcthrottleObject.Translate(0.03f, 0, 0); // Using Translate method to move the object
                    inputDetected = true;
                }
                // Handle RT press and throttle movement
                if (XInput.GetUp(XInput.Button.RIndexTrigger) && currentthrottlePosition < abcthrottleLimit) // position limit for throttle
                {
                    abcthrottleObject.Translate(-0.03f, 0, 0);
                    inputDetected = true;
                }

                // Handle RB button press for plunger position
                if (XInput.GetUp(XInput.Button.RShoulder) || Input.GetKeyDown(KeyCode.JoystickButton5))
                {

                    inputDetected = true;
                }

                // Reset position on RB button release
                if (XInput.GetUp(XInput.Button.RShoulder) || Input.GetKeyUp(KeyCode.JoystickButton5))
                {
                    inputDetected = true;
                }

                // Handle LB button press for plunger position
                if (XInput.GetDown(XInput.Button.LShoulder) || Input.GetKeyDown(KeyCode.JoystickButton4))
                {
                    inputDetected = true;
                }

                // Reset position on LB button release
                if (XInput.GetUp(XInput.Button.LShoulder) || Input.GetKeyUp(KeyCode.JoystickButton4))
                {
                    inputDetected = true;
                }
            }


            // Handle LT press and throttle movement
            if (XInput.GetDown(XInput.Button.LIndexTrigger) && currentthrottlePosition > -abcthrottleLimit) // position limit for throttle
            {
                float move = throttleVelocity * Time.deltaTime;
                abcthrottleObject.Translate(0, 0, -move); // Using Translate method to move the object
                currentthrottlePosition -= move; // Update the throttle position value
                inputDetected = true;
            }

            // Handle RT press and throttle movement
            if (XInput.GetDown(XInput.Button.RIndexTrigger) && currentthrottlePosition < abcthrottleLimit) // position limit for throttle
            {
                float move = throttleVelocity * Time.deltaTime;
                abcthrottleObject.Translate(0, 0, move); // Using Translate method to move the object
                currentthrottlePosition += move; // Update the throttle position value
                inputDetected = true;
            }

            // Handle X rotation for abcYObject and abcControllerX (Right Arrow or primaryThumbstick.x > 0)
            // Thumbstick direction: right
            if ((Input.GetKey(KeyCode.RightArrow) || primaryThumbstick.x > 0))
            {
                if (currentRotationX < rotationLimitX)
                {
                    float rotateX = (Input.GetKey(KeyCode.RightArrow) ? keyboardVelocityX : primaryThumbstick.x * vrVelocity) * Time.deltaTime;
                    abcYObject.Rotate(-rotateX, 0, 0);
                    currentRotationX += rotateX;
                    inputDetected = true;
                }
                if (currentControllerRotationX < controllerrotationLimitX)
                {
                    float controllerRotateX = (Input.GetKey(KeyCode.RightArrow) ? keyboardControllerVelocityX : primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                    abcControllerX.Rotate(-controllerRotateX, 0, 0);
                    currentControllerRotationX += controllerRotateX;
                    inputDetected = true;
                }
            }

            // Handle X rotation for abcYObject and abcControllerX (Left Arrow or primaryThumbstick.x < 0)
            // Thumbstick direction: left
            if ((Input.GetKey(KeyCode.LeftArrow) || primaryThumbstick.x < 0))
            {
                if (currentRotationX > -rotationLimitX)
                {
                    float rotateX = (Input.GetKey(KeyCode.LeftArrow) ? keyboardVelocityX : -primaryThumbstick.x * vrVelocity) * Time.deltaTime;
                    abcYObject.Rotate(rotateX, 0, 0);
                    currentRotationX -= rotateX;
                    inputDetected = true;
                }
                if (currentControllerRotationX > -controllerrotationLimitX)
                {
                    float controllerRotateX = (Input.GetKey(KeyCode.LeftArrow) ? keyboardControllerVelocityX : -primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                    abcControllerX.Rotate(controllerRotateX, 0, 0);
                    currentControllerRotationX -= controllerRotateX;
                    inputDetected = true;
                }
            }

            // Handle Z rotation for abcXObject and abcControllerZ (Down Arrow or primaryThumbstick.y < 0)
            // Thumbstick direction: down
            if ((Input.GetKey(KeyCode.DownArrow) || primaryThumbstick.y < 0))
            {
                if (currentRotationZ > -rotationLimitZ)
                {
                    float rotateZ = (Input.GetKey(KeyCode.DownArrow) ? keyboardVelocityZ : -primaryThumbstick.y * vrVelocity) * Time.deltaTime;
                    abcXObject.Rotate(0, 0, rotateZ);
                    currentRotationZ -= rotateZ;
                    inputDetected = true;
                }
                if (currentControllerRotationZ > -controllerrotationLimitZ)
                {
                    float controllerRotateZ = (Input.GetKey(KeyCode.DownArrow) ? keyboardControllerVelocityZ : -primaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                    abcControllerZ.Rotate(0, 0, controllerRotateZ);
                    currentControllerRotationZ -= controllerRotateZ;
                    inputDetected = true;
                }
            }

            // Handle Z rotation for abcXObject and abcControllerZ (Up Arrow or primaryThumbstick.y > 0)
            // Thumbstick direction: up
            if ((Input.GetKey(KeyCode.UpArrow) || primaryThumbstick.y > 0))
            {
                if (currentRotationZ < rotationLimitZ)
                {
                    float rotateZ = (Input.GetKey(KeyCode.UpArrow) ? keyboardVelocityZ : primaryThumbstick.y * vrVelocity) * Time.deltaTime;
                    abcXObject.Rotate(0, 0, -rotateZ);
                    currentRotationZ += rotateZ;
                    inputDetected = true;
                }
                if (currentControllerRotationZ < controllerrotationLimitZ)
                {
                    float controllerRotateZ = (Input.GetKey(KeyCode.UpArrow) ? keyboardControllerVelocityZ : primaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                    abcControllerZ.Rotate(0, 0, -controllerRotateZ);
                    currentControllerRotationZ += controllerRotateZ;
                    inputDetected = true;
                }
            }

            // Handle Y rotation for abcYObject and abcControllerY (Unused axis or secondaryThumbstick.x)
            // Thumbstick direction: left/right
            if ((Input.GetKey(KeyCode.None) || secondaryThumbstick.x != 0))
            {
                if (secondaryThumbstick.x > 0 && currentRotationY < rotationLimitY)
                {
                    float rotateY = secondaryThumbstick.x * vrVelocity * Time.deltaTime;
                    abcYObject.Rotate(0, rotateY, 0);
                    currentRotationY += rotateY;
                    inputDetected = true;
                }
                if (secondaryThumbstick.x > 0 && currentControllerRotationY < controllerrotationLimitY)
                {
                    float controllerRotateY = secondaryThumbstick.x * vrControllerVelocity * Time.deltaTime;
                    abcControllerY.Rotate(0, controllerRotateY, 0);
                    currentControllerRotationY += controllerRotateY;
                    inputDetected = true;
                }
                if (secondaryThumbstick.x < 0 && currentRotationY > -rotationLimitY)
                {
                    float rotateY = secondaryThumbstick.x * vrVelocity * Time.deltaTime;
                    abcYObject.Rotate(0, rotateY, 0);
                    currentRotationY -= rotateY;
                    inputDetected = true;
                }
                if (secondaryThumbstick.x < 0 && currentControllerRotationY > -controllerrotationLimitY)
                {
                    float controllerRotateY = secondaryThumbstick.x * vrControllerVelocity * Time.deltaTime;
                    abcControllerY.Rotate(0, controllerRotateY, 0);
                    currentControllerRotationY -= controllerRotateY;
                    inputDetected = true;
                }
            }
            /*
            // Handle unused axis rotation for abcYObject and abcControllerY
            // This can be mapped to secondaryThumbstick.y for additional rotation control
            // Thumbstick direction: up/down
            if (secondaryThumbstick.y != 0)
            {
                if (secondaryThumbstick.y > 0 && currentRotationY < rotationLimitY)
                {
                    float rotateY = secondaryThumbstick.y * vrVelocity * Time.deltaTime;
                    abcYObject.Rotate(0, rotateY, 0);
                    currentRotationY += rotateY;
                    inputDetected = true;
                }
                if (secondaryThumbstick.y > 0 && currentControllerRotationY < controllerrotationLimitY)
                {
                    float controllerRotateY = secondaryThumbstick.y * vrControllerVelocity * Time.deltaTime;
                    abcControllerY.Rotate(0, controllerRotateY, 0);
                    currentControllerRotationY += controllerRotateY;
                    inputDetected = true;
                }
                if (secondaryThumbstick.y < 0 && currentRotationY > -rotationLimitY)
                {
                    float rotateY = secondaryThumbstick.y * vrVelocity * Time.deltaTime;
                    abcYObject.Rotate(0, rotateY, 0);
                    currentRotationY -= rotateY;
                    inputDetected = true;
                }
                if (secondaryThumbstick.y < 0 && currentControllerRotationY > -controllerrotationLimitY)
                {
                    float controllerRotateY = secondaryThumbstick.y * vrControllerVelocity * Time.deltaTime;
                    abcControllerY.Rotate(0, controllerRotateY, 0);
                    currentControllerRotationY -= controllerRotateY;
                    inputDetected = true;
                }
            }
            */
            // Center the rotation if no input is detected
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
        }

        void CenterRotation()
        {
            // Center X-axis
            if (currentRotationX > 0)
            {
                float unrotateX = Mathf.Min(centeringVelocityX * Time.deltaTime, currentRotationX);
                abcYObject.Rotate(unrotateX, 0, 0);
                currentRotationX -= unrotateX;
            }
            else if (currentRotationX < 0)
            {
                float unrotateX = Mathf.Min(centeringVelocityX * Time.deltaTime, -currentRotationX);
                abcYObject.Rotate(-unrotateX, 0, 0);
                currentRotationX += unrotateX;
            }

            // Center Y-axis
            if (currentRotationY > 0)
            {
                float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, currentRotationY);
                abcXObject.Rotate(0, unrotateY, 0);
                currentRotationY -= unrotateY;
            }
            else if (currentRotationY < 0)
            {
                float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, -currentRotationY);
                abcXObject.Rotate(0, -unrotateY, 0);
                currentRotationY += unrotateY;
            }

            // Center Z-axis
            if (currentRotationZ > 0)
            {
                float unrotateZ = Mathf.Min(centeringVelocityZ * Time.deltaTime, currentRotationZ);
                abcXObject.Rotate(0, 0, unrotateZ);
                currentRotationZ -= unrotateZ;
            }
            else if (currentRotationZ < 0)
            {
                float unrotateZ = Mathf.Min(centeringVelocityZ * Time.deltaTime, -currentRotationZ);
                abcXObject.Rotate(0, 0, -unrotateZ);
                currentRotationZ += unrotateZ;
            }
            //Centering for contoller

            // Center Y-axis Controller rotation
            if (currentControllerRotationY > 0)
            {
                float unrotateY = Mathf.Min(centeringControllerVelocityY * Time.deltaTime, currentControllerRotationY);
                abcControllerY.Rotate(0, unrotateY, 0);   // Rotating to reduce the rotation
                currentControllerRotationY -= unrotateY;    // Reducing the positive rotation
            }
            else if (currentControllerRotationY < 0)
            {
                float unrotateY = Mathf.Min(centeringControllerVelocityY * Time.deltaTime, -currentControllerRotationY);
                abcControllerY.Rotate(0, -unrotateY, 0);  // Rotating to reduce the rotation
                currentControllerRotationY += unrotateY;    // Reducing the negative rotation
            }

            // Center X-Axis Controller rotation
            if (currentControllerRotationX > 0)
            {
                float unrotateX = Mathf.Min(centeringControllerVelocityX * Time.deltaTime, currentControllerRotationX);
                abcControllerX.Rotate(unrotateX, 0, 0);   // Rotating to reduce the rotation
                currentControllerRotationX -= unrotateX;    // Reducing the positive rotation
            }
            else if (currentControllerRotationX < 0)
            {
                float unrotateX = Mathf.Min(centeringControllerVelocityX * Time.deltaTime, -currentControllerRotationX);
                abcControllerX.Rotate(-unrotateX, 0, 0);   // Rotating to reduce the rotation
                currentControllerRotationX += unrotateX;    // Reducing the positive rotation
            }

            // Center Z-axis Controller rotation
            if (currentControllerRotationZ > 0)
            {
                float unrotateZ = Mathf.Min(centeringControllerVelocityZ * Time.deltaTime, currentControllerRotationZ);
                abcControllerZ.Rotate(0, 0, unrotateZ);   // Rotating to reduce the rotation
                currentControllerRotationZ -= unrotateZ;    // Reducing the positive rotation
            }
            else if (currentControllerRotationZ < 0)
            {
                float unrotateZ = Mathf.Min(centeringControllerVelocityZ * Time.deltaTime, -currentControllerRotationZ);
                abcControllerZ.Rotate(0, 0, -unrotateZ);   // Rotating to reduce the rotation
                currentControllerRotationZ += unrotateZ;    // Reducing the positive rotation
            }

            if (currentthrottlePosition > 0)
            {
                float unmove = Mathf.Min(throttlecenteringVelocity * Time.deltaTime, currentthrottlePosition);
                abcthrottleObject.Translate(-unmove, 0, 0); // Move towards center
                currentthrottlePosition -= unmove;
            }
            else if (currentthrottlePosition < 0)
            {
                float unmove = Mathf.Min(throttlecenteringVelocity * Time.deltaTime, -currentthrottlePosition);
                abcthrottleObject.Translate(unmove, 0, 0); // Move towards center
                currentthrottlePosition += unmove;
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

        // Method to toggle the fire1 emissive object
        void ToggleFireEmissive1(bool isActive)
        {
            if (fireemissive1Object != null)
            {
                Renderer renderer = fireemissive1Object.GetComponent<Renderer>();
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
                    logger.Info($"fireemissive1 object emission turned {(isActive ? "on" : "off")}.");
                }
                else
                {
                    logger.Debug("Renderer component is not found on fireemissive1 object.");
                }
            }
            else
            {
                logger.Debug("fireemissive1 object is not assigned.");
            }
        }

        // Method to toggle the fire2 emissive object
        void ToggleFireEmissive2(bool isActive)
        {
            if (fireemissive2Object != null)
            {
                Renderer renderer = fireemissive2Object.GetComponent<Renderer>();
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
                    //    logger.Info($"fireemissive2 object emission turned {(isActive ? "on" : "off")}.");
                }
                else
                {
                    logger.Debug("Renderer component is not found on fireemissive2 object.");
                }
            }
            else
            {
                logger.Debug("fireemissive2 object is not assigned.");
            }
        }

        // Method to toggle the fire1 light
        void ToggleLight1(bool isActive)
        {
            if (fire1_light != null)
            {
                fire1_light.enabled = isActive;
                //     logger.Info($"{fire1_light.name} light turned {(isActive ? "on" : "off")}.");
            }
            else
            {
                logger.Debug("Fire1 light component is not found.");
            }
        }

        // Method to toggle the fire2 light
        void ToggleLight2(bool isActive)
        {
            if (fire2_light != null)
            {
                fire2_light.enabled = isActive;
                //     logger.Info($"{fire2_light.name} light turned {(isActive ? "on" : "off")}.");
            }
            else
            {
                logger.Debug("Fire2 light component is not found.");
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
