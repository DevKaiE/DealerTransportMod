
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Storage;

namespace PackagerExtension.DealerExtension
{
    public class DealerExtendedBrain
    {
        public Dealer Dealer { get; private set; }
        public bool NeedsItems { get; private set; } = false;
        public bool IsGettingItems { get; private set; } = false;
        public DealerExtendedBrain(Dealer dealer)
        {
            Dealer = dealer;
        }
        public bool CalculateNeedsItems()
        {
            int totalItemCount = Dealer.Inventory.GetTotalItemCount();
            List<ItemSlot> slots = Dealer.GetAllSlots().ToArray().ToList();
            int totalAmountSlots = slots.Count;
            int slotsWithItems = 0;
            foreach (ItemSlot slot in slots)
            {
              if (slot.Quantity > 0)
                {
                    slotsWithItems++;
                }
            }
            bool needsitems = (totalAmountSlots - slotsWithItems) > 0;
            NeedsItems = needsitems;
            return needsitems;
        }

        public void AddOrderableItemsFromStorage(StorageEntity storageEntity)
        {
            //Core.MelonLogger.Msg($"Adding orderable items from storage: {storageEntity.name}");
            if (storageEntity == null) return;
            if (IsGettingItems) return;
            IsGettingItems = true;
            Core.MelonLogger.Msg($"Getting orderable items from storage: {storageEntity.name}, IsGettingItems: {IsGettingItems}");
            List<ItemInstance> items = storageEntity.GetAllItems().ToArray().ToList();
            foreach (ItemInstance item in items)
            {
                Core.MelonLogger.Msg($"Checking item: {item.Name}");
                if (item == null) continue;
                if (item.Name.Contains("Unpackaged")) continue;     
                if (item.Category != EItemCategory.Product) continue;
                Dealer.AddItemToInventory(item);
            }
            IsGettingItems = false;
        }
    }
}
