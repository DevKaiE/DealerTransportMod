
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
            if (storageEntity == null) return;
            if (IsGettingItems) return;
            IsGettingItems = true;
            List<ProductDefinition> orderableProducts = Dealer.GetOrderableProducts().ToArray().ToList();
            Core.MelonLogger.Msg(string.Join(", ", orderableProducts));
            List<ItemInstance> items = storageEntity.GetAllItems().ToArray().ToList();
            foreach (ItemInstance item in items)
            {
                if (item == null) continue;
                if (!orderableProducts.Find(x => x.ID == item.ID)) continue;
                Dealer.AddItemToInventory(item);
            }
        }
    }
}
