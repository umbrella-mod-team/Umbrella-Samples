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

namespace WIGUx.Modules.chasehq2Sim
{
    public class chasehq2SimController : MonoBehaviour
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

        private float chasehq2controllerrotationLimitX = 270f;  // Rotation limit for X-axis (stick or wheel)
        private float chasehq2controllerrotationLimitY = 0f;  // Rotation limit for Y-axis (stick or wheel)
        private float chasehq2controllerrotationLimitZ = 0f;  // Rotation limit for Z-axis (stick or wheel)

        private float chasehq2currentControllerRotationX = 0f;  // Current rotation for X-axis (stick or wheel)
        private float chasehq2currentControllerRotationY = 0f;  // Current rotation for Y-axis (stick or wheel)
        private float chasehq2currentControllerRotationZ = 0f;  // Current rotation for Z-axis (stick or wheel)

        private readonly float chasehq2centeringControllerVelocityX = 400.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float chasehq2centeringControllerVelocityY = 400.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float chasehq2centeringControllerVelocityZ = 400.5f;  // Velocity for centering rotation (stick or wheel)

        private Transform chasehq2shifter; // Reference to shifter
        private Vector3 chasehq2shifterStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion chasehq2shifterStartRotation; // Initial controlller positions and rotations for resetting
        private Transform chasehq2controllerX; // Reference to the main animated controller (wheel)
        private Vector3 chasehq2controllerXStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion chasehq2controllerXStartRotation; // Initial controlller positions and rotations for resetting
        private Transform chasehq2controllerY; // Reference to the main animated controller (wheel)
        private Vector3 chasehq2controllerYStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion chasehq2controllerYStartRotation; // Initial controlller positions and rotations for resetting
        private Transform chasehq2controllerZ; // Reference to the main animated controller (wheel)
        private Vector3 chasehq2controllerZStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion chasehq2controllerZStartRotation; // Initial controlller positions and rotations for resetting

        //lights
        private Transform lightsObject;
        public Light[] chasehq2Lights = new Light[4]; // Array to store lights
        public Light chasehq21_light;
        public Light chasehq22_light;
        public Light chasehq23_light;
        public Light chasehq24_light;
        private float flashDuration = 0.15f;
        private float flashInterval = 0.05f;
        private float lightDuration = 0.25f; // Duration during which the lights will be on
        private bool arechasehq2LighsOn = false; // track strobe lights
        private Coroutine chasehq2Coroutine; // Coroutine variable to control the strobe flashing
        private Light[] lights;  
        private bool isChaseHq2Flashing = false; //set the flashing flag
        private bool isChaseHq2inHigh = false; //set the flashing flag
        private bool inFocusMode = false;  // Flag to track focus mode state
        private readonly string[] compatibleGames = { "Chase H.Q. 2" };
        private Dictionary<GameObject, Transform> originalParents = new Dictionary<GameObject, Transform>();  // Dictionary to store original parents of objects
                                                                                                              // Public property to access the Game instance
        void Start()
        {
            logger.Info("Looking For Lights In Chase H.Q. 2");
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
                    case "chasehq21_light":
                        chasehq21_light = light;
                        chasehq2Lights[0] = light;
                        logger.Info("Included Light found in object: " + light.gameObject.name);
                        break;
                    case "chasehq22_light":
                        chasehq22_light = light;
                        chasehq2Lights[1] = light;
                        logger.Info("Included Light found in object: " + light.gameObject.name);
                        break;
                    case "chasehq23_light":
                        chasehq23_light = light;
                        chasehq2Lights[2] = light;
                        logger.Info("Included Light found in object: " + light.gameObject.name);
                        break;
                    case "chasehq24_light":
                        chasehq24_light = light;
                        chasehq2Lights[3] = light;
                        logger.Info("Included Light found in object: " + light.gameObject.name);
                        break;
                    default:
                        logger.Info("Excluded Light found in object: " + light.gameObject.name);
                        break;
                }
            }

            // Log the assigned lights for verification
            for (int i = 0; i < chasehq2Lights.Length; i++)
            {
                if (chasehq2Lights[i] != null)
                {
                    logger.Info($"chasehq2Lights[{i}] assigned to: {chasehq2Lights[i].name}");
                }
                else
                {
                    logger.Error($"chasehq2Lights[{i}] is not assigned!");
                }
            }

            chasehq2shifter = transform.Find("chasehq2shifter");
            if (chasehq2shifter != null)
            {
                logger.Info("chasehq2shifter object found.");
                chasehq2shifterStartPosition = chasehq2shifter.transform.position;
                chasehq2shifterStartRotation = chasehq2shifter.transform.rotation;
            }
            // Find chasehqcontrollerX for player 1
            chasehq2controllerX = transform.Find("chasehq2controllerX");
            if (chasehq2controllerX != null)
            {
                logger.Info("chasehq2controllerX object found.");
                // Store initial position and rotation of the stick
                chasehq2controllerXStartPosition = chasehq2controllerX.transform.position;
                chasehq2controllerXStartRotation = chasehq2controllerX.transform.rotation;

                // Find chasehq2controllerY under chasehq2controllerX
                chasehq2controllerY = chasehq2controllerX.Find("chasehq2controllerY");
                if (chasehq2controllerY != null)
                {
                    logger.Info("chasehq2controllerY object found.");
                    // Store initial position and rotation of the stick
                    chasehq2controllerYStartPosition = chasehq2controllerY.transform.position;
                    chasehq2controllerYStartRotation = chasehq2controllerY.transform.rotation;

                    // Find chasehq2controllerZ under chasehq2controllerY
                    chasehq2controllerZ = chasehq2controllerY.Find("chasehq2controllerZ");
                    if (chasehq2controllerZ != null)
                    {
                        logger.Info("chasehq2controllerZ object found.");
                        // Store initial position and rotation of the stick
                        chasehq2controllerZStartPosition = chasehq2controllerZ.transform.position;
                        chasehq2controllerZStartRotation = chasehq2controllerZ.transform.rotation;
                    }
                    else
                    {
                        logger.Error("chasehq2controllerZ object not found under chasehq2controllerY!");
                    }
                }
                else
                {
                    logger.Error("chasehq2controllerY object not found under chasehq2controllerX!");
                }
            }
            else
            {
                logger.Error("chasehq2controllerX object not found!!");
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
            logger.Info("Compatible Rom Dectected, Ready to Chase AGAIN!... ");
            logger.Info("Chase HQ 2 Module starting...");
            logger.Info("Be on the Lookout!...");
            // Reset controllers to initial positions and rotations
            if (chasehq2controllerX != null)
            {
                chasehq2controllerX.position = chasehq2controllerXStartPosition;
                chasehq2controllerX.rotation = chasehq2controllerXStartRotation;
            }
            if (chasehq2controllerY != null)
            {
                chasehq2controllerY.position = chasehq2controllerYStartPosition;
                chasehq2controllerY.rotation = chasehq2controllerYStartRotation;
            }
            if (chasehq2controllerZ != null)
            {
                chasehq2controllerZ.position = chasehq2controllerZStartPosition;
                chasehq2controllerZ.rotation = chasehq2controllerZStartRotation;
            }
            if (chasehq2shifter != null)
            {
                chasehq2shifter.position = chasehq2shifterStartPosition;
                chasehq2shifter.rotation = chasehq2shifterStartRotation;
            }


            // Reset rotation allowances and current rotation values
            //player 1
            chasehq2currentControllerRotationX = 0f;
            chasehq2currentControllerRotationY = 0f;
            chasehq2currentControllerRotationZ = 0f;
            //player 2

            inFocusMode = true;  // Set focus mode flag
        }

        void EndFocusMode()
        {
            // Reset controllers to initial positions and rotations
            if (chasehq2controllerX != null)
            {
                chasehq2controllerX.position = chasehq2controllerXStartPosition;
                chasehq2controllerX.rotation = chasehq2controllerXStartRotation;
            }
            if (chasehq2controllerY != null)
            {
                chasehq2controllerY.position = chasehq2controllerYStartPosition;
                chasehq2controllerY.rotation = chasehq2controllerYStartRotation;
            }
            if (chasehq2controllerZ != null)
            {
                chasehq2controllerZ.position = chasehq2controllerZStartPosition;
                chasehq2controllerZ.rotation = chasehq2controllerZStartRotation;
            }

            StopCoroutine(chasehq2Coroutine);
            Togglechasehq2Light1(false);
            Togglechasehq2Light2(false);

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
                        if (!isChaseHq2Flashing)
                        {
                            // Start the flashing if not already flashing
                            chasehq2Coroutine = StartCoroutine(Flashchasehq2Lights());
                            isChaseHq2Flashing = true;
                        }
                        else
                        {
                            // Stop the flashing if it's currently active
                            StopCoroutine(chasehq2Coroutine);
                            Togglechasehq2Light1(false);
                            Togglechasehq2Light2(false);
                            Togglechasehq2Light3(false);
                            Togglechasehq2Light4(false);
                            chasehq2Coroutine = null;
                            isChaseHq2Flashing = false;
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
                        if (!isChaseHq2inHigh)
                        {
                            // Start the flashing if not already flashing
                            chasehq2shifter.Rotate(0, 0, 45f);
                            isChaseHq2inHigh = true;
                        }
                        else
                        {
                            chasehq2shifter.Rotate(0, 0, -45f);
                            isChaseHq2inHigh = false;
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
                    if (!isChaseHq2inHigh)
                    {
                        // Start the flashing if not already flashing
                        chasehq2shifter.Rotate(0, 0, 45f);
                        isChaseHq2inHigh = true;
                    }
                    else
                    {
                        chasehq2shifter.Rotate(0, 0, -45f);
                        isChaseHq2inHigh = false;
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
                if ((Input.GetKey(KeyCode.RightArrow) || XInput.Get(XInput.Button.DpadRight) || primaryThumbstick.x > 0) && chasehq2currentControllerRotationX < chasehq2controllerrotationLimitX)
            {
                float p1controllerRotateX = (Input.GetKey(KeyCode.RightArrow) || XInput.Get(XInput.Button.DpadRight) ? keyboardControllerVelocityX : primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                chasehq2controllerX.Rotate(-p1controllerRotateX, 0, 0);
                chasehq2currentControllerRotationX += p1controllerRotateX;
                inputDetected = true;
            }
            // Thumbstick direction: left
            if ((Input.GetKey(KeyCode.LeftArrow) || XInput.Get(XInput.Button.DpadLeft) || primaryThumbstick.x < 0) && chasehq2currentControllerRotationX > -chasehq2controllerrotationLimitX)
            {
                float p1controllerRotateX = (Input.GetKey(KeyCode.LeftArrow) || XInput.Get(XInput.Button.DpadLeft) ? keyboardControllerVelocityX : -primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                chasehq2controllerX.Rotate(p1controllerRotateX, 0, 0);
                chasehq2currentControllerRotationX -= p1controllerRotateX;
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
                if (!isChaseHq2Flashing)
                {
                    // Start the flashing if not already flashing
                    chasehq2Coroutine = StartCoroutine(Flashchasehq2Lights());
                    isChaseHq2Flashing = true;
                }
                else
                {
                    // Stop the flashing if it's currently active
                    StopCoroutine(chasehq2Coroutine);
                    Togglechasehq2Light1(false);
                    Togglechasehq2Light2(false);
                    Togglechasehq2Light3(false);
                    Togglechasehq2Light4(false);
                    chasehq2Coroutine = null;
                    isChaseHq2Flashing = false;
                }
                inputDetected = true;
            }
            // shift button pressed
            if (XInput.GetDown(XInput.Button.Y))
            {
                if (!isChaseHq2inHigh)
                {
                    // Start the flashing if not already flashing
                    chasehq2shifter.Rotate(0, 0, 45f);
                    isChaseHq2inHigh = true;
                }
                else
                {
                    chasehq2shifter.Rotate(0, 0, -45f);
                    isChaseHq2inHigh = false;
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
            if (chasehq2currentControllerRotationX > 0)
            {
                float p1unrotateX = Mathf.Min(chasehq2centeringControllerVelocityX * Time.deltaTime, chasehq2currentControllerRotationX);
                chasehq2controllerX.Rotate(p1unrotateX, 0, 0);   // Rotating to reduce the rotation
                chasehq2currentControllerRotationX -= p1unrotateX;    // Reducing the positive rotation
            }
            else if (chasehq2currentControllerRotationX < 0)
            {
                float p1unrotateX = Mathf.Min(chasehq2centeringControllerVelocityX * Time.deltaTime, -chasehq2currentControllerRotationX);
                chasehq2controllerX.Rotate(-p1unrotateX, 0, 0);   // Rotating to reduce the rotation
                chasehq2currentControllerRotationX += p1unrotateX;    // Reducing the positive rotation
            }

            // Center Y-axis Controller rotation
            if (chasehq2currentControllerRotationY > 0)
            {
                float p1unrotateY = Mathf.Min(chasehq2centeringControllerVelocityY * Time.deltaTime, chasehq2currentControllerRotationY);
                chasehq2controllerY.Rotate(0, p1unrotateY, 0);   // Rotating to reduce the rotation
                chasehq2currentControllerRotationY -= p1unrotateY;    // Reducing the positive rotation
            }
            else if (chasehq2currentControllerRotationY < 0)
            {
                float p1unrotateY = Mathf.Min(chasehq2centeringControllerVelocityY * Time.deltaTime, -chasehq2currentControllerRotationY);
                chasehq2controllerY.Rotate(0, -p1unrotateY, 0);  // Rotating to reduce the rotation
                chasehq2currentControllerRotationY += p1unrotateY;    // Reducing the negative rotation
            }


            // Center Z-axis Controller rotation
            if (chasehq2currentControllerRotationZ > 0)
            {
                float p1unrotateZ = Mathf.Min(chasehq2centeringControllerVelocityZ * Time.deltaTime, chasehq2currentControllerRotationZ);
                chasehq2controllerZ.Rotate(0, 0, p1unrotateZ);   // Rotating to reduce the rotation
                chasehq2currentControllerRotationZ -= p1unrotateZ;    // Reducing the positive rotation
            }
            else if (chasehq2currentControllerRotationZ < 0)
            {
                float p1unrotateZ = Mathf.Min(chasehq2centeringControllerVelocityZ * Time.deltaTime, -chasehq2currentControllerRotationZ);
                chasehq2controllerZ.Rotate(0, 0, -p1unrotateZ);   // Rotating to reduce the rotation
                chasehq2currentControllerRotationZ += p1unrotateZ;    // Reducing the positive rotation
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
        void Togglechasehq2Lights(bool isActive)
        {
            for (int i = 0; i < lights.Length; i++)
            {
                lights[i].enabled = isActive;
            }

            logger.Info($"Lights turned {(isActive ? "on" : "off")}.");
        }

        IEnumerator Flashchasehq2Lights()
        {
            int currentIndex = 0; // Start with the first light in the array

            while (true)
            {
                // Select the current light
                Light light = chasehq2Lights[currentIndex];

                // Check if the light is not null
                if (light != null)
                {
                    // Log the chosen light
                    // logger.Debug($"Flashing {light.name}");

                    // Turn on the chosen light
                    Togglechasehq2Light(light, true);

                    // Wait for the flash duration
                    yield return new WaitForSeconds(flashDuration);

                    // Turn off the chosen light
                    Togglechasehq2Light(light, false);

                    // Wait for the next flash interval
                    yield return new WaitForSeconds(flashInterval - flashDuration);
                }
                else
                {
                    logger.Debug("Light is null.");
                }

                // Move to the next light in the array
                currentIndex = (currentIndex + 1) % chasehq2Lights.Length;
            }
        }

        void Togglechasehq2Light(Light light, bool isActive)
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

        void Togglechasehq2Light1(bool isActive)
        {
            Togglechasehq2Light(chasehq21_light, isActive);
        }

        void Togglechasehq2Light2(bool isActive)
        {
            Togglechasehq2Light(chasehq22_light, isActive);
        }
        void Togglechasehq2Light3(bool isActive)
        {
            Togglechasehq2Light(chasehq23_light, isActive);
        }
        void Togglechasehq2Light4(bool isActive)
        {
            Togglechasehq2Light(chasehq24_light, isActive);
        }

    }
}
