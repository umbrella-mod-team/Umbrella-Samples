using UnityEngine;
using UnityEngine.XR;
using WIGU;

namespace WIGUx.Modules.ToggleEmission
{

    public class ToggleEmissionController : MonoBehaviour
    {
        /// <summary>
        /// Get logger from service provider to use logging feature in our controller
        /// </summary>
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        private readonly float velocity = 1.5f;
        private readonly float velocityKey = 20.5f;
        private Transform childLayer;

        void Start()
        {
            logger.Info("ToggleEmission script started...");

            childLayer = transform.Find("Front");
        }

        void Update()
        {
            // If user is not selecting or grabbing the object no need to continue
            if (!PlayerControllerHelper.IsObjectSelectedOrGrabbed(gameObject))
                return;

            if (Input.GetKey(KeyCode.J))
            {
                childLayer.Rotate(0, 0, velocityKey * Time.deltaTime, Space.Self);
            }

            if (Input.GetKey(KeyCode.H))
            {
                childLayer.Rotate(0, 0, -velocityKey * Time.deltaTime, Space.Self);
            }

            if (XRDevice.isPresent)
            {
                if (XRDevice.isPresent && OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger))
                {
                    childLayer.Rotate(0, 0, -velocity, Space.Self);
                }

                if (OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger))
                {
                    childLayer.Rotate(0, 0, velocity, Space.Self);
                }
            }
        }
    }
}
