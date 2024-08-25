using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using EmuVR.InputManager;
using System.Collections;

namespace WIGUx.Modules.r360MotionSim
{
    public class r360MotionSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        private readonly float keyboardVelocityX = 100.5f;  // Velocity for keyboard input
        private readonly float keyboardVelocityY = 100.5f;  // Velocity for keyboard input
        private readonly float keyboardVelocityZ = 100.5f;  // Velocity for keyboard input
        private readonly float vrVelocity = 75.5f;        // Velocity for VR controller input

        private readonly float centeringVelocityX = 75.5f;  // Velocity for centering rotation
        private readonly float centeringVelocityY = 75.5f;  // Velocity for centering rotation
        private readonly float centeringVelocityZ = 75.5f;  // Velocity for centering rotation

        // Controller animation 
        // Speeds for the animation of the in game flight stick or wheel
        private readonly float keyboardControllerVelocityX = 300.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityY = 300.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityZ = 300.5f;  // Velocity for keyboard input
        private readonly float vrControllerVelocity = 250.5f;        // Velocity for VR controller input

        private float controllerrotationLimitX = 20f;  // Rotation limit for X-axis (stick or wheel)
        private float controllerrotationLimitY = 20f;  // Rotation limit for Y-axis (stick or wheel)
        private float controllerrotationLimitZ = 20f;  // Rotation limit for Z-axis (stick or wheel)

        private float currentControllerRotationX = 0f;  // Current rotation for X-axis (stick or wheel)
        private float currentControllerRotationY = 0f;  // Current rotation for Y-axis (stick or wheel)
        private float currentControllerRotationZ = 0f;  // Current rotation for Z-axis (stick or wheel)

        private readonly float centeringControllerVelocityX = 400.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float centeringControllerVelocityY = 400.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float centeringControllerVelocityZ = 400.5f;  // Velocity for centering rotation (stick or wheel)

        private Transform r360ControllerX; // Reference to the main animated controller (wheel)
        private Vector3 r360ControllerXStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion r360ControllerXStartRotation; // Initial controlller positions and rotations for resetting
        private Transform r360ControllerY; // Reference to the main animated controller (wheel)
        private Vector3 r360ControllerYStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion r360ControllerYStartRotation; // Initial controlller positions and rotations for resetting
        private Transform r360ControllerZ; // Reference to the main animated controller (wheel)
        private Vector3 r360ControllerZStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion r360ControllerZStartRotation; // Initial controlller positions and rotations for resetting

        private float adjustSpeed = 1.0f;  // Adjust this adjustment speed as needed a lower number will lead to smaller adustments

        private float rotationLimitX = 1080f;  // Rotation limit for X-axis
        private float rotationLimitY = 1080f;  // Rotation limit for Y-axis
        private float rotationLimitZ = 1080f;  // Rotation limit for Z-axis

        private float currentRotationX = 0f;  // Current rotation for X-axis
        private float currentRotationY = 0f;  // Current rotation for Y-axis
        private float currentRotationZ = 0f;  // Current rotation for Z-axis

        private Transform r360XObject; // Reference to the main X object
        private Transform r360YObject; // Reference to the main Y object
        private Transform r360ZObject; // Reference to the main Z object
        private GameObject cockpitCam;    // Reference to the cockpit camera

        // Initial positions and rotations for resetting
        private Vector3 r360XStartPosition;
        private Quaternion r360XStartRotation;
        private Vector3 r360YStartPosition;
        private Quaternion r360YStartRotation;
        private Vector3 r360ZStartPosition;
        private Quaternion r360ZStartRotation;
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

        //lights
        private Transform thrustObject;
        public Renderer[] frontEmissiveObjects;
        public Renderer[] leftEmissiveObjects;
        public Renderer[] rightEmissiveObjects;
        private Coroutine frontCoroutine;
        private Coroutine leftCoroutine;
        private Coroutine rightCoroutine;
        private float frontFlashDuration = 0.4f;
        private float frontFlashDelay = 0.17f;
        private float sideFlashDuration = 0.02f;
        private float sideFlashDelay = 0.005f;
        private float frontDangerDuration = 0.2f;
        private float frontDangerDelay = 0.2f;
        private float sideDangerDuration = 0.1f;
        private float sideDangerDelay = 0.2f;
        private Transform fireemissiveObject;
        public Light fire1_light;
        public Light fire2_light;
        public float lightDuration = 0.35f; // Duration during which the lights will be on

        private Light[] lights;
        private readonly string[] compatibleGames = { "glocr360", "wingwar360" };
        private bool inFocusMode = false;  // Flag to track focus mode state

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

            // Find r3602X object in hierarchy
            r360XObject = transform.Find("r360X");
            if (r360XObject != null)
            {
                logger.Info("r360X object found.");
                r360XStartPosition = r360XObject.position;
                r360XStartRotation = r360XObject.rotation;

                // Find r360Y object under r360X
                r360YObject = r360XObject.Find("r360Y");
                if (r360YObject != null)
                {
                    logger.Info("r360Y object found.");
                    r360YStartPosition = r360YObject.position;
                    r360YStartRotation = r360YObject.rotation;

                    // Find r360Z object under r360X
                    r360ZObject = r360YObject.Find("r360Z");
                    if (r360ZObject != null)
                    {
                        logger.Info("r360Z object found.");
                        r360ZStartPosition = r360ZObject.position;
                        r360ZStartRotation = r360ZObject.rotation;

                        // Find fireemissive object under sharrierX
                        fireemissiveObject = r360ZObject.Find("fireemissive");
                        if (fireemissiveObject != null)
                        {
                            logger.Info("fireemissive object found.");
                            // Ensure the fireemissive object is initially off
                            Renderer renderer = fireemissiveObject.GetComponent<Renderer>();
                            if (renderer != null)
                            {
                                renderer.material.DisableKeyword("_EMISSION");
                            }
                            else
                            {
                                logger.Debug("Renderer component is not found on fireemissive object.");
                            }
                        }
                        else
                        {
                            logger.Debug("fireemissive object not found under r360Z.");
                        }

                        // Find r360ControllerX under r360Z
                        r360ControllerX = r360ZObject.Find("r360ControllerX");
                        if (r360ControllerX != null)
                        {
                             // Store initial position and rotation of the stick
                            r360ControllerXStartPosition = r360ControllerX.transform.position; // these could cause the controller to mess up
                            r360ControllerXStartRotation = r360ControllerX.transform.rotation;

                            // Find r360ControllerY under r360ControllerX
                            r360ControllerY = r360ControllerX.Find("r360ControllerY");
                        if (r360ControllerY != null)
                        {
                            logger.Info("r360ControllerY object found.");
                            // Store initial position and rotation of the stick
                            r360ControllerYStartPosition = r360ControllerY.transform.position;// these could cause the controller to mess up
                            r360ControllerYStartRotation = r360ControllerY.transform.rotation;

                                // Find r360ControllerZ under r360ControllerY
                                r360ControllerZ = r360ControllerY.Find("r360ControllerZ");
                            if (r360ControllerZ != null)
                            {
                                logger.Info("r360ControllerZ object found.");
                                    // Store initial position and rotation of the stick
                                    r360ControllerZStartPosition = r360ControllerZ.transform.position;// these could cause the controller to mess up
                                    r360ControllerZStartRotation = r360ControllerZ.transform.rotation;
                            }
                            else
                            {
                                logger.Error("r360ControllerZ object not found under r360ControllerY!");
                            }
                        }
                        else
                        {
                            logger.Error("r360ControllerY object not found under aburnerControllerX!");
                        }
                    }
                    else
                    {
                        logger.Error("r360ControllerX object not found under r360Z!");
                    }


                    // Find cockpit camera under cockpit
                    cockpitCam = r360ZObject.Find("eyes/cockpitcam")?.gameObject;
                        if (cockpitCam != null)
                        {
                            logger.Info("Cockpitcam object found.");

                            // Store initial position and rotation of cockpit cam
                            cockpitCamStartPosition = cockpitCam.transform.position;
                            cockpitCamStartRotation = cockpitCam.transform.rotation;
                        }
                        else
                        {
                            logger.Error("Cockpitcam object not found under r360Z!");
                        }
                    }
                    else
                    {
                        logger.Error("r360Z object not found under r360Y!");
                    }
                }

                else
                {
                    logger.Error("r360Y object not found under r360X!");
                }
            }
            else
            {
                logger.Error("r360X object not found!");
            }


            // Gets all Light components in the target object and its children
            Light[] allLights = r360ZObject.GetComponentsInChildren<Light>();
            // Initialize a new list to store filtered lights
            List<Light> filteredLights = new List<Light>();
            // Initialize left and right arrays from r360ZObject
            // Log the names of the objects containing the Light components and filter out unwanted lights
            foreach (Light light in allLights)
            {
                if (light.gameObject.name != "screen_light(Clone)" && light.gameObject.name != "ambient_light")
                {
                    filteredLights.Add(light);
                    logger.Info("Included Light found in object: " + light.gameObject.name);
                }
                else
                {
                    logger.Info("Excluded Light found in object: " + light.gameObject.name);
                }
            }
            InitializeEmissiveArrays();
            // Store the filtered lights
            lights = filteredLights.ToArray();
            StartAttractPattern();
        }

        void Update()
        {
            bool inputDetected = false; // Initialize at the beginning of the Update method
            if (Input.GetKey(KeyCode.O))
            {
                logger.Info("Resetting Positions");
                ResetPositions();
            }
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
                HandleVRInput(ref inputDetected);  // Pass by reference
                HandleKeyboardInput(ref inputDetected);  // Pass by reference
                                                         // Reset position on button release
                if (Input.GetButtonUp("Fire1"))
                {
                    inputDetected = true;
                }

                // Fire2
                if (Input.GetButtonDown("Fire2"))
                {
                    ToggleFireEmissive(true);
                    ToggleLights(true);
                    inputDetected = true;
                }

                // Reset position on button release
                if (Input.GetButtonUp("Fire2"))
                {
                    ToggleFireEmissive(false);
                    ToggleLights(false);
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
            logger.Info("Compatible Rom Dectected, Lower Safty Bar...");
            logger.Info("Sega R360 Motion Sim starting...");
            logger.Info("Vomit bags are below the seat...");
            StartDangerPattern();
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
                logger.Error("CockpitCam object not found under r360Z!");
            }
            playerCamera.transform.localScale = playerCameraStartScale;
    //        cockpitCam.transform.position = cockpitCamStartPosition; // new hotness
    //        cockpitCam.transform.rotation = cockpitCamStartRotation; // new hotness
            inFocusMode = true;  // Set focus mode flag
        }

        void EndFocusMode()
        {
            logger.Info("Exiting Focus Mode...");
            // Restore original parents of objects
            RestoreOriginalParent(playerCamera, "PlayerCamera");
            RestoreOriginalParent(playerVRSetup, "PlayerVRSetup.PlayerRig");

            // Reset r360X to initial positions and rotations
            if (r360XObject != null)
            {
                r360XObject.position = r360XStartPosition;
                r360XObject.rotation = r360XStartRotation;
            }

            // Reset r360Y object to initial position and rotation
            if (r360YObject != null)
            {
                r360YObject.position = r360YStartPosition;
                r360YObject.rotation = r360YStartRotation;
            }
            // Reset r360Z object to initial position and rotation
            if (r360ZObject != null)
            {
                r360ZObject.position = r360ZStartPosition;
                r360ZObject.rotation = r360ZStartRotation;
            }
            if (r360ControllerX != null)
            {
                r360ControllerX.position = r360ControllerXStartPosition;
                r360ControllerX.rotation = r360ControllerXStartRotation;
            }
            // Reset r360Y to initial positions and rotations
            if (r360ControllerY != null)
            {
                r360ControllerY.position = r360ControllerYStartPosition;
                r360ControllerY.rotation = r360ControllerYStartRotation;
            }
            if (r360ControllerZ != null)
            {
                r360ControllerZ.position = r360ControllerZStartPosition;
                r360ControllerZ.rotation = r360ControllerZStartRotation;
            }

            // Reset rotation allowances and current rotation values
            logger.Info("Resetting Positions");
            ResetPositions();
            StartAttractPattern();
            inFocusMode = false;  // Clear focus mode flag
        }

        void HandleKeyboardInput(ref bool inputDetected)
        {
            if (!inFocusMode) return;
            /*
            // Handle keyboard input for pitch and roll
            if (Input.GetKey(KeyCode.DownArrow) && currentRotationX > -rotationLimitX)
            {
                float rotateX = keyboardVelocityX * Time.deltaTime;
                r360XObject.Rotate(rotateX, 0, 0);
                currentRotationX -= rotateX;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.UpArrow) && currentRotationX < rotationLimitX)
            {
                float rotateX = keyboardVelocityX * Time.deltaTime;
                r360XObject.Rotate(-rotateX, 0, 0);
                currentRotationX += rotateX;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.LeftArrow) && currentRotationY > -rotationLimitY)
            {
                float rotateY = keyboardVelocityY * Time.deltaTime;
                r360YObject.Rotate(0, rotateY, 0);
                currentRotationY -= rotateY;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.RightArrow) && currentRotationY < rotationLimitY)
            {
                float rotateY = keyboardVelocityY * Time.deltaTime;
                r360YObject.Rotate(0, -rotateY, 0);
                currentRotationY += rotateY;
                inputDetected = true;
            }
            /*
            if (Input.GetKey(KeyCode.DownArrow) && currentRotationZ > -rotationLimitZ)
            {
                float rotateZ = keyboardVelocityZ * Time.deltaTime;
                r360ZObject.Rotate(0, 0, rotateZ);
                currentRotationZ -= rotateZ;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.UpArrow) && currentRotationZ < rotationLimitZ)
            {
                float rotateZ = keyboardVelocityZ * Time.deltaTime;
                r360ZObject.Rotate(0, 0, -rotateZ);
                currentRotationZ += rotateZ;
                inputDetected = true;
            }
            */
            /*
            // Stick Rotations 
            // Stick Y Rotation
            if (Input.GetKey(KeyCode.RightArrow) && currentControllerRotationY > -controllerrotationLimitY)
            {
                float rotateY = keyboardControllerVelocityY * Time.deltaTime;
                r360ControllerY.Rotate(0, rotateY, 0);
                currentControllerRotationY -= rotateY;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.LeftArrow) && currentControllerRotationY < controllerrotationLimitY)
            {
                float rotateY = keyboardControllerVelocityY * Time.deltaTime;
                r360ControllerY.Rotate(0, -rotateY, 0);
                currentControllerRotationY += rotateY;
                inputDetected = true;
            }


            // Stick X Rotation
            if (Input.GetKey(KeyCode.UpArrow) && currentControllerRotationX > -controllerrotationLimitX)
            {
                float rotateX = keyboardControllerVelocityX * Time.deltaTime;
                r360ControllerZ.Rotate(rotateX, 0, 0);
                currentControllerRotationX -= rotateX;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.DownArrow) && currentControllerRotationX < controllerrotationLimitX)
            {
                float rotateX = keyboardControllerVelocityX * Time.deltaTime;
                r360ControllerZ.Rotate(-rotateX, 0, 0);
                currentControllerRotationX += rotateX;
                inputDetected = true;
            }
            /*
            // Stick Z Rotation
            if (Input.GetKey(KeyCode.LeftArrow) && currentControllerRotationZ > -controllerrotationLimitZ)
            {
                float rotateZ = keyboardControllerVelocityZ * Time.deltaTime;
                r360ControllerZ.Rotate(0, 0, rotateZ);
                currentControllerRotationZ -= rotateZ;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.RightArrow) && currentControllerRotationZ < controllerrotationLimitZ)
            {
                float rotateZ = keyboardControllerVelocityZ * Time.deltaTime;
                r360ControllerZ.Rotate(0, 0, -rotateZ);
                currentControllerRotationZ += rotateZ;
                inputDetected = true;
            }
            */

            // Center the rotation if no input is detected
            if (!inputDetected)
            {
                CenterRotation();
            }
        }

        void HandleVRInput(ref bool inputDetected)
        {
            if (!inFocusMode) return;

            // Capture analog stick inputs
            Vector2 primaryThumbstick = Vector2.zero;
            Vector2 secondaryThumbstick = Vector2.zero;

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

            if (XInput.IsConnected)
            {
                primaryThumbstick = XInput.Get(XInput.Axis.LThumbstick);
                Vector2 xboxPrimaryThumbstick = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                Vector2 xboxSecondaryThumbstick = new Vector2(Input.GetAxis("RightStickHorizontal"), Input.GetAxis("RightStickVertical"));
                // Ximput controller input
                // Combine VR and Xbox inputs
                primaryThumbstick += xboxPrimaryThumbstick;
                secondaryThumbstick += xboxSecondaryThumbstick;
                primaryThumbstick = XInput.Get(XInput.Axis.LThumbstick);

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
                    inputDetected = true;
                }

                // LeftTrigger
                if (XInput.GetDown(XInput.Button.LIndexTrigger))
                {
                    inputDetected = true;
                }

                // Reset position on button release
                if (XInput.GetUp(XInput.Button.LIndexTrigger))
                {
                    inputDetected = true;
                }

                // Handle RB button press for plunger position
                if (XInput.GetDown(XInput.Button.RShoulder) || Input.GetKeyDown(KeyCode.JoystickButton5))
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

            // Handle forward rotation (Thumbstick right)
            if (primaryThumbstick.x > 0 && currentRotationY > -rotationLimitY)
            {
                float rotateY = primaryThumbstick.x * vrVelocity * Time.deltaTime;
                r360YObject.Rotate(0, -rotateY, 0);  // Rotate forward
                currentRotationY += rotateY;  // Update current rotation (more negative)
                inputDetected = true;
            }

            // Handle backward rotation (Thumbstick left)
            if (primaryThumbstick.x < 0 && currentRotationY < rotationLimitY)
            {
                float rotateY = primaryThumbstick.x * vrVelocity * Time.deltaTime;
                r360YObject.Rotate(0, -rotateY, 0);  // Rotate backward
                currentRotationY += rotateY;  // Update current rotation (more positive)
                inputDetected = true;
            }

            // Handle left rotation (Thumbstick down)
            if (primaryThumbstick.y < 0 && currentRotationX > -rotationLimitX)
            {
                float rotateX = primaryThumbstick.y * vrVelocity * Time.deltaTime;
                r360XObject.Rotate(-rotateX, 0, 0);  // Rotate left
                currentRotationX += rotateX;  // Update current rotation (more negative)
                inputDetected = true;
            }

            // Handle right rotation (Thumbstick up)
            if (primaryThumbstick.y > 0 && currentRotationX < rotationLimitX)
            {
                float rotateX = primaryThumbstick.y * vrVelocity * Time.deltaTime;
                r360XObject.Rotate(-rotateX, 0, 0);  // Rotate right
                currentRotationX += rotateX;  // Update current rotation (more positive)
                inputDetected = true;
            }

            /*
            // Handle forward rotation (Thumbstick up)
            if (secondaryThumbstick.y > 0 && currentRotationY < rotationLimitY)
            {
                float rotateY = secondaryThumbstick.y * vrVelocity * Time.deltaTime;
                r360YObject.Rotate(0, -rotateY, 0);  // Rotate Y
                currentRotationY += rotateY;  // Update current rotation (more negative)
                inputDetected = true;
            }

            // Handle backward rotation (Thumbstick down)
            if (secondaryThumbstick.y < 0 && currentRotationY > -rotationLimitY)
            {
                float rotateY = secondaryThumbstick.y * vrVelocity * Time.deltaTime;
                r360YObject.Rotate(0, -rotateY, 0);  // Rotate Y
                currentRotationY += rotateY;  // Update current rotation (more positive)
                inputDetected = true;
            }
            // Handle left rotation (Thumbstick left)
            if (secondaryThumbstick.x < 0 && currentRotationY > -rotationLimitY)
            {
                float rotateY = secondaryThumbstick.x * vrVelocity * Time.deltaTime;
                r360YObject.Rotate(0, rotateY, 0);  // Rotate Y
                currentRotationY += rotateY;  // Update current rotation (more positive)
                inputDetected = true;
            }

            // Handle right rotation (Thumbstick right)
            if (secondaryThumbstick.x > 0 && currentRotationY < rotationLimitY)
            {
                float rotateY = secondaryThumbstick.x * vrVelocity * Time.deltaTime;
                r360YObject.Rotate(0, rotateY, 0);  // Rotate Y
                currentRotationY += rotateY;  // Update current rotation (more negative)
                inputDetected = true;
            }
            */

            //HANDLE INGAME CONTROLLER ANIMATION

            // Handle Y rotation (Thumbstick right)
            if (primaryThumbstick.x > 0 && currentControllerRotationY < controllerrotationLimitY)
            {
                float rotateY = primaryThumbstick.x * vrControllerVelocity * Time.deltaTime;
                r360ControllerY.Rotate(0, -rotateY, 0);  // Rotate Y to the left
                currentControllerRotationY += rotateY;  // Update current rotation (more positive)
                inputDetected = true;
            }
            // Handle Y rotation (Thumbstick left)
            if (primaryThumbstick.x < 0 && currentControllerRotationY > -controllerrotationLimitY)
            {
                float rotateY = -primaryThumbstick.x * vrControllerVelocity * Time.deltaTime;
                r360ControllerY.Rotate(0, rotateY, 0);  // Rotate Y to the right
                currentControllerRotationY -= rotateY;  // Update current rotation (more negative)
                inputDetected = true;
            }

            // Handle controller rotation (Thumbstick down)

            if (primaryThumbstick.y > 0 && currentControllerRotationX < controllerrotationLimitX)
            {
                float rotateX = primaryThumbstick.y * vrControllerVelocity * Time.deltaTime;
                r360ControllerZ.Rotate(-rotateX, 0, 0);  // Rotate left (negative direction)
                currentControllerRotationX += rotateX;  // Update current rotation (more negative)
                inputDetected = true;
            }
            // Handle controller rotation (Thumbstick up)
            if (primaryThumbstick.y < 0 && currentControllerRotationX > -controllerrotationLimitX)
            {
                float rotateX = -primaryThumbstick.y * vrControllerVelocity * Time.deltaTime;
                r360ControllerZ.Rotate(rotateX, 0, 0);  // Rotate right (positive direction)
                currentControllerRotationX -= rotateX;  // Update current rotation (more positive)
                inputDetected = true;
            }

            /*
            // Handle controller rotation (Thumbstick up)
            if (primaryThumbstick.y < 0 && currentControllerRotationZ > -controllerrotationLimitZ)
            {
                float rotateZ = -primaryThumbstick.x * vrControllerVelocity * Time.deltaTime;
                r360ControllerZ.Rotate(0, 0, rotateZ);  // Rotate forward
                currentControllerRotationZ -= rotateZ;  // Update current rotation (more negative)
                inputDetected = true;
            }
            // Handle controller rotation (Thumbstick down)
            if (primaryThumbstick.y > 0 && currentControllerRotationZ < controllerrotationLimitZ)
            {
                float rotateZ = -primaryThumbstick.x * vrControllerVelocity * Time.deltaTime;
                r360ControllerZ.Rotate(0, 0, rotateZ);  // Rotate backward
                currentControllerRotationZ -= rotateZ;  // Update current rotation (more positive)
                inputDetected = true;
            }

            // Check if secondary thumbstick input is needed for additional controls
            if (secondaryThumbstick.x != 0f || secondaryThumbstick.y != 0f)
            {
                // Implement additional logic here if needed

                // Print additional control values for debugging
                //    Debug.Log($"Secondary Thumbstick input detected - X: {secondaryThumbstick.x}, Y: {secondaryThumbstick.y}");
            }
            */
        }

        void ResetPositions()
        {
            playerVRSetup.transform.position = playerVRSetupStartPosition;
            playerVRSetup.transform.rotation = playerVRSetupStartRotation;
            playerVRSetup.transform.localScale = playerVRSetupStartScale;
            //playerVRSetup.transform.localScale = new Vector3(1f, 1f, 1f);
            playerCamera.transform.position = playerCameraStartPosition;
            playerCamera.transform.rotation = playerCameraStartRotation;
            playerCamera.transform.localScale = playerCameraStartScale;
            //playerCamera.transform.localScale = new Vector3(1f, 1f, 1f);
            // cockpitCam.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
            currentRotationX = 0f;
            currentRotationY = 0f;
            currentRotationZ = 0f;
            currentControllerRotationX = 0f;
            currentControllerRotationY = 0f;
            currentControllerRotationZ = 0f;
        }

        void CenterRotation()
        {
            // Center X-axis
            if (currentRotationX > 0)
            {
                float unrotateX = Mathf.Min(centeringVelocityX * Time.deltaTime, currentRotationX);
                r360XObject.Rotate(unrotateX, 0, 0);
                currentRotationX -= unrotateX;
            }
            else if (currentRotationX < 0)
            {
                float unrotateX = Mathf.Min(centeringVelocityX * Time.deltaTime, -currentRotationX);
                r360XObject.Rotate(-unrotateX, 0, 0);
                currentRotationX += unrotateX;
            }

            // Center Y-axis
            if (currentRotationY > 0)
            {
                float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, currentRotationY);
                r360YObject.Rotate(0, unrotateY, 0);
                currentRotationY -= unrotateY;
            }
            else if (currentRotationY < 0)
            {
                float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, -currentRotationY);
                r360YObject.Rotate(0, -unrotateY, 0);
                currentRotationY += unrotateY;
            }

            // Center Z-axis
            if (currentRotationZ > 0)
            {
                float unrotateZ = Mathf.Min(centeringVelocityZ * Time.deltaTime, currentRotationZ);
                r360ZObject.Rotate(0, 0, unrotateZ);
                currentRotationZ -= unrotateZ;
            }
            else if (currentRotationZ < 0)
            {
                float unrotateZ = Mathf.Min(centeringVelocityZ * Time.deltaTime, -currentRotationZ);
                r360ZObject.Rotate(0, 0, -unrotateZ);
                currentRotationZ += unrotateZ;
            }
            //Centering for contoller
            // Center Y-axis Controller rotation
            if (currentControllerRotationY > 0)
            {
                float unrotateY = Mathf.Min(centeringControllerVelocityY * Time.deltaTime, currentControllerRotationY);
                r360ControllerY.Rotate(0, unrotateY, 0);   // Rotating to reduce the rotation
                currentControllerRotationY -= unrotateY;    // Reducing the positive rotation
            }
            else if (currentControllerRotationY < 0)
            {
                float unrotateY = Mathf.Min(centeringControllerVelocityY * Time.deltaTime, -currentControllerRotationY);
                r360ControllerY.Rotate(0, -unrotateY, 0);  // Rotating to reduce the rotation
                currentControllerRotationY += unrotateY;    // Reducing the negative rotation
            }

            // Center X-Axis Controller rotation //swapped to Z
            if (currentControllerRotationX > 0)
            {
                float unrotateX = Mathf.Min(centeringControllerVelocityX * Time.deltaTime, currentControllerRotationX);
                r360ControllerZ.Rotate(unrotateX, 0, 0);   // Rotating to reduce the rotation
                currentControllerRotationX -= unrotateX;    // Reducing the positive rotation
            }
            else if (currentControllerRotationX < 0)
            {
                float unrotateX = Mathf.Min(centeringControllerVelocityX * Time.deltaTime, -currentControllerRotationX);
                r360ControllerZ.Rotate(-unrotateX, 0, 0);   // Rotating to reduce the rotation
                currentControllerRotationX += unrotateX;    // Reducing the positive rotation
            }

            // Center Z-axis Controller rotation // Swapped to X
            if (currentControllerRotationZ > 0)
            {
                float unrotateZ = Mathf.Min(centeringControllerVelocityZ * Time.deltaTime, currentControllerRotationZ);
                r360ControllerX.Rotate(0, 0, unrotateZ);   // Rotating to reduce the rotation
                currentControllerRotationZ -= unrotateZ;    // Reducing the positive rotation
            }
            else if (currentControllerRotationZ < 0)
            {
                float unrotateZ = Mathf.Min(centeringControllerVelocityZ * Time.deltaTime, -currentControllerRotationZ);
                r360ControllerX.Rotate(0, 0, -unrotateZ);   // Rotating to reduce the rotation
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


        // Initialize the emissive arrays with the appropriate objects
        private void InitializeEmissiveArrays()
        {
            // Find front emissive objects under "emissive" in the root
            frontEmissiveObjects = new Renderer[16];
            Transform emissiveObject = transform.Find("emissive");
            if (emissiveObject != null)
            {
                frontEmissiveObjects[0] = emissiveObject.Find("emissive1step1")?.GetComponent<Renderer>();
                frontEmissiveObjects[1] = emissiveObject.Find("emissive1step2")?.GetComponent<Renderer>();
                frontEmissiveObjects[2] = emissiveObject.Find("emissive1step3")?.GetComponent<Renderer>();
                frontEmissiveObjects[3] = emissiveObject.Find("emissive1step4")?.GetComponent<Renderer>();
                frontEmissiveObjects[4] = emissiveObject.Find("emissive2step1")?.GetComponent<Renderer>();
                frontEmissiveObjects[5] = emissiveObject.Find("emissive2step2")?.GetComponent<Renderer>();
                frontEmissiveObjects[6] = emissiveObject.Find("emissive2step3")?.GetComponent<Renderer>();
                frontEmissiveObjects[7] = emissiveObject.Find("emissive2step4")?.GetComponent<Renderer>();
                frontEmissiveObjects[8] = emissiveObject.Find("emissive3step1")?.GetComponent<Renderer>();
                frontEmissiveObjects[9] = emissiveObject.Find("emissive3step2")?.GetComponent<Renderer>();
                frontEmissiveObjects[10] = emissiveObject.Find("emissive3step3")?.GetComponent<Renderer>();
                frontEmissiveObjects[11] = emissiveObject.Find("emissive3step4")?.GetComponent<Renderer>();
                frontEmissiveObjects[12] = emissiveObject.Find("emissive4step1")?.GetComponent<Renderer>();
                frontEmissiveObjects[13] = emissiveObject.Find("emissive4step2")?.GetComponent<Renderer>();
                frontEmissiveObjects[14] = emissiveObject.Find("emissive4step3")?.GetComponent<Renderer>();
                frontEmissiveObjects[15] = emissiveObject.Find("emissive4step4")?.GetComponent<Renderer>();
            }

            // Initialize left and right arrays from thrustObject
            leftEmissiveObjects = new Renderer[15];
            rightEmissiveObjects = new Renderer[15];
            thrustObject = r360ZObject.Find("thrust");
            if (thrustObject != null)
            {
                // Left side
                leftEmissiveObjects[0] = thrustObject.Find("thrustL_1_1")?.GetComponent<Renderer>();
                leftEmissiveObjects[1] = thrustObject.Find("thrustL_1_2")?.GetComponent<Renderer>();
                leftEmissiveObjects[2] = thrustObject.Find("thrustL_1_3")?.GetComponent<Renderer>();
                leftEmissiveObjects[3] = thrustObject.Find("thrustL_2_1")?.GetComponent<Renderer>();
                leftEmissiveObjects[4] = thrustObject.Find("thrustL_2_2")?.GetComponent<Renderer>();
                leftEmissiveObjects[5] = thrustObject.Find("thrustL_2_3")?.GetComponent<Renderer>();
                leftEmissiveObjects[6] = thrustObject.Find("thrustL_3_1")?.GetComponent<Renderer>();
                leftEmissiveObjects[7] = thrustObject.Find("thrustL_3_2")?.GetComponent<Renderer>();
                leftEmissiveObjects[8] = thrustObject.Find("thrustL_3_3")?.GetComponent<Renderer>();
                leftEmissiveObjects[9] = thrustObject.Find("thrustL_4_1")?.GetComponent<Renderer>();
                leftEmissiveObjects[10] = thrustObject.Find("thrustL_4_2")?.GetComponent<Renderer>();
                leftEmissiveObjects[11] = thrustObject.Find("thrustL_4_3")?.GetComponent<Renderer>();
                leftEmissiveObjects[12] = thrustObject.Find("thrustL_5_1")?.GetComponent<Renderer>();
                leftEmissiveObjects[13] = thrustObject.Find("thrustL_5_2")?.GetComponent<Renderer>();
                leftEmissiveObjects[14] = thrustObject.Find("thrustL_5_3")?.GetComponent<Renderer>();

                // Right side
                rightEmissiveObjects[0] = thrustObject.Find("thrustR_1_1")?.GetComponent<Renderer>();
                rightEmissiveObjects[1] = thrustObject.Find("thrustR_1_2")?.GetComponent<Renderer>();
                rightEmissiveObjects[2] = thrustObject.Find("thrustR_1_3")?.GetComponent<Renderer>();
                rightEmissiveObjects[3] = thrustObject.Find("thrustR_2_1")?.GetComponent<Renderer>();
                rightEmissiveObjects[4] = thrustObject.Find("thrustR_2_2")?.GetComponent<Renderer>();
                rightEmissiveObjects[5] = thrustObject.Find("thrustR_2_3")?.GetComponent<Renderer>();
                rightEmissiveObjects[6] = thrustObject.Find("thrustR_3_1")?.GetComponent<Renderer>();
                rightEmissiveObjects[7] = thrustObject.Find("thrustR_3_2")?.GetComponent<Renderer>();
                rightEmissiveObjects[8] = thrustObject.Find("thrustR_3_3")?.GetComponent<Renderer>();
                rightEmissiveObjects[9] = thrustObject.Find("thrustR_4_1")?.GetComponent<Renderer>();
                rightEmissiveObjects[10] = thrustObject.Find("thrustR_4_2")?.GetComponent<Renderer>();
                rightEmissiveObjects[11] = thrustObject.Find("thrustR_4_3")?.GetComponent<Renderer>();
                rightEmissiveObjects[12] = thrustObject.Find("thrustR_5_1")?.GetComponent<Renderer>();
                rightEmissiveObjects[13] = thrustObject.Find("thrustR_5_2")?.GetComponent<Renderer>();
                rightEmissiveObjects[14] = thrustObject.Find("thrustR_5_3")?.GetComponent<Renderer>();
            }

            LogMissingObject(frontEmissiveObjects, "frontEmissiveObjects");
            LogMissingObject(leftEmissiveObjects, "leftEmissiveObjects");
            LogMissingObject(rightEmissiveObjects, "rightEmissiveObjects");
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
                    Debug.Log("Renderer component not found on one of the emissive objects.");
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
               //    logger.Error($"{arrayName} object at index {i} not found.");
                }
            }
        }

        // Attract pattern for the front
        IEnumerator FrontAttractPattern()
        {
            int previousStep = -1; // Track the previous step

            while (true)
            {
                // Iterate through each "step" (0 to 3, corresponding to "step 1" to "step 4")
                for (int step = 0; step < 4; step++)
                {
                    // Turn on all lights for the current step
                    for (int group = step; group < frontEmissiveObjects.Length; group += 4)
                    {
                        ToggleEmissive(frontEmissiveObjects[group], true);
                    }

                    // If there was a previous step, wait before turning off its lights
                    if (previousStep >= 0)
                    {
                        yield return new WaitForSeconds(frontFlashDelay);

                        // Turn off the previous step's lights
                        for (int group = previousStep; group < frontEmissiveObjects.Length; group += 4)
                        {
                            ToggleEmissive(frontEmissiveObjects[group], false);
                        }
                    }

                    // Update the previous step
                    previousStep = step;

                    // Wait for the duration before moving to the next step
                    yield return new WaitForSeconds(frontFlashDuration);
                }

                // Turn off the last step's lights after the loop
                yield return new WaitForSeconds(frontFlashDelay);
                for (int group = previousStep; group < frontEmissiveObjects.Length; group += 4)
                {
                    ToggleEmissive(frontEmissiveObjects[group], false);
                }
                previousStep = -1; // Reset previous step for the next cycle
            }
        }

        // Attract pattern for the side
        IEnumerator SideAttractPattern(Renderer[] emissiveObjects)
        {
            int previousIndex = -1; // Track the previous light index

            while (true)
            {
                for (int i = 0; i < emissiveObjects.Length; i++)
                {
                    // Turn on the current light
                    ToggleEmissive(emissiveObjects[i], true);

                    // If there was a previous light, wait before turning it off
                    if (previousIndex >= 0)
                    {
                        yield return new WaitForSeconds(sideFlashDelay); // Use the sideFlashDelay for the overlap timing
                        ToggleEmissive(emissiveObjects[previousIndex], false);
                    }

                    // Update the previous light index
                    previousIndex = i;

                    // Wait before moving to the next light
                    yield return new WaitForSeconds(sideFlashDuration);
                }

                // Turn off the last light after the loop
                yield return new WaitForSeconds(sideFlashDelay);
                ToggleEmissive(emissiveObjects[previousIndex], false);
                previousIndex = -1; // Reset previous index for the next cycle
            }
        }


        IEnumerator FrontDangerPattern()
        {
            while (true)
            {
                // Flash even-numbered lights
                for (int i = 1; i < frontEmissiveObjects.Length; i += 2)
                {
                    ToggleEmissive(frontEmissiveObjects[i], true);
                }
                yield return new WaitForSeconds(frontDangerDuration);

                // Turn off even-numbered lights
                for (int i = 1; i < frontEmissiveObjects.Length; i += 2)
                {
                    ToggleEmissive(frontEmissiveObjects[i], false);
                }

                // Flash odd-numbered lights
                for (int i = 0; i < frontEmissiveObjects.Length; i += 2)
                {
                    ToggleEmissive(frontEmissiveObjects[i], true);
                }
                yield return new WaitForSeconds(frontDangerDuration);

                // Turn off odd-numbered lights
                for (int i = 0; i < frontEmissiveObjects.Length; i += 2)
                {
                    ToggleEmissive(frontEmissiveObjects[i], false);
                }

                yield return new WaitForSeconds(frontDangerDelay);
            }
        }

        // Danger pattern for the sides
        IEnumerator SideDangerPattern(Renderer[] emissiveObjects)
        {
            while (true)
            {
                // Flash even-numbered lights in each group
                for (int group = 1; group < 3; group += 2) // This iterates over the second light in each group (index 1, 4, 7, 10, 13)
                {
                    for (int i = group; i < emissiveObjects.Length; i += 3)
                    {
                        ToggleEmissive(emissiveObjects[i], true);
                    }
                }
                yield return new WaitForSeconds(sideDangerDuration);

                // Turn off even-numbered lights
                for (int group = 1; group < 3; group += 2)
                {
                    for (int i = group; i < emissiveObjects.Length; i += 3)
                    {
                        ToggleEmissive(emissiveObjects[i], false);
                    }
                }

                // Flash odd-numbered lights in each group
                for (int group = 0; group < 3; group += 2) // This iterates over the first and third lights in each group (index 0, 3, 6, 9, 12)
                {
                    for (int i = group; i < emissiveObjects.Length; i += 3)
                    {
                        ToggleEmissive(emissiveObjects[i], true);
                    }
                }
                yield return new WaitForSeconds(sideDangerDuration);

                // Turn off odd-numbered lights
                for (int group = 0; group < 3; group += 2)
                {
                    for (int i = group; i < emissiveObjects.Length; i += 3)
                    {
                        ToggleEmissive(emissiveObjects[i], false);
                    }
                }

                yield return new WaitForSeconds(sideDangerDelay);
            }
        }


        // Method to toggle emissive on or off
        void ToggleEmissive(Renderer renderer, bool isOn)
        {
            if (renderer != null)
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
        }

        // Method to toggle all in the array
        void ToggleAll(Renderer[] emissiveObjects, bool isOn)
        {
            foreach (var renderer in emissiveObjects)
            {
                ToggleEmissive(renderer, isOn);
            }
        }

        public void TurnAllOff()
        {
            ToggleAll(frontEmissiveObjects, false);
            ToggleAll(leftEmissiveObjects, false);
            ToggleAll(rightEmissiveObjects, false);
        }

        public void StartAttractPattern()
        {
            StopCurrentPatterns();

            frontCoroutine = StartCoroutine(FrontAttractPattern());
            leftCoroutine = StartCoroutine(SideAttractPattern(leftEmissiveObjects));
            rightCoroutine = StartCoroutine(SideAttractPattern(rightEmissiveObjects));
        }

        public void StartDangerPattern()
        {
            StopCurrentPatterns();

            frontCoroutine = StartCoroutine(FrontDangerPattern());
            leftCoroutine = StartCoroutine(SideDangerPattern(leftEmissiveObjects));
            rightCoroutine = StartCoroutine(SideDangerPattern(rightEmissiveObjects));
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

        // Method to toggle the lights
        void ToggleLights(bool isActive)
        {
            for (int i = 0; i < lights.Length; i++)
            {
                lights[i].enabled = isActive;
            }

            logger.Info($"Lights turned {(isActive ? "on" : "off")}.");
        }

        // Method to toggle the fireemissive object
        void ToggleFireEmissive(bool isActive)
        {
            if (fireemissiveObject != null)
            {
                Renderer renderer = fireemissiveObject.GetComponent<Renderer>();
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
                    logger.Info($"fireemissive object emission turned {(isActive ? "on" : "off")}.");
                }
                else
                {
                    logger.Debug("Renderer component is not found on fireemissive object.");
                }
            }
            else
            {
                logger.Debug("fireemissive object is not assigned.");
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
