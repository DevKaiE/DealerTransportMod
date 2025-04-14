using MelonLoader;
using UnityEngine;

namespace DealerSelfSupplySystem
{
    public class Config
    {
        // Define the category as static
        public static MelonPreferences_Category balanceCategory;

        // Define the entries as static
        public static MelonPreferences_Entry<bool> multipleDealersPerStorage;
        public static MelonPreferences_Entry<bool> dealerStorageUIClosedByDefault;
        public static MelonPreferences_Entry<float> dealerInventoryThreshold;
        public static MelonPreferences_Entry<KeyCode> uiToggleKey;
        public static MelonPreferences_Entry<bool> smallUIMode;
        public static MelonPreferences_Entry<Vector2> uiPosition;
        public static MelonPreferences_Entry<int> maxDealersPerStorage;
        public static MelonPreferences_Entry<float> dealerStorageCheckInterval;
        public static MelonPreferences_Entry<float> dealerCollectionShareRate;

        public Config()
        {
            // Create the category if it doesn't exist
            balanceCategory = MelonPreferences.CreateCategory("DealerSelfSupplySystem");

            // Create entries with descriptions
            multipleDealersPerStorage = balanceCategory.CreateEntry("MultipleDealersPerStorage", true,
                description: "Allow multiple dealers to be assigned to the same storage.");

            maxDealersPerStorage = balanceCategory.CreateEntry("MaxDealersPerStorage", 3,
                description: "Maximum number of dealers that can be assigned to a single storage when multiple dealers are enabled.");

            dealerCollectionShareRate = balanceCategory.CreateEntry("DealerCollectionShareRate", 0.5f,
                description: "Percentage of empty inventory slots a dealer will try to fill when multiple dealers share a storage (0.1-1.0).");

            dealerStorageUIClosedByDefault = balanceCategory.CreateEntry("DealerStorageUIClosedByDefault", true,
                description: "Set the dealer storage UI to be collapsed by default.");

            dealerInventoryThreshold = balanceCategory.CreateEntry("DealerInventoryThreshold", 0.3f,
                description: "The inventory threshold for a dealer to be considered low on items (0.0 - 1.0).");

            dealerStorageCheckInterval = balanceCategory.CreateEntry("DealerStorageCheckInterval", 60f,
                description: "How often (in seconds) dealers check if they need to collect items from storage.");

            uiToggleKey = balanceCategory.CreateEntry("UIToggleKey", KeyCode.Tab,
                description: "Key used to toggle the dealer assignment UI panel.");

            smallUIMode = balanceCategory.CreateEntry("SmallUIMode", true,
                description: "Use a more compact UI to avoid blocking storage slots.");

            // Default positioning for UI (1,1 is top right corner)
            uiPosition = balanceCategory.CreateEntry("UIPosition", new Vector2(1, 1),
                description: "Position of the UI panel (X,Y in screen space, values from 0-1).");

            // Save the preferences
            MelonPreferences.Save();
        }
    }
}