using HarmonyLib;

namespace WotHCustomItemSDK
{
    internal static class CustomItemGameHooks
    {
        internal static void Apply(Harmony harmony)
        {
            System.Reflection.MethodInfo target =
                AccessTools.Method(typeof(HoldThingDict), "UpdateDataInfoAsync");

            if (target == null)
            {
                Plugin.LogError("HoldThingDict.UpdateDataInfoAsync was not found.");
                return;
            }

            harmony.Patch(
                target,
                prefix: new HarmonyMethod(typeof(CustomItemGameHooks), nameof(BeforeHoldThingDictUpdate))
            );
        }

        private static void BeforeHoldThingDictUpdate()
        {
            CustomItemRuntime.InitializeCustomItemsForGame();
        }
    }
}
