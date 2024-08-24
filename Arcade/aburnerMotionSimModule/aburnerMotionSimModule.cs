using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using EmuVR.InputManager;
using System.Collections;

namespace WIGUx.Modules.aburnerMotionSim
{
    public class aburnerMotionSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        private readonly float keyboardVelocityX = 25.5f;  // Velocity for keyboard input
        private readonly float keyboardVelocityY = 25.5f;  // Velocity for keyboard input
        private readonly float keyboardVelocityZ = 25.5f;  // Velocity for keyboard input
        private readonly float vrVelocity = 30.5f;        // Velocity for VR controller input

        private readonly float centeringVelocityX = 20.5f;  // Velocity for centering rotation
        private readonly float centeringVelocityY = 20.5f;  // Velocity for centering rotation
        private readonly float centeringVelocityZ = 20.5f;  // Velocity for centering rotation

        private float adjustSpeed = 1.0f;  // Adjust this adjustment speed as needed a lower number will lead to smaller adustments

        private float rotationLimitX = 15f;  // Rotation limit for X-axis
        private float rotationLimitY = 15f;  // Rotation limit for Y-axis
        private float rotationLimitZ = 15f;  // Rotation limit for Z-axis

        private float currentRotationX = 0f;  // Current rotation for X-axis
        private float currentRotationY = 0f;  // Current rotation for Y-axis
        private float currentRotationZ = 0f;  // Current rotation for Z-axis

        // Controller animation 
        // Speeds for the animation of the in game flight stick or wheel
        private readonly float keyboardControllerVelocityX = 150.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityY = 150.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityZ = 150.5f;  // Velocity for keyboard input
        private readonly float vrControllerVelocity = 200.5f;        // Velocity for VR controller input

        private float controllerrotationLimitX = 15f;  // Rotation limit for X-axis (stick or wheel)
        private float controllerrotationLimitY = 0f;  // Rotation limit for Y-axis (stick or wheel)
        private float controllerrotationLimitZ = 10f;  // Rotation limit for Z-axis (stick or wheel)

        private float currentControllerRotationX = 0f;  // Current rotation for X-axis (stick or wheel)
        private float currentControllerRotationY = 0f;  // Current rotation for Y-axis (stick or wheel)
        private float currentControllerRotationZ = 0f;  // Current rotation for Z-axis (stick or wheel)

        private readonly float centeringControllerVelocityX = 50.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float centeringControllerVelocityY = 50.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float centeringControllerVelocityZ = 50.5f;  // Velocity for centering rotation (stick or wheel)

        private Transform aburnerControllerX; // Reference to the main animated controller (wheel)
        private Vector3 aburnerControllerXStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion aburnerControllerXStartRotation; // Initial controlller positions and rotations for resetting
        private Transform aburnerControllerY; // Reference to the main animated controller (wheel)
        private Vector3 aburnerControllerYStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion aburnerControllerYStartRotation; // Initial controlller positions and rotations for resetting
        private Transform aburnerControllerZ; // Reference to the main animated controller (wheel)
        private Vector3 aburnerControllerZStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion aburnerControllerZStartRotation; // Initial controlller positions and rotations for resetting

        private Transform aburnerXObject; // Reference to the main X object
        private Transform aburnerYObject; // Reference to the main Y object
        private Transform aburnerZObject; // Reference to the main Z object
        private GameObject cockpitCam;    // Reference to the cockpit camera

        // Initial positions and rotations for resetting
        private Vector3 aburnerXStartPosition;
        private Quaternion aburnerXStartRotation;
        private Vector3 aburnerYStartPosition;
        private Quaternion aburnerYStartRotation;
        private Vector3 aburnerZStartPosition;
        private Quaternion aburnerZStartRotation;
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
        private Transform fireemissiveObject;
        private Transform fireemissive2Object;
        public Light fire1_light;
        public Light fire2_light;
        public float lightDuration = 0.35f; // Duration during which the lights will be on
  //      public float fireLightIntensity = 3.0f; // Intensity of the light when it is on
//        public Color fireLightColor = Color.red; // Color of the light when it is on
        private Light[] lights;

        private bool inFocusMode = false;  // Flag to track focus mode state

        private readonly string[] compatibleGames = { "aburner2.zip", "aburner.zip" };

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

            // Find aburnerX object in hierarchy
            aburnerXObject = transform.Find("aburnerX");
            if (aburnerXObject != null)
            {
                logger.Info("aburnerX object found.");
                aburnerXStartPosition = aburnerXObject.position;
                aburnerXStartRotation = aburnerXObject.rotation;

                // Gets all Light components in the target object and its children
                Light[] allLights = aburnerXObject.GetComponentsInChildren<Light>();

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

                /*
                // Find the "firelight1" under aburnerX and get its Light component
                Transform firelight1Transform = aburnerXObject.Find("firelight1");
                if (firelight1Transform != null)
                {
                    logger.Info("fire1_light object found.");
                    firelight1 = fire1_lightTransform.GetComponent<Light>();
                }
                else
                {
                    logger.Error("fire1_light object not found under aburnerX!");
                }

                // Find the "fire2_light" under aburnerX and get its Light component
                Transform fire2_lightTransform = aburnerXObject.Find("fire2_light");
                if (fire2_lightTransform != null)
                {
                    logger.Info("fire2_light object found.");
                    fire2_light = firelight2Transform.GetComponent<Light>();
                }
                else
                {
                    logger.Error("fire2_light object not found under aburnerX!");
                }
                */

                // Find aburnerY object under aburnerX
                aburnerYObject = aburnerXObject.Find("aburnerY");
                if (aburnerYObject != null)
                {
                    logger.Info("aburnerY object found.");
                    aburnerYStartPosition = aburnerYObject.position;
                    aburnerYStartRotation = aburnerYObject.rotation;

                    // Find aburnerZ object under aburnerY
                    aburnerZObject = aburnerYObject.Find("aburnerZ");
                    if (aburnerZObject != null)
                    {
                        logger.Info("aburnerZ object found.");
                        aburnerZStartPosition = aburnerZObject.position;
                        aburnerZStartRotation = aburnerZObject.rotation;

                        // Find fireemissive object under aburnerX
                        fireemissiveObject = aburnerXObject.Find("fireemissive");
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
                            logger.Debug("fireemissive object not found under aburnerX.");
                        }

                        // Find aburnerControllerX under aburnerZ
                        aburnerControllerX = aburnerZObject.Find("aburnerControllerX");
                        if (aburnerControllerX != null)
                        {
                            logger.Info("aburnerControllerX object found.");
                            // Store initial position and rotation of the stick
                            aburnerControllerXStartPosition = aburnerControllerX.transform.position; // these could cause the controller to mess up
                            aburnerControllerXStartRotation = aburnerControllerX.transform.rotation;

                            // Find aburnerControllerY under aburnerControllerX
                            aburnerControllerY = aburnerControllerX.Find("aburnerControllerY");
                            if (aburnerControllerY != null)
                            {
                                logger.Info("aburnerControllerY object found.");
                                // Store initial position and rotation of the stick
                                aburnerControllerYStartPosition = aburnerControllerY.transform.position;
                                aburnerControllerYStartRotation = aburnerControllerY.transform.rotation;

                                // Find aburnerControllerZ under aburnerControllerY
                                aburnerControllerZ = aburnerControllerY.Find("aburnerControllerZ");
                                if (aburnerControllerZ != null)
                                {
                                    logger.Info("aburnerControllerZ object found.");
                                    // Store initial position and rotation of the stick
                                    aburnerControllerZStartPosition = aburnerControllerZ.transform.position;
                                    aburnerControllerZStartRotation = aburnerControllerZ.transform.rotation;
                                }
                                else
                                {
                                    logger.Error("aburnerControllerZ object not found under aburnerControllerY!");
                                }
                            }
                            else
                            {
                                logger.Error("aburnerControllerY object not found under aburnerControllerX!");
                            }
                        }
                        else
                        {
                            logger.Error("aburnerControllerX object not found under aburnerZ!");
                        }

                        // Find cockpit camera under aburnerZ
                        cockpitCam = aburnerZObject.Find("eyes/cockpitcam")?.gameObject;
                        if (cockpitCam != null)
                        {
                            logger.Info("Cockpitcam object found.");

                            // Store initial position and rotation of cockpit cam
                            cockpitCamStartPosition = cockpitCam.transform.position;
                            cockpitCamStartRotation = cockpitCam.transform.rotation;
                        }
                        else
                        {
                            logger.Error("Cockpitcam object not found under aburnerZ!");
                        }
                    }
                    else
                    {
                        logger.Error("aburnerZ object not found under aburnerY!");
                    }
                }
                else
                {
                    logger.Error("aburnerY object not found under aburnerX!");
                }
            }
            else
            {
                logger.Error("aburnerX object not found!");
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
            logger.Info("Compatible Rom Dectected, Powering Up Motors...");
            logger.Info("Sega After Burner Motion Sim starting...");
            logger.Info("GET READY!!...");

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
                logger.Error("CockpitCam object not found under aburnerZ!");
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

            // Reset aburnerX to initial positions and rotations
            if (aburnerXObject != null)
            {
                aburnerXObject.position = aburnerXStartPosition;
                aburnerXObject.rotation = aburnerXStartRotation;
            }

            // Reset aburnerY object to initial position and rotation
            if (aburnerYObject != null)
            {
                aburnerYObject.position = aburnerYStartPosition;
                aburnerYObject.rotation = aburnerYStartRotation;
            }
            // Reset aburnerZ object to initial position and rotation
            if (aburnerZObject != null)
            {
                aburnerZObject.position = aburnerZStartPosition;
                aburnerZObject.rotation = aburnerZStartRotation;
            }

            // Reset aburnercontrollerX object to initial position and rotation
            if (aburnerControllerX != null)
            {
                // aburnerController.position = aburnerControllerStartPosition;
                aburnerControllerX.rotation = aburnerControllerXStartRotation;
            }

            // Reset aburnercontrollerY object to initial position and rotation
            if (aburnerControllerY != null)
            {
                // aburnerControllerY.position = aburnerControllerStartPosition; 
                aburnerControllerY.rotation = aburnerControllerYStartRotation;
            }

            // Reset aburnercontrollerZ object to initial position and rotation
            if (aburnerControllerZ != null)
            {
                // aburnerControllerY.position = aburnerControllerStartPosition;
                aburnerControllerZ.rotation = aburnerControllerZStartRotation;
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

            //maybe add a check for xinput? not right now.
            // XInput.IsConnected

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

            // Handle X rotation for aburnerYObject and aburnerControllerX (Right Arrow or primaryThumbstick.x > 0)
            // Thumbstick direction: right
            if ((Input.GetKey(KeyCode.RightArrow) || primaryThumbstick.x > 0))
            {
                if (currentRotationX < rotationLimitX)
                {
                    float rotateX = (Input.GetKey(KeyCode.RightArrow) ? keyboardVelocityX : primaryThumbstick.x * vrVelocity) * Time.deltaTime;
                    aburnerYObject.Rotate(-rotateX, 0, 0);
                    currentRotationX += rotateX;
                    inputDetected = true;
                }
                if (currentControllerRotationX < controllerrotationLimitX)
                {
                    float controllerRotateX = (Input.GetKey(KeyCode.RightArrow) ? keyboardControllerVelocityX : primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                    aburnerControllerX.Rotate(-controllerRotateX, 0, 0);
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
                    aburnerYObject.Rotate(rotateX, 0, 0);
                    currentRotationX -= rotateX;
                    inputDetected = true;
                }
                if (currentControllerRotationX > -controllerrotationLimitX)
                {
                    float controllerRotateX = (Input.GetKey(KeyCode.LeftArrow) ? keyboardControllerVelocityX : -primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                    aburnerControllerX.Rotate(controllerRotateX, 0, 0);
                    currentControllerRotationX -= controllerRotateX;
                    inputDetected = true;
                }
            }

            // Handle Z rotation for aburnerXObject and aburnerControllerZ (Down Arrow or primaryThumbstick.y < 0)
            // Thumbstick direction: down
            if ((Input.GetKey(KeyCode.DownArrow) || primaryThumbstick.y < 0))
            {
                if (currentRotationZ > -rotationLimitZ)
                {
                    float rotateZ = (Input.GetKey(KeyCode.DownArrow) ? keyboardVelocityZ : -primaryThumbstick.y * vrVelocity) * Time.deltaTime;
                    aburnerXObject.Rotate(0, 0, rotateZ);
                    currentRotationZ -= rotateZ;
                    inputDetected = true;
                }
                if (currentControllerRotationZ > -controllerrotationLimitZ)
                {
                    float controllerRotateZ = (Input.GetKey(KeyCode.DownArrow) ? keyboardControllerVelocityZ : -primaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                    aburnerControllerZ.Rotate(0, 0, controllerRotateZ);
                    currentControllerRotationZ -= controllerRotateZ;
                    inputDetected = true;
                }
            }

            // Handle Z rotation for aburnerXObject and aburnerControllerZ (Up Arrow or primaryThumbstick.y > 0)
            // Thumbstick direction: up
            if ((Input.GetKey(KeyCode.UpArrow) || primaryThumbstick.y > 0))
            {
                if (currentRotationZ < rotationLimitZ)
                {
                    float rotateZ = (Input.GetKey(KeyCode.UpArrow) ? keyboardVelocityZ : primaryThumbstick.y * vrVelocity) * Time.deltaTime;
                    aburnerXObject.Rotate(0, 0, -rotateZ);
                    currentRotationZ += rotateZ;
                    inputDetected = true;
                }
                if (currentControllerRotationZ < controllerrotationLimitZ)
                {
                    float controllerRotateZ = (Input.GetKey(KeyCode.UpArrow) ? keyboardControllerVelocityZ : primaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                    aburnerControllerZ.Rotate(0, 0, -controllerRotateZ);
                    currentControllerRotationZ += controllerRotateZ;
                    inputDetected = true;
                }
            }

            // Handle Y rotation for aburnerYObject and aburnerControllerY (Unused axis or secondaryThumbstick.x)
            // Thumbstick direction: left/right
            if ((Input.GetKey(KeyCode.None) || secondaryThumbstick.x != 0))
            {
                if (secondaryThumbstick.x > 0 && currentRotationY < rotationLimitY)
                {
                    float rotateY = secondaryThumbstick.x * vrVelocity * Time.deltaTime;
                    aburnerYObject.Rotate(0, rotateY, 0);
                    currentRotationY += rotateY;
                    inputDetected = true;
                }
                if (secondaryThumbstick.x > 0 && currentControllerRotationY < controllerrotationLimitY)
                {
                    float controllerRotateY = secondaryThumbstick.x * vrControllerVelocity * Time.deltaTime;
                    aburnerControllerY.Rotate(0, controllerRotateY, 0);
                    currentControllerRotationY += controllerRotateY;
                    inputDetected = true;
                }
                if (secondaryThumbstick.x < 0 && currentRotationY > -rotationLimitY)
                {
                    float rotateY = secondaryThumbstick.x * vrVelocity * Time.deltaTime;
                    aburnerYObject.Rotate(0, rotateY, 0);
                    currentRotationY -= rotateY;
                    inputDetected = true;
                }
                if (secondaryThumbstick.x < 0 && currentControllerRotationY > -controllerrotationLimitY)
                {
                    float controllerRotateY = secondaryThumbstick.x * vrControllerVelocity * Time.deltaTime;
                    aburnerControllerY.Rotate(0, controllerRotateY, 0);
                    currentControllerRotationY -= controllerRotateY;
                    inputDetected = true;
                }
            }
            /*
            // Handle unused axis rotation for aburnerYObject and aburnerControllerY
            // This can be mapped to secondaryThumbstick.y for additional rotation control
            // Thumbstick direction: up/down
            if (secondaryThumbstick.y != 0)
            {
                if (secondaryThumbstick.y > 0 && currentRotationY < rotationLimitY)
                {
                    float rotateY = secondaryThumbstick.y * vrVelocity * Time.deltaTime;
                    aburnerYObject.Rotate(0, rotateY, 0);
                    currentRotationY += rotateY;
                    inputDetected = true;
                }
                if (secondaryThumbstick.y > 0 && currentControllerRotationY < controllerrotationLimitY)
                {
                    float controllerRotateY = secondaryThumbstick.y * vrControllerVelocity * Time.deltaTime;
                    aburnerControllerY.Rotate(0, controllerRotateY, 0);
                    currentControllerRotationY += controllerRotateY;
                    inputDetected = true;
                }
                if (secondaryThumbstick.y < 0 && currentRotationY > -rotationLimitY)
                {
                    float rotateY = secondaryThumbstick.y * vrVelocity * Time.deltaTime;
                    aburnerYObject.Rotate(0, rotateY, 0);
                    currentRotationY -= rotateY;
                    inputDetected = true;
                }
                if (secondaryThumbstick.y < 0 && currentControllerRotationY > -controllerrotationLimitY)
                {
                    float controllerRotateY = secondaryThumbstick.y * vrControllerVelocity * Time.deltaTime;
                    aburnerControllerY.Rotate(0, controllerRotateY, 0);
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
                aburnerYObject.Rotate(unrotateX, 0, 0);
                currentRotationX -= unrotateX;
            }
            else if (currentRotationX < 0)
            {
                float unrotateX = Mathf.Min(centeringVelocityX * Time.deltaTime, -currentRotationX);
                aburnerYObject.Rotate(-unrotateX, 0, 0);
                currentRotationX += unrotateX;
            }

            // Center Y-axis
            if (currentRotationY > 0)
            {
                float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, currentRotationY);
                aburnerXObject.Rotate(0, unrotateY, 0);
                currentRotationY -= unrotateY;
            }
            else if (currentRotationY < 0)
            {
                float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, -currentRotationY);
                aburnerXObject.Rotate(0, -unrotateY, 0);
                currentRotationY += unrotateY;
            }

            // Center Z-axis
            if (currentRotationZ > 0)
            {
                float unrotateZ = Mathf.Min(centeringVelocityZ * Time.deltaTime, currentRotationZ);
                aburnerXObject.Rotate(0, 0, unrotateZ);
                currentRotationZ -= unrotateZ;
            }
            else if (currentRotationZ < 0)
            {
                float unrotateZ = Mathf.Min(centeringVelocityZ * Time.deltaTime, -currentRotationZ);
                aburnerXObject.Rotate(0, 0, -unrotateZ);
                currentRotationZ += unrotateZ;
            }
            //Centering for contoller

            // Center Y-axis Controller rotation
            if (currentControllerRotationY > 0)
            {
                float unrotateY = Mathf.Min(centeringControllerVelocityY * Time.deltaTime, currentControllerRotationY);
                aburnerControllerY.Rotate(0, unrotateY, 0);   // Rotating to reduce the rotation
                currentControllerRotationY -= unrotateY;    // Reducing the positive rotation
            }
            else if (currentControllerRotationY < 0)
            {
                float unrotateY = Mathf.Min(centeringControllerVelocityY * Time.deltaTime, -currentControllerRotationY);
                aburnerControllerY.Rotate(0, -unrotateY, 0);  // Rotating to reduce the rotation
                currentControllerRotationY += unrotateY;    // Reducing the negative rotation
            }

            // Center X-Axis Controller rotation
            if (currentControllerRotationX > 0)
            {
                float unrotateX = Mathf.Min(centeringControllerVelocityX * Time.deltaTime, currentControllerRotationX);
                aburnerControllerX.Rotate(unrotateX, 0, 0);   // Rotating to reduce the rotation
                currentControllerRotationX -= unrotateX;    // Reducing the positive rotation
            }
            else if (currentControllerRotationX < 0)
            {
                float unrotateX = Mathf.Min(centeringControllerVelocityX * Time.deltaTime, -currentControllerRotationX);
                aburnerControllerX.Rotate(-unrotateX, 0, 0);   // Rotating to reduce the rotation
                currentControllerRotationX += unrotateX;    // Reducing the positive rotation
            }

            // Center Z-axis Controller rotation
            if (currentControllerRotationZ > 0)
            {
                float unrotateZ = Mathf.Min(centeringControllerVelocityZ * Time.deltaTime, currentControllerRotationZ);
                aburnerControllerZ.Rotate(0, 0, unrotateZ);   // Rotating to reduce the rotation
                currentControllerRotationZ -= unrotateZ;    // Reducing the positive rotation
            }
            else if (currentControllerRotationZ < 0)
            {
                float unrotateZ = Mathf.Min(centeringControllerVelocityZ * Time.deltaTime, -currentControllerRotationZ);
                aburnerControllerZ.Rotate(0, 0, -unrotateZ);   // Rotating to reduce the rotation
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
