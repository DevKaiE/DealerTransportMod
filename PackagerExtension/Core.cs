using HarmonyLib;
using Il2CppScheduleOne.PlayerScripts;
using MelonLoader;
using PackagerExtension;
using PackagerExtension.DealerExtension;
using UnityEngine;

[assembly: MelonInfo(typeof(PackagerExtension.Core), "PackagerExtension", "1.0.0", "KaiNoodles", null)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace PackagerExtension
{
    public class Core : MelonMod
    {
        // Fixed property declaration syntax
        public static MelonLogger.Instance MelonLogger { get; private set; }
        public static DealerStorageManager DealerStorageManager { get; private set; }
        public static bool MainSceneLoaded { get; private set; } = false;
        public override void OnInitializeMelon()
        {
            MelonLogger = LoggerInstance;
            MelonLogger.Msg("Initialization.");
            DealerStorageManager = new DealerStorageManager();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Main")
            {
                MainSceneLoaded = true;
                MelonLogger.Msg("Main scene loaded.");
            }
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Main")
            {
                MainSceneLoaded = false;
                MelonLogger.Msg("Main scene unloaded.");
            }
        }
        public override void OnLateUpdate()
        {
            if (!MainSceneLoaded) return;         
            DealerStorageManager.CheckDealerStorage();
        }
    }
}