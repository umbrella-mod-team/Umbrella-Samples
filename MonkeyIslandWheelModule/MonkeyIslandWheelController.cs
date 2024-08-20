using UnityEngine;
using UnityEngine.XR;

namespace WIGU.Modules.MonkeyIslandWheel
{
    public class MonkeyIslandWheelController : MonoBehaviour
    {
        private IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        private readonly float velocity = 1.5f;
        private Transform childLayer;

        void Start()
        {
            childLayer = transform.Find("Front");
            logger.Debug($"{typeof(MonkeyIslandWheelController)}.Start");
        }

        void Update()
        {
            if (!HandGrabber.IsGrabbed(gameObject))
                return;

            if (Input.GetKeyDown(KeyCode.J))
            {
                childLayer.Rotate(0, 0, velocity * Time.deltaTime, Space.Self);
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                childLayer.Rotate(0, 0, velocity * Time.deltaTime, Space.Self);
            }

            if (XRDevice.isPresent)
            {
                if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger))
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
