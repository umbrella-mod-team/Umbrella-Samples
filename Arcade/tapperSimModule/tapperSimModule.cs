using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using EmuVR.InputManager;
using System.Collections;
using static XInput;

namespace WIGUx.Modules.tapperSim
{
    public class tapperSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        [Header("Input Settings")]
        public string primaryThumbstickHorizontal = "Horizontal"; // Input axis for primary thumbstick horizontal
        public string primaryThumbstickVertical = "Vertical"; // Input axis for primary thumbstick vertical
        public string secondaryThumbstickHorizontal = "RightStickHorizontal"; // Input axis for secondary thumbstick horizontal
        public string secondaryThumbstickVertical = "RightStickVertical"; // Input axis for secondary thumbstick forward/backward
        public string leftTrigger = "LIndexTrigger";
        public string rightTrigger = "RIndexTrigger";

        [Header("Velocity Multiplier Settings")]        // Speeds for the animation of the in game flight stick or wheel
        private float primaryThumbstickRotationMultiplier = 10f; // Multiplier for primary thumbstick rotation intensity
        private float secondaryThumbstickRotationMultiplier = 25f; // Multiplier for secondary thumbstick rotation intensity
        private float triggerRotationMultiplier = 20f; // Multiplier for trigger rotation intensity
        private float adjustSpeed = 1.0f;  // Adjust this adjustment speed as needed a lower number will lead to smaller adustments
        private readonly float rotationSmoothness = 5f;  //sets the smoothness of the rotation
        private readonly float thumbstickVelocity = 50f;  // Velocity for keyboard input
        private readonly float centeringVelocityX = 25f;  // Velocity for centering rotation
        private readonly float centeringVelocityY = 25f;  // Velocity for centering rotation
        private readonly float centeringVelocityZ = 25f;  // Velocity for centering rotation
        private readonly float tapRotation = 90f;        // Velocity for Tap

        [Header("Rotation Tracking")]        // Sets up the rotation varibles and sets them to 0 
        private float currentRotationX = 0f;  // Current rotation for X-axis
        private float currentRotationY = 0f;  // Current rotation for Y-axis
        private float currentRotationZ = 0f;  // Current rotation for Z-axis

        [Header("Rotation Limits")]        // Rotation Limits 
        [SerializeField] float minRotationX = -15f;
        [SerializeField] float maxRotationX = 15f;
        [SerializeField] float minRotationY = -15f;
        [SerializeField] float maxRotationY = 15f;
        [SerializeField] float minRotationZ = -15f;
        [SerializeField] float maxRotationZ = 15f;

        [Header("Position Settings")]     // Initial positions setup


        [Header("Rotation Settings")]     // Initial rotations setup


        [Header("Rotation Limits")]        // Rotation Limits 
        [SerializeField] float minRotationX = -10f;
        [SerializeField] float maxRotationX = 10f;
        [SerializeField] float minRotationY = -15f;
        [SerializeField] float maxRotationY = 15f;
        [SerializeField] float minRotationZ = -10f;
        [SerializeField] float maxRotationZ = 10f;
        [SerializeField] float minRotationTap = -95f;
        [SerializeField] float maxRotationTap = 95f;

        // Speeds for the animation of the in game flight stick or wheel


        private float tapp1currentControlleRotationX = 0f;  // Current rotation for X-axis (stick or wheel)
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
        private GameSystemState systemState; //systemstate
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
                logger.Debug("tapp1controllerX object found.");
                // Store initial position and rotation of the stick
                tapp1controllerXStartPosition = tapp1controllerX.transform.localPosition;
                tapp1controllerXStartRotation = tapp1controllerX.transform.localRotation;

                // Find p1controllerY under tapp1controllerX
                tapp1controllerY = tapp1controllerX.Find("tapp1controllerY");
                if (tapp1controllerY != null)
                {
                    logger.Debug("tapp1controllerY object found.");
                    // Store initial position and rotation of the stick
                    tapp1controllerYStartPosition = tapp1controllerY.transform.localPosition;
                    tapp1controllerYStartRotation = tapp1controllerY.transform.localRotation;

                    // Find tapp1controllerZ under tapp1controllerY
                    tapp1controllerZ = tapp1controllerY.Find("tapp1controllerZ");
                    if (tapp1controllerZ != null)
                    {
                        logger.Debug("tapp1controllerZ object found.");
                        // Store initial position and rotation of the stick
                        tapp1controllerZStartPosition = tapp1controllerZ.transform.localPosition;
                        tapp1controllerZStartRotation = tapp1controllerZ.transform.localRotation;
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
                logger.Debug("p1tapZ object found.");
                // Store initial position and rotation of the stick
                p1tapZStartPosition = p1tapZ.transform.localPosition;
                p1tapZStartRotation = p1tapZ.transform.localRotation;
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
            logger.Debug($"Controlled System Game path String: {controlledSystemGamePathString}");
            logger.Debug("Compatible Rom Dectected, ... Tapping The Beers");
            logger.Debug("Tapper Motion Sim starting...");
            logger.Debug("One Budwiser Please!...");
            inFocusMode = true;  // Set focus mode flag
        }

        void EndFocusMode()
        {
            logger.Debug("Exiting Focus Mode...");
            //player 1
            currentRotationX = 0f;
            currentRotationY = 0f;
            currentRotationZ = 0f;
            tapp1currenttapRotationZ = 0f;
            inFocusMode = false;  // Clear focus mode flag
        }
        private const float THUMBSTICK_DEADZONE = 0.13f; // Adjust as needed

        private Vector2 ApplyDeadzone(Vector2 input, float deadzone)
        {
            input.x = Mathf.Abs(input.x) < deadzone ? 0f : input.x;
            input.y = Mathf.Abs(input.y) < deadzone ? 0f : input.y;
            return input;
        }
        private void MapThumbsticks(ref bool inputDetected)
        {
            if (!inFocusMode) return;

            Vector2 primaryThumbstick = Vector2.zero;
            Vector2 secondaryThumbstick = Vector2.zero;
            float LIndexTrigger = 0f, RIndexTrigger = 0f;
            float primaryHandTrigger = 0f, secondaryHandTrigger = 0f;

            // === INPUT SELECTION WITH DEADZONE ===
            // OVR CONTROLLERS (adds to VR input if both are present)
            if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.Oculus)
            {
                primaryThumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
                secondaryThumbstick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

                LIndexTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger);
                RIndexTrigger = OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger);
                primaryHandTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger);
                secondaryHandTrigger = OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger);

                primaryThumbstick = ApplyDeadzone(primaryThumbstick, THUMBSTICK_DEADZONE);
                secondaryThumbstick = ApplyDeadzone(secondaryThumbstick, THUMBSTICK_DEADZONE);
            }

            // STEAMVR CONTROLLERS (adds to VR input if both are present)
            if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.OpenVR)
            {
                var leftController = SteamVRInput.GetController(HandType.Left);
                var rightController = SteamVRInput.GetController(HandType.Right);
                if (leftController != null) primaryThumbstick += leftController.GetAxis();
                if (rightController != null) secondaryThumbstick += rightController.GetAxis();

                LIndexTrigger = Mathf.Max(LIndexTrigger, SteamVRInput.GetTriggerValue(HandType.Left));
                RIndexTrigger = Mathf.Max(RIndexTrigger, SteamVRInput.GetTriggerValue(HandType.Right));

                primaryThumbstick = ApplyDeadzone(primaryThumbstick, THUMBSTICK_DEADZONE);
                secondaryThumbstick = ApplyDeadzone(secondaryThumbstick, THUMBSTICK_DEADZONE);
            }

            // XBOX CONTROLLER (adds to VR input if both are present)
            if (XInput.IsConnected)
            {
                primaryThumbstick += XInput.Get(XInput.Axis.LThumbstick);
                secondaryThumbstick += XInput.Get(XInput.Axis.RThumbstick);

                // Optionally use Unity Input axes as backup:
                primaryThumbstick += new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                secondaryThumbstick += new Vector2(Input.GetAxis("RightStickHorizontal"), Input.GetAxis("RightStickVertical"));

                LIndexTrigger = Mathf.Max(LIndexTrigger, XInput.Get(XInput.Trigger.LIndexTrigger));
                RIndexTrigger = Mathf.Max(RIndexTrigger, XInput.Get(XInput.Trigger.RIndexTrigger));

                primaryThumbstick = ApplyDeadzone(primaryThumbstick, THUMBSTICK_DEADZONE);
                secondaryThumbstick = ApplyDeadzone(secondaryThumbstick, THUMBSTICK_DEADZONE);
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
            // Thumbstick or D-pad: up (Y+)
            if ((XInput.Get(XInput.Button.DpadUp) || primaryThumbstick.y > 0)
                && tapp1currentControllerRotationY < tapp1controllerMaxRotationY)
            {
                float p1controllerRotateY = thumbstickVelocity * Time.deltaTime;
                float distanceToLimit = tapp1controllerMaxRotationY - tapp1currentControllerRotationY;
                float appliedRotateY = Mathf.Min(p1controllerRotateY, distanceToLimit);
                tapp1controllerY.Rotate(0, 0, -appliedRotateY);
                tapp1currentControllerRotationY += appliedRotateY;
                inputDetected = true; 
 isCenteringRotation = false;
            }

            // Thumbstick or D-pad: down (Y-)
            if ((XInput.Get(XInput.Button.DpadDown) || primaryThumbstick.y < 0)
                && tapp1currentControllerRotationY > tapp1controllerMinRotationY)
            {
                float p1controllerRotateY = thumbstickVelocity * Time.deltaTime;
                float distanceToLimit = tapp1currentControllerRotationY - tapp1controllerMinRotationY;
                float appliedRotateY = Mathf.Min(p1controllerRotateY, distanceToLimit);
                tapp1controllerY.Rotate(0, 0, appliedRotateY);
                tapp1currentControllerRotationY -= appliedRotateY;
                inputDetected = true; 
 isCenteringRotation = false;
            }

            // Thumbstick or D-pad: right (X+)
            if ((XInput.Get(XInput.Button.DpadRight) || primaryThumbstick.x > 0)
                && tapp1currentControllerRotationX < tapp1controllerMaxRotationX)
            {
                float p1controllerRotateX = thumbstickVelocity * Time.deltaTime;
                float distanceToLimit = tapp1controllerMaxRotationX - tapp1currentControllerRotationX;
                float appliedRotateX = Mathf.Min(p1controllerRotateX, distanceToLimit);
                tapp1controllerX.Rotate(-appliedRotateX, 0, 0);
                tapp1currentControllerRotationX += appliedRotateX;
                inputDetected = true; 
 isCenteringRotation = false;
            }

            // Thumbstick or D-pad: left (X-)
            if ((XInput.Get(XInput.Button.DpadLeft) || primaryThumbstick.x < 0)
                && tapp1currentControllerRotationX > tapp1controllerMinRotationX)
            {
                float p1controllerRotateX = thumbstickVelocity * Time.deltaTime;
                float distanceToLimit = tapp1currentControllerRotationX - tapp1controllerMinRotationX;
                float appliedRotateX = Mathf.Min(p1controllerRotateX, distanceToLimit);
                tapp1controllerX.Rotate(appliedRotateX, 0, 0);
                tapp1currentControllerRotationX -= appliedRotateX;
                inputDetected = true; 
 isCenteringRotation = false;
            }

            // Thumbstick or D-pad: Z+ (roll up/right)
            if ((XInput.Get(XInput.Button.DpadUp) || primaryThumbstick.y > 0)
                && tapp1currentControllerRotationZ < tapp1controllerMaxRotationZ)
            {
                float p1controllerRotateZ = thumbstickVelocity * Time.deltaTime;
                float distanceToLimit = tapp1controllerMaxRotationZ - tapp1currentControllerRotationZ;
                float appliedRotateZ = Mathf.Min(p1controllerRotateZ, distanceToLimit);
                tapp1controllerZ.Rotate(0, 0, -appliedRotateZ);
                tapp1currentControllerRotationZ += appliedRotateZ;
                inputDetected = true; 
 isCenteringRotation = false;
            }

            // Thumbstick or D-pad: Z- (roll down/left)
            if ((XInput.Get(XInput.Button.DpadDown) || primaryThumbstick.y < 0)
                && tapp1currentControllerRotationZ > tapp1controllerMinRotationZ)
            {
                float p1controllerRotateZ = thumbstickVelocity * Time.deltaTime;
                float distanceToLimit = tapp1currentControllerRotationZ - tapp1controllerMinRotationZ;
                float appliedRotateZ = Mathf.Min(p1controllerRotateZ, distanceToLimit);
                tapp1controllerZ.Rotate(0, 0, appliedRotateZ);
                tapp1currentControllerRotationZ -= appliedRotateZ;
                inputDetected = true; 
 isCenteringRotation = false;
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
       //     logger.Debug("Centering Tap Rotation - X: " + tapp1currentControllerRotationX + ", Y: " + tapp1currentControllerRotationY + ", Z: " + tapp1currentControllerRotationZ);
        //    logger.Debug("Centering Tap Rotation - Current Tap Z: " + tapp1currenttapRotationZ);
        }




        // Check if object is found and log appropriate message
        void CheckObject(GameObject obj, string name)
        {
            if (obj == null)
            {
                logger.Error($"{gameObject.name} {name} not found!");
            }
            else
            {
                logger.Debug($"{gameObject.name} {name} found.");
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
