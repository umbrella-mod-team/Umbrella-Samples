using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices; // Required for user32.dll
using WIGUx.Modules; // Required for wigx modules


namespace WIGUx.Modules.DirectInput
{
    public static class DirectInputControlModule
    {
        private static string activeConfigPath;
        private static Dictionary<string, InputBinding> directInputBindings;

        [DllImport("user32.dll")]
        private static extern uint GetRawInputData(
            IntPtr hRawInput,
            uint uiCommand,
            out RAWINPUT pData,
            ref uint pcbSize,
            uint cbSizeHeader
        );

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUT
        {
            public RAWINPUTHEADER header;
            public RAWMOUSE mouse;
            public RAWINPUTHID hid;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWMOUSE
        {
            public ushort usFlags;
            public uint ulButtons;
            public uint ulRawButtons;
            public int lLastX;
            public int lLastY;
            public uint ulExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTHID
        {
            public uint dwSizeHid;
            public uint dwCount;
            public byte bRawData;
        }

        public static void Initialize(string iniFile)
        {
            activeConfigPath = Path.Combine(Application.persistentDataPath, "Emulators/MAME/inputs/", iniFile);
            LoadConfig();
        }

        public static void LoadConfig()
        {
            directInputBindings = new Dictionary<string, InputBinding>();
            if (!File.Exists(activeConfigPath))
            {
                Debug.Log("No INI found for DirectInput, using defaults...");
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

                if (!directInputBindings.ContainsKey(action))
                    directInputBindings[action] = new InputBinding();

                InputBinding binding = directInputBindings[action];
                switch (prop)
                {
                    case "Mouse": binding.Mouse = value; break;
                    case "DInput": binding.DInput = value; break;
                    case "Sensitivity":
                        if (float.TryParse(value, out float s))
                            binding.Sensitivity = s;
                        break;
                }
            }
        }

        public static float GetAnalogValue(string action)
        {
            if (!GetDInputRawInput(out RAWINPUT rawInput))
                return 0f;

            if (!directInputBindings.ContainsKey(action))
                return 0f;

            string mappedInput = directInputBindings[action].DInput;

            float rawValue = 0f;

            switch (mappedInput)
            {
                case "StickX": rawValue = rawInput.hid.bRawData; break;
                case "StickY": rawValue = rawInput.hid.bRawData; break;
                case "Wheel": rawValue = rawInput.hid.bRawData; break;
                case "Pedal": rawValue = rawInput.hid.bRawData; break;
                case "Throttle": rawValue = rawInput.hid.bRawData; break;
                case "Rudder": rawValue = rawInput.hid.bRawData; break;
                case "Dial": rawValue = rawInput.hid.bRawData; break;
                case "Spinner": rawValue = rawInput.hid.bRawData; break;
                default:
                    Debug.LogWarning($"DirectInputControlModule: Unknown input type '{mappedInput}' for action '{action}'.");
                    return 0f;
            }

            return ApplyDeadzone(rawValue, action);
        }

        private static float ApplyDeadzone(float value, string action)
        {
            if (!directInputBindings.ContainsKey(action))
                return value;

            float sensitivity = directInputBindings[action].Sensitivity;
            return value * sensitivity;
        }

        public static bool GetDInputRawInput(out RAWINPUT rawInput)
        {
            uint size = (uint)Marshal.SizeOf(typeof(RAWINPUT));
            rawInput = new RAWINPUT();
            if (GetRawInputData(IntPtr.Zero, 0x10000003, out rawInput, ref size, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) == uint.MaxValue)
            {
                return false;
            }
            return true;
        }

        public static bool IsButtonPressed(string action)
        {
            if (!directInputBindings.ContainsKey(action)) return false;

            string mappedInput = directInputBindings[action].DInput;
            if (string.IsNullOrEmpty(mappedInput)) return false;

            return CheckRawButtonPress(mappedInput);
        }

        private static bool CheckRawButtonPress(string input)
        {
            if (GetDInputRawInput(out RAWINPUT rawInput))
            {
                uint buttonState = rawInput.mouse.ulButtons;
                if (input == "Button1" && (buttonState & 0x0001) != 0) return true;
                if (input == "Button2" && (buttonState & 0x0002) != 0) return true;
                if (input == "Button3" && (buttonState & 0x0004) != 0) return true;
                if (input == "Button4" && (buttonState & 0x0008) != 0) return true;
            }
            return false;
        }
    }

    [Serializable]
    public class InputBinding
    {
        public string Mouse;
        public string DInput;
        public float Sensitivity = 1.0f;
    }
}
