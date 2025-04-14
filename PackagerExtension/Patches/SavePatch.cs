using HarmonyLib;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.Persistence.Datas;
using DealerSelfSupplySystem;

namespace DealerSelfSupplySystem.Patches
{
    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.Save), new Type[] { typeof(string) })]
    public static class SavePatch
    {
        public static void Postfix(SaveManager __instance, string saveFolderPath)
        {
            Core.DealerStorageManager.SaveDealerStorageData(saveFolderPath);
            Core.MelonLogger.Msg($"Saved dealer storage data to {saveFolderPath}");
        }
    }

    [HarmonyPatch(typeof(LoadManager), nameof(LoadManager.StartGame))]
    public static class LoadStartPatch
    {
        public static void Postfix(LoadManager __instance, SaveInfo info, bool allowLoadStacking)
        {
            Core.DealerStorageManager.LoadDealerStorageData(info.SavePath);
            Core.MelonLogger.Msg($"Loaded dealer storage data from {info.SavePath}");
        }
    }
}