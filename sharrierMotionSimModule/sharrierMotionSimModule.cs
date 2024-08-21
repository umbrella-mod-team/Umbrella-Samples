using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using EmuVR.InputManager;
using System.Collections;

namespace WIGUx.Modules.sharrierMotionSim
{
    public class sharrierMotionSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        private readonly float keyboardVelocityX = 35.5f;  // Velocity for keyboard input
        private readonly float keyboardVelocityY = 30.5f;  // Velocity for keyboard input
        private readonly float keyboardVelocityZ = 30.5f;  // Velocity for keyboard input
        private readonly float vrVelocity = 30.5f;        // Velocity for VR controller input

        private readonly float centeringVelocityX = 20.5f;  // Velocity for centering rotation
        private readonly float centeringVelocityY = 20.5f;  // Velocity for centering rotation
        private readonly float centeringVelocityZ = 20.5f;  // Velocity for centering rotation

        private float adjustSpeed = 1.0f;  // Adjust this adjustment speed as needed a lower number will lead to smaller adustments

        private float rotationLimitX = 10f;  // Rotation limit for X-axis
        private float rotationLimitY = 10f;  // Rotation limit for Y-axis
        private float rotationLimitZ = 10f;  // Rotation limit for Z-axis

        private float currentRotationX = 0f;  // Current rotation for X-axis
        private float currentRotationY = 0f;  // Current rotation for Y-axis
        private float currentRotationZ = 0f;  // Current rotation for Z-axis

        private Transform sharrierXObject; // Reference to the main X object
        private Transform sharrierYObject; // Reference to the main Y object
        private Transform sharrierZObject; // Reference to the main Z object
        private GameObject cockpitCam;    // Reference to the cockpit camera

        // Initial positions and rotations for resetting
        private Vector3 sharrierXStartPosition;
        private Quaternion sharrierXStartRotation;
        private Vector3 sharrierYStartPosition;
        private Quaternion sharrierYStartRotation;
        private Vector3 sharrierZStartPosition;
        private Quaternion sharrierZStartRotation;
        private Vector3 cockpitCamStartPosition;
        private Quaternion cockpitCamStartRotation;

        // Controller animation 
        // Speeds for the animation of the in game flight stick or wheel
        private readonly float keyboardControllerVelocityX = 50.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityY = 50.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityZ = 50.5f;  // Velocity for keyboard input
        private readonly float vrControllerVelocity = 100.5f;        // Velocity for VR controller input

        private float controllerRotationLimitX = 10f;  // Rotation limit for X-axis (stick or wheel)
        private float controllerRotationLimitY = 0f;  // Rotation limit for Y-axis (stick or wheel)
        private float controllerRotationLimitZ = 10f;  // Rotation limit for Z-axis (stick or wheel)

        private float currentControllerRotationX = 0f;  // Current rotation for X-axis (stick or wheel)
        private float currentControllerRotationY = 0f;  // Current rotation for Y-axis (stick or wheel)
        private float currentControllerRotationZ = 0f;  // Current rotation for Z-axis (stick or wheel)

        private readonly float centeringControllerVelocityX = 50.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float centeringControllerVelocityY = 50.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float centeringControllerVelocityZ = 50.5f;  // Velocity for centering rotation (stick or wheel)

        private Transform sharrierControllerX; // Reference to the main animated controller (wheel)
        private Vector3 sharrierControllerXStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion sharrierControllerXStartRotation; // Initial controlller positions and rotations for resetting
        private Transform sharrierControllerY; // Reference to the main animated controller (wheel)
        private Vector3 sharrierControllerYStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion sharrierControllerYStartRotation; // Initial controlller positions and rotations for resetting
        private Transform sharrierControllerZ; // Reference to the main animated controller (wheel)
        private Vector3 sharrierControllerZStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion sharrierControllerZStartRotation; // Initial controlller positions and rotations for resetting

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
        private Transform fireemissiveObject;
        public Light fire1_light;
        public Light fire2_light;
        public string fireButton = "Fire1"; // Name of the fire button (you can change it according to your needs)
        public string missileButton = "Fire2"; // Name of the fire button (you can change it according to your needs)
        public float lightDuration = 0.35f; // Duration during which the lights will be on
        private Light[] lights;

        private bool inFocusMode = false;  // Flag to track focus mode state

        private readonly string[] compatibleGames = { "sharrier.zip", "sharrierdx.zip" };

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

            // Find sharrierX object in hierarchy
            sharrierXObject = transform.Find("sharrierX");
            if (sharrierXObject != null)
            {
                logger.Info("sharrierX object found.");
                sharrierXStartPosition = sharrierXObject.position;
                sharrierXStartRotation = sharrierXObject.rotation;

                // Gets all Light components in the target object and its children
                Light[] allLights = sharrierXObject.GetComponentsInChildren<Light>();

                // Initialize a new list to store filtered lights
                List<Light> filteredLights = new List<Light>();

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

                // Store the filtered lights
                lights = filteredLights.ToArray();

                // Find sharrierY object under sharrierX
                sharrierYObject = sharrierXObject.Find("sharrierY");
                if (sharrierYObject != null)
                {
                    logger.Info("sharrierY object found.");
                    sharrierYStartPosition = sharrierYObject.position;
                    sharrierYStartRotation = sharrierYObject.rotation;

                    // Find sharrierZ object under sharrierY
                    sharrierZObject = sharrierYObject.Find("sharrierZ");
                    if (sharrierZObject != null)
                    {
                        logger.Info("sharrierZ object found.");
                        sharrierZStartPosition = sharrierZObject.position;
                        sharrierZStartRotation = sharrierZObject.rotation;

                        // Find fireemissive object under sharrierX
                        fireemissiveObject = sharrierZObject.Find("fireemissive");
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
                            logger.Debug("fireemissive object not found under sharrierZ.");
                        }

                        // Find sharrierControllerX under sharrierZ
                        sharrierControllerX = sharrierZObject.Find("sharrierControllerX");
                        if (sharrierControllerX != null)
                        {
                            logger.Info("sharrierControllerX object found.");
                            // Store initial position and rotation of the stick
                            sharrierControllerXStartPosition = sharrierControllerX.transform.position; // these could cause the controller to mess up
                            sharrierControllerXStartRotation = sharrierControllerX.transform.rotation;

                            // Find sharrierControllerY under sharrierControllerX
                            sharrierControllerY = sharrierControllerX.Find("sharrierControllerY");
                            if (sharrierControllerY != null)
                            {
                                logger.Info("sharrierControllerY object found.");
                                // Store initial position and rotation of the stick
                                sharrierControllerYStartPosition = sharrierControllerY.transform.position;
                                sharrierControllerYStartRotation = sharrierControllerY.transform.rotation;

                                // Find sharrierControllerZ under sharrierControllerY
                                sharrierControllerZ = sharrierControllerY.Find("sharrierControllerZ");
                                if (sharrierControllerZ != null)
                                {
                                    logger.Info("sharrierControllerZ object found.");
                                    // Store initial position and rotation of the stick
                                    sharrierControllerZStartPosition = sharrierControllerZ.transform.position;
                                    sharrierControllerZStartRotation = sharrierControllerZ.transform.rotation;
                                }
                                else
                                {
                                    logger.Error("sharrierControllerZ object not found under sharrierControllerY!");
                                }
                            }
                            else
                            {
                                logger.Error("sharrierControllerY object not found under sharrierControllerX!");
                            }
                        }
                        else
                        {
                            logger.Error("sharrierControllerX object not found under sharrierZ!");
                        }

                        // Find cockpit camera under sharrierZ
                        cockpitCam = sharrierZObject.Find("eyes/cockpitcam")?.gameObject;
                        if (cockpitCam != null)
                        {
                            logger.Info("Cockpitcam object found.");

                            // Store initial position and rotation of cockpit cam
                            cockpitCamStartPosition = cockpitCam.transform.position;
                            cockpitCamStartRotation = cockpitCam.transform.rotation;
                        }
                        else
                        {
                            logger.Error("Cockpitcam object not found under sharrierZ!");
                        }
                    }
                    else
                    {
                        logger.Error("sharrierZ object not found under sharrierY!");
                    }
                }
                else
                {
                    logger.Error("sharrierY object not found under sharrierX!");
                }
            }
            else
            {
                logger.Error("sharrierX object not found!");
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

                if (Input.GetButtonDown(fireButton)) // Checks if the fire button has been pressed
                {
                    // Starts the coroutine to turn on the lights for a brief period
                    ToggleFireEmissive(true);
                    ToggleLights(true);
                }

                if (Input.GetButtonUp(fireButton) || Input.GetKey(KeyCode.X)) // Checks if the missile button has been pressed
                {
                    // Starts the coroutine to turn on the lights for a brief period
                    ToggleFireEmissive(false);
                    ToggleLights(false);
                }
            }
        }

        void StartFocusMode()
        {
            string controlledSystemGamePathString = GameSystem.ControlledSystem.Game.path != null ? GameSystem.ControlledSystem.Game.path.ToString() : null;
            logger.Info($"Controlled System Game path String: {controlledSystemGamePathString}");
            logger.Info("Compatible Rom Dectected, Welcome To The Fantasy Zone...");
            logger.Info("Sega Space Harrier DX Motion Sim starting...");
            logger.Info("Get Ready!!..");

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
                logger.Error("CockpitCam object not found under Chair!");
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

            // Reset sharrierX to initial positions and rotations
            if (sharrierXObject != null)
            {
                sharrierXObject.position = sharrierXStartPosition;
                sharrierXObject.rotation = sharrierXStartRotation;
            }

            // Reset sharrierY object to initial position and rotation
            if (sharrierYObject != null)
            {
                sharrierYObject.position = sharrierYStartPosition;
                sharrierYObject.rotation = sharrierYStartRotation;
            }
            // Reset sharrierZ object to initial position and rotation
            if (sharrierZObject != null)
            {
                sharrierZObject.position = sharrierZStartPosition;
                sharrierZObject.rotation = sharrierZStartRotation;
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

            // Handle keyboard input for pitch and roll
            if (Input.GetKey(KeyCode.LeftArrow) && currentRotationX > -rotationLimitX)
            {
                float rotateX = keyboardVelocityX * Time.deltaTime;
                sharrierYObject.Rotate(rotateX, 0, 0);
                currentRotationX -= rotateX;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.RightArrow) && currentRotationX < rotationLimitX)
            {
                float rotateX = keyboardVelocityX * Time.deltaTime;
                sharrierYObject.Rotate(-rotateX, 0, 0);
                currentRotationX += rotateX;
                inputDetected = true;
            }
            /*
            if (Input.GetKey(KeyCode.RightArrow) && currentRotationY > -rotationLimitY)
            {
                float rotateY = keyboardVelocityY * Time.deltaTime;
                sharrierYObject.Rotate(0, rotateY, 0);
                currentRotationY -= rotateY;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.LeftArrow) && currentRotationY < rotationLimitY)
            {
                float rotateY = keyboardVelocityY * Time.deltaTime;
                sharrierYObject.Rotate(0, -rotateY, 0);
                currentRotationY += rotateY;
                inputDetected = true;
            }
            */
            if (Input.GetKey(KeyCode.DownArrow) && currentRotationZ > -rotationLimitZ)
            {
                float rotateZ = keyboardVelocityZ * Time.deltaTime;
                sharrierXObject.Rotate(0, 0, rotateZ);
                currentRotationZ -= rotateZ;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.UpArrow) && currentRotationZ < rotationLimitZ)
            {
                float rotateZ = keyboardVelocityZ * Time.deltaTime;
                sharrierXObject.Rotate(0, 0, -rotateZ);
                currentRotationZ += rotateZ;
                inputDetected = true;
            }

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

            // Handle left rotation (Thumbstick left)
            if (primaryThumbstick.x < 0)
            {
                // Rotate sharrierYObject
                if (currentRotationX > -rotationLimitX)
                {
                    float rotateX = primaryThumbstick.x * vrVelocity * Time.deltaTime;
                    sharrierYObject.Rotate(-rotateX, 0, 0);  // Rotate left
                    currentRotationX += rotateX;  // Update current rotation (more negative)
                    inputDetected = true;
                }

                // Rotate controller
                if (currentControllerRotationX > -controllerRotationLimitX)
                {
                    float controllerRotateX = primaryThumbstick.x * vrControllerVelocity * Time.deltaTime;
                    sharrierControllerX.Rotate(-controllerRotateX, 0, 0);  // Rotate left
                    currentControllerRotationX += controllerRotateX;  // Update current rotation (more negative)
                    inputDetected = true;
                }
            }

            // Handle right rotation (Thumbstick right)
            if (primaryThumbstick.x > 0)
            {
                // Rotate sharrierYObject
                if (currentRotationX < rotationLimitX)
                {
                    float rotateX = primaryThumbstick.x * vrVelocity * Time.deltaTime;
                    sharrierYObject.Rotate(-rotateX, 0, 0);  // Rotate right
                    currentRotationX += rotateX;  // Update current rotation (more positive)
                    inputDetected = true;
                }

                // Rotate controller
                if (currentControllerRotationX < controllerRotationLimitX)
                {
                    float controllerRotateX = primaryThumbstick.x * vrControllerVelocity * Time.deltaTime;
                    sharrierControllerX.Rotate(-controllerRotateX, 0, 0);  // Rotate right
                    currentControllerRotationX += controllerRotateX;  // Update current rotation (more positive)
                    inputDetected = true;
                }
            }

            // Handle forward rotation (Thumbstick down)
            if (primaryThumbstick.y < 0)
            {
                // Rotate sharrierXObject
                if (currentRotationZ > -rotationLimitZ)
                {
                    float rotateZ = -primaryThumbstick.y * vrVelocity * Time.deltaTime;
                    sharrierXObject.Rotate(0, 0, rotateZ);  // Rotate forward
                    currentRotationZ -= rotateZ;  // Update current rotation (more negative)
                    inputDetected = true;
                }

                // Rotate sharrierControllerZ
                if (currentControllerRotationZ > -controllerRotationLimitZ)
                {
                    float controllerRotateZ = -primaryThumbstick.y * vrControllerVelocity * Time.deltaTime;
                    sharrierControllerZ.Rotate(0, 0, controllerRotateZ);  // Rotate forward
                    currentControllerRotationZ -= controllerRotateZ;  // Update current rotation (more negative)
                    inputDetected = true;
                }
            }

            // Handle backward rotation (Thumbstick up)
            if (primaryThumbstick.y > 0)
            {
                // Rotate sharrierXObject
                if (currentRotationZ < rotationLimitZ)
                {
                    float rotateZ = -primaryThumbstick.y * vrVelocity * Time.deltaTime;
                    sharrierXObject.Rotate(0, 0, rotateZ);  // Rotate backward
                    currentRotationZ -= rotateZ;  // Update current rotation (more positive)
                    inputDetected = true;
                }

                // Rotate sharrierControllerZ
                if (currentControllerRotationZ < controllerRotationLimitZ)
                {
                    float controllerRotateZ = -primaryThumbstick.y * vrControllerVelocity * Time.deltaTime;
                    sharrierControllerZ.Rotate(0, 0, controllerRotateZ);  // Rotate backward
                    currentControllerRotationZ -= controllerRotateZ;  // Update current rotation (more positive)
                    inputDetected = true;
                }
            }

            /*
            // Handle forward rotation (Thumbstick up)
            if (secondaryThumbstick.y > 0 && currentRotationY < rotationLimitY)
            {
                float rotateY = secondaryThumbstick.y * vrVelocity * Time.deltaTime;
                sharrierYObject.Rotate(0, -rotateY, 0);  // Rotate Y
                currentRotationY += rotateY;  // Update current rotation (more negative)
                inputDetected = true;
            }

            // Handle backward rotation (Thumbstick down)
            if (secondaryThumbstick.y < 0 && currentRotationY > -rotationLimitY)
            {
                float rotateY = secondaryThumbstick.y * vrVelocity * Time.deltaTime;
                sharrierYObject.Rotate(0, -rotateY, 0);  // Rotate Y
                currentRotationY += rotateY;  // Update current rotation (more positive)
                inputDetected = true;
            }
            // Handle left rotation (Thumbstick left)
            if (secondaryThumbstick.x < 0 && currentRotationY > -rotationLimitY)
            {
                float rotateY = secondaryThumbstick.x * vrVelocity * Time.deltaTime;
                sharrierYObject.Rotate(0, rotateY, 0);  // Rotate Y
                currentRotationY += rotateY;  // Update current rotation (more positive)
                inputDetected = true;
            }

            // Handle right rotation (Thumbstick right)
            if (secondaryThumbstick.x > 0 && currentRotationY < rotationLimitY)
            {
                float rotateY = secondaryThumbstick.x * vrVelocity * Time.deltaTime;
                sharrierYObject.Rotate(0, rotateY, 0);  // Rotate Y
                currentRotationY += rotateY;  // Update current rotation (more negative)
                inputDetected = true;
            }
            */

            // Center the rotation if no input is detected (i think this is redundant)

            if (!inputDetected)
            {
                // CenterRotation();
            }

            // Print primary thumbstick input
            //     Debug.Log($"Primary Thumbstick - X: {primaryThumbstick.x}, Y: {primaryThumbstick.y}");

            // Print secondary thumbstick input
            //     Debug.Log($"Secondary Thumbstick - X: {secondaryThumbstick.x}, Y: {secondaryThumbstick.y}");

            // Check if secondary thumbstick input is needed for additional controls
            if (secondaryThumbstick.x != 0f || secondaryThumbstick.y != 0f)
            {
                // Implement additional logic here if needed

                // Print additional control values for debugging
                //    Debug.Log($"Secondary Thumbstick input detected - X: {secondaryThumbstick.x}, Y: {secondaryThumbstick.y}");
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
                sharrierYObject.Rotate(unrotateX, 0, 0);
                currentRotationX -= unrotateX;
            }
            else if (currentRotationX < 0)
            {
                float unrotateX = Mathf.Min(centeringVelocityX * Time.deltaTime, -currentRotationX);
                sharrierYObject.Rotate(-unrotateX, 0, 0);
                currentRotationX += unrotateX;
            }

            // Center Y-axis
            if (currentRotationY > 0)
            {
                float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, currentRotationY);
                sharrierXObject.Rotate(0, unrotateY, 0);
                currentRotationY -= unrotateY;
            }
            else if (currentRotationY < 0)
            {
                float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, -currentRotationY);
                sharrierXObject.Rotate(0, -unrotateY, 0);
                currentRotationY += unrotateY;
            }

            // Center Z-axis
            if (currentRotationZ > 0)
            {
                float unrotateZ = Mathf.Min(centeringVelocityZ * Time.deltaTime, currentRotationZ);
                sharrierXObject.Rotate(0, 0, unrotateZ);
                currentRotationZ -= unrotateZ;
            }
            else if (currentRotationZ < 0)
            {
                float unrotateZ = Mathf.Min(centeringVelocityZ * Time.deltaTime, -currentRotationZ);
                sharrierXObject.Rotate(0, 0, -unrotateZ);
                currentRotationZ += unrotateZ;
            }
            //Centering for contoller

            // Center Y-axis Controller rotation
            if (currentControllerRotationY > 0)
            {
                float unrotateY = Mathf.Min(centeringControllerVelocityY * Time.deltaTime, currentControllerRotationY);
                sharrierControllerY.Rotate(0, unrotateY, 0);   // Rotating to reduce the rotation
                currentControllerRotationY -= unrotateY;    // Reducing the positive rotation
            }
            else if (currentControllerRotationY < 0)
            {
                float unrotateY = Mathf.Min(centeringControllerVelocityY * Time.deltaTime, -currentControllerRotationY);
                sharrierControllerY.Rotate(0, -unrotateY, 0);  // Rotating to reduce the rotation
                currentControllerRotationY += unrotateY;    // Reducing the negative rotation
            }

            // Center X-Axis Controller rotation
            if (currentControllerRotationX > 0)
            {
                float unrotateX = Mathf.Min(centeringControllerVelocityX * Time.deltaTime, currentControllerRotationX);
                sharrierControllerX.Rotate(unrotateX, 0, 0);   // Rotating to reduce the rotation
                currentControllerRotationX -= unrotateX;    // Reducing the positive rotation
            }
            else if (currentControllerRotationX < 0)
            {
                float unrotateX = Mathf.Min(centeringControllerVelocityX * Time.deltaTime, -currentControllerRotationX);
                sharrierControllerX.Rotate(-unrotateX, 0, 0);   // Rotating to reduce the rotation
                currentControllerRotationX += unrotateX;    // Reducing the positive rotation
            }

            // Center Z-axis Controller rotation
            if (currentControllerRotationZ > 0)
            {
                float unrotateZ = Mathf.Min(centeringControllerVelocityZ * Time.deltaTime, currentControllerRotationZ);
                sharrierControllerZ.Rotate(0, 0, unrotateZ);   // Rotating to reduce the rotation
                currentControllerRotationZ -= unrotateZ;    // Reducing the positive rotation
            }
            else if (currentControllerRotationZ < 0)
            {
                float unrotateZ = Mathf.Min(centeringControllerVelocityZ * Time.deltaTime, -currentControllerRotationZ);
                sharrierControllerZ.Rotate(0, 0, -unrotateZ);   // Rotating to reduce the rotation
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
