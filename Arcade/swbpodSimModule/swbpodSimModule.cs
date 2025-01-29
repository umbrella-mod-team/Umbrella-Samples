using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using EmuVR.InputManager;
using System.Collections;

namespace WIGUx.Modules.swbpodSimModule
{
    public class swbpodSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();
        private bool inFocusMode = false;  // Flag to track focus mode state
        private readonly string[] compatibleGames = { "STAR WARS - Battle Pod" };

        [Header("Stick Settings")]
        private Transform swbpodlstickObject; // Object controlled by the left stick rotation (mirrored)
        private Transform swbpodrstickObject; // Object controlled by the right stick forward/backward

        [Header("Input Settings")]
        public string primaryThumbstickHorizontal = "Horizontal"; // Input axis for primary thumbstick horizontal
        public string primaryThumbstickVertical = "Vertical"; // Input axis for primary thumbstick vertical

        public string secondaryThumbstickHorizontal = "RightStickHorizontal"; // Input axis for secondary thumbstick horizontal
        public string secondaryThumbstickVertical = "RightStickVertical"; // Input axis for secondary thumbstick forward/backward

        [Header("Rotation Settings")]
        public float primaryThumbstickRotationMultiplier = 30f; // Multiplier for primary thumbstick rotation intensity
        public float secondaryThumbstickRotationMultiplier = 40f; // Multiplier for secondary thumbstick rotation intensity

        void Start()
        {

            // Find weclemansX object in hierarchy
            swbpodlstickObject = transform.Find("swbpodlstick");
            if (swbpodlstickObject != null)
            {
                logger.Info("swbpodlstick object found.");
            }

            // Find weclemansX object in hierarchy
            swbpodrstickObject = transform.Find("swbpodrstick");
            if (swbpodrstickObject != null)
            {
                logger.Info("swbpodrstick object found.");
            }
        }
        void Update()
        {

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
                MapThumbsticks();
            }
        }
        void StartFocusMode()
        {
            string controlledSystemGamePathString = GameSystem.ControlledSystem.Game.path != null ? GameSystem.ControlledSystem.Game.path.ToString() : null;
            logger.Info($"Controlled System Game path String: {controlledSystemGamePathString}");
            logger.Info("Compatible Rom Dectected, Activating Projector...");
            logger.Info("Use the Force Luke!");
            logger.Info("I have a bad feeling about this!");
            inFocusMode = true;  // Set focus mode flag
        }
        void EndFocusMode()
        {
            logger.Info("Exiting Focus Mode...");
            inFocusMode = false;  // Clear focus mode flag
        }

        private void MapThumbsticks()
        {
            if (!inFocusMode) return;
            if (swbpodlstickObject == null || swbpodrstickObject == null) return;

            Vector2 primaryThumbstick = Vector2.zero;
            Vector2 secondaryThumbstick = Vector2.zero;

            // XInput controller input
            if (XInput.IsConnected)
            {
                primaryThumbstick = XInput.Get(XInput.Axis.LThumbstick);
                secondaryThumbstick = XInput.Get(XInput.Axis.RThumbstick);

                Debug.Log($"Primary Thumbstick (XBox): {primaryThumbstick}");
                Debug.Log($"Secondary Thumbstick (XBox): {secondaryThumbstick}");
            }

            // VR controller input
            if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.Oculus)
            {
                primaryThumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
                secondaryThumbstick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
                Debug.Log($"Primary Thumbstick (VR): {primaryThumbstick}");
                Debug.Log($"Secondary Thumbstick (VR): {secondaryThumbstick}");
            }

            // Map primary thumbstick to left stick rotation
            Quaternion primaryRotation = Quaternion.Euler(-primaryThumbstick.y * primaryThumbstickRotationMultiplier, 0f, primaryThumbstick.x * primaryThumbstickRotationMultiplier);
            swbpodlstickObject.localRotation = primaryRotation;

            // Map secondary thumbstick to right stick rotation on X-axis
            Quaternion secondaryRotation = Quaternion.Euler(-secondaryThumbstick.y * secondaryThumbstickRotationMultiplier, 0f, 0f);
            swbpodrstickObject.localRotation = secondaryRotation;
        }
    }

}