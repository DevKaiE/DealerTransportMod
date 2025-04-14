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
using System.Collections.Generic;

namespace DealerSelfSupplySystem.DealerExtension
{
    public class DealerExtensionUI
    {
        public Il2CppSystem.Guid UIGUID { get; private set; }
        public StorageMenu StorageMenu { get; private set; }
        public StorageEntity StorageEntity { get; private set; }
        public List<DealerExtendedBrain> AssignedDealers { get; private set; }
        public GameObject DealerUIObject { get; private set; }
        public GameObject ScrollView { get; private set; }
        public GameObject Content { get; private set; }
        public GameObject AddButton { get; private set; }
        public GameObject MainPanel { get; private set; }
        private bool isExpanded = false;
        private bool popupOpen = false;
        private bool closeUIDefault;
        private static KeyCode toggleKey; // Configurable toggle key
        private GameObject noAssignmentText;
        private float dealerItemHeight = 40f;
        private Dictionary<DealerExtendedBrain, GameObject> dealerListItems = new Dictionary<DealerExtendedBrain, GameObject>();

        public DealerExtensionUI(StorageMenu storageMenu, StorageEntity storageEntity)
        {
            toggleKey = Config.uiToggleKey.Value; // Get the toggle key from config
            closeUIDefault = Config.dealerStorageUIClosedByDefault.Value;
            UIGUID = new Il2CppSystem.Guid();
            StorageMenu = storageMenu;
            StorageEntity = storageEntity;
            AssignedDealers = new List<DealerExtendedBrain>();

            // Ensure storageEntity isn't null before proceeding
            if (storageEntity != null)
            {
                AssignedDealers = Core.DealerStorageManager.GetDealersFromStorage(storageEntity);
                CreateDealerStorageUI(storageMenu);
            }
            else
            {
                Core.MelonLogger.Error("StorageEntity is null in DealerExtensionUI constructor");
            }
        }

        public void CreateDealerStorageUI(StorageMenu menu)
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
            CreateMainPanel();

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

        private void CreateMainPanel()
        {
            MainPanel = new GameObject("Panel");
            MainPanel.transform.SetParent(DealerUIObject.transform, false);

            Image panelImage = MainPanel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            RectTransform panelRect = MainPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1, 1);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.pivot = new Vector2(1, 1);
            panelRect.sizeDelta = new Vector2(220, 300); // Larger to accommodate the scroll view
            panelRect.anchoredPosition = new Vector2(-10, -50);

            // Title - moved higher to avoid clipping with the scroll view
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(MainPanel.transform, false);
            Text titleText = titleObj.AddComponent<Text>();
            titleText.text = "Dealer Assignments";
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = 16;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;

            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 30);
            titleRect.anchoredPosition = new Vector2(0, 0);

            // "No dealers assigned" text (shown when list is empty)
            noAssignmentText = new GameObject("NoAssignmentText");
            noAssignmentText.transform.SetParent(MainPanel.transform, false);
            Text noAssignmentTextComp = noAssignmentText.AddComponent<Text>();
            noAssignmentTextComp.text = "No dealers assigned";
            noAssignmentTextComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            noAssignmentTextComp.fontSize = 14;
            noAssignmentTextComp.alignment = TextAnchor.MiddleCenter;
            noAssignmentTextComp.color = Color.white;

            RectTransform noAssignmentRect = noAssignmentText.GetComponent<RectTransform>();
            noAssignmentRect.anchorMin = new Vector2(0, 0.5f);
            noAssignmentRect.anchorMax = new Vector2(1, 0.5f);
            noAssignmentRect.pivot = new Vector2(0.5f, 0.5f);
            noAssignmentRect.sizeDelta = new Vector2(0, 30);
            noAssignmentRect.anchoredPosition = new Vector2(0, 0);

            // Create ScrollView for dealer list
            CreateScrollView();

            // Add "Add Dealer" button at the bottom
            AddButton = new GameObject("AddDealerButton");
            AddButton.transform.SetParent(MainPanel.transform, false);
            Image addButtonImage = AddButton.AddComponent<Image>();
            addButtonImage.color = Styling.PRIMARY_COLOR;

            Button addButtonComp = AddButton.AddComponent<Button>();
            addButtonComp.targetGraphic = addButtonImage;
            addButtonComp.onClick.AddListener((System.Action)OnAddDealerButtonClicked);

            GameObject addButtonText = new GameObject("Text");
            addButtonText.transform.SetParent(AddButton.transform, false);
            Text addText = addButtonText.AddComponent<Text>();
            addText.text = "Add Dealer";
            addText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            addText.fontSize = 14;
            addText.alignment = TextAnchor.MiddleCenter;
            addText.color = Color.white;

            RectTransform addTextRect = addButtonText.GetComponent<RectTransform>();
            addTextRect.anchorMin = Vector2.zero;
            addTextRect.anchorMax = Vector2.one;
            addTextRect.sizeDelta = Vector2.zero;

            RectTransform addButtonRect = AddButton.GetComponent<RectTransform>();
            addButtonRect.anchorMin = new Vector2(0, 0);
            addButtonRect.anchorMax = new Vector2(1, 0);
            addButtonRect.pivot = new Vector2(0.5f, 0);
            addButtonRect.sizeDelta = new Vector2(-40, 40);
            addButtonRect.anchoredPosition = new Vector2(0, 5);

            // Populate the dealer list
            UpdateDealersList();
        }

        private void CreateScrollView()
        {
            // ScrollView container
            ScrollView = new GameObject("ScrollView");
            ScrollView.transform.SetParent(MainPanel.transform, false);

            // Position the scroll view - moved down to accommodate the title
            RectTransform scrollRectTransform = ScrollView.GetComponent<RectTransform>() ?? ScrollView.AddComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0, 0);
            scrollRectTransform.anchorMax = new Vector2(1, 1);
            scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
            scrollRectTransform.sizeDelta = new Vector2(-20, -110); // Make room for title and button
            scrollRectTransform.anchoredPosition = new Vector2(0, -10); // Move down to avoid overlapping title

            Image scrollImage = ScrollView.AddComponent<Image>();
            scrollImage.color = new Color(0.05f, 0.05f, 0.05f, 0.3f); // More transparent background

            ScrollRect scrollRect = ScrollView.AddComponent<ScrollRect>();

            // Create viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(ScrollView.transform, false);

            RectTransform viewportRect = viewport.GetComponent<RectTransform>() ?? viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.pivot = new Vector2(0.5f, 0.5f);

            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0.1f, 0.1f, 0.1f, 0.01f); // Almost invisible

            Mask viewportMask = viewport.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            // Content container (where dealer items will be added)
            Content = new GameObject("Content");
            Content.transform.SetParent(viewport.transform, false);

            RectTransform contentRect = Content.GetComponent<RectTransform>() ?? Content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 10); // Initial height that will be expanded by content
            contentRect.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup verticalLayout = Content.AddComponent<VerticalLayoutGroup>();
            verticalLayout.spacing = 8f; // Increased spacing between items
            verticalLayout.padding = new RectOffset(10, 10, 5, 5); // Less vertical padding
            verticalLayout.childAlignment = TextAnchor.UpperCenter;
            verticalLayout.childControlHeight = false;
            verticalLayout.childForceExpandHeight = false;
            verticalLayout.childControlWidth = true; // Control child width
            verticalLayout.childForceExpandWidth = true; // Force children to expand width

            ContentSizeFitter sizeFitter = Content.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Setup scroll rect references
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 20f;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
        }

        private GameObject CreateDealerListItem(DealerExtendedBrain dealer)
        {
            GameObject itemObj = new GameObject($"DealerItem_{dealer.Dealer.name}");
            itemObj.transform.SetParent(Content.transform, false);

            // Set up the item background with a rounded look
            Image itemBg = itemObj.AddComponent<Image>();
            itemBg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            // Stretch the item to fill the width of the container
            LayoutElement layoutElement = itemObj.AddComponent<LayoutElement>();
            layoutElement.minHeight = dealerItemHeight;
            layoutElement.preferredHeight = dealerItemHeight;
            layoutElement.flexibleWidth = 1;

            // Important: fixed height for the item
            RectTransform itemRect = itemObj.GetComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0, dealerItemHeight);

            // Create dealer name text
            GameObject textObj = new GameObject("DealerName");
            textObj.transform.SetParent(itemObj.transform, false);
            Text nameText = textObj.AddComponent<Text>();
            nameText.text = dealer.Dealer.fullName;
            nameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            nameText.fontSize = 14;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.pivot = new Vector2(0, 0.5f);
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-30, 0);

            // Create remove button with a cleaner look
            GameObject removeBtn = new GameObject("RemoveButton");
            removeBtn.transform.SetParent(itemObj.transform, false);
            Image removeBtnImg = removeBtn.AddComponent<Image>();
            removeBtnImg.color = Styling.DESTRUCTIVE_COLOR;

            Button removeButton = removeBtn.AddComponent<Button>();
            removeButton.targetGraphic = removeBtnImg;

            // Add hover effect
            ColorBlock colors = removeButton.colors;
            colors.normalColor = Styling.DESTRUCTIVE_COLOR;
            colors.highlightedColor = new Color(1f, 0.3f, 0.3f, 1f); // Brighter red on hover
            removeButton.colors = colors;

            removeButton.onClick.AddListener((System.Action)(() => RemoveDealerFromStorage(dealer)));

            // "X" text for remove button
            GameObject xTextObj = new GameObject("X");
            xTextObj.transform.SetParent(removeBtn.transform, false);
            Text xText = xTextObj.AddComponent<Text>();
            xText.text = "X";
            xText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            xText.fontSize = 14;
            xText.alignment = TextAnchor.MiddleCenter;
            xText.color = Color.white;

            RectTransform xTextRect = xTextObj.GetComponent<RectTransform>();
            xTextRect.anchorMin = Vector2.zero;
            xTextRect.anchorMax = Vector2.one;
            xTextRect.sizeDelta = Vector2.zero;

            // Position remove button
            RectTransform removeBtnRect = removeBtn.GetComponent<RectTransform>();
            removeBtnRect.anchorMin = new Vector2(1, 0.5f);
            removeBtnRect.anchorMax = new Vector2(1, 0.5f);
            removeBtnRect.pivot = new Vector2(1, 0.5f);
            removeBtnRect.anchoredPosition = new Vector2(-5, 0); // Moved closer to edge
            removeBtnRect.sizeDelta = new Vector2(24, 24);

            return itemObj;
        }

        private void RemoveDealerFromStorage(DealerExtendedBrain dealer)
        {
            Core.DealerStorageManager.RemoveDealerFromStorage(StorageEntity, dealer);
            UpdateDealersList();
        }

        private void UpdateDealersList()
        {
            // Clear existing items
            foreach (var item in dealerListItems.Values)
            {
                GameObject.Destroy(item);
            }
            dealerListItems.Clear();

            // Get current assigned dealers
            AssignedDealers = Core.DealerStorageManager.GetDealersFromStorage(StorageEntity);

            // Toggle visibility of "No dealers" text
            if (noAssignmentText != null)
            {
                noAssignmentText.SetActive(AssignedDealers.Count == 0);
            }

            // Create list items for each dealer
            foreach (var dealer in AssignedDealers)
            {
                if (dealer?.Dealer != null)
                {
                    GameObject listItem = CreateDealerListItem(dealer);
                    dealerListItems.Add(dealer, listItem);
                }
            }

            // Force layout rebuild
            if (Content != null)
            {
                // Update content height based on number of items
                RectTransform contentRect = Content.GetComponent<RectTransform>();
                if (contentRect != null)
                {
                    float totalHeight = (dealerItemHeight + 5) * AssignedDealers.Count + 20; // height + spacing per item + padding
                    contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, totalHeight);
                }

                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(Content.GetComponent<RectTransform>());
            }
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
            if (open) UpdateDealersList();

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

        public void OnAddDealerButtonClicked()
        {
            MelonCoroutines.Start(HandleDealerSelection());
        }

        private System.Collections.IEnumerator HandleDealerSelection()
        {
            // Check if we can add more dealers to this storage
            bool allowMultipleDealers = Config.multipleDealersPerStorage.Value;
            int maxDealersPerStorage = Config.maxDealersPerStorage.Value;

            // If we're at the max limit for dealers, show a message and return
            if (allowMultipleDealers && AssignedDealers.Count >= maxDealersPerStorage)
            {
                // Show a message informing the user this storage is at capacity
                yield return FlexiblePopup.ShowPopupAndWaitForResult(
                    "Storage Full",
                    new[] { ("This storage already has the maximum number of dealers. You can increase the limit in the mod config.", "OK") },
                    _ => { },
                    new Vector2(0.85f, 0.9f)
                );
                yield break;
            }

            // Get all recruitedDealers that aren't already assigned to this storage
            var assignedDealerIds = AssignedDealers.Select(d => d.Dealer.name).ToList();
            List<(string, string)> dealerOptions = GameUtils.GetRecruitedDealers()
                .Where(d => !assignedDealerIds.Contains(d.name)) // Filter out already assigned dealers
                .Select(d => (d.fullName, d.name))
                .ToList();

            // Add a fallback option if no dealers are found
            if (dealerOptions.Count == 0)
                dealerOptions.Add(("No more dealers available", "None"));

            string dealerChoice = null;

            // Position the popup slightly to the left of the main panel
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
                    bool success = false;

                    // If multiple dealers are not allowed, check if this dealer is already assigned elsewhere
                    if (!allowMultipleDealers)
                    {
                        // Check if this dealer is already assigned to another storage
                        List<StorageEntity> existingStorages = Core.DealerStorageManager.GetAssignedStoragesForDealer(selectedDealer);

                        if (existingStorages.Count > 0 && !existingStorages.Contains(StorageEntity))
                        {
                            // Dealer is already assigned to another storage, show a funny message
                            string message = Messages.GetRandomDealerAlreadyAssignedMessage();
                            selectedDealer.Dealer.SendTextMessage(message);
                        }
                        else
                        {
                            // Dealer is not assigned yet or is assigned to this storage, proceed with assignment
                            success = Core.DealerStorageManager.SetDealerToStorage(StorageEntity, selectedDealer);
                        }
                    }
                    else
                    {
                        // Multiple dealers per storage is enabled, so try adding it
                        success = Core.DealerStorageManager.SetDealerToStorage(StorageEntity, selectedDealer);
                    }

                    if (success)
                    {
                        UpdateDealersList();
                    }
                }
            }
        }
    }
}