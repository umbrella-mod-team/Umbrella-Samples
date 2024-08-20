using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace WIGU.Modules.GenericKeyboard
{
    class KeyHelper
    {
       public  static Dictionary<KeyCode, int> Key = new Dictionary<KeyCode, int>()
{
    { KeyCode.A, 0x41 }, { KeyCode.B, 0x42 }, { KeyCode.C, 0x43 }, { KeyCode.D, 0x44 },
    { KeyCode.E, 0x45 }, { KeyCode.F, 0x46 }, { KeyCode.G, 0x47 }, { KeyCode.H, 0x48 },
    { KeyCode.I, 0x49 }, { KeyCode.J, 0x4A }, { KeyCode.K, 0x4B }, { KeyCode.L, 0x4C },
    { KeyCode.M, 0x4D }, { KeyCode.N, 0x4E }, { KeyCode.O, 0x4F }, { KeyCode.P, 0x50 },
    { KeyCode.Q, 0x51 }, { KeyCode.R, 0x52 }, { KeyCode.S, 0x53 }, { KeyCode.T, 0x54 },
    { KeyCode.U, 0x55 }, { KeyCode.V, 0x56 }, { KeyCode.W, 0x57 }, { KeyCode.X, 0x58 },
    { KeyCode.Y, 0x59 }, { KeyCode.Z, 0x5A }, { KeyCode.Alpha0, 0x30 }, { KeyCode.Alpha1, 0x31 },
    { KeyCode.Alpha2, 0x32 }, { KeyCode.Alpha3, 0x33 }, { KeyCode.Alpha4, 0x34 }, { KeyCode.Alpha5, 0x35 },
    { KeyCode.Alpha6, 0x36 }, { KeyCode.Alpha7, 0x37 }, { KeyCode.Alpha8, 0x38 }, { KeyCode.Alpha9, 0x39 },
    { KeyCode.Keypad0, 0x60 }, { KeyCode.Keypad1, 0x61 }, { KeyCode.Keypad2, 0x62 }, { KeyCode.Keypad3, 0x63 },
    { KeyCode.Keypad4, 0x64 }, { KeyCode.Keypad5, 0x65 }, { KeyCode.Keypad6, 0x66 }, { KeyCode.Keypad7, 0x67 },
    { KeyCode.Keypad8, 0x68 }, { KeyCode.Keypad9, 0x69 }, { KeyCode.KeypadPeriod, 0x6E }, { KeyCode.KeypadDivide, 0x6F },
    { KeyCode.KeypadMultiply, 0x6A }, { KeyCode.KeypadMinus, 0x6D }, { KeyCode.KeypadPlus, 0x6B }, { KeyCode.KeypadEnter, 0x0D },
    { KeyCode.Space, 0x20 }, { KeyCode.Backspace, 0x08 }, { KeyCode.Tab, 0x09 }, { KeyCode.Return, 0x0D },
    { KeyCode.Escape, 0x1B }, { KeyCode.Delete, 0x2E }, { KeyCode.UpArrow, 0x26 }, { KeyCode.DownArrow, 0x28 },
    { KeyCode.LeftArrow, 0x25 }, { KeyCode.RightArrow, 0x27 }, { KeyCode.LeftShift, 0xA0 }, { KeyCode.RightShift, 0xA1 },
    { KeyCode.LeftControl, 0xA2 }, { KeyCode.RightControl, 0xA3 }, { KeyCode.LeftAlt, 0xA4 }, { KeyCode.RightAlt, 0xA5 },{ KeyCode.CapsLock, 0x14 }
};
    }


    public class GenericKeyboardButtonController : MonoBehaviour
    {
        private IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        public GenericKeyboardController GenericKeyboardController { get; set; }
        public KeyCode Key { get; set; }

        static float number = 0.016f - 0.0104f;
        Vector3 normal, pressed;

        void Start()
        {
            normal = transform.localPosition;
            pressed = new Vector3(normal.x, normal.y, normal.z - number);
        }

        void Update()
        {
            if (Input.GetKeyDown(Key))
            {
                logger.Debug("GenericKeyboardButtonController.KeyDown >> " + Key);
                transform.localPosition = pressed;
            }

            if (Input.GetKeyUp(Key))
            {
                logger.Debug("GenericKeyboardButtonController.KeyUp >> " + Key);
                transform.localPosition = normal;
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            logger.Debug($"GenericKeyboardButtonController.OnCollisionEnter >> {Key} >> {collision.gameObject.name}");
        }

        #region Key send event

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        public const int KEYEVENTF_EXTENDEDKEY = 0x0001;
        public const int KEYEVENTF_KEYUP = 0x0002;

        bool isKeyDownPressed = false;

        static void SendTabKey(byte key)
        {
            keybd_event(key, 0, KEYEVENTF_EXTENDEDKEY, 0);
            keybd_event(key, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
        }

        #endregion
    }
}
