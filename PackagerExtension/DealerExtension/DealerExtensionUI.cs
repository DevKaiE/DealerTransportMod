using Il2CppScheduleOne.UI;
using UnityEngine.UI;
using UnityEngine;
using EmployeeExtender.UI;
using Il2CppScheduleOne.Economy;
using Il2CppSystem;
using Il2CppScheduleOne.Storage;
using EmployeeExtender.Utils;
using MelonLoader;

namespace PackagerExtension.DealerExtension
{
    public class DealerExtensionUI
    {
        public Il2CppSystem.Guid UIGUID { get; private set; }
        public StorageMenu StorageMenu { get; private set; }
        public StorageEntity StorageEntity { get; private set; }
        public Dealer AssignedDealer { get; private set; }
        public GameObject DealerUIObject { get; private set; }
        public GameObject Button { get; private set; }

        public DealerExtensionUI(StorageMenu storageMenu, StorageEntity storageEntity)
        {
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

        public void CreateDealerStorageUI(StorageMenu menu, Dealer assignedDealer)
        {
            GameObject panel = null;
            RectTransform panelRect = null;

            DealerUIObject = new GameObject("Dealer");
            GameObject.DontDestroyOnLoad(DealerUIObject);

            Canvas canvas = DealerUIObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            DealerUIObject.AddComponent<GraphicRaycaster>();

            panel = new GameObject("Panel");
            panel.transform.SetParent(DealerUIObject.transform, false);
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.7f, 0.5f);
            panelRect.anchorMax = new Vector2(0.7f, 0.5f);
            panelRect.pivot = new Vector2(0.7f, 0.5f);
            panelRect.sizeDelta = new Vector2(300, 400);
            panelRect.anchoredPosition = Vector2.zero;

            // Title
            FlexiblePopup.CreateText(panel.transform, "Title", "Assigned Dealer", 18, new Vector2(0, panelRect.sizeDelta.y / 2f - 30));

            // Button
            float buttonSpacing = 60f;
            float startY = 0f;
            string buttonText = assignedDealer != null ? assignedDealer.fullName : "No Dealer";

            // Uncomment this line and make sure it handles null assignedDealer
            Button = FlexiblePopup.CreateButton(panel.transform, $"AssignedDealer", buttonText, new Vector2(0, startY - buttonSpacing), () => OnAssignedDealerButtonClicked());
            FlexiblePopup.CreateButton(panel.transform, $"ClearButton", "Clear", new Vector2(0, -60 - buttonSpacing), () => OnClearButtonClicked());
        }

        public void ToggleUI(bool open)
        {
            SetDealer(AssignedDealer);
            DealerUIObject.SetActive(open);
        }

        public void SetDealer(Dealer dealer)
        {
            AssignedDealer = dealer;
            if (Button != null)
            {
                Button.GetComponentInChildren<Text>().text = dealer != null ? dealer.fullName : "No Dealer";
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

            yield return FlexiblePopup.ShowPopupAndWaitForResult(
                "Choose Dealer",
                dealerOptions.ToArray(),
                result => dealerChoice = result
            );

            // Handle dealer selection
            if (!string.IsNullOrEmpty(dealerChoice) && dealerChoice != "None")
            {
                Core.MelonLogger.Msg($"Selected dealer: {dealerChoice}");
                Dealer selectedDealer = GameUtils.GetAllDealers()
                    .FirstOrDefault(d => d.name == dealerChoice);
                if (selectedDealer != null)
                {
                    Core.DealerStorageManager.SetDealerToStorage(StorageEntity, selectedDealer);
                    SetDealer(selectedDealer);
                }
            }
        }
    }
}
