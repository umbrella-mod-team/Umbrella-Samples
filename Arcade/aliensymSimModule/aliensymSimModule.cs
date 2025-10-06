using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR;
using WIGU;
using WIGUx.Modules.MameHookModule;


namespace WIGUx.Modules.aliensymSim
{
    public class aliensymSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        [Header("Lights and Emissives")]     // Setup Emissive and Lights
        private Transform lightsObject;
        public Light[] AlienSymLights = new Light[4]; // Array to store lights
        public Light aliensym1_light;
        public Light aliensym2_light;
        public Light aliensym3_light;
        public Light aliensym4_light;
        private float flashDuration = 0.15f;
        private float flashInterval = 0.15f;
        private float lightDuration = 0.5f; // Duration during which the lights will be on

        private Coroutine strobeCoroutine; // Coroutine variable to control the strobe flashing
        private Light[] lights;

        [Header("Timers and States")]  // Store last states and timers
        private bool areStrobesOn = false; // track strobe lights
        private bool inFocusMode = false;  // Flag to track focus mode state
        private GameSystemState systemState;

        [Header("Rom Check")]
        private GameSystem gameSystem;  // Cached GameSystem for this cabinet.
        private string insertedGameName = string.Empty;
        private string controlledGameName = string.Empty;
        private string configPath;
        private Dictionary<GameObject, Transform> originalParents = new Dictionary<GameObject, Transform>();  // Dictionary to store original parents of objects

        void Start()
        {
            CheckInsertedGameName();
            CheckControlledGameName();
            if (WIGUx.Modules.MameHookModule.MameHookController.ActiveRomsList != null)
            {
                foreach (var rom in WIGUx.Modules.MameHookModule.MameHookController.ActiveRomsList)
                {
                    if (rom == insertedGameName)
                        ReadData();
                }
            }
            configPath = $"./Emulators/MAME/inputs/{insertedGameName}.ini";
            gameSystem = GetComponent<GameSystem>();
            InitializeLights();
            InitializeObjects();
        }
        void Update()
        {
            CheckInsertedGameName();
            CheckControlledGameName();
            if (WIGUx.Modules.MameHookModule.MameHookController.ActiveRomsList != null)
            {
                foreach (var rom in WIGUx.Modules.MameHookModule.MameHookController.ActiveRomsList)
                {
                    if (rom == insertedGameName)
                        ReadData();
                }
            }
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
                //  MapThumbsticks(ref inputDetected);
                //  MapButtons(ref inputDetected);
                //  HandleTransformAdjustment();
            }
        }
        void ReadData()
        {
            // 1) Your original “zeroed” lamp list:
        }
        void StartFocusMode()
        {
            StartStrobes();
            logger.Debug($"{gameObject.name} Starting Focus Mode...");
            //StopCurrentPatterns();
            logger.Debug("Alien Syndrome Module starting...");
            logger.Debug("Watch Out!!..");
            inFocusMode = true;  // Set focus mode flag
        }

        void EndFocusMode()
        {
            logger.Debug("Exiting Focus Mode...");
            StopStrobes();
            inFocusMode = false;  // Clear focus mode flag
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

        void StartStrobes()
        {
            if (!areStrobesOn)
            {
                logger.Debug("Starting strobes");
                strobeCoroutine = StartCoroutine(FlashLights());
                areStrobesOn = true;
            }
        }

        void StopStrobes()
        {
            if (areStrobesOn)
            {
                logger.Debug("Stopping lights");
                StopCoroutine(strobeCoroutine);
                areStrobesOn = false;
            }
        }

        IEnumerator FlashLights()
        {
            while (true)
            {
                // Choose a random light to flash
                int randomIndex = UnityEngine.Random.Range(0, AlienSymLights.Length);
                Light light = AlienSymLights[randomIndex];

                // Check if the light is not null
                if (light != null)
                {
                    // Log the chosen light
                    // logger.Debug($"Flashing {light.name}");

                    // Turn on the chosen light
                    if (light) ToggleLight(light, true);

                    // Wait for a random flash duration
                    float randomFlashDuration = UnityEngine.Random.Range(flashDuration * 0.01f, flashDuration * 0.5f);
                    yield return new WaitForSeconds(randomFlashDuration);

                    // Turn off the chosen light
                    if (light) ToggleLight(light, false);

                    // Wait for a random interval before the next flash
                    float randomFlashInterval = UnityEngine.Random.Range(flashInterval * 0.3f, flashInterval * 0.01f);
                    yield return new WaitForSeconds(randomFlashInterval - randomFlashDuration);
                }
                else
                {
                    logger.Debug("Light is null.");
                }
            }
        }

        void ToggleLight(Light targetLight, bool isActive)
        {
            if (targetLight == null) return;

            // Ensure the GameObject itself is active
            if (targetLight.gameObject.activeSelf != isActive)
                targetLight.gameObject.SetActive(isActive);

            // Then toggle the component
            targetLight.enabled = isActive;
        }

        void ToggleLight1(bool isActive)
        {
            if (aliensym1_light) ToggleLight(aliensym1_light, isActive);
        }

        void ToggleLight2(bool isActive)
        {
            if (aliensym2_light) ToggleLight(aliensym2_light, isActive);
        }

        void ToggleLight3(bool isActive)
        {
            if (aliensym3_light) ToggleLight(aliensym3_light, isActive);
        }

        void ToggleLight4(bool isActive)
        {
            if (aliensym4_light) ToggleLight(aliensym4_light, isActive);
        }

        void InitializeObjects()
        {
            lightsObject = transform.Find("lights");
            if (lightsObject != null)
            {
                logger.Debug("lightsObject found.");
            }
            else
            {
                logger.Error("lightsObject object not found!");
                return; // Early exit if lightsObject is not found
            }
        }
        void InitializeLights()
        {

            // Gets all Light components in the target object and its children
            Light[] lights = transform.GetComponentsInChildren<Light>(true);

            // Log the names of the objects containing the Light components and filter out unwanted lights
            foreach (Light light in lights)
            {
                logger.Debug($"Light found: {light.gameObject.name}");
                switch (light.gameObject.name)
                {
                    case "aliensym1_light":
                        aliensym1_light = light;
                        AlienSymLights[0] = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    case "aliensym2_light":
                        aliensym2_light = light;
                        AlienSymLights[1] = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    case "aliensym3_light":
                        aliensym3_light = light;
                        AlienSymLights[2] = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    case "aliensym4_light":
                        aliensym4_light = light;
                        AlienSymLights[3] = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    default:
                        logger.Debug($"{gameObject.name} Excluded Light found in object: " + light.gameObject.name);
                        break;
                }
            }

            // Log the assigned lights for verification
            for (int i = 0; i < AlienSymLights.Length; i++)
            {
                if (AlienSymLights[i] != null)
                {
                    logger.Debug($"AlienSymLights[{i}] assigned to: {AlienSymLights[i].name}");
                }
                else
                {
                    logger.Error($"AlienSymLights[{i}] is not assigned!");
                }
            }
        }
    }
}
