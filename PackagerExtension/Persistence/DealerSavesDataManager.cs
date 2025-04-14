using System;
using System.IO;
using System.Collections.Generic;
using Il2CppNewtonsoft.Json;
using MelonLoader;

namespace DealerSelfSupplySystem.Persistence
{
    public class DealerSaveDataManager
    {
        private const string MOD_FOLDER_NAME = "DealerSelfSupplySystem";
        public static void SaveData(string gameSaveFolderPath)
        {
            try
            {
                Core.MelonLogger.Msg($"Saving dealer storage assignments for save at: {gameSaveFolderPath}");

                // Create a list to store the assignments
                List<DealerExtension.DealerStorageAssignment> assignments = new List<DealerExtension.DealerStorageAssignment>();

                // Get valid dealer-storage pairs
                foreach (var pair in Core.DealerStorageManager._dealerStorageDictionary)
                {
                    if (pair.Key != null && pair.Value != null)
                    {
                        var storage = pair.Key;
                        var dealer = pair.Value;

                        if (storage != null && dealer?.Dealer != null)
                        {
                            var assignment = new DealerExtension.DealerStorageAssignment
                            {
                                StorageEntityPosition = storage.transform.position, // This sets PosX, PosY, PosZ
                                DealerName = dealer.Dealer.fullName
                            };

                            Core.MelonLogger.Msg($"Saving assignment: {dealer.Dealer.fullName} -> Storage at {storage.transform.position}");
                            assignments.Add(assignment);
                        }
                    }
                }

                // Create folder structure for our mod's saves
                string modSavesFolder = CreateModSaveFolder(gameSaveFolderPath);

                // Use the last segment of the game save path as our save identifier
                string saveIdentifier = Path.GetFileName(gameSaveFolderPath); // e.g., "SaveGame_1"
                string saveFilePath = Path.Combine(modSavesFolder, $"{saveIdentifier}_dealer_storage_data.json");

                // Serialize data using standard .NET serializer
                string json = System.Text.Json.JsonSerializer.Serialize(assignments,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                // Write to file
                File.WriteAllText(saveFilePath, json);
                Core.MelonLogger.Msg($"Successfully saved {assignments.Count} dealer assignments to {saveFilePath}");
            }
            catch (Exception ex)
            {
                Core.MelonLogger.Error($"Failed to save dealer storage assignments: {ex.Message}");
            }
        }

        public static void LoadData(string gameSaveFolderPath)
        {
            try
            {
                Core.MelonLogger.Msg($"Loading dealer storage assignments for save at: {gameSaveFolderPath}");

                // Get the mod saves folder and save identifier
                string modSavesFolder = GetModSaveFolder(gameSaveFolderPath);
                string saveIdentifier = Path.GetFileName(gameSaveFolderPath); // e.g., "SaveGame_1"
                string saveFilePath = Path.Combine(modSavesFolder, $"{saveIdentifier}_dealer_storage_data.json");

                // Check if the file exists
                if (!File.Exists(saveFilePath))
                {
                    Core.MelonLogger.Msg($"No dealer storage assignments file found at {saveFilePath}");
                    return;
                }

                // Read the JSON from file
                string json = File.ReadAllText(saveFilePath);

                if (string.IsNullOrEmpty(json))
                {
                    Core.MelonLogger.Msg("Save file is empty");
                    return;
                }

                // Deserialize the data
                List<DealerExtension.DealerStorageAssignment> assignments =
                    System.Text.Json.JsonSerializer.Deserialize<List<DealerExtension.DealerStorageAssignment>>(json);

                if (assignments == null || assignments.Count == 0)
                {
                    Core.MelonLogger.Warning("Dealer storage data was invalid or empty");
                    return;
                }

                Core.MelonLogger.Msg($"Loading {assignments.Count} dealer storage assignments");

                // We need to wait until all dealers and storage entities are loaded
                MelonCoroutines.Start(Core.DealerStorageManager.RestoreDealerAssignments(assignments));
            }
            catch (Exception ex)
            {
                Core.MelonLogger.Error($"Failed to load dealer storage assignments: {ex.Message}");
            }
        }

        private static string CreateModSaveFolder(string gameSaveFolderPath)
        {
            try
            {
                // Go one level up from the specific save folder (e.g., "SaveGame_1")
                string saveRootFolder = Path.GetDirectoryName(gameSaveFolderPath);

                // Create our mod's save folder next to the game's save folders
                string modSavesFolder = Path.Combine(saveRootFolder, MOD_FOLDER_NAME);

                // Create the directory if it doesn't exist
                if (!Directory.Exists(modSavesFolder))
                {
                    Directory.CreateDirectory(modSavesFolder);
                    Core.MelonLogger.Msg($"Created mod save folder at {modSavesFolder}");
                }

                return modSavesFolder;
            }
            catch (Exception ex)
            {
                Core.MelonLogger.Error($"Failed to create mod save folder: {ex.Message}");

                // Fallback to a location that should be writable
                string fallbackFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DealerSelfSupplySystem");

                if (!Directory.Exists(fallbackFolder))
                {
                    Directory.CreateDirectory(fallbackFolder);
                }

                Core.MelonLogger.Msg($"Using fallback save folder at {fallbackFolder}");
                return fallbackFolder;
            }
        }

        private static string GetModSaveFolder(string gameSaveFolderPath)
        {
            // Go one level up from the specific save folder
            string saveRootFolder = Path.GetDirectoryName(gameSaveFolderPath);

            // Get our mod's save folder path
            string modSavesFolder = Path.Combine(saveRootFolder, MOD_FOLDER_NAME);

            return modSavesFolder;
        }
    }
}