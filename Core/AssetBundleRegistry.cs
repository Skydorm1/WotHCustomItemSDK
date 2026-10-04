using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WotHCustomItemSDK
{
    internal static class AssetBundleRegistry
    {
        internal sealed class BundleHandle
        {
            public string FullPath;
            public AssetBundle Bundle;
            public bool OwnedBySdk;
        }

        private static readonly Dictionary<string, BundleHandle> BundlesByPath =
            new Dictionary<string, BundleHandle>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, BundleHandle> BundlesByName =
            new Dictionary<string, BundleHandle>(StringComparer.OrdinalIgnoreCase);

        private static readonly string[] BundleExtensions =
        {
            ".bundle",
            ".unity3d",
            ".assetbundle"
        };

        public static AssetBundle GetOrLoad(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            string fullPath = NormalizePath(path);
            BundleHandle existing;
            if (BundlesByPath.TryGetValue(fullPath, out existing) &&
                existing != null &&
                existing.Bundle != null)
            {
                return existing.Bundle;
            }

            if (!File.Exists(fullPath))
            {
                Plugin.LogWarning("AssetBundle file not found: " + fullPath);
                return null;
            }

            AssetBundle alreadyLoaded = FindUnityLoadedBundle(fullPath);
            if (alreadyLoaded != null)
            {
                Register(fullPath, alreadyLoaded, false);
                Plugin.Log("Reusing already loaded AssetBundle: " + fullPath);
                return alreadyLoaded;
            }

            try
            {
                AssetBundle bundle = AssetBundle.LoadFromFile(fullPath);
                if (bundle == null)
                {
                    Plugin.LogError("AssetBundle.LoadFromFile returned null: " + fullPath);
                    return null;
                }

                Register(fullPath, bundle, true);
                Plugin.Log("Loaded AssetBundle: " + fullPath);
                return bundle;
            }
            catch (Exception ex)
            {
                Plugin.LogError("Failed to load AssetBundle '" + fullPath + "': " + ex);
                return null;
            }
        }

        internal static IList<BundleHandle> LoadDirectory(string directory)
        {
            List<BundleHandle> result = new List<BundleHandle>();
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                return result;

            string[] files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < files.Length; i++)
            {
                if (!LooksLikeAssetBundleFile(files[i]))
                    continue;

                AssetBundle bundle = GetOrLoad(files[i]);
                if (bundle == null)
                    continue;

                BundleHandle handle;
                if (BundlesByPath.TryGetValue(NormalizePath(files[i]), out handle) && handle != null)
                    result.Add(handle);
            }

            return result;
        }

        internal static IList<BundleHandle> GetLoadedBundlesInDirectory(string directory)
        {
            List<BundleHandle> result = new List<BundleHandle>();
            if (string.IsNullOrWhiteSpace(directory))
                return result;

            string normalizedDirectory = NormalizePath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            foreach (BundleHandle handle in BundlesByPath.Values)
            {
                if (handle == null || handle.Bundle == null)
                    continue;

                if (handle.FullPath.StartsWith(normalizedDirectory, StringComparison.OrdinalIgnoreCase))
                    result.Add(handle);
            }

            result.Sort(delegate(BundleHandle left, BundleHandle right)
            {
                return string.Compare(left.FullPath, right.FullPath, StringComparison.OrdinalIgnoreCase);
            });

            return result;
        }

        internal static bool TryGetPath(AssetBundle bundle, out string path)
        {
            path = null;
            if (bundle == null)
                return false;

            foreach (KeyValuePair<string, BundleHandle> entry in BundlesByPath)
            {
                if (entry.Value != null && entry.Value.Bundle == bundle)
                {
                    path = entry.Key;
                    return true;
                }
            }

            return false;
        }

        internal static bool TryGet(string path, out AssetBundle bundle)
        {
            bundle = null;
            if (string.IsNullOrWhiteSpace(path))
                return false;

            BundleHandle handle;
            if (!BundlesByPath.TryGetValue(NormalizePath(path), out handle) || handle == null || handle.Bundle == null)
                return false;

            bundle = handle.Bundle;
            return true;
        }

        internal static void Shutdown(bool unloadOwnedBundles)
        {
            if (unloadOwnedBundles)
            {
                foreach (BundleHandle handle in BundlesByPath.Values)
                {
                    if (handle == null || handle.Bundle == null || !handle.OwnedBySdk)
                        continue;

                    try { handle.Bundle.Unload(false); }
                    catch { }
                }
            }

            BundlesByPath.Clear();
            BundlesByName.Clear();
        }

        private static void Register(string fullPath, AssetBundle bundle, bool ownedBySdk)
        {
            BundleHandle handle = new BundleHandle
            {
                FullPath = fullPath,
                Bundle = bundle,
                OwnedBySdk = ownedBySdk
            };

            BundlesByPath[fullPath] = handle;
            if (bundle != null && !string.IsNullOrWhiteSpace(bundle.name))
                BundlesByName[bundle.name] = handle;
        }

        private static AssetBundle FindUnityLoadedBundle(string fullPath)
        {
            string fileName = Path.GetFileName(fullPath);
            string fileStem = Path.GetFileNameWithoutExtension(fullPath);

            foreach (AssetBundle bundle in AssetBundle.GetAllLoadedAssetBundles())
            {
                if (bundle == null || string.IsNullOrWhiteSpace(bundle.name))
                    continue;

                if (string.Equals(bundle.name, fileName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(bundle.name, fileStem, StringComparison.OrdinalIgnoreCase))
                {
                    return bundle;
                }
            }

            BundleHandle byName;
            if (BundlesByName.TryGetValue(fileName, out byName) && byName != null)
                return byName.Bundle;
            if (BundlesByName.TryGetValue(fileStem, out byName) && byName != null)
                return byName.Bundle;

            return null;
        }

        private static string NormalizePath(string path)
        {
            try
            {
                return Path.GetFullPath(path)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return path;
            }
        }

        private static bool LooksLikeAssetBundleFile(string path)
        {
            string extension = Path.GetExtension(path);
            if (string.IsNullOrEmpty(extension))
                return true;

            for (int i = 0; i < BundleExtensions.Length; i++)
            {
                if (string.Equals(extension, BundleExtensions[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
