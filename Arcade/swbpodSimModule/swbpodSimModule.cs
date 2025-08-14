using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using EmuVR.InputManager;
using System.Collections;
using System;
using System.IO;

namespace WIGUx.Modules.swbpodSimModule
{
    public class swbpodSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();
        private bool inFocusMode = false;  // Flag to track focus mode state
        private GameSystemState systemState; //systemstate

        [Header("Stick Settings")]
        private Transform LStickObject; // Object controlled by the left stick rotation (mirrored)
        private Transform RStickObject; // Object controlled by the right stick forward/backward

        [Header("Velocity Multiplier Settings")]        // Speeds for the animation of the in game flight stick or wheel
        public float primaryThumbstickRotationMultiplier = 20f; // Multiplier for primary thumbstick rotation intensity
        public float secondaryThumbstickRotationMultiplier = 40f; // Multiplier for secondary thumbstick rotation intensity

        [Header("Input Settings")]
        public string primaryThumbstickHorizontal = "Horizontal"; // Input axis for primary thumbstick horizontal
        public string primaryThumbstickVertical = "Vertical"; // Input axis for primary thumbstick vertical

        public string secondaryThumbstickHorizontal = "RightStickHorizontal"; // Input axis for secondary thumbstick horizontal
        public string secondaryThumbstickVertical = "RightStickVertical"; // Input axis for secondary thumbstick forward/backward

        [Header("Position Settings")]     // Initial positions setup
        private Vector3 LStickStartPosition; // Initial Left Stick positions for resetting
        private Vector3 RStickStartPosition; // Initial Right positions for resetting

        [Header("Rotation Settings")]     // Initial rotations setup

        private Quaternion LStickStartRotation;  // Initial Left Stick rotation for resetting
        private Quaternion RStickStartRotation;  // Initial Right Stick rotation for resetting

        [Header("Rom Check")]     // Check for compatible titles
        private GameSystem gameSystem;  // Cached GameSystem for this cabinet.
        private string insertedGameName = string.Empty;
        private string controlledGameName = string.Empty;
        private string filePath;
        private string configPath;

        void Start()
        {
            gameSystem = GetComponent<GameSystem>();
            CheckInsertedGameName();
            CheckControlledGameName();
            string filePath = $"./Emulators/MAME/outputs/{insertedGameName}.txt";
            string configPath = $"./Emulators/MAME/inputs/{insertedGameName}.ini";
            // Find LStick 
            LStickObject = transform.Find("LStick");
            if (LStickObject != null)
            {
                logger.Debug($"{gameObject.name} LStick object found.");
                // Store initial position and rotation of the Left stick
                LStickStartPosition = LStickObject.localPosition;
                LStickStartRotation = LStickObject.localRotation;
            }
            else
            {
                logger.Debug($"{gameObject.name} LStick object not found.");
            }

            // Find RStick 
            RStickObject = transform.Find("RStick");
            if (RStickObject != null)
            {
                logger.Debug($"{gameObject.name} RStick object found.");
                // Store initial position and rotation of the Right stick
                RStickStartPosition = RStickObject.localPosition;
                RStickStartRotation = RStickObject.localRotation;
            }
            else
            {
                logger.Debug($"{gameObject.name} RStick object not found.");
            }
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
                MapThumbsticks();
            }
        }
        void StartFocusMode()
        {
            string controlledSystemGamePathString = GameSystem.ControlledSystem.Game.path != null ? GameSystem.ControlledSystem.Game.path.ToString() : null;
            logger.Debug($"Controlled System Game path String: {controlledSystemGamePathString}");
            logger.Debug("Compatible Rom Dectected, Activating Projector...");
            logger.Debug("Use the Force Luke!");
            logger.Debug("I have a bad feeling about this!");
            inFocusMode = true;  // Set focus mode flag
        }
        void EndFocusMode()
        {
            logger.Debug("Exiting Focus Mode...");
            inFocusMode = false;  // Clear focus mode flag
        }
        private const float THUMBSTICK_DEADZONE = 0.13f; // Adjust as needed

        private Vector2 ApplyDeadzone(Vector2 input, float deadzone)
        {
            input.x = Mathf.Abs(input.x) < deadzone ? 0f : input.x;
            input.y = Mathf.Abs(input.y) < deadzone ? 0f : input.y;
            return input;
        }

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
            // Map primary thumbstick to LStickObject
            if (LStickObject)
            {
                // Rotation applied on top of starting rotation
                Quaternion primaryRotation = Quaternion.Euler(
                    -primaryThumbstick.y * primaryThumbstickRotationMultiplier,
                    0f,
                    primaryThumbstick.x * primaryThumbstickRotationMultiplier
                );
                LStickObject.localRotation = LStickStartRotation * primaryRotation;
            }

            // Map secondary thumbstick to right stick rotation on X-axis
            if (RStickObject)
            {
                Quaternion secondaryRotation = Quaternion.Euler(
                    -secondaryThumbstick.y * secondaryThumbstickRotationMultiplier,
                    0f,
                    0f
                );
                RStickObject.localRotation = RStickStartRotation * secondaryRotation;
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
    }
}