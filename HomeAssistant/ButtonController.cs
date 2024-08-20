using System.Net;
using UnityEngine;
using UnityEngine.Events;
using WIGU;

namespace WIGUx.Modules.HomeAssistant
{
    public class ButtonController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        IoTButtonController ioTButtonController;

        const string ActionableColliderLocalPath = "ActionableCollider";
        const string ButtonLocalPath = "Button";

        void Start()
        {
            logger.Info($"ButtonController: Starting...");
            var collider = transform.Find(ActionableColliderLocalPath)?.gameObject;
            if (collider == null ) 
            {
                logger.Info($"ButtonController: Error 'ActionableColliderLocalPath': /{ActionableColliderLocalPath} was not found");
                return;
            }
           
            ioTButtonController = collider.AddComponent<IoTButtonController>();

            var button = transform.Find(ButtonLocalPath);
            if (button == null)
            {
                logger.Info($"ButtonController: Error 'ButtonLocalPath': /{ButtonLocalPath} was not found");
                return;
            }

            ioTButtonController.Initialize(button);
        }
    }
}
