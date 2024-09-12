using UnityEngine;

namespace WIGU.Modules.MonkeyIslandWheel
{
    public class MonkeyIslandWheelModule : WiguModule
    {
        /// <summary>
        /// Event executed on WIGU initialization.
        /// </summary>
        public override void OnInitialize()
        {
            Debug.Log($"{GetType().Name}.Init()");
        }

        public override void OnUgcLoaded(AssetBundle assetBundle, GameObject gameObject2)
        {
            Debug.Log($"{GetType().Name}.UgcLoaded: {gameObject2.name}");
        }
    }
}
