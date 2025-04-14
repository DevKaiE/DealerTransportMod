using HarmonyLib;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.Persistence.Datas;
using DealerSelfSupplySystem.Persistence;

namespace DealerSelfSupplySystem.Patches
{
    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.Save), new Type[] { typeof(string) })]
    public static class SavePatch
    {
        public static void Postfix(SaveManager __instance, string saveFolderPath)
        {
            DealerSaveDataManager.SaveData(saveFolderPath);
            Core.MelonLogger.Msg($"Saved dealer storage data for save at {saveFolderPath}");
        }
    }

    [HarmonyPatch(typeof(LoadManager), nameof(LoadManager.StartGame))]
    public static class LoadStartPatch
    {
        public static void Postfix(LoadManager __instance, SaveInfo info, bool allowLoadStacking)
        {
            DealerSaveDataManager.LoadData(info.SavePath);
            Core.MelonLogger.Msg($"Loaded dealer storage data from {info.SavePath}");
        }
    }
}