using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using EmuVR.InputManager;
using System.Collections;

namespace WIGUx.Modules.radmMotionSim
{
    public class radmMotionSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        private readonly float keyboardVelocityX = 15.5f;  // Velocity for keyboard input
        private readonly float keyboardVelocityY = 15.5f;  // Velocity for keyboard input
        private readonly float keyboardVelocityZ = 15.5f;  // Velocity for keyboard input
        private readonly float vrVelocity = 5.5f;        // Velocity for VR controller input

        private readonly float centeringVelocityX = 15.5f;  // Velocity for centering rotation
        private readonly float centeringVelocityY = 15.5f;  // Velocity for centering rotation
        private readonly float centeringVelocityZ = 15.5f;  // Velocity for centering rotation

        private float adjustSpeed = 1.0f;  // Adjust this adjustment speed as needed a lower number will lead to smaller adustments

        private float rotationLimitX = 12f;  // Rotation limit for X-axis
        private float rotationLimitY = 0f;  // Rotation limit for Y-axis
        private float rotationLimitZ = 0f;  // Rotation limit for Z-axis

        private float currentRotationX = 0f;  // Current rotation for X-axis
        private float currentRotationY = 0f;  // Current rotation for Y-axis
        private float currentRotationZ = 0f;  // Current rotation for Z-axis

        // Controller animation 
        // Speeds for the animation of the in game flight stick or wheel
        private readonly float keyboardControllerVelocityX = 500.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityY = 500.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityZ = 500.5f;  // Velocity for keyboard input
        private readonly float vrControllerVelocity = 500.5f;        // Velocity for VR controller input

        private float controllerrotationLimitX = 270f;  // Rotation limit for X-axis (stick or wheel)
        private float controllerrotationLimitY = 0f;  // Rotation limit for Y-axis (stick or wheel)
        private float controllerrotationLimitZ = 0f;  // Rotation limit for Z-axis (stick or wheel)

        private float currentControllerRotationX = 0f;  // Current rotation for X-axis (stick or wheel)
        private float currentControllerRotationY = 0f;  // Current rotation for Y-axis (stick or wheel)
        private float currentControllerRotationZ = 0f;  // Current rotation for Z-axis (stick or wheel)

        private readonly float centeringControllerVelocityX = 300.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float centeringControllerVelocityY = 300.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float centeringControllerVelocityZ = 300.5f;  // Velocity for centering rotation (stick or wheel)

        private Transform radmController; // Reference to the main animated controller (wheel)
        private Vector3 radmControllerStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion radmControllerStartRotation; // Initial controlller positions and rotations for resetting
        /*
        private Transform radmControllerX; // Reference to the main animated controller (wheel)
        private Vector3 radmControllerXStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion radmControllerXStartRotation; // Initial controlller positions and rotations for resetting
        private Transform radmControllerY; // Reference to the main animated controller (wheel)
        private Vector3 radmControllerYStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion radmControllerYStartRotation; // Initial controlller positions and rotations for resetting
        private Transform radmControllerZ; // Reference to the main animated controller (wheel)
        private Vector3 radmControllerZStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion radmControllerZStartRotation; // Initial controlller positions and rotations for resetting
         */

        private Transform radmXObject; // Reference to the main X object
        private Transform radmYObject; // Reference to the main Y object
        private Transform radmZObject; // Reference to the main Z object

        private GameObject cockpitCam;    // Reference to the cockpit camera

        // Initial positions and rotations for resetting
        private Vector3 radmXStartPosition;
        private Quaternion radmXStartRotation;
        private Vector3 radmYStartPosition;
        private Quaternion radmYStartRotation;
        private Vector3 radmZStartPosition;
        private Quaternion radmZStartRotation;
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
        private bool inFocusMode = false;  // Flag to track focus mode state

        //lights
        private Light[] lights;
        private Transform fireemissiveObject;
        private Transform fireemissive2Object;
        public Light fire1_light;
        public Light fire2_light;
        public string fire1Button = "Fire1"; // Name of the fire button
        public string fire2Button = "Fire2"; // Name of the fire button 
        public string fire3Button = "Fire3"; // Name of the fire button 
        public string JumpButton = "Jump"; // Name of the fire button 
        // public float lightDuration = 0.35f; // Duration during which the lights will be on

        private readonly string[] compatibleGames = { "radm.zip", "radm.7z" };

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

            // Find radmX object in hierarchy
            radmXObject = transform.Find("radmX");
            if (radmXObject != null)
            {
                logger.Info("radmX object found.");
                radmXStartPosition = radmXObject.position;
                radmXStartRotation = radmXObject.rotation;

                // Find radmY object under radmX
                radmYObject = radmXObject.Find("radmY");
                if (radmYObject != null)
                {
                    logger.Info("radmY object found.");
                    radmYStartPosition = radmYObject.position;
                    radmYStartRotation = radmYObject.rotation;

                    // Find radmZ object under radmY
                    radmZObject = radmYObject.Find("radmZ");
                    if (radmZObject != null)
                    {
                        logger.Info("radmZ object found.");
                        radmZStartPosition = radmZObject.position;
                        radmZStartRotation = radmZObject.rotation;

                        // Find fireemissive object under radmX
                        fireemissiveObject = radmZObject.Find("emissive");
                        if (fireemissiveObject != null)
                        {
                            logger.Info("emissive object found.");
                            // Ensure the fireemissive object is initially off
                            Renderer renderer = fireemissiveObject.GetComponent<Renderer>();
                            if (renderer != null)
                            {
                                renderer.material.DisableKeyword("_EMISSION");
                            }
                            else
                            {
                                logger.Debug("Renderer component is not found on emissive object.");
                            }
                        }
                        else
                        {
                            logger.Debug("emissive object not found under radmZ.");
                        }

                        // Find fire1_light object
                        fire1_light = radmZObject.Find("fire1_light").GetComponent<Light>();
                        if (fire1_light != null)
                        {
                            logger.Info("fire1_light object found.");
                        }
                        else
                        {
                            logger.Debug("fire1_light object not found.");
                        }

                        // Find fire2_light object
                        fire2_light = radmZObject.Find("fire2_light").GetComponent<Light>();
                        if (fire2_light != null)
                        {
                            logger.Info("fire2_light object found.");
                        }
                        else
                        {
                            logger.Debug("fire2_light object not found.");
                        }

                        // Find radmController under radmZ
                        radmController = radmZObject.Find("radmController");
                        if (cockpitCam != null)
                        {
                            logger.Info("radmController object found.");

                            // Store initial position and rotation of the wheel
                            radmControllerStartPosition = radmController.transform.position;
                            radmControllerStartRotation = radmController.transform.rotation;
                        }
                        else
                        {
                         //   logger.Error("radmController object not found under radmZ!");
                        }

                        // Find cockpit camera under radmZ
                        cockpitCam = radmZObject.Find("eyes/cockpitcam")?.gameObject;
                        if (cockpitCam != null)
                        {
                            logger.Info("Cockpitcam object found.");

                            // Store initial position and rotation of cockpit cam
                            cockpitCamStartPosition = cockpitCam.transform.position;
                            cockpitCamStartRotation = cockpitCam.transform.rotation;
                        }
                        else
                        {
                            logger.Error("Cockpitcam object not found under radmZ!");
                        }
                    }
                    else
                    {
                        logger.Error("radmZ object not found under radmY!");
                    }
                }

                else
                {
                    logger.Error("radmY object not found under radmX!");
                }
            }
            else
            {
                logger.Error("radmX object not found!");
            }
        }

        void Update()
        {
            bool inputDetected = false; // Initialize at the beginning of the Update method

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
            }
        }

        void StartFocusMode()
        {
            string controlledSystemGamePathString = GameSystem.ControlledSystem.Game.path != null ? GameSystem.ControlledSystem.Game.path.ToString() : null;
            logger.Info($"Controlled System Game path String: {controlledSystemGamePathString}");
            logger.Info("Compatible Rom Dectected, Hanging Sonic on the Mirror...");
            logger.Info("Sega Rad Mobile Deluxe Motion Sim starting...");
            logger.Info("El queso está viejo y podrido. Dónde está el sanitario");
            cockpitCam.transform.position = cockpitCamStartPosition; // new hotness
            cockpitCam.transform.rotation = cockpitCamStartRotation; // new hotness
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
                logger.Error("CockpitCam object not found under radmZ!");
            }

            // Reset rotation allowances and current rotation values
            currentRotationX = 0f;
            currentRotationY = 0f;
            currentRotationZ = 0f;
            currentControllerRotationX = 0f;
            currentControllerRotationY = 0f;
            currentControllerRotationZ = 0f;

            inFocusMode = true;  // Set focus mode flag
        }

        void EndFocusMode()
        {
            logger.Info("Exiting Focus Mode...");
            // Restore original parents of objects
            RestoreOriginalParent(playerCamera, "PlayerCamera");
            RestoreOriginalParent(playerVRSetup, "PlayerVRSetup.PlayerRig");

            // Reset radmX to initial positions and rotations
            if (radmXObject != null)
            {
                radmXObject.position = radmXStartPosition;
                radmXObject.rotation = radmXStartRotation;
            }

            // Reset radmY object to initial position and rotation
            if (radmYObject != null)
            {
                radmYObject.position = radmYStartPosition;
                radmYObject.rotation = radmYStartRotation;
            }
            // Reset radmZ object to initial position and rotation
            if (radmZObject != null)
            {
                radmZObject.position = radmZStartPosition;
                radmZObject.rotation = radmZStartRotation;
            }

            // Reset radmcontroller object to initial position and rotation
            if (radmController != null)
            {
                // radmController.position = radmControllerStartPosition; // didnt need this
                radmController.rotation = radmControllerStartRotation; 
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

        void HandleKeyboardInput(ref bool inputDetected)
        {
            if (!inFocusMode) return;

            /*
            // Fire1
            if (Input.GetButtonDown("Fire1"))
            {
                inputDetected = true;
            }

            // Reset position on button release
            if (Input.GetButtonUp("Fire1"))
            {
                inputDetected = true;
            }

            // Fire2
            if (Input.GetButtonDown("Fire2"))
            {
                StartCoroutine(LightsOn());
                inputDetected = true;
            }

            // Reset position on button release
            if (Input.GetButtonUp("Fire2"))
            {
                StartCoroutine(LightsOff());
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

            // Stick Rotations 

            // Stick Y Rotation
            if (Input.GetKey(KeyCode.RightArrow) && currentControllerRotationZ > -controllerrotationLimitZ)
            {
                float rotateZ = keyboardControllerVelocityZ * Time.deltaTime;
                radmController.Rotate(0, 0, rotateZ);
                currentControllerRotationZ -= rotateZ;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.LeftArrow) && currentControllerRotationZ < controllerrotationLimitZ)
            {
                float rotateZ = keyboardControllerVelocityY * Time.deltaTime;
                radmController.Rotate(0, 0, -rotateZ);
                currentControllerRotationZ += rotateZ;
                inputDetected = true;
            }
                        */
            /*
            // Stick X Rotation
            if (Input.GetKey(KeyCode.UpArrow) && currentControllerRotationX > -controllerrotationLimitX)
            {
                float rotateX = keyboardControllerVelocityX * Time.deltaTime;
                radmController.Rotate(rotateX, 0, 0);
                currentControllerRotationX -= rotateX;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.DownArrow) && currentControllerRotationX < controllerrotationLimitX)
            {
                float rotateX = keyboardControllerVelocityX * Time.deltaTime;
                radmController.Rotate(-rotateX, 0, 0);
                currentControllerRotationX += rotateX;
                inputDetected = true;
            }

            // Stick Z Rotation
            if (Input.GetKey(KeyCode.LeftArrow) && currentControllerRotationZ > -controllerrotationLimitZ)
            {
                float rotateZ = keyboardControllerVelocityZ * Time.deltaTime;
                radmController.Rotate(0, 0, rotateZ);
                currentControllerRotationZ -= rotateZ;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.RightArrow) && currentControllerRotationZ < controllerrotationLimitZ)
            {
                float rotateZ = keyboardControllerVelocityZ * Time.deltaTime;
                radmController.Rotate(0, 0, -rotateZ);
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
                    logger.Info("OVR A button pressed");
                }

                // Check if the B button on the right controller is pressed
                if (OVRInput.GetDown(OVRInput.Button.Two))
                {
                    logger.Info("OVR B button pressed");
                }

                // Check if the X button on the left controller is pressed
                if (OVRInput.GetDown(OVRInput.Button.Three))
                {
                    logger.Info("OVR X button pressed");
                }

                // Check if the Y button on the left controller is pressed
                if (OVRInput.GetDown(OVRInput.Button.Four))
                {
                    logger.Info("OVR Y button pressed");
                }

                // Check if the primary index trigger on the right controller is pressed
                if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger))
                {
                    logger.Info("OVR Primary index trigger pressed");
                }

                // Check if the secondary index trigger on the left controller is pressed
                if (OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger))
                {
                    logger.Info("OVR Secondary index trigger pressed");
                }

                // Check if the primary hand trigger on the right controller is pressed
                if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger))
                {
                    logger.Info("OVR Primary hand trigger pressed");
                }

                // Check if the secondary hand trigger on the left controller is pressed
                if (OVRInput.Get(OVRInput.Button.SecondaryHandTrigger))
                {
                    logger.Info("OVR Secondary hand trigger pressed");
                }

                // Check if the primary thumbstick is pressed
                if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick))
                {
                    logger.Info("OVR Primary thumbstick pressed");
                }

                // Check if the secondary thumbstick is pressed
                if (OVRInput.GetDown(OVRInput.Button.SecondaryThumbstick))
                {
                    logger.Info("OVR Secondary thumbstick pressed");
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
                    inputDetected = true;
                }

                // LeftTrigger
                if (XInput.GetDown(XInput.Button.LIndexTrigger))
                {
                    ToggleFireEmissive1(true);
                    ToggleLight1(true);
                    ToggleLight2(true);
                    inputDetected = true;
                }

                // Reset position on button release
                if (XInput.GetUp(XInput.Button.LIndexTrigger))
                {
                    ToggleFireEmissive1(false);
                    ToggleLight1(false);
                    ToggleLight2(false);
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
            // Handle X rotation for aburnerYObject and aburnerControllerX (Right Arrow or primaryThumbstick.x > 0)
            // Thumbstick direction: right
            if ((Input.GetKey(KeyCode.RightArrow) || primaryThumbstick.x > 0))
            {
                if (currentRotationX < rotationLimitX)
                {
                    float rotateX = (Input.GetKey(KeyCode.RightArrow) ? keyboardVelocityX : primaryThumbstick.x * vrVelocity) * Time.deltaTime;
                    radmXObject.Rotate(-rotateX, 0, 0);
                    currentRotationX += rotateX;
                    inputDetected = true;
                }
                if (currentControllerRotationX < controllerrotationLimitX)
                {
                    float controllerRotateX = (Input.GetKey(KeyCode.RightArrow) ? keyboardControllerVelocityX : primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                    radmController.Rotate(-controllerRotateX, 0, 0);
                    currentControllerRotationX += controllerRotateX;
                    inputDetected = true;
                }
            }

            // Handle X rotation for aburnerYObject and aburnerControllerX (Left Arrow or primaryThumbstick.x < 0)
            // Thumbstick direction: left
            if ((Input.GetKey(KeyCode.LeftArrow) || primaryThumbstick.x < 0))
            {
                if (currentRotationX > -rotationLimitX)
                {
                    float rotateX = (Input.GetKey(KeyCode.LeftArrow) ? keyboardVelocityX : -primaryThumbstick.x * vrVelocity) * Time.deltaTime;
                    radmXObject.Rotate(rotateX, 0, 0);
                    currentRotationX -= rotateX;
                    inputDetected = true;
                }
                if (currentControllerRotationX > -controllerrotationLimitX)
                {
                    float controllerRotateX = (Input.GetKey(KeyCode.LeftArrow) ? keyboardControllerVelocityX : -primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                    radmController.Rotate(controllerRotateX, 0, 0);
                    currentControllerRotationX -= controllerRotateX;
                    inputDetected = true;
                }
            }


            /*
            // Handle X rotation (Thumbstick left)
            if (primaryThumbstick.x < 0 && currentRotationX < rotationLimitX)
            {
                float rotateX = primaryThumbstick.x * vrVelocity * Time.deltaTime;
                radmYObject.Rotate(rotateX, 0, 0);
                currentRotationX -= rotateX; 
                inputDetected = true;
            }

            // Handle X rotation (Thumbstick right)
            if (primaryThumbstick.x > 0 && currentRotationX > -rotationLimitX)
            {
                float rotateX = primaryThumbstick.x * vrVelocity * Time.deltaTime;
                radmYObject.Rotate(rotateX, 0, 0);  
                currentRotationX -= rotateX;
                inputDetected = true;
            }

            //HANDLE INGAME CONTROLLER ANIMATION

            // Handle controller rotation

            // Handle X rotation (Thumbstick left)
            if (primaryThumbstick.x < 0 && currentControllerRotationX < controllerrotationLimitX)
            {
                float rotateX = primaryThumbstick.x * vrControllerVelocity * Time.deltaTime;
                radmController.Rotate(rotateX, 0, 0);
                currentControllerRotationX -= rotateX;
                inputDetected = true;
            }
            // Handle X rotation (Thumbstick right)
            if (primaryThumbstick.x > 0 && currentControllerRotationX > -controllerrotationLimitX)
            {
                float rotateX = primaryThumbstick.x * vrControllerVelocity * Time.deltaTime;
                radmController.Rotate(rotateX, 0, 0);
                currentControllerRotationX -= rotateX;
                inputDetected = true;
            }

            /*

            // Handle Y rotation (Thumbstick left)
            if (primaryThumbstick.x > 0 && currentRotationY < rotationLimitY)
            {
                float rotateY = -primaryThumbstick.x * vrVelocity * Time.deltaTime;
                radmYObject.Rotate(0, rotateY, 0);  // Rotate Y to the left
                currentRotationY -= rotateY;  // Update current rotation (more positive)
                inputDetected = true;
            }
            // Handle Y rotation (Thumbstick right)
            if (primaryThumbstick.x < 0 && currentRotationY > -rotationLimitY)
            {
                float rotateY = primaryThumbstick.x * vrVelocity * Time.deltaTime;
                radmYObject.Rotate(0, -rotateY, 0);  // Rotate Y to the right
                currentRotationY += rotateY;  // Update current rotation (more negative)
                inputDetected = true;
            }

            // Handle Z rotation (Thumbstick left)
            if (primaryThumbstick.x > 0 && currentRotationZ > -rotationLimitZ)
            {
                float rotateZ = -primaryThumbstick.x * vrVelocity * Time.deltaTime;
                radmXObject.Rotate(0, 0, -rotateZ);  // Rotate forward
                currentRotationZ += rotateZ;  // Update current rotation (more negative)
                inputDetected = true;
            }
            // Handle Z rotation (Thumbstick right)
            if (primaryThumbstick.x < 0 && currentRotationZ < rotationLimitZ)
            {
                float rotateZ = -primaryThumbstick.x * vrVelocity * Time.deltaTime;
                radmXObject.Rotate(0, 0, -rotateZ);  // Rotate backward
                currentRotationZ += rotateZ;  // Update current rotation (more positive)
                inputDetected = true;
            }

            // Handle rotation (secondary Thumbstick left)
            if (secondaryThumbstick.x < 0 && currentRotationY < rotationLimitY)
            {
                float rotateY = -secondaryThumbstick.x * vrVelocity * Time.deltaTime;
                radmYObject.Rotate(0, -rotateY, 0);  // Rotate Y to the left
                currentRotationY += rotateY;  // Update current rotation (more positive)
                inputDetected = true;
            }
            // Handle rotation (secondary Thumbstick right)
            if (secondaryThumbstick.x > 0 && currentRotationY > -rotationLimitY)
            {
                float rotateY = secondaryThumbstick.x * vrVelocity * Time.deltaTime;
                radmYObject.Rotate(0, rotateY, 0);  // Rotate Y to the right
                currentRotationY -= rotateY;  // Update current rotation (more negative)
                inputDetected = true;
            }

            */

            //END OF CONTROLLER MAP

            /*
             // Handle controller rotation  (Thumbstick up)
            if (primaryThumbstick.y < 0 && currentControllerRotationY < controllerrotationLimitY)
            {
                float rotateY = primaryThumbstick.y * vrControllerVelocity * Time.deltaTime;
                radmController.Rotate(0, rotateY, 0); 
                currentControllerRotationY -= rotateT;  
                inputDetected = true;
            }
            // Handle controller rotation  (Thumbstick down)
            if (primaryThumbstick.y > 0 && currentControllerRotationY > -controllerrotationLimitY)
            {
                float rotateY = primaryThumbstick.y * vrControllerVelocity * Time.deltaTime;
                radmController.Rotate(0, rotateY, 0); 
                currentControllerRotationY -= rotateY; 
                inputDetected = true;
            }

             // Handle controller rotation  (Thumbstick up)
            if (primaryThumbstick.y < 0 && currentControllerRotationZ > -controllerrotationLimitZ)
            {
                float rotateZ = -primaryThumbstick.y * vrControllerVelocity * Time.deltaTime;
                radmController.Rotate(0, 0, rotateZ); 
                currentControllerRotationZ -= rotateZ; 
                inputDetected = true;
            }
            // Handle controller rotation  (Thumbstick down)
            if (primaryThumbstick.y > 0 && currentControllerRotationZ < controllerrotationLimitZ)
            {
                float rotateZ = -primaryThumbstick.y * vrControllerVelocity * Time.deltaTime;
                radmController.Rotate(0, 0, rotateZ);  
                currentControllerRotationZ -= rotateZ; 
                inputDetected = true;
            }
            */
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
            // Center X-axis (forward/backward rotation)

            // Center X-Axis rotation
            if (currentRotationX > 0)
            {
                float unrotateX = Mathf.Min(centeringVelocityX * Time.deltaTime, currentRotationX);
                radmYObject.Rotate(unrotateX, 0, 0);   // Rotating to reduce the rotation
                currentRotationX -= unrotateX;    // Reducing the positive rotation
            }
            else if (currentRotationX < 0)
            {
                float unrotateX = Mathf.Min(centeringVelocityX * Time.deltaTime, -currentRotationX);
                radmYObject.Rotate(-unrotateX, 0, 0);   // Rotating to reduce the rotation
                currentRotationX += unrotateX;    // Reducing the positive rotation
            }

            // Center Y-axis (left/right rotation with secondary thumbstick)
            if (currentRotationY > 0)
            {
                float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, currentRotationY);
                radmZObject.Rotate(0, unrotateY, 0);   // Rotating to reduce the rotation
                currentRotationY -= unrotateY;    // Reducing the positive rotation
            }
            else if (currentRotationY < 0)
            {
                float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, -currentRotationY);
                radmZObject.Rotate(0, -unrotateY, 0);   // Rotating to reduce the rotation
                currentRotationY += unrotateY;    // Reducing the positive rotation
            }

            // Center Z-axis (left/right rotation)
            if (currentRotationZ > 0)
            {
                float unrotateZ = Mathf.Min(centeringVelocityZ * Time.deltaTime, currentRotationZ);
                radmXObject.Rotate(0, 0, unrotateZ);   // Rotating to reduce the rotation
                currentRotationZ -= unrotateZ;    // Reducing the positive rotation
            }
            else if (currentRotationZ < 0)
            {
                float unrotateZ = Mathf.Min(centeringVelocityZ * Time.deltaTime, -currentRotationZ);
                radmXObject.Rotate(0, 0, -unrotateZ);   // Rotating to reduce the rotation
                currentRotationZ += unrotateZ;    // Reducing the positive rotation
            }

            //Centering for contoller
            // Center Y-axis Controller rotation
            if (currentControllerRotationY > 0)
            {
                float unrotateY = Mathf.Min(centeringControllerVelocityY * Time.deltaTime, currentControllerRotationY);
                radmController.Rotate(0, unrotateY, 0);   // Rotating to reduce the rotation
                currentControllerRotationY -= unrotateY;    // Reducing the positive rotation
            }
            else if (currentControllerRotationY < 0)
            {
                float unrotateY = Mathf.Min(centeringControllerVelocityY * Time.deltaTime, -currentControllerRotationY);
                radmController.Rotate(0, -unrotateY, 0);  // Rotating to reduce the rotation
                currentControllerRotationY += unrotateY;    // Reducing the negative rotation
            }

            // Center X-Axis Controller rotation
            if (currentControllerRotationX > 0)
            {
                float unrotateX = Mathf.Min(centeringControllerVelocityX * Time.deltaTime, currentControllerRotationX);
                radmController.Rotate(unrotateX, 0, 0);   // Rotating to reduce the rotation
                currentControllerRotationX -= unrotateX;    // Reducing the positive rotation
            }
            else if (currentControllerRotationX < 0)
            {
                float unrotateX = Mathf.Min(centeringControllerVelocityX * Time.deltaTime, -currentControllerRotationX);
                radmController.Rotate(-unrotateX, 0, 0);   // Rotating to reduce the rotation
                currentControllerRotationX += unrotateX;    // Reducing the positive rotation
            }

            // Center Z-axis Controller rotation
            if (currentControllerRotationZ > 0)
            {
                float unrotateZ = Mathf.Min(centeringControllerVelocityZ * Time.deltaTime, currentControllerRotationZ);
                radmController.Rotate(0, 0, unrotateZ);   // Rotating to reduce the rotation
                currentControllerRotationZ -= unrotateZ;    // Reducing the positive rotation
            }
            else if (currentControllerRotationZ < 0)
            {
                float unrotateZ = Mathf.Min(centeringControllerVelocityZ * Time.deltaTime, -currentControllerRotationZ);
                radmController.Rotate(0, 0, -unrotateZ);   // Rotating to reduce the rotation
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

        // Method to toggle the fire1 emissive object
        void ToggleFireEmissive1(bool isActive)
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
