using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using EmuVR.InputManager;
using System.Collections;
using static XInput;
using static SteamVR_Utils;
using System.IO;

namespace WIGUx.Modules.apbSim
{
    public class apbSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        private readonly float keyboardVelocityX = 25.5f;  // Velocity for keyboard input
        private readonly float keyboardVelocityY = 25.5f;  // Velocity for keyboard input
        private readonly float keyboardVelocityZ = 25.5f;  // Velocity for keyboard input
        private readonly float vrVelocity = 20.5f;        // Velocity for VR controller input

        private float adjustSpeed = 1.0f;  // Adjust this adjustment speed as needed a lower number will lead to smaller adustments

        // Controller animation 


        // Speeds for the animation of the in game flight stick or wheel
        private readonly float keyboardControllerVelocityX = 400.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityY = 400.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityZ = 400.5f;  // Velocity for keyboard input
        private readonly float vrControllerVelocity = 400.5f;        // Velocity for VR/Controller input

        // player 1

        //p1 sticks

        private float apbp1controllerrotationLimitX = 270f;  // Rotation limit for X-axis (stick or wheel)
        private float apbp1controllerrotationLimitY = 0f;  // Rotation limit for Y-axis (stick or wheel)
        private float apbp1controllerrotationLimitZ = 0f;  // Rotation limit for Z-axis (stick or wheel)

        private float apbp1currentControllerRotationX = 0f;  // Current rotation for X-axis (stick or wheel)
        private float apbp1currentControllerRotationY = 0f;  // Current rotation for Y-axis (stick or wheel)
        private float apbp1currentControllerRotationZ = 0f;  // Current rotation for Z-axis (stick or wheel)

        private readonly float apbp1centeringControllerVelocityX = 400.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float apbp1centeringControllerVelocityY = 400.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float apbp1centeringControllerVelocityZ = 400.5f;  // Velocity for centering rotation (stick or wheel)

        private Transform apbp1controllerX; // Reference to the main animated controller (wheel)
        private Vector3 apbp1controllerXStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion apbp1controllerXStartRotation; // Initial controlller positions and rotations for resetting
        private Transform apbp1controllerY; // Reference to the main animated controller (wheel)
        private Vector3 apbp1controllerYStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion apbp1controllerYStartRotation; // Initial controlller positions and rotations for resetting
        private Transform apbp1controllerZ; // Reference to the main animated controller (wheel)
        private Vector3 apbp1controllerZStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion apbp1controllerZStartRotation; // Initial controlller positions and rotations for resetting

        //lights
        private Transform lightsObject;
        public Light[] apbLights = new Light[2]; // Array to store lights
        public Light apb1_light;
        public Light apb2_light;
        public Light apb3_light;
        public Light apb4_light;
        private float flashDuration = 0.15f;
        private float flashInterval = 0.15f;
        private float lightDuration = 0.5f; // Duration during which the lights will be on
        private bool areapbLightsOn = false; // track strobe lights
        private Coroutine apbCoroutine; // Coroutine variable to control the strobe flashing
        private Light[] lights;
        private bool inFocusMode = false;  // Flag to track focus mode state
        private readonly string[] compatibleGames = { "apb" };
        private Dictionary<GameObject, Transform> originalParents = new Dictionary<GameObject, Transform>();  // Dictionary to store original parents of objects
                                                                                                              // Public property to access the Game instance
        void Start()
        {
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
                    case "apb1_light":
                        apb1_light = light;
                        apbLights[0] = light;
                        logger.Info("Included Light found in object: " + light.gameObject.name);
                        break;
                    case "apb2_light":
                        apb2_light = light;
                        apbLights[1] = light;
                        logger.Info("Included Light found in object: " + light.gameObject.name);
                        break;
                    default:
                        logger.Info("Excluded Light found in object: " + light.gameObject.name);
                        break;
                }
            }

            // Log the assigned lights for verification
            for (int i = 0; i < apbLights.Length; i++)
            {
                if (apbLights[i] != null)
                {
                    logger.Info($"apbLights[{i}] assigned to: {apbLights[i].name}");
                }
                else
                {
                    logger.Error($"apbLights[{i}] is not assigned!");
                }
            }

            // Find apbcontrollerX for player 1
            apbp1controllerX = transform.Find("apbp1controllerX");
            if (apbp1controllerX != null)
            {
                logger.Info("apbp1controllerX object found.");
                // Store initial position and rotation of the stick
                apbp1controllerXStartPosition = apbp1controllerX.transform.position;
                apbp1controllerXStartRotation = apbp1controllerX.transform.rotation;

                // Find apbp1controllerY under p1controllerX
                apbp1controllerY = apbp1controllerX.Find("apbp1controllerY");
                if (apbp1controllerY != null)
                {
                    logger.Info("apbp1controllerY object found.");
                    // Store initial position and rotation of the stick
                    apbp1controllerYStartPosition = apbp1controllerY.transform.position;
                    apbp1controllerYStartRotation = apbp1controllerY.transform.rotation;

                    // Find p1controllerZ under p1controllerY
                    apbp1controllerZ = apbp1controllerY.Find("apbp1controllerZ");
                    if (apbp1controllerZ != null)
                    {
                        logger.Info("apbp1controllerZ object found.");
                        // Store initial position and rotation of the stick
                        apbp1controllerZStartPosition = apbp1controllerZ.transform.position;
                        apbp1controllerZStartRotation = apbp1controllerZ.transform.rotation;
                    }
                    else
                    {
                        logger.Error("apbp1controllerZ object not found under apbcontrollerY!");
                    }
                }
                else
                {
                    logger.Error("apbp1controllerY object not found under apbcontrollerX!");
                }
            }
            else
            {
                logger.Error("apbp1controllerX object not found!!");
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
            logger.Info("Compatible Rom Dectected, You are on Duty!...");
            logger.Info("APB Module starting...");
            logger.Info("Grab your Doughnuts!!..");

            // Reset controllers to initial positions and rotations
            if (apbp1controllerX != null)
            {
                apbp1controllerX.position = apbp1controllerXStartPosition;
                apbp1controllerX.rotation = apbp1controllerXStartRotation;
            }
            if (apbp1controllerY != null)
            {
                apbp1controllerY.position = apbp1controllerYStartPosition;
                apbp1controllerY.rotation = apbp1controllerYStartRotation;
            }
            if (apbp1controllerZ != null)
            {
                apbp1controllerZ.position = apbp1controllerZStartPosition;
                apbp1controllerZ.rotation = apbp1controllerZStartRotation;
            }

            //player 1
            apbp1currentControllerRotationX = 0f;
            apbp1currentControllerRotationY = 0f;
            apbp1currentControllerRotationZ = 0f;

            inFocusMode = true;  // Set focus mode flag
        }

        void EndFocusMode()
        {
            // Reset controllers to initial positions and rotations
            if (apbp1controllerX != null)
            {
                apbp1controllerX.position = apbp1controllerXStartPosition;
                apbp1controllerX.rotation = apbp1controllerXStartRotation;
            }
            if (apbp1controllerY != null)
            {
                apbp1controllerY.position = apbp1controllerYStartPosition;
                apbp1controllerY.rotation = apbp1controllerYStartRotation;
            }
            if (apbp1controllerZ != null)
            {
                apbp1controllerZ.position = apbp1controllerZStartPosition;
                apbp1controllerZ.rotation = apbp1controllerZStartRotation;
            }

            StopCoroutine(apbCoroutine);
            ToggleapbLight1(false);
            ToggleapbLight2(false);

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
                //    logger.Info("OVR A button pressed");
                }

                // Check if the B button on the right controller is pressed
                if (OVRInput.GetDown(OVRInput.Button.Two))
                {
                //    logger.Info("OVR B button pressed");
                }

                // Check if the X button on the left controller is pressed
                if (OVRInput.GetDown(OVRInput.Button.Three))
                {
              //      logger.Info("OVR X button pressed");
                }

                // Check if the Y button on the left controller is pressed
                if (OVRInput.GetDown(OVRInput.Button.Four))
                {
                  //  logger.Info("OVR Y button pressed");
                }

                // Check if the primary index trigger on the right controller is pressed
             
                if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
                {
                    if (!areapbLightsOn)
                    {
                        // Start the flashing if not already flashing
                        apbCoroutine = StartCoroutine(FlashapbLights());
                        areapbLightsOn = true;
                    }
                    inputDetected = true;
                }
                if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger))
                {
                    if (apbCoroutine != null)
                    {
                        StopCoroutine(apbCoroutine);
                        apbCoroutine = null;
                    }
                    ToggleapbLight1(false);
                    ToggleapbLight2(false);
                    areapbLightsOn = false;
                    inputDetected = true;
                }
             
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

            // Thumbstick direction: X
            // Thumbstick direction: right
            if ((Input.GetKey(KeyCode.RightArrow) || XInput.Get(XInput.Button.DpadRight) || primaryThumbstick.x > 0) && apbp1currentControllerRotationX < apbp1controllerrotationLimitX)
            {
                float p1controllerRotateX = (Input.GetKey(KeyCode.RightArrow) || XInput.Get(XInput.Button.DpadRight) ? keyboardControllerVelocityX : primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                apbp1controllerX.Rotate(-p1controllerRotateX, 0, 0);
                apbp1currentControllerRotationX += p1controllerRotateX;
                inputDetected = true;
            }
            // Thumbstick direction: left
            if ((Input.GetKey(KeyCode.LeftArrow) || XInput.Get(XInput.Button.DpadLeft) || primaryThumbstick.x < 0) && apbp1currentControllerRotationX > -apbp1controllerrotationLimitX)
            {
                float p1controllerRotateX = (Input.GetKey(KeyCode.LeftArrow) || XInput.Get(XInput.Button.DpadLeft) ? keyboardControllerVelocityX : -primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                apbp1controllerX.Rotate(p1controllerRotateX, 0, 0);
                apbp1currentControllerRotationX -= p1controllerRotateX;
                inputDetected = true;
            }

            // Handle Start button press for plunger position
            if (Input.GetButtonDown("Fire2"))
            {
                if (!areapbLightsOn)
                {
                    // Start the flashing if not already flashing
                    apbCoroutine = StartCoroutine(FlashapbLights());
                    areapbLightsOn = true;
                }
                inputDetected = true;
            }

            // Fire2 button released
            if (Input.GetButtonUp("Fire2"))
            {
                if (apbCoroutine != null)
                {
                    StopCoroutine(apbCoroutine);
                    apbCoroutine = null;
                }
                ToggleapbLight1(false);
                ToggleapbLight2(false);
                areapbLightsOn = false;
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
            if (apbp1currentControllerRotationX > 0)
            {
                float p1unrotateX = Mathf.Min(apbp1centeringControllerVelocityX * Time.deltaTime, apbp1currentControllerRotationX);
                apbp1controllerX.Rotate(p1unrotateX, 0, 0);   // Rotating to reduce the rotation
                apbp1currentControllerRotationX -= p1unrotateX;    // Reducing the positive rotation
            }
            else if (apbp1currentControllerRotationX < 0)
            {
                float p1unrotateX = Mathf.Min(apbp1centeringControllerVelocityX * Time.deltaTime, -apbp1currentControllerRotationX);
                apbp1controllerX.Rotate(-p1unrotateX, 0, 0);   // Rotating to reduce the rotation
                apbp1currentControllerRotationX += p1unrotateX;    // Reducing the positive rotation
            }

            // Center Y-axis Controller rotation
            if (apbp1currentControllerRotationY > 0)
            {
                float p1unrotateY = Mathf.Min(apbp1centeringControllerVelocityY * Time.deltaTime, apbp1currentControllerRotationY);
                apbp1controllerY.Rotate(0, p1unrotateY, 0);   // Rotating to reduce the rotation
                apbp1currentControllerRotationY -= p1unrotateY;    // Reducing the positive rotation
            }
            else if (apbp1currentControllerRotationY < 0)
            {
                float p1unrotateY = Mathf.Min(apbp1centeringControllerVelocityY * Time.deltaTime, -apbp1currentControllerRotationY);
                apbp1controllerY.Rotate(0, -p1unrotateY, 0);  // Rotating to reduce the rotation
                apbp1currentControllerRotationY += p1unrotateY;    // Reducing the negative rotation
            }


            // Center Z-axis Controller rotation
            if (apbp1currentControllerRotationZ > 0)
            {
                float p1unrotateZ = Mathf.Min(apbp1centeringControllerVelocityZ * Time.deltaTime, apbp1currentControllerRotationZ);
                apbp1controllerZ.Rotate(0, 0, p1unrotateZ);   // Rotating to reduce the rotation
                apbp1currentControllerRotationZ -= p1unrotateZ;    // Reducing the positive rotation
            }
            else if (apbp1currentControllerRotationZ < 0)
            {
                float p1unrotateZ = Mathf.Min(apbp1centeringControllerVelocityZ * Time.deltaTime, -apbp1currentControllerRotationZ);
                apbp1controllerZ.Rotate(0, 0, -p1unrotateZ);   // Rotating to reduce the rotation
                apbp1currentControllerRotationZ += p1unrotateZ;    // Reducing the positive rotation
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
        void ToggleapbLights(bool isActive)
        {
            for (int i = 0; i < lights.Length; i++)
            {
                lights[i].enabled = isActive;
            }

            logger.Info($"Lights turned {(isActive ? "on" : "off")}.");
        }

        IEnumerator FlashapbLights()
        {
            int currentIndex = 0; // Start with the first light in the array

            while (true)
            {
                // Select the current light
                Light light = apbLights[currentIndex];

                // Check if the light is not null
                if (light != null)
                {
                    // Log the chosen light
                    // logger.Debug($"Flashing {light.name}");

                    // Turn on the chosen light
                    ToggleapbLight(light, true);

                    // Wait for the flash duration
                    yield return new WaitForSeconds(flashDuration);

                    // Turn off the chosen light
                    ToggleapbLight(light, false);

                    // Wait for the next flash interval
                    yield return new WaitForSeconds(flashInterval - flashDuration);
                }
                else
                {
                    logger.Debug("Light is null.");
                }

                // Move to the next light in the array
                currentIndex = (currentIndex + 1) % apbLights.Length;
            }
        }

        void ToggleapbLight(Light light, bool isActive)
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

        void ToggleapbLight1(bool isActive)
        {
            ToggleapbLight(apb1_light, isActive);
        }

        void ToggleapbLight2(bool isActive)
        {
            ToggleapbLight(apb2_light, isActive);
        }

    }
}
