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

namespace WIGUx.Modules.chasehqSim
{
    public class chasehqSimController : MonoBehaviour
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

        private float p1controllerrotationLimitX = 270f;  // Rotation limit for X-axis (stick or wheel)
        private float p1controllerrotationLimitY = 0f;  // Rotation limit for Y-axis (stick or wheel)
        private float p1controllerrotationLimitZ = 0f;  // Rotation limit for Z-axis (stick or wheel)

        private float p1currentControllerRotationX = 0f;  // Current rotation for X-axis (stick or wheel)
        private float p1currentControllerRotationY = 0f;  // Current rotation for Y-axis (stick or wheel)
        private float p1currentControllerRotationZ = 0f;  // Current rotation for Z-axis (stick or wheel)

        private readonly float p1centeringControllerVelocityX = 400.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float p1centeringControllerVelocityY = 400.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float p1centeringControllerVelocityZ = 400.5f;  // Velocity for centering rotation (stick or wheel)

        private Transform chasehqshifter; // Reference to shifter
        private Transform p1controllerX; // Reference to the main animated controller (wheel)
        private Vector3 p1controllerXStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion p1controllerXStartRotation; // Initial controlller positions and rotations for resetting
        private Transform p1controllerY; // Reference to the main animated controller (wheel)
        private Vector3 p1controllerYStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion p1controllerYStartRotation; // Initial controlller positions and rotations for resetting
        private Transform p1controllerZ; // Reference to the main animated controller (wheel)
        private Vector3 p1controllerZStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion p1controllerZStartRotation; // Initial controlller positions and rotations for resetting
        private Game _game;

        //p1 buttons

        //p1 button movement limits
        private float p1positionLimitstart = 0.003f;  // position limit  for player 1 start button
        private float p1positionLimit1 = 0.003f;  // position limit  for player 1 button 1
        private float p1positionLimit2 = 0.003f;  // position limit  for player 1 button 2
        private float p1positionLimit3 = 0.003f;  // position limit  for player 1 button 3
        private float p1positionLimit4 = 0.003f;  // position limit  for player 1 button 4
        private float p1positionLimit5 = 0.003f;  // position limit  for player 1 button 5
        private float p1positionLimit6 = 0.003f;  // position limit  for player 1 button 6

        private float p1currentStartPosition = 0f;  // Current postion for start button for player 1
        private float p1currentButton1Position = 0f;  // Current position for player 1 button 1
        private float p1currentButton2Position = 0f;  // Current position for player 1 button 2
        private float p1currentButton3Position = 0f;  // Current position for player 1 button 3
        private float p1currentButton4Position = 0f;  // Current position for player 1 button 4
        private float p1currentButton5Position = 0f;  // Current position for player 1 button 5
        private float p1currentButton6Position = 0f;  // Current position for player 1 button 6

        private float p1currentPlungerStartPosition = 0f;  // Current postion for start plunger for player 1
        private float p1currentPlunger1Position = 0f;  // Current position for player 1 plunger 1
        private float p1currentPlunger2Position = 0f;  // Current position for player 1 plunger 2
        private float p1currentPlunger3Position = 0f;  // Current position for player 1 plunger 3
        private float p1currentPlunger4Position = 0f;  // Current position for player 1 plunger 4
        private float p1currentPlunger5Position = 0f;  // Current position for player 1 plunger 5
        private float p1currentPlunger6Position = 0f;  // Current position for player 1 plunger 6

        private Transform p1startObject; // Reference to start button on player 1
        private Transform p1button1Object; // Reference to button1 on player 1
        private Transform p1button2Object; // Reference to button2 on player 1
        private Transform p1button3Object; // Reference to button3 on player 1
        private Transform p1button4Object; // Reference to button4 on player 1
        private Transform p1button5Object; // Reference to button5 on player 1
        private Transform p1button6Object; // Reference to button6 on player 1
        private Transform p1startPlunger; // Reference to start button on player 1
        private Transform p1plunger1Object; // Reference to plunger1 on player 1
        private Transform p1plunger2Object; // Reference to plunger2 on player 1
        private Transform p1plunger3Object; // Reference to plunger3 on player 1
        private Transform p1plunger4Object; // Reference to plunger4 on player 1
        private Transform p1plunger5Object; // Reference to plunger5 on player 1
        private Transform p1plunger6Object; // Reference to plunger6 on player 1

        private Vector3 p1startObjectStartPosition;
        private Quaternion p1startObjectStartRotation;
        private Vector3 p1button1ObjectStartPosition;
        private Quaternion p1button1ObjectStartRotation;
        private Vector3 p1button2ObjectStartPosition;
        private Quaternion p1button2ObjectStartRotation;
        private Vector3 p1button3ObjectStartPosition;
        private Quaternion p1button3ObjectStartRotation;
        private Vector3 p1button4ObjectStartPosition;
        private Quaternion p1button4ObjectStartRotation;
        private Vector3 p1button5ObjectStartPosition;
        private Quaternion p1button5ObjectStartRotation;
        private Vector3 p1button6ObjectStartPosition;
        private Quaternion p1button6ObjectStartRotation;
        private Vector3 p1startPlungerStartPosition;
        private Quaternion p1startPlungerStartRotation;
        private Vector3 p1plunger1ObjectStartPosition;
        private Quaternion p1plunger1ObjectStartRotation;
        private Vector3 p1plunger2ObjectStartPosition;
        private Quaternion p1plunger2ObjectStartRotation;
        private Vector3 p1plunger3ObjectStartPosition;
        private Quaternion p1plunger3ObjectStartRotation;
        private Vector3 p1plunger4ObjectStartPosition;
        private Quaternion p1plunger4ObjectStartRotation;
        private Vector3 p1plunger5ObjectStartPosition;
        private Quaternion p1plunger5ObjectStartRotation;
        private Vector3 p1plunger6ObjectStartPosition;
        private Quaternion p1plunger6ObjectStartRotation;

        //player 2

        //p2 sticks
        private float p2controllerrotationLimitX = 10f;  // Rotation limit for X-axis (stick or wheel)
        private float p2controllerrotationLimitY = 10f;  // Rotation limit for Y-axis (stick or wheel)
        private float p2controllerrotationLimitZ = 10f;  // Rotation limit for Z-axis (stick or wheel)

        private float p2currentControllerRotationX = 0f;  // Current rotation for X-axis (stick or wheel)
        private float p2currentControllerRotationY = 0f;  // Current rotation for Y-axis (stick or wheel)
        private float p2currentControllerRotationZ = 0f;  // Current rotation for Z-axis (stick or wheel)

        private readonly float p2centeringControllerVelocityX = 50.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float p2centeringControllerVelocityY = 50.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float p2centeringControllerVelocityZ = 50.5f;  // Velocity for centering rotation (stick or wheel)

        private Transform p2controllerX; // Reference to the main animated controller (wheel)
        private Vector3 p2controllerXStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion p2controllerXStartRotation; // Initial controlller positions and rotations for resetting
        private Transform p2controllerY; // Reference to the main animated controller (wheel)
        private Vector3 p2controllerYStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion p2controllerYStartRotation; // Initial controlller positions and rotations for resetting
        private Transform p2controllerZ; // Reference to the main animated controller (wheel)
        private Vector3 p2controllerZStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion p2controllerZStartRotation; // Initial controlller positions and rotations for resetting

        //p1 buttons

        //p2 button movement limits

        private float p2positionLimitstart = .003f;  // position limit  for player 2 start button
        private float p2positionLimit1 = 0.003f;  // position limit for player 2 button 1
        private float p2positionLimit2 = 0.003f;  // position limit for player 2 button 2
        private float p2positionLimit3 = 0.003f;  // position limit for player 2 button 3
        private float p2positionLimit4 = 0.003f;  // position limit for player 2 button 4
        private float p2positionLimit5 = 0.003f;  // position limit for player 2 button 5
        private float p2positionLimit6 = 0.003f;  // position limit for player 2 button 6

        private float p2currentStartPosition = 0f;  // Current postion for start button for player 2
        private float p2currentButton1Position = 0f;  // Current position for player 2 button 1
        private float p2currentButton2Position = 0f;  // Current position for player 2 button 2
        private float p2currentButton3Position = 0f;  // Current position for player 2 button 3
        private float p2currentButton4Position = 0f;  // Current position for player 2 button 4
        private float p2currentButton5Position = 0f;  // Current position for player 2 button 5
        private float p2currentButton6Position = 0f;  // Current position for player 2 button 6

        private float p2currentPlungerStartPosition = 0f;  // Current postion for start plunger for player 2
        private float p2currentPlunger1Position = 0f;  // Current position for player 2 plunger 1
        private float p2currentPlunger2Position = 0f;  // Current position for player 2 plunger 2
        private float p2currentPlunger3Position = 0f;  // Current position for player 2 plunger 3
        private float p2currentPlunger4Position = 0f;  // Current position for player 2 plunger 4
        private float p2currentPlunger5Position = 0f;  // Current position for player 2 plunger 5
        private float p2currentPlunger6Position = 0f;  // Current position for player 2 plunger 6

        private Transform p2startObject; // Reference to start button on player 2
        private Transform p2button1Object; // Reference to button1 on player 2
        private Transform p2button2Object; // Reference to button2 on player 2
        private Transform p2button3Object; // Reference to button3 on player 2
        private Transform p2button4Object; // Reference to button4 on player 2
        private Transform p2button5Object; // Reference to button5 on player 2
        private Transform p2button6Object; // Reference to button6 on player 2
        private Transform p2startPlunger; // Reference to start button on player 2
        private Transform p2plunger1Object; // Reference to plunger1 on player 2
        private Transform p2plunger2Object; // Reference to plunger2 on player 2
        private Transform p2plunger3Object; // Reference to plunger3 on player 2
        private Transform p2plunger4Object; // Reference to plunger4 on player 2
        private Transform p2plunger5Object; // Reference to plunger5 on player 2
        private Transform p2plunger6Object; // Reference to plunger6 on player 2

        private Vector3 p2startObjectStartPosition;
        private Quaternion p2startObjectStartRotation;
        private Vector3 p2button1ObjectStartPosition;
        private Quaternion p2button1ObjectStartRotation;
        private Vector3 p2button2ObjectStartPosition;
        private Quaternion p2button2ObjectStartRotation;
        private Vector3 p2button3ObjectStartPosition;
        private Quaternion p2button3ObjectStartRotation;
        private Vector3 p2button4ObjectStartPosition;
        private Quaternion p2button4ObjectStartRotation;
        private Vector3 p2button5ObjectStartPosition;
        private Quaternion p2button5ObjectStartRotation;
        private Vector3 p2button6ObjectStartPosition;
        private Quaternion p2button6ObjectStartRotation;
        private Vector3 p2startPlungerStartPosition;
        private Quaternion p2startPlungerStartRotation;
        private Vector3 p2plunger1ObjectStartPosition;
        private Quaternion p2plunger1ObjectStartRotation;
        private Vector3 p2plunger2ObjectStartPosition;
        private Quaternion p2plunger2ObjectStartRotation;
        private Vector3 p2plunger3ObjectStartPosition;
        private Quaternion p2plunger3ObjectStartRotation;
        private Vector3 p2plunger4ObjectStartPosition;
        private Quaternion p2plunger4ObjectStartRotation;
        private Vector3 p2plunger5ObjectStartPosition;
        private Quaternion p2plunger5ObjectStartRotation;
        private Vector3 p2plunger6ObjectStartPosition;
        private Quaternion p2plunger6ObjectStartRotation;

        //lights
        private Transform lightsObject;
        public Light[] chasehqLights = new Light[2]; // Array to store lights
        public Light chasehq1_light;
        public Light chasehq2_light;
        public Light chasehq3_light;
        public Light chasehq4_light;
        private float flashDuration = 0.15f;
        private float flashInterval = 0.15f;
        private float lightDuration = 0.5f; // Duration during which the lights will be on
        private bool arechasehqLighsOn = false; // track strobe lights
        private Coroutine chasehqCoroutine; // Coroutine variable to control the strobe flashing
        public string fire1Button = "Fire1"; // Name of the fire button
        public string fire2Button = "Fire2"; // Name of the fire button 
        public string fire3Button = "Fire3"; // Name of the fire button 
        public string JumpButton = "Jump"; // Name of the fire button 
        public string LBButton = "LB"; // Name of the fire button 
        public string RBButton = "RB"; // Name of the fire button 
        public string StartButton = "Start"; // Name of the fire button 
        private Light[] lights;  
        private bool isChaseHqFlashing = false; //set the flashing flag
        private bool isChaseHqinHigh = false; //set the flashing flag
        private bool inFocusMode = false;  // Flag to track focus mode state
        private readonly string[] compatibleGames = { "chasehq" };
        private Dictionary<GameObject, Transform> originalParents = new Dictionary<GameObject, Transform>();  // Dictionary to store original parents of objects
                                                                                                              // Public property to access the Game instance
        void Start()
        {
            logger.Info("Looking For Lights In Chase HQ");
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

            chasehqshifter = transform.Find("p1shifter");
            // Gets all Light components in the target object and its children
            Light[] allLights = lightsObject.GetComponentsInChildren<Light>();

            // Log the names of the objects containing the Light components and filter out unwanted lights
            foreach (Light light in allLights)
            {
                logger.Info($"Light found: {light.gameObject.name}");
                switch (light.gameObject.name)
                {
                    case "chasehq1_light":
                        chasehq1_light = light;
                        chasehqLights[0] = light;
                        logger.Info("Included Light found in object: " + light.gameObject.name);
                        break;
                    case "chasehq2_light":
                        chasehq2_light = light;
                        chasehqLights[1] = light;
                        logger.Info("Included Light found in object: " + light.gameObject.name);
                        break;
                    default:
                        logger.Info("Excluded Light found in object: " + light.gameObject.name);
                        break;
                }
            }

            // Log the assigned lights for verification
            for (int i = 0; i < chasehqLights.Length; i++)
            {
                if (chasehqLights[i] != null)
                {
                    logger.Info($"chasehqLights[{i}] assigned to: {chasehqLights[i].name}");
                }
                else
                {
                    logger.Error($"chasehqLights[{i}] is not assigned!");
                }
            }

            // Find controllerX for player 1
            p1controllerX = transform.Find("p1controllerX");
            if (p1controllerX != null)
            {
                logger.Info("p1controllerX object found.");
                // Store initial position and rotation of the stick
                p1controllerXStartPosition = p1controllerX.transform.position;
                p1controllerXStartRotation = p1controllerX.transform.rotation;

                // Find p1controllerY under p1controllerX
                p1controllerY = p1controllerX.Find("p1controllerY");
                if (p1controllerY != null)
                {
                    logger.Info("p1controllerY object found.");
                    // Store initial position and rotation of the stick
                    p1controllerYStartPosition = p1controllerY.transform.position;
                    p1controllerYStartRotation = p1controllerY.transform.rotation;

                    // Find p1controllerZ under p1controllerY
                    p1controllerZ = p1controllerY.Find("p1controllerZ");
                    if (p1controllerZ != null)
                    {
                        logger.Info("p1controllerZ object found.");
                        // Store initial position and rotation of the stick
                        p1controllerZStartPosition = p1controllerZ.transform.position;
                        p1controllerZStartRotation = p1controllerZ.transform.rotation;
                    }
                    else
                    {
                        logger.Error("p1controllerZ object not found under controllerY!");
                    }
                }
                else
                {
                    logger.Error("p1controllerY object not found under controllerX!");
                }
            }
            else
            {
                logger.Error("p1controllerX object not found!!");
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
            // Keyboard Input Handling
            if (Input.GetKeyDown(KeyCode.W))
            {
                logger.Info("W key pressed");
            }
            if (Input.GetKeyDown(KeyCode.A))
            {
                logger.Info("A key pressed");
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                logger.Info("S key pressed");
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                logger.Info("D key pressed");
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                logger.Info("Spacebar pressed");
            }
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                logger.Info("Left Shift pressed");
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                logger.Info("Escape key pressed");
            }
        }

        private void HandleVRInput()
        {
            var leftController = VRControllerHelper.GetController(HandType.Left);
            var rightController = VRControllerHelper.GetController(HandType.Right);

            // Left Controller Input
            if (leftController != null)
            {
                if (leftController.GetPressDown(EVRButtonId.k_EButton_SteamVR_Trigger))
                {
                    logger.Info("Left Trigger pressed");
                }
                if (leftController.GetPressDown(EVRButtonId.k_EButton_SteamVR_Touchpad))
                {
                    logger.Info("Left Touchpad pressed");
                }
                if (leftController.GetPressDown(EVRButtonId.k_EButton_Grip))
                {
                    logger.Info("Left Grip pressed");
                }
                if (leftController.GetPressDown(EVRButtonId.k_EButton_ApplicationMenu))
                {
                    logger.Info("Left Application Menu pressed");
                }
            }

            // Right Controller Input
            if (rightController != null)
            {
                if (rightController.GetPressDown(EVRButtonId.k_EButton_SteamVR_Trigger))
                {
                    logger.Info("Right Trigger pressed");
                }
                if (rightController.GetPressDown(EVRButtonId.k_EButton_SteamVR_Touchpad))
                {
                    logger.Info("Right Touchpad pressed");
                }
                if (rightController.GetPressDown(EVRButtonId.k_EButton_Grip))
                {
                    logger.Info("Right Grip pressed");
                }
                if (rightController.GetPressDown(EVRButtonId.k_EButton_ApplicationMenu))
                {
                    logger.Info("Right Application Menu pressed");
                }
            }
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
            logger.Info("Module starting...");

            // Reset controllers to initial positions and rotations
            if (p1controllerX != null)
            {
                p1controllerX.position = p1controllerXStartPosition;
                p1controllerX.rotation = p1controllerXStartRotation;
            }
            if (p1controllerY != null)
            {
                p1controllerY.position = p1controllerYStartPosition;
                p1controllerY.rotation = p1controllerYStartRotation;
            }
            if (p1controllerZ != null)
            {
                p1controllerZ.position = p1controllerZStartPosition;
                p1controllerZ.rotation = p1controllerZStartRotation;
            }
            // Reset controller2 to initial positions and rotations
            if (p2controllerX != null)
            {
                p2controllerX.position = p2controllerXStartPosition;
                p2controllerX.rotation = p2controllerXStartRotation;
            }
            if (p2controllerY != null)
            {
                p2controllerY.position = p2controllerYStartPosition;
                p2controllerY.rotation = p2controllerYStartRotation;
            }
            if (p2controllerZ != null)
            {
                p2controllerZ.position = p2controllerZStartPosition;
                p2controllerZ.rotation = p2controllerZStartRotation;
            }
            //Buttons

            // Reset ps1startObject object to initial position and rotation
            if (p1startObject != null)
            {
                p1startObject.position = p1startObjectStartPosition;
                p1startObject.rotation = p1startObjectStartRotation;
            }
            // Reset p1button1Object to initial positions and rotations
            if (p1button1Object != null)
            {
                p1button1Object.position = p1button1ObjectStartPosition;
                p1button1Object.rotation = p1button1ObjectStartRotation;
            }
            // Reset p1button2Object to initial positions and rotations
            if (p1button1Object != null)
            {
                p1button2Object.position = p1button2ObjectStartPosition;
                p1button2Object.rotation = p1button2ObjectStartRotation;
            }
            // Reset p1button3Object to initial positions and rotations
            if (p1button3Object != null)
            {
                p1button3Object.position = p1button3ObjectStartPosition;
                p1button3Object.rotation = p1button3ObjectStartRotation;
            }
            // Reset p1button4Object to initial positions and rotations
            if (p1button4Object != null)
            {
                p1button4Object.position = p1button4ObjectStartPosition;
                p1button4Object.rotation = p1button4ObjectStartRotation;
            }
            // Reset p1button5Object to initial positions and rotations
            if (p1button5Object != null)
            {
                p1button5Object.position = p1button5ObjectStartPosition;
                p1button5Object.rotation = p1button5ObjectStartRotation;
            }
            // Reset p1button6Object to initial positions and rotations
            if (p1button6Object != null)
            {
                p1button6Object.position = p1button6ObjectStartPosition;
                p1button6Object.rotation = p1button6ObjectStartRotation;
            }

            // Reset ps2startObject object to initial position and rotation
            if (p2startObject != null)
            {
                p2startObject.position = p2startObjectStartPosition;
                p2startObject.rotation = p2startObjectStartRotation;
            }
            // Reset p1button1Object to initial positions and rotations
            if (p2button1Object != null)
            {
                p2button1Object.position = p2button1ObjectStartPosition;
                p2button1Object.rotation = p2button1ObjectStartRotation;
            }
            // Reset p1button2Object to initial positions and rotations
            if (p2button2Object != null)
            {
                p2button2Object.position = p2button2ObjectStartPosition;
                p2button2Object.rotation = p2button2ObjectStartRotation;
            }
            // Reset p1button3Object to initial positions and rotations
            if (p2button3Object != null)
            {
                p2button3Object.position = p2button3ObjectStartPosition;
                p2button3Object.rotation = p2button3ObjectStartRotation;
            }
            // Reset p1button4Object to initial positions and rotations
            if (p2button4Object != null)
            {
                p2button4Object.position = p2button4ObjectStartPosition;
                p2button4Object.rotation = p2button4ObjectStartRotation;
            }
            // Reset p1button5Object to initial positions and rotations
            if (p2button5Object != null)
            {
                p2button5Object.position = p2button5ObjectStartPosition;
                p2button5Object.rotation = p2button5ObjectStartRotation;
            }
            // Reset p1button6Object to initial positions and rotations
            if (p2button6Object != null)
            {
                p2button6Object.position = p2button6ObjectStartPosition;
                p2button6Object.rotation = p2button6ObjectStartRotation;
            }

            // Reset buttons current values
            p1currentStartPosition = 0f;
            p1currentButton1Position = 0f;
            p1currentButton2Position = 0f;
            p1currentButton3Position = 0f;
            p1currentButton4Position = 0f;
            p1currentButton5Position = 0f;
            p1currentButton6Position = 0f;
            p1currentStartPosition = 0f;
            p2currentButton1Position = 0f;
            p2currentButton2Position = 0f;
            p2currentButton3Position = 0f;
            p2currentButton4Position = 0f;
            p2currentButton5Position = 0f;
            p2currentButton6Position = 0f;

            // Reset rotation allowances and current rotation values
            //player 1
            p1currentControllerRotationX = 0f;
            p1currentControllerRotationY = 0f;
            p1currentControllerRotationZ = 0f;
            //player 2
            p2currentControllerRotationX = 0f;
            p2currentControllerRotationY = 0f;
            p2currentControllerRotationZ = 0f;

            inFocusMode = true;  // Set focus mode flag
        }

        void EndFocusMode()
        {
            // Reset controllers to initial positions and rotations
            if (p1controllerX != null)
            {
                p1controllerX.position = p1controllerXStartPosition;
                p1controllerX.rotation = p1controllerXStartRotation;
            }
            if (p1controllerY != null)
            {
                p1controllerY.position = p1controllerYStartPosition;
                p1controllerY.rotation = p1controllerYStartRotation;
            }
            if (p1controllerZ != null)
            {
                p1controllerZ.position = p1controllerZStartPosition;
                p1controllerZ.rotation = p1controllerZStartRotation;
            }
            // Reset controller2 to initial positions and rotations
            if (p2controllerX != null)
            {
                p2controllerX.position = p2controllerXStartPosition;
                p2controllerX.rotation = p2controllerXStartRotation;
            }
            if (p2controllerY != null)
            {
                p2controllerY.position = p2controllerYStartPosition;
                p2controllerY.rotation = p2controllerYStartRotation;
            }
            if (p2controllerZ != null)
            {
                p2controllerZ.position = p2controllerZStartPosition;
                p2controllerZ.rotation = p2controllerZStartRotation;
            }
            //Buttons

            // Reset ps1startObject object to initial position and rotation
            if (p1startObject != null)
            {
                p1startObject.position = p1startObjectStartPosition;
                p1startObject.rotation = p1startObjectStartRotation;
            }
            // Reset p1button1Object to initial positions and rotations
            if (p1button1Object != null)
            {
                p1button1Object.position = p1button1ObjectStartPosition;
                p1button1Object.rotation = p1button1ObjectStartRotation;
            }
            // Reset p1button2Object to initial positions and rotations
            if (p1button1Object != null)
            {
                p1button2Object.position = p1button2ObjectStartPosition;
                p1button2Object.rotation = p1button2ObjectStartRotation;
            }
            // Reset p1button3Object to initial positions and rotations
            if (p1button3Object != null)
            {
                p1button3Object.position = p1button3ObjectStartPosition;
                p1button3Object.rotation = p1button3ObjectStartRotation;
            }
            // Reset p1button4Object to initial positions and rotations
            if (p1button4Object != null)
            {
                p1button4Object.position = p1button4ObjectStartPosition;
                p1button4Object.rotation = p1button4ObjectStartRotation;
            }
            // Reset p1button5Object to initial positions and rotations
            if (p1button5Object != null)
            {
                p1button5Object.position = p1button5ObjectStartPosition;
                p1button5Object.rotation = p1button5ObjectStartRotation;
            }
            // Reset p1button6Object to initial positions and rotations
            if (p1button6Object != null)
            {
                p1button6Object.position = p1button6ObjectStartPosition;
                p1button6Object.rotation = p1button6ObjectStartRotation;
            }

            // Reset ps2startObject object to initial position and rotation
            if (p2startObject != null)
            {
                p2startObject.position = p2startObjectStartPosition;
                p2startObject.rotation = p2startObjectStartRotation;
            }
            // Reset p1button1Object to initial positions and rotations
            if (p2button1Object != null)
            {
                p2button1Object.position = p2button1ObjectStartPosition;
                p2button1Object.rotation = p2button1ObjectStartRotation;
            }
            // Reset p1button2Object to initial positions and rotations
            if (p2button2Object != null)
            {
                p2button2Object.position = p2button2ObjectStartPosition;
                p2button2Object.rotation = p2button2ObjectStartRotation;
            }
            // Reset p1button3Object to initial positions and rotations
            if (p2button3Object != null)
            {
                p2button3Object.position = p2button3ObjectStartPosition;
                p2button3Object.rotation = p2button3ObjectStartRotation;
            }
            // Reset p1button4Object to initial positions and rotations
            if (p2button4Object != null)
            {
                p2button4Object.position = p2button4ObjectStartPosition;
                p2button4Object.rotation = p2button4ObjectStartRotation;
            }
            // Reset p1button5Object to initial positions and rotations
            if (p2button5Object != null)
            {
                p2button5Object.position = p2button5ObjectStartPosition;
                p2button5Object.rotation = p2button5ObjectStartRotation;
            }
            // Reset p1button6Object to initial positions and rotations
            if (p2button6Object != null)
            {
                p2button6Object.position = p2button6ObjectStartPosition;
                p2button6Object.rotation = p2button6ObjectStartRotation;
            }

            // Reset buttons current values
            p1currentStartPosition = 0f;
            p1currentButton1Position = 0f;
            p1currentButton2Position = 0f;
            p1currentButton3Position = 0f;
            p1currentButton4Position = 0f;
            p1currentButton5Position = 0f;
            p1currentButton6Position = 0f;
            p1currentStartPosition = 0f;
            p2currentButton1Position = 0f;
            p2currentButton2Position = 0f;
            p2currentButton3Position = 0f;
            p2currentButton4Position = 0f;
            p2currentButton5Position = 0f;
            p2currentButton6Position = 0f;

            StopCoroutine(chasehqCoroutine);
            TogglechasehqLight1(false);
            TogglechasehqLight2(false);

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
                        if (!isChaseHqFlashing)
                        {
                            // Start the flashing if not already flashing
                            chasehqCoroutine = StartCoroutine(FlashchasehqLights());
                            isChaseHqFlashing = true;
                        }
                        else
                        {
                            // Stop the flashing if it's currently active
                            StopCoroutine(chasehqCoroutine);
                            TogglechasehqLight1(false);
                            TogglechasehqLight2(false);
                            chasehqCoroutine = null;
                            isChaseHqFlashing = false;
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
                        if (!isChaseHqinHigh)
                        {
                            // Start the flashing if not already flashing
                            chasehqshifter.Rotate(0, 0, 45f);
                            isChaseHqinHigh = true;
                        }
                        else
                        {
                            chasehqshifter.Rotate(0, 0, -45f);
                            isChaseHqinHigh = false;
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
                    if (!isChaseHqinHigh)
                    {
                        // Start the flashing if not already flashing
                        chasehqshifter.Rotate(0, 0, 45f);
                        isChaseHqinHigh = true;
                    }
                    else
                    {
                        chasehqshifter.Rotate(0, 0, -45f);
                        isChaseHqinHigh = false;
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
                if ((Input.GetKey(KeyCode.RightArrow) || XInput.Get(XInput.Button.DpadRight) || primaryThumbstick.x > 0) && p1currentControllerRotationX < p1controllerrotationLimitX)
            {
                float p1controllerRotateX = (Input.GetKey(KeyCode.RightArrow) || XInput.Get(XInput.Button.DpadRight) ? keyboardControllerVelocityX : primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                p1controllerX.Rotate(-p1controllerRotateX, 0, 0);
                p1currentControllerRotationX += p1controllerRotateX;
                inputDetected = true;
            }
            // Thumbstick direction: left
            if ((Input.GetKey(KeyCode.LeftArrow) || XInput.Get(XInput.Button.DpadLeft) || primaryThumbstick.x < 0) && p1currentControllerRotationX > -p1controllerrotationLimitX)
            {
                float p1controllerRotateX = (Input.GetKey(KeyCode.LeftArrow) || XInput.Get(XInput.Button.DpadLeft) ? keyboardControllerVelocityX : -primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                p1controllerX.Rotate(p1controllerRotateX, 0, 0);
                p1currentControllerRotationX -= p1controllerRotateX;
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

            // p1 start

            // Handle Start button press for plunger position
            if ((XInput.GetDown(XInput.Button.Start) || Input.GetKeyDown(KeyCode.JoystickButton7)) && p1currentStartPosition < p1positionLimitstart)
            {
                float p1startPlungerPosition = (Input.GetKey(KeyCode.X) ? keyboardControllerVelocityX : vrControllerVelocity) * Time.deltaTime;
                p1startPlunger.position += new Vector3(0, -0.003f, 0);
                p1currentStartPosition += p1startPlungerPosition;
                inputDetected = true;
            }

            // Reset position on Start button release
            if (XInput.GetUp(XInput.Button.Start) || Input.GetKeyUp(KeyCode.JoystickButton7))
            {
                p1startPlunger.position = p1startPlungerStartPosition;
                p1currentStartPosition = 0f; // Reset the current position
                inputDetected = true;
            }

            // Fire1
            if (Input.GetButtonDown("Fire1") && p1currentPlunger1Position < p1positionLimit1)
            {
                float p1plunger1ObjectPosition = (Input.GetKey(KeyCode.X) ? keyboardControllerVelocityX : vrControllerVelocity) * Time.deltaTime;
                p1plunger1Object.position += new Vector3(0, -0.003f, 0);
                p1currentPlunger1Position += p1plunger1ObjectPosition;
                inputDetected = true;
            }

            // Reset position on button release
            if (Input.GetButtonUp("Fire1"))
            {
                p1plunger1Object.position = p1plunger1ObjectStartPosition;
                p1currentPlunger1Position = 0f; // Reset the current position
                inputDetected = true;
            }

            // Fire2
            if (Input.GetButtonDown("Fire2") && p1currentPlunger2Position < p2positionLimit1)
            {
                float p1plunger2ObjectPosition = (Input.GetKey(KeyCode.X) ? keyboardControllerVelocityX : vrControllerVelocity) * Time.deltaTime;
                p1plunger2Object.position += new Vector3(0, -0.003f, 0);
                p1currentPlunger2Position += p1plunger2ObjectPosition;
                inputDetected = true;
            }

            // Reset position on button release
            if (Input.GetButtonUp("Fire2"))
            {
                p1plunger2Object.position = p1plunger2ObjectStartPosition;
                p1currentPlunger2Position = 0f; // Reset the current position
                inputDetected = true;
            }

            // Fire3
            if (Input.GetButtonDown("Fire3") && p1currentPlunger3Position < p1positionLimit3)
            {
                float p1plunger3ObjectPosition = (Input.GetKey(KeyCode.X) ? keyboardControllerVelocityX : vrControllerVelocity) * Time.deltaTime;
                p1plunger3Object.position += new Vector3(0, -0.003f, 0);
                p1currentPlunger3Position += p1plunger3ObjectPosition;
                inputDetected = true;
            }

            // Reset position on button release
            if (Input.GetButtonUp("Fire3"))
            {
                p1plunger3Object.position = p1plunger3ObjectStartPosition;
                p1currentPlunger3Position = 0f; // Reset the current position
                inputDetected = true;
            }
            // Thunbstick button pressed
            if (XInput.GetDown(XInput.Button.LThumbstick))
            {
                if (!isChaseHqFlashing)
                {
                    // Start the flashing if not already flashing
                    chasehqCoroutine = StartCoroutine(FlashchasehqLights());
                    isChaseHqFlashing = true;
                }
                else
                {
                    // Stop the flashing if it's currently active
                    StopCoroutine(chasehqCoroutine);
                    TogglechasehqLight1(false);
                    TogglechasehqLight2(false);
                    chasehqCoroutine = null;
                    isChaseHqFlashing = false;
                }
                inputDetected = true;
            }
            // shift button pressed
            if (XInput.GetDown(XInput.Button.Y))
            {
                if (!isChaseHqinHigh)
                {
                    // Start the flashing if not already flashing
                    chasehqshifter.Rotate(0, 0, 45f);
                    isChaseHqinHigh = true;
                }
                else
                {
                    chasehqshifter.Rotate(0, 0, -45f);
                    isChaseHqinHigh = false;
                }
                inputDetected = true;
            }

            /*
                        // Jump
                        if (Input.GetButtonDown("Jump") && p1currentPlunger4Position < p1positionLimit4)
                        {
                            float p1plunger4ObjectPosition = (Input.GetKey(KeyCode.X) ? keyboardControllerVelocityX : vrControllerVelocity) * Time.deltaTime;
                            p1plunger4Object.position += new Vector3(0, -0.003f, 0);
                            p1currentPlunger4Position += p1plunger4ObjectPosition;
                            inputDetected = true;
                        }

                        // Reset position on button release
                        if (Input.GetButtonUp("Jump"))
                        {
                            p1plunger4Object.position = p1plunger4ObjectStartPosition;
                            p1currentPlunger4Position = 0f; // Reset the current position
                            inputDetected = true;
                        }
            */
            // Handle RB button press for plunger position
            if ((XInput.GetDown(XInput.Button.RShoulder) || Input.GetKeyDown(KeyCode.JoystickButton5)) && p1currentPlunger5Position < p1positionLimit5)
            {
                float p1plunger5ObjectPosition = (Input.GetKey(KeyCode.X) ? keyboardControllerVelocityX : vrControllerVelocity) * Time.deltaTime;
                p1plunger5Object.position += new Vector3(0, -0.003f, 0);
                p1currentPlunger5Position += p1plunger5ObjectPosition;
                inputDetected = true;
            }

            // Reset position on button release
            if (XInput.GetUp(XInput.Button.RShoulder) || Input.GetKeyUp(KeyCode.JoystickButton5))
            {
                p1plunger5Object.position = p1plunger5ObjectStartPosition;
                p1currentPlunger5Position = 0f; // Reset the current position
                inputDetected = true;
            }

            // Handle LB button press for plunger position
            if ((XInput.GetDown(XInput.Button.LShoulder) || Input.GetKeyDown(KeyCode.JoystickButton4)) && p1currentPlunger6Position < p1positionLimit5)
            {
                float p1plunger6ObjectPosition = (Input.GetKey(KeyCode.X) ? keyboardControllerVelocityX : vrControllerVelocity) * Time.deltaTime;
                p1plunger6Object.position += new Vector3(0, -0.003f, 0);
                p1currentPlunger6Position += p1plunger6ObjectPosition;
                inputDetected = true;
            }

            // Reset position on button release
            if (XInput.GetUp(XInput.Button.LShoulder) || Input.GetKeyUp(KeyCode.JoystickButton4))
            {
                p1plunger6Object.position = p1plunger6ObjectStartPosition;
                p1currentPlunger6Position = 0f; // Reset the current position
                inputDetected = true;
            }

            // Handle LT press (assuming LT is mapped to a button in your XInput class)
            if (XInput.GetDown(XInput.Button.LIndexTrigger))
            {
                inputDetected = true;
            }

            // Reset position on LT release
            if (XInput.GetUp(XInput.Button.LIndexTrigger))
            {
                inputDetected = true;
            }
            // Handle RT press (assuming RT is mapped to a button in your XInput class)
            if (XInput.GetDown(XInput.Button.RIndexTrigger))
            {
                inputDetected = true;
            }

            // Reset position on LT release
            if (XInput.GetUp(XInput.Button.RIndexTrigger))
            {
                inputDetected = true;
            }


            /*
            //Back
            // Check if the Back button is pressed
            if (XInput.GetButtonDown("Back") || Input.GetKeyDown(KeyCode.JoystickButton6))
            {
                logger.Info("Xbox Back button pressed");
            }

            //Start
            // Check if the Start button is pressed
            if (XInput.GetButtonDown("Start") || Input.GetKeyDown(KeyCode.JoystickButton7))
            {
                logger.Info("Xbox Start button pressed");
            }

            // Check if the Left Thumbstick is clicked
            if (Input.GetButtonDown("LThumb") || Input.GetKeyDown(KeyCode.JoystickButton8))
            {
                Debug.Log("Xbox Left Thumbstick clicked");
            }

            // Check if the Right Thumbstick is clicked
            if (Input.GetButtonDown("RThumb") || Input.GetKeyDown(KeyCode.JoystickButton9))
            {
                Debug.Log("Xbox Right Thumbstick clicked");
            }
            */

            // Check for mouse movement
            //  float mouseX = Input.GetAxis("Mouse X");
            //  float mouseY = Input.GetAxis("Mouse Y");
            // Center the rotation if no input is detected
            if (!inputDetected)
            {
                CenterRotation();
            }
        }

        void CenterRotation()
        {
            //Centering for contoller 1

            // Center X-Axis Controller rotation
            if (p1currentControllerRotationX > 0)
            {
                float p1unrotateX = Mathf.Min(p1centeringControllerVelocityX * Time.deltaTime, p1currentControllerRotationX);
                p1controllerX.Rotate(p1unrotateX, 0, 0);   // Rotating to reduce the rotation
                p1currentControllerRotationX -= p1unrotateX;    // Reducing the positive rotation
            }
            else if (p1currentControllerRotationX < 0)
            {
                float p1unrotateX = Mathf.Min(p1centeringControllerVelocityX * Time.deltaTime, -p1currentControllerRotationX);
                p1controllerX.Rotate(-p1unrotateX, 0, 0);   // Rotating to reduce the rotation
                p1currentControllerRotationX += p1unrotateX;    // Reducing the positive rotation
            }

            // Center Y-axis Controller rotation
            if (p1currentControllerRotationY > 0)
            {
                float p1unrotateY = Mathf.Min(p1centeringControllerVelocityY * Time.deltaTime, p1currentControllerRotationY);
                p1controllerY.Rotate(0, p1unrotateY, 0);   // Rotating to reduce the rotation
                p1currentControllerRotationY -= p1unrotateY;    // Reducing the positive rotation
            }
            else if (p1currentControllerRotationY < 0)
            {
                float p1unrotateY = Mathf.Min(p1centeringControllerVelocityY * Time.deltaTime, -p1currentControllerRotationY);
                p1controllerY.Rotate(0, -p1unrotateY, 0);  // Rotating to reduce the rotation
                p1currentControllerRotationY += p1unrotateY;    // Reducing the negative rotation
            }


            // Center Z-axis Controller rotation
            if (p1currentControllerRotationZ > 0)
            {
                float p1unrotateZ = Mathf.Min(p1centeringControllerVelocityZ * Time.deltaTime, p1currentControllerRotationZ);
                p1controllerZ.Rotate(0, 0, p1unrotateZ);   // Rotating to reduce the rotation
                p1currentControllerRotationZ -= p1unrotateZ;    // Reducing the positive rotation
            }
            else if (p1currentControllerRotationZ < 0)
            {
                float p1unrotateZ = Mathf.Min(p1centeringControllerVelocityZ * Time.deltaTime, -p1currentControllerRotationZ);
                p1controllerZ.Rotate(0, 0, -p1unrotateZ);   // Rotating to reduce the rotation
                p1currentControllerRotationZ += p1unrotateZ;    // Reducing the positive rotation
            }

            //Centering for contoller 2

            // Center X-Axis Controller rotation
            if (p2currentControllerRotationX > 0)
            {
                float p2unrotateX = Mathf.Min(p2centeringControllerVelocityX * Time.deltaTime, p2currentControllerRotationX);
                p2controllerX.Rotate(p2unrotateX, 0, 0);   // Rotating to reduce the rotation
                p2currentControllerRotationX -= p2unrotateX;    // Reducing the positive rotation
            }
            else if (p2currentControllerRotationX < 0)
            {
                float p2unrotateX = Mathf.Min(p2centeringControllerVelocityX * Time.deltaTime, -p2currentControllerRotationX);
                p2controllerX.Rotate(-p2unrotateX, 0, 0);   // Rotating to reduce the rotation
                p2currentControllerRotationX += p2unrotateX;    // Reducing the positive rotation
            }

            // Center Y-axis Controller rotation
            if (p2currentControllerRotationY > 0)
            {
                float p2unrotateY = Mathf.Min(p2centeringControllerVelocityY * Time.deltaTime, p2currentControllerRotationY);
                p2controllerY.Rotate(0, p2unrotateY, 0);   // Rotating to reduce the rotation
                p2currentControllerRotationY -= p2unrotateY;    // Reducing the positive rotation
            }
            else if (p2currentControllerRotationY < 0)
            {
                float p2unrotateY = Mathf.Min(p2centeringControllerVelocityY * Time.deltaTime, -p2currentControllerRotationY);
                p2controllerY.Rotate(0, -p2unrotateY, 0);  // Rotating to reduce the rotation
                p2currentControllerRotationY += p2unrotateY;    // Reducing the negative rotation
            }

            // Center Z-axis Controller rotation
            if (p2currentControllerRotationZ > 0)
            {
                float p2unrotateZ = Mathf.Min(p2centeringControllerVelocityZ * Time.deltaTime, p2currentControllerRotationZ);
                p2controllerZ.Rotate(0, 0, p2unrotateZ);   // Rotating to reduce the rotation
                p2currentControllerRotationZ -= p2unrotateZ;    // Reducing the positive rotation
            }
            else if (p2currentControllerRotationZ < 0)
            {
                float p2unrotateZ = Mathf.Min(p2centeringControllerVelocityZ * Time.deltaTime, -p2currentControllerRotationZ);
                p2controllerZ.Rotate(0, 0, -p2unrotateZ);   // Rotating to reduce the rotation
                p2currentControllerRotationZ += p2unrotateZ;    // Reducing the positive rotation
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
        void TogglechasehqLights(bool isActive)
        {
            for (int i = 0; i < lights.Length; i++)
            {
                lights[i].enabled = isActive;
            }

            logger.Info($"Lights turned {(isActive ? "on" : "off")}.");
        }

        IEnumerator FlashchasehqLights()
        {
            int currentIndex = 0; // Start with the first light in the array

            while (true)
            {
                // Select the current light
                Light light = chasehqLights[currentIndex];

                // Check if the light is not null
                if (light != null)
                {
                    // Log the chosen light
                    // logger.Debug($"Flashing {light.name}");

                    // Turn on the chosen light
                    TogglechasehqLight(light, true);

                    // Wait for the flash duration
                    yield return new WaitForSeconds(flashDuration);

                    // Turn off the chosen light
                    TogglechasehqLight(light, false);

                    // Wait for the next flash interval
                    yield return new WaitForSeconds(flashInterval - flashDuration);
                }
                else
                {
                    logger.Debug("Light is null.");
                }

                // Move to the next light in the array
                currentIndex = (currentIndex + 1) % chasehqLights.Length;
            }
        }


        void TogglechasehqLight(Light light, bool isActive)
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

        void TogglechasehqLight1(bool isActive)
        {
            TogglechasehqLight(chasehq1_light, isActive);
        }

        void TogglechasehqLight2(bool isActive)
        {
            TogglechasehqLight(chasehq2_light, isActive);
        }

    }
}
