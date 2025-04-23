using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Storage;
using MelonLoader;
using DealerSelfSupplySystem.Utils;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

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

            try
            {
                // Safely get slots
                var slotsArray = Dealer.GetAllSlots();
                if (slotsArray == null)
                {
                    Core.MelonLogger.Msg($"Dealer {Dealer.fullName} has no inventory slots array");
                    NeedsItems = false;
                    return false;
                }

                List<ItemSlot> slots = new List<ItemSlot>();
                foreach (var slot in slotsArray)
                {
                    if (slot != null)
                        slots.Add(slot);
                }

                // Early exit if dealer has no slots
                if (slots.Count == 0)
                {
                    Core.MelonLogger.Msg($"Dealer {Dealer.fullName} has no inventory slots");
                    NeedsItems = false;
                    return false;
                }

                // Count slots with items vs empty slots
                int totalSlots = slots.Count;
                int filledSlots = 0;

                foreach (var slot in slots)
                {
                    if (slot.Quantity > 0)
                        filledSlots++;
                }

                // Calculate fill percentage
                float fillPercentage = (float)filledSlots / totalSlots;
                bool needsItems = fillPercentage < INVENTORY_THRESHOLD;

                Core.MelonLogger.Msg($"Dealer {Dealer.fullName} inventory status: {filledSlots}/{totalSlots} slots filled ({fillPercentage:P1})");

                if (needsItems != NeedsItems)
                {
                    // Log state change
                    if (needsItems)
                        Core.MelonLogger.Msg($"Dealer {Dealer.fullName} inventory low ({fillPercentage:P1}), needs restocking");
                    else
                        Core.MelonLogger.Msg($"Dealer {Dealer.fullName} inventory sufficient ({fillPercentage:P1})");
                }

                NeedsItems = needsItems;
                return needsItems;
            }
            catch (Exception ex)
            {
                Core.MelonLogger.Error($"Error checking dealer inventory: {ex.Message}");
                NeedsItems = false;
                return false;
            }
        }

        public bool TryCollectItemsFromStorage(StorageEntity storageEntity)
        {
            try
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
            catch (Exception ex)
            {
                Core.MelonLogger.Error($"Error in TryCollectItemsFromStorage: {ex.Message}");
                IsProcessingItems = false;
                return false;
            }
        }

        private bool HasValidItemsForDealer(StorageEntity storageEntity)
        {
            try
            {
                // Get all items in storage
                var itemsArray = storageEntity.GetAllItems();
                if (itemsArray == null)
                {
                    Core.MelonLogger.Msg($"Storage {storageEntity.name} has no items array");
                    return false;
                }

                List<ItemInstance> items = new List<ItemInstance>();
                foreach (var item in itemsArray)
                {
                    if (item != null)
                        items.Add(item);
                }

                Core.MelonLogger.Msg($"Dealer {Dealer.fullName} checking storage {storageEntity.name}: {items.Count} total items");

                // Count valid items and their total quantity
                int validItemTypes = 0;
                int totalValidQuantity = 0;

                foreach (var item in items)
                {
                    if (item != null &&
                        item.Category == EItemCategory.Product &&
                        !item.Name.Contains("Unpackaged"))
                    {
                        validItemTypes++;
                        totalValidQuantity += item.Quantity;
                        Core.MelonLogger.Msg($"  - Valid item: {item.Name} (Quantity: {item.Quantity})");
                    }
                }

                if (validItemTypes > 0)
                {
                    Core.MelonLogger.Msg($"Found {validItemTypes} valid item types with total quantity of {totalValidQuantity} in storage {storageEntity.name}");
                    return true;
                }

                Core.MelonLogger.Msg($"No valid items found in storage {storageEntity.name}");
                return false;
            }
            catch (Exception ex)
            {
                Core.MelonLogger.Error($"Error checking valid items: {ex.Message}");
                return false;
            }
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
                int itemsCollected = AddItemsFromStorage(storageEntity);

                if (itemsCollected > 0)
                {
                    Core.MelonLogger.Msg($"Dealer {Dealer.fullName} successfully collected items from storage {storageEntity.name}");
                    TotalItemsCollected += itemsCollected;
                    LastCollectionTime = DateTime.Now;
                    Dealer.SendTextMessage(Messages.GetRandomItemCollectionMessage(true));
                }
                else
                {
                    Core.MelonLogger.Msg($"Dealer {Dealer.fullName} found no suitable items in storage {storageEntity.name}");
                    Dealer.SendTextMessage(Messages.GetRandomItemCollectionMessage(false));
                }

                // Simulate return travel
                yield return new WaitForSeconds(travelTime);

                Core.MelonLogger.Msg($"Dealer {Dealer.fullName} has returned from storage {storageEntity.name}");
            }
            
            finally
            {
                IsProcessingItems = false;
            }
        }

        public int AddItemsFromStorage(StorageEntity storageEntity)
        {
            try
            {
                if (storageEntity == null)
                {
                    Core.MelonLogger.Error("Cannot add items: storage entity is null");
                    return 0;
                }

                // Get all items in storage
                var itemsArray = storageEntity.GetAllItems();
                if (itemsArray == null)
                {
                    Core.MelonLogger.Error("Storage returned null items array");
                    return 0;
                }

                List<ItemInstance> storageItems = new List<ItemInstance>();
                foreach (var item in itemsArray)
                {
                    if (item != null)
                        storageItems.Add(item);
                }

                int itemsTransferred = 0;
                int totalQuantityTransferred = 0;
                int maxItemsToTake = CalculateMaxItemsToTake();

                Core.MelonLogger.Msg($"Dealer {Dealer.fullName} attempting to collect up to {maxItemsToTake} items from {storageItems.Count} items in storage");

                // Filter for valid products (not unpackaged)
                List<ItemInstance> validItems = new List<ItemInstance>();
                foreach (var item in storageItems)
                {
                    if (item != null &&
                        item.Category == EItemCategory.Product &&
                        !item.Name.Contains("Unpackaged"))
                    {
                        validItems.Add(item);
                        Core.MelonLogger.Msg($"Found valid item: {item.Name} (Quantity: {item.Quantity})");
                    }
                }

                Core.MelonLogger.Msg($"Found {validItems.Count} valid item types in storage {storageEntity.name}");

                // Process each valid item in storage
                foreach (var item in validItems)
                {
                    if (totalQuantityTransferred >= maxItemsToTake)
                        break;
                    
                    // Get the current quantity of this item
                    int quantityToTake = Mathf.Min(item.Quantity, maxItemsToTake - totalQuantityTransferred);

                    if (quantityToTake <= 0)
                        continue;

                    // Add a copy of the item to dealer inventory with appropriate quantity
                    ItemInstance itemCopy = item.GetCopy(quantityToTake);

                    if (itemCopy == null)
                    {
                        Core.MelonLogger.Warning($"Failed to create copy of item {item.Name}");
                        continue;
                    }

                    // Add to dealer inventory
                    Dealer.AddItemToInventory(itemCopy);

                    item.ChangeQuantity(-quantityToTake);
                    itemsTransferred++;
                    totalQuantityTransferred += quantityToTake;
                    Core.MelonLogger.Msg($"Transferred {quantityToTake} of {item.Name} to dealer {Dealer.fullName}");

                }

                Core.MelonLogger.Msg($"Dealer {Dealer.fullName} collected {totalQuantityTransferred} items (from {itemsTransferred} stacks) from storage {storageEntity.name}");
                return totalQuantityTransferred;
            }
            catch (Exception ex)
            {
                Core.MelonLogger.Error($"Error adding items from storage: {ex.Message}\n{ex.StackTrace}");
                return 0;
            }
        }

        private int CalculateMaxItemsToTake()
        {
            try
            {
                // Calculate empty slots
                var slotsArray = Dealer.GetAllSlots();
                if (slotsArray == null)
                    return 1;

                List<ItemSlot> slots = new List<ItemSlot>();
                foreach (var slot in slotsArray)
                {
                    if (slot != null)
                        slots.Add(slot);
                }

                // Calculate total capacity and current items
                int totalCapacity = 0;
                int currentItems = 0;

                foreach (var slot in slots)
                {
                    if (slot.ItemInstance != null)
                    {
                        totalCapacity += slot.ItemInstance.StackLimit;
                        currentItems += slot.ItemInstance.Quantity;
                    }
                    else
                    {
                        // Assume empty slots have standard capacity of 20
                        totalCapacity += 20;
                    }
                }

                int availableCapacity = totalCapacity - currentItems;

                // Consider how many other dealers might be assigned to the same storage
                float takePercentage = 0.75f; // Default take percentage for single dealer

                // If multiple dealers per storage is enabled, use the configurable share rate
                if (Config.multipleDealersPerStorage.Value)
                {
                    takePercentage = Config.dealerCollectionShareRate.Value;
                    takePercentage = Mathf.Clamp(takePercentage, 0.1f, 1.0f);
                }

                // Take at most N% of available capacity, minimum 1
                int maxItems = Mathf.Max(1, Mathf.FloorToInt(availableCapacity * takePercentage));

                Core.MelonLogger.Msg($"Dealer {Dealer.fullName} capacity: {currentItems}/{totalCapacity}, " +
                                    $"Available capacity: {availableCapacity}, Share rate: {takePercentage:P1}, " +
                                    $"Will take up to: {maxItems} items");

                return maxItems;
            }
            catch (Exception ex)
            {
                Core.MelonLogger.Error($"Error calculating max items to take: {ex.Message}");
                return 1; // Return minimum value on error
            }
        }
    }
}