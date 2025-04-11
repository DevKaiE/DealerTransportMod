using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Storage;
using Il2CppScheduleOne.UI;

namespace PackagerExtension.DealerExtension
{
    public class DealerStorageManager
    {

        private static Dictionary<StorageEntity, DealerExtendedBrain> _dealerStorageDictionary;
        private static Dictionary<StorageEntity, DealerExtensionUI> _dealerStorageUIDictionary;
        private static Dictionary<StorageMenu, StorageEntity> _storageMenuStorageEntityDictionary;
        private static List<DealerExtendedBrain> _dealerExtendedBrainList;

        public DealerStorageManager()
        {
            _dealerStorageDictionary = new Dictionary<StorageEntity, DealerExtendedBrain>();
            _dealerStorageUIDictionary = new Dictionary<StorageEntity, DealerExtensionUI>();
            _storageMenuStorageEntityDictionary = new Dictionary<StorageMenu, StorageEntity>();
            _dealerExtendedBrainList = new List<DealerExtendedBrain>();
        }

        public void SetDealerToStorage(StorageEntity storageEntity, DealerExtendedBrain dealer)
        {
            if (!_dealerStorageDictionary.ContainsKey(storageEntity))
            {
                _dealerStorageDictionary.Add(storageEntity, dealer);
            }
            else
            {
                _dealerStorageDictionary[storageEntity] = dealer;
            }
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
            foreach (var kvp in _dealerStorageDictionary)
            {
                if (kvp.Value != null)
                {
                    DealerExtendedBrain dealerEx = kvp.Value;
                    Dealer dealer = dealerEx.Dealer;
                    dealerEx.CalculateNeedsItems();
                    bool needsItems = dealerEx.NeedsItems;
                    if (!needsItems) break;
                    //Core.MelonLogger.Msg($"Checking dealer: {dealer.fullName}, Current Contract: {dealer.currentContract.Customer.name}");
                    //if (dealer.currentContract) break;

                    StorageEntity storageEntity = kvp.Key;
                    if (storageEntity == null) break;

                    dealerEx.AddOrderableItemsFromStorage(storageEntity);

                }
            }
        }
    }
}
