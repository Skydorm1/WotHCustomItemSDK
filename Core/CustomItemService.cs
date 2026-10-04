using System;
using System.Collections.Generic;

namespace WotHCustomItemSDK
{
    public static class CustomItemService
    {
        public static void UnlockAllCustomItems()
        {
            IList<WotHCustomItemAsset> items = CustomItemRuntime.CustomItems;
            Plugin.Log("Unlocking all custom items...");

            for (int i = 0; i < items.Count; i++)
            {
                WotHCustomItemAsset itemAsset = items[i];
                if (itemAsset == null || itemAsset.Item == null || itemAsset.Item.HoldThingData == null)
                    continue;

                string itemId = itemAsset.Item.HoldThingData.ID;
                if (string.IsNullOrWhiteSpace(itemId))
                    continue;

                UnlockItem(itemId);
            }

            Plugin.Log("All custom items unlock pass completed.");
        }

        public static bool UnlockItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                Plugin.LogWarning("Unlock Item failed: Item ID is empty.");
                return false;
            }

            itemId = itemId.Trim();

            try
            {
                if (!HoldThingDict.ThingIdToData.ContainsKey(itemId))
                {
                    Plugin.LogError("Item '" + itemId + "' was NOT found in HoldThingDict.");
                    return false;
                }

                HoldThingMainSaveData saveData = StageSave.GetThingClearSaveData(itemId);
                if (saveData == null)
                {
                    Plugin.LogError("StageSave.GetThingClearSaveData returned null for item '" + itemId + "'.");
                    return false;
                }

                ManagerBase<BagManager>.Instance.AddThing(
                    saveData,
                    1,
                    view: false,
                    true);

                Plugin.Log("Successfully unlocked item: " + itemId);
                return true;
            }
            catch (Exception ex)
            {
                Plugin.LogError("Failed to unlock item '" + itemId + "': " + ex);
                return false;
            }
        }
    }
}
