using EmployeeExtender.Utils;
using Il2CppNewtonsoft.Json;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Storage;
using Il2CppScheduleOne.UI;
using MelonLoader;
using UnityEngine;

namespace DealerSelfSupplySystem.DealerExtension
{
    [Serializable]
    public class DealerStorageAssignment
    {
        public string StorageEntityName { get; set; }
        public string DealerName { get; set; }
    }
    public class DealerStorageManager
    {
        private Dictionary<StorageEntity, DealerExtendedBrain> _dealerStorageDictionary;
        private Dictionary<StorageEntity, DealerExtensionUI> _dealerStorageUIDictionary;
        private Dictionary<StorageMenu, StorageEntity> _storageMenuStorageEntityDictionary;
        private List<DealerExtendedBrain> _dealerExtendedBrainList;

        // Configuration
        private float _checkInterval = 60f; // Check dealer storage every 60 seconds
        private float _lastCheckTime = 0f;
        private bool _isEnabled = true;
        private readonly string saveFileName = "dealer_storage_data.json";
        public DealerStorageManager()
        {
            _dealerStorageDictionary = new Dictionary<StorageEntity, DealerExtendedBrain>();
            _dealerStorageUIDictionary = new Dictionary<StorageEntity, DealerExtensionUI>();
            _storageMenuStorageEntityDictionary = new Dictionary<StorageMenu, StorageEntity>();
            _dealerExtendedBrainList = new List<DealerExtendedBrain>();
        }

        public bool IsDealerAssignedToAnyStorage(DealerExtendedBrain dealer)
        {
            return _dealerStorageDictionary.Values.Any(d => d == dealer);
        }

        public StorageEntity GetAssignedStorageForDealer(DealerExtendedBrain dealer)
        {
            return _dealerStorageDictionary.FirstOrDefault(kvp => kvp.Value == dealer).Key;
        }

        public bool SetDealerToStorage(StorageEntity storageEntity, DealerExtendedBrain dealer)
        {
            // Check if this dealer is already assigned to another storage
            StorageEntity existingStorage = GetAssignedStorageForDealer(dealer);

            // If dealer is already assigned to another storage, don't allow it
            if (existingStorage != null && existingStorage != storageEntity)
            {
                Core.MelonLogger.Msg($"Dealer {dealer.Dealer.fullName} is already assigned to {existingStorage.name}");
                return false;
            }

            // If there's already a different dealer assigned to this storage, replace them
            if (_dealerStorageDictionary.ContainsKey(storageEntity) &&
                _dealerStorageDictionary[storageEntity] != null &&
                _dealerStorageDictionary[storageEntity] != dealer)
            {
                Core.MelonLogger.Msg($"Replacing dealer {_dealerStorageDictionary[storageEntity].Dealer.fullName} with {dealer.Dealer.fullName} for storage {storageEntity.name}");
            }

            // Update or add the assignment
            if (!_dealerStorageDictionary.ContainsKey(storageEntity))
            {
                _dealerStorageDictionary.Add(storageEntity, dealer);
            }
            else
            {
                _dealerStorageDictionary[storageEntity] = dealer;
            }

            return true;
        }

        public void RemoveDealerFromStorage(StorageEntity storageEntity)
        {
            if (_dealerStorageDictionary.ContainsKey(storageEntity))
            {
                _dealerStorageDictionary[storageEntity] = null;
            }
        }

        public StorageEntity GetStorageFromDealer(DealerExtendedBrain dealer)
        {
            if (_dealerStorageDictionary.ContainsValue(dealer))
            {
                return _dealerStorageDictionary.FirstOrDefault(x => x.Value == dealer).Key;
            }
            return null;
        }

        public DealerExtendedBrain GetDealerFromStorage(StorageEntity storageEntity)
        {
            if (_dealerStorageDictionary.ContainsKey(storageEntity))
            {
                return _dealerStorageDictionary[storageEntity];
            }
            return null;
        }

        public DealerExtensionUI GetDealerExtensionUI(StorageEntity storageEntity)
        {
            if (_dealerStorageUIDictionary.ContainsKey(storageEntity))
            {
                return _dealerStorageUIDictionary[storageEntity];
            }
            return null;
        }

        public void SetDealerExtensionUI(StorageEntity storageEntity, DealerExtensionUI dealerExtensionUI)
        {
            if (!_dealerStorageUIDictionary.ContainsKey(storageEntity))
            {
                _dealerStorageUIDictionary.Add(storageEntity, dealerExtensionUI);
            }
            else
            {
                _dealerStorageUIDictionary[storageEntity] = dealerExtensionUI;
            }
        }

        public StorageEntity GetStorageMenu(StorageMenu storageMenu)
        {
            if (_storageMenuStorageEntityDictionary.ContainsKey(storageMenu))
            {
                return _storageMenuStorageEntityDictionary[storageMenu];
            }
            return null;
        }

        public void SetStorageMenu(StorageMenu storageMenu, StorageEntity storageEntity)
        {
            if (!_storageMenuStorageEntityDictionary.ContainsKey(storageMenu))
            {
                _storageMenuStorageEntityDictionary.Add(storageMenu, storageEntity);
            }
            else
            {
                _storageMenuStorageEntityDictionary[storageMenu] = storageEntity;
            }
        }

        public void AddDealerExtendedBrain(DealerExtendedBrain dealer)
        {
            if (_dealerExtendedBrainList.Contains(dealer)) return;
            _dealerExtendedBrainList.Add(dealer);
        }

        public List<DealerExtendedBrain> GetAllDealersExtendedBrain()
        {
            return _dealerExtendedBrainList;
        }

        public void CheckDealerStorage()
        {
            // Only check at intervals to avoid performance impact
            if (Time.time - _lastCheckTime < _checkInterval || !_isEnabled)
                return;

            _lastCheckTime = Time.time;

            // Use a separate list to avoid modification during iteration
            List<KeyValuePair<StorageEntity, DealerExtendedBrain>> validPairs =
                _dealerStorageDictionary.Where(kvp => kvp.Key != null && kvp.Value != null).ToList();

            Core.MelonLogger.Msg($"Checking {validPairs.Count} dealer-storage assignments");

            foreach (var kvp in validPairs)
            {
                DealerExtendedBrain dealerEx = kvp.Value;
                StorageEntity storageEntity = kvp.Key;

                // Check if objects are still valid
                if (dealerEx.Dealer == null || storageEntity == null)
                {
                    Core.MelonLogger.Warning($"Found invalid dealer or storage reference, cleaning up");
                    // Clean up invalid entries
                    _dealerStorageDictionary.Remove(kvp.Key);
                    continue;
                }

                // Try to have the dealer collect items
                dealerEx.TryCollectItemsFromStorage(storageEntity);
            }
        }

        // New methods for configuration
        public void SetCheckInterval(float seconds)
        {
            _checkInterval = Mathf.Max(10f, seconds); // Minimum 10 seconds
        }

        public void EnableAutoCollection(bool enabled)
        {
            _isEnabled = enabled;
        }

        public bool IsAutoCollectionEnabled()
        {
            return _isEnabled;
        }

        // Get collection statistics
        public string GetCollectionStats()
        {
            int totalDealers = _dealerExtendedBrainList.Count;
            int assignedDealers = _dealerStorageDictionary.Values.Count(d => d != null);
            int totalItemsCollected = _dealerExtendedBrainList.Sum(d => d.TotalItemsCollected);

            return $"Assigned Dealers: {assignedDealers}/{totalDealers}\n" +
                   $"Total Items Collected: {totalItemsCollected}";

        }

        public void SaveDealerStorageData(string saveFolderPath)
        {
            try
            {
                // Create a list to store the assignments
                List<DealerStorageAssignment> assignments = new List<DealerStorageAssignment>();

                // Get valid dealer-storage pairs
                foreach (var pair in _dealerStorageDictionary.Where(kvp => kvp.Key != null && kvp.Value != null))
                {
                    StorageEntity storage = pair.Key;
                    DealerExtendedBrain dealer = pair.Value;

                    if (storage != null && dealer?.Dealer != null)
                    {
                        assignments.Add(new DealerStorageAssignment
                        {
                            StorageEntityName = storage.name,
                            DealerName = dealer.Dealer.fullName
                        });
                    }
                }

                // Serialize to JSON
                Il2CppSystem.Object il2CppAssignments = (Il2CppSystem.Object)(object)assignments; // Explicit cast to Il2CppSystem.Object
                string json = JsonConvert.SerializeObject(il2CppAssignments, Formatting.Indented);
                string fullPath = Path.Combine(saveFolderPath, saveFileName);

                // Write to file
                File.WriteAllText(fullPath, json);
                Core.MelonLogger.Msg($"Successfully saved dealer storage assignments to {fullPath}");
            }
            catch (Exception ex)
            {
                Core.MelonLogger.Error($"Failed to save dealer storage assignments: {ex.Message}");
            }
        }

        public void LoadDealerStorageData(string saveFolderPath)
        {
            string fullPath = Path.Combine(saveFolderPath, saveFileName);

            if (!File.Exists(fullPath))
            {
                Core.MelonLogger.Msg($"No dealer storage assignments file found at {fullPath}");
                return;
            }

            try
            {
                // Read from file
                string json = File.ReadAllText(fullPath);
                List<DealerStorageAssignment> assignments = JsonConvert.DeserializeObject<List<DealerStorageAssignment>>(json);

                if (assignments == null || assignments.Count == 0)
                {
                    Core.MelonLogger.Warning("Dealer storage data file was invalid or empty");
                    return;
                }

                Core.MelonLogger.Msg($"Loading {assignments.Count} dealer storage assignments");

                // We need to wait until all dealers and storage entities are loaded in the game
                // before we can restore assignments, so we'll set up a delayed restore
                MelonCoroutines.Start(RestoreDealerAssignments(assignments));
            }
            catch (Exception ex)
            {
                Core.MelonLogger.Error($"Failed to load dealer storage assignments: {ex.Message}");
            }
        }

        private System.Collections.IEnumerator RestoreDealerAssignments(List<DealerStorageAssignment> assignments)
        {
            // Wait a moment for game to fully initialize
            yield return new WaitForSeconds(2f);

            int restoredCount = 0;

            // Get all dealers and storage entities
            List<Dealer> allDealers = GameUtils.GetRecruitedDealers();
            List<StorageEntity> allStorages = FindAllStorageEntities();

            foreach (var assignment in assignments)
            {
                // Find matching dealer and storage
                Dealer matchedDealer = allDealers.FirstOrDefault(d => d.fullName == assignment.DealerName);
                StorageEntity matchedStorage = allStorages.FirstOrDefault(s => s.name == assignment.StorageEntityName);

                if (matchedDealer != null && matchedStorage != null)
                {
                    // Find or create dealer brain
                    DealerExtendedBrain dealerBrain = _dealerExtendedBrainList.FirstOrDefault(d => d.Dealer == matchedDealer);
                    if (dealerBrain == null)
                    {
                        dealerBrain = new DealerExtendedBrain(matchedDealer);
                        AddDealerExtendedBrain(dealerBrain);
                    }

                    // Restore assignment
                    SetDealerToStorage(matchedStorage, dealerBrain);
                    restoredCount++;
                }
            }

            Core.MelonLogger.Msg($"Successfully restored {restoredCount} dealer-storage assignments");
        }

        private List<StorageEntity> FindAllStorageEntities()
        {
            // Find all storage entities in the scene
            return UnityEngine.GameObject.FindObjectsOfType<StorageEntity>().ToList();
        }

        public void CleanUp()
        {
            // Clean up all dictionaries
            _dealerStorageDictionary.Clear();
            _dealerStorageUIDictionary.Clear();
            _storageMenuStorageEntityDictionary.Clear();
            _dealerExtendedBrainList.Clear();
            // Optionally, you can also destroy the UI objects if needed
            foreach (var ui in _dealerStorageUIDictionary.Values)
            {
                GameObject.Destroy(ui.DealerUIObject);
            }
            _dealerStorageUIDictionary.Clear();
        }


    }
}