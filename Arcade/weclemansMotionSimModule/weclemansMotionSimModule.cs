using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using EmuVR.InputManager;
using System.Collections;

namespace WIGUx.Modules.weclemansMotionSim
{
    public class weclemansMotionSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        private readonly float keyboardVelocityX = 35.5f;  // Velocity for keyboard input
        private readonly float keyboardVelocityY = 35.5f;  // Velocity for keyboard input
        private readonly float keyboardVelocityZ = 35.5f;  // Velocity for keyboard input
        private readonly float vrVelocity = 35.5f;        // Velocity for VR controller input

        private readonly float centeringVelocityX = 60.5f;  // Velocity for centering rotation
        private readonly float centeringVelocityY = 60.5f;  // Velocity for centering rotation
        private readonly float centeringVelocityZ = 60.5f;  // Velocity for centering rotation

        private float adjustSpeed = 1.0f;  // Adjust this adjustment speed as needed a lower number will lead to smaller adustments

        private float rotationLimitX = 0f;  // Rotation limit for X-axis
        private float rotationLimitY = 90f;  // Rotation limit for Y-axis
        private float rotationLimitZ = 0f;  // Rotation limit for Z-axis

        private float currentRotationX = 0f;  // Current rotation for X-axis
        private float currentRotationY = 0f;  // Current rotation for Y-axis
        private float currentRotationZ = 0f;  // Current rotation for Z-axis

        // Controller animation 
        // Speeds for the animation of the in game flight stick or wheel
        private readonly float keyboardControllerVelocityX = 400.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityY = 400.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityZ = 400.5f;  // Velocity for keyboard input
        private readonly float vrControllerVelocity = 400.5f;        // Velocity for VR controller input

        private float controllerrotationLimitX = 0f;  // Rotation limit for X-axis (stick or wheel)
        private float controllerrotationLimitY = 270f;  // Rotation limit for Y-axis (stick or wheel)
        private float controllerrotationLimitZ = 0f;  // Rotation limit for Z-axis (stick or wheel)

        private float currentControllerRotationX = 0f;  // Current rotation for X-axis (stick or wheel)
        private float currentControllerRotationY = 0f;  // Current rotation for Y-axis (stick or wheel)
        private float currentControllerRotationZ = 0f;  // Current rotation for Z-axis (stick or wheel)

        private readonly float centeringControllerVelocityX = 400.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float centeringControllerVelocityY = 400.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float centeringControllerVelocityZ = 400.5f;  // Velocity for centering rotation (stick or wheel)

        private Transform weclemansController; // Reference to the main animated controller (wheel)
        private Vector3 weclemansControllerStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion weclemansControllerStartRotation; // Initial controlller positions and rotations for resetting
        /*
        private Transform weclemansControllerX; // Reference to the main animated controller (wheel)
        private Vector3 weclemansControllerXStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion weclemansControllerXStartRotation; // Initial controlller positions and rotations for resetting
        private Transform weclemansControllerY; // Reference to the main animated controller (wheel)
        private Vector3 weclemansControllerYStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion weclemansControllerYStartRotation; // Initial controlller positions and rotations for resetting
        private Transform weclemansControllerZ; // Reference to the main animated controller (wheel)
        private Vector3 weclemansControllerZStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion weclemansControllerZStartRotation; // Initial controlller positions and rotations for resetting
         */

        private Transform weclemansXObject; // Reference to the main X object
        private Transform weclemansYObject; // Reference to the main Y object
        private Transform weclemansZObject; // Reference to the main Z object

        private GameObject cockpitCam;    // Reference to the cockpit camera

        // Initial positions and rotations for resetting
        private Vector3 weclemansXStartPosition;
        private Quaternion weclemansXStartRotation;
        private Vector3 weclemansYStartPosition;
        private Quaternion weclemansYStartRotation;
        private Vector3 weclemansZStartPosition;
        private Quaternion weclemansZStartRotation;
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
        public string fire1Button = "Fire1"; // Name of the fire button
        public string fire2Button = "Fire2"; // Name of the fire button 
        public string fire3Button = "Fire3"; // Name of the fire button 
        public string JumpButton = "Jump"; // Name of the fire button 
        // public float lightDuration = 0.35f; // Duration during which the lights will be on

        private readonly string[] compatibleGames = { "wecleman.zip", "wecleman.7z" };

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

            // Find weclemansX object in hierarchy
            weclemansXObject = transform.Find("weclemansX");
            if (weclemansXObject != null)
            {
                logger.Info("weclemansX object found.");
                weclemansXStartPosition = weclemansXObject.position;
                weclemansXStartRotation = weclemansXObject.rotation;

                // Find weclemansY object under weclemansX
                weclemansYObject = weclemansXObject.Find("weclemansY");
                if (weclemansYObject != null)
                {
                    logger.Info("weclemansY object found.");
                    weclemansYStartPosition = weclemansYObject.position;
                    weclemansYStartRotation = weclemansYObject.rotation;

                    // Find weclemansZ object under weclemansY
                    weclemansZObject = weclemansYObject.Find("weclemansZ");
                    if (weclemansZObject != null)
                    {
                        logger.Info("weclemansZ object found.");
                        weclemansZStartPosition = weclemansZObject.position;
                        weclemansZStartRotation = weclemansZObject.rotation;

                        // Find weclemansController under weclemansZ
                        weclemansController = weclemansZObject.Find("weclemansController");
                        if (cockpitCam != null)
                        {
                            logger.Info("weclemansController object found.");

                            // Store initial position and rotation of the wheel
                            weclemansControllerStartPosition = weclemansController.transform.position;
                            weclemansControllerStartRotation = weclemansController.transform.rotation;
                        }
                        else
                        {
                         //   logger.Error("weclemansController object not found under weclemansZ!");
                        }

                        // Find cockpit camera under weclemansZ
                        cockpitCam = weclemansZObject.Find("eyes/cockpitcam")?.gameObject;
                        if (cockpitCam != null)
                        {
                            logger.Info("Cockpitcam object found.");

                            // Store initial position and rotation of cockpit cam
                            cockpitCamStartPosition = cockpitCam.transform.position;
                            cockpitCamStartRotation = cockpitCam.transform.rotation;
                        }
                        else
                        {
                            logger.Error("Cockpitcam object not found under weclemansZ!");
                        }
                    }
                    else
                    {
                        logger.Error("weclemansZ object not found under weclemansY!");
                    }
                }

                else
                {
                    logger.Error("weclemansY object not found under weclemansX!");
                }
            }
            else
            {
                logger.Error("weclemansX object not found!");
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
                HandleVRInput(ref inputDetected);  // Pass by reference
                HandleKeyboardInput(ref inputDetected);  // Pass by reference
            }
        }

        void StartFocusMode()
        {
            string controlledSystemGamePathString = GameSystem.ControlledSystem.Game.path != null ? GameSystem.ControlledSystem.Game.path.ToString() : null;
            logger.Info($"Controlled System Game path String: {controlledSystemGamePathString}");
            logger.Info("Compatible Rom Dectected, Activating Motor...");
            logger.Info("Konami WEC Lemans Motion Sim starting...");
            logger.Info("GET DIZZY! Wait Dizzy what do you mean?");
            cockpitCam.transform.position = cockpitCamStartPosition; // new hotness
            cockpitCam.transform.rotation = cockpitCamStartRotation; // new hotness
            weclemansControllerStartPosition = weclemansController.transform.position;
            weclemansControllerStartRotation = weclemansController.transform.rotation;
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
                logger.Error("CockpitCam object not found under weclemansZ!");
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

            // Reset weclemansX to initial positions and rotations
            if (weclemansXObject != null)
            {
                weclemansXObject.position = weclemansXStartPosition;
                weclemansXObject.rotation = weclemansXStartRotation;
            }

            // Reset weclemansY object to initial position and rotation
            if (weclemansYObject != null)
            {
                weclemansYObject.position = weclemansYStartPosition;
                weclemansYObject.rotation = weclemansYStartRotation;
            }
            // Reset weclemansZ object to initial position and rotation
            if (weclemansZObject != null)
            {
                weclemansZObject.position = weclemansZStartPosition;
                weclemansZObject.rotation = weclemansZStartRotation;
            }

            // Reset weclemanscontroller object to initial position and rotation
            if (weclemansController != null)
            {
                weclemansController.position = weclemansControllerStartPosition; // didnt need this
                weclemansController.rotation = weclemansControllerStartRotation; 
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
            */
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
            /*
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
            */
            /*
            // Handle keyboard input for pitch and roll
            if (Input.GetKey(KeyCode.UpArrow) && currentRotationX > -rotationLimitX)
            {
                float rotateX = keyboardVelocityX * Time.deltaTime;
                weclemansYObject.Rotate(rotateX, 0, 0);
                currentRotationX -= rotateX;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.DownArrow) && currentRotationX < rotationLimitX)
            {
                float rotateX = keyboardVelocityX * Time.deltaTime;
                weclemansYObject.Rotate(-rotateX, 0, 0);
                currentRotationX += rotateX;
                inputDetected = true;
            }
            */
 
            if (Input.GetKey(KeyCode.RightArrow) && currentRotationZ > -rotationLimitZ)
            {
                float rotateZ = keyboardVelocityZ * Time.deltaTime;
                weclemansXObject.Rotate(0, 0, rotateZ);
                currentRotationZ -= rotateZ;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.LeftArrow) && currentRotationZ < rotationLimitZ)
            {
                float rotateZ = keyboardVelocityZ * Time.deltaTime;
                weclemansXObject.Rotate(0, 0, -rotateZ);
                currentRotationZ += rotateZ;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.LeftArrow) && currentRotationY > -rotationLimitY)
            {
                float rotateY = keyboardVelocityY * Time.deltaTime;
                weclemansZObject.Rotate(0, rotateY, 0);
                currentRotationY -= rotateY;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.RightArrow) && currentRotationY < rotationLimitY)
            {
                float rotateY = keyboardVelocityY * Time.deltaTime;
                weclemansZObject.Rotate(0, -rotateY, 0);
                currentRotationY += rotateY;
                inputDetected = true;
            }
            
            // Stick Rotations 

            // Stick Y Rotation
            if (Input.GetKey(KeyCode.RightArrow) && currentControllerRotationY > -controllerrotationLimitY)
            {
                float rotateY = keyboardControllerVelocityY * Time.deltaTime;
                weclemansController.Rotate(0, rotateY, 0);
                currentControllerRotationY -= rotateY;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.LeftArrow) && currentControllerRotationY < controllerrotationLimitY)
            {
                float rotateY = keyboardControllerVelocityY * Time.deltaTime;
                weclemansController.Rotate(0, -rotateY, 0);
                currentControllerRotationY += rotateY;
                inputDetected = true;
            }
       

            /*
            // Stick X Rotation
            if (Input.GetKey(KeyCode.UpArrow) && currentControllerRotationX > -controllerrotationLimitX)
            {
                float rotateX = keyboardControllerVelocityX * Time.deltaTime;
                weclemansController.Rotate(rotateX, 0, 0);
                currentControllerRotationX -= rotateX;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.DownArrow) && currentControllerRotationX < controllerrotationLimitX)
            {
                float rotateX = keyboardControllerVelocityX * Time.deltaTime;
                weclemansController.Rotate(-rotateX, 0, 0);
                currentControllerRotationX += rotateX;
                inputDetected = true;
            }

            // Stick Z Rotation
            if (Input.GetKey(KeyCode.LeftArrow) && currentControllerRotationZ > -controllerrotationLimitZ)
            {
                float rotateZ = keyboardControllerVelocityZ * Time.deltaTime;
                weclemansController.Rotate(0, 0, rotateZ);
                currentControllerRotationZ -= rotateZ;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.RightArrow) && currentControllerRotationZ < controllerrotationLimitZ)
            {
                float rotateZ = keyboardControllerVelocityZ * Time.deltaTime;
                weclemansController.Rotate(0, 0, -rotateZ);
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

            /*
            // Handle X rotation (Thumbstick up)
            if (primaryThumbstick.y > 0 && currentRotationX > -rotationLimitX)
            {
                float rotateX = primaryThumbstick.y * vrVelocity * Time.deltaTime;
                weclemansYObject.Rotate(rotateX, 0, 0);  // Rotate up
                currentRotationX -= rotateX;  // Update current rotation (more negative)
                inputDetected = true;
            }

            // Handle X rotation (Thumbstick down)
            if (primaryThumbstick.y < 0 && currentRotationX < rotationLimitX)
            {
                float rotateX = primaryThumbstick.y * vrVelocity * Time.deltaTime;
                weclemansYObject.Rotate(rotateX, 0, 0);  // Rotate backward (nose up)
                currentRotationX -= rotateX;
                inputDetected = true;
            }
            */

            // Handle Y rotation (Thumbstick left)
            if (primaryThumbstick.x > 0 && currentRotationY < rotationLimitY)
            {
                float rotateY = -primaryThumbstick.x * vrVelocity * Time.deltaTime;
                weclemansYObject.Rotate(0, rotateY, 0);  // Rotate Y to the left
                currentRotationY -= rotateY;  // Update current rotation (more positive)
                inputDetected = true;
            }
            // Handle Y rotation (Thumbstick right)
            if (primaryThumbstick.x < 0 && currentRotationY > -rotationLimitY)
            {
                float rotateY = primaryThumbstick.x * vrVelocity * Time.deltaTime;
                weclemansYObject.Rotate(0, -rotateY, 0);  // Rotate Y to the right
                currentRotationY += rotateY;  // Update current rotation (more negative)
                inputDetected = true;
            }

            // Handle Z rotation (Thumbstick left)
            if (primaryThumbstick.x > 0 && currentRotationZ > -rotationLimitZ)
            {
                float rotateZ = -primaryThumbstick.x * vrVelocity * Time.deltaTime;
                weclemansXObject.Rotate(0, 0, -rotateZ);  // Rotate forward
                currentRotationZ += rotateZ;  // Update current rotation (more negative)
                inputDetected = true;
            }
            // Handle Z rotation (Thumbstick right)
            if (primaryThumbstick.x < 0 && currentRotationZ < rotationLimitZ)
            {
                float rotateZ = -primaryThumbstick.x * vrVelocity * Time.deltaTime;
                weclemansXObject.Rotate(0, 0, -rotateZ);  // Rotate backward
                currentRotationZ += rotateZ;  // Update current rotation (more positive)
                inputDetected = true;
            }

            /*
            // Handle rotation (secondary Thumbstick left)
            if (secondaryThumbstick.x < 0 && currentRotationY < rotationLimitY)
            {
                float rotateY = -secondaryThumbstick.x * vrVelocity * Time.deltaTime;
                weclemansYObject.Rotate(0, -rotateY, 0);  // Rotate Y to the left
                currentRotationY += rotateY;  // Update current rotation (more positive)
                inputDetected = true;
            }
            // Handle rotation (secondary Thumbstick right)
            if (secondaryThumbstick.x > 0 && currentRotationY > -rotationLimitY)
            {
                float rotateY = secondaryThumbstick.x * vrVelocity * Time.deltaTime;
                weclemansYObject.Rotate(0, rotateY, 0);  // Rotate Y to the right
                currentRotationY -= rotateY;  // Update current rotation (more negative)
                inputDetected = true;
            }
            */
            //END OF CONTROLLER MAP

            //HANDLE INGAME CONTROLLER ANIMATION

            // Handle controller rotation (Thumbstick right)

            // Handle Y rotation (Thumbstick left)
            if (primaryThumbstick.x < 0 && currentControllerRotationY < controllerrotationLimitY)
            {
                float rotateY = -primaryThumbstick.x * vrControllerVelocity * Time.deltaTime;
                weclemansController.Rotate(0, -rotateY, 0);  // Rotate Y to the left
                currentControllerRotationY += rotateY;  // Update current rotation (more positive)
                inputDetected = true;
            }
            // Handle Y rotation (Thumbstick right)
            if (primaryThumbstick.x > 0 && currentControllerRotationY > -controllerrotationLimitY)
            {
                float rotateY = primaryThumbstick.x * vrControllerVelocity * Time.deltaTime;
                weclemansController.Rotate(0, rotateY, 0);  // Rotate Y to the right
                currentControllerRotationY -= rotateY;  // Update current rotation (more negative)
                inputDetected = true;
            }

            /*
             // Handle controller rotation  (Thumbstick up)
            if (primaryThumbstick.y < 0 && currentControllerRotationX < controllerrotationLimitX)
            {
                float rotateX = primaryThumbstick.y * vrControllerVelocity * Time.deltaTime;
                weclemansController.Rotate(rotateX, 0, 0);  // Rotate left (negative direction)
                currentControllerRotationX -= rotateX;  // Update current rotation (more negative)
                inputDetected = true;
            }
            // Handle controller rotation  (Thumbstick down)
            if (primaryThumbstick.y > 0 && currentControllerRotationX > -controllerrotationLimitX)
            {
                float rotateX = primaryThumbstick.y * vrControllerVelocity * Time.deltaTime;
                weclemansController.Rotate(rotateX, 0, 0);  // Rotate right (positive direction)
                currentControllerRotationX -= rotateX;  // Update current rotation (more positive)
                inputDetected = true;
            }

            // Handle controller rotation (Thumbstick left)
            if (primaryThumbstick.x < 0 && currentControllerRotationZ > -controllerrotationLimitZ)
            {
                float rotateZ = -primaryThumbstick.x * vrControllerVelocity * Time.deltaTime;
                weclemansController.Rotate(0, 0, rotateZ);  // Rotate forward
                currentControllerRotationZ -= rotateZ;  // Update current rotation (more negative)
                inputDetected = true;
            }
            // Handle controller rotation (Thumbstick right)
            if (primaryThumbstick.x > 0 && currentControllerRotationZ < controllerrotationLimitZ)
            {
                float rotateZ = -primaryThumbstick.x * vrControllerVelocity * Time.deltaTime;
                weclemansController.Rotate(0, 0, rotateZ);  // Rotate backward
                currentControllerRotationZ -= rotateZ;  // Update current rotation (more positive)
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
                weclemansYObject.Rotate(unrotateX, 0, 0);   // Rotating to reduce the rotation
                currentRotationX -= unrotateX;    // Reducing the positive rotation
            }
            else if (currentRotationX < 0)
            {
                float unrotateX = Mathf.Min(centeringVelocityX * Time.deltaTime, -currentRotationX);
                weclemansYObject.Rotate(-unrotateX, 0, 0);   // Rotating to reduce the rotation
                currentRotationX += unrotateX;    // Reducing the positive rotation
            }

            // Center Y-axis (left/right rotation with secondary thumbstick)
            if (currentRotationY > 0)
            {
                float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, currentRotationY);
                weclemansZObject.Rotate(0, unrotateY, 0);   // Rotating to reduce the rotation
                currentRotationY -= unrotateY;    // Reducing the positive rotation
            }
            else if (currentRotationY < 0)
            {
                float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, -currentRotationY);
                weclemansZObject.Rotate(0, -unrotateY, 0);   // Rotating to reduce the rotation
                currentRotationY += unrotateY;    // Reducing the positive rotation
            }

            // Center Z-axis (left/right rotation)
            if (currentRotationZ > 0)
            {
                float unrotateZ = Mathf.Min(centeringVelocityZ * Time.deltaTime, currentRotationZ);
                weclemansXObject.Rotate(0, 0, unrotateZ);   // Rotating to reduce the rotation
                currentRotationZ -= unrotateZ;    // Reducing the positive rotation
            }
            else if (currentRotationZ < 0)
            {
                float unrotateZ = Mathf.Min(centeringVelocityZ * Time.deltaTime, -currentRotationZ);
                weclemansXObject.Rotate(0, 0, -unrotateZ);   // Rotating to reduce the rotation
                currentRotationZ += unrotateZ;    // Reducing the positive rotation
            }

            //Centering for contoller
            // Center Y-axis Controller rotation
            if (currentControllerRotationY > 0)
            {
                float unrotateY = Mathf.Min(centeringControllerVelocityY * Time.deltaTime, currentControllerRotationY);
                weclemansController.Rotate(0, unrotateY, 0);   // Rotating to reduce the rotation
                currentControllerRotationY -= unrotateY;    // Reducing the positive rotation
            }
            else if (currentControllerRotationY < 0)
            {
                float unrotateY = Mathf.Min(centeringControllerVelocityY * Time.deltaTime, -currentControllerRotationY);
                weclemansController.Rotate(0, -unrotateY, 0);  // Rotating to reduce the rotation
                currentControllerRotationY += unrotateY;    // Reducing the negative rotation
            }

            // Center X-Axis Controller rotation
            if (currentControllerRotationX > 0)
            {
                float unrotateX = Mathf.Min(centeringControllerVelocityX * Time.deltaTime, currentControllerRotationX);
                weclemansController.Rotate(unrotateX, 0, 0);   // Rotating to reduce the rotation
                currentControllerRotationX -= unrotateX;    // Reducing the positive rotation
            }
            else if (currentControllerRotationX < 0)
            {
                float unrotateX = Mathf.Min(centeringControllerVelocityX * Time.deltaTime, -currentControllerRotationX);
                weclemansController.Rotate(-unrotateX, 0, 0);   // Rotating to reduce the rotation
                currentControllerRotationX += unrotateX;    // Reducing the positive rotation
            }

            // Center Z-axis Controller rotation
            if (currentControllerRotationZ > 0)
            {
                float unrotateZ = Mathf.Min(centeringControllerVelocityZ * Time.deltaTime, currentControllerRotationZ);
                weclemansController.Rotate(0, 0, unrotateZ);   // Rotating to reduce the rotation
                currentControllerRotationZ -= unrotateZ;    // Reducing the positive rotation
            }
            else if (currentControllerRotationZ < 0)
            {
                float unrotateZ = Mathf.Min(centeringControllerVelocityZ * Time.deltaTime, -currentControllerRotationZ);
                weclemansController.Rotate(0, 0, -unrotateZ);   // Rotating to reduce the rotation
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
