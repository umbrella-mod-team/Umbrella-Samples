using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using EmuVR.InputManager;
using System.Collections;

namespace WIGUx.Modules.glocMotionSim
{
    public class glocMotionSimController : MonoBehaviour
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
        private float glocthrottleLimit = 3f;  // position limit for throttle
        private float currentthrottlePosition = 0f;  // Current throttle position value
        private Quaternion glocthrottleStartRotation;  //shouldnt need this
        private Vector3 glocthrottleStartPosition; // Initial throttle positions and rotations for resetting
        private Transform glocthrottleObject; // Reference to the Throttle

        //controllers
        private Transform glocControllerX; // Reference to the main animated controller (wheel)
        private Vector3 glocControllerXStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion glocControllerXStartRotation; // Initial controlller positions and rotations for resetting
        private Transform glocControllerY; // Reference to the main animated controller (wheel)
        private Vector3 glocControllerYStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion glocControllerYStartRotation; // Initial controlller positions and rotations for resetting
        private Transform glocControllerZ; // Reference to the main animated controller (wheel)
        private Vector3 glocControllerZStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion glocControllerZStartRotation; // Initial controlller positions and rotations for resetting
     
        //cockpit
        private Transform glocXObject; // Reference to the main X object
        private Transform glocYObject; // Reference to the main Y object
        private Transform glocZObject; // Reference to the main Z object
        private GameObject cockpitCam;    // Reference to the cockpit camera

        // Initial positions and rotations for resetting
        private Vector3 glocXStartPosition;
        private Quaternion glocXStartRotation;
        private Vector3 glocYStartPosition;
        private Quaternion glocYStartRotation;
        private Vector3 glocZStartPosition;
        private Quaternion glocZStartRotation;
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
        public float lightDuration = 0.35f; // Duration during which the lights will be on
                                            //      public float fireLightIntensity = 3.0f; // Intensity of the light when it is on
                                            //        public Color fireLightColor = Color.red; // Color of the light when it is on
        private Light[] lights;

        private bool inFocusMode = false;  // Flag to track focus mode state

        private readonly string[] compatibleGames = { "gloc.zip" };

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

            // Find glocX object in hierarchy
            glocXObject = transform.Find("glocX");
            if (glocXObject != null)
            {
                logger.Info("glocX object found.");
                glocXStartPosition = glocXObject.position;
                glocXStartRotation = glocXObject.rotation;

                // Find glocY object under glocX
                glocYObject = glocXObject.Find("glocY");
                if (glocYObject != null)
                {
                    logger.Info("glocY object found.");
                    glocYStartPosition = glocYObject.position;
                    glocYStartRotation = glocYObject.rotation;

                    // Find glocZ object under glocY
                    glocZObject = glocYObject.Find("glocZ");
                    if (glocZObject != null)
                    {
                        logger.Info("glocZ object found.");
                        glocZStartPosition = glocZObject.position;
                        glocZStartRotation = glocZObject.rotation;

                        // Find plunger4 object under button4
                        glocthrottleObject = glocZObject.Find("throttle");
                        if (glocthrottleObject != null)
                        {
                            logger.Info("glocthrottleObject found.");
                            glocthrottleStartPosition = glocthrottleObject.position;
                            glocthrottleStartRotation = glocthrottleObject.rotation;
                        }
                        else
                        {
                            logger.Error("glocthrottleObject object not found under glocZObject!");
                        }

                        // Find glocControllerX under glocZ
                        glocControllerX = glocZObject.Find("glocControllerX");
                        if (glocControllerX != null)
                        {
                            logger.Info("glocControllerX object found.");
                            // Store initial position and rotation of the stick
                            glocControllerXStartPosition = glocControllerX.transform.position; // these could cause the controller to mess up
                            glocControllerXStartRotation = glocControllerX.transform.rotation;

                            // Find glocControllerY under glocControllerX
                            glocControllerY = glocControllerX.Find("glocControllerY");
                            if (glocControllerY != null)
                            {
                                logger.Info("glocControllerY object found.");
                                // Store initial position and rotation of the stick
                                glocControllerYStartPosition = glocControllerY.transform.position;
                                glocControllerYStartRotation = glocControllerY.transform.rotation;

                                // Find glocControllerZ under glocControllerY
                                glocControllerZ = glocControllerY.Find("glocControllerZ");
                                if (glocControllerZ != null)
                                {
                                    logger.Info("glocControllerZ object found.");
                                    // Store initial position and rotation of the stick
                                    glocControllerZStartPosition = glocControllerZ.transform.position;
                                    glocControllerZStartRotation = glocControllerZ.transform.rotation;
                                }
                                else
                                {
                                    logger.Error("glocControllerZ object not found under glocControllerY!");
                                }
                            }
                            else
                            {
                                logger.Error("glocControllerY object not found under glocControllerX!");
                            }
                        }
                        else
                        {
                            logger.Error("glocControllerX object not found under glocZ!");
                        }

                        // Find cockpit camera under glocZ
                        cockpitCam = glocZObject.Find("eyes/cockpitcam")?.gameObject;
                        if (cockpitCam != null)
                        {
                            logger.Info("Cockpitcam object found.");

                            // Store initial position and rotation of cockpit cam
                            cockpitCamStartPosition = cockpitCam.transform.position;
                            cockpitCamStartRotation = cockpitCam.transform.rotation;
                        }
                        else
                        {
                            logger.Error("Cockpitcam object not found under glocZ!");
                        }
                    }
                    else
                    {
                        logger.Error("glocZ object not found under glocY!");
                    }
                }
                else
                {
                    logger.Error("glocY object not found under glocX!");
                }
            }
            else
            {
                logger.Error("glocX object not found!");
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
                    inputDetected = true;
                }

                // Reset position on button release
                if (Input.GetButtonUp("Fire2"))
                {
                    inputDetected = true;
                }

                // Fire3
                if (Input.GetButtonDown("Fire3"))
                {
                    inputDetected = true;
                }

                // Reset position on button release
                if (Input.GetButtonUp("Fire3"))
                {
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
            logger.Info("Sega GLOC Motion Sim starting...");
            logger.Info("GET READY, AGAIN, AGAIN?!!!...");
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
                logger.Error("CockpitCam object not found under glocZ!");
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



            // Reset glocX to initial positions and rotations
            if (glocXObject != null)
            {
                glocXObject.position = glocXStartPosition;
                glocXObject.rotation = glocXStartRotation;
            }

            // Reset glocY object to initial position and rotation
            if (glocYObject != null)
            {
                glocYObject.position = glocYStartPosition;
                glocYObject.rotation = glocYStartRotation;
            }
            // Reset glocZ object to initial position and rotation
            if (glocZObject != null)
            {
                glocZObject.position = glocZStartPosition;
                glocZObject.rotation = glocZStartRotation;
            }
            // Reset glocthrottle object to initial position and rotation
            if (glocthrottleObject != null)
            {
                glocthrottleObject.position = glocthrottleStartPosition;
                glocthrottleObject.rotation = glocthrottleStartRotation;
            }
            // Reset gloccontrollerX object to initial position and rotation
            if (glocControllerX != null)
            {
                // glocController.position = glocControllerStartPosition;
                glocControllerX.rotation = glocControllerXStartRotation;
            }

            // Reset gloccontrollerY object to initial position and rotation
            if (glocControllerY != null)
            {
                // glocControllerY.position = glocControllerStartPosition; 
                glocControllerY.rotation = glocControllerYStartRotation;
            }

            // Reset gloccontrollerZ object to initial position and rotation
            if (glocControllerZ != null)
            {
                // glocControllerY.position = glocControllerStartPosition;
                glocControllerZ.rotation = glocControllerZStartRotation;
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
                    inputDetected = true;
                }

                // Reset on X release
                if (XInput.GetUp(XInput.Button.X))
                {
                    inputDetected = true;
                }

                // Handle LT press and throttle movement
                if (XInput.GetDown(XInput.Button.LIndexTrigger) && currentthrottlePosition > -glocthrottleLimit) // position limit for throttle
                {
                    glocthrottleObject.Translate(-0.03f, 0, 0); // Using Translate method to move the object
                    inputDetected = true;
                }

                // Handle LT press and throttle movement
                if (XInput.GetUp(XInput.Button.LIndexTrigger) && currentthrottlePosition > -glocthrottleLimit) // position limit for throttle
                {
                    glocthrottleObject.Translate(0.03f, 0, 0);
                    inputDetected = true;
                }
                // Handle RT press and throttle movement
                if (XInput.GetDown(XInput.Button.RIndexTrigger) && currentthrottlePosition < glocthrottleLimit) // position limit for throttle
                {
                    glocthrottleObject.Translate(0.03f, 0, 0); // Using Translate method to move the object
                    inputDetected = true;
                }
                // Handle RT press and throttle movement
                if (XInput.GetUp(XInput.Button.RIndexTrigger) && currentthrottlePosition < glocthrottleLimit) // position limit for throttle
                {
                    glocthrottleObject.Translate(-0.03f, 0, 0);
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
            if (XInput.GetDown(XInput.Button.LIndexTrigger) && currentthrottlePosition > -glocthrottleLimit) // position limit for throttle
            {
                float move = throttleVelocity * Time.deltaTime;
                glocthrottleObject.Translate(0, 0, -move); // Using Translate method to move the object
                currentthrottlePosition -= move; // Update the throttle position value
                inputDetected = true;
            }

            // Handle RT press and throttle movement
            if (XInput.GetDown(XInput.Button.RIndexTrigger) && currentthrottlePosition < glocthrottleLimit) // position limit for throttle
            {
                float move = throttleVelocity * Time.deltaTime;
                glocthrottleObject.Translate(0, 0, move); // Using Translate method to move the object
                currentthrottlePosition += move; // Update the throttle position value
                inputDetected = true;
            }

            // Handle X rotation for glocYObject and glocControllerX (Right Arrow or primaryThumbstick.x > 0)
            // Thumbstick direction: right
            if ((Input.GetKey(KeyCode.RightArrow) || primaryThumbstick.x > 0))
            {
                if (currentRotationX < rotationLimitX)
                {
                    float rotateX = (Input.GetKey(KeyCode.RightArrow) ? keyboardVelocityX : primaryThumbstick.x * vrVelocity) * Time.deltaTime;
                    glocYObject.Rotate(-rotateX, 0, 0);
                    currentRotationX += rotateX;
                    inputDetected = true;
                }
                if (currentControllerRotationX < controllerrotationLimitX)
                {
                    float controllerRotateX = (Input.GetKey(KeyCode.RightArrow) ? keyboardControllerVelocityX : primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                    glocControllerX.Rotate(-controllerRotateX, 0, 0);
                    currentControllerRotationX += controllerRotateX;
                    inputDetected = true;
                }
            }

            // Handle X rotation for glocYObject and glocControllerX (Left Arrow or primaryThumbstick.x < 0)
            // Thumbstick direction: left
            if ((Input.GetKey(KeyCode.LeftArrow) || primaryThumbstick.x < 0))
            {
                if (currentRotationX > -rotationLimitX)
                {
                    float rotateX = (Input.GetKey(KeyCode.LeftArrow) ? keyboardVelocityX : -primaryThumbstick.x * vrVelocity) * Time.deltaTime;
                    glocYObject.Rotate(rotateX, 0, 0);
                    currentRotationX -= rotateX;
                    inputDetected = true;
                }
                if (currentControllerRotationX > -controllerrotationLimitX)
                {
                    float controllerRotateX = (Input.GetKey(KeyCode.LeftArrow) ? keyboardControllerVelocityX : -primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                    glocControllerX.Rotate(controllerRotateX, 0, 0);
                    currentControllerRotationX -= controllerRotateX;
                    inputDetected = true;
                }
            }

            // Handle Z rotation for glocXObject and glocControllerZ (Down Arrow or primaryThumbstick.y < 0)
            // Thumbstick direction: down
            if ((Input.GetKey(KeyCode.DownArrow) || primaryThumbstick.y < 0))
            {
                if (currentRotationZ > -rotationLimitZ)
                {
                    float rotateZ = (Input.GetKey(KeyCode.DownArrow) ? keyboardVelocityZ : -primaryThumbstick.y * vrVelocity) * Time.deltaTime;
                    glocXObject.Rotate(0, 0, rotateZ);
                    currentRotationZ -= rotateZ;
                    inputDetected = true;
                }
                if (currentControllerRotationZ > -controllerrotationLimitZ)
                {
                    float controllerRotateZ = (Input.GetKey(KeyCode.DownArrow) ? keyboardControllerVelocityZ : -primaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                    glocControllerZ.Rotate(0, 0, controllerRotateZ);
                    currentControllerRotationZ -= controllerRotateZ;
                    inputDetected = true;
                }
            }

            // Handle Z rotation for glocXObject and glocControllerZ (Up Arrow or primaryThumbstick.y > 0)
            // Thumbstick direction: up
            if ((Input.GetKey(KeyCode.UpArrow) || primaryThumbstick.y > 0))
            {
                if (currentRotationZ < rotationLimitZ)
                {
                    float rotateZ = (Input.GetKey(KeyCode.UpArrow) ? keyboardVelocityZ : primaryThumbstick.y * vrVelocity) * Time.deltaTime;
                    glocXObject.Rotate(0, 0, -rotateZ);
                    currentRotationZ += rotateZ;
                    inputDetected = true;
                }
                if (currentControllerRotationZ < controllerrotationLimitZ)
                {
                    float controllerRotateZ = (Input.GetKey(KeyCode.UpArrow) ? keyboardControllerVelocityZ : primaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                    glocControllerZ.Rotate(0, 0, -controllerRotateZ);
                    currentControllerRotationZ += controllerRotateZ;
                    inputDetected = true;
                }
            }

            // Handle Y rotation for glocYObject and glocControllerY (Unused axis or secondaryThumbstick.x)
            // Thumbstick direction: left/right
            if ((Input.GetKey(KeyCode.None) || secondaryThumbstick.x != 0))
            {
                if (secondaryThumbstick.x > 0 && currentRotationY < rotationLimitY)
                {
                    float rotateY = secondaryThumbstick.x * vrVelocity * Time.deltaTime;
                    glocYObject.Rotate(0, rotateY, 0);
                    currentRotationY += rotateY;
                    inputDetected = true;
                }
                if (secondaryThumbstick.x > 0 && currentControllerRotationY < controllerrotationLimitY)
                {
                    float controllerRotateY = secondaryThumbstick.x * vrControllerVelocity * Time.deltaTime;
                    glocControllerY.Rotate(0, controllerRotateY, 0);
                    currentControllerRotationY += controllerRotateY;
                    inputDetected = true;
                }
                if (secondaryThumbstick.x < 0 && currentRotationY > -rotationLimitY)
                {
                    float rotateY = secondaryThumbstick.x * vrVelocity * Time.deltaTime;
                    glocYObject.Rotate(0, rotateY, 0);
                    currentRotationY -= rotateY;
                    inputDetected = true;
                }
                if (secondaryThumbstick.x < 0 && currentControllerRotationY > -controllerrotationLimitY)
                {
                    float controllerRotateY = secondaryThumbstick.x * vrControllerVelocity * Time.deltaTime;
                    glocControllerY.Rotate(0, controllerRotateY, 0);
                    currentControllerRotationY -= controllerRotateY;
                    inputDetected = true;
                }
            }
            /*
            // Handle unused axis rotation for glocYObject and glocControllerY
            // This can be mapped to secondaryThumbstick.y for additional rotation control
            // Thumbstick direction: up/down
            if (secondaryThumbstick.y != 0)
            {
                if (secondaryThumbstick.y > 0 && currentRotationY < rotationLimitY)
                {
                    float rotateY = secondaryThumbstick.y * vrVelocity * Time.deltaTime;
                    glocYObject.Rotate(0, rotateY, 0);
                    currentRotationY += rotateY;
                    inputDetected = true;
                }
                if (secondaryThumbstick.y > 0 && currentControllerRotationY < controllerrotationLimitY)
                {
                    float controllerRotateY = secondaryThumbstick.y * vrControllerVelocity * Time.deltaTime;
                    glocControllerY.Rotate(0, controllerRotateY, 0);
                    currentControllerRotationY += controllerRotateY;
                    inputDetected = true;
                }
                if (secondaryThumbstick.y < 0 && currentRotationY > -rotationLimitY)
                {
                    float rotateY = secondaryThumbstick.y * vrVelocity * Time.deltaTime;
                    glocYObject.Rotate(0, rotateY, 0);
                    currentRotationY -= rotateY;
                    inputDetected = true;
                }
                if (secondaryThumbstick.y < 0 && currentControllerRotationY > -controllerrotationLimitY)
                {
                    float controllerRotateY = secondaryThumbstick.y * vrControllerVelocity * Time.deltaTime;
                    glocControllerY.Rotate(0, controllerRotateY, 0);
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
                glocYObject.Rotate(unrotateX, 0, 0);
                currentRotationX -= unrotateX;
            }
            else if (currentRotationX < 0)
            {
                float unrotateX = Mathf.Min(centeringVelocityX * Time.deltaTime, -currentRotationX);
                glocYObject.Rotate(-unrotateX, 0, 0);
                currentRotationX += unrotateX;
            }

            // Center Y-axis
            if (currentRotationY > 0)
            {
                float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, currentRotationY);
                glocXObject.Rotate(0, unrotateY, 0);
                currentRotationY -= unrotateY;
            }
            else if (currentRotationY < 0)
            {
                float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, -currentRotationY);
                glocXObject.Rotate(0, -unrotateY, 0);
                currentRotationY += unrotateY;
            }

            // Center Z-axis
            if (currentRotationZ > 0)
            {
                float unrotateZ = Mathf.Min(centeringVelocityZ * Time.deltaTime, currentRotationZ);
                glocXObject.Rotate(0, 0, unrotateZ);
                currentRotationZ -= unrotateZ;
            }
            else if (currentRotationZ < 0)
            {
                float unrotateZ = Mathf.Min(centeringVelocityZ * Time.deltaTime, -currentRotationZ);
                glocXObject.Rotate(0, 0, -unrotateZ);
                currentRotationZ += unrotateZ;
            }
            //Centering for contoller

            // Center Y-axis Controller rotation
            if (currentControllerRotationY > 0)
            {
                float unrotateY = Mathf.Min(centeringControllerVelocityY * Time.deltaTime, currentControllerRotationY);
                glocControllerY.Rotate(0, unrotateY, 0);   // Rotating to reduce the rotation
                currentControllerRotationY -= unrotateY;    // Reducing the positive rotation
            }
            else if (currentControllerRotationY < 0)
            {
                float unrotateY = Mathf.Min(centeringControllerVelocityY * Time.deltaTime, -currentControllerRotationY);
                glocControllerY.Rotate(0, -unrotateY, 0);  // Rotating to reduce the rotation
                currentControllerRotationY += unrotateY;    // Reducing the negative rotation
            }

            // Center X-Axis Controller rotation
            if (currentControllerRotationX > 0)
            {
                float unrotateX = Mathf.Min(centeringControllerVelocityX * Time.deltaTime, currentControllerRotationX);
                glocControllerX.Rotate(unrotateX, 0, 0);   // Rotating to reduce the rotation
                currentControllerRotationX -= unrotateX;    // Reducing the positive rotation
            }
            else if (currentControllerRotationX < 0)
            {
                float unrotateX = Mathf.Min(centeringControllerVelocityX * Time.deltaTime, -currentControllerRotationX);
                glocControllerX.Rotate(-unrotateX, 0, 0);   // Rotating to reduce the rotation
                currentControllerRotationX += unrotateX;    // Reducing the positive rotation
            }

            // Center Z-axis Controller rotation
            if (currentControllerRotationZ > 0)
            {
                float unrotateZ = Mathf.Min(centeringControllerVelocityZ * Time.deltaTime, currentControllerRotationZ);
                glocControllerZ.Rotate(0, 0, unrotateZ);   // Rotating to reduce the rotation
                currentControllerRotationZ -= unrotateZ;    // Reducing the positive rotation
            }
            else if (currentControllerRotationZ < 0)
            {
                float unrotateZ = Mathf.Min(centeringControllerVelocityZ * Time.deltaTime, -currentControllerRotationZ);
                glocControllerZ.Rotate(0, 0, -unrotateZ);   // Rotating to reduce the rotation
                currentControllerRotationZ += unrotateZ;    // Reducing the positive rotation
            }

            if (currentthrottlePosition > 0)
            {
                float unmove = Mathf.Min(throttlecenteringVelocity * Time.deltaTime, currentthrottlePosition);
                glocthrottleObject.Translate(-unmove, 0, 0); // Move towards center
                currentthrottlePosition -= unmove;
            }
            else if (currentthrottlePosition < 0)
            {
                float unmove = Mathf.Min(throttlecenteringVelocity * Time.deltaTime, -currentthrottlePosition);
                glocthrottleObject.Translate(unmove, 0, 0); // Move towards center
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
