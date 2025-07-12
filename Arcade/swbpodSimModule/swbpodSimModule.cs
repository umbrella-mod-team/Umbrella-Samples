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

        [Header("Input Settings")]
        public string primaryThumbstickHorizontal = "Horizontal"; // Input axis for primary thumbstick horizontal
        public string primaryThumbstickVertical = "Vertical"; // Input axis for primary thumbstick vertical

        public string secondaryThumbstickHorizontal = "RightStickHorizontal"; // Input axis for secondary thumbstick horizontal
        public string secondaryThumbstickVertical = "RightStickVertical"; // Input axis for secondary thumbstick forward/backward

        [Header("Rotation Settings")]
        public float primaryThumbstickRotationMultiplier = 20f; // Multiplier for primary thumbstick rotation intensity
        public float secondaryThumbstickRotationMultiplier = 40f; // Multiplier for secondary thumbstick rotation intensity

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
            // Find LStickObject object in hierarchy
            LStickObject = transform.Find("LStick");
            if (LStickObject != null)
            {
                logger.Debug("LStick object found.");
            }

            // Find RStickObject object in hierarchy
            RStickObject = transform.Find("RStick");
            if (RStickObject != null)
            {
                logger.Debug("RStick object found.");
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

        private void MapThumbsticks()
        {
            if (!inFocusMode) return;
            if (LStickObject == null || RStickObject == null) return;

            Vector2 primaryThumbstick = Vector2.zero;
            Vector2 secondaryThumbstick = Vector2.zero;

            // XInput controller input
            if (XInput.IsConnected)
            {
                primaryThumbstick = XInput.Get(XInput.Axis.LThumbstick);
                secondaryThumbstick = XInput.Get(XInput.Axis.RThumbstick);

                logger.Debug($"Primary Thumbstick (XBox): {primaryThumbstick}");
                logger.Debug($"Secondary Thumbstick (XBox): {secondaryThumbstick}");
            }

            // VR controller input
            if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.Oculus)
            {
                primaryThumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
                secondaryThumbstick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
                logger.Debug($"Primary Thumbstick (VR): {primaryThumbstick}");
                logger.Debug($"Secondary Thumbstick (VR): {secondaryThumbstick}");
            }
            // Map primary thumbstick to LStickObject
            if (LStickObject)
            {
                Quaternion primaryRotation = Quaternion.Euler(-primaryThumbstick.y * primaryThumbstickRotationMultiplier, 0f, primaryThumbstick.x * primaryThumbstickRotationMultiplier);
                LStickObject.localRotation = primaryRotation;
            }

            // Map secondary thumbstick to right stick rotation on X-axis
            if (RStickObject)
            {  
                Quaternion secondaryRotation = Quaternion.Euler(-secondaryThumbstick.y * secondaryThumbstickRotationMultiplier, 0f, 0f);
                RStickObject.localRotation = secondaryRotation;
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