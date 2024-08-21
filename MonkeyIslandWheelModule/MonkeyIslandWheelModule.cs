using UnityEngine;

namespace WIGU.Modules.MonkeyIslandWheel
{
    public class MonkeyIslandWheelModule : WiguModule
    {
        private IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        /// <summary>
        /// Event executed on WIGU initialization.
        /// </summary>
        public override void OnInitialize()
        {
            logger.Debug($"{nameof(MonkeyIslandWheelModule)}.Init()");
        }

        public override void OnUgcLoaded(AssetBundle assetBundle, GameObject gameObject2)
        {
            logger.Debug($"{nameof(MonkeyIslandWheelModule)}.UgcLoaded: {gameObject2.name}");

        }
    }
}
