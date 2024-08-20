using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

namespace WIGU.Modules.GenericKeyboard
{
    public class LedButtonController : MonoBehaviour
    {
        private IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        public KeyCode Key { get; set; }

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int keyCode);

        private const int VK_CAPITAL = 0x14;

        Renderer renderer;
        Material material;

        void Start()
        {
            logger.Debug("LedButtonController >> Start");
             if (Input.GetKeyDown(Key))
            // Obtener el renderer del objeto
            renderer = GetComponent<Renderer>();
            material = renderer.material;
            RefreshMat();
        }

        bool isCaps;

        void Refresh()
        {
            if (Input.GetKey(Key))
            {
                isCaps = !isCaps;
                RefreshMat();
            }
        }

        void RefreshMat()
        {
            logger.Debug("LedButtonController >> isCaps: " + isCaps);
            renderer.material = isCaps ? material : null;
        }

        void Update()
        {
            Refresh();
        }
    }

    public class GenericKeyboardController : MonoBehaviour
    {
        private IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        public static IntPtr mainWindowHandle;

        const string LedCapsLock = "LedCapsLock";
        const string LedNumLock = "LedNumLock";

        void Start()
        {
            // Obtener el proceso actual de Unity
            Process currentProcess = Process.GetCurrentProcess();

            // Obtener el identificador de la ventana principal del proceso actual
            mainWindowHandle = currentProcess.MainWindowHandle;

            // search button on layers
            logger.Debug("GenericKeyboardController >> Start.");

            for (int i = 0; i < transform.childCount; i++)
            {
                var eee = transform.GetChild(i);
                if (eee.name.StartsWith("Btn"))
                {
                    var name = eee.name.Substring("Btn".Length);
                    logger.Debug("GenericKeyboardController >> Found " + name + " button.");

                    if (Enum.TryParse<KeyCode> (name, out var ddd)) {
                        var controller = eee.gameObject.AddComponent<GenericKeyboardButtonController>();
                        controller.Key = ddd;
                        controller.GenericKeyboardController = this;
                    }
                } 
                else if (eee.name == LedCapsLock)
                {
                    logger.Debug("GenericKeyboardController >> Found " + LedCapsLock + " Led.");
                    var led = eee.gameObject.AddComponent<LedButtonController>();
                    led.Key = KeyCode.F1;
                }
                else if (eee.name == LedNumLock)
                {
                    logger.Debug("GenericKeyboardController >> Found " + LedNumLock + " Led.");
                    var led = eee.gameObject.AddComponent<LedButtonController>();
                    led.Key = KeyCode.F2;
                }
            }
        }

        void Update()
        {

        }
    }
}
