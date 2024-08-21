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
            // Check if any of the specified keys are pressed
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

