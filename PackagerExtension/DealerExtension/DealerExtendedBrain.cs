using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Storage;
using MelonLoader;
using DealerSelfSupplySystem.Utils;
using UnityEngine;

namespace DealerSelfSupplySystem.DealerExtension
{
    public class DealerExtendedBrain
    {
        public Dealer Dealer { get; private set; }
        public bool NeedsItems { get; private set; } = false;
        public bool IsProcessingItems { get; private set; } = false;
        private float lastInventoryCheckTime = 0f;
        private const float INVENTORY_CHECK_INTERVAL = 30f; // Check inventory every 30 seconds
        private const float INVENTORY_THRESHOLD = 0.4f; // Consider dealer needs items if less than 40% of slots filled
        private bool _canBeInturrupted = false; // Set to true if you want to allow interruption of item collection by contracts

        // Stats tracking
        public int TotalItemsCollected { get; private set; } = 0;
        public DateTime LastCollectionTime { get; private set; }

        public DealerExtendedBrain(Dealer dealer)
        {
            Dealer = dealer;
        }

        public bool CalculateNeedsItems()
        {
            // Only calculate if we haven't checked recently to avoid constant calculations
            if (Time.time - lastInventoryCheckTime < INVENTORY_CHECK_INTERVAL)
                return NeedsItems;

            lastInventoryCheckTime = Time.time;

            List<ItemSlot> slots = Dealer.GetAllSlots().ToArray().ToList();

            // Early exit if dealer has no slots
            if (slots.Count == 0)
            {
                NeedsItems = false;
                return false;
            }

            int totalAmountSlots = slots.Count;
            int slotsWithItems = slots.Count(slot => slot.Quantity > 0);

            // Check if we're below threshold
            float fillPercentage = (float)slotsWithItems / totalAmountSlots;
            bool needsItems = fillPercentage < INVENTORY_THRESHOLD;

            if (needsItems != NeedsItems)
            {
                // Log state change
                if (needsItems)
                    Core.MelonLogger.Msg($"Dealer {Dealer.fullName} inventory low ({fillPercentage:P0}), needs restocking");
                else
                    Core.MelonLogger.Msg($"Dealer {Dealer.fullName} inventory sufficient ({fillPercentage:P0})");
            }

            NeedsItems = needsItems;
            return needsItems;
        }

        public bool TryCollectItemsFromStorage(StorageEntity storageEntity)
        {
            if (storageEntity == null)
            {
                Core.MelonLogger.Error("Cannot collect items: storage entity is null");
                return false;
            }

            // Don't collect if the dealer is in a contract or doesn't need items
            if (Dealer.currentContract != null)
            {  
                Core.MelonLogger.Msg($"Dealer {Dealer.fullName} is busy with a contract, cannot collect items");
                return false; 
            }
      

            if (!CalculateNeedsItems())
                return false;

            if (IsProcessingItems)
            { 
                Core.MelonLogger.Msg($"Dealer {Dealer.fullName} is already processing items, cannot collect again");
                return false;
            }

            // Check if storage has any valid items for this dealer
            if (!HasValidItemsForDealer(storageEntity))
            {
                Core.MelonLogger.Msg($"Storage {storageEntity.name} has no valid items for {Dealer.fullName}");
                return false;
            }

            // Start collecting process with a delay to simulate travel time
            IsProcessingItems = true;            
            MelonCoroutines.Start(SimulateItemCollection(storageEntity));
            return true;
        }

        private bool HasValidItemsForDealer(StorageEntity storageEntity)
        {
            List<ItemInstance> items = storageEntity.GetAllItems().ToArray().ToList();
            List<ProductDefinition> orderableProducts = Dealer.GetOrderableProducts().ToArray().ToList();

            return items.Any(item =>
                item != null &&
                item.Category == EItemCategory.Product &&
                !item.Name.Contains("Unpackaged") &&
                orderableProducts.Any(p => p.ID == item.ID));
        }

        private float CalculateTravelTime(StorageEntity storageEntity)
        {
            // Calculate distance-based travel time if positions are available
            try
            {
                if (Dealer.transform != null && storageEntity.transform != null)
                {
                    float distance = Vector3.Distance(Dealer.transform.position, storageEntity.transform.position);
                    float baseTime = Mathf.Max(5f, distance * 0.5f); // 0.5 seconds per unit of distance, minimum 5 seconds

                    // Apply random variance (±20%) to make it more natural
                    return baseTime * UnityEngine.Random.Range(0.8f, 1.2f);
                }
            }
            catch (Exception ex)
            {
                Core.MelonLogger.Warning($"Error calculating travel distance: {ex.Message}. Using default time.");
            }

            // Default if distance calculation fails
            return UnityEngine.Random.Range(10f, 20f);
        }

        private System.Collections.IEnumerator SimulateItemCollection(StorageEntity storageEntity)
        {

            float travelTime = CalculateTravelTime(storageEntity);

            // Log the start of the collection process
            Core.MelonLogger.Msg($"Dealer {Dealer.fullName} is traveling to storage {storageEntity.name}, ETA: {travelTime:F1} seconds");
            try
            {
                // Wait for the "travel time" to simulate the dealer moving to the storage
                yield return new WaitForSeconds(travelTime);

                // Check if conditions still valid (dealer might have gotten a contract during travel)
                if (Dealer.currentContract != null && _canBeInturrupted)
                {
                    Core.MelonLogger.Msg($"Dealer {Dealer.fullName} received a contract during travel, aborting item collection");
                    IsProcessingItems = false;
                    yield break;
                }

                // Process the items
                Core.MelonLogger.Msg($"Dealer {Dealer.fullName} has reached storage {storageEntity.name} and is collecting items");

                // Collect the items
                int itemsCollected = AddOrderableItemsFromStorage(storageEntity);

                if (itemsCollected > 0)
                {
                    Core.MelonLogger.Msg($"Dealer {Dealer.fullName} collected {itemsCollected} items from storage {storageEntity.name}");
                    TotalItemsCollected += itemsCollected;
                    LastCollectionTime = DateTime.Now;
                    Dealer.SendTextMessage(Messages.GetRandomItemCollectionMessage(true));
                }
                else
                {
                    Core.MelonLogger.Msg($"Dealer {Dealer.fullName} found no suitable items in storage {storageEntity.name}");
                    Dealer.SendTextMessage(Messages.GetRandomItemCollectionMessage(false));
                }

                // Simulate return travel (half the original time)
                yield return new WaitForSeconds(travelTime);

                Core.MelonLogger.Msg($"Dealer {Dealer.fullName} has returned from storage {storageEntity.name}");
            }
            finally
            {
                IsProcessingItems = false;
               
            }
        }

        public int AddOrderableItemsFromStorage(StorageEntity storageEntity)
        {
            if (storageEntity == null) return 0;

            // Get what products this dealer can sell
            List<ProductDefinition> orderableProducts = Dealer.GetOrderableProducts().ToArray().ToList();
            List<ItemInstance> items = storageEntity.GetAllItems().ToArray().ToList();
            int itemsTransferred = 0;
            int maxItemsToTake = CalculateMaxItemsToTake();

            // First prioritize items that the dealer can actually sell
            foreach (ItemInstance item in items)
            {
                if (itemsTransferred >= maxItemsToTake)
                    break;

                if (item == null) continue;

                // Skip unpackaged products
                if (item.Name.Contains("Unpackaged")) continue;
                if (item.Category != EItemCategory.Product) continue;

                // Check if the dealer can sell this product
                if (orderableProducts.Any(p => p.ID == item.ID))
                {
                    // Add to dealer inventory
                    Dealer.AddItemToInventory(item);
                    itemsTransferred++;
                }
            }

            return itemsTransferred;
        }

        private int CalculateMaxItemsToTake()
        {
            // Calculate how many empty slots the dealer has
            List<ItemSlot> slots = Dealer.GetAllSlots().ToArray().ToList();
            int emptySlots = slots.Count(slot => slot.Quantity == 0);

            // Take at most 75% of available empty slots to prevent 
            // dealers from emptying storage completely
            return Mathf.Max(1, Mathf.FloorToInt(emptySlots * 0.75f));
        }
    }
}