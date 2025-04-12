using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Storage;
using Il2CppScheduleOne.UI;
using UnityEngine;

namespace PackagerExtension.DealerExtension
{
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
    }
}