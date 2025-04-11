using HarmonyLib;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.Storage;
using Il2CppScheduleOne.UI;
using PackagerExtension;
using PackagerExtension.DealerExtension;

namespace PackagerExtension.Patches
{
    [HarmonyPatch(typeof(StorageMenu), nameof(StorageMenu.Open), new Type[] { typeof(StorageEntity) })]
    public static class StorageEntityOpenedPatch
    {
        public static void Postfix(StorageMenu __instance, StorageEntity entity)
        {
            Core.MelonLogger.Msg($"StorageEntity opened: {entity.name}");
            Dealer assignedDealer = Core.DealerStorageManager.GetDealerFromStorage(entity);
            DealerExtensionUI extensionUI = Core.DealerStorageManager.GetDealerExtensionUI(entity);
            if (extensionUI == null)
            {
                extensionUI = new DealerExtensionUI(__instance, entity);
                Core.DealerStorageManager.SetDealerExtensionUI(entity, extensionUI);
                Core.MelonLogger.Msg($"Created DealerExtensionUI for {entity.name}");
            }
            extensionUI.ToggleUI(true);
        }
    }

    [HarmonyPatch(typeof(StorageMenu), nameof(StorageMenu.CloseMenu))]
    public static class StorageEntityClosedPatch
    {
        public static void Postfix(StorageMenu __instance)
        {
            Core.MelonLogger.Msg($"StorageEntity closed: {__instance.name}");
            StorageEntity entity = Core.DealerStorageManager.GetStorageMenu(__instance);
            if (entity == null) return;
            DealerExtensionUI extensionUI = Core.DealerStorageManager.GetDealerExtensionUI(entity);
            Core.MelonLogger.Msg($"Removing DealerExtensionUI for {entity.name}");
            if (extensionUI == null) return;
            Core.MelonLogger.Msg($"Closing DealerExtensionUI for {entity.name}");
            extensionUI.ToggleUI(false);

        }
    }

    [HarmonyPatch(typeof(StorageEntity), nameof(StorageEntity.OnClosed))]
    public static class StorageEntityClosedPatch2
    {
        public static void Postfix(StorageEntity __instance)
        {
            
            DealerExtensionUI extensionUI = Core.DealerStorageManager.GetDealerExtensionUI(__instance);
            Core.MelonLogger.Msg($"Removing DealerExtensionUI for {__instance.name}");
            if (extensionUI == null) return;
            Core.MelonLogger.Msg($"Closing DealerExtensionUI for {__instance.name}");
            extensionUI.ToggleUI(false);

        }
    }
}
