using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using EmuVR.InputManager;
using System.Collections;

namespace WIGUx.Modules.tbladeMotionSim
{
    public class tbladeMotionSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        private readonly float keyboardVelocityX = 25.5f;  // Velocity for keyboard input
        private readonly float keyboardVelocityY = 25.5f;  // Velocity for keyboard input
        private readonly float keyboardVelocityZ = 25.5f;  // Velocity for keyboard input
        private readonly float vrVelocity = 5.5f;        // Velocity for VR controller input

        private readonly float centeringVelocityX = 15.5f;  // Velocity for centering rotation
        private readonly float centeringVelocityY = 20.5f;  // Velocity for centering rotation
        private readonly float centeringVelocityZ = 15.5f;  // Velocity for centering rotation

        private float adjustSpeed = 1.0f;  // Adjust this adjustment speed as needed a lower number will lead to smaller adustments

        private float rotationLimitX = 0f;  // Rotation limit for X-axis
        private float rotationLimitY = 15f;  // Rotation limit for Y-axis
        private float rotationLimitZ = 0f;  // Rotation limit for Z-axis

        private float currentRotationX = 0f;  // Current rotation for X-axis
        private float currentRotationY = 0f;  // Current rotation for Y-axis
        private float currentRotationZ = 0f;  // Current rotation for Z-axis

        private Transform tbladeXObject; // Reference to the main X object
        private Transform tbladeYObject; // Reference to the main Y object
        private Transform tbladeZObject; // Reference to the main Z object
        private GameObject cockpitCam;    // Reference to the cockpit camera

        // Initial positions and rotations for resetting
        private Vector3 tbladeXStartPosition;
        private Quaternion tbladeXStartRotation;
        private Vector3 tbladeYStartPosition;
        private Quaternion tbladeYStartRotation;
        private Vector3 tbladeZStartPosition;
        private Quaternion tbladeZStartRotation;
        private Vector3 cockpitCamStartPosition;
        private Quaternion cockpitCamStartRotation;

        // Controller animation 
        // Speeds for the animation of the in game flight stick or wheel
        private readonly float keyboardControllerVelocityX = 35.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityY = 35.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityZ = 35.5f;  // Velocity for keyboard input
        private readonly float vrControllerVelocity = 35.5f;        // Velocity for VR controller input

        private float controllerrotationLimitX = 5f;  // Rotation limit for X-axis (stick or wheel)
        private float controllerrotationLimitY = 0f;  // Rotation limit for Y-axis (stick or wheel)
        private float controllerrotationLimitZ = 5f;  // Rotation limit for Z-axis (stick or wheel)

        private float currentControllerRotationX = 0f;  // Current rotation for X-axis (stick or wheel)
        private float currentControllerRotationY = 0f;  // Current rotation for Y-axis (stick or wheel)
        private float currentControllerRotationZ = 0f;  // Current rotation for Z-axis (stick or wheel)

        private readonly float centeringControllerVelocityX = 20.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float centeringControllerVelocityY = 20.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float centeringControllerVelocityZ = 20.5f;  // Velocity for centering rotation (stick or wheel)

        private Transform tbladeControllerX; // Reference to the main animated controller (wheel)
        private Vector3 tbladeControllerXStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion tbladeControllerXStartRotation; // Initial controlller positions and rotations for resetting
        private Transform tbladeControllerY; // Reference to the main animated controller (wheel)
        private Vector3 tbladeControllerYStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion tbladeControllerYStartRotation; // Initial controlller positions and rotations for resetting
        private Transform tbladeControllerZ; // Reference to the main animated controller (wheel)
        private Vector3 tbladeControllerZStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion tbladeControllerZStartRotation; // Initial controlller positions and rotations for resetting

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

        private Transform fireemissiveObject;
        private Transform fireemissive2Object;
        public string fire1Button = "Fire1"; // Name of the fire button
        public string fire2Button = "Fire2"; // Name of the fire button 
        public string fire3Button = "Fire3"; // Name of the fire button 
        public string JumpButton = "Jump"; // Name of the fire button 
        public Light fire1_light;
        public Light fire2_light;
        public float lightDuration = 0.35f; // Duration during which the lights will be on
        private bool inFocusMode = false;  // Flag to track focus mode state
        private Light[] lights;

        private readonly string[] compatibleGames = { "thndrbld.zip" };

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
            tbladeXObject = transform.Find("tbladeX");
            if (tbladeXObject != null)
            {
                logger.Info("tbladeX object found.");
                tbladeXStartPosition = tbladeXObject.position;
                tbladeXStartRotation = tbladeXObject.rotation;

                // Find tbladeY object under tbladeX
                tbladeYObject = tbladeXObject.Find("tbladeY");
                if (tbladeYObject != null)
                {
                    logger.Info("tbladeY object found.");
                    tbladeYStartPosition = tbladeYObject.position;
                    tbladeYStartRotation = tbladeYObject.rotation;

                    // Find tbladeZ object under tbladeY
                    tbladeZObject = tbladeYObject.Find("tbladeZ");
                    if (tbladeZObject != null)
                    {
                        logger.Info("tbladeZ object found.");
                        tbladeZStartPosition = tbladeZObject.position;
                        tbladeZStartRotation = tbladeZObject.rotation;

                        // Find tbladeControllerX under tbladeZ
                        tbladeControllerX = tbladeZObject.Find("tbladeControllerX");
                        if (tbladeControllerX != null)
                        {
                            logger.Info("tbladeControllerX object found.");
                            // Store initial position and rotation of the stick
                            tbladeControllerXStartPosition = tbladeControllerX.transform.position; // these could cause the controller to mess up
                            tbladeControllerXStartRotation = tbladeControllerX.transform.rotation;

                            // Find tbladeControllerY under tbladeControllerX
                            tbladeControllerY = tbladeControllerX.Find("tbladeControllerY");
                            if (tbladeControllerY != null)
                            {
                                logger.Info("tbladeControllerY object found.");
                                // Store initial position and rotation of the stick
                                tbladeControllerYStartPosition = tbladeControllerY.transform.position;
                                tbladeControllerYStartRotation = tbladeControllerY.transform.rotation;

                                // Find tbladeControllerZ under tbladeControllerY
                                tbladeControllerZ = tbladeControllerY.Find("tbladeControllerZ");
                                if (tbladeControllerZ != null)
                                {
                                    logger.Info("tbladeControllerZ object found.");
                                    // Store initial position and rotation of the stick
                                    tbladeControllerZStartPosition = tbladeControllerZ.transform.position;
                                    tbladeControllerZStartRotation = tbladeControllerZ.transform.rotation;
                                }
                                else
                                {
                                    logger.Error("tbladeControllerZ object not found under tbladeControllerY!");
                                }
                            }
                            else
                            {
                                logger.Error("tbladeControllerY object not found under tbladeControllerX!");
                            }
                        }
                        else
                        {
                            logger.Error("tbladeControllerX object not found under tbladeZ!");
                        }

                        // Find cockpit camera under cockpit
                        cockpitCam = tbladeZObject.Find("eyes/cockpitcam")?.gameObject;
                        if (cockpitCam != null)
                        {
                            logger.Info("Cockpitcam object found.");

                            // Store initial position and rotation of cockpit cam
                            cockpitCamStartPosition = cockpitCam.transform.position;
                            cockpitCamStartRotation = cockpitCam.transform.rotation;
                        }
                        else
                        {
                            logger.Error("Cockpitcam object not found under tbladeZ!");
                        }
                    }
                    else
                    {
                        logger.Error("tbladeZ object not found under tbladeY!");
                    }
                }

                else
                {
                    logger.Error("tbladeY object not found under tbladeX!");
                }
            }
            else
            {
                logger.Error("tbladeX object not found!");
            }

            // Find fireemissive object under gforce2Z
            fireemissiveObject = tbladeZObject.Find("fireemissive");
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
                logger.Debug("fireemissive object not found under tbladeZZ.");
            }

            // Find fireemissive2 object under tbladeZ2Z
            fireemissive2Object = tbladeZObject.Find("fireemissive2");
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
                logger.Debug("fireemissive2 object not found under tbladeZZ.");
            }

            // Find fire1_light object
            fire1_light = tbladeZObject.Find("fire1_light").GetComponent<Light>();
            if (fire1_light != null)
            {
                logger.Info("fire1_light object found.");
            }
            else
            {
                logger.Debug("fire1_light object not found.");
            }

            // Find fire2_light object
            fire2_light = tbladeZObject.Find("fire2_light").GetComponent<Light>();
            if (fire2_light != null)
            {
                logger.Info("fire2_light object found.");
            }
            else
            {
                logger.Debug("fire2_light object not found.");
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
                HandleInput(ref inputDetected);  // Pass by reference
            }
        }

        void StartFocusMode()
        {
            string controlledSystemGamePathString = GameSystem.ControlledSystem.Game.path != null ? GameSystem.ControlledSystem.Game.path.ToString() : null;
            logger.Info($"Controlled System Game path String: {controlledSystemGamePathString}");
            logger.Info("Compatible Rom Dectected, ... Rotors Spinning Up");
            logger.Info("Sega Thunder Blade Delxue Motion Sim starting...");
            logger.Info("Wait isn't that Blue Thunder?...");
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
                logger.Error("CockpitCam object not found under gforceZ!");
            }

            // Reset rotation allowances and current rotation values
            currentRotationX = 0f;
            currentRotationY = 0f;
            currentRotationZ = 0f;

            inFocusMode = true;  // Set focus mode flag
        }

        void EndFocusMode()
        {
            logger.Info("Exiting Focus Mode...");
            // Restore original parents of objects
            RestoreOriginalParent(playerCamera, "PlayerCamera");
            RestoreOriginalParent(playerVRSetup, "PlayerVRSetup.PlayerRig");

            // Reset tbladeX to initial positions and rotations
            if (tbladeXObject != null)
            {
                tbladeXObject.position = tbladeXStartPosition;
                tbladeXObject.rotation = tbladeXStartRotation;
            }

            // Reset tbladeY object to initial position and rotation
            if (tbladeYObject != null)
            {
                tbladeYObject.position = tbladeYStartPosition;
                tbladeYObject.rotation = tbladeYStartRotation;
            }
            // Reset tbladeZ object to initial position and rotation
            if (tbladeZObject != null)
            {
                tbladeZObject.position = tbladeZStartPosition;
                tbladeZObject.rotation = tbladeZStartRotation;
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

            logger.Info("Resetting Positions");
            ResetPositions();

            inFocusMode = false;  // Clear focus mode flag
        }

        void HandleInput(ref bool inputDetected)
        {
            if (!inFocusMode) return;

            Vector2 primaryThumbstick = Vector2.zero;
            Vector2 secondaryThumbstick = Vector2.zero;

            //maybe add a check for xinput? not right now.
            if (XInput.IsConnected)
            {
                primaryThumbstick = XInput.Get(XInput.Axis.LThumbstick);
            }

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

            // Fire1
            if (Input.GetButtonDown("Fire1"))
            {
                ToggleFireEmissive1(true);
                ToggleLight1(true);
                inputDetected = true;
            }

            // Reset position on button release
            if (Input.GetButtonUp("Fire1"))
            {
                ToggleFireEmissive1(false);
                ToggleLight1(false);
                inputDetected = true;
            }

            // Fire2
            if (Input.GetButtonDown("Fire2"))
            {
                ToggleFireEmissive2(true);
                ToggleLight2(true);
                inputDetected = true;
            }

            // Reset position on button release
            if (Input.GetButtonUp("Fire2"))
            {
                ToggleFireEmissive2(false);
                ToggleLight2(false);
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

            // Thumbstick direction: down
            if (Input.GetKey(KeyCode.DownArrow) || primaryThumbstick.y > 0 || XInput.Get(XInput.Button.DpadDown))
            {
                if (currentControllerRotationZ < controllerrotationLimitZ)
                {
                    float controllerRotateZ = (Input.GetKey(KeyCode.DownArrow) || XInput.Get(XInput.Button.DpadDown) ? keyboardControllerVelocityZ : primaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                    tbladeControllerZ.Rotate(0, 0, -controllerRotateZ); // Maintain original direction
                    currentControllerRotationZ += controllerRotateZ;
                    inputDetected = true;
                }
            }

            // Thumbstick direction: up
            if (Input.GetKey(KeyCode.UpArrow) || primaryThumbstick.y < 0 || XInput.Get(XInput.Button.DpadUp))
            {
                if (currentControllerRotationZ > -controllerrotationLimitZ)
                {
                    float controllerRotateZ = (Input.GetKey(KeyCode.UpArrow) || XInput.Get(XInput.Button.DpadUp) ? keyboardControllerVelocityZ : -primaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                    tbladeControllerZ.Rotate(0, 0, controllerRotateZ); // Maintain original direction
                    currentControllerRotationZ -= controllerRotateZ;
                    inputDetected = true;
                }
            }

            // Handle Z rotation for tbladeXObject and tbladeControllerZ (Left Arrow or primaryThumbstick.x < 0)
            // Thumbstick direction: left
            if (Input.GetKey(KeyCode.LeftArrow) || primaryThumbstick.x < 0 || XInput.Get(XInput.Button.DpadLeft))
            {
                if (primaryThumbstick.x < 0 && currentRotationY < rotationLimitY)
                {
                    float rotateY = (Input.GetKey(KeyCode.LeftArrow) || XInput.Get(XInput.Button.DpadLeft) ? keyboardVelocityZ : -primaryThumbstick.x * vrVelocity) * Time.deltaTime;
                    tbladeYObject.Rotate(0, -rotateY, 0);  
                    currentRotationY += rotateY;  
                    inputDetected = true;
                }
                if (currentControllerRotationX > -controllerrotationLimitX)
                {
                    float controllerRotateX = (Input.GetKey(KeyCode.LeftArrow) || XInput.Get(XInput.Button.DpadLeft) ? keyboardControllerVelocityX : -primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                    tbladeControllerX.Rotate(controllerRotateX, 0, 0);
                    currentControllerRotationX -= controllerRotateX;
                    inputDetected = true;
                }
            }

            // Handle Z rotation for tbladeXObject and tbladeControllerZ (Right Arrow or primaryThumbstick.x > 0)
            // Thumbstick direction: right
            if (Input.GetKey(KeyCode.RightArrow) || primaryThumbstick.x > 0 || XInput.Get(XInput.Button.DpadRight))
            {
                if (primaryThumbstick.x > 0 && currentRotationY > -rotationLimitY)
                {
                    float rotateY = (Input.GetKey(KeyCode.RightArrow) || XInput.Get(XInput.Button.DpadRight) ? keyboardVelocityZ : primaryThumbstick.x * vrVelocity) * Time.deltaTime;
                    tbladeYObject.Rotate(0, rotateY, 0);  
                    currentRotationY -= rotateY;  
                    inputDetected = true;
                }
                if (currentControllerRotationX < controllerrotationLimitX)
                {
                    float controllerRotateX = (Input.GetKey(KeyCode.RightArrow) || XInput.Get(XInput.Button.DpadRight) ? keyboardControllerVelocityX : primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                    tbladeControllerX.Rotate(-controllerRotateX, 0, 0);
                    currentControllerRotationX += controllerRotateX;
                    inputDetected = true;
                }

            }

            /*
            // Handle left rotation (Thumbstick left)
            if (secondaryThumbstick.x < 0 && currentRotationY > -rotationLimitY)
            {
                float rotateY = secondaryThumbstick.x * vrVelocity * Time.deltaTime;
                tbladeYObject.Rotate(0, rotateY, 0);  // Rotate Y
                currentRotationY += rotateY;  // Update current rotation (more positive)
                inputDetected = true;
            }

            // Handle right rotation (Thumbstick right)
            if (secondaryThumbstick.x > 0 && currentRotationY < rotationLimitY)
            {
                float rotateY = secondaryThumbstick.x * vrVelocity * Time.deltaTime;
                tbladeYObject.Rotate(0, rotateY, 0);  // Rotate Y
                currentRotationY += rotateY;  // Update current rotation (more negative)
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
        }

        void CenterRotation()
        {
            // Center X-axis
            if (currentRotationX > 0)
            {
                float unrotateX = Mathf.Min(centeringVelocityX * Time.deltaTime, currentRotationX);
                tbladeYObject.Rotate(unrotateX, 0, 0);
                currentRotationX -= unrotateX;
            }
            else if (currentRotationX < 0)
            {
                float unrotateX = Mathf.Min(centeringVelocityX * Time.deltaTime, -currentRotationX);
                tbladeYObject.Rotate(-unrotateX, 0, 0);
                currentRotationX += unrotateX;
            }

            // Center Y-axis
            if (currentRotationY > 0)
            {
                float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, currentRotationY);
                tbladeXObject.Rotate(0, unrotateY, 0);
                currentRotationY -= unrotateY;
            }
            else if (currentRotationY < 0)
            {
                float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, -currentRotationY);
                tbladeXObject.Rotate(0, -unrotateY, 0);
                currentRotationY += unrotateY;
            }

            // Center Z-axis
            if (currentRotationZ > 0)
            {
                float unrotateZ = Mathf.Min(centeringVelocityZ * Time.deltaTime, currentRotationZ);
                tbladeXObject.Rotate(0, 0, unrotateZ);
                currentRotationZ -= unrotateZ;
            }
            else if (currentRotationZ < 0)
            {
                float unrotateZ = Mathf.Min(centeringVelocityZ * Time.deltaTime, -currentRotationZ);
                tbladeXObject.Rotate(0, 0, -unrotateZ);
                currentRotationZ += unrotateZ;
            }
            //Centering for contoller

            // Center Y-axis Controller rotation
            if (currentControllerRotationY > 0)
            {
                float unrotateY = Mathf.Min(centeringControllerVelocityY * Time.deltaTime, currentControllerRotationY);
                tbladeControllerY.Rotate(0, unrotateY, 0);   // Rotating to reduce the rotation
                currentControllerRotationY -= unrotateY;    // Reducing the positive rotation
            }
            else if (currentControllerRotationY < 0)
            {
                float unrotateY = Mathf.Min(centeringControllerVelocityY * Time.deltaTime, -currentControllerRotationY);
                tbladeControllerY.Rotate(0, -unrotateY, 0);  // Rotating to reduce the rotation
                currentControllerRotationY += unrotateY;    // Reducing the negative rotation
            }

            // Center X-Axis Controller rotation
            if (currentControllerRotationX > 0)
            {
                float unrotateX = Mathf.Min(centeringControllerVelocityX * Time.deltaTime, currentControllerRotationX);
                tbladeControllerX.Rotate(unrotateX, 0, 0);   // Rotating to reduce the rotation
                currentControllerRotationX -= unrotateX;    // Reducing the positive rotation
            }
            else if (currentControllerRotationX < 0)
            {
                float unrotateX = Mathf.Min(centeringControllerVelocityX * Time.deltaTime, -currentControllerRotationX);
                tbladeControllerX.Rotate(-unrotateX, 0, 0);   // Rotating to reduce the rotation
                currentControllerRotationX += unrotateX;    // Reducing the positive rotation
            }

            // Center Z-axis Controller rotation
            if (currentControllerRotationZ > 0)
            {
                float unrotateZ = Mathf.Min(centeringControllerVelocityZ * Time.deltaTime, currentControllerRotationZ);
                tbladeControllerZ.Rotate(0, 0, unrotateZ);   // Rotating to reduce the rotation
                currentControllerRotationZ -= unrotateZ;    // Reducing the positive rotation
            }
            else if (currentControllerRotationZ < 0)
            {
                float unrotateZ = Mathf.Min(centeringControllerVelocityZ * Time.deltaTime, -currentControllerRotationZ);
                tbladeControllerZ.Rotate(0, 0, -unrotateZ);   // Rotating to reduce the rotation
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
         //           logger.Info($"fireemissive object emission turned {(isActive ? "on" : "off")}.");
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
             //       logger.Info($"fireemissive2 object emission turned {(isActive ? "on" : "off")}.");
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
      //          logger.Info($"{fire1_light.name} light turned {(isActive ? "on" : "off")}.");
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
         //       logger.Info($"{fire2_light.name} light turned {(isActive ? "on" : "off")}.");
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
