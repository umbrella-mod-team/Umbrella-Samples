using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using EmuVR.InputManager;
using System.Collections;
using static XInput;

namespace WIGUx.Modules.tapperMotionSim
{
    public class tapperSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        private readonly float keyboardVelocityX = 25.5f;  // Velocity for keyboard input
        private readonly float keyboardVelocityY = 25.5f;  // Velocity for keyboard input
        private readonly float keyboardVelocityZ = 25.5f;  // Velocity for keyboard input
        private readonly float vrVelocity = 30.5f;        // Velocity for VR controller input

        // Speeds for the animation of the in game flight stick or wheel
        private readonly float keyboardControllerVelocityX = 150.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityY = 150.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityZ = 150.5f;  // Velocity for keyboard input
        private readonly float vrControllerVelocity = 200.5f;        // Velocity for VR/Controller input
        private readonly float tapVelocity = 250.5f;        // Velocity for VR/Controller input

        // player 1

        //p1 sticks

        private float p1controllerrotationLimitX = 10f;  // Rotation limit for X-axis (stick or wheel)
        private float p1controllerrotationLimitY = 0f;  // Rotation limit for Y-axis (stick or wheel)
        private float p1controllerrotationLimitZ = 10f;  // Rotation limit for Z-axis (stick or wheel)
        private float p1taprotationLimitZ = 45f;  // Rotation limit for Z-axis (stick or wheel)

        private float p1currentControllerRotationX = 0f;  // Current rotation for X-axis (stick or wheel)
        private float p1currentControllerRotationY = 0f;  // Current rotation for Y-axis (stick or wheel)
        private float p1currentControllerRotationZ = 0f;  // Current rotation for Z-axis (stick or wheel)
        private float p1currenttapRotationZ = 0f;  // Current rotation for Z-axis (stick or wheel)

        private readonly float p1centeringControllerVelocityX = 150.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float p1centeringControllerVelocityY = 150.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float p1centeringControllerVelocityZ = 150.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float p1centeringtapVelocityZ = 250.5f;  // Velocity for centering rotation (stick or wheel)

        private Transform p1controllerX; // Reference to the main animated controller (wheel)
        private Vector3 p1controllerXStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion p1controllerXStartRotation; // Initial controlller positions and rotations for resetting
        private Transform p1controllerY; // Reference to the main animated controller (wheel)
        private Vector3 p1controllerYStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion p1controllerYStartRotation; // Initial controlller positions and rotations for resetting
        private Transform p1controllerZ; // Reference to the main animated controller (wheel)
        private Vector3 p1controllerZStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion p1controllerZStartRotation; // Initial controlller positions and rotations for resetting
        private Transform p1tapZ; // Reference to the tap object
        private Vector3 p1tapZStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion p1tapZStartRotation; // Initial controlller positions and rotations for resetting

        //lights
        public Light fire1_light;
        public Light fire2_light;
        public string fire1Button = "Fire1"; // Name of the fire button
        public string fire2Button = "Fire2"; // Name of the fire button 
        public string fire3Button = "Fire3"; // Name of the fire button 
        public string JumpButton = "Jump"; // Name of the fire button 
        public string StartButton = "Start"; // Name of the fire button 
        public float lightDuration = 0.35f; // Duration during which the lights will be on

        private Light[] lights;
        private readonly string[] compatibleGames = { "tapper.zip" };

        private bool inFocusMode = false;  // Flag to track focus mode state

        private Dictionary<GameObject, Transform> originalParents = new Dictionary<GameObject, Transform>();  // Dictionary to store original parents of objects
                                                                                                              // Public property to access the Game instance
        void Start()
        {
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

            // Find tapZ for player 1
            p1tapZ = transform.Find("p1tapZ");
            if (p1tapZ != null)
            {
                logger.Info("p1tapZ object found.");
                // Store initial position and rotation of the stick
                p1tapZStartPosition = p1tapZ.transform.position;
                p1tapZStartRotation = p1tapZ.transform.rotation;
            }
            else
            {
                logger.Error("p1tapZ object not found!");
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
            }
        }

        void StartFocusMode()
        {
            string controlledSystemGamePathString = GameSystem.ControlledSystem.Game.path != null ? GameSystem.ControlledSystem.Game.path.ToString() : null;
            logger.Info($"Controlled System Game path String: {controlledSystemGamePathString}");
            logger.Info("Compatible Rom Dectected, ... Tapping The Beers");
            logger.Info("Tapper Motion Sim starting...");
            logger.Info("One Budwiser Please!...");
            inFocusMode = true;  // Set focus mode flag
        }

        void EndFocusMode()
        {
            logger.Info("Exiting Focus Mode...");
            //player 1
            p1currentControllerRotationX = 0f;
            p1currentControllerRotationY = 0f;
            p1currentControllerRotationZ = 0f;
            p1currenttapRotationZ = 0f;
            inFocusMode = false;  // Clear focus mode flag
        }

        //sexy new combined input handler
        // Handle input method
        //sexy new combined input handler
        void HandleInput(ref bool inputDetected)
        {
            if (!inFocusMode) return;

            Vector2 primaryThumbstick = Vector2.zero;
            Vector2 secondaryThumbstick = Vector2.zero; ;

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
            }
            /*
            // Fire1
            if (XInput.GetDown(XInput.Button.A))
            {
                p1tapZ.Rotate(0, 0, 45f);
                inputDetected = true;
            }
            */
            /*
            // Fire1
            if (XInput.GetUp(XInput.Button.A))
            {
                p1tapZ.Rotate(0, 0, 0);
                inputDetected = true;
            }
            */


            // Fire1
            if (Input.GetButton("Fire1") && p1currenttapRotationZ < p1taprotationLimitZ)
            {
                float tapVelocity = 250.5f; // Example value, ensure this is correctly set in your actual code
                float p1tapRotateZ = tapVelocity * Time.deltaTime; // Simplified calculation since the condition is always true

                // Check if the increment would exceed the positive limit
                if (p1currenttapRotationZ + p1tapRotateZ > p1taprotationLimitZ)
                {
                    p1tapRotateZ = p1taprotationLimitZ - p1currenttapRotationZ; // Adjust to reach exactly the limit
                }

                p1tapZ.Rotate(0, 0, p1tapRotateZ); // Apply positive rotation
                p1currenttapRotationZ += p1tapRotateZ; // Adjusting current rotation in the positive direction
                inputDetected = true;

                // Info logs to monitor the values
         //       logger.Info("tapVelocity: " + tapVelocity);
       //         logger.Info("Time.deltaTime: " + Time.deltaTime);
        //        logger.Info("p1tapRotateZ: " + p1tapRotateZ);
      //          logger.Info("p1currenttapRotationZ: " + p1currenttapRotationZ);
    //            logger.Info("p1taprotationLimitZ: " + p1taprotationLimitZ);
            }


            // Thumbstick direction: Y
            // Thumbstick direction: up
            if ((Input.GetKey(KeyCode.UpArrow) || XInput.Get(XInput.Button.DpadUp) || primaryThumbstick.y > 0) && p1currentControllerRotationY < p1controllerrotationLimitY)
            {
                float p1controllerRotateY = (Input.GetKey(KeyCode.UpArrow) || XInput.Get(XInput.Button.DpadUp) ? keyboardControllerVelocityY : primaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                p1controllerY.Rotate(0, 0, -p1controllerRotateY);
                p1currentControllerRotationY += p1controllerRotateY;
                inputDetected = true;
            }
            // Thumbstick direction: down
            if ((Input.GetKey(KeyCode.DownArrow) || XInput.Get(XInput.Button.DpadDown) || primaryThumbstick.y < 0) && p1currentControllerRotationY > -p1controllerrotationLimitY)
            {
                float p1controllerRotateY = (Input.GetKey(KeyCode.DownArrow) || XInput.Get(XInput.Button.DpadDown) ? keyboardControllerVelocityY : -primaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                p1controllerY.Rotate(0, 0, p1controllerRotateY);
                p1currentControllerRotationY -= p1controllerRotateY;
                inputDetected = true;
            }

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
            }
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
            // Center Z-axis Tap rotation
            if (p1currenttapRotationZ > 0)
            {
                float p1tapunrotateZ = Mathf.Min(p1centeringtapVelocityZ * Time.deltaTime, p1currenttapRotationZ);
                p1tapZ.Rotate(0, 0, p1tapunrotateZ);   // Rotating to reduce the rotation
                p1currenttapRotationZ -= p1tapunrotateZ;    // Reducing the positive rotation
            }
            else if (p1currenttapRotationZ < 0)
            {
                float p1tapunrotateZ = Mathf.Min(p1centeringtapVelocityZ * Time.deltaTime, -p1currenttapRotationZ);
                p1tapZ.Rotate(0, 0, -p1tapunrotateZ);   // Rotating to reduce the rotation
                p1currenttapRotationZ += p1tapunrotateZ;    // Reducing the positive rotation
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
