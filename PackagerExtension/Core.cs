using EmployeeExtender.Utils;
using HarmonyLib;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.PlayerScripts;
using MelonLoader;
using DealerSelfSupplySystem;
using DealerSelfSupplySystem.DealerExtension;
using UnityEngine;

[assembly: MelonInfo(typeof(DealerSelfSupplySystem.Core), "DealerSelfSupplySystem", "1.0.0", "KaiNoodles", null)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace DealerSelfSupplySystem
{
    public class Core : MelonMod
    {
        // Fixed property declaration syntax
        public static MelonLogger.Instance MelonLogger { get; private set; }
        public static DealerStorageManager DealerStorageManager { get; private set; }
        public static bool MainSceneLoaded { get; private set; } = false;
        public static Config Config { get; private set; }
        public override void OnInitializeMelon()
        {
            MelonLogger = LoggerInstance;
            MelonLogger.Msg("Initialization.");
            Config = new Config();
            DealerStorageManager = new DealerStorageManager();
            
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Main")
            {
                MainSceneLoaded = true;
                MelonLogger.Msg("Main scene loaded.");
                InitializeExtendedDealers();
            }
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Main")
            {
                MainSceneLoaded = false;
                MelonLogger.Msg("Main scene unloaded.");
                DealerStorageManager.CleanUp();
            }
        }
        public override void OnLateUpdate()
        {
            if (!MainSceneLoaded) return;         
            DealerStorageManager.CheckDealerStorage();
        }

        public void InitializeExtendedDealers()
        {
            List<Dealer> dealers = GameUtils.GetAllDealers();
            foreach (Dealer dealer in dealers)
            {
                if (dealer == null) continue;
                DealerExtendedBrain dealerExtendedBrain = new DealerExtendedBrain(dealer);
                DealerStorageManager.AddDealerExtendedBrain(dealerExtendedBrain);
            }
        }
    }
}