using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using EmuVR.InputManager;
using System.Collections;

namespace WIGUx.Modules.shangonMotionSim
{
    public class shangonMotionSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        private readonly float keyboardVelocityX = 40.5f;  // Velocity for keyboard input
        private readonly float keyboardVelocityY = 40.5f;  // Velocity for keyboard input
        private readonly float keyboardVelocityZ = 40.5f;  // Velocity for keyboard input
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

        private Transform shangonXObject; // Reference to the main X object
        private Transform shangonYObject; // Reference to the main Y object
        private Transform shangonZObject; // Reference to the main Z object
        private GameObject cockpitCam;    // Reference to the cockpit camera

        // Initial positions and rotations for resetting
        private Vector3 shangonXStartPosition;
        private Quaternion shangonXStartRotation;
        private Vector3 shangonYStartPosition;
        private Quaternion shangonYStartRotation;
        private Vector3 shangonZStartPosition;
        private Quaternion shangonZStartRotation;
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
        private Transform centerEyeAnchor;

        //lights
        private Transform fireemissiveObject;
        private Transform fireemissive2Object;
        public string fire1Button = "Fire1"; // Name of the fire button
        public string fire2Button = "Fire2"; // Name of the fire button 
        public string fire3Button = "Fire3"; // Name of the fire button 
        public string JumpButton = "Jump"; // Name of the fire button 
        public Light fire1_light;
        public Light fire2_light;
        public float lightDuration = 0.35f; // Duration during which the lights will be on
        private Light[] lights;

        private bool inFocusMode = false;  // Flag to track focus mode state

        private readonly string[] compatibleGames = { "shangon" };

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
            if (cameraObject != null)
            {
                centerEyeAnchor = cameraObject.transform;
            }

            // Find gfpce2X object in hierarchy
            shangonXObject = transform.Find("shangonX");
            if (shangonXObject != null)
            {
                logger.Info("shangonX object found.");
                shangonXStartPosition = shangonXObject.position;
                shangonXStartRotation = shangonXObject.rotation;

                // Find shangonY object under shangonX
                shangonYObject = shangonXObject.Find("shangonY");
                if (shangonYObject != null)
                {
                    logger.Info("shangonY object found.");
                    shangonYStartPosition = shangonYObject.position;
                    shangonYStartRotation = shangonYObject.rotation;

                    // Find shangonZ object under shangonX
                    shangonZObject = shangonYObject.Find("shangonZ");
                    if (shangonZObject != null)
                    {
                        logger.Info("shangonZ object found.");
                        shangonZStartPosition = shangonZObject.position;
                        shangonZStartRotation = shangonZObject.rotation;

                        // Find cockpit camera under cockpit
                        cockpitCam = shangonZObject.Find("eyes/cockpitcam")?.gameObject;
                        if (cockpitCam != null)
                        {
                            logger.Info("Cockpitcam object found.");

                            // Store initial position and rotation of cockpit cam
                            cockpitCamStartPosition = cockpitCam.transform.position;
                            cockpitCamStartRotation = cockpitCam.transform.rotation;
                        }
                        else
                        {
                            logger.Error("Cockpitcam object not found under shangonZ!");
                        }
                    }
                    else
                    {
                        logger.Error("shangonZ object not found under shangonY!");
                    }
                }

                else
                {
                    logger.Error("shangonY object not found under shangonX!");
                }
            }
            else
            {
                logger.Error("shangonX object not found!");
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
                HandleVRInput(ref inputDetected);  // Pass by reference
                HandleKeyboardInput(ref inputDetected);  // Pass by reference
                HandleTransformAdjustment();            
            }
        }


        void StartFocusMode()
        {
            string controlledSystemGamePathString = GameSystem.ControlledSystem.Game.path != null ? GameSystem.ControlledSystem.Game.path.ToString() : null;
            logger.Info($"Controlled System Game path String: {controlledSystemGamePathString}");
            logger.Info("Compatible Rom Dectected, Greetings Kazuma Kiryu...");
            logger.Info("Sega Hang On DX Motion Sim starting...");
            logger.Info("HANG ON!!..");
            cockpitCam.transform.position = cockpitCamStartPosition; // new hotness
            cockpitCam.transform.rotation = cockpitCamStartRotation; // new hotness

            // Set objects as children of cockpit cam for focus mode
            if (cockpitCam != null)
            {
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
                logger.Error("CockpitCam object not found under Chair!");
            }

            // Reset rotation allowances and current rotation values
            currentRotationX = 0f;
            currentRotationY = 0f;
            currentRotationZ = 0f;

            inFocusMode = true;  // Set focus mode flag
        }

        void EndFocusMode()
        {
            logger.Info("Exiting Focus Mode...");
            // Restore original parents of objects
            RestoreOriginalParent(playerCamera, "PlayerCamera");
            RestoreOriginalParent(playerVRSetup, "PlayerVRSetup.PlayerRig");

            // Reset shangonX to initial positions and rotations
            if (shangonXObject != null)
            {
                shangonXObject.position = shangonXStartPosition;
                shangonXObject.rotation = shangonXStartRotation;
            }

            // Reset shangonY object to initial position and rotation
            if (shangonYObject != null)
            {
                shangonYObject.position = shangonYStartPosition;
                shangonYObject.rotation = shangonYStartRotation;
            }
            // Reset shangonZ object to initial position and rotation
            if (shangonZObject != null)
            {
                shangonZObject.position = shangonZStartPosition;
                shangonZObject.rotation = shangonZStartRotation;
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

            logger.Info("Resetting Positions");
            ResetPositions();

            inFocusMode = false;  // Clear focus mode flag
        }

        void HandleKeyboardInput(ref bool inputDetected)
        {
            if (!inFocusMode) return;

            /*
            // Handle keyboard input for pitch and roll
            if (Input.GetKey(KeyCode.LeftArrow) && currentRotationX > -rotationLimitX)
            {
                float rotateX = keyboardVelocityX * Time.deltaTime;
                shangonYObject.Rotate(rotateX, 0, 0);
                currentRotationX -= rotateX;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.RightArrow) && currentRotationX < rotationLimitX)
            {
                float rotateX = keyboardVelocityX * Time.deltaTime;
                shangonYObject.Rotate(-rotateX, 0, 0);
                currentRotationX += rotateX;
                inputDetected = true;
            }
            */
            /*
            if (Input.GetKey(KeyCode.RightArrow) && currentRotationY > -rotationLimitY)
            {
                float rotateY = keyboardVelocityY * Time.deltaTime;
                shangonYObject.Rotate(0, rotateY, 0);
                currentRotationY -= rotateY;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.LeftArrow) && currentRotationY < rotationLimitY)
            {
                float rotateY = keyboardVelocityY * Time.deltaTime;
                shangonYObject.Rotate(0, -rotateY, 0);
                currentRotationY += rotateY;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.DownArrow) && currentRotationZ > -rotationLimitZ)
            {
                float rotateZ = keyboardVelocityZ * Time.deltaTime;
                shangonXObject.Rotate(0, 0, rotateZ);
                currentRotationZ -= rotateZ;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.UpArrow) && currentRotationZ < rotationLimitZ)
            {
                float rotateZ = keyboardVelocityZ * Time.deltaTime;
                shangonXObject.Rotate(0, 0, -rotateZ);
                currentRotationZ += rotateZ;
                inputDetected = true;
            }
            */
            // Center the rotation if no input is detected
            if (!inputDetected)
            {
                CenterRotation();
            }
        }

        void HandleVRInput(ref bool inputDetected)
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
                    Debug.Log("OVR A button pressed");
                }

                // Check if the B button on the right controller is pressed
                if (OVRInput.GetDown(OVRInput.Button.Two))
                {
                    Debug.Log("OVR B button pressed");
                }

                // Check if the X button on the left controller is pressed
                if (OVRInput.GetDown(OVRInput.Button.Three))
                {
                    Debug.Log("OVR X button pressed");
                }

                // Check if the Y button on the left controller is pressed
                if (OVRInput.GetDown(OVRInput.Button.Four))
                {
                    Debug.Log("OVR Y button pressed");
                }

                // Check if the primary index trigger on the right controller is pressed
                if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger))
                {
                    Debug.Log("OVR Primary index trigger pressed");
                }

                // Check if the secondary index trigger on the left controller is pressed
                if (OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger))
                {
                    Debug.Log("OVR Secondary index trigger pressed");
                }

                // Check if the primary hand trigger on the right controller is pressed
                if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger))
                {
                    Debug.Log("OVR Primary hand trigger pressed");
                }

                // Check if the secondary hand trigger on the left controller is pressed
                if (OVRInput.Get(OVRInput.Button.SecondaryHandTrigger))
                {
                    Debug.Log("OVR Secondary hand trigger pressed");
                }

                // Check if the primary thumbstick is pressed
                if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick))
                {
                    Debug.Log("OVR Primary thumbstick pressed");
                }

                // Check if the secondary thumbstick is pressed
                if (OVRInput.GetDown(OVRInput.Button.SecondaryThumbstick))
                {
                    Debug.Log("OVR Secondary thumbstick pressed");
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
                    ToggleFireEmissive1(true);
                    ToggleLight1(true);
                    ToggleLight2(true);
                    inputDetected = true;
                }

                // Reset position on button release
                if (XInput.GetUp(XInput.Button.LIndexTrigger))
                {
                    ToggleFireEmissive1(false);
                    ToggleLight1(false);
                    ToggleLight2(false);
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

            // Handle X rotation for shangonYObject and shangonControllerX (Right Arrow or primaryThumbstick.x > 0)
            // Thumbstick direction: right
            if ((Input.GetKey(KeyCode.RightArrow) || primaryThumbstick.x > 0))
            {
                if (currentRotationX < rotationLimitX)
                {
                    float rotateX = (Input.GetKey(KeyCode.RightArrow) ? keyboardVelocityX : primaryThumbstick.x * vrVelocity) * Time.deltaTime;
                    shangonYObject.Rotate(-rotateX, 0, 0);
                    currentRotationX += rotateX;
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
                    shangonYObject.Rotate(rotateX, 0, 0);
                    currentRotationX -= rotateX;
                    inputDetected = true;
                }
            }

            /*
// Handle X rotation for shangonYObject(Right Arrow or primaryThumbstick.x > 0)
if (Input.GetKey(KeyCode.RightArrow) || primaryThumbstick.x > 0)
{
        if (currentRotationX < rotationLimitX)
        {
            float inputVelocityX = Input.GetKey(KeyCode.RightArrow) ? keyboardVelocityX : primaryThumbstick.x * vrVelocity;
            float targetRotationX = currentRotationX + inputVelocityX * Time.deltaTime;
            targetRotationX = Mathf.Clamp(targetRotationX, -rotationLimitX, rotationLimitX); // Ensure we don't exceed the limits
            float rotateX = Mathf.Lerp(currentRotationX, targetRotationX, 0.1f); // Smooth the rotation
            shangonYObject.Rotate(-rotateX, 0, 0);
            currentRotationX = targetRotationX;
            inputDetected = true;
        }
    }

// Handle X rotation for shangonYObject (Left Arrow or primaryThumbstick.x < 0)
if (Input.GetKey(KeyCode.LeftArrow) || primaryThumbstick.x < 0)
{
    if (currentRotationX > -rotationLimitX)
    {
        float inputVelocityX = Input.GetKey(KeyCode.LeftArrow) ? keyboardVelocityX : -primaryThumbstick.x * vrVelocity;
        float targetRotationX = currentRotationX + inputVelocityX * Time.deltaTime;
        targetRotationX = Mathf.Clamp(targetRotationX, -rotationLimitX, rotationLimitX); // Ensure we don't exceed the limits
        float rotateX = Mathf.Lerp(currentRotationX, targetRotationX, 0.1f); // Smooth the rotation
        shangonYObject.Rotate(rotateX, 0, 0);
        currentRotationX = targetRotationX;
        inputDetected = true;
    }
}
// Center the rotation if no input is detected (i think this is redundant)
            */
            if (!inputDetected)
            {
                // CenterRotation();
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
                shangonYObject.Rotate(unrotateX, 0, 0);
                currentRotationX -= unrotateX;
            }
            else if (currentRotationX < 0)
            {
                float unrotateX = Mathf.Min(centeringVelocityX * Time.deltaTime, -currentRotationX);
                shangonYObject.Rotate(-unrotateX, 0, 0);
                currentRotationX += unrotateX;
            }

            // Center Y-axis
            if (currentRotationY > 0)
            {
                float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, currentRotationY);
                shangonXObject.Rotate(0, unrotateY, 0);
                currentRotationY -= unrotateY;
            }
            else if (currentRotationY < 0)
            {
                float unrotateY = Mathf.Min(centeringVelocityY * Time.deltaTime, -currentRotationY);
                shangonXObject.Rotate(0, -unrotateY, 0);
                currentRotationY += unrotateY;
            }

            // Center Z-axis
            if (currentRotationZ > 0)
            {
                float unrotateZ = Mathf.Min(centeringVelocityZ * Time.deltaTime, currentRotationZ);
                shangonXObject.Rotate(0, 0, unrotateZ);
                currentRotationZ -= unrotateZ;
            }
            else if (currentRotationZ < 0)
            {
                float unrotateZ = Mathf.Min(centeringVelocityZ * Time.deltaTime, -currentRotationZ);
                shangonXObject.Rotate(0, 0, -unrotateZ);
                currentRotationZ += unrotateZ;
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
