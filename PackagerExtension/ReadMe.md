# Dealer Auto-Resupply System v1.1.0

![Version](https://img.shields.io/badge/version-1.1.0-blue.svg)
![MelonLoader](https://img.shields.io/badge/MelonLoader-0.7+-green.svg)

A MelonLoader mod for Schedule I that automates inventory management for dealers. When dealers run low on product, they'll automatically travel to their assigned storage, collect suitable items, and return to their position.

## New in v1.1.0: Multiple Dealers per Storage!

This update brings the highly requested feature of assigning multiple dealers to the same storage location. Now you can have a team of dealers all working from the same stash spot!

### New Features

- **Multiple Dealers per Storage**: Assign up to 3 dealers (configurable) to the same storage location
- **Scrollable Dealer List UI**: Easily see and manage all assigned dealers
- **Resource Sharing**: Intelligent collection system prevents dealers from emptying storage too quickly
- **Configurable Settings**: Fine-tune how dealers share storage resources

## All Features

- **Storage Assignment System**: Assign dealers to specific storage locations through an intuitive UI
- **Multiple Dealer Support**: Assign multiple dealers to the same storage for more efficient operations
- **Automated Inventory Monitoring**: Dealers automatically track their inventory levels and restock when below threshold
- **Smart Collection Logic**: Dealers only collect items they can actually sell
- **Realistic Travel Simulation**: Travel time based on distance between dealer and storage location
- **Dealer Notifications**: Receive text messages from dealers when they collect items or find empty storage
- **Balanced Gameplay**: Configure limits to maintain game balance

## Installation

1. Make sure you have [MelonLoader](https://github.com/LavaGang/MelonLoader) installed (version 0.7 or higher)
2. Download the latest release of Dealer Auto-Resupply System
3. Place the `DealerAutoResupply.dll` file in your Schedule I game folder under `\Mods\`
4. Launch the game with MelonLoader

## How to Use

### Assigning Dealers to Storage

1. Open any storage container in the game
2. Look for the dealer management panel on the right side of the storage menu
3. Click the "Add Dealer" button at the bottom of the panel
4. Select a dealer from the popup menu
5. The dealer will now be associated with this storage location
6. Repeat to add multiple dealers (if enabled in settings)

### Removing a Dealer Assignment

1. Open the storage container
2. In the dealer management panel, click the "X" button next to the dealer you want to remove

### How It Works

- Dealers periodically check their inventory (configurable interval, default 60 seconds)
- If a dealer's inventory falls below the threshold (default 40% capacity), they'll begin the collection process
- The dealer will "travel" to the storage location (simulated based on distance)
- Once there, they'll collect items they can sell (up to a percentage of their empty slots)
- When multiple dealers share a storage, they'll each take a smaller percentage to avoid one dealer emptying the storage
- After collection, they'll "travel" back to their position
- You'll receive a text message from the dealer informing you of their success or failure

## Configuration

The mod includes several configuration options that can be adjusted in the MelonLoader preferences:

| Setting | Default | Description |
|---------|---------|-------------|
| MultipleDealersPerStorage | true | Allow multiple dealers to be assigned to the same storage |
| MaxDealersPerStorage | 3 | Maximum number of dealers that can be assigned to a single storage |
| DealerCollectionShareRate | 0.5 | Percentage of empty inventory slots a dealer will try to fill when sharing storage |
| DealerInventoryThreshold | 0.3 | Threshold below which dealers will attempt to restock (0.0-1.0) |
| DealerStorageCheckInterval | 60 | How often dealers check if they need to restock (seconds) |
| UIToggleKey | Tab | Key used to toggle the dealer assignment UI panel |

To change these settings:
1. Run the game once with the mod installed
2. Exit the game
3. Open `UserData\MelonPreferences.cfg` in a text editor
4. Find the `[DealerSelfSupplySystem]` section
5. Adjust the values as desired
6. Save the file and restart the game

## Planned Features

- Customizable dealer behavior profiles
- Priority system for dealers to favor certain products
- Delivery system for dealers to transfer items between storages

## Credits

- Developed by KaikiNoodles

## License

This project is licensed under the MIT License - see the LICENSE file for details.