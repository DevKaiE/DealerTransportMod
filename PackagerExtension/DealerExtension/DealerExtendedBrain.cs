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
        private float INVENTORY_THRESHOLD; // Consider dealer needs items if less than X% of slots filled
        private bool _canBeInturrupted = false; // Set to true if you want to allow interruption of item collection by contracts

        // Stats tracking
        public int TotalItemsCollected { get; private set; } = 0;
        public DateTime LastCollectionTime { get; private set; }
        public string LastProcessedStorageName { get; private set; } = "None";

        public DealerExtendedBrain(Dealer dealer)
        {
            Dealer = dealer;
            INVENTORY_THRESHOLD = Config.dealerInventoryThreshold.Value;
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
            // Get all items in storage
            List<ItemInstance> items = storageEntity.GetAllItems().ToArray().ToList();

            // Debug logging to investigate what's available
            Core.MelonLogger.Msg($"Dealer {Dealer.fullName} checking storage {storageEntity.name}:");
            Core.MelonLogger.Msg($"- Storage has {items.Count} total items");

            // Only check if items are products and not unpackaged
            bool hasValidItems = items.Any(item =>
                item != null &&
                item.Category == EItemCategory.Product &&
                !item.Name.Contains("Unpackaged"));

            if (hasValidItems)
            {
                // Count how many valid items were found
                int validItemCount = items.Count(item =>
                    item != null &&
                    item.Category == EItemCategory.Product &&
                    !item.Name.Contains("Unpackaged"));

                Core.MelonLogger.Msg($"- Found {validItemCount} valid items in storage");
                return true;
            }

            // Debug: Log the first few items in storage for debugging
            if (items.Count > 0)
            {
                Core.MelonLogger.Msg("Storage contains:");
                foreach (var item in items.Take(5))
                {
                    if (item != null)
                        Core.MelonLogger.Msg($"  - {item.Name} (Type: {item.GetType()}, Category: {item.Category})");
                }
            }

            Core.MelonLogger.Msg($"- No valid items found for dealer {Dealer.fullName}");
            return false;
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
            // Store the name of the storage being processed
            LastProcessedStorageName = storageEntity.name;

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

            List<ItemInstance> items = storageEntity.GetAllItems().ToArray().ToList();
            int itemsTransferred = 0;
            int maxItemsToTake = CalculateMaxItemsToTake();

            Core.MelonLogger.Msg($"Dealer {Dealer.fullName} attempting to collect up to {maxItemsToTake} items");

            // Simply collect any product that's not unpackaged
            foreach (ItemInstance item in items.ToList()) // Use ToList to avoid modification during iteration
            {
                if (itemsTransferred >= maxItemsToTake)
                    break;

                if (item == null) continue;

                // Skip unpackaged products
                if (item.Name.Contains("Unpackaged")) continue;
                if (item.Category != EItemCategory.Product) continue;

                // Add to dealer inventory without checking orderableProducts
                Dealer.AddItemToInventory(item);
                itemsTransferred++;
            }

            return itemsTransferred;
        }

        private int CalculateMaxItemsToTake()
        {
            // Calculate how many empty slots the dealer has
            List<ItemSlot> slots = Dealer.GetAllSlots().ToArray().ToList();
            int emptySlots = slots.Count(slot => slot.Quantity == 0);

            // Consider how many other dealers might be assigned to the same storage
            float takePercentage = 0.75f; // Default take percentage for single dealer

            // If multiple dealers per storage is enabled, use the configurable share rate
            if (Config.multipleDealersPerStorage.Value)
            {
                takePercentage = Config.dealerCollectionShareRate.Value;

                // Ensure the share rate is within reasonable bounds
                takePercentage = Mathf.Clamp(takePercentage, 0.1f, 1.0f);
            }

            // Take at most N% of available empty slots
            return Mathf.Max(1, Mathf.FloorToInt(emptySlots * takePercentage));
        }
    }
}