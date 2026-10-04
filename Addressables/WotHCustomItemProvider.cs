using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace WotHCustomItemSDK
{
    public class WotHCustomItemProvider : IResourceProvider
    {
        public string ProviderId => "WotH.CustomItemProvider";
    
        public ProviderBehaviourFlags BehaviourFlags => ProviderBehaviourFlags.None;
    
        private readonly List<AssetBundle> bundles;
    
        private readonly List<WotHCustomBundleAsset> bundleAssets;
    
        public WotHCustomItemProvider(
            List<AssetBundle> bundles,
            List<WotHCustomBundleAsset> bundleAssets)
        {
            this.bundles = bundles;
    
            this.bundleAssets = bundleAssets;
        }
    
        public bool CanProvide(
            Type type,
            IResourceLocation location)
        {
            if (location == null)
                return false;
    
    
            if (location.ResourceType == null)
                return false;
    
            if (type.IsAssignableFrom(location.ResourceType))
                return true;
    
            if (location.ResourceType == typeof(Sprite))
            {
                if (type.IsAssignableFrom(typeof(Sprite)))
                {
                    return true;
                }
            }
    
    
            return false;
        }
    
        public Type GetDefaultType(IResourceLocation location)
        {
            if (location == null)
                return typeof(UnityEngine.Object);
    
            if (location.ResourceType == null)
                return typeof(UnityEngine.Object);
    
            return location.ResourceType;
        }
    
        public void Provide(ProvideHandle provideHandle)
        {
            IResourceLocation location = provideHandle.Location;
    
            if (location == null)
            {
                provideHandle.Complete<UnityEngine.Object>(
                    null,
                    false,
                    new Exception(
                        "WotH custom provider received " +
                        "a null location."
                    )
                );
    
                return;
            }
    
            string internalAssetName = location.InternalId;
    
            Type requestedType = provideHandle.Type;
    
            WotHCustomBundleAsset bundleAsset =
                FindBundleAsset(
                    internalAssetName
                );
    
            if (bundleAsset == null)
            {
                provideHandle.Complete<UnityEngine.Object>(
                    null,
                    false,
                    new Exception(
                        "Custom bundle asset not found: " +
                        internalAssetName
                    )
                );
    
                return;
            }
    
    
            if (bundleAsset.Bundle == null)
            {
                provideHandle.Complete<UnityEngine.Object>(
                    null,
                    false,
                    new Exception(
                        "Bundle is null for asset: " +
                        internalAssetName
                    )
                );
    
                return;
            }
    
            if (requestedType == typeof(Sprite))
            {
                Sprite sprite =
                    LoadSprite(
                        bundleAsset
                    );
    
                if (sprite != null)
                {
                    provideHandle.Complete(
                        sprite,
                        true,
                        null
                    );
    
                    return;
                }
    
                provideHandle.Complete<UnityEngine.Object>(
                    null,
                    false,
                    new Exception(
                        "Could not load Sprite from custom " +
                        "bundle asset: " +
                        internalAssetName
                    )
                );
    
                return;
            }
    
            try
            {
                UnityEngine.Object result =
                    bundleAsset.Bundle.LoadAsset(
                        bundleAsset.AssetName,
                        requestedType
                    );
    
                if (result == null)
                {
                    provideHandle.Complete<UnityEngine.Object>(
                        null,
                        false,
                        new Exception(
                            "AssetBundle.LoadAsset returned null. " +
                            "Asset: " +
                            bundleAsset.AssetName +
                            " RequestedType: " +
                            requestedType
                        )
                    );
    
                    return;
                }
    
                provideHandle.Complete(
                    result,
                    true,
                    null
                );
            }
            catch (Exception ex)
            {
                provideHandle.Complete<UnityEngine.Object>(
                    null,
                    false,
                    ex
                );
            }
        }
    
        private Sprite LoadSprite(WotHCustomBundleAsset bundleAsset)
        {
            if (bundleAsset == null)
                return null;
    
            if (bundleAsset.Bundle == null)
                return null;
    
    
            try
            {
                Sprite sprite =
                    bundleAsset.Bundle.LoadAsset<Sprite>(
                        bundleAsset.AssetName
                    );
    
    
                if (sprite != null)
                    return sprite;
            }
            catch
            {
            }
    
            try
            {
                Texture2D texture =
                    bundleAsset.Bundle.LoadAsset<Texture2D>(
                        bundleAsset.AssetName
                    );
    
    
                if (texture == null)
                    return null;
    
    
                return Sprite.Create(
                    texture,
                    new Rect(
                        0f,
                        0f,
                        texture.width,
                        texture.height
                    ),
                    new Vector2(
                        0.5f,
                        0.5f
                    ),
                    100f
                );
            }
            catch
            {
                return null;
            }
        }
    
        private WotHCustomBundleAsset FindBundleAsset(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
                return null;
    
            for (int i = 0; i < bundleAssets.Count; i++)
            {
                WotHCustomBundleAsset asset = bundleAssets[i];
    
                if (asset == null)
                    continue;
    
                if (string.Equals(
                    asset.AssetName,
                    assetName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return asset;
                }
            }
    
            return null;
        }
    
        public void Release(
            IResourceLocation location,
            object asset)
        {
        }
    }
}
