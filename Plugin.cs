using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.IO;
using UnityEngine;

namespace WotHCustomItemSDK
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.skydorm.woth.customitemsdk";
        public const string PluginName = "WotH Custom Item SDK";
        public const string PluginVersion = "1.0.0";

        internal static Plugin Instance;
        internal static ConfigEntry<string> BundleFile;
        internal static ConfigEntry<bool> LogVerbose;
        internal static ConfigEntry<string> ModPackDirectory;
        internal static ConfigEntry<bool> EnableCustomBagCategories;
        internal static ConfigEntry<bool> EnableUnlockerWindow;

        internal static ManualLogSource LogSource
        {
            get { return Instance == null ? null : Instance.Logger; }
        }

        private Harmony _harmony;
        private CustomItemRuntime _runtime;
        private CustomItemUnlockWindow _unlockWindow;

        private void Awake()
        {
            Instance = this;

            BundleFile = Config.Bind(
                "Assets",
                "AssetBundle",
                "UnLimitBagSprites.bundle",
                "Sprite AssetBundle used by the custom bag category UI. Relative paths are resolved from the SDK plugin folder."
            );

            LogVerbose = Config.Bind(
                "Debug",
                "VerboseLogging",
                true,
                "Enable verbose runtime diagnostics."
            );

            ModPackDirectory = Config.Bind(
                "Assets",
                "ModPackDirectory",
                "WotHCustomItems",
                "Folder below BepInEx/plugins containing custom item AssetBundles."
            );

            EnableCustomBagCategories = Config.Bind(
                "Features",
                "EnableCustomBagCategories",
                true,
                "Adds the custom item and custom package bag tabs."
            );

            EnableUnlockerWindow = Config.Bind(
                "Features",
                "EnableUnlockerWindow",
                true,
                "Enable the F2 debug unlock window."
            );

            try
            {
                _harmony = new Harmony(PluginGuid);

                PackageRegistry.Initialize();

                _runtime = CustomItemRuntime.Create();

                CustomItemGameHooks.Apply(_harmony);

                if (EnableCustomBagCategories.Value)
                {
                    CustomBagCategoryPatch.Initialize();
                    CustomBagCategoryPatch.Apply(_harmony);
                }

                _unlockWindow = new CustomItemUnlockWindow();

                Log("SDK initialized. Shared AssetBundle registry is active.");
            }
            catch (Exception ex)
            {
                LogError("Failed to initialize: " + ex);
                throw;
            }
        }

        private void Update()
        {
            if (_runtime != null)
                _runtime.Tick();

            if (EnableUnlockerWindow.Value && _unlockWindow != null)
                _unlockWindow.Tick();
        }

        private void OnGUI()
        {
            if (EnableUnlockerWindow.Value && _unlockWindow != null)
                _unlockWindow.Draw();
        }

        private void OnDestroy()
        {
            try
            {
                if (_harmony != null)
                    _harmony.UnpatchSelf();
            }
            catch (Exception ex)
            {
                LogError("Failed to unpatch Harmony: " + ex);
            }

            AssetBundleRegistry.Shutdown(false);
            Instance = null;
        }

        internal static string GetPluginDirectory()
        {
            string assemblyPath = typeof(Plugin).Assembly.Location;
            if (!string.IsNullOrWhiteSpace(assemblyPath))
            {
                string directory = Path.GetDirectoryName(assemblyPath);
                if (!string.IsNullOrWhiteSpace(directory))
                    return Path.GetFullPath(directory);
            }

            return Paths.PluginPath;
        }

        internal static string ResolveBundlePath(string configuredPath)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
                configuredPath = "UnLimitBagSprites.bundle";

            if (Path.IsPathRooted(configuredPath))
                return Path.GetFullPath(configuredPath);

            string pluginDirectory = GetPluginDirectory();
            string bundleName = Path.GetFileName(configuredPath);
            string[] candidates =
            {
                Path.Combine(pluginDirectory, configuredPath),
                Path.Combine(pluginDirectory, "UnLimitBagBepInExMod", bundleName),
                Path.Combine(Paths.PluginPath, "UnLimitBagBepInExMod", bundleName),
                Path.Combine(Paths.PluginPath, bundleName)
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                if (File.Exists(candidates[i]))
                    return Path.GetFullPath(candidates[i]);
            }

            if (Directory.Exists(pluginDirectory))
            {
                string[] matches = Directory.GetFiles(
                    pluginDirectory,
                    bundleName,
                    SearchOption.AllDirectories);
                Array.Sort(matches, StringComparer.OrdinalIgnoreCase);
                if (matches.Length > 0)
                    return Path.GetFullPath(matches[0]);
            }

            return Path.Combine(pluginDirectory, configuredPath);
        }

        internal static string ResolveModPackDirectory()
        {
            string configured = ModPackDirectory == null || string.IsNullOrWhiteSpace(ModPackDirectory.Value)
                ? "WotHCustomItems"
                : ModPackDirectory.Value;

            if (Path.IsPathRooted(configured))
                return Path.GetFullPath(configured);

            string configuredPath = Path.Combine(Paths.PluginPath, configured);
            return Directory.Exists(configuredPath) ? configuredPath : Paths.PluginPath;
        }

        internal static void Log(string message)
        {
            if (Instance == null)
            {
                Debug.Log("[WotH Custom Item SDK] " + message);
                return;
            }

            Instance.Logger.LogInfo(message);
        }

        internal static void LogWarning(string message)
        {
            if (Instance == null)
            {
                Debug.LogWarning("[WotH Custom Item SDK] " + message);
                return;
            }

            Instance.Logger.LogWarning(message);
        }

        internal static void LogError(string message)
        {
            if (Instance == null)
            {
                Debug.LogError("[WotH Custom Item SDK] " + message);
                return;
            }

            Instance.Logger.LogError(message);
        }
    }
}
