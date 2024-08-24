using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using EmuVR.InputManager;
using System.Collections;
using static XInput;

namespace WIGUx.Modules.beatmaniaLightSim
{
    public class beatmaniaLightController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        public Renderer beatEmissiveRenderer;
        public Light beat1Light;
        public Light beat2Light;
        public Light beat3Light;
        public Light beat4Light;
        private float BeatIntensity = 1.5f;
        private float BeatIntensityLimit = 25.0f;
        private float BeatDuration = 0.25f; // Adjust this value for how long the intensity lasts after a key press
        private float CurrentBeatIntensity = 0f;
        private Coroutine beatFlashCoroutine;
        private bool inFocusMode = false;  // Flag to track focus mode state
        private readonly string[] compatibleGames = { "aliensym" };

        void Start()
        {
            // Initialize and ensure the emissive is turned off and lights are at 0 intensity at the start
            beatEmissiveRenderer = transform.Find("emissive/beat")?.GetComponent<Renderer>();
            if (beatEmissiveRenderer != null)
            {
                ToggleBeatEmissive(false);
            }
            else
            {
               logger.Info("Beat emissive Renderer not found!");
            }

            // Find and assign beat lights, with logging
            beat1Light = transform.Find("emissive/beat1")?.GetComponent<Light>();
            if (beat1Light != null)
            {
                logger.Info("beat1Light found and assigned.");
            }
            else
            {
                logger.Error("beat1Light not found under emissive/beat1.");
            }

            beat2Light = transform.Find("emissive/beat2")?.GetComponent<Light>();
            if (beat2Light != null)
            {
                logger.Info("beat2Light found and assigned.");
            }
            else
            {
                logger.Error("beat2Light not found under emissive/beat2.");
            }

            beat3Light = transform.Find("emissive/beat3")?.GetComponent<Light>();
            if (beat3Light != null)
            {
                logger.Info("beat3Light found and assigned.");
            }
            else
            {
                logger.Error("beat3Light not found under emissive/beat3.");
            }

            beat4Light = transform.Find("emissive/beat4")?.GetComponent<Light>();
            if (beat4Light != null)
            {
                logger.Info("beat4Light found and assigned.");
            }
            else
            {
                logger.Error("beat4Light not found under emissive/beat4.");
            }

            SetLightIntensity(0);
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
            logger.Info("Compatible Rom Dectected, Feel the Beat!...");
            logger.Info("Beatmania Module starting Sim starting...");

            // Reset controllers to initial positions and rotations

            inFocusMode = true;  // Set focus mode flag
        }

        void EndFocusMode()
        {
            inFocusMode = false;  // Clear focus mode flag
        }


        //sexy new combined input handler
        void HandleInput(ref bool inputDetected)
        {
            if (!inFocusMode) return;

            Vector2 primaryThumbstick = Vector2.zero;
            Vector2 secondaryThumbstick = Vector2.zero;

            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.F) ||
                Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.C) ||
                Input.GetKeyDown(KeyCode.V))
            {
                if (beatFlashCoroutine != null)
                {
                    StopCoroutine(beatFlashCoroutine);
                }

                CurrentBeatIntensity += BeatIntensity;
                CurrentBeatIntensity = Mathf.Min(CurrentBeatIntensity, BeatIntensityLimit); // Clamp to BeatIntensityLimit

                // If you want to limit how much the duration can increase
                if (BeatDuration < 0.25f)  // Example limit for BeatDuration
                {
                    BeatDuration += 0.25f;  // Increase duration with each key press
                }

                beatFlashCoroutine = StartCoroutine(ManageBeatEffect());
            }

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
                    inputDetected = true;
                }
                if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger))
                {
                    inputDetected = true;
                }
                /*
                // Check if the secondary index trigger on the left controller is pressed
                if (OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger))
                {
                    logger.Info("OVR Secondary index trigger pressed");
                }

                // Check if the primary hand trigger on the right controller is pressed
                if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger))
                {
                    logger.Info("OVR Primary hand trigger pressed");
                }

                // Check if the secondary hand trigger on the left controller is pressed
                if (OVRInput.Get(OVRInput.Button.SecondaryHandTrigger))
                {
                    logger.Info("OVR Secondary hand trigger pressed");
                }

                // Check if the primary thumbstick is pressed
                if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick))
                {
                    logger.Info("OVR Primary thumbstick pressed");
                }

                // Check if the secondary thumbstick is pressed
                if (OVRInput.GetDown(OVRInput.Button.SecondaryThumbstick))
                {
                    logger.Info("OVR Secondary thumbstick pressed");
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
                    inputDetected = true;
                }

                // Reset position on button release
                if (XInput.GetUp(XInput.Button.LIndexTrigger))
                {
                    inputDetected = true;
                }
            }
        }
        IEnumerator ManageBeatEffect()
        {
            while (CurrentBeatIntensity > 0)
            {
                SetLightIntensity(CurrentBeatIntensity);
                ToggleBeatEmissive(CurrentBeatIntensity > 0);

                // Decrease intensity over time
                CurrentBeatIntensity -= (BeatIntensity / BeatDuration) * Time.deltaTime;

                // Optionally, adjust the rate of decrease for `BeatDuration`
                BeatDuration = Mathf.Max(BeatDuration - (Time.deltaTime / 2), 0.25f);  // Slower reduction over time, with a minimum value

                yield return null;
            }

            // Ensure everything is off when intensity reaches 0
            SetLightIntensity(0);
            ToggleBeatEmissive(false);
            beatFlashCoroutine = null;
        }

        void SetLightIntensity(float intensity)
        {
            if (beat1Light != null) beat1Light.intensity = intensity;
            if (beat2Light != null) beat2Light.intensity = intensity;
            if (beat3Light != null) beat3Light.intensity = intensity;
            if (beat4Light != null) beat4Light.intensity = intensity;
        }

        void ToggleBeatEmissive(bool isOn)
        {
            if (isOn)
            {
                beatEmissiveRenderer.material.EnableKeyword("_EMISSION");
                beatEmissiveRenderer.material.SetFloat("_EmissionIntensity", CurrentBeatIntensity / BeatIntensityLimit);
            }
            else
            {
                beatEmissiveRenderer.material.DisableKeyword("_EMISSION");
            }
        }
    }
}

