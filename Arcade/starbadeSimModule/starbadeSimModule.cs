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

namespace WIGUx.Modules.starbladeSim
{
    public class starbladeSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();
        private float adjustSpeed = 1.0f;  // Adjust this adjustment speed as needed a lower number will lead to smaller adustments

        // Controller animation 

        // Speeds for the animation of the in game flight stick or wheel
        private readonly float keyboardControllerVelocityX = 100.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityY = 100.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityZ = 100.5f;  // Velocity for keyboard input
        private readonly float vrControllerVelocity = 100.5f;        // Velocity for VR/Controller input

        //mouse
        private readonly float mouseSensitivityX = 350.5f;  // Velocity for mouse input
        private readonly float mouseSensitivityY = 350.5f;  // Velocity for mouse input
        private readonly float mouseSensitivityZ = 350.5f;  // Velocity for mouse input
        //controller

        private float starbladeControllerrotationLimitX = 0f;  // Rotation limit for X-axis (stick or wheel)
        private float starbladeControllerrotationLimitY = 20f;  // Rotation limit for Y-axis (stick or wheel)
        private float starbladeControllerrotationLimitZ = 15f;  // Rotation limit for Z-axis (stick or wheel)

        private float starbladeCurrentControllerRotationX = 0f;  // Current rotation for X-axis (stick or wheel)
        private float starbladeCurrentControllerRotationY = 0f;  // Current rotation for Y-axis (stick or wheel)
        private float starbladeCurrentControllerRotationZ = 0f;  // Current rotation for Z-axis (stick or wheel)

        private readonly float starbladeCenteringControllerVelocityX = 25f;  // Velocity for centering rotation (stick or wheel)
        private readonly float starbladeCenteringControllerVelocityY = 25f;  // Velocity for centering rotation (stick or wheel)
        private readonly float starbladeCenteringControllerVelocityZ = 25f;  // Velocity for centering rotation (stick or wheel)

        private Transform starbladeshifter; // Reference to shifter
        private Vector3 starbladeshifterStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion starbladeshifterStartRotation; // Initial controlller positions and rotations for resetting
        private Transform starbladeControllerX; // Reference to the main animated controller (wheel)
        private Vector3 starbladeControllerXStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion starbladeControllerXStartRotation; // Initial controlller positions and rotations for resetting
        private Transform starbladeControllerY; // Reference to the main animated controller (wheel)
        private Vector3 starbladeControllerYStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion starbladeControllerYStartRotation; // Initial controlller positions and rotations for resetting
        private Transform starbladeControllerZ; // Reference to the main animated controller (wheel)
        private Vector3 starbladeControllerZStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion starbladeControllerZStartRotation; // Initial controlller positions and rotations for resetting

        //lights
        private Light starblade_firelight;
        private Transform starblade_fireemissiveObject;
        private Transform lightsObject;
        public Light[] starbladeLights = new Light[2]; // Array to store lights
        public Light starblade1_light;
        public Light starblade2_light;

        private float flashDuration = 0.15f;
        private float flashInterval = 0.15f;
        private float lightDuration = 0.5f; // Duration during which the lights will be on
        private bool arestarbladeLighsOn = false; // track strobe lights
        private Coroutine starbladeCoroutine; // Coroutine variable to control the strobe flashing
        private Light[] lights;
        private bool isstarbladeFlashing = false; //set the flashing flag
        private bool isstarbladeinHigh = false; //set the flashing flag
        private bool inFocusMode = false;  // Flag to track focus mode state
        private readonly string[] compatibleGames = { "starblad.zip" };
        private Dictionary<GameObject, Transform> originalParents = new Dictionary<GameObject, Transform>();  // Dictionary to store original parents of objects
                                                                                                              // Public property to access the Game instance
        void Start()
        {
            logger.Info("Looking For Lights In Starblade");
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
                    case "starblade1_light":
                        starblade1_light = light;
                        starbladeLights[0] = light;
                        logger.Info("Included Light found in object: " + light.gameObject.name);
                        break;
                    case "starblade2_light":
                        starblade2_light = light;
                        starbladeLights[1] = light;
                        logger.Info("Included Light found in object: " + light.gameObject.name);
                        break;
                    default:
                        logger.Info("Excluded Light found in object: " + light.gameObject.name);
                        break;
                }
            }

            // Log the assigned lights for verification
            for (int i = 0; i < starbladeLights.Length; i++)
            {
                if (starbladeLights[i] != null)
                {
                    logger.Info($"starbladeLights[{i}] assigned to: {starbladeLights[i].name}");
                }
                else
                {
                    logger.Error($"starbladeLights[{i}] is not assigned!");
                }
            }
            starbladeshifter = transform.Find("starbladeshifter");
            if (starbladeshifter != null)
            {
                logger.Info("starbladeshifter object found.");
                starbladeshifterStartPosition = starbladeshifter.transform.position;
                starbladeshifterStartRotation = starbladeshifter.transform.rotation;
            }
            // Find starbladeControllerX for player 1
            starbladeControllerX = transform.Find("starbladeControllerX");
            if (starbladeControllerX != null)
            {
                logger.Info("starbladeControllerX object found.");
                // Store initial position and rotation of the stick
                starbladeControllerXStartPosition = starbladeControllerX.transform.position;
                starbladeControllerXStartRotation = starbladeControllerX.transform.rotation;

                // Find starbladeControllerY under starbladep1controllerX
                starbladeControllerY = starbladeControllerX.Find("starbladeControllerY");
                if (starbladeControllerY != null)
                {
                    logger.Info("starbladeControllerY object found.");
                    // Store initial position and rotation of the stick
                    starbladeControllerYStartPosition = starbladeControllerY.transform.position;
                    starbladeControllerYStartRotation = starbladeControllerY.transform.rotation;

                    // Find starbladeControllerZ under starbladeControllerY
                    starbladeControllerZ = starbladeControllerY.Find("starbladeControllerZ");
                    if (starbladeControllerZ != null)
                    {
                        logger.Info("starbladeControllerZ object found.");
                        // Store initial position and rotation of the stick
                        starbladeControllerZStartPosition = starbladeControllerZ.transform.position;
                        starbladeControllerZStartRotation = starbladeControllerZ.transform.rotation;
                        // Find the starblade_firelight within starbladeControllerZ
                        // Find fireemissive object and Light component under starbladeControllerZ
                        Transform lightTransform = starbladeControllerZ.Find("starblade_firelight");

                        if (lightTransform != null)
                        {
                            starblade_firelight = lightTransform.GetComponent<Light>();

                            if (starblade_firelight == null)
                            {
                                Debug.LogError("Light component not found on starblade_firelight.");
                            }
                        }
                        else
                        {
                            Debug.LogError("starblade_firelight object not found under starbladeControllerZ.");
                        }

                        starblade_fireemissiveObject = starbladeControllerZ.Find("starblade_fireemissive");
                        if (starblade_fireemissiveObject != null)
                        {
                            logger.Info("starblade_fireemissive object found.");
                            // Ensure the fireemissive object is initially off
                            Renderer renderer = starblade_fireemissiveObject.GetComponent<Renderer>();
                            if (renderer != null)
                            {
                                renderer.material.DisableKeyword("_EMISSION");
                            }
                            else
                            {
                                logger.Debug("Renderer component is not found on starblade_fireemissive object.");
                            }
                        }
                        else
                        {
                            logger.Debug("starblade_fireemissive object not found under aburnerX.");
                        }
                    }
                    else
                    {
                        logger.Error("starbladeControllerZ object not found under ControllerY!");
                    }
                }
                else
                {
                    logger.Error("starbladeControllerY object not found under ControllerX!");
                }
            }
            else
            {
                logger.Error("starbladeControllerX object not found!!");
            }
            ToggleFireLight(false);
            ToggleFireEmissive(false);
            TogglestarbladeLight1(false);
            TogglestarbladeLight2(false);

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
                HandleMouseRotation(ref inputDetected);
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
            logger.Info("Compatible Rom Dectected, Prepaing Starblade for launch... ");
            logger.Info("Star Blade Module starting...");
            logger.Info("It's been an honor!...");


            // Reset controllers to initial positions and rotations
            if (starbladeControllerX != null)
            {
                starbladeControllerX.position = starbladeControllerXStartPosition;
                starbladeControllerX.rotation = starbladeControllerXStartRotation;
            }
            if (starbladeControllerY != null)
            {
                starbladeControllerY.position = starbladeControllerYStartPosition;
                starbladeControllerY.rotation = starbladeControllerYStartRotation;
            }
            if (starbladeControllerZ != null)
            {
                starbladeControllerZ.position = starbladeControllerZStartPosition;
                starbladeControllerZ.rotation = starbladeControllerZStartRotation;
            }
            if (starbladeshifter != null)
            {
                starbladeshifter.position = starbladeshifterStartPosition;
                starbladeshifter.rotation = starbladeshifterStartRotation;
            }


            // Reset rotation allowances and current rotation values
            starbladeCurrentControllerRotationX = 0f;
            starbladeCurrentControllerRotationY = 0f;
            starbladeCurrentControllerRotationZ = 0f;

            starbladeCoroutine = StartCoroutine(FlashstarbladeLights());
            isstarbladeFlashing = true;
            inFocusMode = true;  // Set focus mode flag
        }

        void EndFocusMode()
        {
            // Reset controllers to initial positions and rotations
            if (starbladeControllerX != null)
            {
                starbladeControllerX.position = starbladeControllerXStartPosition;
                starbladeControllerX.rotation = starbladeControllerXStartRotation;
            }
            if (starbladeControllerY != null)
            {
                starbladeControllerY.position = starbladeControllerYStartPosition;
                starbladeControllerY.rotation = starbladeControllerYStartRotation;
            }
            if (starbladeControllerZ != null)
            {
                starbladeControllerZ.position = starbladeControllerZStartPosition;
                starbladeControllerZ.rotation = starbladeControllerZStartRotation;
            }

            StopCoroutine(starbladeCoroutine);
            TogglestarbladeLight1(false);
            TogglestarbladeLight2(false);
            starbladeCoroutine = null;
            isstarbladeFlashing = false;

            inFocusMode = false;  // Clear focus mode flag
        }
        void HandleMouseRotation(ref bool inputDetected)
        {
            // Get mouse input for Y and X axes
            float mouseRotateZ = Input.GetAxis("Mouse Y") * mouseSensitivityY * Time.deltaTime;
            float mouseRotateY = Input.GetAxis("Mouse X") * mouseSensitivityX * Time.deltaTime;

            // Check current rotation and new proposed rotation for Z axis
            float newRotationZ = starbladeCurrentControllerRotationZ - mouseRotateZ;
            if (newRotationZ >= -starbladeControllerrotationLimitZ && newRotationZ <= starbladeControllerrotationLimitZ)
            {
                starbladeControllerZ.Rotate(0, 0, mouseRotateZ);
                starbladeCurrentControllerRotationZ = newRotationZ; // Update current rotation
                inputDetected = true;
            }

            // Check current rotation and new proposed rotation for Y axis
            float newRotationY = starbladeCurrentControllerRotationY + mouseRotateY;
            if (newRotationY >= -starbladeControllerrotationLimitY && newRotationY <= starbladeControllerrotationLimitY)
            {
                starbladeControllerY.Rotate(0, -mouseRotateY, 0);
                starbladeCurrentControllerRotationY = newRotationY; // Update current rotation
                inputDetected = true;
            }
        }

        //sexy new combined input handler
        void HandleInput(ref bool inputDetected)
        {
            if (!inFocusMode) return;

            Vector2 primaryThumbstick = Vector2.zero;
            Vector2 secondaryThumbstick = Vector2.zero;
            // Fire2
            if (Input.GetButtonDown("Fire1"))
            {
                ToggleFireEmissive(true);
                ToggleFireLight(true);
                inputDetected = true;
            }

            // Reset position on button release
            if (Input.GetButtonUp("Fire1"))
            {
                ToggleFireEmissive(false);
                ToggleFireLight(false);
                inputDetected = true;
            }
            /*
            // Fire2
            if (Input.GetButtonDown("Fire2"))
            {
                ToggleFireEmissive(true);
                ToggleFireLight(true);
                inputDetected = true;
            }

            // Reset position on button release
            if (Input.GetButtonUp("Fire2"))
            {
                ToggleFireEmissive(false);
                ToggleFireLight(false);
                inputDetected = true;
            }

            // Fire2
            if (Input.GetButtonDown("Fire3"))
            {
                ToggleFireEmissive(true);
                ToggleFireLight(true);
                inputDetected = true;
            }

            // Reset position on button release
            if (Input.GetButtonUp("Fire3"))
            {
                ToggleFireEmissive(false);
                ToggleFireLight(false);
                inputDetected = true;
            }

            // Fire2
            if (Input.GetButtonDown("Jump"))
            {
                ToggleFireEmissive(true);
                ToggleFireLight(true);
                inputDetected = true;
            }

            // Reset position on button release
            if (Input.GetButtonUp("Jump"))
            {
                ToggleFireEmissive(false);
                ToggleFireLight(false);
                inputDetected = true;
            }
            */

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
                /*
                if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick))
                {
                    logger.Info("OVR Primary thumbstick pressed");
                    {
                        inputDetected = true;
                    }
                }
                */
                // Check if the secondary index trigger on the left controller is pressed
                if (OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger))
                {
                    logger.Info("OVR Secondary index trigger pressed");
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
            
            // Thumbstick direction: Y
            // Thumbstick direction: Right
            if ((Input.GetKey(KeyCode.RightArrow) || XInput.Get(XInput.Button.DpadRight) || primaryThumbstick.x > 0) && starbladeCurrentControllerRotationY < starbladeControllerrotationLimitY)
            {
                float p1controllerRotateY = (Input.GetKey(KeyCode.RightArrow) || XInput.Get(XInput.Button.DpadRight) ? keyboardControllerVelocityY : primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                starbladeControllerY.Rotate(0, -p1controllerRotateY, 0);
                starbladeCurrentControllerRotationY += p1controllerRotateY;
                inputDetected = true;
            }
            // Thumbstick direction: Left
            if ((Input.GetKey(KeyCode.LeftArrow) || XInput.Get(XInput.Button.DpadLeft) || primaryThumbstick.x < 0) && starbladeCurrentControllerRotationY > -starbladeControllerrotationLimitY)
            {
                float p1controllerRotateY = (Input.GetKey(KeyCode.LeftArrow) || XInput.Get(XInput.Button.DpadLeft) ? keyboardControllerVelocityY : -primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                starbladeControllerY.Rotate(0, p1controllerRotateY, 0);
                starbladeCurrentControllerRotationY -= p1controllerRotateY;
                inputDetected = true;
            }

            // Thumbstick direction: X
            // Thumbstick direction: right
            if ((Input.GetKey(KeyCode.RightArrow) || XInput.Get(XInput.Button.DpadRight) || primaryThumbstick.x > 0) && starbladeCurrentControllerRotationX < starbladeControllerrotationLimitX)
            {
                float p1controllerRotateX = (Input.GetKey(KeyCode.RightArrow) || XInput.Get(XInput.Button.DpadRight) ? keyboardControllerVelocityX : primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                starbladeControllerX.Rotate(-p1controllerRotateX, 0, 0);
                starbladeCurrentControllerRotationX += p1controllerRotateX;
                inputDetected = true;
            }
            // Thumbstick direction: left
            if ((Input.GetKey(KeyCode.LeftArrow) || XInput.Get(XInput.Button.DpadLeft) || primaryThumbstick.x < 0) && starbladeCurrentControllerRotationX > -starbladeControllerrotationLimitX)
            {
                float p1controllerRotateX = (Input.GetKey(KeyCode.LeftArrow) || XInput.Get(XInput.Button.DpadLeft) ? keyboardControllerVelocityX : -primaryThumbstick.x * vrControllerVelocity) * Time.deltaTime;
                starbladeControllerX.Rotate(p1controllerRotateX, 0, 0);
                starbladeCurrentControllerRotationX -= p1controllerRotateX;
                inputDetected = true;
            }
            // Thumbstick direction: Z
            // Thumbstick or D-pad direction: Up
            if ((primaryThumbstick.y > 0 || XInput.Get(XInput.Button.DpadUp)) && starbladeCurrentControllerRotationZ < starbladeControllerrotationLimitZ)
            {
                float p1controllerRotateZ = (Input.GetKey(KeyCode.UpArrow) || XInput.Get(XInput.Button.DpadUp) ? keyboardControllerVelocityZ : primaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                starbladeControllerZ.Rotate(0, 0, -p1controllerRotateZ);
                starbladeCurrentControllerRotationZ += p1controllerRotateZ;
                inputDetected = true;
            }

            // Thumbstick or D-pad direction: Down
            if ((primaryThumbstick.y < 0 || XInput.Get(XInput.Button.DpadDown)) && starbladeCurrentControllerRotationZ > -starbladeControllerrotationLimitZ)
            {
                float p1controllerRotateZ = (Input.GetKey(KeyCode.DownArrow) || XInput.Get(XInput.Button.DpadDown) ? keyboardControllerVelocityZ : -primaryThumbstick.y * vrControllerVelocity) * Time.deltaTime;
                starbladeControllerZ.Rotate(0, 0, p1controllerRotateZ);
                starbladeCurrentControllerRotationZ -= p1controllerRotateZ;
                inputDetected = true;

            }

            /*
            // Thunbstick button pressed
            if (XInput.GetDown(XInput.Button.LThumbstick))
            {
                if (!isstarbladeFlashing)
                {
                    // Start the flashing if not already flashing
                    starbladeCoroutine = StartCoroutine(FlashstarbladeLights());
                    isstarbladeFlashing = true;
                }
                else
                {
                    // Stop the flashing if it's currently active
                    StopCoroutine(starbladeCoroutine);
                    TogglestarbladeLight1(false);
                    TogglestarbladeLight2(false);
                    starbladeCoroutine = null;
                    isstarbladeFlashing = false;
                }
                inputDetected = true;
            }
            */
            // shift button pressed
            if (XInput.GetDown(XInput.Button.Y))
            {
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
                if (starbladeCurrentControllerRotationX > 0)
                {
                    float p1unrotateX = Mathf.Min(starbladeCenteringControllerVelocityX * Time.deltaTime, starbladeCurrentControllerRotationX);
                    starbladeControllerX.Rotate(p1unrotateX, 0, 0);   // Rotating to reduce the rotation
                    starbladeCurrentControllerRotationX -= p1unrotateX;    // Reducing the positive rotation
                }
                else if (starbladeCurrentControllerRotationX < 0)
                {
                    float p1unrotateX = Mathf.Min(starbladeCenteringControllerVelocityX * Time.deltaTime, -starbladeCurrentControllerRotationX);
                    starbladeControllerX.Rotate(-p1unrotateX, 0, 0);   // Rotating to reduce the rotation
                    starbladeCurrentControllerRotationX += p1unrotateX;    // Reducing the positive rotation
                }

                // Center Y-axis Controller rotation
                if (starbladeCurrentControllerRotationY > 0)
                {
                    float p1unrotateY = Mathf.Min(starbladeCenteringControllerVelocityY * Time.deltaTime, starbladeCurrentControllerRotationY);
                    starbladeControllerY.Rotate(0, p1unrotateY, 0);   // Rotating to reduce the rotation
                    starbladeCurrentControllerRotationY -= p1unrotateY;    // Reducing the positive rotation
                }
                else if (starbladeCurrentControllerRotationY < 0)
                {
                    float p1unrotateY = Mathf.Min(starbladeCenteringControllerVelocityY * Time.deltaTime, -starbladeCurrentControllerRotationY);
                    starbladeControllerY.Rotate(0, -p1unrotateY, 0);  // Rotating to reduce the rotation
                    starbladeCurrentControllerRotationY += p1unrotateY;    // Reducing the negative rotation
                }


                // Center Z-axis Controller rotation
                if (starbladeCurrentControllerRotationZ > 0)
                {
                    float p1unrotateZ = Mathf.Min(starbladeCenteringControllerVelocityZ * Time.deltaTime, starbladeCurrentControllerRotationZ);
                    starbladeControllerZ.Rotate(0, 0, p1unrotateZ);   // Rotating to reduce the rotation
                    starbladeCurrentControllerRotationZ -= p1unrotateZ;    // Reducing the positive rotation
                }
                else if (starbladeCurrentControllerRotationZ < 0)
                {
                    float p1unrotateZ = Mathf.Min(starbladeCenteringControllerVelocityZ * Time.deltaTime, -starbladeCurrentControllerRotationZ);
                    starbladeControllerZ.Rotate(0, 0, -p1unrotateZ);   // Rotating to reduce the rotation
                    starbladeCurrentControllerRotationZ += p1unrotateZ;    // Reducing the positive rotation
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

            void ToggleFireEmissive(bool isActive)
            {
                if (starblade_fireemissiveObject != null)
                {
                    Renderer renderer = starblade_fireemissiveObject.GetComponent<Renderer>();
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
                   //     logger.Info($"fireemissive object emission turned {(isActive ? "on" : "off")}.");
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

            void ToggleFireLight(bool isActive)
            {
                // Toggle the light directly if the component is valid
                if (starblade_firelight != null)
                {
                    starblade_firelight.enabled = isActive;
                }
                else
                {
                    Debug.LogWarning("Attempted to toggle a null Light component.");
                }
            }

            // Method to toggle the lights
            void TogglestarbladeLights(bool isActive)
            {
                for (int i = 0; i < lights.Length; i++)
                {
                    lights[i].enabled = isActive;
                }

              //  logger.Info($"Lights turned {(isActive ? "on" : "off")}.");
            }

            IEnumerator FlashstarbladeLights()
            {
                int currentIndex = 0; // Start with the first light in the array

                while (true)
                {
                    // Select the current light
                    Light light = starbladeLights[currentIndex];

                    // Check if the light is not null
                    if (light != null)
                    {
                        // Log the chosen light
                        // logger.Debug($"Flashing {light.name}");

                        // Turn on the chosen light
                        TogglestarbladeLight(light, true);

                        // Wait for the flash duration
                        yield return new WaitForSeconds(flashDuration);

                        // Turn off the chosen light
                        TogglestarbladeLight(light, false);

                        // Wait for the next flash interval
                        yield return new WaitForSeconds(flashInterval - flashDuration);
                    }
                    else
                    {
                        logger.Debug("Light is null.");
                    }

                    // Move to the next light in the array
                    currentIndex = (currentIndex + 1) % starbladeLights.Length;
                }
            }

            void TogglestarbladeLight(Light light, bool isActive)
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

            void TogglestarbladeLight1(bool isActive)
            {
                TogglestarbladeLight(starblade1_light, isActive);
            }

            void TogglestarbladeLight2(bool isActive)
            {
                TogglestarbladeLight(starblade2_light, isActive);
            }

        }
    }

