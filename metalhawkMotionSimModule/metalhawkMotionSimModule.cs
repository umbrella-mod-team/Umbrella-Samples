using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using EmuVR.InputManager;
using System.Collections;

namespace WIGUx.Modules.metalhawkMotionSim
{
    public class metalhawkMotionSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        private readonly float keyboardVelocityX = 30.5f;  // Velocity for keyboard input
        private readonly float keyboardVelocityY = 30.5f;  // Velocity for keyboard input
        private readonly float keyboardVelocityZ = 30.5f;  // Velocity for keyboard input
        private readonly float vrVelocity = 30.5f;        // Velocity for VR controller input

        private readonly float centeringVelocityX = 15.5f;  // Velocity for centering rotation
        private readonly float centeringVelocityY = 15.5f;  // Velocity for centering rotation
        private readonly float centeringVelocityZ = 15.5f;  // Velocity for centering rotation

        private float adjustSpeed = 1.0f;  // Adjust this adjustment speed as needed a lower number will lead to smaller adustments

        private float rotationLimitX = 15f;  // Rotation limit for X-axis
        private float rotationLimitY = 0f;  // Rotation limit for Y-axis
        private float rotationLimitZ = 15f;  // Rotation limit for Z-axis

        private float currentRotationX = 0f;  // Current rotation for X-axis
        private float currentRotationY = 0f;  // Current rotation for Y-axis
        private float currentRotationZ = 0f;  // Current rotation for Z-axis

        private Transform metalhawkXObject; // Reference to the main X object
        private Transform metalhawkYObject; // Reference to the main Y object
        private Transform metalhawkZObject; // Reference to the main Z object
        private GameObject cockpitCam;    // Reference to the cockpit camera

        // Initial positions and rotations for resetting
        private Vector3 metalhawkXStartPosition;
        private Quaternion metalhawkXStartRotation;
        private Vector3 metalhawkYStartPosition;
        private Quaternion metalhawkYStartRotation;
        private Vector3 metalhawkZStartPosition;
        private Quaternion metalhawkZStartRotation;
        private Vector3 cockpitCamStartPosition;
        private Quaternion cockpitCamStartRotation;

        // Controller animation 
        // Speeds for the animation of the in game flight stick or wheel
        private readonly float keyboardControllerVelocityX = 100.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityY = 100.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityZ = 100.5f;  // Velocity for keyboard input
        private readonly float vrControllerVelocity = 100.5f;        // Velocity for VR controller input

        private float controllerrotationLimitX = 10f;  // Rotation limit for X-axis (stick or wheel)
        private float controllerrotationLimitY = 0f;  // Rotation limit for Y-axis (stick or wheel)
        private float controllerrotationLimitZ = 10f;  // Rotation limit for Z-axis (stick or wheel)

        private float currentControllerRotationX = 0f;  // Current rotation for X-axis (stick or wheel)
        private float currentControllerRotationY = 0f;  // Current rotation for Y-axis (stick or wheel)
        private float currentControllerRotationZ = 0f;  // Current rotation for Z-axis (stick or wheel)

        private readonly float centeringControllerVelocityX = 50.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float centeringControllerVelocityY = 50.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float centeringControllerVelocityZ = 50.5f;  // Velocity for centering rotation (stick or wheel)

        private Transform metalhawkControllerX; // Reference to the main animated controller (wheel)
        private Vector3 metalhawkControllerXStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion metalhawkControllerXStartRotation; // Initial controlller positions and rotations for resetting
        private Transform metalhawkControllerY; // Reference to the main animated controller (wheel)
        private Vector3 metalhawkControllerYStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion metalhawkControllerYStartRotation; // Initial controlller positions and rotations for resetting
        private Transform metalhawkControllerZ; // Reference to the main animated controller (wheel)
        private Vector3 metalhawkControllerZStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion metalhawkControllerZStartRotation; // Initial controlller positions and rotations for resetting

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

        private readonly string[] compatibleGames = { "metlhawk.zip" };

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
            metalhawkXObject = transform.Find("metalhawkX");
            if (metalhawkXObject != null)
            {
                logger.Info("metalhawkX object found.");
                metalhawkXStartPosition = metalhawkXObject.position;
                metalhawkXStartRotation = metalhawkXObject.rotation;

                // Find metalhawkY object under metalhawkX
                metalhawkYObject = metalhawkXObject.Find("metalhawkY");
                if (metalhawkYObject != null)
                {
                    logger.Info("metalhawkY object found.");
                    metalhawkYStartPosition = metalhawkYObject.position;
                    metalhawkYStartRotation = metalhawkYObject.rotation;

                    // Find metalhawkZ object under metalhawkY
                    metalhawkZObject = metalhawkYObject.Find("metalhawkZ");
                    if (metalhawkZObject != null)
                    {
                        logger.Info("metalhawkZ object found.");
                        metalhawkZStartPosition = metalhawkZObject.position;
                        metalhawkZStartRotation = metalhawkZObject.rotation;

                        // Find fireemissive object under sharrierX
                        fireemissiveObject = metalhawkZObject.Find("fireemissive");
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
                            logger.Debug("fireemissive object not found under metalhawkZ.");
                        }

                        // Find fireemissive object under sharrierX
                        fireemissive2Object = metalhawkZObject.Find("fireemissive2");
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
                            logger.Debug("fireemissive2 object not found under metalhawkZ.");
                        }


                        // Find metalhawkControllerX under metalhawkZ
                        metalhawkControllerX = metalhawkZObject.Find("metalhawkControllerX");
                        if (metalhawkControllerX != null)
                        {
                            logger.Info("metalhawkControllerX object found.");
                            // Store initial position and rotation of the stick
                            metalhawkControllerXStartPosition = metalhawkControllerX.transform.position; // these could cause the controller to mess up
                            metalhawkControllerXStartRotation = metalhawkControllerX.transform.rotation;

                            // Find metalhawkControllerY under metalhawkControllerX
                            metalhawkControllerY = metalhawkControllerX.Find("metalhawkControllerY");
                            if (metalhawkControllerY != null)
                            {
                                logger.Info("metalhawkControllerY object found.");
                                // Store initial position and rotation of the stick
                                metalhawkControllerYStartPosition = metalhawkControllerY.transform.position;
                                metalhawkControllerYStartRotation = metalhawkControllerY.transform.rotation;

                                // Find metalhawkControllerZ under metalhawkControllerY
                                metalhawkControllerZ = metalhawkControllerY.Find("metalhawkControllerZ");
                                if (metalhawkControllerZ != null)
                                {
                                    logger.Info("metalhawkControllerZ object found.");
                                    // Store initial position and rotation of the stick
                                    metalhawkControllerZStartPosition = metalhawkControllerZ.transform.position;
                                    metalhawkControllerZStartRotation = metalhawkControllerZ.transform.rotation;
                                }
                                else
                                {
                                    logger.Error("metalhawkControllerZ object not found under metalhawkControllerY!");
                                }
                            }
                            else
                            {
                                logger.Error("metalhawkControllerY object not found under metalhawkControllerX!");
                            }
                        }
                        else
                        {
                            logger.Error("metalhawkControllerX object not found under metalhawkZ!");
                        }

                        // Gets all Light components in the target object and its children
                        Light[] allLights = metalhawkZObject.GetComponentsInChildren<Light>();

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


                        // Find cockpit camera under cockpit
                        cockpitCam = metalhawkZObject.Find("eyes/cockpitcam")?.gameObject;
                        if (cockpitCam != null)
                        {
                            logger.Info("Cockpitcam object found.");

                            // Store initial position and rotation of cockpit cam
                            cockpitCamStartPosition = cockpitCam.transform.position;
                            cockpitCamStartRotation = cockpitCam.transform.rotation;
                        }
                        else
                        {
                            logger.Error("Cockpitcam object not found under metalhawkZ!");
                        }
                    }
                    else
                    {
                        logger.Error("metalhawkZ object not found under metalhawkY!");
                    }
                }

                else
                {
                    logger.Error("metalhawkY object not found under metalhawkX!");
                }
            }
            else
            {
                logger.Error("metalhawkX object not found!");
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
            logger.Info("Compatible Rom Dectected, Booting a foot...");
            logger.Info("Namco Metal Hawk Motion Sim starting...");
            logger.Info("Spread your wings and fly!...");
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

            // Reset metalhawkX to initial positions and rotations
            if (metalhawkXObject != null)
            {
                metalhawkXObject.position = metalhawkXStartPosition;
                metalhawkXObject.rotation = metalhawkXStartRotation;
            }

            // Reset metalhawkY object to initial position and rotation
            if (metalhawkYObject != null)
            {
                metalhawkYObject.position = metalhawkYStartPosition;
                metalhawkYObject.rotation = metalhawkYStartRotation;
            }
            // Reset metalhawkZ object to initial position and rotation
            if (metalhawkZObject != null)
            {
                metalhawkZObject.position = metalhawkZStartPosition;
                metalhawkZObject.rotation = metalhawkZStartRotation;
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
            // XInput.IsConnected

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
                inputDetected = true;
            } 

                       // Handle X rotation for metalhawkYObject and metalhawkControllerX (Down Arrow or primaryThumbstick.y > 0)
            // Thumbstick direction: down
            if ((Input.GetKey(KeyCode.DownArrow) || primaryThumbstick.y > 0))
            {
                if (currentRotationX > -rotationLimitX)
                {
                    float rotateX = (Input.GetKey(KeyCode.DownArrow) ? keyboardVelocityX : primaryThumbstick.y * vrVelocity) * Time.deltaTime;
                    metalhawkYObject.Rotate(rotateX, 0, 0);
                    currentRotationX -= rotateX;
                    inputDetected = true;
                }
                if (currentControllerRotationX > -controllerrotationLimitX)
                {
                    float controllerRotateX = (Input.GetKey(KeyCode.DownArrow) ? keyboardControllerVelocityX : primaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                    metalhawkControllerX.Rotate(controllerRotateX, 0, 0);
                    currentControllerRotationX -= controllerRotateX;
                    inputDetected = true;
                }
            }

            // Handle X rotation for metalhawkYObject and metalhawkControllerX (Up Arrow or primaryThumbstick.y < 0)
            // Thumbstick direction: up
            if ((Input.GetKey(KeyCode.UpArrow) || primaryThumbstick.y < 0))
            {
                if (currentRotationX < rotationLimitX)
                {
                    float rotateX = (Input.GetKey(KeyCode.UpArrow) ? keyboardVelocityX : -primaryThumbstick.y * vrVelocity) * Time.deltaTime;
                    metalhawkYObject.Rotate(-rotateX, 0, 0);
                    currentRotationX += rotateX;
                    inputDetected = true;
                }
                if (currentControllerRotationX < controllerrotationLimitX)
                {
                    float controllerRotateX = (Input.GetKey(KeyCode.UpArrow) ? keyboardControllerVelocityX : -primaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                    metalhawkControllerX.Rotate(-controllerRotateX, 0, 0);
                    currentControllerRotationX += controllerRotateX;
                    inputDetected = true;
                }
            }

            // Handle Z rotation for metalhawkXObject and metalhawkControllerZ (Down Arrow or primaryThumbstick.y < 0)
            // Thumbstick direction: Left
            if ((Input.GetKey(KeyCode.LeftArrow) || primaryThumbstick.x < 0))
            {
                if (currentRotationZ > -rotationLimitZ)
                {
                    float rotateZ = (Input.GetKey(KeyCode.LeftArrow) ? keyboardVelocityZ : -primaryThumbstick.x * vrVelocity) * Time.deltaTime;
                    metalhawkXObject.Rotate(0, 0, rotateZ);
                    currentRotationZ -= rotateZ;
                    inputDetected = true;
                }
                if (currentControllerRotationZ > -controllerrotationLimitZ)
                {
                    float controllerRotateZ = (Input.GetKey(KeyCode.LeftArrow) ? keyboardControllerVelocityZ : -primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                    metalhawkControllerZ.Rotate(0, 0, controllerRotateZ);
                    currentControllerRotationZ -= controllerRotateZ;
                    inputDetected = true;
                }
            }

            // Handle Z rotation for metalhawkXObject and metalhawkControllerZ (Up Arrow or primaryThumbstick.y > 0)
            // Thumbstick direction: right
            if ((Input.GetKey(KeyCode.RightArrow) || primaryThumbstick.x > 0))
            {
                if (currentRotationZ < rotationLimitZ)
                {
                    float rotateZ = (Input.GetKey(KeyCode.RightArrow) ? keyboardVelocityZ : primaryThumbstick.x * vrVelocity) * Time.deltaTime;
                    metalhawkXObject.Rotate(0, 0, -rotateZ);
                    currentRotationZ += rotateZ;
                    inputDetected = true;
                }
                if (currentControllerRotationZ < controllerrotationLimitZ)
                {
                    float controllerRotateZ = (Input.GetKey(KeyCode.RightArrow) ? keyboardControllerVelocityZ : primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                    metalhawkControllerZ.Rotate(0, 0, -controllerRotateZ);
                    currentControllerRotationZ += controllerRotateZ;
                    inputDetected = true;
                }
            }

            // Handle left rotation (Thumbstick left)
            if (secondaryThumbstick.x < 0 && currentRotationY > -rotationLimitY)
            {
                float rotateY = secondaryThumbstick.x * vrVelocity * Time.deltaTime;
                metalhawkYObject.Rotate(0, rotateY, 0);  // Rotate Y
                currentRotationY += rotateY;  // Update current rotation (more positive)
                inputDetected = true;
            }

            // Handle right rotation (Thumbstick right)
            if (secondaryThumbstick.x > 0 && currentRotationY < rotationLimitY)
            {
                float rotateY = secondaryThumbstick.x * vrVelocity * Time.deltaTime;
                metalhawkYObject.Rotate(0, rotateY, 0);  // Rotate Y
                currentRotationY += rotateY;  // Update current rotation (more negative)
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
                metalhawkYObject.Rotate(unrotateX, 0, 0);
                currentRotationX -= unrotateX;
            }
            else if (currentRotationX < 0)
            {
                float unrotateX = Mathf.Min(centeringVelocityX * Time.deltaTime, -currentRotationX);
                metalhawkYObject.Rotate(-unrotateX, 0, 0);
                currentRotationX += unrotateX;
            }

            // Center Y-axis
            if (currentRotationY > 0)
            {
                float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, currentRotationY);
                metalhawkXObject.Rotate(0, unrotateY, 0);
                currentRotationY -= unrotateY;
            }
            else if (currentRotationY < 0)
            {
                float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, -currentRotationY);
                metalhawkXObject.Rotate(0, -unrotateY, 0);
                currentRotationY += unrotateY;
            }

            // Center Z-axis
            if (currentRotationZ > 0)
            {
                float unrotateZ = Mathf.Min(centeringVelocityZ * Time.deltaTime, currentRotationZ);
                metalhawkXObject.Rotate(0, 0, unrotateZ);
                currentRotationZ -= unrotateZ;
            }
            else if (currentRotationZ < 0)
            {
                float unrotateZ = Mathf.Min(centeringVelocityZ * Time.deltaTime, -currentRotationZ);
                metalhawkXObject.Rotate(0, 0, -unrotateZ);
                currentRotationZ += unrotateZ;
            }
            //Centering for contoller

            // Center Y-axis Controller rotation
            if (currentControllerRotationY > 0)
            {
                float unrotateY = Mathf.Min(centeringControllerVelocityY * Time.deltaTime, currentControllerRotationY);
                metalhawkControllerY.Rotate(0, unrotateY, 0);   // Rotating to reduce the rotation
                currentControllerRotationY -= unrotateY;    // Reducing the positive rotation
            }
            else if (currentControllerRotationY < 0)
            {
                float unrotateY = Mathf.Min(centeringControllerVelocityY * Time.deltaTime, -currentControllerRotationY);
                metalhawkControllerY.Rotate(0, -unrotateY, 0);  // Rotating to reduce the rotation
                currentControllerRotationY += unrotateY;    // Reducing the negative rotation
            }

            // Center X-Axis Controller rotation
            if (currentControllerRotationX > 0)
            {
                float unrotateX = Mathf.Min(centeringControllerVelocityX * Time.deltaTime, currentControllerRotationX);
                metalhawkControllerX.Rotate(unrotateX, 0, 0);   // Rotating to reduce the rotation
                currentControllerRotationX -= unrotateX;    // Reducing the positive rotation
            }
            else if (currentControllerRotationX < 0)
            {
                float unrotateX = Mathf.Min(centeringControllerVelocityX * Time.deltaTime, -currentControllerRotationX);
                metalhawkControllerX.Rotate(-unrotateX, 0, 0);   // Rotating to reduce the rotation
                currentControllerRotationX += unrotateX;    // Reducing the positive rotation
            }

            // Center Z-axis Controller rotation
            if (currentControllerRotationZ > 0)
            {
                float unrotateZ = Mathf.Min(centeringControllerVelocityZ * Time.deltaTime, currentControllerRotationZ);
                metalhawkControllerZ.Rotate(0, 0, unrotateZ);   // Rotating to reduce the rotation
                currentControllerRotationZ -= unrotateZ;    // Reducing the positive rotation
            }
            else if (currentControllerRotationZ < 0)
            {
                float unrotateZ = Mathf.Min(centeringControllerVelocityZ * Time.deltaTime, -currentControllerRotationZ);
                metalhawkControllerZ.Rotate(0, 0, -unrotateZ);   // Rotating to reduce the rotation
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
                    logger.Info($"fireemissive2 object emission turned {(isActive ? "on" : "off")}.");
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
