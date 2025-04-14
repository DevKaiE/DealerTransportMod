﻿using EmployeeExtender.Utils;
using HarmonyLib;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.PlayerScripts;
using MelonLoader;
using DealerSelfSupplySystem.DealerExtension;
using UnityEngine;
using Il2CppScheduleOne.Persistence;
using DealerSelfSupplySystem.Persistence;

[assembly: MelonInfo(typeof(DealerSelfSupplySystem.Core), "DealerSelfSupplySystem", "1.1.0", "KaiNoodles", null)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace DealerSelfSupplySystem
{
    public class Core : MelonMod
    {
        public static MelonLogger.Instance MelonLogger { get; private set; }
        public static DealerStorageManager DealerStorageManager { get; private set; }
        public static bool MainSceneLoaded { get; private set; } = false;
        public static Config Config { get; private set; }

        // Add a retry mechanism for dealer initialization
        private static bool dealersInitialized = false;

        public override void OnInitializeMelon()
        {
            MelonLogger = LoggerInstance;
            MelonLogger.Msg("DealerSelfSupplySystem initializing...");
            Config = new Config();
            DealerStorageManager = new DealerStorageManager();
            MelonLogger.Msg("Initialization complete.");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Main")
            {
                MainSceneLoaded = true;
                MelonLogger.Msg("Main scene loaded.");

                // Start a coroutine to initialize dealers with retry capability
                MelonCoroutines.Start(InitializeExtendedDealersWithRetry());
            }
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Main")
            {
                MainSceneLoaded = false;
                dealersInitialized = false;
                MelonLogger.Msg("Main scene unloaded. Cleaning up dealer storage manager...");
                DealerStorageManager.CleanUp();
            }
        }

        public override void OnLateUpdate()
        {
            if (!MainSceneLoaded || !dealersInitialized) return;
            DealerStorageManager.CheckDealerStorage();
        }

        private System.Collections.IEnumerator InitializeExtendedDealersWithRetry()
        {
            // Wait a bit to ensure everything is loaded
            yield return new WaitForSeconds(1f);

            int attempts = 0;
            int maxAttempts = 5;

            while (!dealersInitialized && attempts < maxAttempts)
            {
                attempts++;
                MelonLogger.Msg($"Attempting to initialize dealers (attempt {attempts}/{maxAttempts})...");

                List<Dealer> dealers = GameUtils.GetAllDealers();
                if (dealers.Count > 0)
                {
                    foreach (Dealer dealer in dealers)
                    {
                        if (dealer == null) continue;
                        DealerExtendedBrain dealerExtendedBrain = new DealerExtendedBrain(dealer);
                        DealerStorageManager.AddDealerExtendedBrain(dealerExtendedBrain);
                    }

                    dealersInitialized = true;
                    MelonLogger.Msg($"Successfully initialized {dealers.Count} dealers");
                }
                else
                {
                    MelonLogger.Msg($"No dealers found on attempt {attempts}, will retry in 3 seconds...");
                    yield return new WaitForSeconds(3f);
                }
            }

            if (!dealersInitialized)
            {
                MelonLogger.Error($"Failed to initialize dealers after {maxAttempts} attempts");
            }
        }

        public void InitializeExtendedDealers()
        {
            List<Dealer> dealers = GameUtils.GetAllDealers();

            if (dealers.Count > 0)
            {
                foreach (Dealer dealer in dealers)
                {
                    if (dealer == null) continue;
                    DealerExtendedBrain dealerExtendedBrain = new DealerExtendedBrain(dealer);
                    DealerStorageManager.AddDealerExtendedBrain(dealerExtendedBrain);
                }

                MelonLogger.Msg($"Successfully initialized {dealers.Count} dealers");
                dealersInitialized = true;
            }
            else
            {
                MelonLogger.Warning("No dealers found during initialization");
            }
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.F9))
            {
                SavePoint.SAVE_COOLDOWN = 0f;
            }
        }
    }
}