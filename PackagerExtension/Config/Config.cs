using MelonLoader;

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

        public Config()
        {
            // Create the category if it doesn't exist
            balanceCategory = MelonPreferences.CreateCategory("DealerSelfSupplySystem");

            // Create entries with descriptions
            multipleDealersPerStorage = balanceCategory.CreateEntry("MultipleDealersPerStorage", false,
                description: "Allow multiple dealers to be assigned to the same storage.");

            dealerStorageUIClosedByDefault = balanceCategory.CreateEntry("DealerStorageUIClosedByDefault", true,
                description: "Set the dealer storage UI to be closed by default.");

            dealerInventoryThreshold = balanceCategory.CreateEntry("DealerInventoryThreshold", 0.3f,
                description: "The inventory threshold for a dealer to be considered low on items (0.0 - 1.0).");

            // Save the preferences
            MelonPreferences.Save();
        }
    }
}