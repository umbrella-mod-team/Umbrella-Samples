using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using WIGU;
using WIGUx.Modules.MameHookModule;

namespace WIGUx.Modules.ddrSim
{
    public class ddrSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        [Header("Lights and Emissives")]     // Setup Emissive and Lights
        private Transform foot_1p_leftObject;
        private Transform foot_1p_rightObject;
        private Transform foot_1p_upObject;
        private Transform foot_1p_downObject;
        private Transform foot_2p_leftObject;
        private Transform foot_2p_rightObject;
        private Transform foot_2p_upObject;
        private Transform foot_2p_downObject;
        private Transform body_left_highObject;
        private Transform body_left_lowObject;
        private Transform body_right_highObject;
        private Transform body_right_lowObject;
        private Transform lamp0Object;
        private Transform lamp1Object;
        private Transform speakerObject;
        private Light foot_1p_left_light;
        private Light foot_1p_right_light;
        private Light foot_1p_up_light;
        private Light foot_1p_down_light;
        private Light foot_2p_left_light;
        private Light foot_2p_right_light;
        private Light foot_2p_up_light;
        private Light foot_2p_down_light;
        private Light body_left_high_light;
        private Light body_left_low_light;
        private Light body_right_high_light;
        private Light body_right_low_light;
        private Light lamp0_light;
        private Light lamp1_light;
        private Light speaker1_light;
        private Light speaker2_light;
        private Light[] lights;        //array of lights
        private bool inFocusMode = false;  // Flag to track focus mode state
        private GameSystemState systemState; //systemstate
        Dictionary<string, int> lastLampStates = new Dictionary<string, int>
             {
                      { "foot 1p left", 0 }, { "foot 1p right", 0 }, { "foot 1p up", 0 }, { "foot 1p down", 0 },
        { "foot 2p left", 0 }, { "foot 2p right", 0 }, { "foot 2p up", 0 }, { "foot 2p down", 0 },
        { "body left high", 0 }, { "body left low", 0 }, { "body right high", 0 }, { "body right low", 0 },
        { "lamp0", 0 }, { "lamp1", 0 }, { "speaker", 0 }
             };

        [Header("Rom Check")]
        private GameSystem gameSystem;  // Cached GameSystem for this cabinet.
        private string insertedGameName = string.Empty;
        private string controlledGameName = string.Empty;
        private string configPath;

        void Start()
        {
            gameSystem = GetComponent<GameSystem>();
            CheckInsertedGameName();
            CheckControlledGameName();
            configPath = $"./Emulators/MAME/inputs/{insertedGameName}.ini";
            InitializeObjects();
            InitializeLights();
            DisableAllEmissives();
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
        }

        void ReadData()
        {
            // 1) original “zeroed” lamp list:
            var currentLampStates = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "foot 1p left",  0 }, { "foot 1p right", 0 },
                { "foot 1p up",    0 }, { "foot 1p down",  0 },
                { "foot 2p left",  0 }, { "foot 2p right", 0 },
                { "foot 2p up",    0 }, { "foot 2p down",  0 },
                { "body left high",0 }, { "body left low", 0 },
                { "body right high",0}, { "body right low",0 },
                { "lamp0",         0 }, { "lamp1",        0 },
                { "speaker",       0 }
            };
            // 2) Reflectively fetch the lamp list (falling back if needed)
            IEnumerable<string> lampList = null;
            var hookType = Type.GetType(
                "WIGUx.Modules.MameHookModule.MameHookController, WIGUx.Modules.MameHookModule"
            );
            if (hookType != null)
            {
                var lampProp = hookType.GetProperty(
                    "currentLampState",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
                );
                lampList = lampProp?.GetValue(null) as IEnumerable<string>;
            }
            if (lampList == null)
                lampList = MameHookController.currentLampState;

            // 3) Parse into your state dictionary
            if (lampList != null)
            {
                foreach (var entry in lampList)
                {
                    var parts = entry.Split('|');
                    if (parts.Length != 2) continue;

                    string lamp = parts[0].Trim();
                    if (currentLampStates.ContainsKey(lamp)
                        && int.TryParse(parts[1].Trim(), out int value))
                    {
                        currentLampStates[lamp] = value;
                    }
                }
            }

            // 4) Dispatch only those lamps to the existing logic
            foreach (var kv in currentLampStates)
            {
                // matches: void ProcessLampState(string lampKey, Dictionary<string,int> currentStates)
                ProcessLampState(kv.Key, currentLampStates);
            }
        }
      
        // 🔹 Helper function for safe lamp processing
        void ProcessLampState(string lampKey, Dictionary<string, int> currentStates)
        {
            if (!lastLampStates.ContainsKey(lampKey))
            {
                lastLampStates[lampKey] = 0;
                logger.Debug($"Added missing key '{lampKey}' to lastLampStates.");
            }

            if (currentStates.TryGetValue(lampKey, out int newValue))
            {
                if (lastLampStates[lampKey] != newValue)
                {
                    lastLampStates[lampKey] = newValue;

                    // Call the corresponding function dynamically
                    switch (lampKey)
                    {
                        case "foot 1p left": Processfoot_1p_left(newValue); break;
                        case "foot 1p right": Processfoot_1p_right(newValue); break;
                        case "foot 1p up": Processfoot_1p_up(newValue); break;
                        case "foot 1p down": Processfoot_1p_down(newValue); break;

                        case "foot 2p left": Processfoot_2p_left(newValue); break;
                        case "foot 2p right": Processfoot_2p_right(newValue); break;
                        case "foot 2p up": Processfoot_2p_up(newValue); break;
                        case "foot 2p down": Processfoot_2p_down(newValue); break;

                        case "body left high": Processbody_left_high(newValue); break;
                        case "body left low": Processbody_left_low(newValue); break;
                        case "body right high": Processbody_right_high(newValue); break;
                        case "body right low": Processbody_right_low(newValue); break;

                        case "lamp0": ProcessLamp0(newValue); break;
                        case "lamp1": ProcessLamp1(newValue); break;
                        case "speaker": Processspeaker(newValue); break;

                        default:
                            // logger.Debug($"No processing function for '{lampKey}'");
                            break;
                    }
                }
            }
            else
            {
                logger.Debug($"Lamp key '{lampKey}' not found in current states.");
            }
        }
        void StartFocusMode()
        {
            logger.Debug($"{gameObject.name} Starting Focus Mode...");
           // StopCurrentPatterns();
            logger.Debug("Warming up lights..");
            inFocusMode = true;  // Set focus mode flag
        }

        void EndFocusMode()
        {
            logger.Debug("Exiting Focus Mode...");
            ResetPositions();
            inFocusMode = false;  // Clear focus mode flag
        }

        void ResetPositions()      // Reset objects and cockpit cam to initial position and rotation
        {
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
   
        void Processfoot_1p_left(int state)
        {
            logger.Debug($"foot_1p_left updated: {state}");

            // Update lights
            if (foot_1p_left_light) ToggleLight(foot_1p_left_light, state == 1);
            // Update emissive material
            if (foot_1p_leftObject) ToggleEmissive(foot_1p_leftObject.gameObject, state == 1);
        }
        void Processfoot_1p_right(int state)
        {
            logger.Debug($"foot_1p_right updated: {state}");

            // Update lights
            if (foot_1p_right_light) ToggleLight(foot_1p_right_light, state == 1);
            // Update emissive material
            if (foot_1p_rightObject) ToggleEmissive(foot_1p_rightObject.gameObject, state == 1);
        }
        void Processfoot_1p_down(int state)
        {
            logger.Debug($"foot_1p_down updated: {state}");

            // Update lights
            if (foot_1p_down_light) ToggleLight(foot_1p_down_light, state == 1);
            // Update emissive material
            if (foot_1p_downObject) ToggleEmissive(foot_1p_downObject.gameObject, state == 1);
        }
        void Processfoot_1p_up(int state)
        {
            logger.Debug($"foot_1p_up updated: {state}");

            // Update lights
            if (foot_1p_up_light) ToggleLight(foot_1p_up_light, state == 1);
            // Update emissive material
            if (foot_1p_upObject) ToggleEmissive(foot_1p_upObject.gameObject, state == 1);
        }
        void Processfoot_2p_left(int state)
        {
            logger.Debug($"Processfoot_2p_left updated: {state}");

            // Update lights
            if (foot_2p_left_light) ToggleLight(foot_2p_left_light, state == 1);
            // Update emissive material
            if (foot_2p_leftObject) ToggleEmissive(foot_2p_leftObject.gameObject, state == 1);
        }
        void Processfoot_2p_right(int state)
        {
            logger.Debug($"foot_2p_right updated: {state}");

            // Update lights
            if (foot_2p_right_light) ToggleLight(foot_2p_right_light, state == 1);
            // Update emissive material
            if (foot_2p_rightObject) ToggleEmissive(foot_2p_rightObject.gameObject, state == 1);
        }
        void Processfoot_2p_up(int state)
        {
            logger.Debug($"foot_2p_up updated: {state}");

            // Update lights
            if (foot_2p_up_light) ToggleLight(foot_2p_up_light, state == 1);
            // Update emissive material
            if (foot_2p_upObject) ToggleEmissive(foot_2p_upObject.gameObject, state == 1);
        }
        void Processfoot_2p_down(int state)
        {
            logger.Debug($"foot_2p_down updated: {state}");

            // Update lights
            if (foot_2p_down_light) ToggleLight(foot_2p_down_light, state == 1);
            // Update emissive material
            if (foot_2p_downObject) ToggleEmissive(foot_2p_downObject.gameObject, state == 1);
        }
        void Processbody_left_high(int state)
        {
            logger.Debug($"body_left_high updated: {state}");

            // Update lights
            if (body_left_high_light) ToggleLight(body_left_high_light, state == 1);
            // Update emissive material
            if (body_left_highObject) ToggleEmissive(body_left_highObject.gameObject, state == 1);
        }
        void Processbody_left_low(int state)
        {
            logger.Debug($"body_left_low updated: {state}");

            // Update lights
            if (body_left_low_light) ToggleLight(body_left_low_light, state == 1);
            // Update emissive material
            if (body_left_lowObject) ToggleEmissive(body_left_lowObject.gameObject, state == 1);
        }
        void Processbody_right_high(int state)
        {
            logger.Debug($"body_right_high updated: {state}");

            // Update lights
            if (body_right_high_light) ToggleLight(body_right_high_light, state == 1);
            // Update emissive material
            if (body_right_highObject) ToggleEmissive(body_right_highObject.gameObject, state == 1);
        }
        void Processbody_right_low(int state)
        {
            logger.Debug($"body_right_low updated: {state}");

            // Update lights
            if (body_right_low_light) ToggleLight(body_right_low_light, state == 1);
            // Update emissive material
            if (body_right_lowObject) ToggleEmissive(body_right_lowObject.gameObject, state == 1);
        }
        void Processspeaker(int state)
        {
            logger.Debug($"speaker updated: {state}");

            // Update lights
            if (speaker1_light) ToggleLight(speaker1_light, state == 1);
            if (speaker2_light) ToggleLight(speaker2_light, state == 1);
            // Update emissive material
            if (speakerObject) ToggleEmissive(speakerObject.gameObject, state == 1);
        }
        // Individual function for lamp0
        void ProcessLamp0(int state)
        {
            logger.Debug($"Lamp 0 updated: {state}");

            // Update lights

            if (lamp0_light) ToggleLight(lamp0_light, state == 1);
            // Update emissive material
            if (lamp0Object) ToggleEmissive(lamp0Object.gameObject, state == 1);
        }
        // Individual function for lamp1
        void ProcessLamp1(int state)
        {
            logger.Debug($"Lamp 1 updated: {state}");

            // Update lights
            if (lamp1_light) ToggleLight(lamp1_light, state == 1);
            // Update emissive material
            if (lamp1Object) ToggleEmissive(lamp1Object.gameObject, state == 1);
        }

        void InitializeObjects()
        {
            foot_1p_leftObject = FindObject("emissive/foot_1p_left"); 
            foot_1p_rightObject = FindObject("emissive/foot_1p_right");
            foot_1p_upObject = FindObject("emissive/foot_1p_up");
            foot_1p_downObject = FindObject("emissive/foot_1p_down");
            foot_2p_leftObject = FindObject("emissive/foot_2p_left");
            foot_2p_rightObject = FindObject("emissive/foot_2p_right");
            foot_2p_upObject = FindObject("emissive/foot_2p_up");
            foot_2p_downObject = FindObject("emissive/foot_2p_down");
            body_left_highObject = FindObject("emissive/body_left_high");
            body_left_lowObject = FindObject("emissive/body_left_low");
            body_right_highObject = FindObject("emissive/body_right_high");
            body_right_lowObject = FindObject("emissive/body_right_low");
            lamp0Object = FindObject("emissive/lamp0");
            lamp1Object = FindObject("emissive/lamp1");
            speakerObject = FindObject("emissive/speaker");
        }
        // Log the names of the objects containing the Light components and filter out unwanted lights
        void InitializeLights()
        {
            // Gets all Light components in the target object and its children
            Light[] lights = transform.GetComponentsInChildren<Light>(true);

            foreach (Light light in lights)
            {
                switch (light.gameObject.name)
                {
                    case "lamp0_light":
                        lamp0_light = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    case "lamp1_light":
                        lamp1_light = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    case "foot_1p_left_light":
                        foot_1p_left_light = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    case "foot_1p_right_light":
                        foot_1p_right_light = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    case "foot_1p_up_light":
                        foot_1p_up_light = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    case "foot_1p_down_light":
                        foot_1p_down_light = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    case "foot_2p_left_light":
                        foot_2p_left_light = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    case "foot_2p_right_light":
                        foot_2p_right_light = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    case "foot_2p_up_light":
                        foot_2p_up_light = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    case "foot_2p_down_light":
                        foot_2p_down_light = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    case "body_left_high_light":
                        body_left_high_light = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    case "body_left_low_light":
                        body_left_low_light = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    case "body_right_high_light":
                        body_right_high_light = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    case "body_right_low_light":
                        body_right_low_light = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    case "speaker1_light":
                        speaker1_light = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    case "speaker2_light":
                        speaker2_light = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    default:
                        logger.Debug($"{gameObject.name} Excluded Light found in object: " + light.gameObject.name);
                        break;
                }
            }
        }

        Renderer FindEmissiveRenderer(Transform target)
        {
            if (target == null)
            {
                logger.Debug("Transform is null!");
                return null;
            }

            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                ToggleEmissiveRenderer(renderer, false);
                logger.Debug($"{target.name} Renderer found and assigned.");
            }
            else
            {
                logger.Error($"{target.name} Renderer not found!");
            }
            return renderer;
        }

        Renderer FindEmissiveRenderer(string path)
        {
            Transform target = transform.Find(path);
            return FindEmissiveRenderer(target);
        }
        Light FindLight(string path)
        {
            Light light = transform.Find(path)?.GetComponent<Light>();
            if (light != null)
            {
                logger.Debug($"{path} Light found and assigned.");
            }
            else
            {
                logger.Error($"{path} Light not found!");
            }
            return light;
        }
        Transform FindObject(string path)
        {
            Transform obj = transform.Find(path);
            if (obj != null)
            {
                logger.Debug($"{path} Object found and assigned.");
            }
            else
            {
                logger.Error($"{path} Object not found!");
            }
            return obj;
        }
        void DisableAllEmissives()
        {
            logger.Debug("Disabling all emissives at startup...");

            // List of all emissive GameObjects
            Transform[] emissiveObjects = new Transform[]
            {
        foot_1p_leftObject, foot_1p_rightObject, foot_1p_upObject, foot_1p_downObject, foot_2p_leftObject, foot_2p_rightObject, foot_2p_upObject, foot_2p_downObject, body_left_highObject,
        body_left_lowObject, body_right_highObject, body_right_highObject, body_right_lowObject, lamp0Object, lamp1Object, speakerObject
            };

            // Loop through each emissive object and disable its emission
            foreach (var emissiveObject in emissiveObjects)
            {
                if (emissiveObject != null)
                {
                    Renderer renderer = emissiveObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.DisableKeyword("_EMISSION");
                        logger.Debug($"{emissiveObject.name} emission disabled.");
                    }
                }
                else
                {
                    logger.Debug($"Emissive object {emissiveObject?.name} not found.");
                }
            }
        }
        // Method to enable or disable emissive based on the Transform reference
        void ToggleEmissiveRenderer(Renderer renderer, bool isOn)
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

        void ToggleEmissive(GameObject targetObject, bool isActive)
        {
            if (targetObject != null)
            {
                Renderer renderer = targetObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material material = renderer.material;

                    if (isActive)
                    {
                        material.EnableKeyword("_EMISSION");
                    }
                    else
                    {
                        material.DisableKeyword("_EMISSION");
                    }

                    // logger.Debug($"{targetObject.name} emissive state set to {(isActive ? "ON" : "OFF")}.");
                }
                else
                {
                    logger.Debug($"{gameObject.name} Renderer component not found on {targetObject.name}.");
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} {targetObject.name} emissive object is not assigned.");
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

    }
}