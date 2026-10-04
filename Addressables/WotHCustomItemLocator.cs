using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace WotHCustomItemSDK
{
    public class WotHCustomItemLocator : IResourceLocator
    {
        private const string ItemLabel = "ItemSOC";
    
        public string LocatorId => "WotH.CustomItemLocator";
    
        public IEnumerable<object> Keys => locationsByKey.Keys;
    
        private readonly Dictionary<
            object,
            List<IResourceLocation>
        > locationsByKey =
            new Dictionary<
                object,
                List<IResourceLocation>
            >();
    
        private readonly WotHCustomItemProvider provider;
    
        private readonly List<WotHCustomItemAsset> itemAssets;
    
        private readonly List<WotHCustomBundleAsset> bundleAssets;
    
        public WotHCustomItemLocator(
            WotHCustomItemProvider provider,
            List<WotHCustomItemAsset> itemAssets,
            List<WotHCustomBundleAsset> bundleAssets)
        {
            this.provider = provider;
    
            this.itemAssets = itemAssets;
    
            this.bundleAssets = bundleAssets;
    
            BuildLocations();
        }
    
        private void BuildLocations()
        {
            for (int i = 0; i < itemAssets.Count; i++)
            {
                WotHCustomItemAsset itemAsset = itemAssets[i];
    
                if (itemAsset == null)
                    continue;
    
                if (itemAsset.Item == null)
                    continue;
    
                if (itemAsset.Item.HoldThingData == null)
                    continue;
    
                string itemId = itemAsset.Item.HoldThingData.ID;
    
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    LoggerLog(
                        "HoldThingContainer has no Item ID: " +
                        itemAsset.AssetName
                    );
    
                    continue;
                }
    
                AddLocation(
                    ItemLabel,
                    itemAsset.AssetName,
                    typeof(HoldThingContainer)
                );
    
                string objectRoot =
                    GetObjectRootPath(
                        itemAsset.AssetName
                    );
    
                if (string.IsNullOrWhiteSpace(objectRoot))
                {
                    Debug.LogWarning(
                        "[WotH Custom Item Locator] " +
                        "Could not determine Content/Objects root for item: " +
                        itemAsset.AssetName
                    );
    
                    continue;
                }
    
                BuildImageAliases(
                    itemId,
                    objectRoot
                );
    
                BuildFeatureAliases(
                    itemId,
                    objectRoot
                );

                BuildAdditionalAssetAliases(
                    objectRoot
                );
            }
    
            for (int i = 0; i < bundleAssets.Count; i++)
            {
                WotHCustomBundleAsset bundleAsset = bundleAssets[i];
    
                if (bundleAsset == null)
                    continue;
    
                if (bundleAsset.Asset == null)
                    continue;
    
                Type type = bundleAsset.AssetType;
    
                if (type == null)
                    type = bundleAsset.Asset.GetType();
    
                AddLocation(
                    bundleAsset.AssetName,
                    bundleAsset.AssetName,
                    type
                );
            }
        }
    
        private static string NormalizeAssetPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return null;
    
            return assetPath
                .Replace('\\', '/')
                .TrimStart('/');
        }
    
        private static string GetObjectRootPath(string assetPath)
        {
            string normalized =
                NormalizeAssetPath(
                    assetPath
                );
    
            if (string.IsNullOrWhiteSpace(normalized))
                return null;
    
            const string objectsMarker = "content/objects/";
    
            int markerIndex =
                normalized.IndexOf(
                    objectsMarker,
                    StringComparison.OrdinalIgnoreCase
                );
    
            if (markerIndex < 0)
                return null;
    
            int objectNameStart = markerIndex + objectsMarker.Length;
    
            int objectNameEnd =
                normalized.IndexOf(
                    '/',
                    objectNameStart
                );
    
            if (objectNameEnd < 0)
                return null;
    
            return normalized.Substring(
                0,
                objectNameEnd
            );
        }
    
        private static bool IsInsideObjectRoot(
            string assetPath,
            string objectRoot)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return false;
    
            if (string.IsNullOrWhiteSpace(objectRoot))
                return false;
    
            string normalizedAssetPath =
                NormalizeAssetPath(
                    assetPath
                );
    
            string normalizedObjectRoot =
                NormalizeAssetPath(
                    objectRoot
                );
    
            if (string.IsNullOrWhiteSpace(normalizedAssetPath))
                return false;
    
            if (string.IsNullOrWhiteSpace(normalizedObjectRoot))
                return false;
    
            string prefix =
                normalizedObjectRoot.TrimEnd('/') +
                "/";
    
            return normalizedAssetPath.StartsWith(
                prefix,
                StringComparison.OrdinalIgnoreCase
            );
        }
    
        private void BuildImageAliases(
        string itemId,
        string objectRoot)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return;
    
            if (string.IsNullOrWhiteSpace(objectRoot))
                return;
    
            string normalizedObjectRoot = NormalizeAssetPath(objectRoot);
    
            string iconMarker =
                normalizedObjectRoot +
                "/arts/newitemsicon/";
    
            for (int i = 0; i < bundleAssets.Count; i++)
            {
                WotHCustomBundleAsset bundleAsset = bundleAssets[i];
    
                if (bundleAsset == null)
                    continue;
    
                if (bundleAsset.Asset == null)
                    continue;
    
                if (bundleAsset.AssetType != typeof(Sprite) &&
                    bundleAsset.AssetType != typeof(Texture2D))
                {
                    continue;
                }
    
                string normalizedAssetName =
                    NormalizeAssetPath(
                        bundleAsset.AssetName
                    );
    
                if (!IsDirectChildOfFolder(
                        normalizedAssetName,
                        iconMarker))
                {
                    continue;
                }
    
                string fileName =
                    Path.GetFileName(
                        normalizedAssetName
                    );
    
                if (string.IsNullOrWhiteSpace(fileName))
                    continue;
    
                if (!fileName.EndsWith(
                        ".png",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
    
                string fileBaseName =
                    Path.GetFileNameWithoutExtension(
                        fileName
                    );
    
                if (!string.Equals(
                        fileBaseName,
                        itemId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
    
                string gamePath =
                    "Assets/Arts/NewItemsIcon/" +
                    itemId +
                    ".png";
    
                RegisterFoundImageAlias(
                    gamePath,
                    bundleAsset.AssetName,
                    "ICON"
                );
    
                break;
            }
    
            string newItemsMarker =
                normalizedObjectRoot +
                "/arts/newitems/";
    
            string expectedPrefix =
                "item_" +
                itemId;
    
            for (int i = 0; i < bundleAssets.Count; i++)
            {
                WotHCustomBundleAsset bundleAsset = bundleAssets[i];
    
                if (bundleAsset == null)
                    continue;
    
                if (bundleAsset.Asset == null)
                    continue;
    
                if (bundleAsset.AssetType != typeof(Sprite) &&
                    bundleAsset.AssetType != typeof(Texture2D))
                {
                    continue;
                }
    
                string normalizedAssetName =
                    NormalizeAssetPath(
                        bundleAsset.AssetName
                    );
    
                if (!IsDirectChildOfFolder(
                        normalizedAssetName,
                        newItemsMarker))
                {
                    continue;
                }
    
                string fileName =
                    Path.GetFileName(
                        normalizedAssetName
                    );
    
                if (string.IsNullOrWhiteSpace(fileName))
                    continue;
    
                if (!fileName.EndsWith(
                        ".png",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
    
                string fileBaseName =
                    Path.GetFileNameWithoutExtension(
                        fileName
                    );
    
                if (string.IsNullOrWhiteSpace(fileBaseName))
                    continue;
    
                if (!fileBaseName.StartsWith(
                        expectedPrefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
    
                if (string.Equals(
                        fileBaseName,
                        expectedPrefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    RegisterFoundImageAlias(
                        "Assets/Arts/NewItems/Item_" +
                        itemId +
                        ".png",
    
                        bundleAsset.AssetName,
    
                        "MAIN"
                    );
    
                    continue;
                }
    
                if (string.Equals(
                        fileBaseName,
                        expectedPrefix + "_s",
                        StringComparison.OrdinalIgnoreCase))
                {
                    RegisterFoundImageAlias(
                        "Assets/Arts/NewItems/Item_" +
                        itemId +
                        "_s.png",
    
                        bundleAsset.AssetName,
    
                        "SMALL"
                    );
    
                    continue;
                }
    
                string maskPrefix =
                    expectedPrefix +
                    "_mb";
    
                if (fileBaseName.StartsWith(
                        maskPrefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    string indexString =
                        fileBaseName.Substring(
                            maskPrefix.Length
                        );
    
                    int index;
    
                    if (int.TryParse(
                            indexString,
                            out index))
                    {
                        RegisterFoundImageAlias(
                            "Assets/Arts/NewItems/" +
                            "Item_" +
                            itemId +
                            "_mb" +
                            index +
                            ".png",
    
                            bundleAsset.AssetName,
    
                            "MASK"
                        );
    
                        continue;
                    }
                }
    
                string skinPrefix =
                    expectedPrefix +
                    "_skin";
    
                if (fileBaseName.StartsWith(
                        skinPrefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    string indexString =
                        fileBaseName.Substring(
                            skinPrefix.Length
                        );
    
                    int index;
    
                    if (int.TryParse(
                            indexString,
                            out index))
                    {
                        RegisterFoundImageAlias(
                            "Assets/Arts/NewItems/" +
                            "Item_" +
                            itemId +
                            "_skin" +
                            index +
                            ".png",
    
                            bundleAsset.AssetName,
    
                            "SKIN"
                        );
    
                        continue;
                    }
                }
            }
    
            string itemChangeSkinMarker =
                normalizedObjectRoot +
                "/arts/itemchangeskin/";
    
            string itemChangeSkinPrefix =
                "item_" +
                itemId +
                "_";
    
            for (int i = 0; i < bundleAssets.Count; i++)
            {
                WotHCustomBundleAsset bundleAsset = bundleAssets[i];
    
                if (bundleAsset == null)
                    continue;
    
                if (bundleAsset.Asset == null)
                    continue;
    
                if (bundleAsset.AssetType != typeof(Sprite) &&
                    bundleAsset.AssetType != typeof(Texture2D))
                {
                    continue;
                }
    
                string normalizedAssetName =
                    NormalizeAssetPath(
                        bundleAsset.AssetName
                    );
    
                if (!IsDirectChildOfFolder(
                        normalizedAssetName,
                        itemChangeSkinMarker))
                {
                    continue;
                }
    
                string fileName =
                    Path.GetFileName(
                        normalizedAssetName
                    );
    
                if (string.IsNullOrWhiteSpace(fileName))
                    continue;
    
                if (!fileName.EndsWith(
                        ".png",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
    
                string fileBaseName =
                    Path.GetFileNameWithoutExtension(
                        fileName
                    );
    
                if (!fileBaseName.StartsWith(
                        itemChangeSkinPrefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
    
                string indexString =
                    fileBaseName.Substring(
                        itemChangeSkinPrefix.Length
                    );
    
                int index;
    
                if (!int.TryParse(
                        indexString,
                        out index))
                {
                    continue;
                }
    
                RegisterFoundImageAlias(
                    "Assets/Arts/ItemChangeSkin/" +
                    "Item_" +
                    itemId +
                    "_" +
                    index +
                    ".png",
    
                    bundleAsset.AssetName,
    
                    "ITEM CHANGE SKIN"
                );
            }
        }
    
        private bool IsDirectChildOfFolder(
        string assetPath,
        string folderPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return false;
    
            if (string.IsNullOrWhiteSpace(folderPath))
                return false;
    
    
            string normalizedAssetPath = NormalizeAssetPath(assetPath);
    
            string normalizedFolderPath =
                NormalizeAssetPath(folderPath)
                    .TrimEnd('/') + "/";
    
    
            if (!normalizedAssetPath.StartsWith(
                    normalizedFolderPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
    
    
            string remaining =
                normalizedAssetPath.Substring(
                    normalizedFolderPath.Length
                );
    
            return !remaining.Contains("/");
        }
    
        private void RegisterFoundImageAlias(
        string gamePath,
        string bundleAssetPath,
        string imageType)
        {
            AddLocation(
                gamePath,
                bundleAssetPath,
                typeof(Sprite)
            );
        }
    
        private void BuildFeatureAliases(
            string itemId,
            string objectRoot)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return;
            }
    
            string suffix =
                "_" +
                itemId +
                ".asset";
    
            Dictionary<string, string> featureNames =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    {
                        "dialoguetrigger",
                        "DialogueTrigger"
                    },

                    {
                        "changecolor",
                        "ChangeColor"
                    },

                    {
                        "maskthing",
                        "MaskThing"
                    },
                    {
                        "animationplayer",
                        "AnimationPlayer"
                    },
                    {
                        "foreachani",
                        "ForEachAni"
                    }
                };
    
            for (int i = 0; i < bundleAssets.Count; i++)
            {
                WotHCustomBundleAsset bundleAsset = bundleAssets[i];
    
                if (bundleAsset == null)
                    continue;
    
                if (bundleAsset.Asset == null)
                    continue;
    
                if (!IsInsideObjectRoot(
                    bundleAsset.AssetName,
                    objectRoot))
                {
                    continue;
                }
    
                string normalizedAssetName =
                NormalizeAssetPath(
                    bundleAsset.AssetName
                );
    
                if (normalizedAssetName.IndexOf(
                    "/scriptable/",
                    StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }
    
                string fileName =
                    Path.GetFileName(
                        bundleAsset.AssetName);
    
                if (string.IsNullOrEmpty(fileName))
                {
                    continue;
                }
    
                if (!fileName.EndsWith(
                        suffix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
    
                if (bundleAsset.Asset is HoldThingContainer)
                    continue;
    
                string fileNameWithoutExtension =
                    Path.GetFileNameWithoutExtension(
                        fileName);
    
                if (string.IsNullOrWhiteSpace(fileNameWithoutExtension))
                    continue;
    
                string itemSuffix = "_" + itemId;
    
                if (!fileNameWithoutExtension.EndsWith(
                        itemSuffix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
    
                string bundleFeatureName =
                    fileNameWithoutExtension.Substring(
                        0,
                        fileNameWithoutExtension.Length -
                        itemSuffix.Length);
    
                if (string.IsNullOrWhiteSpace(
                    bundleFeatureName))
                {
                    continue;
                }
    
                string gameFeatureName;
    
                if (!featureNames.TryGetValue(
                        bundleFeatureName,
                        out gameFeatureName))
                {
                    continue;
                }
    
                string gamePath =
                    "Assets/Scriptable/Features/" +
                    gameFeatureName +
                    "/" +
                    gameFeatureName +
                    "_" +
                    itemId +
                    ".asset";
    
                Type assetType = bundleAsset.AssetType;
    
                if (assetType == null)
                {
                    assetType = bundleAsset.Asset.GetType();
                }
    
                AddLocation(
                    gamePath,
                    bundleAsset.AssetName,
                    assetType);
    
                Debug.Log(
                    "[WotH Custom Item Locator] " +
                    "FEATURE alias: " +
                    gamePath +
                    " -> " +
                    bundleAsset.AssetName +
                    " | Type=" +
                    assetType.FullName);
            }
        }

        private void BuildAdditionalAssetAliases(string objectRoot)
        {
            if (string.IsNullOrWhiteSpace(objectRoot))
                return;

            string normalizedObjectRoot = NormalizeAssetPath(objectRoot);

            if (string.IsNullOrWhiteSpace(normalizedObjectRoot))
                return;


            Dictionary<string, string> folderMappings =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase)
                {
            {
                "/animation/animationclips/",
                "Assets/Animation/AnimationClips"
            },

            {
                "/animation/newitemanimation/",
                "Assets/Animation/NewItemAnimation"
            },

            {
                "/scriptable/anievent/",
                "Assets/Scriptable/AniEvent"
            },

            {
                "/scriptable/features/foreachani/",
                "Assets/Scriptable/Features/ForEachAni"
            },

            {
                "/scriptable/features/changecolor/",
                "Assets/Scriptable/Features/ChangeColor"
            },

            {
                "/scriptable/features/maskthing/",
                "Assets/Scriptable/Features/MaskThing"
            },

            {
                "/scriptable/features/animationplayer/",
                "Assets/Scriptable/Features/AnimationPlayer"
            },

            {
                "/textures/sprites/",
                "Assets/Textures/Sprites"
            },

            {
                "/textures/texture2ds/",
                "Assets/Textures/Texture2Ds"
            }
                };


            string normalizedRoot = normalizedObjectRoot.TrimEnd('/');


            for (int i = 0; i < bundleAssets.Count; i++)
            {
                WotHCustomBundleAsset bundleAsset = bundleAssets[i];

                if (bundleAsset == null)
                    continue;

                if (bundleAsset.Asset == null)
                    continue;

                string normalizedAssetName =
                    NormalizeAssetPath(
                        bundleAsset.AssetName
                    );

                if (string.IsNullOrWhiteSpace(normalizedAssetName))
                    continue;


                if (!IsInsideObjectRoot(
                    normalizedAssetName,
                    normalizedRoot))
                {
                    continue;
                }


                string relativeToObjectRoot =
                    normalizedAssetName.Substring(
                        normalizedRoot.Length
                    );


                foreach (KeyValuePair<string, string> mapping in folderMappings)
                {
                    string bundleFolder = mapping.Key;

                    string gameFolder = mapping.Value;


                    if (!relativeToObjectRoot.StartsWith(
                        bundleFolder,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }


                    string relativeAssetPath =
                        relativeToObjectRoot.Substring(
                            bundleFolder.Length
                        );

                    if (string.IsNullOrWhiteSpace(relativeAssetPath))
                        continue;

                    string gamePath =
                        gameFolder +
                        "/" +
                        relativeAssetPath;


                    Type assetType = bundleAsset.AssetType;

                    if (assetType == null)
                    {
                        assetType = bundleAsset.Asset.GetType();
                    }

                    AddLocation(
                        gamePath,
                        bundleAsset.AssetName,
                        assetType
                    );

                    break;
                }
            }
        }

        private void AddLocation(
            object key,
            string internalId,
            Type resourceType)
        {
            if (key == null)
                return;
    
            if (string.IsNullOrWhiteSpace(internalId))
                return;
    
            if (resourceType == null)
                return;
    
            List<IResourceLocation> locations;
    
            if (!locationsByKey.TryGetValue(
                key,
                out locations))
            {
                locations = new List<IResourceLocation>();
    
                locationsByKey.Add(
                    key,
                    locations
                );
            }
    
            for (int i = 0; i < locations.Count; i++)
            {
                IResourceLocation existing = locations[i];
    
                if (existing == null)
                    continue;
    
                if (string.Equals(
                    existing.InternalId,
                    internalId,
                    StringComparison.OrdinalIgnoreCase)
                    &&
                    existing.ResourceType ==
                    resourceType)
                {
                    return;
                }
            }
    
            ResourceLocationBase location =
                new ResourceLocationBase(
                    key.ToString(),
                    internalId,
                    provider.ProviderId,
                    resourceType
                );
    
            locations.Add(location);
        }
    
        public bool Locate(
            object key,
            Type type,
            out IList<IResourceLocation> locations)
        {
            locations = null;
    
            if (key == null)
                return false;
    
            List<IResourceLocation> allLocations;
    
            if (!locationsByKey.TryGetValue(
                key,
                out allLocations))
            {
                return false;
            }
    
            if (type == null)
            {
                locations = allLocations;
    
                return allLocations.Count > 0;
            }
    
            List<IResourceLocation> filtered = new List<IResourceLocation>();
    
            for (int i = 0; i < allLocations.Count; i++)
            {
                IResourceLocation location = allLocations[i];
    
                if (location == null)
                    continue;
    
                if (location.ResourceType == null)
                    continue;
    
                if (type.IsAssignableFrom(location.ResourceType))
                {
                    filtered.Add(
                        location
                    );
    
                    continue;
                }
    
                if (location.ResourceType ==
                    typeof(Sprite)
                    &&
                    type.IsAssignableFrom(
                        typeof(Sprite)))
                {
                    filtered.Add(
                        location
                    );
                }
            }
    
    
            if (filtered.Count == 0)
            {
                Debug.LogWarning(
                    "[WotH Custom Item Locator] LOCATE FAILED: " +
                    "Key=" +
                    key +
                    " | RequestedType=" +
                    (type == null ? "NULL" : type.FullName) +
                    " | AvailableLocations=" +
                    allLocations.Count
                );
    
                for (int i = 0; i < allLocations.Count; i++)
                {
                    IResourceLocation debugLocation = allLocations[i];
    
                    if (debugLocation == null)
                        continue;
    
                    Debug.LogWarning(
                        "[WotH Custom Item Locator]   Candidate: " +
                        debugLocation.PrimaryKey +
                        " | InternalId=" +
                        debugLocation.InternalId +
                        " | ResourceType=" +
                        (debugLocation.ResourceType == null
                            ? "NULL"
                            : debugLocation.ResourceType.FullName)
                    );
                }
    
                return false;
            }
    
    
            locations = filtered;
    
    
            return true;
        }
    
        private static void LoggerLog(
            string message)
        {
            Debug.Log(
                "[WotH Custom Item Locator] " +
                message
            );
        }
    }
}
