using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace WotHCustomItemSDK
{
    internal static class PackageRegistry
    {
        internal sealed class PackageInfo
        {
            public string Name;
            public AssetBundle Bundle;
            public string BundlePath;
        }

        private static readonly Dictionary<string, PackageInfo> PackagesByName =
            new Dictionary<string, PackageInfo>(StringComparer.OrdinalIgnoreCase);

        private static readonly List<PackageInfo> OrderedPackages =
            new List<PackageInfo>();

        private static readonly Regex PackageNameRegex = new Regex(
            @"""packageName""\s*:\s*""([^""]+)""",
            RegexOptions.Compiled
        );

        public static IList<string> PackageNames
        {
            get
            {
                List<string> names = new List<string>();
                for (int i = 0; i < OrderedPackages.Count; i++)
                    names.Add(OrderedPackages[i].Name);
                return names;
            }
        }

        internal static IList<AssetBundle> Bundles
        {
            get
            {
                List<AssetBundle> bundles = new List<AssetBundle>();
                for (int i = 0; i < OrderedPackages.Count; i++)
                {
                    PackageInfo info = OrderedPackages[i];
                    if (info != null && info.Bundle != null && !bundles.Contains(info.Bundle))
                        bundles.Add(info.Bundle);
                }
                return bundles;
            }
        }

        internal static void Initialize()
        {
            OrderedPackages.Clear();
            PackagesByName.Clear();

            string directory = Plugin.ResolveModPackDirectory();
            if (!Directory.Exists(directory))
            {
                Plugin.LogWarning("Modpack directory not found: " + directory);
                return;
            }

            IList<AssetBundleRegistry.BundleHandle> bundles = LoadCandidateBundles(directory);

            for (int i = 0; i < bundles.Count; i++)
            {
                AssetBundleRegistry.BundleHandle handle = bundles[i];
                if (handle == null || handle.Bundle == null)
                    continue;

                string packageName = ReadPackageName(handle.Bundle, handle.FullPath);
                if (string.IsNullOrWhiteSpace(packageName))
                    continue;

                packageName = packageName.Trim();
                PackageInfo existing;
                if (PackagesByName.TryGetValue(packageName, out existing))
                {
                    Plugin.Log(
                        "Skipping duplicate package '" + packageName + "' from " + handle.FullPath +
                        ". Already registered from " + existing.BundlePath + "."
                    );
                    continue;
                }

                PackageInfo info = new PackageInfo
                {
                    Name = packageName,
                    Bundle = handle.Bundle,
                    BundlePath = handle.FullPath
                };

                PackagesByName.Add(packageName, info);
                OrderedPackages.Add(info);
            }

            Plugin.Log(
                "Found " + OrderedPackages.Count +
                " unique modpack package(s) in " + directory + "."
            );
        }

        internal static bool TryGetByBundle(AssetBundle bundle, out string packageName, out string bundlePath)
        {
            packageName = null;
            bundlePath = null;
            if (bundle == null)
                return false;

            foreach (PackageInfo info in OrderedPackages)
            {
                if (info != null && info.Bundle == bundle)
                {
                    packageName = info.Name;
                    bundlePath = info.BundlePath;
                    return true;
                }
            }

            return false;
        }

        internal static bool TryGet(string packageName, out PackageInfo info)
        {
            info = null;
            if (string.IsNullOrWhiteSpace(packageName))
                return false;

            return PackagesByName.TryGetValue(packageName.Trim(), out info) && info != null;
        }

        internal static Sprite LoadSprite(string packageName, string assetName)
        {
            PackageInfo info;
            if (!TryGet(packageName, out info) || info.Bundle == null || string.IsNullOrWhiteSpace(assetName))
            {
                Plugin.LogWarning("No loaded package bundle found for '" + packageName + "'.");
                return null;
            }

            try
            {
                Sprite sprite = info.Bundle.LoadAsset<Sprite>(assetName);
                if (sprite != null)
                    return sprite;

                Sprite[] sprites = info.Bundle.LoadAllAssets<Sprite>();
                for (int i = 0; i < sprites.Length; i++)
                {
                    if (sprites[i] != null &&
                        string.Equals(sprites[i].name, assetName, StringComparison.OrdinalIgnoreCase))
                        return sprites[i];
                }
            }
            catch (Exception ex)
            {
                Plugin.LogError(
                    "Failed to load package sprite '" + assetName + "' from '" + packageName + "': " + ex
                );
            }

            Plugin.LogError(
                "Package sprite '" + assetName + "' was not found in package '" + packageName + "'."
            );
            return null;
        }

        private static string ReadPackageName(AssetBundle bundle, string bundlePath)
        {
            string embeddedName = ReadPackageNameFromBundle(bundle);
            if (!string.IsNullOrWhiteSpace(embeddedName))
                return embeddedName;

            if (string.IsNullOrWhiteSpace(bundlePath))
                return null;

            string directory = Path.GetDirectoryName(bundlePath);
            if (string.IsNullOrWhiteSpace(directory))
                return null;

            string parentDirectory = Directory.GetParent(directory) == null
                ? null
                : Directory.GetParent(directory).FullName;
            string[] metadataPaths =
            {
                Path.Combine(directory, "packagename.json"),
                Path.Combine(directory, "Content", "packagename.json"),
                parentDirectory == null ? string.Empty : Path.Combine(parentDirectory, "Content", "packagename.json")
            };

            for (int i = 0; i < metadataPaths.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(metadataPaths[i]) || !File.Exists(metadataPaths[i]))
                    continue;

                try
                {
                    string value = ParsePackageName(File.ReadAllText(metadataPaths[i]));
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
                catch (Exception ex)
                {
                    Plugin.LogWarning("Could not read package metadata '" + metadataPaths[i] + "': " + ex.Message);
                }
            }

            return null;
        }

        private static string ReadPackageNameFromBundle(AssetBundle bundle)
        {
            if (bundle == null)
                return null;

            try
            {
                TextAsset direct = bundle.LoadAsset<TextAsset>("packagename");
                if (direct != null)
                {
                    string value = ParsePackageName(direct.text);
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }
            catch { }

            try
            {
                TextAsset[] texts = bundle.LoadAllAssets<TextAsset>();
                for (int i = 0; i < texts.Length; i++)
                {
                    TextAsset text = texts[i];
                    if (text == null)
                        continue;

                    if (string.Equals(text.name, "packagename", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(text.name, "packagename.json", StringComparison.OrdinalIgnoreCase))
                    {
                        string value = ParsePackageName(text.text);
                        if (!string.IsNullOrWhiteSpace(value))
                            return value;
                    }
                }
            }
            catch { }

            return null;
        }


        private static IList<AssetBundleRegistry.BundleHandle> LoadCandidateBundles(string directory)
        {
            if (!SamePath(directory, BepInEx.Paths.PluginPath))
                return AssetBundleRegistry.LoadDirectory(directory);

            List<AssetBundleRegistry.BundleHandle> result = new List<AssetBundleRegistry.BundleHandle>();
            if (!Directory.Exists(directory))
                return result;

            string sdkDirectory = Plugin.GetPluginDirectory();
            string[] pluginDirectories = Directory.GetDirectories(directory);
            Array.Sort(pluginDirectories, StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < pluginDirectories.Length; i++)
            {
                string pluginDirectory = pluginDirectories[i];
                if (SamePath(pluginDirectory, sdkDirectory))
                    continue;

                IList<AssetBundleRegistry.BundleHandle> loaded =
                    AssetBundleRegistry.LoadDirectory(pluginDirectory);

                for (int j = 0; j < loaded.Count; j++)
                {
                    if (loaded[j] != null && loaded[j].Bundle != null)
                        result.Add(loaded[j]);
                }
            }

            return result;
        }

        private static bool SamePath(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
                return false;

            try
            {
                string normalizedLeft = Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string normalizedRight = Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static string ParsePackageName(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            Match match = PackageNameRegex.Match(json);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }
    }
}
