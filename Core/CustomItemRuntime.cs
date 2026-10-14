using BepInEx.Logging;
using Newtonsoft.Json;
using UnityEngine.AddressableAssets;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WotHCustomItemSDK
{
    internal sealed class CustomItemRuntime
    {
    private static CustomItemRuntime Instance;

    private static BepInEx.Logging.ManualLogSource Logger
    {
        get { return Plugin.LogSource; }
    }

    private bool customItemsInitialized;
    private readonly List<AssetBundle> loadedBundles = new List<AssetBundle>();
    private readonly List<WotHCustomBundleAsset> bundleAssets = new List<WotHCustomBundleAsset>();
    private readonly List<WotHCustomItemAsset> itemAssets = new List<WotHCustomItemAsset>();
    private readonly List<WotHLocalizationData> pendingLocalizations = new List<WotHLocalizationData>();
    private bool localizationApplied;
    private WotHCustomItemProvider provider;
    private WotHCustomItemLocator locator;
    private bool addressablesRegistered;

    public static bool IsInitialized
    {
        get { return Instance != null && Instance.customItemsInitialized; }
    }

    public static IList<WotHCustomItemAsset> CustomItems
    {
        get
        {
            if (Instance == null)
                return new List<WotHCustomItemAsset>();

            return Instance.itemAssets.AsReadOnly();
        }
    }

    internal static CustomItemRuntime Create()
    {
        if (Instance == null)
            Instance = new CustomItemRuntime();

        return Instance;
    }

    private CustomItemRuntime()
    {
    }

    internal void Tick()
    {
        if (!localizationApplied &&
            I2.Loc.LocalizationManager.Sources != null &&
            I2.Loc.LocalizationManager.Sources.Count > 0)
        {
            ApplyPendingLocalizations();
            localizationApplied = true;
        }
    }


    public static void InitializeCustomItemsForGame()
    {
        if (Instance == null)
        {
            Debug.LogError(
                "[WotH Custom Item SDK] " +
                "Instance is null."
            );

            return;
        }


        Instance.InitializeCustomItemsInternal();
    }

    private void InitializeCustomItemsInternal()
    {
        if (customItemsInitialized)
        {
            Logger.LogInfo(
                "Custom items already initialized. Skipping."
            );

            return;
        }

        try
        {
            LoadCustomBundles();

            if (loadedBundles.Count == 0)
            {
                Logger.LogWarning(
                    "No custom bundles were loaded."
                );

                customItemsInitialized =
                    true;

                return;
            }

            IndexAllBundleAssets();

            FindHoldThingContainers();
            ImportCustomLocalization();

            if (itemAssets.Count == 0)
            {
                Logger.LogWarning(
                    "No HoldThingContainer assets were found " +
                    "in custom bundles."
                );

                customItemsInitialized =
                    true;

                return;
            }

            RegisterAddressablesProviderAndLocator();

            customItemsInitialized =
                true;

            Logger.LogInfo(
                "Custom HoldThingContainer count: " +
                itemAssets.Count
            );

            Logger.LogInfo(
                "Indexed bundle asset count: " +
                bundleAssets.Count
            );
        }
        catch (Exception ex)
        {
            Logger.LogError(
                "CUSTOM ITEM INITIALIZATION FAILED:"
            );

            Logger.LogError(
                ex.ToString()
            );

            customItemsInitialized =
                false;
        }
    }

    private void LoadCustomBundles()
    {
        loadedBundles.Clear();

        IList<AssetBundle> bundles = PackageRegistry.Bundles;
        for (int i = 0; i < bundles.Count; i++)
        {
            AssetBundle bundle = bundles[i];
            if (bundle != null && !loadedBundles.Contains(bundle))
                loadedBundles.Add(bundle);
        }

        if (loadedBundles.Count == 0)
            Plugin.LogWarning("No custom AssetBundles with registered package metadata were found.");
        else
            Plugin.Log("Using " + loadedBundles.Count + " registered custom package AssetBundle(s).");
    }
    private void IndexAllBundleAssets()
    {
        bundleAssets.Clear();

        foreach (AssetBundle bundle in loadedBundles)
        {
            if (bundle == null)
                continue;

            string[] assetNames = bundle.GetAllAssetNames();

            foreach (string assetName in assetNames)
            {
                if (string.IsNullOrEmpty(assetName))
                    continue;

                try
                {
                    UnityEngine.Object asset =
                        bundle.LoadAsset(
                            assetName,
                            typeof(UnityEngine.Object)
                        );


                    if (asset == null)
                    {
                        Logger.LogWarning(
                            "Could not load asset for indexing: " +
                            assetName
                        );

                        continue;
                    }


                    string bundlePath;
                    AssetBundleRegistry.TryGetPath(bundle, out bundlePath);

                    string packageName;
                    string packageBundlePath;
                    if (PackageRegistry.TryGetByBundle(bundle, out packageName, out packageBundlePath))
                        bundlePath = packageBundlePath;

                    WotHCustomBundleAsset entry =
                        new WotHCustomBundleAsset
                        {
                            Bundle = bundle,
                            AssetName = assetName,
                            Asset = asset,
                            AssetType = asset.GetType(),
                            BundlePath = bundlePath,
                            PackageName = packageName
                        };


                    bundleAssets.Add(
                        entry
                    );


                    Logger.LogInfo(
                        "Indexed: " +
                        assetName +
                        " -> " +
                        asset.GetType().FullName
                    );
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(
                        "Could not index asset '" +
                        assetName +
                        "': " +
                        ex.Message
                    );
                }
            }
        }


        Logger.LogInfo(
            "Finished indexing bundles. " +
            "Total indexed assets: " +
            bundleAssets.Count
        );
    }
    private void FindHoldThingContainers()
    {
        itemAssets.Clear();

        foreach (WotHCustomBundleAsset bundleAsset in bundleAssets)
        {
            if (bundleAsset == null)
                continue;

            if (bundleAsset.Asset == null)
                continue;

            HoldThingContainer item = bundleAsset.Asset as HoldThingContainer;

            if (item == null)
                continue;

            WotHCustomItemAsset customAsset =
                new WotHCustomItemAsset
                {
                    Bundle = bundleAsset.Bundle,
                    AssetName = bundleAsset.AssetName,
                    Item = item,
                    BundlePath = bundleAsset.BundlePath,
                    PackageName = bundleAsset.PackageName
                };

            itemAssets.Add(
                customAsset
            );
        }


        Logger.LogInfo(
            "Finished scanning bundles."
        );


        Logger.LogInfo(
            "Found " +
            itemAssets.Count +
            " HoldThingContainer asset(s)."
        );
    }
    private void ImportCustomLocalization()
    {
        pendingLocalizations.Clear();

        int localizationCount = 0;

        for (int i = 0; i < bundleAssets.Count; i++)
        {
            WotHCustomBundleAsset bundleAsset =
                bundleAssets[i];

            if (bundleAsset == null ||
                bundleAsset.Asset == null)
            {
                continue;
            }

            TextAsset jsonAsset =
                bundleAsset.Asset as TextAsset;

            if (jsonAsset == null)
                continue;

            if (!bundleAsset.AssetName.EndsWith(
                    ".json",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string fileName =
                Path.GetFileName(bundleAsset.AssetName);

            if (!fileName.StartsWith(
                    "itemloc_",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            WotHLocalizationData localization;

            try
            {
                localization =
                    JsonConvert.DeserializeObject<WotHLocalizationData>(
                        jsonAsset.text
                    );
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    "[WotH Custom Localization] " +
                    "Failed to parse JSON: " +
                    bundleAsset.AssetName +
                    " | " +
                    ex
                );

                continue;
            }

            if (localization == null)
            {
                Logger.LogError(
                    "[WotH Custom Localization] " +
                    "JSON produced no localization data: " +
                    bundleAsset.AssetName
                );

                continue;
            }

            if (localization.id <= 0)
            {
                Logger.LogError(
                    "[WotH Custom Localization] " +
                    "Invalid Item ID in: " +
                    bundleAsset.AssetName
                );

                continue;
            }

            string itemId = localization.id.ToString();


            pendingLocalizations.Add(localization);


            string itemNameTerm = "ItemName/" + itemId;

            if (localization.itemNameTranslations != null)
            {
                for (int t = 0; t < localization.itemNameTranslations.Count; t++)
                {
                    WotHLocalizationData.TranslationData translation =
                        localization.itemNameTranslations[t];

                    if (translation == null)
                        continue;

                    if (string.IsNullOrWhiteSpace(translation.languageCode))
                        continue;
                }
            }

            string itemInfoTerm = "ItemInfo/" + itemId;

            if (localization.itemInfoTranslations != null)
            {
                for (int t = 0; t < localization.itemInfoTranslations.Count; t++)
                {
                    WotHLocalizationData.TranslationData translation =
                        localization.itemInfoTranslations[t];

                    if (translation == null)
                        continue;

                    if (string.IsNullOrWhiteSpace(translation.languageCode))
                        continue;
                }
            }

            localizationCount++;
        }

        Logger.LogInfo(
            "[WotH Custom Localization] " +
            "Localization scan finished. " +
            "Queued entries: " +
            localizationCount
        );
    }
    private void ApplyPendingLocalizations()
    {
        if (pendingLocalizations.Count == 0)
        {
            Logger.LogInfo(
                "[WotH Custom Localization] " +
                "No pending localizations to apply."
            );

            return;
        }

        Logger.LogInfo(
            "[WotH Custom Localization] " +
            "I2 LanguageSourceData is available. " +
            "Applying pending localizations..."
        );

        int appliedCount = 0;

        for (int i = 0; i < pendingLocalizations.Count; i++)
        {
            WotHLocalizationData localization = pendingLocalizations[i];

            if (localization == null)
                continue;

            string itemId = localization.id.ToString();

            string itemNameTerm = "ItemName/" + itemId;

            if (localization.itemNameTranslations != null)
            {
                for (int t = 0; t < localization.itemNameTranslations.Count; t++)
                {
                    WotHLocalizationData.TranslationData translation =
                        localization.itemNameTranslations[t];

                    if (translation == null)
                        continue;

                    if (string.IsNullOrWhiteSpace(translation.languageCode))
                        continue;

                    bool success =
                        LocalizationRuntime.AddOrUpdateTerm(
                            itemNameTerm,
                            translation.languageCode,
                            translation.translation ?? ""
                        );

                    if (success)
                    {
                        appliedCount++;
                    }
                    else
                    {
                        Logger.LogError(
                            "[WotH Custom Localization] " +
                            "Failed to apply Name: " +
                            itemNameTerm +
                            " | Language=" +
                            translation.languageCode
                        );
                    }
                }
            }

            string itemInfoTerm = "ItemInfo/" + itemId;

            if (localization.itemInfoTranslations != null)
            {
                for (int t = 0; t < localization.itemInfoTranslations.Count; t++)
                {
                    WotHLocalizationData.TranslationData translation =
                        localization.itemInfoTranslations[t];

                    if (translation == null)
                        continue;

                    if (string.IsNullOrWhiteSpace(translation.languageCode))
                        continue;

                    bool success =
                        LocalizationRuntime.AddOrUpdateTerm(
                            itemInfoTerm,
                            translation.languageCode,
                            translation.translation ?? ""
                        );

                    if (success)
                    {
                        appliedCount++;
                    }
                    else
                    {
                        Logger.LogError(
                            "[WotH Custom Localization] " +
                            "Failed to apply Info: " +
                            itemInfoTerm +
                            " | Language=" +
                            translation.languageCode
                        );
                    }
                }
            }
        }

        Logger.LogInfo(
            "[WotH Custom Localization] " +
            "Localization application finished. " +
            "Applied translations: " +
            appliedCount
        );

        pendingLocalizations.Clear();
    }
    private void RegisterAddressablesProviderAndLocator()
    {
        if (addressablesRegistered)
            return;

        Logger.LogInfo(
            "Registering custom Addressables provider..."
        );

        provider =
            new WotHCustomItemProvider(
                loadedBundles,
                bundleAssets
            );

        Addressables.ResourceManager
            .ResourceProviders
            .Add(provider);

        locator =
            new WotHCustomItemLocator(
                provider,
                itemAssets,
                bundleAssets
            );

        Addressables.AddResourceLocator(
            locator
        );

        addressablesRegistered = true;
    }
}
}
