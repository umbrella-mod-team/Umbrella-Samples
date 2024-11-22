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

        private float tapp1controllerrotationLimitX = 10f;  // Rotation limit for X-axis (stick or wheel)
        private float tapp1controllerrotationLimitY = 0f;  // Rotation limit for Y-axis (stick or wheel)
        private float tapp1controllerrotationLimitZ = 10f;  // Rotation limit for Z-axis (stick or wheel)
        private float tapp1taprotationLimitZ = 95f;  // Rotation limit for Z-axis (stick or wheel)

        private float tapp1currentControllerRotationX = 0f;  // Current rotation for X-axis (stick or wheel)
        private float tapp1currentControllerRotationY = 0f;  // Current rotation for Y-axis (stick or wheel)
        private float tapp1currentControllerRotationZ = 0f;  // Current rotation for Z-axis (stick or wheel)
        private float tapp1currenttapRotationZ = 0f;  // Current rotation for Z-axis (stick or wheel)

        private readonly float tapp1centeringControllerVelocityX = 150.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float tapp1centeringControllerVelocityY = 150.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float tapp1centeringControllerVelocityZ = 150.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float tapp1centeringtapVelocityZ = 250.5f;  // Velocity for centering rotation (stick or wheel)

        private Transform tapp1controllerX; // Reference to the main animated controller (wheel)
        private Vector3 tapp1controllerXStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion tapp1controllerXStartRotation; // Initial controlller positions and rotations for resetting
        private Transform tapp1controllerY; // Reference to the main animated controller (wheel)
        private Vector3 tapp1controllerYStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion tapp1controllerYStartRotation; // Initial controlller positions and rotations for resetting
        private Transform tapp1controllerZ; // Reference to the main animated controller (wheel)
        private Vector3 tapp1controllerZStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion tapp1controllerZStartRotation; // Initial controlller positions and rotations for resetting
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
       // public float lightDuration = 0.35f; // Duration during which the lights will be on

        // private Light[] lights;
        private readonly string[] compatibleGames = { "tapper.zip", "tapper.7z" };

        private bool inFocusMode = false;  // Flag to track focus mode state

        private Dictionary<GameObject, Transform> originalParents = new Dictionary<GameObject, Transform>();  // Dictionary to store original parents of objects
                                                                                                              // Public property to access the Game instance
        void Start()
        {
            // Find tapp1controllerX for player 1
            tapp1controllerX = transform.Find("tapp1controllerX");
            if (tapp1controllerX != null)
            {
                logger.Info("tapp1controllerX object found.");
                // Store initial position and rotation of the stick
                tapp1controllerXStartPosition = tapp1controllerX.transform.position;
                tapp1controllerXStartRotation = tapp1controllerX.transform.rotation;

                // Find p1controllerY under tapp1controllerX
                tapp1controllerY = tapp1controllerX.Find("tapp1controllerY");
                if (tapp1controllerY != null)
                {
                    logger.Info("tapp1controllerY object found.");
                    // Store initial position and rotation of the stick
                    tapp1controllerYStartPosition = tapp1controllerY.transform.position;
                    tapp1controllerYStartRotation = tapp1controllerY.transform.rotation;

                    // Find tapp1controllerZ under tapp1controllerY
                    tapp1controllerZ = tapp1controllerY.Find("tapp1controllerZ");
                    if (tapp1controllerZ != null)
                    {
                        logger.Info("tapp1controllerZ object found.");
                        // Store initial position and rotation of the stick
                        tapp1controllerZStartPosition = tapp1controllerZ.transform.position;
                        tapp1controllerZStartRotation = tapp1controllerZ.transform.rotation;
                    }
                    else
                    {
                        logger.Error("tapp1controllerZ object not found under tapp1controllerY!");
                    }
                }
                else
                {
                    logger.Error("tapp1controllerY object not found under tapp1controllerX!");
                }
            }
            else
            {
                logger.Error("tapp1controllerX object not found!!");
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
            tapp1currentControllerRotationX = 0f;
            tapp1currentControllerRotationY = 0f;
            tapp1currentControllerRotationZ = 0f;
            tapp1currenttapRotationZ = 0f;
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
            if (Input.GetButton("Fire1") && tapp1currenttapRotationZ < tapp1taprotationLimitZ)
            {
                float tapVelocity = 250.5f; // Example value, ensure this is correctly set in your actual code
                float p1tapRotateZ = tapVelocity * Time.deltaTime; // Simplified calculation since the condition is always true

                // Check if the increment would exceed the positive limit
                if (tapp1currenttapRotationZ + p1tapRotateZ > tapp1taprotationLimitZ)
                {
                    p1tapRotateZ = tapp1taprotationLimitZ - tapp1currenttapRotationZ; // Adjust to reach exactly the limit
                }

                p1tapZ.Rotate(0, 0, p1tapRotateZ); // Apply positive rotation
                tapp1currenttapRotationZ += p1tapRotateZ; // Adjusting current rotation in the positive direction
                inputDetected = true;

                // Info logs to monitor the values
             //   logger.Info("tapVelocity: " + tapVelocity);
             //   logger.Info("Time.deltaTime: " + Time.deltaTime);
             //   Vector3 rotation = p1tapZ.transform.rotation.eulerAngles;
            //   logger.Info("Current Rotation - X: " + rotation.x + ", Y: " + rotation.y + ", Z: " + rotation.z);
            //    logger.Info("p1currenttapRotationZ: " + tapp1currenttapRotationZ);
            //    logger.Info("p1taprotationLimitZ: " + tapp1taprotationLimitZ);
            }


            // Thumbstick direction: Y
            // Thumbstick direction: up
            if ((Input.GetKey(KeyCode.UpArrow) || XInput.Get(XInput.Button.DpadUp) || primaryThumbstick.y > 0) && tapp1currentControllerRotationY < tapp1controllerrotationLimitY)
            {
                float p1controllerRotateY = (Input.GetKey(KeyCode.UpArrow) || XInput.Get(XInput.Button.DpadUp) ? keyboardControllerVelocityY : primaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                tapp1controllerY.Rotate(0, 0, -p1controllerRotateY);
                tapp1currentControllerRotationY += p1controllerRotateY;
                inputDetected = true;
            }
            // Thumbstick direction: down
            if ((Input.GetKey(KeyCode.DownArrow) || XInput.Get(XInput.Button.DpadDown) || primaryThumbstick.y < 0) && tapp1currentControllerRotationY > -tapp1controllerrotationLimitY)
            {
                float p1controllerRotateY = (Input.GetKey(KeyCode.DownArrow) || XInput.Get(XInput.Button.DpadDown) ? keyboardControllerVelocityY : -primaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                tapp1controllerY.Rotate(0, 0, p1controllerRotateY);
                tapp1currentControllerRotationY -= p1controllerRotateY;
                inputDetected = true;
            }

            // Thumbstick direction: X
            // Thumbstick direction: right
            if ((Input.GetKey(KeyCode.RightArrow) || XInput.Get(XInput.Button.DpadRight) || primaryThumbstick.x > 0) && tapp1currentControllerRotationX < tapp1controllerrotationLimitX)
            {
                float p1controllerRotateX = (Input.GetKey(KeyCode.RightArrow) || XInput.Get(XInput.Button.DpadRight) ? keyboardControllerVelocityX : primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                tapp1controllerX.Rotate(-p1controllerRotateX, 0, 0);
                tapp1currentControllerRotationX += p1controllerRotateX;
                inputDetected = true;
            }
            // Thumbstick direction: left
            if ((Input.GetKey(KeyCode.LeftArrow) || XInput.Get(XInput.Button.DpadLeft) || primaryThumbstick.x < 0) && tapp1currentControllerRotationX > -tapp1controllerrotationLimitX)
            {
                float p1controllerRotateX = (Input.GetKey(KeyCode.LeftArrow) || XInput.Get(XInput.Button.DpadLeft) ? keyboardControllerVelocityX : -primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                tapp1controllerX.Rotate(p1controllerRotateX, 0, 0);
                tapp1currentControllerRotationX -= p1controllerRotateX;
                inputDetected = true;
            }

            // Thumbstick direction: Z
            // Thumbstick or D-pad direction: Up
            if ((primaryThumbstick.y > 0 || XInput.Get(XInput.Button.DpadUp)) && tapp1currentControllerRotationZ < tapp1controllerrotationLimitZ)
            {
                float p1controllerRotateZ = (Input.GetKey(KeyCode.UpArrow) || XInput.Get(XInput.Button.DpadUp) ? keyboardControllerVelocityZ : primaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                tapp1controllerZ.Rotate(0, 0, -p1controllerRotateZ);
                tapp1currentControllerRotationZ += p1controllerRotateZ;
                inputDetected = true;
            }

            // Thumbstick or D-pad direction: Down
            if ((primaryThumbstick.y < 0 || XInput.Get(XInput.Button.DpadDown)) && tapp1currentControllerRotationZ > -tapp1controllerrotationLimitZ)
            {
                float p1controllerRotateZ = (Input.GetKey(KeyCode.DownArrow) || XInput.Get(XInput.Button.DpadDown) ? keyboardControllerVelocityZ : -primaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                tapp1controllerZ.Rotate(0, 0, p1controllerRotateZ);
                tapp1currentControllerRotationZ -= p1controllerRotateZ;
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
            if (tapp1currentControllerRotationX > 0)
            {
                float p1unrotateX = Mathf.Min(tapp1centeringControllerVelocityX * Time.deltaTime, tapp1currentControllerRotationX);
                tapp1controllerX.Rotate(p1unrotateX, 0, 0);   // Rotating to reduce the rotation
                tapp1currentControllerRotationX -= p1unrotateX;    // Reducing the positive rotation
            }
            else if (tapp1currentControllerRotationX < 0)
            {
                float p1unrotateX = Mathf.Min(tapp1centeringControllerVelocityX * Time.deltaTime, -tapp1currentControllerRotationX);
                tapp1controllerX.Rotate(-p1unrotateX, 0, 0);   // Rotating to reduce the rotation
                tapp1currentControllerRotationX += p1unrotateX;    // Reducing the positive rotation
            }

            // Center Y-axis Controller rotation
            if (tapp1currentControllerRotationY > 0)
            {
                float p1unrotateY = Mathf.Min(tapp1centeringControllerVelocityY * Time.deltaTime, tapp1currentControllerRotationY);
                tapp1controllerY.Rotate(0, p1unrotateY, 0);   // Rotating to reduce the rotation
                tapp1currentControllerRotationY -= p1unrotateY;    // Reducing the positive rotation
            }
            else if (tapp1currentControllerRotationY < 0)
            {
                float p1unrotateY = Mathf.Min(tapp1centeringControllerVelocityY * Time.deltaTime, -tapp1currentControllerRotationY);
                tapp1controllerY.Rotate(0, -p1unrotateY, 0);  // Rotating to reduce the rotation
                tapp1currentControllerRotationY += p1unrotateY;    // Reducing the negative rotation
            }

            // Center Z-axis Controller rotation
            if (tapp1currentControllerRotationZ > 0)
            {
                float p1unrotateZ = Mathf.Min(tapp1centeringControllerVelocityZ * Time.deltaTime, tapp1currentControllerRotationZ);
                tapp1controllerZ.Rotate(0, 0, p1unrotateZ);   // Rotating to reduce the rotation
                tapp1currentControllerRotationZ -= p1unrotateZ;    // Reducing the positive rotation
            }
            else if (tapp1currentControllerRotationZ < 0)
            {
                float p1unrotateZ = Mathf.Min(tapp1centeringControllerVelocityZ * Time.deltaTime, -tapp1currentControllerRotationZ);
                tapp1controllerZ.Rotate(0, 0, -p1unrotateZ);   // Rotating to reduce the rotation
                tapp1currentControllerRotationZ += p1unrotateZ;    // Reducing the positive rotation
            }
            // Center Z-Axis Tap rotation
            if (tapp1currenttapRotationZ > 0)
            {
                float p1tapunrotateZ = Mathf.Min(tapp1centeringtapVelocityZ * Time.deltaTime, tapp1currenttapRotationZ);
                p1tapZ.Rotate(0, 0, -p1tapunrotateZ);   // Rotating to reduce the rotation
                tapp1currenttapRotationZ -= p1tapunrotateZ;    // Reducing the positive rotation
            }
            else if (tapp1currenttapRotationZ < 0)
            {
                float p1tapunrotateZ = Mathf.Min(tapp1centeringtapVelocityZ * Time.deltaTime, -tapp1currenttapRotationZ);
                p1tapZ.Rotate(0, 0, p1tapunrotateZ);   // Rotating to reduce the rotation
                tapp1currenttapRotationZ += p1tapunrotateZ;    // Reducing the positive rotation
            }

            // Log values for debugging
       //     logger.Info("Centering Tap Rotation - X: " + tapp1currentControllerRotationX + ", Y: " + tapp1currentControllerRotationY + ", Z: " + tapp1currentControllerRotationZ);
        //    logger.Info("Centering Tap Rotation - Current Tap Z: " + tapp1currenttapRotationZ);
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
