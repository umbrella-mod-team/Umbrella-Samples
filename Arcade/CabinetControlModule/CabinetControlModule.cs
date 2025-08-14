using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using System;
using System.Runtime.InteropServices; // Required for user32.dll
using WIGUx.Modules;

namespace WIGUx.Modules.CabinetControl
{
    public class CabinetControlModule : MonoBehaviour
    {
        private static string activeConfigPath;
        private static Dictionary<string, InputBinding> controlBindings;

        [Header("Control UI")]
        public GameObject bindingUI;
        public Button saveButton;
        public Button rebindButton;
        public Transform bindingsContainer;
        public GameObject bindingEntryPrefab;
        private string selectedBinding;
        private bool waitingForInput = false;

        void Start()
        {
            SetActiveConfig("default.ini"); // Default game config
            bindingUI.SetActive(false);
            saveButton.onClick.AddListener(SaveConfig);
            rebindButton.onClick.AddListener(StartRebinding);
            PopulateBindingsList();
        }

        public static void SetActiveConfig(string iniFile)
        {
            activeConfigPath = Path.Combine(Application.persistentDataPath, "Emulators/MAME/inputs/", iniFile);
            LoadConfig();
        }

        public static void LoadConfig()
        {
            controlBindings = new Dictionary<string, InputBinding>();
            if (!File.Exists(activeConfigPath))
            {
                Debug.Log("No INI found, using defaults...");
                return;
            }

            string[] lines = File.ReadAllLines(activeConfigPath);
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || !line.Contains("="))
                    continue;

                string[] parts = line.Split('=');
                if (parts.Length < 2) continue;

                string key = parts[0].Trim();
                string value = parts[1].Trim();
                string[] keyParts = key.Split('.');
                if (keyParts.Length < 2) continue;

                string action = keyParts[0];
                string prop = keyParts[1];

                if (!controlBindings.ContainsKey(action))
                    controlBindings[action] = new InputBinding();

                InputBinding binding = controlBindings[action];
                switch (prop)
                {
                    case "Keyboard": binding.Keyboard = value; break;
                    case "Mouse": binding.Mouse = value; break;
                    case "XInput": binding.XInput = value; break;
                    case "DInput": binding.DInput = value; break;
                    case "VR": binding.VR = value; break;
                    case "Sensitivity":
                        if (float.TryParse(value, out float s))
                            binding.Sensitivity = s;
                        break;
                }
            }
        }

        public static void SaveConfig()
        {
            using (var writer = new StreamWriter(activeConfigPath))
            {
                foreach (var kv in controlBindings)
                {
                    writer.WriteLine($"{kv.Key}.Keyboard={kv.Value.Keyboard}");
                    writer.WriteLine($"{kv.Key}.Mouse={kv.Value.Mouse}");
                    writer.WriteLine($"{kv.Key}.XInput={kv.Value.XInput}");
                    writer.WriteLine($"{kv.Key}.DInput={kv.Value.DInput}");
                    writer.WriteLine($"{kv.Key}.VR={kv.Value.VR}");
                    writer.WriteLine($"{kv.Key}.Sensitivity={kv.Value.Sensitivity}");
                }
            }
        }

        void PopulateBindingsList()
        {
            if (!bindingsContainer || !bindingEntryPrefab) return;

            foreach (Transform child in bindingsContainer)
                Destroy(child.gameObject);

            foreach (var kv in controlBindings)
            {
                GameObject entry = Instantiate(bindingEntryPrefab, bindingsContainer);
                entry.transform.Find("BindingName").GetComponent<Text>().text = kv.Key;
                entry.transform.Find("BindingValue").GetComponent<Text>().text = GetBindingString(kv.Value);

                Slider slider = entry.transform.Find("SensitivitySlider")?.GetComponent<Slider>();
                if (slider)
                {
                    slider.value = kv.Value.Sensitivity;
                    slider.onValueChanged.AddListener((val) => kv.Value.Sensitivity = val);
                }

                Button btn = entry.GetComponent<Button>();
                if (btn)
                    btn.onClick.AddListener(() => SelectBinding(kv.Key));
            }
        }

        void SelectBinding(string bindingKey)
        {
            selectedBinding = bindingKey;
        }

        void StartRebinding()
        {
            // Ensure the active INI is set before opening the binding menu
            CabinetControlModule.SetActiveConfig(activeConfigPath);

            if (string.IsNullOrEmpty(selectedBinding)) return;
            waitingForInput = true;
        }

        string GetBindingString(InputBinding b)
        {
            return string.Format("Keyboard: {0}, Mouse: {1}, XInput: {2}, DInput: {3}, VR: {4}, Sensitivity: {5:F1}",
                b.Keyboard, b.Mouse, b.XInput, b.DInput, b.VR, b.Sensitivity);
        }
    }

    [Serializable]
    public class InputBinding
    {
        public string Keyboard;
        public string Mouse;
        public string XInput;
        public string DInput;
        public string VR;
        public float Sensitivity = 1.0f;
    }
}






