using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using EmuVR.InputManager;
using System.Collections;

namespace WIGUx.Modules.arcadeControllerMotionSim
{
    public class arcadeControllerSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        private readonly float keyboardVelocityX = 25.5f;  // Velocity for keyboard input
        private readonly float keyboardVelocityY = 25.5f;  // Velocity for keyboard input
        private readonly float keyboardVelocityZ = 25.5f;  // Velocity for keyboard input
        private readonly float vrVelocity = 30.5f;        // Velocity for VR controller input

        private float adjustSpeed = 1.0f;  // Adjust this adjustment speed as needed a lower number will lead to smaller adustments

        // Controller animation 


        // Speeds for the animation of the in game flight stick or wheel
        private readonly float keyboardControllerVelocityX = 150.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityY = 150.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityZ = 150.5f;  // Velocity for keyboard input
        private readonly float vrControllerVelocity = 200.5f;        // Velocity for VR/Controller input

        // player 1

        //p1 sticks

        private float p1controllerrotationLimitX = 10f;  // Rotation limit for X-axis (stick or wheel)
        private float p1controllerrotationLimitY = 0f;  // Rotation limit for Y-axis (stick or wheel)
        private float p1controllerrotationLimitZ = 10f;  // Rotation limit for Z-axis (stick or wheel)

        private float p1currentControllerRotationX = 0f;  // Current rotation for X-axis (stick or wheel)
        private float p1currentControllerRotationY = 0f;  // Current rotation for Y-axis (stick or wheel)
        private float p1currentControllerRotationZ = 0f;  // Current rotation for Z-axis (stick or wheel)

        private readonly float p1centeringControllerVelocityX = 50.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float p1centeringControllerVelocityY = 50.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float p1centeringControllerVelocityZ = 50.5f;  // Velocity for centering rotation (stick or wheel)

        private Transform p1controllerX; // Reference to the main animated controller (wheel)
        private Vector3 p1controllerXStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion p1controllerXStartRotation; // Initial controlller positions and rotations for resetting
        private Transform p1controllerY; // Reference to the main animated controller (wheel)
        private Vector3 p1controllerYStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion p1controllerYStartRotation; // Initial controlller positions and rotations for resetting
        private Transform p1controllerZ; // Reference to the main animated controller (wheel)
        private Vector3 p1controllerZStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion p1controllerZStartRotation; // Initial controlller positions and rotations for resetting

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
        public Light fire1_light;
        public Light fire2_light;
        public Light fire3_light;
        public Light fire4_light;
        public Light fire5_light;
        public Light fire6_light;
        public string fire1Button = "Fire1"; // Name of the fire button
        public string fire2Button = "Fire2"; // Name of the fire button 
        public string fire3Button = "Fire3"; // Name of the fire button 
        public string JumpButton = "Jump"; // Name of the fire button 
        public string LBButton = "LB"; // Name of the fire button 
        public string RBButton = "RB"; // Name of the fire button 
        public string StartButton = "Start"; // Name of the fire button 
        public float lightDuration = 0.35f; // Duration during which the lights will be on

        private Light[] lights;

        private bool inFocusMode = false;  // Flag to track focus mode state

        private Dictionary<GameObject, Transform> originalParents = new Dictionary<GameObject, Transform>();  // Dictionary to store original parents of objects

        void Start()
        {
            logger.Info("Arcade Wheels, Sticks and Buttons Motion Sim starting...");

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


            // Find controller2X for player 2
            p2controllerX = transform.Find("p2controllerX");
            if (p2controllerX != null)
            {
                logger.Info("p2controllerX object found.");
                // Store initial position and rotation of the stick
                p2controllerXStartPosition = p2controllerX.transform.position;
                p2controllerXStartRotation = p2controllerX.transform.rotation;

                // Find p2controllerY under p2controllerX
                p2controllerY = p2controllerX.Find("p2controllerY");
                if (p2controllerY != null)
                {
                    logger.Info("p2controllerY object found.");
                    // Store initial position and rotation of the stick
                    p2controllerYStartPosition = p2controllerY.transform.position;
                    p2controllerYStartRotation = p2controllerY.transform.rotation;

                    // Find p2controllerZ under p2controllerY
                    p2controllerZ = p2controllerY.Find("p2controllerZ");
                    if (p2controllerZ != null)
                    {
                        logger.Info("p2controllerZ object found.");
                        // Store initial position and rotation of the stick
                        p2controllerZStartPosition = p2controllerZ.transform.position;
                        p2controllerZStartRotation = p2controllerZ.transform.rotation;
                    }
                    else
                    {
                        logger.Error("p2controllerZ object not found under p2controllerY!");
                    }
                }
                else
                {
                    logger.Error("p2controllerY object not found under p2controllerX!");
                }
            }
            else
            {
                logger.Error("p2controllerX object not found!!");
            }
            // Find p1 start button object in hierarchy
            p1startObject = transform.Find("start");
            if (p1button1Object != null)
            {
                logger.Info("p1startObject found.");
                p1startObjectStartPosition = p1startObject.position;
                p1startObjectStartRotation = p1startObject.rotation;

                // Find plunger1 object under button1
                p1startPlunger = p1button1Object.Find("p1startplunger");
                if (p1startPlunger != null)
                {
                    logger.Info("p1startplunger found.");
                    p2startPlungerStartPosition = p1startPlunger.position;
                    p2startPlungerStartRotation = p1startPlunger.rotation;
                }
                else
                {
                    logger.Error("p1startplunger object not found under p1startObject!");
                }
            }
            else
            {
                logger.Error("p1startplunger object not found!");
            }

            // Find p1 button1 object in hierarchy
            p1button1Object = transform.Find("p1button1");
            if (p1button1Object != null)
            {
                logger.Info("p1button1Object found.");
                p1button1ObjectStartPosition = p1button1Object.position;
                p1button1ObjectStartRotation = p1button1Object.rotation;

                // Find plunger1 object under button1
                p1plunger1Object = p1button1Object.Find("p1plunger1");
                if (p1plunger1Object != null)
                {
                    logger.Info("plungerObject1 found.");
                    p1plunger1ObjectStartPosition = p1plunger1Object.position;
                    p1plunger1ObjectStartRotation = p1plunger1Object.rotation;
                }
                else
                {
                    logger.Error("p1plunger1Object object not found under p1button1Object!");
                }
            }
            else
            {
                logger.Error("p1plunger1Object object not found!");
            }

            // Find p1 button2 object in hierarchy
            p1button2Object = transform.Find("p1button2");
            if (p1button2Object != null)
            {
                logger.Info("p1button2Object found.");
                p1button2ObjectStartPosition = p1button2Object.position;
                p1button2ObjectStartRotation = p1button2Object.rotation;

                // Find plunger2 object under button2
                p1plunger2Object = p1button2Object.Find("p1plunger2");
                if (p1plunger2Object != null)
                {
                    logger.Info("plungerObject2 found.");
                    p1plunger2ObjectStartPosition = p1plunger2Object.position;
                    p1plunger2ObjectStartRotation = p1plunger2Object.rotation;
                }
                else
                {
                    logger.Error("p1plunger2Object object not found under p1button2Object!");
                }
            }
            else
            {
                logger.Error("p1plunger2Object object not found!");
            }

            // Find p1 button3 object in hierarchy
            p1button3Object = transform.Find("p1button3");
            if (p1button3Object != null)
            {
                logger.Info("p1button3Object found.");
                p1button3ObjectStartPosition = p1button3Object.position;
                p1button3ObjectStartRotation = p1button3Object.rotation;

                // Find plunger3 object under button3
                p1plunger3Object = p1button3Object.Find("p1plunger3");
                if (p1plunger3Object != null)
                {
                    logger.Info("plungerObject3 found.");
                    p1plunger3ObjectStartPosition = p1plunger3Object.position;
                    p1plunger3ObjectStartRotation = p1plunger3Object.rotation;
                }
                else
                {
                    logger.Error("p1plunger3Object object not found under p1button3Object!");
                }
            }
            else
            {
                logger.Error("p1plunger3Object object not found!");
            }

            // Find p1 button4 object in hierarchy
            p1button4Object = transform.Find("p1button4");
            if (p1button4Object != null)
            {
                logger.Info("p1button4Object found.");
                p1button4ObjectStartPosition = p1button4Object.position;
                p1button4ObjectStartRotation = p1button4Object.rotation;

                // Find plunger4 object under button4
                p1plunger4Object = p1button4Object.Find("p1plunger4");
                if (p1plunger4Object != null)
                {
                    logger.Info("plungerObject4 found.");
                    p1plunger4ObjectStartPosition = p1plunger4Object.position;
                    p1plunger4ObjectStartRotation = p1plunger4Object.rotation;
                }
                else
                {
                    logger.Error("p1plunger4Object object not found under p1button4Object!");
                }
            }
            else
            {
                logger.Error("p1plunger4Object object not found!");
            }

            // Find p1 button5 object in hierarchy
            p1button5Object = transform.Find("p1button5");
            if (p1button5Object != null)
            {
                logger.Info("p1button5Object found.");
                p1button5ObjectStartPosition = p1button5Object.position;
                p1button5ObjectStartRotation = p1button5Object.rotation;

                // Find plunger5 object under button5
                p1plunger5Object = p1button5Object.Find("p1plunger5");
                if (p1plunger5Object != null)
                {
                    logger.Info("plungerObject5 found.");
                    p1plunger5ObjectStartPosition = p1plunger5Object.position;
                    p1plunger5ObjectStartRotation = p1plunger5Object.rotation;
                }
                else
                {
                    logger.Error("p1plunger5Object object not found under p1button5Object!");
                }
            }
            else
            {
                logger.Error("p1plunger5Object object not found!");
            }

            // Find p1 button6 object in hierarchy
            p1button6Object = transform.Find("p1button6");
            if (p1button6Object != null)
            {
                logger.Info("p1button6Object found.");
                p1button6ObjectStartPosition = p1button6Object.position;
                p1button6ObjectStartRotation = p1button6Object.rotation;

                // Find plunger6 object under button6
                p1plunger6Object = p1button6Object.Find("p1plunger6");
                if (p1plunger6Object != null)
                {
                    logger.Info("plungerObject6 found.");
                    p1plunger6ObjectStartPosition = p1plunger6Object.position;
                    p1plunger6ObjectStartRotation = p1plunger6Object.rotation;
                }
                else
                {
                    logger.Error("p1plunger6Object object not found under p1button6Object!");
                }
            }
            else
            {
                logger.Error("p1plunger6Object object not found!");
            }
        }

        void Update()
        {
            bool inputDetected = false;  // Initialize at the beginning of the Update method

            if (GameSystem.ControlledSystem != null && !inFocusMode)
            {
                StartFocusMode();
            }

            if (GameSystem.ControlledSystem == null && inFocusMode)
            {
                EndFocusMode();
            }

            if (inFocusMode)
            {
                HandleInput(ref inputDetected);  // Pass by reference
            //    HandleButtonAnimations();
            }
        }

        void StartFocusMode()
        {
            logger.Info("Entering Focus Mode...");

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
            logger.Info("Exiting Focus Mode...");
            //player 1
            p1currentControllerRotationX = 0f;
            p1currentControllerRotationY = 0f;
            p1currentControllerRotationZ = 0f;
            //player 2
            p2currentControllerRotationX = 0f;
            p2currentControllerRotationY = 0f;
            p2currentControllerRotationZ = 0f;

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
                Vector2 xboxPrimaryThumbstick = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                Vector2 xboxSecondaryThumbstick = new Vector2(Input.GetAxis("RightStickHorizontal"), Input.GetAxis("RightStickVertical"));

            // Combine VR and Xbox inputs
            primaryThumbstick += xboxPrimaryThumbstick;
            secondaryThumbstick += xboxSecondaryThumbstick;
            // Get the trigger axis values
            float xboxLeftTrigger = Input.GetAxis("LeftTrigger");
            float xboxRightTrigger = Input.GetAxis("RightTrigger");


            // Check if the Left Bumper is pressed
            if (Input.GetButtonDown("LB"))
            {
                logger.Info("Xbox Left Bumper pressed");
            }

            // Check if the Right Bumper is pressed
            if (Input.GetButtonDown("RB"))
            {
                logger.Info("Xbox Right Bumper pressed");
            }

            // Check if the Back button is pressed
            if (Input.GetButtonDown("Back"))
            {
                logger.Info("Xbox Back button pressed");
            }

            // Check if the Start button is pressed
            if (Input.GetButtonDown("Start"))
            {
                logger.Info("Xbox Start button pressed");
            }

            /* 
            // Check if the Left Thumbstick is clicked
            if (Input.GetButtonDown("LThumb"))
            {
                logger.Info("Xbox Left Thumbstick clicked");
            }

            // Check if the Right Thumbstick is clicked
            if (Input.GetButtonDown("RThumb"))
            {
                logger.Info("Xbox Right Thumbstick clicked");
            }

            // Thumbstick direction: Y
            // Thumbstick direction: up
            if (primaryThumbstick.y > 0 && p1currentControllerRotationY < p1controllerrotationLimitY)
            {
                float p1controllerRotateY = (Input.GetKey(KeyCode.UpArrow) ? keyboardControllerVelocityY : primaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                p1controllerY.Rotate(0, 0, -p1controllerRotateY);
                p1currentControllerRotationY += p1controllerRotateY;
                inputDetected = true;
            }
            // Thumbstick direction: down
            if (primaryThumbstick.y < 0 && p1currentControllerRotationY > -p1controllerrotationLimitY)
            {
                float p1controllerRotateY = (Input.GetKey(KeyCode.DownArrow) ? keyboardControllerVelocityY : -primaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                p1controllerY.Rotate(0, 0, p1controllerRotateY);
                p1currentControllerRotationY -= p1controllerRotateY;
                inputDetected = true;
            }
            */

            // Thumbstick direction: X
            // Thumbstick direction: right
            if (primaryThumbstick.x > 0 && p1currentControllerRotationX < p1controllerrotationLimitX)
            {
                float p1controllerRotateX = (Input.GetKey(KeyCode.RightArrow) ? keyboardControllerVelocityX : primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                p1controllerX.Rotate(-p1controllerRotateX, 0, 0);
                p1currentControllerRotationX += p1controllerRotateX;
                inputDetected = true;
            }
            // Thumbstick direction: left
            if (primaryThumbstick.x < 0 && p1currentControllerRotationX > -p1controllerrotationLimitX)
            {
                float p1controllerRotateX = (Input.GetKey(KeyCode.LeftArrow) ? keyboardControllerVelocityX : -primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                p1controllerX.Rotate(p1controllerRotateX, 0, 0);
                p1currentControllerRotationX -= p1controllerRotateX;
                inputDetected = true;
            }

            // Thumbstick direction: Z
            // Thumbstick direction: Up
            if (primaryThumbstick.y > 0 && p1currentControllerRotationZ < p1controllerrotationLimitZ)
            {
                float p1controllerRotateZ = (Input.GetKey(KeyCode.UpArrow) ? keyboardControllerVelocityZ : primaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                p1controllerZ.Rotate(0, 0, -p1controllerRotateZ);
                p1currentControllerRotationZ += p1controllerRotateZ;
                inputDetected = true;
            }
            // Thumbstick direction: Down
            if (primaryThumbstick.y < 0 && p1currentControllerRotationZ > -p1controllerrotationLimitZ)
            {
                float p1controllerRotateZ = (Input.GetKey(KeyCode.DownArrow) ? keyboardControllerVelocityZ : -primaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                p1controllerZ.Rotate(0, 0, p1controllerRotateZ);
                p1currentControllerRotationZ -= p1controllerRotateZ;
                inputDetected = true;
            }

            // This can be mapped to secondaryThumbstick for p2 tests
            // Thumbstick direction: X
            // Thumbstick direction: right
            if (secondaryThumbstick.x > 0 && p2currentControllerRotationX < p2controllerrotationLimitX)
            {
                float p2controllerRotateX = (Input.GetKey(KeyCode.RightArrow) ? keyboardControllerVelocityX : secondaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                p2controllerX.Rotate(-p2controllerRotateX, 0, 0);
                p2currentControllerRotationX += p2controllerRotateX;
                inputDetected = true;
            }
            // Thumbstick direction: left
            if (secondaryThumbstick.x < 0 && p2currentControllerRotationX > -p2controllerrotationLimitX)
            {
                float p2controllerRotateX = (Input.GetKey(KeyCode.LeftArrow) ? keyboardControllerVelocityX : -secondaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                p2controllerX.Rotate(p2controllerRotateX, 0, 0);
                p2currentControllerRotationX -= p2controllerRotateX;
                inputDetected = true;
            }

            // Thumbstick direction: Z
            // Thumbstick direction: Up
            if (secondaryThumbstick.y > 0 && p2currentControllerRotationZ < p2controllerrotationLimitZ)
            {
                float p2controllerRotateZ = (Input.GetKey(KeyCode.UpArrow) ? keyboardControllerVelocityZ : secondaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                p2controllerZ.Rotate(0, 0, -p2controllerRotateZ);
                p2currentControllerRotationZ += p2controllerRotateZ;
                inputDetected = true;
            }
            // Thumbstick direction: Down
            if (secondaryThumbstick.y < 0 && p2currentControllerRotationZ > -p2controllerrotationLimitZ)
            {
                float p2controllerRotateZ = (Input.GetKey(KeyCode.DownArrow) ? keyboardControllerVelocityZ : -secondaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                p2controllerZ.Rotate(0, 0, p2controllerRotateZ);
                p2currentControllerRotationZ -= p2controllerRotateZ;
                inputDetected = true;
            }

            /*
            // Thumbstick direction: up 
            if (secondaryThumbstick.y > 0 && p2currentControllerRotationY < p2controllerrotationLimitY)
            {
                float p2controllerRotateY = (Input.GetKey(KeyCode.UpArrow) ? keyboardControllerVelocityY : secondaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                p2controllerY.Rotate(0, p2controllerRotateY, 0);
                p2currentControllerRotationY += p2controllerRotateY;
                inputDetected = true;
            }
            // Thumbstick direction: down
            if (secondaryThumbstick.y < 0 && p2currentControllerRotationY > -p2controllerrotationLimitY)
            {
                float p2controllerRotateY = (Input.GetKey(KeyCode.DownArrow) ? keyboardControllerVelocityY : -secondaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                p2controllerY.Rotate(0, p2controllerRotateY, 0);
                p2currentControllerRotationY -= p2controllerRotateY;
                inputDetected = true;
            }
           */

            // Check if the primary index trigger on the right controller is pressed
            if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger))
            {
                Debug.Log("OVR Primary index trigger pressed");
            }

            // p1 start

            if (Input.GetButtonDown("Start") && p1currentPlungerStartPosition < p1positionLimitstart)
            {
                float p1currentStartPosition = (Input.GetKey(KeyCode.Z) ? keyboardControllerVelocityX : vrControllerVelocity) * Time.deltaTime;
                p1startPlunger.position += new Vector3(0, -0.003f, 0);
                p1currentPlungerStartPosition += p1currentStartPosition;
                inputDetected = true;
            }

            // Reset position on button release
            if (Input.GetButtonUp("Start"))
            {
                p1startPlunger.position = p1startPlungerStartPosition;
                p1currentStartPosition = 0f; // Reset the current position
                inputDetected = true;
            }

            // Check if the Back button is pressed
            if (Input.GetKeyDown(KeyCode.JoystickButton6))
            {
                Debug.Log("Xbox Back button pressed");
            }

            // Check if the Start button is pressed
            if (Input.GetKeyDown(KeyCode.JoystickButton7))
            {
                Debug.Log("Xbox Start button pressed");
            }

            // Check if the Left Thumbstick is clicked
            if (Input.GetKeyDown(KeyCode.JoystickButton8))
            {
                Debug.Log("Xbox Left Thumbstick clicked");
            }

            // Check if the Right Thumbstick is clicked
            if (Input.GetKeyDown(KeyCode.JoystickButton9))
            {
                Debug.Log("Xbox Right Thumbstick clicked");
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

            // RB
            if (Input.GetButtonDown("RB") || Input.GetKeyDown(KeyCode.JoystickButton5) && p1currentPlunger5Position < p1positionLimit5)
            {
                float p1plunger5ObjectPosition = (Input.GetKey(KeyCode.X) ? keyboardControllerVelocityX : vrControllerVelocity) * Time.deltaTime;
                p1plunger5Object.position += new Vector3(0, -0.003f, 0);
                p1currentPlunger5Position += p1plunger5ObjectPosition;
                inputDetected = true;
            }

            // Reset position on button release
            if (Input.GetButtonDown("RB") || Input.GetKeyDown(KeyCode.JoystickButton5))
            {
                p1plunger5Object.position = p1plunger5ObjectStartPosition;
                p1currentPlunger5Position = 0f; // Reset the current position
                inputDetected = true;
            }

            // LB
            if (Input.GetButtonDown("LB") || Input.GetKeyDown(KeyCode.JoystickButton4) && p1currentPlunger6Position < p1positionLimit5)
            {
                float p1plunger6ObjectPosition = (Input.GetKey(KeyCode.X) ? keyboardControllerVelocityX : vrControllerVelocity) * Time.deltaTime;
                p1plunger6Object.position += new Vector3(0, -0.003f, 0);
                p1currentPlunger6Position += p1plunger6ObjectPosition;
                inputDetected = true;
            }

            // Reset position on button release
            if (Input.GetButtonDown("LB") || Input.GetKeyDown(KeyCode.JoystickButton4))
            {
                p1plunger6Object.position = p1plunger6ObjectStartPosition;
                p1currentPlunger6Position = 0f; // Reset the current position
                inputDetected = true;
            }

            /*
            if (Input.GetButtonDown("LB") || Input.GetKeyDown(KeyCode.JoystickButton4))
            {
                Debug.Log("Xbox Left Bumper pressed");
            }

            // Check if the Right Bumper is pressed
            if (Input.GetButtonDown("RB") || Input.GetKeyDown(KeyCode.JoystickButton5))
            {
                Debug.Log("Xbox Right Bumper pressed");
            }

            //Back
            // Check if the Back button is pressed
            if (Input.GetButtonDown("Back") || Input.GetKeyDown(KeyCode.JoystickButton6))
            {
                Debug.Log("Xbox Back button pressed");
            }

            //Start
            // Check if the Start button is pressed
            if (Input.GetButtonDown("Start") || Input.GetKeyDown(KeyCode.JoystickButton7))
            {
                Debug.Log("Xbox Start button pressed");
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
    }
}
