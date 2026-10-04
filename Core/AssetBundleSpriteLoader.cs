using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WotHCustomItemSDK
{
    internal static class AssetBundleSpriteLoader
    {
        private static string _loadedPath;
        private static AssetBundle _bundle;
        private static readonly Dictionary<string, Sprite> SpriteCache =
            new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        public const string CustomNormal = "CustomMode";
        public const string CustomHighlight = "CustomMode_Hover";
        public const string CustomSelected = "SetCustomMode";
        public const string PackageNormal = "CustomPackageMode";
        public const string PackageHighlight = "CustomPackageMode_Hover";
        public const string PackageSelected = "SetCustomPackageMode";

        public static bool EnsureLoaded()
        {
            string path = Plugin.ResolveBundlePath(Plugin.BundleFile.Value);
            if (_bundle != null && string.Equals(_loadedPath, path, StringComparison.OrdinalIgnoreCase))
                return true;

            if (!File.Exists(path))
            {
                Plugin.LogError("AssetBundle not found: " + path);
                return false;
            }

            _bundle = AssetBundleRegistry.GetOrLoad(path);
            _loadedPath = path;
            SpriteCache.Clear();

            if (_bundle == null)
                return false;

            Plugin.Log("Loaded sprite AssetBundle through shared registry: " + path);
            return true;
        }

        public static Sprite LoadSprite(string assetName)
        {
            if (string.IsNullOrWhiteSpace(assetName) || !EnsureLoaded())
                return null;

            Sprite cached;
            if (SpriteCache.TryGetValue(assetName, out cached) && cached != null)
                return cached;

            try
            {
                Sprite sprite = _bundle.LoadAsset<Sprite>(assetName);
                if (sprite == null)
                {
                    Sprite[] sprites = _bundle.LoadAllAssets<Sprite>();
                    for (int i = 0; i < sprites.Length; i++)
                    {
                        if (sprites[i] != null &&
                            string.Equals(sprites[i].name, assetName, StringComparison.OrdinalIgnoreCase))
                        {
                            sprite = sprites[i];
                            break;
                        }
                    }
                }

                if (sprite != null)
                {
                    SpriteCache[assetName] = sprite;
                    return sprite;
                }
            }
            catch (Exception ex)
            {
                Plugin.LogError("Failed to load sprite '" + assetName + "': " + ex);
            }

            Plugin.LogError("Sprite asset not found in bundle: " + assetName);
            return null;
        }
    }
}
