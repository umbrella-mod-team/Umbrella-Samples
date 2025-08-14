using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using WIGU;
using WIGUx.Modules.MameHookModule;

namespace WIGUx.Modules.beatmaniaSim
{
    public class beatmaniaSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        [Header("Lights and Emissives")]     // Setup Emissive and Lights
        Dictionary<string, int> lastLampStates = new Dictionary<string, int>
    {
        { "p1button_1", 0 }, { "p1button_2", 0 }, { "p1button_3", 0 }, { "p1button_4", 0 }, { "p1button_5", 0 },
        { "p2button_1", 0 }, { "p2button_2", 0 }, { "p2button_3", 0 }, { "p2button_4", 0 }, { "p2button_5", 0 },
        { "left_blue_hlt", 0 }, { "left_red_hlt", 0 }, { "left_ssr", 0 },
        { "right_red_hlt", 0 }, { "right_blue_hlt", 0 }, { "right_ssr", 0 },
        { "p1start", 0 }, { "p2start", 0 }, { "effect", 0 }, { "coin_blocker", 0 }
    };
        private Transform p1button_1Object;
        private Transform p1button_2Object;
        private Transform p1button_3Object;
        private Transform p1button_4Object;
        private Transform p1button_5Object;
        private Transform p2button_1Object;
        private Transform p2button_2Object;
        private Transform p2button_3Object;
        private Transform p2button_4Object;
        private Transform p2button_5Object;
        private Transform left_blue_hltObject;
        private Transform left_red_hltObject;
        private Transform left_ssrObject;
        private Transform right_red_hltObject;
        private Transform right_blue_hltObject;
        private Transform right_ssrObject;
        private Transform p1startObject;
        private Transform p2startObject;
        private Transform effectObject;
        private Transform coin_blockerObject;
        private Light p1button_1_light;
        private Light p1button_2_light;
        private Light p1button_3_light;
        private Light p1button_4_light;
        private Light p1button_5_light;
        private Light p2button_1_light;
        private Light p2button_2_light;
        private Light p2button_3_light;
        private Light p2button_4_light;
        private Light p2button_5_light;
        private Light left_blue_hlt_light;
        private Light left_red_hlt_light;
        private Light left_ssr_light;
        private Light right_red_hlt_light;
        private Light right_blue_hlt_light;
        private Light right_ssr_light;
        private Light p1start_light;
        private Light p2start_light;
        private Light effect1_light;
        private Light effect2_light;
        private Light effect3_light;
        private Light effect4_light;
        private Light effect5_light;
        private Light effect6_light;
        private Light coin_blocker_light;
        private Light[] lights;
        private GameSystemState systemState;

        [Header("Rom Check")]
        private bool inFocusMode = false;  // Flag to track focus mode state
        private GameSystem gameSystem;  // Cached GameSystem for this cabinet.
        private string insertedGameName = string.Empty;
        private string controlledGameName = string.Empty;
        private string configPath;

        void Start()
        {
            CheckInsertedGameName();
            CheckControlledGameName();
            configPath = $"./Emulators/MAME/inputs/{insertedGameName}.ini";
            gameSystem = GetComponent<GameSystem>();
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

            }
        }
       
        void StartFocusMode()
        {
            logger.Debug($"{gameObject.name} Starting Focus Mode...");
            // StopCurrentPatterns();
            logger.Debug("Beatmania Module starting Sim starting...");
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
        void ReadData()
        {
            // Initialize lamp names exactly as in original version
            Dictionary<string, int> currentLampStates = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        { "p1button_1", 0 }, { "p1button_2", 0 }, { "p1button_3", 0 }, { "p1button_4", 0 }, { "p1button_5", 0 },
        { "p2button_1", 0 }, { "p2button_2", 0 }, { "p2button_3", 0 }, { "p2button_4", 0 }, { "p2button_5", 0 },
        { "left_blue_hlt", 0 }, { "left_red_hlt", 0 }, { "left_ssr", 0 },
        { "right_red_hlt", 0 }, { "right_blue_hlt", 0 }, { "right_ssr", 0 },
        { "p1start", 0 }, { "p2start", 0 }, { "effect", 0 }, { "coin_blocker", 0 }
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
            // 4) Dispatch only those lamps to your existing logic
            foreach (var lamp in currentLampStates.Keys)
            {
                ProcessLampState(lamp, currentLampStates);
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
                        case "p1button_1": Processp1button_1(newValue); break;
                        case "p1button_2": Processp1button_2(newValue); break;
                        case "p1button_3": Processp1button_3(newValue); break;
                        case "p1button_4": Processp1button_4(newValue); break;
                        case "p1button_5": Processp1button_5(newValue); break;

                        case "p2button_1": Processp2button_1(newValue); break;
                        case "p2button_2": Processp2button_2(newValue); break;
                        case "p2button_3": Processp2button_3(newValue); break;
                        case "p2button_4": Processp2button_4(newValue); break;
                        case "p2button_5": Processp2button_5(newValue); break;

                        case "left_blue_hlt": Processleft_blue_hlt(newValue); break;
                        case "left_red_hlt": Processleft_red_hlt(newValue); break;
                        case "left_ssr": Processleft_ssr(newValue); break;
                        case "right_red_hlt": Processright_red_hlt(newValue); break;
                        case "right_blue_hlt": Processright_blue_hlt(newValue); break;
                        case "right_ssr": Processright_ssr(newValue); break;

                        case "p1start": Processp1start(newValue); break;
                        case "p2start": Processp2start(newValue); break;
                        case "effect": Processeffect(newValue); break;
                        case "coin_blocker": Processcoin_blocker(newValue); break;

                        default:
                            logger.Debug($"No processing function for '{lampKey}'");
                            break;
                    }
                }
            }
            else
            {
                logger.Debug($"Lamp key '{lampKey}' not found in current states.");
            }
        }
        void Processp1button_1(int state)
        {
            logger.Debug($"p1button_1 updated: {state}");

            // Update lights
            if (p1button_1_light) ToggleLight(p1button_1_light, state == 1);
            // Update emissive material
            if (p1button_1Object) ToggleEmissive(p1button_1Object.gameObject, state == 1);
        }
        void Processp1button_2(int state)
        {
            logger.Debug($"p1button_2 updated: {state}");

            // Update lights
            if (p1button_2_light) ToggleLight(p1button_2_light, state == 1);
            // Update emissive material
            if (p1button_2Object) ToggleEmissive(p1button_2Object.gameObject, state == 1);
        }
        void Processp1button_3(int state)
        {
            logger.Debug($"p1button_3 updated: {state}");

            // Update lights
            if (p1button_3_light) ToggleLight(p1button_3_light, state == 1);
            // Update emissive material
            if (p1button_3Object) ToggleEmissive(p1button_3Object.gameObject, state == 1);
        }
        void Processp1button_4(int state)
        {
            logger.Debug($"p1button_4 updated: {state}");

            // Update lights
            if (p1button_4_light) ToggleLight(p1button_4_light, state == 1);
            // Update emissive material
            if (p1button_4Object) ToggleEmissive(p1button_4Object.gameObject, state == 1);
        }
        void Processp1button_5(int state)
        {
            logger.Debug($"p1button_5 updated: {state}");

            // Update lights
            if (p1button_5_light) ToggleLight(p1button_5_light, state == 1);
            // Update emissive material
            if (p1button_5Object) ToggleEmissive(p1button_5Object.gameObject, state == 1);
        }
        void Processp2button_1(int state)
        {
            logger.Debug($"p2button_1 updated: {state}");

            // Update lights
            if (p2button_1_light) ToggleLight(p2button_1_light, state == 1);
            // Update emissive material
            if (p2button_1Object) ToggleEmissive(p2button_1Object.gameObject, state == 1);
        }
        void Processp2button_2(int state)
        {
            logger.Debug($"p2button_2 updated: {state}");

            // Update lights
            if (p2button_2_light) ToggleLight(p2button_2_light, state == 1);
            // Update emissive material
            if (p2button_2Object) ToggleEmissive(p2button_2Object.gameObject, state == 1);
        }
        void Processp2button_3(int state)
        {
            logger.Debug($"p2button_3 updated: {state}");

            // Update lights
            if (p2button_3_light) ToggleLight(p2button_3_light, state == 1);
            // Update emissive material
            if (p2button_3Object) ToggleEmissive(p2button_3Object.gameObject, state == 1);
        }
        void Processp2button_4(int state)
        {
            logger.Debug($"p2button_4 updated: {state}");

            // Update lights
            if (p2button_4_light) ToggleLight(p2button_4_light, state == 1);
            // Update emissive material
            if (p2button_4Object) ToggleEmissive(p2button_4Object.gameObject, state == 1);
        }
        void Processp2button_5(int state)
        {
            logger.Debug($"p2button_5 updated: {state}");

            // Update lights
            if (p2button_5_light) ToggleLight(p2button_5_light, state == 1);
            // Update emissive material
            if (p2button_5Object) ToggleEmissive(p2button_5Object.gameObject, state == 1);
        }
        void Processleft_blue_hlt(int state)
        {
            logger.Debug($"left_blue_hlt updated: {state}");

            // Update lights
            if (left_blue_hlt_light) ToggleLight(left_blue_hlt_light, state == 1);
            // Update emissive material
            if (left_blue_hltObject) ToggleEmissive(left_blue_hltObject.gameObject, state == 1);
        }
        void Processleft_red_hlt(int state)
        {
            logger.Debug($"left_red_hlt updated: {state}");

            // Update lights
            if (left_red_hlt_light) ToggleLight(left_red_hlt_light, state == 1);
            // Update emissive material
            if (left_red_hltObject) ToggleEmissive(left_red_hltObject.gameObject, state == 1);
        }
        void Processleft_ssr(int state)
        {
            logger.Debug($"left_ssr updated: {state}");

            // Update lights
            if (left_ssr_light) ToggleLight(left_ssr_light, state == 1);
            // Update emissive material
            if (left_ssrObject) ToggleEmissive(left_ssrObject.gameObject, state == 1);
        }
        void Processright_red_hlt(int state)
        {
            logger.Debug($"right_red_hlt updated: {state}");

            // Update lights
            if (right_red_hlt_light) ToggleLight(right_red_hlt_light, state == 1);
            // Update emissive material
            if (right_red_hltObject) ToggleEmissive(right_red_hltObject.gameObject, state == 1);
        }
        void Processright_blue_hlt(int state)
        {
            logger.Debug($"right_blue_hlt updated: {state}");

            // Update lights
            if (right_blue_hlt_light) ToggleLight(right_blue_hlt_light, state == 1);
            // Update emissive material
            if (right_blue_hltObject) ToggleEmissive(right_blue_hltObject.gameObject, state == 1);
        }
        void Processright_ssr(int state)
        {
            logger.Debug($"right_ssr updated: {state}");

            // Update lights
            if (right_ssr_light) ToggleLight(right_ssr_light, state == 1);
            // Update emissive material
            if (right_ssrObject) ToggleEmissive(right_ssrObject.gameObject, state == 1);
        }
        void Processeffect(int state)
        {
            logger.Debug($"effect updated: {state}");

            // Update lights
            if (effect1_light) ToggleLight(effect1_light, state == 1);
            if (effect2_light) ToggleLight(effect2_light, state == 1);
            if (effect3_light) ToggleLight(effect3_light, state == 1);
            if (effect4_light) ToggleLight(effect4_light, state == 1);
            if (effect5_light) ToggleLight(effect5_light, state == 1);
            if (effect6_light) ToggleLight(effect6_light, state == 1);
            // Update emissive material
            if (effectObject) ToggleEmissive(effectObject.gameObject, state == 1);
        }
        // Individual function for p1start
        void Processp1start(int state)
        {
            logger.Debug($"sp1start updated: {state}");

            // Update lights

            if (p1start_light) ToggleLight(p1start_light, state == 1);
            // Update emissive material
            if (p1startObject) ToggleEmissive(p1startObject.gameObject, state == 1);
        }
        // Individual function for p2start
        void Processp2start(int state)
        {
            logger.Debug($"p2start updated: {state}");

            // Update lights
            if (p2start_light) ToggleLight(p2start_light, state == 1);
            // Update emissive material
            if (p2startObject) ToggleEmissive(p2startObject.gameObject, state == 1);
        }
        // Individual function for coin_blocker
        void Processcoin_blocker(int state)
        {
            logger.Debug($"coin_blocker updated: {state}");

            // Update lights
            if (coin_blocker_light) ToggleLight(coin_blocker_light, state == 1);
            // Update emissive material
            if (coin_blockerObject) ToggleEmissive(coin_blockerObject.gameObject, state == 1);
        }
        void InitializeObjects()
            {
            p1button_1Object = FindObject("emissive/p1button_1");
            p1button_2Object = FindObject("emissive/p1button_2");
            p1button_4Object = FindObject("emissive/p1button_4");
            p1button_3Object = FindObject("emissive/p1button_3");
            p1button_5Object = FindObject("emissive/p1button_5");
            p2button_1Object = FindObject("emissive/p2button_1");
            p2button_2Object = FindObject("emissive/p2button_2");
            p2button_3Object = FindObject("emissive/p2button_3");
            p2button_4Object = FindObject("emissive/p2button_4");
            p2button_5Object = FindObject("emissive/p2button_5");
            /*
            left_blue_hltObject = FindObject("emissive/left_blue_hlt");
            left_red_hltObject = FindObject("emissive/left_red_hlt");
            left_ssrObject = FindObject("emissive/left_ssr");
            right_red_hltObject = FindObject("emissive/right_red_hlt");
            right_blue_hltObject = FindObject("emissive/right_blue_hlt");
            right_ssrObject = FindObject("emissive/right_ssr");
            coin_blockerObject = FindObject("emissive/coin_blocker");
            */
            p1startObject = FindObject("emissive/p1start");
            p2startObject = FindObject("emissive/p2start");
            effectObject = FindObject("emissive/effect");

        }
        void InitializeLights()
        {
            p1button_1_light = FindLight("emissive/p1button_1_light");
            p1button_2_light = FindLight("emissive/p1button_2_light");
            p1button_4_light = FindLight("emissive/p1button_4_light");
            p1button_3_light = FindLight("emissive/p1button_3_light");
            p1button_5_light = FindLight("emissive/p1button_5_light");
            p2button_1_light = FindLight("emissive/p2button_1_light");
            p2button_2_light = FindLight("emissive/p2button_2_light");
            p2button_3_light = FindLight("emissive/p2button_3_light");
            p2button_4_light = FindLight("emissive/p2button_4_light");
            p2button_5_light = FindLight("emissive/p2button_5_light");
            left_blue_hlt_light = FindLight("emissive/left_blue_hlt_light");
            left_red_hlt_light = FindLight("emissive/left_red_hlt_light");
            left_ssr_light = FindLight("emissive/left_ssr_light");
            right_red_hlt_light = FindLight("emissive/right_red_hlt_light");
            right_blue_hlt_light = FindLight("emissive/right_blue_hlt_light");
            right_ssr_light = FindLight("emissive/right_ssr_light");
            p1start_light = FindLight("emissive/p1start_light");
            p2start_light = FindLight("emissive/p2start_light");
            effect1_light = FindLight("emissive/effect1_light");
            effect2_light = FindLight("emissive/effect2_light");
            effect3_light = FindLight("emissive/effect3_light");
            effect4_light = FindLight("emissive/effect4_light");
            effect5_light = FindLight("emissive/effect5_light");
            effect6_light = FindLight("emissive/effect6_light");
            // coin_blocker_light = FindLight("emissive/coin_blocker_light");
        }
        Renderer FindEmissiveRenderer(Transform target)
        {
            if (target == null)
            {
                logger.Error("Transform is null!");
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
        p1button_1Object, p1button_2Object, p1button_3Object, p1button_4Object, p1button_5Object, p2button_1Object, p2button_2Object, p2button_3Object, p2button_4Object,
        p2button_5Object, p1startObject, p2startObject, effectObject
            }; // left_blue_hltObject, left_red_hltObject, left_ssrObject, right_red_hltObject, right_blue_hltObject, right_ssrObject,

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







/*


    public Renderer beatEmissiveRenderer;
        public Light beat1Light;
        public Light beat2Light;
        public Light beat3Light;
        public Light beat4Light;
        private float BeatIntensity = 2.5f;
        private float BeatIntensityLimit = 25.0f;
        private float BeatDuration = 0.25f; // Adjust this value for how long the intensity lasts after a key press
        private float CurrentBeatIntensity = 0f;
        private Coroutine beatFlashCoroutine;
        private bool inFocusMode = false;  // Flag to track focus mode state
        private readonly string[] compatibleGames = { "", "bm2ndmix", "bm3", "bm36th", "bm37th", "bm3core", "bm3final", "bm3rdmix", "bm4thmix", "bm5thmix", "bm6thmix", "bm7thmix", "bmaster", "bmboxing", "bmbugs", "bmcbowl", "bmclubmx", "bmcompm2", "bmcompmx", "bmcorerm", "bmcpokr", "bmdct", "bmfinal", "bmiidx", "bmiidx2", "bmiidx3", "bmiidx4", "bmiidx5", "bmiidx6", "bmiidx7", "bmiidx8", "bmiidxc", "bmiidxc2", "bmiidxs", "bmjr", "bml3", "bml3kanji", "bml3mp1802", "bml3mp1805" };
        string romname = GameSystem.ControlledSystem.Game.path != null
? Path.GetFileNameWithoutExtension(GameSystem.ControlledSystem.Game.path.ToString())
: null;
        private beatfilePath => $"./Emulators/MAME Master/outputs/.txt";
        private Dictionary<GameObject, Transform> originalParents = new Dictionary<GameObject, Transform>();  // Dictionary to store original parents of objects

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
               logger.Debug("Beat emissive Renderer not found!");
            }

            // Find and assign beat lights, with logging
            beat1Light = transform.Find("emissive/beat1")?.GetComponent<Light>();
            if (beat1Light != null)
            {
                logger.Debug("beat1Light found and assigned.");
            }
            else
            {
                logger.Error("beat1Light not found under emissive/beat1.");
            }

            beat2Light = transform.Find("emissive/beat2")?.GetComponent<Light>();
            if (beat2Light != null)
            {
                logger.Debug("beat2Light found and assigned.");
            }
            else
            {
                logger.Error("beat2Light not found under emissive/beat2.");
            }

            beat3Light = transform.Find("emissive/beat3")?.GetComponent<Light>();
            if (beat3Light != null)
            {
                logger.Debug("beat3Light found and assigned.");
            }
            else
            {
                logger.Error("beat3Light not found under emissive/beat3.");
            }

            beat4Light = transform.Find("emissive/beat4")?.GetComponent<Light>();
            if (beat4Light != null)
            {
                logger.Debug("beat4Light found and assigned.");
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
            logger.Debug($"Controlled System Game path String: {controlledSystemGamePathString}");
            logger.Debug("Compatible Rom Dectected, Feel the Beat!...");
            logger.Debug("Beatmania Module starting Sim starting...");



        //sexy new combined input handler
        void HandleInput(ref bool inputDetected)
        {
            if (!inFocusMode) return;

            // Handle key input for beat flash
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.F) ||
                Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.C) ||
                Input.GetKeyDown(KeyCode.V))
            {
                HandleBeatFlash();
            }

            Vector2 primaryThumbstick = Vector2.zero;
            Vector2 secondaryThumbstick = Vector2.zero;

        void HandleBeatFlash()
        {
            if (beatFlashCoroutine != null)
            {
                StopCoroutine(beatFlashCoroutine);  // Stop the existing coroutine if one is running
            }

            // Increase beat intensity and clamp it to a maximum limit
            CurrentBeatIntensity += BeatIntensity;
            CurrentBeatIntensity = Mathf.Min(CurrentBeatIntensity, BeatIntensityLimit);

            // Restart the coroutine to manage the beat effect
            beatFlashCoroutine = StartCoroutine(ManageBeatEffect());
        }

        IEnumerator ManageBeatEffect()
        {
            // Calculate the intensity decrease rate
            float decayRate = BeatIntensity / BeatDuration;

            while (CurrentBeatIntensity > 0)
            {
                SetLightIntensity(CurrentBeatIntensity);
                ToggleBeatEmissive(CurrentBeatIntensity > 0);

                // Decrease intensity over time
                CurrentBeatIntensity -= decayRate * Time.deltaTime;

                // Ensure intensity does not drop below 0
                if (CurrentBeatIntensity < 0)
                {
                    CurrentBeatIntensity = 0;
                }

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

*/