using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Storage;
using Il2CppScheduleOne.UI;

namespace PackagerExtension.DealerExtension
{
    public class DealerStorageManager
    {

        private static Dictionary<StorageEntity, Dealer> _dealerStorageDictionary;
        private static Dictionary<StorageEntity, DealerExtensionUI> _dealerStorageUIDictionary;
        private static Dictionary<StorageMenu, StorageEntity> _storageMenuStorageEntityDictionary;

        public DealerStorageManager()
        {
            _dealerStorageDictionary = new Dictionary<StorageEntity, Dealer>();
            _dealerStorageUIDictionary = new Dictionary<StorageEntity, DealerExtensionUI>();
            _storageMenuStorageEntityDictionary = new Dictionary<StorageMenu, StorageEntity>();
        }

        public void SetDealerToStorage(StorageEntity storageEntity, Dealer dealer)
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

        public StorageEntity GetStorageFromDealer(Dealer dealer)
        {
            if (_dealerStorageDictionary.ContainsValue(dealer))
            {
                return _dealerStorageDictionary.FirstOrDefault(x => x.Value == dealer).Key;
            }
            return null;
        }

        public Dealer GetDealerFromStorage(StorageEntity storageEntity)
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

        public void CheckDealerStorage()
        {
            foreach (var kvp in _dealerStorageDictionary)
            {
                if (kvp.Value != null)
                {
                    Dealer dealer = kvp.Value;
                    bool needsItems = DealerExtendedBrain.NeedsItems(dealer);
                    if (!needsItems) break;
                    //Core.MelonLogger.Msg($"Checking dealer: {dealer.fullName}, Current Contract: {dealer.currentContract.Customer.name}");
                    if (dealer.currentContract) break;
                    StorageEntity storageEntity = kvp.Key;
                    if (storageEntity == null) break;
                    List<ItemInstance> items = storageEntity.GetAllItems().ToArray().ToList();
                    ProductManager productManager = ProductManager.Instance;
                    List<ProductDefinition> products = productManager.AllProducts.ToArray().ToList();                    
                    foreach (ItemInstance item in items)
                    {
                        if (item != null && item.Category == EItemCategory.Product && products.Find(p => p.ID == item.ID))
                        {
                            dealer.AddItemToInventory(item);
                            Core.MelonLogger.Msg($"Added item: {item.Name} to dealer: {dealer.fullName}");
                        }
                    }

                }
            }
        }
    }
}
