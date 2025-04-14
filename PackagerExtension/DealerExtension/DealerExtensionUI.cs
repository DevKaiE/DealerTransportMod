using Il2CppScheduleOne.UI;
using UnityEngine.UI;
using UnityEngine;
using DealerSelfSupplySystem.UI;
using Il2CppScheduleOne.Economy;
using Il2CppSystem;
using Il2CppScheduleOne.Storage;
using MelonLoader;
using DealerSelfSupplySystem.Utils;
using DealerSelfSupplySystem;

namespace DealerSelfSupplySystem.DealerExtension
{
    public class DealerExtensionUI
    {
        public Il2CppSystem.Guid UIGUID { get; private set; }
        public StorageMenu StorageMenu { get; private set; }
        public StorageEntity StorageEntity { get; private set; }
        public DealerExtendedBrain AssignedDealer { get; private set; }
        public GameObject DealerUIObject { get; private set; }
        public GameObject Button { get; private set; }
        public GameObject MainPanel { get; private set; }
        private bool isExpanded = false;
        private bool popupOpen = false;
        private bool closeUIDefault;
        private static KeyCode toggleKey; // Configurable toggle key

        public DealerExtensionUI(StorageMenu storageMenu, StorageEntity storageEntity)
        {
            toggleKey = Config.uiToggleKey.Value; // Get the toggle key from config
            closeUIDefault = Config.dealerStorageUIClosedByDefault.Value;
            UIGUID = new Il2CppSystem.Guid();
            StorageMenu = storageMenu;
            StorageEntity = storageEntity;
            

            // Ensure storageEntity isn't null before proceeding
            if (storageEntity != null)
            {
                AssignedDealer = Core.DealerStorageManager.GetDealerFromStorage(storageEntity);
                CreateDealerStorageUI(storageMenu, AssignedDealer);
            }
            else
            {
                Core.MelonLogger.Error("StorageEntity is null in DealerExtensionUI constructor");
            }
        }

        public void CreateDealerStorageUI(StorageMenu menu, DealerExtendedBrain assignedDealer)
        {
            // Main container
            DealerUIObject = new GameObject("DealerUI");
            GameObject.DontDestroyOnLoad(DealerUIObject);

            Canvas canvas = DealerUIObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            DealerUIObject.AddComponent<GraphicRaycaster>();

            // Create toggle button (always visible)
            CreateToggleButton();

            // Create main panel (initially collapsed)
            CreateMainPanel(assignedDealer);

            // Set initial state
            SetExpanded(false);
        }

        private void CreateToggleButton()
        {
            GameObject toggleButton = new GameObject("ToggleButton");
            toggleButton.transform.SetParent(DealerUIObject.transform, false);

            // Create a clean button background
            Image buttonImage = toggleButton.AddComponent<Image>();
            buttonImage.color = Styling.PRIMARY_COLOR;

            Button button = toggleButton.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            // Add hover color transition
            ColorBlock colors = button.colors;
            colors.normalColor = Styling.PRIMARY_COLOR;
            colors.highlightedColor = Styling.SECONDARY_COLOR;
            colors.pressedColor = Styling.SECONDARY_COLOR;
            button.colors = colors;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener((System.Action)TogglePanel);

            // Create a dollar sign as a simple, recognizable symbol for dealers
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(toggleButton.transform, false);
            Text text = textObj.AddComponent<Text>();
            text.text = "$";  // Dollar sign is universal and will render correctly
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 18;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            // Make text fully visible
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // Position the button in the top right corner but further to the edge
            RectTransform buttonRect = toggleButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1, 1);
            buttonRect.anchorMax = new Vector2(1, 1);
            buttonRect.pivot = new Vector2(1, 1);
            buttonRect.anchoredPosition = new Vector2(-10, -10); // Move more to the right edge
            buttonRect.sizeDelta = new Vector2(32, 32);
        }

        private void CreateMainPanel(DealerExtendedBrain assignedDealer)
        {
            MainPanel = new GameObject("Panel");
            MainPanel.transform.SetParent(DealerUIObject.transform, false);

            Image panelImage = MainPanel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            RectTransform panelRect = MainPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1, 1);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.pivot = new Vector2(1, 1);
            panelRect.sizeDelta = new Vector2(180, 140); // Slightly smaller width
            panelRect.anchoredPosition = new Vector2(-10, -50); // Position it further to the right edge

            // Title
            FlexiblePopup.CreateText(MainPanel.transform, "Title", "Dealer Assignment", 14, new Vector2(0, 40));

            // Dealer selection button
            string buttonText = assignedDealer != null ? assignedDealer.Dealer.fullName : "No Dealer";
            Button = FlexiblePopup.CreateButton(MainPanel.transform, "AssignedDealer", buttonText, new Vector2(0, 0), () => OnAssignedDealerButtonClicked(), new Vector2(140, 35));

            // Clear button
            FlexiblePopup.CreateButton(MainPanel.transform, "ClearButton", "Clear", new Vector2(0, -40), () => OnClearButtonClicked(), new Vector2(140, 35), EButtonType.Destructive);
        }

        private void TogglePanel()
        {
            SetExpanded(!isExpanded);
            if (popupOpen) FlexiblePopup.ClosePopup();
        }

        private void SetExpanded(bool expanded)
        {
            isExpanded = expanded;
            if (MainPanel != null)
            {
                MainPanel.SetActive(expanded);
            }
        }

        public void ToggleUI(bool open)
        {
            SetDealer(AssignedDealer);
            if (DealerUIObject != null)
            {
                DealerUIObject.SetActive(open);
                // Always start in collapsed state when opening
                if (open && closeUIDefault)
                {
                    SetExpanded(false);
                }
            }
            if (!open) FlexiblePopup.ClosePopup();
        }

        public void Update()
        {
            // Check for toggle key press
            if (DealerUIObject != null && DealerUIObject.activeSelf && Input.GetKeyDown(toggleKey))
            {
                TogglePanel();
            }
        }

        public void SetDealer(DealerExtendedBrain dealer)
        {
            AssignedDealer = dealer;
            if (Button != null)
            {
                Button.GetComponentInChildren<Text>().text = dealer != null ? dealer.Dealer.fullName : "No Dealer";
            }
        }

        public void OnAssignedDealerButtonClicked()
        {
            MelonCoroutines.Start(HandleDealerSelection());
        }

        public void OnClearButtonClicked()
        {
            SetDealer(null);
            Core.DealerStorageManager.RemoveDealerFromStorage(StorageEntity);
        }

        private System.Collections.IEnumerator HandleDealerSelection()
        {
            List<(string, string)> dealerOptions = GameUtils.GetRecruitedDealers()
                        .Select(d => (d.name, d.name))
                        .ToList();

            // Add a fallback option if no dealers are found
            if (dealerOptions.Count == 0)
                dealerOptions.Add(("No dealers available", "None"));

            string dealerChoice = null;

            // Simply position it slightly to the left of the main panel
            Vector2 popupPosition = new Vector2(0.85f, 0.9f);

            yield return FlexiblePopup.ShowPopupAndWaitForResult(
                "Choose Dealer",
                dealerOptions.ToArray(),
                result => dealerChoice = result,
                popupPosition
            );

            // Handle dealer selection
            if (!string.IsNullOrEmpty(dealerChoice) && dealerChoice != "None")
            {
                DealerExtendedBrain selectedDealer = Core.DealerStorageManager.GetAllDealersExtendedBrain()
                    .FirstOrDefault(d => d.Dealer.name == dealerChoice);

                if (selectedDealer != null)
                {
                    // Check if this dealer is already assigned to another storage
                    StorageEntity existingStorage = Core.DealerStorageManager.GetAssignedStorageForDealer(selectedDealer);

                    if (existingStorage != null && existingStorage != StorageEntity)
                    {
                        // Dealer is already assigned to another storage, show a funny message
                        string message = Messages.GetRandomDealerAlreadyAssignedMessage();
                        selectedDealer.Dealer.SendTextMessage(message);
                    }
                    else
                    {
                        // Dealer is not assigned yet or is assigned to this storage, proceed with assignment
                        bool success = Core.DealerStorageManager.SetDealerToStorage(StorageEntity, selectedDealer);
                        if (success)
                        {
                            SetDealer(selectedDealer);
                        }
                    }
                }
            }
        }
    }
}