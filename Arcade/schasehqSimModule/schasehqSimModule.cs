using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using EmuVR.InputManager;
using System.Collections;
using static XInput;
using Valve.VR;
using static SteamVR_Utils;
using System.IO;
using MelonLoader.ICSharpCode.SharpZipLib.GZip;

namespace WIGUx.Modules.schasehqSim
{
    public class schasehqSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        private readonly float keyboardVelocityX = 25.5f;  // Velocity for keyboard input
        private readonly float keyboardVelocityY = 25.5f;  // Velocity for keyboard input
        private readonly float keyboardVelocityZ = 25.5f;  // Velocity for keyboard input
        private readonly float vrVelocity = 30.5f;        // Velocity for VR controller input

        private float adjustSpeed = 1.0f;  // Adjust this adjustment speed as needed a lower number will lead to smaller adustments

        // Controller animation 

        // Speeds for the animation of the in game flight stick or wheel
        private readonly float keyboardControllerVelocityX = 400.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityY = 400.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityZ = 400.5f;  // Velocity for keyboard input
        private readonly float vrControllerVelocity = 400.5f;        // Velocity for VR/Controller input

        // player 1

        //p1 sticks

        private float schasehqp1controllerrotationLimitX = 270f;  // Rotation limit for X-axis (stick or wheel)
        private float schasehqp1controllerrotationLimitY = 0f;  // Rotation limit for Y-axis (stick or wheel)
        private float schasehqp1controllerrotationLimitZ = 0f;  // Rotation limit for Z-axis (stick or wheel)

        private float schasehqp1currentControllerRotationX = 0f;  // Current rotation for X-axis (stick or wheel)
        private float schasehqp1currentControllerRotationY = 0f;  // Current rotation for Y-axis (stick or wheel)
        private float schasehqp1currentControllerRotationZ = 0f;  // Current rotation for Z-axis (stick or wheel)

        private readonly float schasehqp1centeringControllerVelocityX = 400.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float schasehqp1centeringControllerVelocityY = 400.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float schasehqp1centeringControllerVelocityZ = 400.5f;  // Velocity for centering rotation (stick or wheel)

        private Transform schasehqshifter; // Reference to shifter
        private Vector3 schasehqshifterStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion schasehqshifterStartRotation; // Initial controlller positions and rotations for resetting
        private Transform schasehqp1controllerX; // Reference to the main animated controller (wheel)
        private Vector3 schasehqp1controllerXStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion schasehqp1controllerXStartRotation; // Initial controlller positions and rotations for resetting
        private Transform schasehqp1controllerY; // Reference to the main animated controller (wheel)
        private Vector3 schasehqp1controllerYStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion schasehqp1controllerYStartRotation; // Initial controlller positions and rotations for resetting
        private Transform schasehqp1controllerZ; // Reference to the main animated controller (wheel)
        private Vector3 schasehqp1controllerZStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion schasehqp1controllerZStartRotation; // Initial controlller positions and rotations for resetting

        //lights
        private Transform lightsObject;
        public Light[] schasehqLights = new Light[2]; // Array to store lights
        public Light schasehq1_light;
        public Light schasehq2_light;
        public Light schasehq3_light;
        public Light schasehq4_light;
        private float flashDuration = 0.15f;
        private float flashInterval = 0.15f;
        private float lightDuration = 0.5f; // Duration during which the lights will be on
        private bool areschasehqLighsOn = false; // track strobe lights
        private Coroutine schasehqCoroutine; // Coroutine variable to control the strobe flashing
        private Light[] lights;  
        private bool isschasehqFlashing = false; //set the flashing flag
        private bool isschasehqinHigh = false; //set the flashing flag
        private bool inFocusMode = false;  // Flag to track focus mode state
        private readonly string[] compatibleGames = { "superchs.zip" };
        private Dictionary<GameObject, Transform> originalParents = new Dictionary<GameObject, Transform>();  // Dictionary to store original parents of objects
                                                                                                              // Public property to access the Game instance
        void Start()
        {
            logger.Info("Looking For Lights In Super Chase - Criminal Termination");
            lightsObject = transform.Find("lights");
            if (lightsObject != null)
            {
                logger.Info("lightsObject found.");
            }
            else
            {
                logger.Error("lightsObject object not found!");
                return; // Early exit if lightsObject is not found
            }
            // Gets all Light components in the target object and its children
            Light[] allLights = lightsObject.GetComponentsInChildren<Light>();

            // Log the names of the objects containing the Light components and filter out unwanted lights
            foreach (Light light in allLights)
            {
                logger.Info($"Light found: {light.gameObject.name}");
                switch (light.gameObject.name)
                {
                    case "schasehq1_light":
                        schasehq1_light = light;
                        schasehqLights[0] = light;
                        logger.Info("Included Light found in object: " + light.gameObject.name);
                        break;
                    case "schasehq2_light":
                        schasehq2_light = light;
                        schasehqLights[1] = light;
                        logger.Info("Included Light found in object: " + light.gameObject.name);
                        break;
                    default:
                        logger.Info("Excluded Light found in object: " + light.gameObject.name);
                        break;
                }
            }

            // Log the assigned lights for verification
            for (int i = 0; i < schasehqLights.Length; i++)
            {
                if (schasehqLights[i] != null)
                {
                    logger.Info($"schasehqLights[{i}] assigned to: {schasehqLights[i].name}");
                }
                else
                {
                    logger.Error($"schasehqLights[{i}] is not assigned!");
                }
            }
            schasehqshifter = transform.Find("schasehqshifter");
            if (schasehqshifter != null)
            {
                logger.Info("schasehqshifter object found.");
                schasehqshifterStartPosition = schasehqshifter.transform.position;
                schasehqshifterStartRotation = schasehqshifter.transform.rotation;
            }
            // Find schasehqcontrollerX for player 1
            schasehqp1controllerX = transform.Find("schasehqp1controllerX");
            if (schasehqp1controllerX != null)
            {
                logger.Info("schasehqp1controllerX object found.");
                // Store initial position and rotation of the stick
                schasehqp1controllerXStartPosition = schasehqp1controllerX.transform.position;
                schasehqp1controllerXStartRotation = schasehqp1controllerX.transform.rotation;

                // Find schasehqp1controllerY under schasehqp1controllerX
                schasehqp1controllerY = schasehqp1controllerX.Find("schasehqp1controllerY");
                if (schasehqp1controllerY != null)
                {
                    logger.Info("schasehqp1controllerY object found.");
                    // Store initial position and rotation of the stick
                    schasehqp1controllerYStartPosition = schasehqp1controllerY.transform.position;
                    schasehqp1controllerYStartRotation = schasehqp1controllerY.transform.rotation;

                    // Find schasehqp1controllerZ under schasehqp1controllerY
                    schasehqp1controllerZ = schasehqp1controllerY.Find("schasehqp1controllerZ");
                    if (schasehqp1controllerZ != null)
                    {
                        logger.Info("schasehqp1controllerZ object found.");
                        // Store initial position and rotation of the stick
                        schasehqp1controllerZStartPosition = schasehqp1controllerZ.transform.position;
                        schasehqp1controllerZStartRotation = schasehqp1controllerZ.transform.rotation;
                    }
                    else
                    {
                        logger.Error("schasehqp1controllerZ object not found under controllerY!");
                    }
                }
                else
                {
                    logger.Error("schasehqp1controllerY object not found under controllerX!");
                }
            }
            else
            {
                logger.Error("schasehqp1controllerX object not found!!");
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
                HandleInput(ref inputDetected);  // Pass by reference
                HandleKBInput();
                HandleVRInput();
                HandleXInput();
            }
        }

        private void HandleKBInput()
        {

        }

        private void HandleVRInput()
        {
            var leftController = VRControllerHelper.GetController(HandType.Left);
            var rightController = VRControllerHelper.GetController(HandType.Right);
        }

        private void HandleXInput()
        {
            Vector2 primaryThumbstick = Vector2.zero;
            Vector2 secondaryThumbstick = Vector2.zero;

            if (XInput.IsConnected)
            {
                primaryThumbstick = XInput.Get(XInput.Axis.LThumbstick);
                secondaryThumbstick = XInput.Get(XInput.Axis.RThumbstick);

                Vector2 xboxPrimaryThumbstick = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                Vector2 xboxSecondaryThumbstick = new Vector2(Input.GetAxis("RightStickHorizontal"), Input.GetAxis("RightStickVertical"));

                // Combine VR and Xbox inputs
                primaryThumbstick += xboxPrimaryThumbstick;
                secondaryThumbstick += xboxSecondaryThumbstick;
                /*
                // Handle RT press
                if (XInput.GetDown(XInput.Button.RIndexTrigger))
                {
                    logger.Info("XInput Right Trigger pressed");
                }

                // Reset position on RT release
                if (XInput.GetUp(XInput.Button.RIndexTrigger))
                {
                    logger.Info("XInput Right Trigger released");
                }

                // Handle LT press
                if (XInput.GetDown(XInput.Button.LIndexTrigger))
                {
                    logger.Info("XInput Left Trigger pressed");
                }

                // Reset position on LT release
                if (XInput.GetUp(XInput.Button.LIndexTrigger))
                {
                    logger.Info("XInput Left Trigger released");
                }

                // Handle A button
                if (XInput.GetDown(XInput.Button.A))
                {
                    logger.Info("XInput Button A pressed");
                }

                // Handle B button
                if (XInput.GetDown(XInput.Button.B))
                {
                    logger.Info("XInput Button B pressed");
                }

                // Handle X button
                if (XInput.GetDown(XInput.Button.X))
                {
                    logger.Info("XInput Button X pressed");
                }

                // Handle Y button
                if (XInput.GetDown(XInput.Button.Y))
                {
                    logger.Info("XInput Button Y pressed");
                }

                // Handle Start button
                if (XInput.GetDown(XInput.Button.Start))
                {
                    logger.Info("XInput Start Button pressed");
                }

                // Handle Back button
                if (XInput.GetDown(XInput.Button.Back))
                {
                    logger.Info("XInput Back Button pressed");
                }

                // Handle Left Shoulder
                if (XInput.GetDown(XInput.Button.LShoulder))
                {
                    logger.Info("XInput Left Shoulder Button pressed");
                }

                // Handle Right Shoulder
                if (XInput.GetDown(XInput.Button.RShoulder))
                {
                    logger.Info("XInput Right Shoulder Button pressed");
                }
                */
            }
        }


        public static class VRControllerHelper
        {
            public static SteamVR_Controller.Device GetController(HandType handType)
            {
                int index = -1;

                // Get the device index based on the hand type
                switch (handType)
                {
                    case HandType.Left:
                        index = SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.Leftmost);
                        break;
                    case HandType.Right:
                        index = SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.Rightmost);
                        break;
                }

                // Return the controller device if a valid index was found
                if (index != -1)
                {
                    return SteamVR_Controller.Input(index);
                }
                return null;
            }
        }


        void StartFocusMode()
        {
            string controlledSystemGamePathString = GameSystem.ControlledSystem.Game.path != null ? GameSystem.ControlledSystem.Game.path.ToString() : null;
            logger.Info($"Controlled System Game path String: {controlledSystemGamePathString}");
            logger.Info("Compatible Rom Dectected, Ready to Chase Again?... ");
            logger.Info("Super Chase - Criminal Termination Module starting...");
            logger.Info("Prepare For Criminal Termination!...");
            // Reset controllers to initial positions and rotations
            if (schasehqp1controllerX != null)
            {
                schasehqp1controllerX.position = schasehqp1controllerXStartPosition;
                schasehqp1controllerX.rotation = schasehqp1controllerXStartRotation;
            }
            if (schasehqp1controllerY != null)
            {
                schasehqp1controllerY.position = schasehqp1controllerYStartPosition;
                schasehqp1controllerY.rotation = schasehqp1controllerYStartRotation;
            }
            if (schasehqp1controllerZ != null)
            {
                schasehqp1controllerZ.position = schasehqp1controllerZStartPosition;
                schasehqp1controllerZ.rotation = schasehqp1controllerZStartRotation;
            }
            if (schasehqshifter != null)
            {
                schasehqshifter.position = schasehqshifterStartPosition;
                schasehqshifter.rotation = schasehqshifterStartRotation;
            }


            // Reset rotation allowances and current rotation values
            //player 1
            schasehqp1currentControllerRotationX = 0f;
            schasehqp1currentControllerRotationY = 0f;
            schasehqp1currentControllerRotationZ = 0f;
            //player 2

            inFocusMode = true;  // Set focus mode flag
        }

        void EndFocusMode()
        {
            // Reset controllers to initial positions and rotations
            if (schasehqp1controllerX != null)
            {
                schasehqp1controllerX.position = schasehqp1controllerXStartPosition;
                schasehqp1controllerX.rotation = schasehqp1controllerXStartRotation;
            }
            if (schasehqp1controllerY != null)
            {
                schasehqp1controllerY.position = schasehqp1controllerYStartPosition;
                schasehqp1controllerY.rotation = schasehqp1controllerYStartRotation;
            }
            if (schasehqp1controllerZ != null)
            {
                schasehqp1controllerZ.position = schasehqp1controllerZStartPosition;
                schasehqp1controllerZ.rotation = schasehqp1controllerZStartRotation;
            }

            StopCoroutine(schasehqCoroutine);
            ToggleschasehqLight1(false);
            ToggleschasehqLight2(false);

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
                Vector2 ovrPrimaryThumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
                Vector2 ovrSecondaryThumbstick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
                float ovrPrimaryIndexTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger);
                float ovrSecondaryIndexTrigger = OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger);
                float ovrPrimaryHandTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger);
                float ovrSecondaryHandTrigger = OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger);


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

                // Check if the primary hand trigger on the right controller is pressed
                if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger))
                {
                    logger.Info("OVR Primary hand trigger pressed");

                }
                // Check if the primary thumbstick is pressed
                if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick))
                {
                    logger.Info("OVR Primary thumbstick pressed");
                    {
                        if (!isschasehqFlashing)
                        {
                            // Start the flashing if not already flashing
                            schasehqCoroutine = StartCoroutine(FlashschasehqLights());
                            isschasehqFlashing = true;
                        }
                        else
                        {
                            // Stop the flashing if it's currently active
                            StopCoroutine(schasehqCoroutine);
                            ToggleschasehqLight1(false);
                            ToggleschasehqLight2(false);
                            schasehqCoroutine = null;
                            isschasehqFlashing = false;
                        }

                        inputDetected = true;
                    }
                }

                // Check if the secondary index trigger on the left controller is pressed
                if (OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger))
                {
                    logger.Info("OVR Secondary index trigger pressed");
                }

                // Check if the secondary hand trigger on the righttroller is pressed
                if (OVRInput.Get(OVRInput.Button.SecondaryHandTrigger))
                {
                    if (SteamVRInput.GetDown(SteamVRInput.Button.RGrip))
                    {
                        logger.Info("RGrip pressed");
                        if (!isschasehqinHigh)
                        {
                            // Start the flashing if not already flashing
                            schasehqshifter.Rotate(0, 0, 45f);
                            isschasehqinHigh = true;
                        }
                        else
                        {
                            schasehqshifter.Rotate(0, 0, -45f);
                            isschasehqinHigh = false;
                        }
                        inputDetected = true;
                    }
                }

                // Check if the secondary thumbstick is pressed
                if (OVRInput.GetDown(OVRInput.Button.SecondaryThumbstick))
                {
                    logger.Info("OVR Secondary thumbstick pressed");
                }
            }
            else if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.OpenVR)
            {
                var leftController = SteamVRInput.GetController(HandType.Left);
                var rightController = SteamVRInput.GetController(HandType.Right);
                primaryThumbstick = leftController.GetAxis();
                secondaryThumbstick = rightController.GetAxis();
                if (SteamVRInput.GetDown(SteamVRInput.Button.RGrip))
                {
                    logger.Info("RGrip pressed");
                    if (!isschasehqinHigh)
                    {
                        // Start the flashing if not already flashing
                        schasehqshifter.Rotate(0, 0, 45f);
                        isschasehqinHigh = true;
                    }
                    else
                    {
                        schasehqshifter.Rotate(0, 0, -45f);
                        isschasehqinHigh = false;
                    }
                    inputDetected = true;
                }
                
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
            // Thumbstick direction: Y
            // Thumbstick direction: Right
            if ((Input.GetKey(KeyCode.RightArrow) || XInput.Get(XInput.Button.DpadRight) || primaryThumbstick.x > 0) && p1currentControllerRotationY < p1controllerrotationLimitY)
            {
                float p1controllerRotateY = (Input.GetKey(KeyCode.RightArrow) || XInput.Get(XInput.Button.DpadRight) ? keyboardControllerVelocityY : primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                p1controllerY.Rotate(0, -p1controllerRotateY, 0);
                p1currentControllerRotationY += p1controllerRotateY;
                inputDetected = true;
            }
            // Thumbstick direction: Left
            if ((Input.GetKey(KeyCode.LeftArrow) || XInput.Get(XInput.Button.DpadLeft) || primaryThumbstick.x < 0) && p1currentControllerRotationY > -p1controllerrotationLimitY)
            {
                float p1controllerRotateY = (Input.GetKey(KeyCode.LeftArrow) || XInput.Get(XInput.Button.DpadLeft) ? keyboardControllerVelocityY : -primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                p1controllerY.Rotate(0, p1controllerRotateY, 0);
                p1currentControllerRotationY -= p1controllerRotateY;
                inputDetected = true;
            }
            */

                // Thumbstick direction: X
                // Thumbstick direction: right
                if ((Input.GetKey(KeyCode.RightArrow) || XInput.Get(XInput.Button.DpadRight) || primaryThumbstick.x > 0) && schasehqp1currentControllerRotationX < schasehqp1controllerrotationLimitX)
            {
                float p1controllerRotateX = (Input.GetKey(KeyCode.RightArrow) || XInput.Get(XInput.Button.DpadRight) ? keyboardControllerVelocityX : primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                schasehqp1controllerX.Rotate(-p1controllerRotateX, 0, 0);
                schasehqp1currentControllerRotationX += p1controllerRotateX;
                inputDetected = true;
            }
            // Thumbstick direction: left
            if ((Input.GetKey(KeyCode.LeftArrow) || XInput.Get(XInput.Button.DpadLeft) || primaryThumbstick.x < 0) && schasehqp1currentControllerRotationX > -schasehqp1controllerrotationLimitX)
            {
                float p1controllerRotateX = (Input.GetKey(KeyCode.LeftArrow) || XInput.Get(XInput.Button.DpadLeft) ? keyboardControllerVelocityX : -primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                schasehqp1controllerX.Rotate(p1controllerRotateX, 0, 0);
                schasehqp1currentControllerRotationX -= p1controllerRotateX;
                inputDetected = true;
            }
            /*
            // Thumbstick direction: Z
            // Thumbstick or D-pad direction: Up
            if ((primaryThumbstick.y > 0 || XInput.Get(XInput.Button.DpadUp)) && p1currentControllerRotationZ < p1controllerrotationLimitZ)
            {
                float p1controllerRotateZ = (Input.GetKey(KeyCode.UpArrow) || XInput.Get(XInput.Button.DpadUp) ? keyboardControllerVelocityZ : primaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                p1controllerZ.Rotate(0, 0, -p1controllerRotateZ);
                p1currentControllerRotationZ += p1controllerRotateZ;
                inputDetected = true;
            }

            // Thumbstick or D-pad direction: Down
            if ((primaryThumbstick.y < 0 || XInput.Get(XInput.Button.DpadDown)) && p1currentControllerRotationZ > -p1controllerrotationLimitZ)
            {
                float p1controllerRotateZ = (Input.GetKey(KeyCode.DownArrow) || XInput.Get(XInput.Button.DpadDown) ? keyboardControllerVelocityZ : -primaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                p1controllerZ.Rotate(0, 0, p1controllerRotateZ);
                p1currentControllerRotationZ -= p1controllerRotateZ;
                inputDetected = true;
            */

            // Check if the primary index trigger on the right controller is pressed
            if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger))
            {
            //    logger.Info("OVR Primary index trigger pressed");
            }
          
            // Thunbstick button pressed
            if (XInput.GetDown(XInput.Button.LThumbstick))
            {
                if (!isschasehqFlashing)
                {
                    // Start the flashing if not already flashing
                    schasehqCoroutine = StartCoroutine(FlashschasehqLights());
                    isschasehqFlashing = true;
                }
                else
                {
                    // Stop the flashing if it's currently active
                    StopCoroutine(schasehqCoroutine);
                    ToggleschasehqLight1(false);
                    ToggleschasehqLight2(false);
                    schasehqCoroutine = null;
                    isschasehqFlashing = false;
                }
                inputDetected = true;
            }
            // shift button pressed
            if (XInput.GetDown(XInput.Button.Y))
            {
                if (!isschasehqinHigh)
                {
                    // Start the flashing if not already flashing
                    schasehqshifter.Rotate(0, 0, 45f);
                    isschasehqinHigh = true;
                }
                else
                {
                    schasehqshifter.Rotate(0, 0, -45f);
                    isschasehqinHigh = false;
                }
                inputDetected = true;
            }
    
            if (!inputDetected)
            {
                CenterRotation();
            }
        }

        void CenterRotation()
        {
            //Centering for contoller 1

            // Center X-Axis Controller rotation
            if (schasehqp1currentControllerRotationX > 0)
            {
                float p1unrotateX = Mathf.Min(schasehqp1centeringControllerVelocityX * Time.deltaTime, schasehqp1currentControllerRotationX);
                schasehqp1controllerX.Rotate(p1unrotateX, 0, 0);   // Rotating to reduce the rotation
                schasehqp1currentControllerRotationX -= p1unrotateX;    // Reducing the positive rotation
            }
            else if (schasehqp1currentControllerRotationX < 0)
            {
                float p1unrotateX = Mathf.Min(schasehqp1centeringControllerVelocityX * Time.deltaTime, -schasehqp1currentControllerRotationX);
                schasehqp1controllerX.Rotate(-p1unrotateX, 0, 0);   // Rotating to reduce the rotation
                schasehqp1currentControllerRotationX += p1unrotateX;    // Reducing the positive rotation
            }

            // Center Y-axis Controller rotation
            if (schasehqp1currentControllerRotationY > 0)
            {
                float p1unrotateY = Mathf.Min(schasehqp1centeringControllerVelocityY * Time.deltaTime, schasehqp1currentControllerRotationY);
                schasehqp1controllerY.Rotate(0, p1unrotateY, 0);   // Rotating to reduce the rotation
                schasehqp1currentControllerRotationY -= p1unrotateY;    // Reducing the positive rotation
            }
            else if (schasehqp1currentControllerRotationY < 0)
            {
                float p1unrotateY = Mathf.Min(schasehqp1centeringControllerVelocityY * Time.deltaTime, -schasehqp1currentControllerRotationY);
                schasehqp1controllerY.Rotate(0, -p1unrotateY, 0);  // Rotating to reduce the rotation
                schasehqp1currentControllerRotationY += p1unrotateY;    // Reducing the negative rotation
            }


            // Center Z-axis Controller rotation
            if (schasehqp1currentControllerRotationZ > 0)
            {
                float p1unrotateZ = Mathf.Min(schasehqp1centeringControllerVelocityZ * Time.deltaTime, schasehqp1currentControllerRotationZ);
                schasehqp1controllerZ.Rotate(0, 0, p1unrotateZ);   // Rotating to reduce the rotation
                schasehqp1currentControllerRotationZ -= p1unrotateZ;    // Reducing the positive rotation
            }
            else if (schasehqp1currentControllerRotationZ < 0)
            {
                float p1unrotateZ = Mathf.Min(schasehqp1centeringControllerVelocityZ * Time.deltaTime, -schasehqp1currentControllerRotationZ);
                schasehqp1controllerZ.Rotate(0, 0, -p1unrotateZ);   // Rotating to reduce the rotation
                schasehqp1currentControllerRotationZ += p1unrotateZ;    // Reducing the positive rotation
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

        // Method to toggle the lights
        void ToggleschasehqLights(bool isActive)
        {
            for (int i = 0; i < lights.Length; i++)
            {
                lights[i].enabled = isActive;
            }

            logger.Info($"Lights turned {(isActive ? "on" : "off")}.");
        }

        IEnumerator FlashschasehqLights()
        {
            int currentIndex = 0; // Start with the first light in the array

            while (true)
            {
                // Select the current light
                Light light = schasehqLights[currentIndex];

                // Check if the light is not null
                if (light != null)
                {
                    // Log the chosen light
                    // logger.Debug($"Flashing {light.name}");

                    // Turn on the chosen light
                    ToggleschasehqLight(light, true);

                    // Wait for the flash duration
                    yield return new WaitForSeconds(flashDuration);

                    // Turn off the chosen light
                    ToggleschasehqLight(light, false);

                    // Wait for the next flash interval
                    yield return new WaitForSeconds(flashInterval - flashDuration);
                }
                else
                {
                    logger.Debug("Light is null.");
                }

                // Move to the next light in the array
                currentIndex = (currentIndex + 1) % schasehqLights.Length;
            }
        }

        void ToggleschasehqLight(Light light, bool isActive)
        {
            if (light != null)
            {
                light.enabled = isActive;
                // logger.Info($"{light.name} light turned {(isActive ? "on" : "off")}.");
            }
            else
            {
                logger.Debug($"{light?.name} light component is not found.");
            }
        }

        void ToggleschasehqLight1(bool isActive)
        {
            ToggleschasehqLight(schasehq1_light, isActive);
        }

        void ToggleschasehqLight2(bool isActive)
        {
            ToggleschasehqLight(schasehq2_light, isActive);
        }

    }
}
