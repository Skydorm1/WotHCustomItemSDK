using UnityEngine;

namespace WotHCustomItemSDK.Examples
{
    internal static class CustomItemConsumerExample
    {
        public static void InspectItem(string itemId)
        {
            WotHCustomItemAsset item;
            if (!WotHCustomItemSDK.TryGetCustomItem(itemId, out item))
                return;
        }

        public static Sprite LoadIcon(string bundlePath, string assetName)
        {
            return WotHCustomItemSDK.LoadAsset<Sprite>(bundlePath, assetName);
        }
    }
}
