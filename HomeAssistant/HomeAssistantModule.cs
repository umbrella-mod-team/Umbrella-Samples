//using HADotNet.Core.Clients;
//using HADotNet.Core;
using UnityEngine;
using WIGU;

namespace WIGUx.Modules.HomeAssistant
{
    public class HomeAssistantModule : WIGUModule
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        string token = "your_home_assistant_token";

        // internal static ConfigClient configClient;
        /// <summary>
        /// Event executed on WIGU initialization.
        /// </summary>
        public override void OnInitialize()
        {
            logger.Info($"{GetType().Assembly.FullName} OnInitialize");
            ServiceProvider.Instance.Register<IHomeAssistantService>(new HomeAssistantApi().SetToken(token));
        }

        /// <summary>
        /// This event is executed on each room loaded.
        /// </summary>
        /// <param name="index">Level's index loaded</param>
        public override void OnLevelLoaded(int index)
        {
            logger.Info($"{GetType().Assembly.FullName} OnLevelLoaded: {index}");
        }

        /// <summary>
        /// Executes on game initialization when all the UGC's are loaded into memory.
        /// You can modifiy initial properties or add/remove components.
        /// </summary>
        /// <param name="gameObject2">GameObject loaded.</param>
        public override void OnUgcLoaded(AssetBundle assetBundle, GameObject gameObject2)
        {
            logger.Info($"{GetType().Assembly.FullName} OnUgcLoaded: {gameObject2.name}");
        }
    }
}
