using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Diploma.UI
{
    /// <summary>
    /// Автоматически создаёт/проверяет UI элементы для настроек.
    /// Присоединить к UI_Managers GameObject в MainMenu сцене.
    /// </summary>
    public class SettingsUISetup : MonoBehaviour
    {
        [Header("References (optional - will auto-find if empty)")]
        public MainMenuManager mainMenuManager;
        public SettingsManager settingsManager;

        [Header("Prefabs")]
        public GameObject settingItemPrefab;

        private void Awake()
        {
            Debug.Log("[SettingsUISetup] Awake started");
            EnsureSettingsButton();
            EnsureSettingsPanel();
        }

        private void EnsureSettingsButton()
        {
            Debug.Log("[SettingsUISetup] EnsureSettingsButton started");

            // Получаем MainMenuManager если не назначен
            if (mainMenuManager == null)
            {
                mainMenuManager = GetComponent<MainMenuManager>();
                Debug.Log($"[SettingsUISetup] GetComponent<MainMenuManager>: {(mainMenuManager != null ? "found" : "not found")}");
            }

            if (mainMenuManager == null)
            {
                Debug.LogError("[SettingsUISetup] MainMenuManager not found! Assign it in inspector or ensure it's on the same GameObject.");
                return;
            }

            // Если кнопка уже назначена, ничего не делаем
            if (mainMenuManager.settingsButton != null)
            {
                Debug.Log("[SettingsUISetup] settingsButton already assigned, skipping.");
                return;
            }

            // Ищем MainMenuPanel
            Transform mainMenuPanel = null;
            
            // Сначала ищем среди детей этого GameObject
            mainMenuPanel = transform.Find("MainMenuPanel");
            if (mainMenuPanel == null)
            {
                // Затем ищем в сцене
                GameObject panelGO = GameObject.Find("MainMenuPanel");
                if (panelGO != null)
                    mainMenuPanel = panelGO.transform;
            }

            if (mainMenuPanel == null)
            {
                Debug.LogError("[SettingsUISetup] MainMenuPanel not found in scene! Expected under Canvas.");
                return;
            }

            Debug.Log("[SettingsUISetup] Creating SettingsButton...");

            // Создаём кнопку
            GameObject buttonObj = new GameObject("SettingsButton", 
                typeof(RectTransform), 
                typeof(CanvasRenderer), 
                typeof(Image), 
                typeof(Button), 
                typeof(TMP_Text));

            if (buttonObj == null)
            {
                Debug.LogError("[SettingsUISetup] Failed to create SettingsButton GameObject!");
                return;
            }

            // Назначаем родителя
            buttonObj.transform.SetParent(mainMenuPanel, false);

            // Настраиваем RectTransform
            RectTransform rt = buttonObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0);
                rt.anchorMax = new Vector2(0.5f, 0);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, 60);
                rt.sizeDelta = new Vector2(180, 40);
            }

            // Настраиваем Image (фон)
            Image img = buttonObj.GetComponent<Image>();
            if (img != null)
                img.color = new Color(0.129f, 0.588f, 0.953f, 1f);

            // Настраиваем Text (TMP)
            TMP_Text txt = buttonObj.GetComponent<TMP_Text>();
            if (txt != null)
            {
                txt.text = "SETTINGS ⚙️";
                txt.fontSize = 20;
                txt.color = Color.white;
                txt.alignment = TextAlignmentOptions.Center;
                if (txt.rectTransform != null)
                {
                    txt.rectTransform.anchorMin = Vector2.zero;
                    txt.rectTransform.anchorMax = Vector2.one;
                    txt.rectTransform.sizeDelta = Vector2.zero;
                }
                txt.raycastTarget = false;
            }

            // Назначаем кнопку
            Button btn = buttonObj.GetComponent<Button>();
            if (btn != null)
            {
                mainMenuManager.settingsButton = btn;
                Debug.Log("[SettingsUISetup] SettingsButton created and assigned.");
            }
            else
            {
                Debug.LogError("[SettingsUISetup] Button component missing on SettingsButton!");
                Destroy(buttonObj);
            }
        }

        private void EnsureSettingsPanel()
        {
            Debug.Log("[SettingsUISetup] EnsureSettingsPanel started");

            if (settingsManager == null)
            {
                settingsManager = GetComponent<SettingsManager>();
                Debug.Log($"[SettingsUISetup] GetComponent<SettingsManager>: {(settingsManager != null ? "found" : "not found")}");
            }

            if (settingsManager == null)
            {
                Debug.LogError("[SettingsUISetup] SettingsManager not found! Assign it in inspector or ensure it's on the same GameObject.");
                return;
            }

            if (settingsManager.settingsPanel != null)
            {
                Debug.Log("[SettingsUISetup] settingsPanel already exists, checking content fix...");

                // Если панель уже есть, возможно settingsContent ссылается на несуществующий объект
                // Попробуем исправить ссылку
                TryFixSettingsContent(settingsManager);
                return;
            }

            // Ищем Canvas
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[SettingsUISetup] Canvas not found in scene!");
                return;
            }

            Debug.Log("[SettingsUISetup] Creating SettingsPanel...");

            GameObject panelObj = new GameObject("SettingsPanel", 
                typeof(RectTransform), 
                typeof(CanvasRenderer), 
                typeof(Image));
            panelObj.transform.SetParent(canvas.transform, false);

            RectTransform rt = panelObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(600, 700);
                rt.anchoredPosition = Vector2.zero;
            }

            Image img = panelObj.GetComponent<Image>();
            if (img != null)
                img.color = new Color(0.117f, 0.117f, 0.117f, 0.94f);

            settingsManager.settingsPanel = panelObj;

            CreateSettingsTitle(panelObj.transform);
            CreateCloseButton(panelObj.transform);
            CreateResetButton(panelObj.transform);
            CreateScrollView(panelObj.transform);

            Debug.Log("[SettingsUISetup] SettingsPanel created successfully.");
        }

        private void CreateSettingsTitle(Transform parent)
        {
            if (parent == null) return;

            GameObject titleObj = new GameObject("SettingsTitle", 
                typeof(RectTransform), 
                typeof(CanvasRenderer), 
                typeof(TMP_Text));
            titleObj.transform.SetParent(parent, false);

            RectTransform rt = titleObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 1);
                rt.anchorMax = new Vector2(0.5f, 1);
                rt.pivot = new Vector2(0.5f, 1);
                rt.anchoredPosition = new Vector2(0, -30);
                rt.sizeDelta = new Vector2(400, 50);
            }

            TMP_Text txt = titleObj.GetComponent<TMP_Text>();
            if (txt != null)
            {
                txt.text = "GENERATION SETTINGS";
                txt.fontSize = 32;
                txt.color = Color.white;
                txt.alignment = TextAlignmentOptions.Center;
                if (txt.rectTransform != null)
                {
                    txt.rectTransform.anchorMin = Vector2.zero;
                    txt.rectTransform.anchorMax = Vector2.one;
                    txt.rectTransform.sizeDelta = Vector2.zero;
                }
            }
        }

        private void CreateCloseButton(Transform parent)
        {
            if (parent == null) return;

            GameObject btnObj = new GameObject("CloseButton", 
                typeof(RectTransform), 
                typeof(CanvasRenderer), 
                typeof(Image), 
                typeof(Button), 
                typeof(TMP_Text));
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(1, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(1, 1);
                rt.anchoredPosition = new Vector2(-20, -10);
                rt.sizeDelta = new Vector2(40, 40);
            }

            Image img = btnObj.GetComponent<Image>();
            if (img != null)
                img.color = new Color(0.784f, 0.196f, 0.196f, 1f);

            TMP_Text txt = btnObj.GetComponent<TMP_Text>();
            if (txt != null)
            {
                txt.text = "✕";
                txt.fontSize = 24;
                txt.color = Color.white;
                txt.alignment = TextAlignmentOptions.Center;
                if (txt.rectTransform != null)
                {
                    txt.rectTransform.anchorMin = Vector2.zero;
                    txt.rectTransform.anchorMax = Vector2.one;
                    txt.rectTransform.sizeDelta = Vector2.zero;
                }
            }
        }

        private void CreateResetButton(Transform parent)
        {
            if (parent == null) return;

            GameObject btnObj = new GameObject("ResetButton", 
                typeof(RectTransform), 
                typeof(CanvasRenderer), 
                typeof(Image), 
                typeof(Button), 
                typeof(TMP_Text));
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0);
                rt.anchorMax = new Vector2(0.5f, 0);
                rt.pivot = new Vector2(0.5f, 0);
                rt.anchoredPosition = new Vector2(0, 30);
                rt.sizeDelta = new Vector2(200, 40);
            }

            Image img = btnObj.GetComponent<Image>();
            if (img != null)
                img.color = new Color(1f, 0.596f, 0f, 1f);

            TMP_Text txt = btnObj.GetComponent<TMP_Text>();
            if (txt != null)
            {
                txt.text = "RESET TO DEFAULTS";
                txt.fontSize = 18;
                txt.color = Color.white;
                txt.alignment = TextAlignmentOptions.Center;
                if (txt.rectTransform != null)
                {
                    txt.rectTransform.anchorMin = Vector2.zero;
                    txt.rectTransform.anchorMax = Vector2.one;
                    txt.rectTransform.sizeDelta = Vector2.zero;
                }
            }

            // Назначим кнопку в SettingsManager позже в CreateScrollView
        }

        private void CreateScrollView(Transform parent)
        {
            GameObject svObj = new GameObject("SettingsScrollView", 
                typeof(RectTransform), 
                typeof(Image), 
                typeof(ScrollRect));
            svObj.transform.SetParent(parent, false);

            RectTransform rt = svObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = new Vector2(20, 80);
                rt.offsetMax = new Vector2(-20, -80);
            }

            Image img = svObj.GetComponent<Image>();
            if (img != null)
                img.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

            ScrollRect scroll = svObj.GetComponent<ScrollRect>();
            if (scroll != null)
            {
                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped;
            }

            // Viewport
            GameObject viewportObj = new GameObject("Viewport", 
                typeof(RectTransform), 
                typeof(Image), 
                typeof(Mask));
            viewportObj.transform.SetParent(svObj.transform, false);

            RectTransform vpRt = viewportObj.GetComponent<RectTransform>();
            if (vpRt != null)
            {
                vpRt.anchorMin = Vector2.zero;
                vpRt.anchorMax = Vector2.one;
                vpRt.sizeDelta = Vector2.zero;
                vpRt.pivot = new Vector2(0, 1);
            }

            Image vpImg = viewportObj.GetComponent<Image>();
            if (vpImg != null)
                vpImg.color = new Color(0.1f, 0.1f, 0.1f, 0f);

            Mask mask = viewportObj.GetComponent<Mask>();
            if (mask != null)
                mask.showMaskGraphic = false;

            // Content
            GameObject contentObj = new GameObject("SettingsContent", 
                typeof(RectTransform), 
                typeof(VerticalLayoutGroup));
            contentObj.transform.SetParent(viewportObj.transform, false);

            RectTransform ctRt = contentObj.GetComponent<RectTransform>();
            if (ctRt != null)
            {
                ctRt.anchorMin = new Vector2(0, 1);
                ctRt.anchorMax = new Vector2(1, 1);
                ctRt.pivot = new Vector2(0.5f, 1);
                ctRt.sizeDelta = new Vector2(0, 0);
                ctRt.anchoredPosition = Vector2.zero;
            }

            VerticalLayoutGroup vlg = contentObj.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.padding.top = 10;
                vlg.padding.bottom = 10;
                vlg.padding.left = 10;
                vlg.padding.right = 10;
                vlg.spacing = 10;
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
            }

            ContentSizeFitter csf = contentObj.GetComponent<ContentSizeFitter>();
            if (csf != null)
            {
                csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            // Настраиваем ScrollRect
            if (scroll != null)
            {
                scroll.viewport = vpRt;
                scroll.content = ctRt;
            }

            // Назначаем в SettingsManager
            Transform existingContent = settingsManager.settingsContent;
            if (existingContent == null || existingContent.parent != viewportObj.transform)
            {
                settingsManager.settingsContent = contentObj.transform;
                Debug.Log($"[SettingsUISetup] Assigned settingsContent (SettingsContent) to SettingsManager.");
            }
            else if (existingContent != contentObj.transform)
            {
                Debug.LogWarning($"[SettingsUISetup] settingsContent exists as '{existingContent.name}' under Viewport. " +
                    $"Overwriting to '{contentObj.name}'. Rename back in inspector if needed.");
                settingsManager.settingsContent = contentObj.transform;
            }

            // Назначаем кнопки Close и Reset
            Transform closeBtn = parent.Find("CloseButton");
            if (closeBtn != null)
            {
                Button closeBtnComp = closeBtn.GetComponent<Button>();
                if (closeBtnComp != null)
                    settingsManager.closeButton = closeBtnComp;
            }

            Transform resetBtn = parent.Find("ResetButton");
            if (resetBtn != null)
            {
                Button resetBtnComp = resetBtn.GetComponent<Button>();
                if (resetBtnComp != null)
                    settingsManager.resetButton = resetBtnComp;
            }

            Debug.Log("[SettingsUISetup] SettingsPanel setup complete.");
        }

        /// <summary>
        /// Исправляет settingsContent ссылку если она указывает на несуществующий объект.
        /// Вызывается даже если панель уже существует в сцене.
        /// </summary>
        private void TryFixSettingsContent(SettingsManager sm)
        {
            if (sm.settingsContent == null || sm.settingsContent.gameObject == null)
            {
                Debug.LogWarning("[SettingsUISetup] settingsContent is null, searching via ScrollRect...");

                // Ищем ScrollRect в панели и берём его content
                ScrollRect scroll = sm.settingsPanel.GetComponentInChildren<ScrollRect>(true);
                if (scroll != null && scroll.content != null)
                {
                    sm.settingsContent = scroll.content;
                    Debug.Log($"[SettingsUISetup] Fixed settingsContent via ScrollRect.content: {scroll.content.name}");
                }
                else
                {
                    Debug.LogError("[SettingsUISetup] Could not fix settingsContent — no ScrollRect or ScrollRect.content found!");
                }
                return;
            }

            // Проверяем, что content находится внутри ScrollView → Viewport
            Transform current = sm.settingsContent;
            bool foundScrollView = false;
            while (current != null)
            {
                if (current.name.Contains("SettingsScrollView"))
                {
                    foundScrollView = true;
                    break;
                }
                current = current.parent;
            }

            if (!foundScrollView)
            {
                Debug.LogWarning($"[SettingsUISetup] settingsContent path looks wrong (not inside SettingsScrollView). Attempting auto-fix...");

                // Находим ScrollRect в панели
                ScrollRect scroll = sm.settingsPanel.GetComponentInChildren<ScrollRect>(true);
                if (scroll != null && scroll.content != null)
                {
                    sm.settingsContent = scroll.content;
                    Debug.Log($"[SettingsUISetup] Fixed settingsContent to: {scroll.content.name}");
                }
            }
        }
    }
}
