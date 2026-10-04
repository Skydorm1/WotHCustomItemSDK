using UnityEngine;

namespace WotHCustomItemSDK
{
    public sealed class WotHCustomItemAsset
    {
        public AssetBundle Bundle;
        public string AssetName;
        public HoldThingContainer Item;
        public string BundlePath;
        public string PackageName;

        public string ItemId
        {
            get
            {
                if (Item == null || Item.HoldThingData == null)
                    return null;

                return Item.HoldThingData.ID;
            }
        }
    }
}
