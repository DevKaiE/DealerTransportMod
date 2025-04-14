using System;
using System.IO;
using System.Collections.Generic;
using Il2CppNewtonsoft.Json;
using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.Storage;
using DealerSelfSupplySystem.DealerExtension;

namespace DealerSelfSupplySystem.Persistence
{
    [Serializable]
    public class DealerStorageAssignment
    {
        // Storage information
        public string StorageName { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }

        // Dealer information
        public string DealerName { get; set; }
        public string DealerFullName { get; set; } // Store both name and fullName for more reliable matching

        [System.Text.Json.Serialization.JsonIgnore]
        public Vector3 StorageEntityPosition
        {
            get { return new Vector3(PosX, PosY, PosZ); }
            set
            {
                PosX = value.x;
                PosY = value.y;
                PosZ = value.z;
            }
        }
    }

    public class DealerSaveDataManager
    {
        private const string MOD_FOLDER_NAME = "DealerSelfSupplySystem";
        private const float POSITION_MATCH_THRESHOLD = 10.0f; // Much more forgiving distance threshold

        public static void SaveData(string gameSaveFolderPath)
        {
            try
            {
                Core.MelonLogger.Msg($"Saving dealer storage assignments for save at: {gameSaveFolderPath}");

                List<DealerStorageAssignment> assignments = new List<DealerStorageAssignment>();

                foreach (var pair in Core.DealerStorageManager._dealerStorageDictionary)
                {
                    if (pair.Key != null && pair.Value != null)
                    {
                        var storage = pair.Key;
                        var dealer = pair.Value;

                        if (storage != null && dealer?.Dealer != null)
                        {
                            var assignment = new DealerStorageAssignment
                            {
                                StorageName = storage.name,
                                StorageEntityPosition = storage.transform.position,
                                DealerName = dealer.Dealer.name,
                                DealerFullName = dealer.Dealer.fullName
                            };

                            Core.MelonLogger.Msg($"Saving assignment: {dealer.Dealer.fullName} -> Storage '{storage.name}' at {storage.transform.position}");
                            assignments.Add(assignment);
                        }
                    }
                }

                string modSavesFolder = CreateModSaveFolder(gameSaveFolderPath);
                string saveIdentifier = Path.GetFileName(gameSaveFolderPath);
                string saveFilePath = Path.Combine(modSavesFolder, $"{saveIdentifier}_dealer_storage_data.json");

                string json = System.Text.Json.JsonSerializer.Serialize(assignments,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(saveFilePath, json);
                Core.MelonLogger.Msg($"Successfully saved {assignments.Count} dealer assignments to {saveFilePath}");
            }
            catch (Exception ex)
            {
                Core.MelonLogger.Error($"Failed to save dealer storage assignments: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public static void LoadData(string gameSaveFolderPath)
        {
            try
            {
                Core.MelonLogger.Msg($"Loading dealer storage assignments for save at: {gameSaveFolderPath}");

                string modSavesFolder = GetModSaveFolder(gameSaveFolderPath);
                string saveIdentifier = Path.GetFileName(gameSaveFolderPath);
                string saveFilePath = Path.Combine(modSavesFolder, $"{saveIdentifier}_dealer_storage_data.json");

                if (!File.Exists(saveFilePath))
                {
                    Core.MelonLogger.Msg($"No dealer storage assignments file found at {saveFilePath}");
                    return;
                }

                string json = File.ReadAllText(saveFilePath);

                if (string.IsNullOrEmpty(json))
                {
                    Core.MelonLogger.Msg("Save file is empty");
                    return;
                }

                List<DealerStorageAssignment> assignments = null;
                try
                {
                    assignments = System.Text.Json.JsonSerializer.Deserialize<List<DealerStorageAssignment>>(json);
                }
                catch (Exception jsonEx)
                {
                    Core.MelonLogger.Error($"Failed to deserialize JSON: {jsonEx.Message}");
                    return;
                }

                if (assignments == null || assignments.Count == 0)
                {
                    Core.MelonLogger.Warning("Dealer storage data was invalid or empty");
                    return;
                }

                Core.MelonLogger.Msg($"Loading {assignments.Count} dealer storage assignments");

                // Use a longer delay to ensure all objects are loaded
                MelonCoroutines.Start(DelayedRestoreDealerAssignments(assignments));
            }
            catch (Exception ex)
            {
                Core.MelonLogger.Error($"Failed to load dealer storage assignments: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // Add more delay before restoration
        private static System.Collections.IEnumerator DelayedRestoreDealerAssignments(List<DealerStorageAssignment> assignments)
        {
            // Wait longer for game to initialize properly
            yield return new WaitForSeconds(1f);

            // Wait for main scene to be loaded
            int maxAttempts = 10;
            int attempts = 0;

            while (!Core.MainSceneLoaded && attempts < maxAttempts)
            {
                //Core.MelonLogger.Msg($"Waiting for main scene to load... attempt {attempts + 1}/{maxAttempts}");
                yield return new WaitForSeconds(3f);
                attempts++;
            }

            // Additional delay to ensure entities are loaded
            //Core.MelonLogger.Msg("Main scene loaded. Waiting for entities to initialize...");
            yield return new WaitForSeconds(1f);

            // Then attempt to restore
            yield return RestoreDealerAssignments(assignments);
        }

        private static System.Collections.IEnumerator RestoreDealerAssignments(List<DealerStorageAssignment> assignments)
        {
            int restoredCount = 0;

            // Get all dealers and storage entities
            List<Dealer> allDealers = DealerSelfSupplySystem.Utils.GameUtils.GetRecruitedDealers();
            if (allDealers.Count == 0)
            {
                //Core.MelonLogger.Warning("No dealers found, will retry in 5 seconds");
                yield return new WaitForSeconds(5f);
                allDealers = DealerSelfSupplySystem.Utils.GameUtils.GetRecruitedDealers();
                if (allDealers.Count == 0)
                {
                    //Core.MelonLogger.Error("Still no dealers found after retry, assignment restoration failed");
                    yield break;
                }
            }

            List<StorageEntity> allStorages = FindAllStorageEntities();
            Core.MelonLogger.Msg($"Found {allStorages.Count} storage entities");

            // Try to restore assignments with multiple retry attempts
            for (int retryRound = 0; retryRound < 3; retryRound++)
            {
                foreach (var assignment in assignments)
                {
                    // Skip already restored assignments
                    if (IsAssignmentAlreadyRestored(assignment, allDealers, allStorages))
                        continue;

                    //Core.MelonLogger.Msg($"Trying to restore assignment: {assignment.DealerFullName} -> {assignment.StorageName} at {assignment.StorageEntityPosition}");

                    // Find matching dealer
                    Dealer matchedDealer = FindMatchingDealer(allDealers, assignment);
                    if (matchedDealer == null)
                    {
                        Core.MelonLogger.Warning($"Could not find dealer with name: {assignment.DealerFullName}");
                        continue;
                    }

                    // Find matching storage
                    StorageEntity matchedStorage = FindMatchingStorage(allStorages, assignment);
                    if (matchedStorage == null)
                    {
                        Core.MelonLogger.Warning($"Could not find matching storage for {assignment.StorageName} near position {assignment.StorageEntityPosition}");
                        continue;
                    }

                    // Get or create dealer brain
                    DealerExtendedBrain dealerBrain = Core.DealerStorageManager.GetAllDealersExtendedBrain()
                                                      .FirstOrDefault(d => d.Dealer == matchedDealer);
                    if (dealerBrain == null)
                    {
                        dealerBrain = new DealerExtendedBrain(matchedDealer);
                        Core.DealerStorageManager.AddDealerExtendedBrain(dealerBrain);
                    }

                    // Restore the assignment
                    bool success = Core.DealerStorageManager.SetDealerToStorage(matchedStorage, dealerBrain);
                    if (success)
                    {
                        restoredCount++;
                        Core.MelonLogger.Msg($"Successfully restored assignment: {matchedDealer.fullName} -> {matchedStorage.name}");
                    }
                }

                // If we restored all assignments, no need for more retries
                if (restoredCount == assignments.Count)
                    break;

                // Wait between retry rounds
                yield return new WaitForSeconds(5f);
            }

            Core.MelonLogger.Msg($"Successfully restored {restoredCount}/{assignments.Count} dealer-storage assignments");
        }

        private static bool IsAssignmentAlreadyRestored(DealerStorageAssignment assignment, List<Dealer> dealers, List<StorageEntity> storages)
        {
            Dealer dealer = FindMatchingDealer(dealers, assignment);
            StorageEntity storage = FindMatchingStorage(storages, assignment);

            if (dealer == null || storage == null)
                return false;

            // Check if this dealer is already assigned to this storage
            DealerExtendedBrain brain = Core.DealerStorageManager.GetAllDealersExtendedBrain()
                                        .FirstOrDefault(d => d.Dealer == dealer);

            if (brain == null)
                return false;

            StorageEntity assignedStorage = Core.DealerStorageManager.GetAssignedStorageForDealer(brain);
            return assignedStorage == storage;
        }

        private static Dealer FindMatchingDealer(List<Dealer> dealers, DealerStorageAssignment assignment)
        {
            // Try finding by full name (case insensitive)
            Dealer nameMatchedDealer = dealers.FirstOrDefault(d =>
                string.Equals(d.fullName, assignment.DealerFullName, StringComparison.OrdinalIgnoreCase));

            if (nameMatchedDealer != null)
            {
                return nameMatchedDealer;
            }

            // Try finding by object name
            Dealer objectNameMatch = dealers.FirstOrDefault(d =>
                string.Equals(d.name, assignment.DealerName, StringComparison.OrdinalIgnoreCase));

            if (objectNameMatch != null)
            {
                return objectNameMatch;
            }

            // Try partial name match as last resort
            return dealers.FirstOrDefault(d =>
                !string.IsNullOrEmpty(d.fullName) &&
                !string.IsNullOrEmpty(assignment.DealerFullName) &&
                (d.fullName.Contains(assignment.DealerFullName) || assignment.DealerFullName.Contains(d.fullName)));
        }

        private static StorageEntity FindMatchingStorage(List<StorageEntity> storages, DealerStorageAssignment assignment)
        {
            Vector3 targetPosition = assignment.StorageEntityPosition;

            // Try finding by exact name match
            var nameMatches = storages.Where(s => s.name == assignment.StorageName).ToList();
            if (nameMatches.Count == 1)
            {
                return nameMatches[0];
            }
            else if (nameMatches.Count > 1)
            {
                // If multiple name matches, find closest one by position
                return FindClosestStorage(nameMatches, targetPosition);
            }

            // Last resort: find any storage near the saved position
            return FindClosestStorage(storages, targetPosition, POSITION_MATCH_THRESHOLD);
        }

        private static StorageEntity FindClosestStorage(List<StorageEntity> storages, Vector3 position, float maxDistance = float.MaxValue)
        {
            StorageEntity closest = null;
            float closestDistance = maxDistance;

            foreach (var storage in storages)
            {
                float distance = Vector3.Distance(storage.transform.position, position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = storage;
                }
            }

            return closest;
        }

        private static List<StorageEntity> FindAllStorageEntities()
        {
            try
            {
                // Find all storage entities in the scene
                return UnityEngine.GameObject.FindObjectsOfType<StorageEntity>().ToList();
            }
            catch (Exception ex)
            {
                Core.MelonLogger.Error($"Error finding storage entities: {ex.Message}");
                return new List<StorageEntity>();
            }
        }

        private static string CreateModSaveFolder(string gameSaveFolderPath)
        {
            try
            {
                string saveRootFolder = Path.GetDirectoryName(gameSaveFolderPath);
                string modSavesFolder = Path.Combine(saveRootFolder, MOD_FOLDER_NAME);

                if (!Directory.Exists(modSavesFolder))
                {
                    Directory.CreateDirectory(modSavesFolder);
                    Core.MelonLogger.Msg($"Created mod save folder at {modSavesFolder}");
                }

                return modSavesFolder;
            }
            catch (Exception ex)
            {
                Core.MelonLogger.Error($"Failed to create mod save folder: {ex.Message}");

                string fallbackFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DealerSelfSupplySystem");

                if (!Directory.Exists(fallbackFolder))
                {
                    Directory.CreateDirectory(fallbackFolder);
                }

                Core.MelonLogger.Msg($"Using fallback save folder at {fallbackFolder}");
                return fallbackFolder;
            }
        }

        private static string GetModSaveFolder(string gameSaveFolderPath)
        {
            string saveRootFolder = Path.GetDirectoryName(gameSaveFolderPath);
            string modSavesFolder = Path.Combine(saveRootFolder, MOD_FOLDER_NAME);
            return modSavesFolder;
        }
    }
}