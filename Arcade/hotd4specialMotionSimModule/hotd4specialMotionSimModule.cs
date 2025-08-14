using EmuVR.InputManager;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using WIGU;
using static OVRPlugin;
using static UnityEngine.UI.Image;

namespace WIGUx.Modules.hotd4specialMotionSim
{
    public class hotd4specialMotionSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        [Header("Object Settings")]
        private Transform seatMotorObject; // Reference to the Seatmotor object
        private GameObject cockpitCam;    // Reference to the Desktop Camera
        private GameObject vrCam;    // Reference to the VR Camera  
        private GameObject playerCamera;   // Reference to the Player Camera
        private GameObject playerVRSetup;   // Reference to the VR Camera

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
        private readonly float chairVelocity = 150f;  // Velocity for keyboard input and seat motor rotation speed
        private readonly float rotationSmoothness = 5f;  //sets the smoothness of the rotation
        private readonly float thumbstickVelocity = 50f;  // Velocity for keyboard input
        private readonly float centeringVelocityX = 25f;  // Velocity for centering rotation
        private readonly float centeringVelocityY = 25f;  // Velocity for centering rotation
        private readonly float centeringVelocityZ = 25f;  // Velocity for centering rotation

        [Header("Position Settings")]     // Initial positions setup
        private Vector3 seatMotorStartPosition; // Initial Seatmotor positions for resetting
        private Vector3 playerCameraStartPosition;  // Initial Player Camera positions for resetting
        private Vector3 playerVRSetupStartPosition;  // Initial PlayerVR positions for resetting
        private Vector3 cockpitCamStartPosition;  // Initial cockpitCam positionsfor resetting
        private Vector3 vrCamStartPosition;    // Initial vrCam positionsfor resetting
        private Vector3 playerCameraInitialWorldScale; // Initial Player Camera world scale for resetting
        private Vector3 playerVRSetupInitialWorldScale; // Initial PlayerVR world scale for resetting

        [Header("Rotation Settings")]     // Initial rotations setup
        private Quaternion seatMotorStartRotation;  // Initial Seatmotor rotation for resetting
        private Quaternion playerCameraStartRotation;  // Initial Player Camera rotation for resetting
        private Quaternion playerVRSetupStartRotation;  // Initial PlayerVR rotation for resetting
        private Quaternion cockpitCamStartRotation;  // Initial cockpitCam rotation for resetting
        private Quaternion vrCamStartRotation;      // Initial VRCam rotation for resetting

        [Header("Timers and States")]  // Store last states and timers
        private bool inFocusMode = false;  // Flag to track focus mode state
        private bool isCenteringRotation = false; // Flag to track centering rotation state
        private bool isRiding = false; // Set riding state to false
        private GameSystemState systemState; //systemstate

        [SerializeField] private float stickCooldown = 1.5f;
        private float lastSeatRotationTime = -Mathf.Infinity;

        [Header("Collider Triggers")]
        [SerializeField] private Collider cockpitCollider;

        [Header("Rom Check")]
        private string configPath;
        private GameSystem gameSystem;  // Cached GameSystem for this cabinet.
        private string insertedGameName = string.Empty;
        private string controlledGameName = string.Empty;
        private Dictionary<GameObject, Transform> originalParents = new Dictionary<GameObject, Transform>();

        void Start()
        {
            CheckInsertedGameName();
            CheckControlledGameName();
            configPath = $"./Emulators/MAME/inputs/{insertedGameName}.ini";
            gameSystem = GetComponent<GameSystem>();
            InitializeLights();
            InitializeObjects();
        }

        void Update()
        {
            CheckInsertedGameName();
            CheckControlledGameName();

            // Enter focus when names match
            if (!string.IsNullOrEmpty(insertedGameName)
                && !string.IsNullOrEmpty(controlledGameName)
                && insertedGameName == controlledGameName
                && !inFocusMode)
            {
                StartFocusMode();
            }
            if (GameSystem.ControlledSystem == null && inFocusMode)
            {
                EndFocusMode();
            }
            if (inFocusMode)
            {
                HandleTransformAdjustment();
                MapThumbsticks();   // new VR stick control
            }
        }
        private const float THUMBSTICK_DEADZONE = 0.13f; // Adjust as needed

        private Vector2 ApplyDeadzone(Vector2 input, float deadzone)
        {
            input.x = Mathf.Abs(input.x) < deadzone ? 0f : input.x;
            input.y = Mathf.Abs(input.y) < deadzone ? 0f : input.y;
            return input;
        }
        // New: map VR thumbsticks to seat motor rotation
        private void MapThumbsticks()
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
            // Calculate target yaw relative to starting rotation
            float baseYaw = seatMotorStartRotation.eulerAngles.y;
            float targetYaw = baseYaw;
            if (primaryThumbstick.y > 0.5f)
                targetYaw = baseYaw;               // Up: original
            else if (primaryThumbstick.y < -0.5f)
                targetYaw = baseYaw + 180f;        // Down
            else if (primaryThumbstick.x < -0.5f)
                targetYaw = baseYaw + 45f;         // Left
            else if (primaryThumbstick.x > 0.5f)
                targetYaw = baseYaw - 45f;         // Right
            else
                return;

            // Enforce 3s delay between rotations
            lastSeatRotationTime = Time.time;
            StartCoroutine(RotateSeatMotorToAngle(targetYaw));
        }
        void StartFocusMode()
        {
            logger.Debug($"{gameObject.name} Starting Focus Mode...");
            // StopCurrentPatterns();
            if (cockpitCam != null)
            {
                cockpitCam.transform.localPosition = cockpitCamStartPosition;
                cockpitCam.transform.localRotation = cockpitCamStartRotation;
            }
            if (vrCam != null)
            {
                vrCam.transform.localPosition = vrCamStartPosition;
                vrCam.transform.localRotation = vrCamStartRotation;
            }
            if (playerCamera != null)
            {
                playerCameraStartPosition = playerCamera.transform.position;
                playerCameraStartRotation = playerCamera.transform.rotation;
            }

            if (playerVRSetup != null)
            {
                playerVRSetupStartPosition = playerVRSetup.transform.position;
                playerVRSetupStartRotation = playerVRSetup.transform.rotation;
            }
            // Check containment
            bool inside = false;
            if (cockpitCollider != null)
            {
                Vector3 camPos = playerCamera.transform.position;
                bool boundsContains = cockpitCollider.bounds.Contains(camPos);
                Vector3 closest = cockpitCollider.ClosestPoint(camPos);
                inside = (closest == camPos);
                // logger.Debug($"{gameObject.name} Containment check - bounds.Contains: {boundsContains}, ClosestPoint==pos: {inside}");
            }

            if (cockpitCollider != null && inside)
            {
                if (playerVRSetup == null)
                {
                    // Parent and apply offset to PlayerCamera
                    SaveOriginalParent(playerCamera);
                    Vector3 worldPos = playerCamera.transform.position;
                    Quaternion worldRot = playerCamera.transform.rotation;
                    playerCameraInitialWorldScale = playerCamera.transform.lossyScale;
                    KeyEmulator.SendQandEKeypress();
                    playerCamera.transform.SetParent(cockpitCam.transform, false);
                    playerCamera.transform.position = worldPos;
                    playerCamera.transform.rotation = worldRot;
                    NormalizeWorldScale(playerCamera, cockpitCam.transform);

                    logger.Debug($"{gameObject.name} Player is aboard and strapped in.");
                    isRiding = true; // Set riding state to true
                }
                if (playerVRSetup != null)
                {
                    // Parent and apply offset to PlayerVRSetup
                    SaveOriginalParent(playerVRSetup);
                    Vector3 worldPos = playerVRSetup.transform.position;
                    Quaternion worldRot = playerVRSetup.transform.rotation;
                    playerVRSetupInitialWorldScale = playerVRSetup.transform.lossyScale;
                    KeyEmulator.SendQandEKeypress();
                    playerVRSetup.transform.SetParent(vrCam.transform, false);
                    playerVRSetup.transform.position = worldPos;
                    playerVRSetup.transform.rotation = worldRot;
                    NormalizeWorldScale(playerVRSetup, vrCam.transform);

                    logger.Debug($"{gameObject.name} VR Player is aboard and strapped in!");
                    logger.Debug("Watch out for Zombies behind you!");
                    logger.Debug("Dont Get Dizzy!");
                    isRiding = true; // Set riding state to true
                }
            }
            else
            {
                logger.Debug("Player is not aboard the ride, Starting Without the player aboard.");
            }

            inFocusMode = true;
        }

        void EndFocusMode()
        {
            logger.Debug("Exiting Focus Mode...");
            RestoreOriginalParent(playerCamera, "PlayerCamera");
            RestoreOriginalParent(playerVRSetup, "PlayerVRSetup.PlayerRig");
            ResetPositions();
            inFocusMode = false;
        }

        // Reset PlayerCamera and PlayerVRSetup positions and rotations
        void ResetPositions()
        {
            if (seatMotorObject != null)
            {
                seatMotorObject.localPosition = seatMotorStartPosition;
                seatMotorObject.localRotation = seatMotorStartRotation;
            }
            if (isRiding == true)
            {
                if (cockpitCam != null)
                {
                    cockpitCam.transform.localPosition = cockpitCamStartPosition;
                    cockpitCam.transform.localRotation = cockpitCamStartRotation;
                }
                if (vrCam != null)
                {
                    vrCam.transform.localPosition = vrCamStartPosition;
                    vrCam.transform.localRotation = vrCamStartRotation;
                }
                if (playerVRSetup != null)
                {
                    playerVRSetup.transform.position = playerVRSetupStartPosition;
                    playerVRSetup.transform.rotation = playerVRSetupStartRotation;
                }
                if (playerCamera != null)
                {
                    playerCamera.transform.position = playerCameraStartPosition;
                    playerCamera.transform.rotation = playerCameraStartRotation;
                }
                isRiding = false; // Set riding state to false
            }
            else
            {
                logger.Debug("Player was not aboard the ride, skipping reset.");
            }
        }

        void HandleTransformAdjustment()
        {
            if (!inFocusMode) return;

            bool cockpitCamMoved = false;
            bool vrCamMoved = false;

            // Move BOTH cameras if isRiding is true
            if (isRiding)
            {
                // Desktop camera (cockpitCam)
                if (cockpitCam != null)
                {
                    if (Input.GetKey(KeyCode.Home))
                    {
                        cockpitCam.transform.localPosition += Vector3.forward * adjustSpeed * Time.deltaTime;
                        cockpitCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.End))
                    {
                        cockpitCam.transform.localPosition += Vector3.back * adjustSpeed * Time.deltaTime;
                        cockpitCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.UpArrow))
                    {
                        cockpitCam.transform.localPosition += Vector3.up * adjustSpeed * Time.deltaTime;
                        cockpitCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.DownArrow))
                    {
                        cockpitCam.transform.localPosition += Vector3.down * adjustSpeed * Time.deltaTime;
                        cockpitCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.LeftArrow))
                    {
                        cockpitCam.transform.localPosition += Vector3.left * adjustSpeed * Time.deltaTime;
                        cockpitCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.RightArrow))
                    {
                        cockpitCam.transform.localPosition += Vector3.right * adjustSpeed * Time.deltaTime;
                        cockpitCamMoved = true;
                    }
                    if (Input.GetKeyDown(KeyCode.Backspace))
                    {
                        cockpitCam.transform.Rotate(0, 90, 0);
                        cockpitCamMoved = true;
                    }
                }

                // VR camera (vrCam)
                if (vrCam != null)
                {
                    if (Input.GetKey(KeyCode.Home))
                    {
                        vrCam.transform.localPosition += Vector3.forward * adjustSpeed * Time.deltaTime;
                        vrCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.End))
                    {
                        vrCam.transform.localPosition += Vector3.back * adjustSpeed * Time.deltaTime;
                        vrCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.UpArrow))
                    {
                        vrCam.transform.localPosition += Vector3.up * adjustSpeed * Time.deltaTime;
                        vrCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.DownArrow))
                    {
                        vrCam.transform.localPosition += Vector3.down * adjustSpeed * Time.deltaTime;
                        vrCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.LeftArrow))
                    {
                        vrCam.transform.localPosition += Vector3.left * adjustSpeed * Time.deltaTime;
                        vrCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.RightArrow))
                    {
                        vrCam.transform.localPosition += Vector3.right * adjustSpeed * Time.deltaTime;
                        vrCamMoved = true;
                    }
                    if (Input.GetKeyDown(KeyCode.Backspace))
                    {
                        vrCam.transform.Rotate(0, 90, 0);
                        vrCamMoved = true;
                    }
                }
            }

            // Save and log **only if there was a change**
            if (vrCam != null && vrCamMoved)
            {
                vrCamStartPosition = vrCam.transform.localPosition;
                vrCamStartRotation = vrCam.transform.localRotation;
                Debug.Log($"{gameObject.name}vrCam localPosition: " + vrCam.transform.localPosition.ToString("F4"));
            }
            if (cockpitCam != null && cockpitCamMoved)
            {
                cockpitCamStartPosition = cockpitCam.transform.localPosition;
                cockpitCamStartRotation = cockpitCam.transform.localRotation;
                Debug.Log($"{gameObject.name} cockpitCam localPosition: " + cockpitCam.transform.localPosition.ToString("F4"));
            }
        }
        // Coroutine to rotate the seat motor to a specific Y angle using chairVelocity
        private IEnumerator RotateSeatMotorToAngle(float targetY)
        {
            if (seatMotorObject == null)
                yield break;

            // Get current Euler angles
            Vector3 currentEuler = seatMotorObject.eulerAngles;
            float currentY = currentEuler.y;

            // Rotate towards the target angle
            while (!Mathf.Approximately(currentY, targetY))
            {
                currentY = Mathf.MoveTowardsAngle(currentY, targetY, chairVelocity * Time.deltaTime);
                seatMotorObject.eulerAngles = new Vector3(currentEuler.x, currentY, currentEuler.z);
                yield return null;
            }
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
        void NormalizeWorldScale(GameObject obj, Transform newParent)
        {
            if (obj == null || newParent == null) return;

            Vector3 parentWorldScale = newParent.lossyScale;
            Vector3 targetWorldScale = Vector3.one;

            // Use the correct initial world scale for each object
            if (obj == playerCamera)
                targetWorldScale = playerCameraInitialWorldScale;
            else if (obj == playerVRSetup)
                targetWorldScale = playerVRSetupInitialWorldScale;
            else
                return; // Do nothing for unknown objects

            obj.transform.localScale = new Vector3(
                targetWorldScale.x / parentWorldScale.x,
                targetWorldScale.y / parentWorldScale.y,
                targetWorldScale.z / parentWorldScale.z
            );
        }
        // Save original parent of object in dictionary
        void SaveOriginalParent(GameObject obj)
        {
            if (obj != null && !originalParents.ContainsKey(obj))
            {
                if (obj.transform.parent != null)
                {
                    originalParents[obj] = obj.transform.parent;
                }
                else
                {
                    originalParents[obj] = null; // Explicitly store that this was in the root
                    logger.Debug($"{gameObject.name} Object {obj.name} was in the root and has no parent.");
                }
            }
        }

        // Restore original parent of object and log appropriate message
        void RestoreOriginalParent(GameObject obj, string name)
        {
            if (obj == null)
            {
                logger.Error($"RestoreOriginalParent: {name} is NULL!");
                return;
            }

            if (!originalParents.ContainsKey(obj))
            {
                logger.Warning($"RestoreOriginalParent: No original parent found for {name}");
                return;
            }

            Transform originalParent = originalParents[obj];

            Vector3 worldPos = obj.transform.position;
            Quaternion worldRot = obj.transform.rotation;

            // If the original parent was NULL, place the object back in the root
            if (originalParent == null)
            {
                obj.transform.SetParent(null, false);  // Moves it back to the root
                obj.transform.position = worldPos;
                obj.transform.rotation = worldRot;

                // When no parent, just set localScale to initial world scale
                if (obj == playerCamera)
                    obj.transform.localScale = playerCameraInitialWorldScale;
                else if (obj == playerVRSetup)
                    obj.transform.localScale = playerVRSetupInitialWorldScale;

                logger.Debug($"{name} restored to root.");
            }
            else
            {
                obj.transform.SetParent(originalParent, false);
                obj.transform.position = worldPos;
                obj.transform.rotation = worldRot;
                NormalizeWorldScale(obj, originalParent);

                logger.Debug($"{name} restored to original parent: {originalParent.name}");
            }
        }
        private void CheckInsertedGameName()
        {
            if (gameSystem != null && gameSystem.Game != null && !string.IsNullOrEmpty(gameSystem.Game.path))
                insertedGameName = FileNameHelper.GetFileName(gameSystem.Game.path);
            else
                insertedGameName = string.Empty;
        }

        private void CheckControlledGameName()
        {
            if (GameSystem.ControlledSystem != null && GameSystem.ControlledSystem.Game != null
                && !string.IsNullOrEmpty(GameSystem.ControlledSystem.Game.path))
                controlledGameName = FileNameHelper.GetFileName(GameSystem.ControlledSystem.Game.path);
            else
                controlledGameName = string.Empty;
        }

        // Helper class to extract and sanitize file names.
        public static class FileNameHelper
        {
            // Extracts the file name without the extension and replaces invalid file characters with underscores.
            public static string GetFileName(string filePath)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string FileName = System.Text.RegularExpressions.Regex.Replace(fileName, "[\\/:*?\"<>|]", "_");
                return FileName;
            }
        }
        public static class KeyEmulator
        {
            // Virtual key codes for Q and E
            const byte VK_Q = 0x51;
            const byte VK_E = 0x45;
            const uint KEYEVENTF_KEYDOWN = 0x0000;
            const uint KEYEVENTF_KEYUP = 0x0002;

            [DllImport("user32.dll")]
            static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

            public static void SendQandEKeypress()
            {
                // Send Q down
                keybd_event(VK_Q, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                // Send E down
                keybd_event(VK_E, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);

                // Send Q up
                keybd_event(VK_Q, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                // Send E up
                keybd_event(VK_E, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
        }
        void InitializeLights()
        {
            // Gets all Light components in the target object and its children
            Light[] lights = transform.GetComponentsInChildren<Light>(true);

            foreach (Light light in lights)
            {
                switch (light.gameObject.name)
                {
                    default:
                        logger.Debug($"{gameObject.name} Excluded Light found in object: " + light.gameObject.name);
                        break;
                }
            }
        }
        void InitializeObjects()
        {
            // Find references to PlayerCamera and VR setup objects
            playerCamera = PlayerVRSetup.PlayerCamera.gameObject;

            // Find and assign the whole VR rig try SteamVR first, then Oculus
            playerVRSetup = GameObject.Find("Player/[SteamVRCameraRig]");
            // If not found, try to find the Oculus VR rig
            if (playerVRSetup == null)
            {
                playerVRSetup = GameObject.Find("OVRCameraRig");
            }

            // Check if objects are found
            CheckObject(playerCamera, "PlayerCamera");
            if (playerVRSetup != null)
            {
                CheckObject(playerVRSetup, playerVRSetup.name); // will print either [SteamVRCameraRig] or OVRCameraRig
            }
            else
            {
                logger.Debug($"{gameObject.name} No VR Devices found. No SteamVR or OVR present)");
            }
            // Find seatMotor object in hierarchy
            seatMotorObject = transform.Find("seatMotor");
            if (seatMotorObject != null)
            {
                logger.Debug("seatMotor object found.");
                seatMotorStartPosition = seatMotorObject.localPosition;
                seatMotorStartRotation = seatMotorObject.localRotation;

                // Find vrCam under seatMotor
                vrCam = seatMotorObject.Find("eyes/vrcam")?.gameObject;
                if (vrCam != null)
                {
                    logger.Debug("vrCam object found.");

                    // Store initial position and rotation of  vrCam
                    vrCamStartPosition = vrCam.transform.localPosition;
                    vrCamStartRotation = vrCam.transform.localRotation;
                }
                else
                {
                    logger.Error("vrcam object not found under seatMotor!");
                }
                // Find cockpitcam under seatMotor
                cockpitCam = seatMotorObject.Find("eyes/cockpitcam")?.gameObject;
                if (cockpitCam != null)
                {
                    logger.Debug("Cockpitcam object found.");

                    // Store initial position and rotation of cockpit cam
                    cockpitCamStartPosition = cockpitCam.transform.localPosition;
                    cockpitCamStartRotation = cockpitCam.transform.localRotation;
                }
                else
                {
                    logger.Error("Cockpitcam object not found under seatMotor!");
                }

            }
            else
            {
                logger.Error("seatMotor object not found!");
            }


            // Attempt to find cockpitCollider by name
            if (cockpitCollider == null)
            {
                Collider[] colliders = GetComponentsInChildren<Collider>(true); // true = include inactive
                foreach (var col in colliders)
                {
                    if (col.gameObject.name == "Cockpit")
                    {
                        cockpitCollider = col;
                        logger.Debug($"{gameObject.name} cockpitCollider found in children: {cockpitCollider.name}");
                        break;
                    }
                }
            }
        }
    }
}