using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading.Tasks;

namespace EmployeeExtender.UI
{
    internal class FlexiblePopup
    {
        public static event Action OnPopupCancelled;

        private static readonly Vector2 DEFAULT_POSITION = new Vector2(0.5f, 0.5f);
        private static GameObject popupObject;
        private static TaskCompletionSource<string> choiceTaskCompletionSource;

        public static void Initialize()
        {
            // Called once when the mod loads
        }

        public static Task<string> Show(string title, (string buttonText, string choiceResult)[] options)
        {
            return Show(title, options, DEFAULT_POSITION);
        }

        public static async Task<string> Show(string title, (string buttonText, string choiceResult)[] options, Vector2 position)
        {
            choiceTaskCompletionSource = new TaskCompletionSource<string>();

            if (popupObject == null)
            {
                CreateDialog(title, options, position);
            }
            else
            {
                UpdateDialog(title, options);
            }

            if (popupObject != null)
            {
                popupObject.SetActive(true);
            }
            else
            {
                MelonLogger.Error("Failed to create or find FlexiblePopup object.");
                choiceTaskCompletionSource.SetResult(null);
            }

            return await choiceTaskCompletionSource.Task;
        }

        private static void CreateDialog(string title, (string buttonText, string choiceResult)[] options)
        {
            CreateDialog(title, options, DEFAULT_POSITION);
        }

        private static void CreateDialog(string title, (string buttonText, string choiceResult)[] options, Vector2 position)
        {
            GameObject panel = null;
            RectTransform panelRect = null;

            try
            {
                popupObject = new GameObject("FlexiblePopup");
                GameObject.DontDestroyOnLoad(popupObject);

                Canvas canvas = popupObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                popupObject.AddComponent<GraphicRaycaster>();

                panel = new GameObject("Panel");
                panel.transform.SetParent(popupObject.transform, false);
                Image panelImage = panel.AddComponent<Image>();
                panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
                panelRect = panel.GetComponent<RectTransform>();
                panelRect.anchorMin = position;
                panelRect.anchorMax = position;
                panelRect.pivot = position;
                panelRect.sizeDelta = new Vector2(300, 200 + (options.Length - 2) * 50);
                panelRect.anchoredPosition = position;

                CreateText(panel.transform, "Title", title, 18, new Vector2(0, panelRect.sizeDelta.y / 2f - 30));

                float buttonSpacing = 60f;
                float startY = (options.Length - 1) * buttonSpacing / 2f - 30f;

                for (int i = 0; i < options.Length; i++)
                {
                    int index = i;
                    CreateButton(panel.transform, $"OptionButton_{i}", options[i].buttonText, new Vector2(0, startY - i * buttonSpacing), () => OnOptionButtonClicked(options[index].choiceResult));
                }

                CreateButton(panel.transform, "CloseButton", "X", new Vector2(panelRect.sizeDelta.x / 2f - 15, panelRect.sizeDelta.y / 2f - 15), OnCloseButtonClicked, new Vector2(30, 30));

                popupObject.SetActive(false);
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Error creating FlexiblePopup: {e.Message}\n{e.StackTrace}");
                if (popupObject != null) GameObject.Destroy(popupObject);
                popupObject = null;
                choiceTaskCompletionSource?.TrySetResult(null);
            }
        }

        private static void UpdateDialog(string title, (string buttonText, string choiceResult)[] options)
        {
            if (popupObject == null)
            {
                CreateDialog(title, options);
                return;
            }

            Text titleText = popupObject.transform.Find("Panel/Title")?.GetComponent<Text>();
            if (titleText != null)
            {
                titleText.text = title;
            }

            RectTransform panelRect = popupObject.transform.Find("Panel")?.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.sizeDelta = new Vector2(300, 200 + (options.Length - 2) * 50);

                for (int i = 0; i < 20; i++)
                {
                    Transform existingButton = panelRect.transform.Find($"OptionButton_{i}");
                    if (existingButton != null)
                    {
                        GameObject.Destroy(existingButton.gameObject);
                    }
                }

                float buttonSpacing = 60f;
                float startY = (options.Length - 1) * buttonSpacing / 2f - 30f;

                for (int i = 0; i < options.Length; i++)
                {
                    int index = i;
                    CreateButton(panelRect.transform, $"OptionButton_{i}", options[i].buttonText, new Vector2(0, startY - i * buttonSpacing), () => OnOptionButtonClicked(options[index].choiceResult));
                }
            }
        }

        public static GameObject CreateButton(Transform parent, string name, string buttonText, Vector2 position, Action onClickAction, Vector2? size = null)
        {
            Vector2 buttonSize = size ?? new Vector2(200, 40);

            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);

            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener((System.Action)onClickAction);

            GameObject textObj = CreateText(buttonObj.transform, "Text", buttonText, 14, Vector2.zero);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = position;
            buttonRect.sizeDelta = buttonSize;

            return buttonObj;
        }

        public static GameObject CreateText(Transform parent, string name, string textContent, int fontSize, Vector2 position)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            Text textComp = textObj.AddComponent<Text>();
            textComp.text = textContent;
            textComp.fontSize = fontSize;
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.color = Color.white;
            textComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            if (textComp.font == null)
            {
                MelonLogger.Warning("Arial.ttf not found. UI Text may not render correctly.");
            }

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = position;
            textRect.sizeDelta = new Vector2(280, 30);

            return textObj;
        }

        private static void OnOptionButtonClicked(string choiceResult)
        {
            choiceTaskCompletionSource?.TrySetResult(choiceResult);
            if (popupObject != null) popupObject.SetActive(false);
            MelonLogger.Msg($"Option '{choiceResult}' selected");
        }

        private static void OnCloseButtonClicked()
        {
            choiceTaskCompletionSource?.TrySetResult(null);
            if (popupObject != null) popupObject.SetActive(false);
            MelonLogger.Msg("Popup closed");
            OnPopupCancelled?.Invoke();
        }

        public static void ClosePopup()
        {
            if (popupObject != null)
            {
                popupObject.SetActive(false);
            }
            choiceTaskCompletionSource?.TrySetResult(null);
        }

        public static void Destroy()
        {
            if (popupObject != null)
            {
                GameObject.Destroy(popupObject);
                popupObject = null;
            }
            choiceTaskCompletionSource?.TrySetResult(null);
            OnPopupCancelled = null;
        }

        public static System.Collections.IEnumerator ShowPopupAndWaitForResult(
            string title,
            (string buttonText, string choiceResult)[] options,
            Action<string> resultCallback)
        {
            return ShowPopupAndWaitForResult(title, options, resultCallback, DEFAULT_POSITION);
        }

        public static System.Collections.IEnumerator ShowPopupAndWaitForResult(
            string title,
            (string buttonText, string choiceResult)[] options,
            Action<string> resultCallback,
            Vector2 position)
        {
            Task<string> choiceTask = FlexiblePopup.Show(title, options, position);

            while (!choiceTask.IsCompleted)
            {
                if (choiceTask.IsFaulted || choiceTask.IsCanceled)
                {
                    resultCallback(null);
                    yield break;
                }
                yield return null;
            }

            resultCallback(choiceTask.Result);
            FlexiblePopup.ClosePopup();
            yield return new WaitForSeconds(0.2f);
        }
    }
}
