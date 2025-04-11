
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.ItemFramework;

namespace PackagerExtension.DealerExtension
{
    public static class DealerExtendedBrain
    {
        public static bool NeedsItems(Dealer dealer)
        {
            int totalItemCount = dealer.Inventory.GetTotalItemCount();
            List<ItemSlot> slots = dealer.GetAllSlots().ToArray().ToList();
            int totalAmountSlots = slots.Count;
            int slotsWithItems = 0;
            foreach (ItemSlot slot in slots)
            {
              if (slot.Quantity > 0)
                {
                    slotsWithItems++;
                }
            }
            return (totalAmountSlots - slotsWithItems) > 0;
        }
    }
}
