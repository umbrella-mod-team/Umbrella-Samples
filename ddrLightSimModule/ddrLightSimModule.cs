using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using EmuVR.InputManager;
using System.Collections;
using static XInput;
using static SteamVR_Utils;
using System.IO;

namespace WIGUx.Modules.ddrLightSim
{
    public class ddrLightController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        public Renderer ddr1EmissiveRenderer;
        public Renderer ddr2EmissiveRenderer;
        public Renderer ddr3EmissiveRenderer;
        public Renderer ddr4EmissiveRenderer;
        public Renderer ddr5EmissiveRenderer;
        public Renderer ddr6EmissiveRenderer;
        public Renderer ddr7EmissiveRenderer;
        public Renderer ddr8EmissiveRenderer;
        public Light ddr1Light;
        public Light ddr2Light;
        public Light ddr3Light;
        public Light ddr4Light;

        private float ddrIntensity = 10.0f;
        private float ddrIntensityLimit = 25.0f;
        private float ddrDuration = 0.25f;
        private Coroutine ddrFlashCoroutine;

        private float ddrattractFlashDuration = 0.35f;
        private float ddrattractFlashDelay = 0.1f;
        private float ddrlightOverlapDelay = 0.1f;

        private readonly string[] compatibleGames = { "ddr" };
        private bool inFocusMode = false;

        void Start()
        {
            InitializeEmissives();
            InitializeLights();
            SetLightIntensity(0);
            StartAttractMode();
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
                StartAttractMode();
            }

            if (inFocusMode)
            {
                HandleXInput();
            }
        }

        void InitializeEmissives()
        {
            ddr1EmissiveRenderer = FindEmissiveRenderer("emissive/ddr1e");
            ddr2EmissiveRenderer = FindEmissiveRenderer("emissive/ddr2e");
            ddr3EmissiveRenderer = FindEmissiveRenderer("emissive/ddr3e");
            ddr4EmissiveRenderer = FindEmissiveRenderer("emissive/ddr4e");
            ddr5EmissiveRenderer = FindEmissiveRenderer("emissive/ddr5e");
            ddr6EmissiveRenderer = FindEmissiveRenderer("emissive/ddr6e");
            ddr7EmissiveRenderer = FindEmissiveRenderer("emissive/ddr7e");
            ddr8EmissiveRenderer = FindEmissiveRenderer("emissive/ddr8e");
        }

        Renderer FindEmissiveRenderer(string path)
        {
            Renderer renderer = transform.Find(path)?.GetComponent<Renderer>();
            if (renderer != null)
            {
                ToggleEmissive(renderer, false);
                logger.Info($"{path} Renderer found and assigned.");
            }
            else
            {
                logger.Error($"{path} Renderer not found!");
            }
            return renderer;
        }

        void InitializeLights()
        {
            ddr1Light = FindLight("emissive/ddr1");
            ddr2Light = FindLight("emissive/ddr2");
            ddr3Light = FindLight("emissive/ddr3");
            ddr4Light = FindLight("emissive/ddr4");
        }

        Light FindLight(string path)
        {
            Light light = transform.Find(path)?.GetComponent<Light>();
            if (light != null)
            {
                logger.Info($"{path} Light found and assigned.");
            }
            else
            {
                logger.Error($"{path} Light not found!");
            }
            return light;
        }

        void StartFocusMode()
        {
            logger.Info("Module starting...");

            inFocusMode = true;  // Set focus mode flag

            // Stop attract mode
            if (ddrFlashCoroutine != null)
            {
                StopCoroutine(ddrFlashCoroutine);
                ddrFlashCoroutine = null;
            }
        }

        void EndFocusMode()
        {
            inFocusMode = false;  // Clear focus mode flag
        }

        public void StartAttractMode()
        {
            if (ddrFlashCoroutine != null)
            {
                StopCoroutine(ddrFlashCoroutine);
            }

            ddrFlashCoroutine = StartCoroutine(AttractPattern());
        }

        IEnumerator AttractPattern()
        {
            Renderer[] emissives = new Renderer[] { ddr1EmissiveRenderer, ddr2EmissiveRenderer, ddr3EmissiveRenderer, ddr4EmissiveRenderer, ddr5EmissiveRenderer, ddr6EmissiveRenderer, ddr7EmissiveRenderer, ddr8EmissiveRenderer };
            Light[] lights = new Light[] { ddr1Light, ddr2Light, ddr3Light, ddr4Light };

            while (true)
            {
                for (int step = 0; step < 4; step++)
                {
                    // Turn off all emissives first
                    DisableEmission(emissives);

                    // Light up the appropriate emissive pairs (1&5, 2&6, 3&7, 4&8)
                    ToggleEmissive(emissives[step], true);
                    ToggleEmissive(emissives[step + 4], true);

                    // Light up the corresponding light, with intensity ramping from BeatIntensity to 0
                    StartCoroutine(RampLightIntensity(lights[step]));

                    // Wait before moving to the next step, while allowing overlap
                    yield return new WaitForSeconds(ddrattractFlashDuration - ddrlightOverlapDelay);

                    // Delay at the last light
                    if (step == 3)
                    {
                        yield return new WaitForSeconds(ddrattractFlashDelay);
                    }
                }
            }
        }

        IEnumerator RampLightIntensity(Light light)
        {
            float intensity = ddrIntensity;
            while (intensity > 0)
            {
                if (light != null)
                {
                    light.intensity = intensity;
                }
                intensity -= (ddrIntensity / ddrattractFlashDuration) * Time.deltaTime;
                yield return null;
            }
            if (light != null)
            {
                light.intensity = 0;
            }
        }

        void DisableEmission(Renderer[] emissiveObjects)
        {
            foreach (var renderer in emissiveObjects)
            {
                if (renderer != null)
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
                else
                {
                    logger.Debug("Renderer component not found on one of the emissive objects.");
                }
            }
        }

        void ToggleEmissive(Renderer renderer, bool isOn)
        {
            if (renderer != null)
            {
                if (isOn)
                {
                    renderer.material.EnableKeyword("_EMISSION");
                }
                else
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
            }
        }

        private void HandleXInput()
        {
            if (!XInput.IsConnected) return;

            if (inFocusMode)
            {
                // Handle Y button
                if (XInput.GetDown(XInput.Button.Y))
                {
                    logger.Info("XInput Button Y pressed");
                    ToggleEmissive(ddr3EmissiveRenderer, true);
                    ToggleEmissive(ddr7EmissiveRenderer, true);
                    StartCoroutine(RampLightIntensity(ddr2Light));
                }

                if (XInput.GetUp(XInput.Button.Y))
                {
                    logger.Info("XInput Button Y released");
                    ToggleEmissive(ddr3EmissiveRenderer, false);
                    ToggleEmissive(ddr7EmissiveRenderer, false);
                }

                // Handle A button
                if (XInput.GetDown(XInput.Button.A))
                {
                    logger.Info("XInput Button A pressed");
                    ToggleEmissive(ddr4EmissiveRenderer, true);
                    ToggleEmissive(ddr8EmissiveRenderer, true);
                    StartCoroutine(RampLightIntensity(ddr4Light));
                }

                if (XInput.GetUp(XInput.Button.A))
                {
                    logger.Info("XInput Button A released");
                    ToggleEmissive(ddr4EmissiveRenderer, false);
                    ToggleEmissive(ddr8EmissiveRenderer, false);
                }

                // Handle B button
                if (XInput.GetDown(XInput.Button.B))
                {
                    logger.Info("XInput Button B pressed");
                    ToggleEmissive(ddr2EmissiveRenderer, true);
                    ToggleEmissive(ddr6EmissiveRenderer, true);
                    StartCoroutine(RampLightIntensity(ddr3Light));
                }

                if (XInput.GetUp(XInput.Button.B))
                {
                    logger.Info("XInput Button B released");
                    ToggleEmissive(ddr2EmissiveRenderer, false);
                    ToggleEmissive(ddr6EmissiveRenderer, false);
                }

                // Handle X button
                if (XInput.GetDown(XInput.Button.X))
                {
                    logger.Info("XInput Button X pressed");
                    ToggleEmissive(ddr1EmissiveRenderer, true);
                    ToggleEmissive(ddr5EmissiveRenderer, true);
                    StartCoroutine(RampLightIntensity(ddr1Light));
                }

                if (XInput.GetUp(XInput.Button.X))
                {
                    logger.Info("XInput Button X released");
                    ToggleEmissive(ddr1EmissiveRenderer, false);
                    ToggleEmissive(ddr5EmissiveRenderer, false);
                }
            }
            else
            {
                StartAttractMode();
            }
        }

        void SetLightIntensity(float intensity)
        {
            if (ddr1Light != null) ddr1Light.intensity = intensity;
            if (ddr2Light != null) ddr2Light.intensity = intensity;
            if (ddr3Light != null) ddr3Light.intensity = intensity;
            if (ddr4Light != null) ddr4Light.intensity = intensity;
        }
    }
}
