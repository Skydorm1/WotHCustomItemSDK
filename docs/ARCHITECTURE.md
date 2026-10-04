# Architecture notes

The SDK deliberately separates lifecycle orchestration from reusable services.

`Plugin` is the single BepInEx entry point. It owns Harmony and forwards Unity's `Update`/`OnGUI` to small services. It does not contain item discovery or Addressables logic.

`CustomItemRuntime` performs the one-time import triggered by `HoldThingDict.UpdateDataInfoAsync`. It takes already loaded bundles from `AssetBundleRegistry`, indexes their assets, finds `HoldThingContainer` objects, queues localization data and installs the custom Addressables provider + locator.

`CustomItemService` contains operations that can be reused by other mods. Unlocking is intentionally here instead of in the UI.

`AssetBundleRegistry` is the shared foundation: PackageRegistry, CustomItemRuntime and the bag sprite loader all resolve through it. The same canonical bundle path therefore maps to a single `AssetBundle` instance in the SDK.

`WotHCustomItemProvider` and `WotHCustomItemLocator` are kept independent because they implement two different Addressables contracts. The provider loads the real AssetBundle asset; the locator maps the game's ItemSOC and related asset paths to the provider.

The bag category patch is isolated under `Bag/`. It can therefore be replaced or disabled later without changing the custom-item importer itself.
