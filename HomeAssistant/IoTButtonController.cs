using UnityEngine;
using WIGU;

namespace WIGUx.Modules.HomeAssistant
{
    public class IoTButtonController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();
        static IHomeAssistantService service = ServiceProvider.Instance.GetService<IHomeAssistantService>();

        private bool isPressed;
        private Collider presser;
        public Transform button;
        private AudioSource sound;

        public void Initialize(Transform buttonTransform)
        {
            logger.Info($"IoTButtonController: Initializing...");
            button = buttonTransform;
            sound = GetComponent<AudioSource>();
        }

        /// <summary>
        /// When the collider associated to this button collides any object this method is executed
        /// </summary>
        /// <param name="other"></param>
        private void OnTriggerEnter(Collider other)
        {
            // if there are no button associed we don't want to continue
            if (button == null)
                return;

            // If the button is not pressed then we start the press action
            if (!isPressed)
            {
                isPressed = true;
                logger.Info($"Pressed!!!!");

                // Moves the button model position to simulate a press
                button.localPosition = new Vector3(0, -0.089f, 0);

                // OnTriggerExit needs to know the object who is no longer pressed so we keep it for compare
                presser = other;

                // This sends a notification to Home Assistant to Switch on/off the light!
                service.SetLightToggle(StaticLights.LamparaDePie);

                // Beautiful sound when press!
                sound.Play();
            }
        }

        /// <summary>
        /// This thriggers when any object no longer collide with the current object
        /// </summary>
        /// <param name="other"></param>
        private void OnTriggerExit(Collider other)
        {
            // OnTriggerExit only reacts to the object pressed 
            if ( other == presser)
            {
                logger.Info($"Unpressed!!!!");

                // Let's leave the button model in the original position
                button.localPosition = new Vector3(0, -0.007f, 0);

                isPressed = false;
            }
        }
    }
}
