using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using Diploma.Generation;

namespace Diploma.UI
{
    /// <summary>
    /// Менеджер настроек генерации. Динамически создаёт список параметров WorldGenConfig.
    /// Исключает: префабы, пороги биомов, октавы, минимальные расстояния, mapSize.
    /// Автосохранение в PlayerPrefs.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject settingsPanel;
        public Button settingsButton;
        public Button closeButton;
        public Button resetButton;
        public Transform settingsContent;
        public GameObject settingItemPrefab;

        [Header("Configuration")]
        public WorldGenConfig config;

        [Header("Excluded Categories")]
        public bool excludeBuildingPrefabs = true;
        public bool excludeTreePrefabs = true;
        public bool excludeLampPrefabs = true;
        public bool excludeBenchPrefabs = true;
        public bool excludeBiomeThresholds = true;
        public bool excludeBiomeOctaves = true;
        public bool excludeMinDistances = true;
        public bool excludeTerrainSmooth = true;

        [Header("Setting Item Layout")]
        [Tooltip("Размер шрифта текста лейбла")]
        public float labelFontSize = 20f;
        [Tooltip("Размер шрифта минимум (при автосайзе)")]
        public float labelFontSizeMin = 18f;
        [Tooltip("Размер шрифта максимум (при автосайзе)")]
        public float labelFontSizeMax = 28f;
        [Tooltip("Высота области лейбла в пикселях")]
        public float labelHeight = 35f;
        [Tooltip("Высота слайдера в пикселях")]
        public float sliderHeight = 32f;
        [Tooltip("Отступ между лейблом и слайдером в пикселях (внутри элемента)")]
        public float itemSpacing = 4f;
        [Tooltip("Отступ между соседними элементами настроек (item ↔ item)")]
        public float itemItemSpacing = 8f;
        [Tooltip("Отступ от левого края контейнера до текста лейбла")]
        public float labelLeftPadding = 8f;
        [Tooltip("Ширина области с названием параметра в пикселях")]
        public float labelWidth = 180f;
        [Tooltip("Общая высота элемента настройки в пикселях (0 = auto)")]
        public float itemTotalHeight = 0f;

        private Dictionary<string, Slider> parameterSliders = new Dictionary<string, Slider>();
        private Dictionary<string, TMP_Text> parameterLabels = new Dictionary<string, TMP_Text>();
        private Dictionary<string, object> defaultValues = new Dictionary<string, object>();
        private bool isPanelOpen = false;

        private const string SETTINGS_PREFIX = "WorldGenConfig_";

        private void Awake()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);

            CacheDefaultValues();
        }

        private void Start()
        {
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OpenSettings);
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseSettings);
            if (resetButton != null)
                resetButton.onClick.AddListener(ResetToDefaults);
        }

        private void OnDestroy()
        {
            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(OpenSettings);
            if (closeButton != null)
                closeButton.onClick.RemoveListener(CloseSettings);
            if (resetButton != null)
                resetButton.onClick.RemoveListener(ResetToDefaults);
        }

        private void CacheDefaultValues()
        {
            if (config == null) return;

            defaultValues.Clear();
            var fields = config.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.IsInitOnly) continue;
                defaultValues[field.Name] = field.GetValue(config);
            }
        }

            public void OpenSettings()
            {
                if (settingsPanel == null || config == null || settingItemPrefab == null || settingsContent == null)
                {
                    Debug.LogError("[SettingsManager] Missing references! " +
                        $"settingsPanel={settingsPanel != null}, config={config != null}, " +
                        $"settingItemPrefab={settingItemPrefab != null}, settingsContent={settingsContent != null}. " +
                        $"Assign all references in inspector.");
                    return;
                }

                // Safety: repair a stale or broken settingsContent
                if (settingsContent == null || settingsContent.gameObject == null)
                {
                    Debug.LogWarning("[SettingsManager] settingsContent is null or destroyed, searching for ScrollRect...");
                    ScrollRect scroll = settingsPanel.GetComponentInChildren<ScrollRect>(true);
                    if (scroll != null && scroll.content != null)
                    {
                        settingsContent = scroll.content;
                        Debug.Log($"[SettingsManager] Recovered settingsContent from ScrollRect: {settingsContent.name}");
                    }
                    else
                    {
                        Debug.LogError("[SettingsManager] Cannot recover settingsContent! No ScrollRect found.");
                        return;
                    }
                }

                // Ensure the content is still alive and inside the scroll view viewport
                if (settingsContent != null && !IsContentInsideScrollView(settingsContent))
                {
                    Debug.LogWarning("[SettingsManager] settingsContent is not inside SettingsScrollView/Viewport, re-finding...");
                    ScrollRect scrollForRelink = settingsPanel.GetComponentInChildren<ScrollRect>(true);
                    if (scrollForRelink != null && scrollForRelink.content != null)
                    {
                        settingsContent = scrollForRelink.content;
                        Debug.Log($"[SettingsManager] Re-linked settingsContent to: {settingsContent.name}");
                    }
                }

                // Fix broken ScrollRect / Viewport that clips everything
                // Fix broken ScrollRect / Viewport that clips everything
                FixScrollRect();

                // Configure SettingsContent VLG per inspector parameters
                if (settingsContent != null)
                {
                    VerticalLayoutGroup contentVLG = settingsContent.GetComponent<VerticalLayoutGroup>();
                    if (contentVLG != null)
                    {
                        contentVLG.spacing = itemItemSpacing;          // отступ между item'ами
                        contentVLG.padding = new RectOffset(              // отступ от левого края контейнера
                            Mathf.RoundToInt(labelLeftPadding),    // left — не обрезает, округляет
                            Mathf.RoundToInt(labelLeftPadding * 0.5f), // right — немного меньше
                            Mathf.RoundToInt(labelLeftPadding * 0.5f), // top
                            Mathf.RoundToInt(labelLeftPadding * 0.5f)  // bottom
                        );
                        contentVLG.childAlignment = TextAnchor.UpperLeft;  // не центрировать, прижимать к верху-слева
                        contentVLG.childControlWidth = true;               // разрешаем VLG контролировать ширину (все равно false-expand)
                        contentVLG.childControlHeight = true;
                        contentVLG.childForceExpandWidth = true;           // дети растягиваются на всю доступную ширину
                        contentVLG.childForceExpandHeight = false;
                        Debug.Log($"[SettingsManager] Configured SettingsContent VLG: spacing={itemItemSpacing}, " +
                            $"padding.left={labelLeftPadding}, upperForceExpandWidth=true");
                    }
                }

                // Force Viewport VLG to compute its final width BEFORE children are created
                ScrollRect srForViewport = settingsPanel.GetComponentInChildren<ScrollRect>(true);
                if (srForViewport != null && srForViewport.viewport != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(srForViewport.viewport.GetComponent<RectTransform>());
                    Debug.Log("[SettingsManager] Viewport VLG rebuilt. Viewport rect=" + srForViewport.viewport.GetComponent<RectTransform>().rect);
                }
                LoadSettings();

                foreach (Transform child in settingsContent)
                    Destroy(child.gameObject);

                parameterSliders.Clear();
                parameterLabels.Clear();

                CreateSettingItems();

                // Force settingsContent VLG to recompute children widths/heights after items are created
                if (settingsContent != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(settingsContent.GetComponent<RectTransform>());
                    Debug.Log("[SettingsManager] SettingsContent VLG rebuilt. Content rect=" + settingsContent.GetComponent<RectTransform>().rect);
                }

                 // Force layout and canvas system to update,
                 // otherwise ScrollRect keeps stale content positions from before items were added
                UnityEngine.Canvas.ForceUpdateCanvases();

                 if (settingsContent != null && settingsContent is RectTransform contentRtForDiag)
                {
                    // Diagnose layout chain before rebuild
                    VerticalLayoutGroup vlg = settingsContent.GetComponent<VerticalLayoutGroup>();
                    ContentSizeFitter csf = settingsContent.GetComponent<ContentSizeFitter>();
                    LayoutElement contentLe = settingsContent.GetComponent<LayoutElement>();
                    Debug.Log($"[SettingsManager] Pre-rebuild: content rect={contentRtForDiag.rect}, " +
                        $"VLG={vlg != null}, CSF={csf != null}, " +
                        $"CSF_verticalFit={(csf != null ? csf.verticalFit.ToString() : "N/A")}, " +
                        $"VLG_childControlHeight={(vlg != null ? vlg.childControlHeight.ToString() : "N/A")}, " +
                        $"content LayoutElement={(contentLe != null ? $"exists preferredH={contentLe.preferredHeight}" : "null")}");

                    int childCount = settingsContent.childCount;
                    Debug.Log($"[SettingsManager] Content has {childCount} children (first 5):");
                    for (int i = 0; i < childCount && i < 5; i++)
                    {
                        Transform child = settingsContent.GetChild(i);
                        LayoutElement le = child.GetComponent<LayoutElement>();
                        RectTransform crt = child.GetComponent<RectTransform>();
                        Debug.Log($"[SettingsManager] Child[{i}] '{child.name}': " +
                            $"LE={le != null}, preferredH={(le != null ? le.preferredHeight.ToString("F1") : "N/A")}, " +
                            $"RT_sizeDelta={(crt != null ? crt.sizeDelta.ToString() : "N/A")}, " +
                            $"RT_anchoredPos={(crt != null ? crt.anchoredPosition.ToString() : "N/A")}");
                    }

                     // Rebuild chain: ScrollRect parent first, then content
                     ScrollRect parentScroll2 = settingsPanel.GetComponentInChildren<ScrollRect>(true);
                     if (parentScroll2 != null && parentScroll2.gameObject != contentRtForDiag.gameObject)
                         LayoutRebuilder.ForceRebuildLayoutImmediate(parentScroll2.GetComponent<RectTransform>());
                    LayoutRebuilder.ForceRebuildLayoutImmediate(contentRtForDiag);
                    Debug.Log($"[SettingsManager] Post-LayoutRebuild: SettingsContent. Rect={contentRtForDiag.rect}");

                    // Diagnose again after rebuild
                    VerticalLayoutGroup vlgAfter = settingsContent.GetComponent<VerticalLayoutGroup>();
                    ContentSizeFitter csfAfter = settingsContent.GetComponent<ContentSizeFitter>();
                    Debug.Log($"[SettingsManager] Post-rebuild: VLG={vlgAfter != null}, CSF={csfAfter != null}, " +
                        $"CSF_verticalFit={(csfAfter != null ? csfAfter.verticalFit.ToString() : "N/A")}");

                    // Re-check each child preferred height after rebuild
                    for (int i = 0; i < childCount && i < 5; i++)
                    {
                        Transform child = settingsContent.GetChild(i);
                        LayoutElement le = child.GetComponent<LayoutElement>();
                        Debug.Log($"[SettingsManager] Post-rebuild Child[{i}] '{child.name}': " +
                            $"preferredH={(le != null ? le.preferredHeight.ToString("F1") : "N/A")}");
                    }
                }

                // Force final canvas update
                UnityEngine.Canvas.ForceUpdateCanvases();

                // Set content height programmatically — ContentSizeFitter was removed to avoid conflict with VLG
                if (settingsContent is RectTransform contentRtForHeight)
                {
                    float totalContentHeight = 0f;
                    for (int i = 0; i < settingsContent.childCount; i++)
                    {
                        Transform child = settingsContent.GetChild(i);
                        RectTransform childRt = child.GetComponent<RectTransform>();
                        totalContentHeight += (childRt != null ? childRt.sizeDelta.y : 55f);
                        if (i < settingsContent.childCount - 1)
                            totalContentHeight += 10f; // VLG spacing
                    }
                    totalContentHeight += 20f; // VLG padding top+bottom

                    contentRtForHeight.sizeDelta = new Vector2(contentRtForHeight.sizeDelta.x, totalContentHeight);
                    Debug.Log($"[SettingsManager] Set content height to {totalContentHeight:F0}. Rect: {contentRtForHeight.rect}");

                // Rebuild ScrollRect so it picks up new content size
                if (settingsPanel != null)
                {
                    ScrollRect scrollForRebuild = settingsPanel.GetComponentInChildren<ScrollRect>(true);
                    if (scrollForRebuild != null)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollForRebuild.GetComponent<RectTransform>());
                        Debug.Log($"[SettingsManager] Rebuilt ScrollRect. Content size: {scrollForRebuild.content?.GetComponent<RectTransform>()?.rect}");
                    }
                }
                }

                settingsPanel.SetActive(true);
                isPanelOpen = true;
            }

            /// <summary>
            /// Ensures ScrollRect and Viewport are configured so items are actually visible.
            /// Fixes: horizontal=true, movementType=Elastic, Viewport Image alpha=0 + Mask.showMaskGraphic=false.
            /// </summary>
            private void FixScrollRect()
            {
                if (settingsPanel == null)
                {
                    Debug.LogWarning("[FixScrollRect] settingsPanel is null, aborting.");
                    return;
                }

                ScrollRect scroll = settingsPanel.GetComponentInChildren<ScrollRect>(true);
                if (scroll == null)
                {
                    Debug.LogWarning("[FixScrollRect] No ScrollRect found in settingsPanel.");
                    return;
                }

                Debug.Log($"[FixScrollRect] Found ScrollRect: h={scroll.horizontal}, v={scroll.vertical}, movement={scroll.movementType}, " +
                    $"viewport={scroll.viewport?.name ?? "null"}, content={scroll.content?.name ?? "null"}");

                // Check if this ScrollRect is on the content itself (would be wrong)
                Debug.Log($"[FixScrollRect] ScrollRect is on: {scroll.gameObject.name}, parent: {scroll.transform.parent?.name ?? "root"}");

                // Log ContentSizeFitter and VerticalLayoutGroup on content
                if (scroll.content != null)
                {
                    ContentSizeFitter csf = scroll.content.GetComponent<ContentSizeFitter>();
                    VerticalLayoutGroup vlg = scroll.content.GetComponent<VerticalLayoutGroup>();
                    Debug.Log($"[FixScrollRect] Content '{scroll.content.name}': " +
                        $"CSF={csf != null}{(csf != null ? $", vFit={csf.verticalFit}" : "")}, " +
                        $"VLG={vlg != null}{(vlg != null ? $", chControlH={vlg.childControlHeight}" : "")}");
                }

                // Fix horizontal scrolling — should be vertical-only
                if (scroll.horizontal)
                {
                    scroll.horizontal = false;
                    Debug.Log("[FixScrollRect] Fixed: horizontal = false");
                }

                // Fix movement type — Elastic can send content off-screen
                if (scroll.movementType != ScrollRect.MovementType.Clamped)
                {
                    scroll.movementType = ScrollRect.MovementType.Clamped;
                    Debug.Log($"[FixScrollRect] Fixed: movementType = {scroll.movementType}");
                }

                 // Fix Viewport: Image alpha + Mask
                 if (scroll.viewport != null)
                {
                    Debug.Log($"[FixScrollRect] Viewport '{scroll.viewport.name}': " +
                        $"rect={scroll.viewport.GetComponent<RectTransform>()?.rect}, " +
                        $"hasRectMask={(scroll.viewport.GetComponent<RectMask2D>() != null)}, " +
                        $"hasMask={(scroll.viewport.GetComponent<Mask>() != null)}");

                    Image vpImage = scroll.viewport.GetComponent<Image>();
                    if (vpImage != null)
                    {
                        if (vpImage.color.a <= 0.01f)
                        {
                            vpImage.color = new Color(vpImage.color.r, vpImage.color.g, vpImage.color.b, 1f);
                            Debug.Log("[FixScrollRect] Fixed: Viewport Image alpha was 0 → set to 1");
                        }
                        else
                        {
                            Debug.Log($"[FixScrollRect] Viewport Image alpha OK: {vpImage.color.a:F2}");
                        }
                    }
                    else
                    {
                        Debug.Log("[FixScrollRect] Viewport has no Image component");
                    }

                    Mask vpMask = scroll.viewport.GetComponent<Mask>();
                    if (vpMask != null)
                    {
                        if (!vpMask.showMaskGraphic)
                        {
                            vpMask.showMaskGraphic = true;
                            Debug.Log("[FixScrollRect] Fixed: Mask.showMaskGraphic was false → set to true");
                        }
                        else
                        {
                            Debug.Log("[FixScrollRect] Mask.showMaskGraphic already = true");
                        }
                    }
                    else
                    {
                        Debug.Log("[FixScrollRect] Viewport has no Mask component");
                    }
                }
                else
                {
                    Debug.LogWarning("[FixScrollRect] ScrollRect.viewport is NULL!");
                }
            }

            private bool IsContentInsideScrollView(Transform content)
            {
                if (content == null) return false;
                Transform current = content.parent;
                while (current != null)
                {
                    if (current.name.Contains("SettingsScrollView"))
                        return true;
                    current = current.parent;
                }
                return false;
            }

        public void CloseSettings()
        {
            if (settingsPanel == null) return;

            SaveSettings();
            settingsPanel.SetActive(false);
            isPanelOpen = false;
        }

        private void CreateSettingItems()
        {
            if (config == null)
            {
                Debug.LogError("[SettingsManager] config is null! Assign WorldGenConfig in inspector.");
                return;
            }
            if (settingItemPrefab == null)
            {
                Debug.LogError("[SettingsManager] settingItemPrefab is null! Assign SettingItem prefab in inspector.");
                return;
            }
            if (settingsContent == null)
            {
                Debug.LogError("[SettingsManager] settingsContent is null! Assign ScrollView content RectTransform in settings inspector.");
                return;
            }

            var fields = config.GetType().GetFields(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance);

            int created = 0, skipped = 0;

            foreach (var field in fields)
            {
                if (field.IsInitOnly || field.IsLiteral) continue;
                if (field.FieldType.IsArray) continue;

                string fieldName = field.Name;
                object value = field.GetValue(config);

                if (ShouldExcludeParameter(fieldName))
                    continue;

                 GameObject itemGO = Instantiate(settingItemPrefab, settingsContent);
                itemGO.name = "Setting_" + fieldName;

                // Log immediate after instantiate
                RectTransform rtCheck = itemGO.GetComponent<RectTransform>();
                Debug.Log($"[SettingsManager] Instantiate '{itemGO.name}': " +
                    $"RT={(rtCheck != null)}, anchorMin={(rtCheck != null ? rtCheck.anchorMin.ToString() : "N/A")}, " +
                    $"anchorMax={(rtCheck != null ? rtCheck.anchorMax.ToString() : "N/A")}, " +
                    $"sizeDelta={(rtCheck != null ? rtCheck.sizeDelta.ToString() : "N/A")}, " +
                    $"anchoredPos={(rtCheck != null ? rtCheck.anchoredPosition.ToString() : "N/A")}, " +
                    $"parent='{itemGO.transform.parent?.name ?? "null"}'");

                // Disable any embedded Canvas on the prefab to avoid rendering conflicts with the main panel Canvas
                Canvas embeddedCanvas = itemGO.GetComponent<Canvas>();
                if (embeddedCanvas != null)
                {
                    embeddedCanvas.enabled = false;
                    Debug.Log($"[SettingsManager] Disabled embedded Canvas on '{itemGO.name}'.");
                }

                int uiLayer = LayerMask.NameToLayer("UI");
                if (uiLayer >= 0)
                    itemGO.layer = uiLayer;

                // ---------- root (setting item wrapper) ----------
                RectTransform rootRt = itemGO.GetComponent<RectTransform>();
                if (rootRt != null)
                {
                    // root anchors spread horizontally (0,0)-(1,0): top edge pinned to parent top
                    rootRt.anchorMin = new Vector2(0, 0);
                    rootRt.anchorMax = new Vector2(1, 0);
                    rootRt.pivot = new Vector2(0.5f, 1f);
                    rootRt.anchoredPosition = Vector2.zero;
                    Debug.Log("[SettingsManager] Fixed root RT anchorMin=" + rootRt.anchorMin + " anchorMax=" + rootRt.anchorMax);
                }
                else
                {
                    Debug.LogWarning("[SettingsManager] No RectTransform on root!");
                }

                // root explicit height — parent VLG reads this via LayoutElement
                LayoutElement rootLe = itemGO.GetComponent<LayoutElement>();
                if (rootLe == null)
                    rootLe = itemGO.AddComponent<LayoutElement>();
                float totalH = itemTotalHeight > 0f ? itemTotalHeight : labelHeight + itemSpacing + sliderHeight;
                rootLe.preferredHeight = totalH;
                rootLe.minHeight = totalH;
                rootLe.flexibleHeight = 0f;
                rootLe.preferredWidth = -1f;     // fill parent width
                rootLe.minWidth = 0f;

                // VLG stacks Label above Slider vertically inside each item
                VerticalLayoutGroup vlg = itemGO.GetComponent<VerticalLayoutGroup>();
                if (vlg == null)
                {
                    vlg = itemGO.AddComponent<VerticalLayoutGroup>();
                }
                vlg.padding = new RectOffset(0, 0, 0, 0);
                vlg.spacing = itemSpacing;   // отступ label ↔ slider внутри одного item
                vlg.childControlWidth = true;   // уважаем preferredWidth детей
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = false; // не принудительно растягиваем — preferredWidth работает
                vlg.childForceExpandHeight = false;
                vlg.childAlignment = TextAnchor.UpperLeft;

                // ---------- children layout ----------
                // VLG childControlHeight drives child Y-positions via SetChildAlongAxis automatically.
                // Use LayoutElement.preferredWidth/Height to tell VLG the geometry of each child.
                if (itemGO.transform.Find("Label") is Transform labelTf
                    && labelTf.GetComponent<RectTransform>() is RectTransform labelRt)
                {
                    // Anchor: left edge at x=0 in parent local space (left-aligned container)
                    labelRt.anchorMin = new Vector2(0f, 0f);
                    labelRt.anchorMax = new Vector2(0f, 0f);   // not stretched — fixed preferredWidth
                    labelRt.pivot = new Vector2(0f, 0f);       // pivot = left-bottom corner
                    labelRt.anchoredPosition = new Vector2(labelLeftPadding, 0f); // отступ от левого края

                    LayoutElement labelLe = labelRt.GetComponent<LayoutElement>();
                    if (labelLe == null) labelLe = labelRt.gameObject.AddComponent<LayoutElement>();
                    labelLe.preferredHeight = labelHeight;
                    labelLe.minHeight = labelHeight;
                    labelLe.flexibleHeight = 0f;
                    labelLe.preferredWidth = labelWidth;  // фиксированная ширина области названия
                    labelLe.flexibleWidth = 0f;
                }

                if (itemGO.transform.Find("Slider") is Transform sliderTf
                    && sliderTf.GetComponent<RectTransform>() is RectTransform sliderRt)
                {
                    // Anchor: anchorMin.x=0 anchorMax.x=1 → stretched full width
                    sliderRt.anchorMin = new Vector2(0f, 0f);
                    sliderRt.anchorMax = new Vector2(1f, 0f);
                    sliderRt.pivot = new Vector2(0.5f, 0f);
                    sliderRt.anchoredPosition = new Vector2(0f, 0f);

                    LayoutElement sliderLe = sliderRt.GetComponent<LayoutElement>();
                    if (sliderLe == null) sliderLe = sliderRt.gameObject.AddComponent<LayoutElement>();
                    sliderLe.preferredHeight = sliderHeight;
                    sliderLe.minHeight = sliderHeight;
                    sliderLe.flexibleHeight = 0f;
                    sliderLe.flexibleWidth = 1f;   // приоритет на растягивание — занимает всё оставшееся пространство
                }

                TMP_Text label = null;
                Slider slider = null;

                SettingItem si = itemGO.GetComponent<SettingItem>();
                if (si != null)
                {
                    label = si.LabelText;
                    slider = si.ValueSlider;
                }
                if (label == null) label = itemGO.GetComponent<TMP_Text>();
                if (slider == null) slider = itemGO.GetComponent<Slider>();
                if (label == null) label = itemGO.GetComponentInChildren<TMP_Text>(true);
                if (slider == null) slider = itemGO.GetComponentInChildren<Slider>(true);

                if (slider == null || label == null)
                {
                    Debug.LogError($"[SettingsManager] Prefab for '{fieldName}' missing TMP_Text/Slider. Destroying.");
                    Destroy(itemGO);
                    skipped++;
                    continue;
                }

                // Font size from inspector
                label.fontSize = labelFontSize;
                label.fontSizeMin = labelFontSizeMin;
                label.fontSizeMax = labelFontSizeMax;

                // Configure label: Left = 1 | Top = 64 → alignment = 65
                label.alignment = TMPro.TextAlignmentOptions.Left | TMPro.TextAlignmentOptions.Top;
                label.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                label.margin = new Vector4(4, 0, 4, 0); // 4px left+right padding
                label.overflowMode = TMPro.TextOverflowModes.Overflow;
                label.enableAutoSizing = false;
                label.autoSizeTextContainer = true;

                if (value is int intValue)
                {
                    GetIntRange(fieldName, out int min, out int max);
                    slider.minValue = min;
                    slider.maxValue = max;
                    slider.wholeNumbers = true;
                    slider.value = intValue;
                    label.text = $"{fieldName}: {intValue}";
                }
                else if (value is float floatValue)
                {
                    GetFloatRange(fieldName, out float min, out float max, out string fmt);
                    slider.minValue = min;
                    slider.maxValue = max;
                    slider.wholeNumbers = false;
                    slider.value = floatValue;
                    label.text = $"{fieldName}: {floatValue.ToString(fmt)}";
                }
                else
                {
                    Debug.LogWarning($"[SettingsManager] Unsupported type {field.FieldType.Name} for {fieldName}, skipping.");
                    Destroy(itemGO);
                    skipped++;
                    continue;
                }

                parameterSliders[fieldName] = slider;
                parameterLabels[fieldName] = label;

                slider.onValueChanged.RemoveAllListeners();
                slider.onValueChanged.AddListener((float val) => OnParameterChanged(fieldName, val));

                created++;
            }

              Debug.Log($"[SettingsManager] Created {created} items ({skipped} skipped). " +
                  $"Parameters: {string.Join(", ", parameterSliders.Keys)}");
        }

        private void OnParameterChanged(string fieldName, float newValue)
        {
            if (config == null) return;

            var field = config.GetType().GetField(fieldName);
            if (field == null) return;

            object convertedValue;

            if (field.FieldType == typeof(int))
            {
                convertedValue = Mathf.RoundToInt(newValue);
            }
            else if (field.FieldType == typeof(float))
            {
                if (fieldName == "biomeScale")
                    convertedValue = Mathf.Round(newValue * 1000f) / 1000f;
                else if (fieldName == "biomePersistence")
                    convertedValue = Mathf.Round(newValue * 100f) / 100f;
                else if (fieldName == "biomeLacunarity")
                    convertedValue = Mathf.Round(newValue * 10f) / 10f;
                else
                    convertedValue = newValue;
            }
            else
            {
                return;
            }

            field.SetValue(config, convertedValue);

            if (parameterLabels.TryGetValue(fieldName, out TMP_Text label))
            {
                if (convertedValue is float f)
                {
                    string fmt = GetFloatFormat(fieldName);
                    label.text = $"{fieldName}: {f.ToString(fmt)}";
                }
                else if (convertedValue is int i)
                {
                    label.text = $"{fieldName}: {i}";
                }
            }
        }

        private void GetIntRange(string fieldName, out int min, out int max)
        {
            switch (fieldName)
            {
                case "districtCount": min = 1; max = 20; break;
                case "extraEdges": min = 0; max = 10; break;
                case "roadRadius": min = 0; max = 3; break;
                case "districtMargin": min = 0; max = 20; break;
                case "districtMinDistance": min = 0; max = 30; break;
                case "districtGridStep": min = 1; max = 20; break;
                case "maxNodeDegree": min = 1; max = 4; break;
                case "minBuildingGap": min = 0; max = 5; break;
                case "minBuildingDistance": min = 0; max = 5; break;
                case "terrainSmoothChance": min = 0; max = 100; break;
                case "biomeOctaves": min = 1; max = 6; break;
                case "decorationMinDistanceFromRoad": min = 0; max = 5; break;
                case "decorationMaxDistanceFromRoad": min = 0; max = 50; break;
                case "treeSpawnChance": min = 0; max = 100; break;
                case "maxTreeCount": min = 0; max = 1000; break;
                case "lampInterval": min = 2; max = 20; break;
                case "minRoadSegmentLength": min = 3; max = 20; break;
                case "minBenchDistance": min = 0; max = 5; break;
                default: min = 0; max = 100; break;
            }
        }

        private void GetFloatRange(string fieldName, out float min, out float max, out string format)
        {
            switch (fieldName)
            {
                case "biomeScale": min = 0.01f; max = 0.2f; format = "F3"; break;
                case "biomePersistence": min = 0.1f; max = 1.0f; format = "F2"; break;
                case "biomeLacunarity": min = 1.0f; max = 4.0f; format = "F1"; break;
                default: min = 0f; max = 1f; format = "F2"; break;
            }
        }

        private string GetFloatFormat(string fieldName)
        {
            switch (fieldName)
            {
                case "biomeScale": return "F3";
                case "biomeLacunarity": return "F1";
                default: return "F2";
            }
        }

        private bool ShouldExcludeParameter(string fieldName)
        {
            string fnLower = fieldName.ToLower();

            // Prefab arrays and IDs — excludeBuildingPrefabs catches buildingPrefabIds and buildingPrefabWeights
            if (excludeBuildingPrefabs && fnLower.Contains("buildingprefab"))
                return true;
            if (excludeBuildingPrefabs && fnLower.Contains("buildingweight"))
                return true;
            if (excludeTreePrefabs && fnLower.Contains("treeprefab"))
                return true;
            if (excludeLampPrefabs && fnLower.Contains("lampprefab"))
                return true;
            if (excludeBenchPrefabs && fnLower.Contains("benchprefab"))
                return true;

            // Biome thresholds
            if (excludeBiomeThresholds && fnLower.Contains("threshold"))
                return true;

            // Biome octaves — скрываем только параметры с описаниями октав, не само поле biomeOctaves
            if (excludeBiomeOctaves && fnLower.Contains("octavechance"))
                return true;

            // Minimum distances — точные суффиксы тех трёх полей, что нужно скрыть
            if (excludeMinDistances && fnLower.EndsWith("minbuildingdistance"))
                return true;
            if (excludeMinDistances && fnLower.EndsWith("decorationmindistancefromroad"))
                return true;
            if (excludeMinDistances && fnLower.EndsWith("minbenchdistance"))
                return true;

            // Terrain smooth
            if (excludeTerrainSmooth && fnLower.Contains("smooth"))
                return true;

            // Hash fields
            if (fnLower.Contains("hash"))
                return true;

            // Map size (Vector2Int — no slider UI)
            if (fnLower.Contains("mapsize"))
                return true;

            return false;
        }

        public void ResetToDefaults()
        {
            if (config == null || defaultValues.Count == 0) return;

            var fields = config.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.IsInitOnly) continue;
                if (defaultValues.TryGetValue(field.Name, out object defaultValue))
                {
                    field.SetValue(config, defaultValue);
                }
            }

            OpenSettings();
        }

        public void SaveSettings()
        {
            if (config == null) return;

            var fields = config.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.IsInitOnly) continue;
                if (ShouldExcludeParameter(field.Name)) continue;

                object value = field.GetValue(config);
                string key = SETTINGS_PREFIX + field.Name;
                string strValue = value.ToString();
                PlayerPrefs.SetString(key, strValue);
            }
            PlayerPrefs.Save();
            Debug.Log("[SettingsManager] Settings saved to PlayerPrefs");
        }

        public void LoadSettings()
        {
            if (config == null) return;

            var fields = config.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.IsInitOnly) continue;
                if (ShouldExcludeParameter(field.Name)) continue;

                string key = SETTINGS_PREFIX + field.Name;
                if (PlayerPrefs.HasKey(key))
                {
                    string savedValue = PlayerPrefs.GetString(key);
                    try
                    {
                        object converted = null;
                        if (field.FieldType == typeof(int))
                        {
                            converted = int.Parse(savedValue);
                        }
                        else if (field.FieldType == typeof(float))
                        {
                            converted = float.Parse(savedValue);
                        }
                        else if (field.FieldType == typeof(Vector2Int))
                        {
                            var parts = savedValue.Split(',');
                            if (parts.Length == 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
                            {
                                converted = new Vector2Int(x, y);
                            }
                        }

                        if (converted != null)
                        {
                            field.SetValue(config, converted);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[SettingsManager] Failed to load setting {field.Name}: {e.Message}");
                    }
                }
            }

            Debug.Log("[SettingsManager] Settings loaded from PlayerPrefs");
        }

        public bool IsOpen() => isPanelOpen;
    }
}
