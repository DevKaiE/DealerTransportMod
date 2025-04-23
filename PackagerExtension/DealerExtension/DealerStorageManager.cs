using DealerSelfSupplySystem.Utils;
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
        // These separate properties will be used for serialization
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }
        public string DealerName { get; set; }

        // This property is for convenience in code but won't be serialized directly
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
    public class DealerStorageManager
    {
        // Modified to support multiple dealers per storage
        internal Dictionary<StorageEntity, List<DealerExtendedBrain>> _dealerStorageDictionary;
        internal Dictionary<StorageEntity, DealerExtensionUI> _dealerStorageUIDictionary;
        private Dictionary<StorageMenu, StorageEntity> _storageMenuStorageEntityDictionary;
        private List<DealerExtendedBrain> _dealerExtendedBrainList;

        // Configuration
        private float _checkInterval = 60f; // Check dealer storage every 60 seconds
        private float _lastCheckTime = 0f;
        private bool _isEnabled = true;
        private readonly string saveFileName = "dealer_storage_data.json";

        public DealerStorageManager()
        {
            _dealerStorageDictionary = new Dictionary<StorageEntity, List<DealerExtendedBrain>>();
            _dealerStorageUIDictionary = new Dictionary<StorageEntity, DealerExtensionUI>();
            _storageMenuStorageEntityDictionary = new Dictionary<StorageMenu, StorageEntity>();
            _dealerExtendedBrainList = new List<DealerExtendedBrain>();
        }

        public bool IsDealerAssignedToAnyStorage(DealerExtendedBrain dealer)
        {
            return _dealerStorageDictionary.Any(kvp => kvp.Value.Contains(dealer));
        }

        public List<StorageEntity> GetAssignedStoragesForDealer(DealerExtendedBrain dealer)
        {
            return _dealerStorageDictionary
                .Where(kvp => kvp.Value.Contains(dealer))
                .Select(kvp => kvp.Key)
                .ToList();
        }

        public bool SetDealerToStorage(StorageEntity storageEntity, DealerExtendedBrain dealer)
        {
            // Check if multiple dealers per storage is allowed
            bool allowMultipleDealers = Config.multipleDealersPerStorage.Value;
            int maxDealersPerStorage = Config.maxDealersPerStorage.Value;

            // Initialize the list if this storage doesn't have one yet
            if (!_dealerStorageDictionary.ContainsKey(storageEntity))
            {
                _dealerStorageDictionary[storageEntity] = new List<DealerExtendedBrain>();
            }

            // Check if this dealer is already assigned to this storage
            if (_dealerStorageDictionary[storageEntity].Contains(dealer))
            {
                Core.MelonLogger.Msg($"Dealer {dealer.Dealer.fullName} is already assigned to {storageEntity.name}");
                return false;
            }

            // If multiple dealers are not allowed, clear the current list
            if (!allowMultipleDealers)
            {
                if (_dealerStorageDictionary[storageEntity].Count > 0)
                {
                    Core.MelonLogger.Msg($"Replacing dealer(s) with {dealer.Dealer.fullName} for storage {storageEntity.name}");
                    _dealerStorageDictionary[storageEntity].Clear();
                }
            }
            else
            {
                // Check if we've reached the maximum number of dealers for this storage
                if (_dealerStorageDictionary[storageEntity].Count >= maxDealersPerStorage)
                {
                    Core.MelonLogger.Msg($"Storage {storageEntity.name} already has the maximum number of dealers ({maxDealersPerStorage})");
                    dealer.Dealer.SendTextMessage($"This storage is already at capacity with {maxDealersPerStorage} dealers. Use a different storage or increase the limit in config.");
                    return false;
                }
            }

            // Add the dealer to the storage
            _dealerStorageDictionary[storageEntity].Add(dealer);
            Core.MelonLogger.Msg($"Added dealer {dealer.Dealer.fullName} to storage {storageEntity.name}");

            return true;
        }

        public void RemoveDealerFromStorage(StorageEntity storageEntity, DealerExtendedBrain dealer = null)
        {
            if (!_dealerStorageDictionary.ContainsKey(storageEntity))
                return;

            if (dealer == null)
            {
                // Remove all dealers from this storage
                _dealerStorageDictionary[storageEntity].Clear();
                Core.MelonLogger.Msg($"Cleared all dealers from storage {storageEntity.name}");
            }
            else
            {
                // Remove specific dealer
                if (_dealerStorageDictionary[storageEntity].Contains(dealer))
                {
                    _dealerStorageDictionary[storageEntity].Remove(dealer);
                    Core.MelonLogger.Msg($"Removed dealer {dealer.Dealer.fullName} from storage {storageEntity.name}");
                }
            }
        }

        public List<DealerExtendedBrain> GetDealersFromStorage(StorageEntity storageEntity)
        {
            if (_dealerStorageDictionary.ContainsKey(storageEntity))
            {
                return _dealerStorageDictionary[storageEntity];
            }
            return new List<DealerExtendedBrain>();
        }

        // For backward compatibility, get the first dealer
        public DealerExtendedBrain GetDealerFromStorage(StorageEntity storageEntity)
        {
            if (_dealerStorageDictionary.ContainsKey(storageEntity) &&
                _dealerStorageDictionary[storageEntity].Count > 0)
            {
                return _dealerStorageDictionary[storageEntity][0];
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

            // Create a copy of the dictionary entries to avoid modification issues during iteration
            var storageAssignments = _dealerStorageDictionary
                .Where(kvp => kvp.Key != null && kvp.Value.Count > 0)
                .ToList();

            int totalAssignmentsChecked = 0;
            int totalCollectionAttempts = 0;

            // Process each storage entity separately
            foreach (var kvp in storageAssignments)
            {
                StorageEntity storageEntity = kvp.Key;
                List<DealerExtendedBrain> dealers = kvp.Value.ToList(); // Create a copy for safe iteration

                // Process each dealer assigned to this storage
                foreach (var dealerEx in dealers)
                {
                    // Skip invalid dealers
                    if (dealerEx?.Dealer == null)
                    {
                        Core.MelonLogger.Warning($"Removing invalid dealer reference from storage {storageEntity.name}");
                        _dealerStorageDictionary[storageEntity].Remove(dealerEx);
                        continue;
                    }

                    totalAssignmentsChecked++;

                    // Only try to collect if the dealer needs items
                    if (dealerEx.CalculateNeedsItems())
                    {
                        bool collectionStarted = dealerEx.TryCollectItemsFromStorage(storageEntity);
                        if (collectionStarted)
                        {
                            totalCollectionAttempts++;
                        }
                    }
                }
            }

            if (totalAssignmentsChecked > 0)
            {
                Core.MelonLogger.Msg($"Checked {totalAssignmentsChecked} dealer-storage assignments, {totalCollectionAttempts} dealers are collecting items");
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
            int assignedDealers = _dealerStorageDictionary.Values.SelectMany(list => list).Distinct().Count();
            int totalItemsCollected = _dealerExtendedBrainList.Sum(d => d.TotalItemsCollected);
            int totalAssignments = _dealerStorageDictionary.Values.Sum(list => list.Count);

            return $"Assigned Dealers: {assignedDealers}/{totalDealers}\n" +
                   $"Total Assignments: {totalAssignments}\n" +
                   $"Total Items Collected: {totalItemsCollected}";
        }

        public Dictionary<StorageEntity, List<DealerExtendedBrain>> GetAllAssignments()
        {
            return _dealerStorageDictionary.Where(kvp => kvp.Key != null && kvp.Value.Count > 0)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public System.Collections.IEnumerator RestoreDealerAssignments(List<DealerStorageAssignment> assignments)
        {
            // Wait a moment for game to fully initialize
            yield return new WaitForSeconds(2f);

            int restoredCount = 0;

            // Get all dealers and storage entities
            List<Dealer> allDealers = GameUtils.GetRecruitedDealers();
            List<StorageEntity> allStorages = FindAllStorageEntities();

            Core.MelonLogger.Msg($"Found {allDealers.Count} dealers and {allStorages.Count} storage entities");

            // Log storage positions for debugging
            foreach (var storage in allStorages)
            {
                Core.MelonLogger.Msg($"Storage: {storage.name}, Position: {storage.transform.position}");
            }

            foreach (var assignment in assignments)
            {
                Core.MelonLogger.Msg($"Trying to restore assignment: StoragePos={assignment.StorageEntityPosition}, DealerName={assignment.DealerName}");

                // Find matching dealer
                Dealer matchedDealer = allDealers.FirstOrDefault(d => d.fullName == assignment.DealerName);
                if (matchedDealer == null)
                {
                    Core.MelonLogger.Warning($"Could not find dealer with name: {assignment.DealerName}");
                    continue;
                }

                // Find closest storage entity to the saved position
                StorageEntity matchedStorage = null;
                float closestDistance = 1.0f; // Use a small tolerance (1 unit)

                foreach (var storage in allStorages)
                {
                    float distance = Vector3.Distance(storage.transform.position, assignment.StorageEntityPosition);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        matchedStorage = storage;
                    }
                }

                if (matchedStorage == null)
                {
                    Core.MelonLogger.Warning($"Could not find storage near position: {assignment.StorageEntityPosition}");
                    continue;
                }

                Core.MelonLogger.Msg($"Found matching storage at position {matchedStorage.transform.position}, distance: {closestDistance}");

                // Find or create dealer brain
                DealerExtendedBrain dealerBrain = _dealerExtendedBrainList.FirstOrDefault(d => d.Dealer == matchedDealer);
                if (dealerBrain == null)
                {
                    dealerBrain = new DealerExtendedBrain(matchedDealer);
                    AddDealerExtendedBrain(dealerBrain);
                    Core.MelonLogger.Msg($"Created new DealerExtendedBrain for {matchedDealer.fullName}");
                }

                // Restore assignment
                bool success = SetDealerToStorage(matchedStorage, dealerBrain);
                if (success)
                {
                    restoredCount++;
                    Core.MelonLogger.Msg($"Successfully restored assignment: {matchedDealer.fullName} -> {matchedStorage.name}");
                }
                else
                {
                    Core.MelonLogger.Warning($"Failed to set dealer {matchedDealer.fullName} to storage {matchedStorage.name}");
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