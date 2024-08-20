using UnityEngine;
using WIGU;

namespace WIGUx.Modules.Level
{
    public class LevelController : MonoBehaviour
    {
        IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        void Start()
        {
            logger.Debug("Level was loadded!!!!");
        }
    }
}
