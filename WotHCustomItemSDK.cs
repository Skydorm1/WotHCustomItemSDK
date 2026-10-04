using System;
using System.Collections.Generic;
using UnityEngine;

namespace WotHCustomItemSDK
{
    public static class WotHCustomItemSDK
    {
        public static IList<string> PackageNames
        {
            get { return PackageRegistry.PackageNames; }
        }

        public static IList<WotHCustomItemAsset> CustomItems
        {
            get { return CustomItemRuntime.CustomItems; }
        }

        public static bool IsInitialized
        {
            get { return CustomItemRuntime.IsInitialized; }
        }

        public static AssetBundle GetOrLoadBundle(string path)
        {
            return AssetBundleRegistry.GetOrLoad(path);
        }

        public static T LoadAsset<T>(string path, string assetName) where T : UnityEngine.Object
        {
            AssetBundle bundle = AssetBundleRegistry.GetOrLoad(path);
            if (bundle == null || string.IsNullOrWhiteSpace(assetName))
                return null;

            try
            {
                T result = bundle.LoadAsset<T>(assetName);
                if (result != null)
                    return result;

                T[] assets = bundle.LoadAllAssets<T>();
                for (int i = 0; i < assets.Length; i++)
                {
                    if (assets[i] != null && string.Equals(assets[i].name, assetName, StringComparison.OrdinalIgnoreCase))
                        return assets[i];
                }
            }
            catch (Exception ex)
            {
                Plugin.LogError("Failed to load asset '" + assetName + "' from '" + path + "': " + ex);
            }

            return null;
        }

        public static bool TryGetCustomItem(string itemId, out WotHCustomItemAsset item)
        {
            item = null;
            if (string.IsNullOrWhiteSpace(itemId))
                return false;

            IList<WotHCustomItemAsset> items = CustomItems;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && string.Equals(items[i].ItemId, itemId.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    item = items[i];
                    return true;
                }
            }

            return false;
        }

        public static Sprite LoadPackageSprite(string packageName, string assetName)
        {
            return PackageRegistry.LoadSprite(packageName, assetName);
        }

        public static bool AddOrUpdateLocalization(
            string term,
            string languageCode,
            string translation,
            string description = "")
        {
            return LocalizationRuntime.AddOrUpdateTerm(term, languageCode, translation, description);
        }

        public static bool UnlockItem(string itemId)
        {
            return CustomItemService.UnlockItem(itemId);
        }
    }
}
