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
    /// Per-parameter setting for WorldGenConfig fields.
    /// Stored in the inspector list — each entry controls how one config field appears in the settings panel.
    /// </summary>
    [Serializable]
    public class ParameterSetting
    {
        [Tooltip("Имя поля в WorldGenConfig (заполняется автоматически)")]
        public string fieldName;

        [Tooltip("Показывать ли этот параметр в панели настроек")]
        public bool enabled = true;

        [Tooltip("Минимальное значение для слайдера (0 = использовать дефолт)")]
        public float minValue = 0f;

        [Tooltip("Максимальное значение для слайдера (0 = использовать дефолт)")]
        public float maxValue = 0f;

        [Tooltip("Текст для отображения вместо имени поля (пустое = использовать fieldName)")]
        public string displayName = "";
    }

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

        [Header("Per-Parameter Settings")]
        [Tooltip("Автозаполняется всеми числовыми параметрами WorldGenConfig при старте. " +
                 "enabled = показывать в панели настроек. " +
                 "min/max = границы слайдера. " +
                 "displayName = текст лейбла (пустой = использовать имя поля).")]
        public List<ParameterSetting> parameterSettings = new List<ParameterSetting>();

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
        private Dictionary<string, ParameterSetting> parameterSettingsByName = new Dictionary<string, ParameterSetting>();
        private bool isPanelOpen = false;
        private bool parameterSettingsBuilt = false;

        private const string SETTINGS_PREFIX = "WorldGenConfig_";
        private const string PARAM_SETTINGS_KEY = "SettingsManager_ParameterSettings";

        private void Awake()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);

            CacheDefaultValues();
            LoadParameterSettings();
            BuildParameterSettingsList();
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

        // =====================================================================
        // ParameterSettings management
        // =====================================================================

        /// <summary>
        /// Ensures parameterSettings list has one entry per numeric field in WorldGenConfig.
        /// Called in Awake() — the inspector list is fully populated automatically on startup.
        /// Merges with any entries already saved in PlayerPrefs from a previous session.
        /// </summary>
        private void BuildParameterSettingsList()
        {
            if (config == null) return;

            parameterSettingsByName.Clear();

            var fields = config.GetType().GetFields(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance);

            var entriesToRemove = new HashSet<string>(parameterSettings.Select(s => s.fieldName));

            foreach (var field in fields)
            {
                if (field.IsInitOnly || field.IsLiteral) continue;
                if (field.FieldType.IsArray) continue;
                if (field.FieldType != typeof(int) && field.FieldType != typeof(float)) continue;

                string fieldName = field.Name;
                entriesToRemove.Remove(fieldName);

                ParameterSetting ps = parameterSettings.FirstOrDefault(s => s.fieldName == fieldName);
                if (ps == null)
                {
                    ps = new ParameterSetting
                    {
                        fieldName = fieldName,
                        enabled = true,
                        displayName = ""
                    };
                    parameterSettings.Add(ps);
                }
                // Always keep displayName in sync with field name when empty
                if (string.IsNullOrWhiteSpace(ps.displayName))
                {
                    ps.displayName = fieldName;
                }
                // Initialise range if still unset
                if (ps.minValue == 0f && ps.maxValue == 0f)
                {
                    if (field.FieldType == typeof(int))
                    {
                        GetIntRange(fieldName, out int dMin, out int dMax);
                        ps.minValue = dMin;
                        ps.maxValue = dMax;
                    }
                    else if (field.FieldType == typeof(float))
                    {
                        GetFloatRange(fieldName, out float dMin, out float dMax, out _);
                        ps.minValue = dMin;
                        ps.maxValue = dMax;
                    }
                }

                parameterSettingsByName[fieldName] = ps;
            }

            // Remove entries whose fields no longer exist in the config asset
            parameterSettings.RemoveAll(s => entriesToRemove.Contains(s.fieldName));
            parameterSettingsBuilt = true;
        }

        /// <summary>
        /// Persist per-parameter overrides (enabled, min/max, displayName) to PlayerPrefs.
        /// </summary>
        private void SaveParameterSettings()
        {
            if (parameterSettings == null || parameterSettings.Count == 0)
            {
                PlayerPrefs.DeleteKey(PARAM_SETTINGS_KEY);
                return;
            }

            try
            {
                var entries = parameterSettings
                    .Where(s => !string.IsNullOrEmpty(s.fieldName))
                    .Select(s => $"{s.fieldName}|{(s.enabled ? 1 : 0)}|{s.minValue}|{s.maxValue}|{s.displayName.Replace("|", "&#124;")}")
                    .ToArray();

                PlayerPrefs.SetString(PARAM_SETTINGS_KEY, string.Join("\n", entries));
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SettingsManager] Failed to save ParameterSettings: {e.Message}");
            }
        }

        /// <summary>
        /// Load per-parameter overrides from PlayerPrefs. Merges into existing list.
        /// </summary>
        private void LoadParameterSettings()
        {
            string json = PlayerPrefs.GetString(PARAM_SETTINGS_KEY, "");
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                var lines = json.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var loaded = new Dictionary<string, ParameterSetting>();

                foreach (var line in lines)
                {
                    var parts = line.Split(new[] { '|' }, 5);
                    if (parts.Length < 5) continue;

                    string fn = parts[0];
                    if (!bool.TryParse(parts[1], out bool enabled)) continue;
                    if (!float.TryParse(parts[2], out float minVal)) continue;
                    if (!float.TryParse(parts[3], out float maxVal)) continue;
                    string display = parts[4].Replace("&#124;", "|");

                    loaded[fn] = new ParameterSetting
                    {
                        fieldName = fn,
                        enabled = enabled,
                        minValue = minVal,
                        maxValue = maxVal,
                        displayName = display
                    };
                }

                // Merge loaded into existing list (preserve order, update matching entries)
                for (int i = 0; i < parameterSettings.Count; i++)
                {
                    if (loaded.TryGetValue(parameterSettings[i].fieldName, out ParameterSetting loadedPs))
                    {
                        parameterSettings[i] = loadedPs;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SettingsManager] Failed to load ParameterSettings: {e.Message}");
            }
        }

        /// <summary>
        /// Returns the ParameterSetting for a field, or null if not found / disabled.
        /// </summary>
        private bool TryGetParameterSetting(string fieldName, out ParameterSetting ps)
        {
            ps = null;
            if (string.IsNullOrEmpty(fieldName)) return false;
            parameterSettingsByName.TryGetValue(fieldName, out ps);
            return ps != null && ps.enabled;
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

        // =====================================================================
        // Range helpers — now read from ParameterSetting when available
        // =====================================================================

        private static readonly Dictionary<string, (int min, int max)> IntFallbackRanges = new Dictionary<string, (int, int)>
        {
            { "districtCount", (1, 20) },
            { "extraEdges", (0, 10) },
            { "roadRadius", (0, 3) },
            { "districtMargin", (0, 20) },
            { "districtMinDistance", (0, 30) },
            { "districtGridStep", (1, 20) },
            { "maxNodeDegree", (1, 4) },
            { "minBuildingGap", (0, 5) },
            { "minBuildingDistance", (0, 5) },
            { "terrainSmoothChance", (0, 100) },
            { "biomeOctaves", (1, 6) },
            { "decorationMinDistanceFromRoad", (0, 5) },
            { "decorationMaxDistanceFromRoad", (0, 50) },
            { "treeSpawnChance", (0, 100) },
            { "maxTreeCount", (0, 1000) },
            { "lampInterval", (2, 20) },
            { "minRoadSegmentLength", (3, 20) },
            { "minBenchDistance", (0, 5) }
        };

        private static readonly Dictionary<string, (float min, float max, string fmt)> FloatFallbackRanges = new Dictionary<string, (float, float, string)>
        {
            { "biomeScale", (0.01f, 0.2f, "F3") },
            { "biomePersistence", (0.1f, 1.0f, "F2") },
            { "biomeLacunarity", (1.0f, 4.0f, "F1") }
        };

        private void GetIntRange(string fieldName, out int min, out int max)
        {
            if (parameterSettingsBuilt
                && TryGetParameterSetting(fieldName, out ParameterSetting ps)
                && ps.minValue > 0f && ps.maxValue > 0f
                && ps.minValue < ps.maxValue)
            {
                min = Mathf.RoundToInt(ps.minValue);
                max = Mathf.RoundToInt(ps.maxValue);
            }
            else if (IntFallbackRanges.TryGetValue(fieldName, out var fb))
            {
                min = fb.min;
                max = fb.max;
            }
            else
            {
                min = 0;
                max = 100;
            }
        }

        private void GetFloatRange(string fieldName, out float min, out float max, out string format)
        {
            if (parameterSettingsBuilt
                && TryGetParameterSetting(fieldName, out ParameterSetting ps)
                && ps.minValue > 0f && ps.maxValue > 0f
                && ps.minValue < ps.maxValue)
            {
                min = ps.minValue;
                max = ps.maxValue;
                if (fieldName == "biomeScale") format = "F3";
                else if (fieldName == "biomeLacunarity") format = "F1";
                else format = "F2";
            }
            else if (FloatFallbackRanges.TryGetValue(fieldName, out var fb))
            {
                min = fb.min;
                max = fb.max;
                format = fb.fmt;
            }
            else
            {
                min = 0f;
                max = 1f;
                format = "F2";
            }
        }

        private string GetFloatFormat(string fieldName)
        {
            if (fieldName == "biomeScale") return "F3";
            if (fieldName == "biomeLacunarity") return "F1";
            return "F2";
        }

        // =====================================================================
        // Parameter label helpers
        // =====================================================================

        /// <summary>
        /// Returns the display name for a config field. Falls back to fieldName if
        /// no ParameterSetting exists or its displayName is empty.
        /// </summary>
        private string GetDisplayName(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName)) return fieldName;
            if (parameterSettingsBuilt
                && parameterSettingsByName.TryGetValue(fieldName, out ParameterSetting ps)
                && !string.IsNullOrWhiteSpace(ps.displayName))
            {
                return ps.displayName;
            }
            return fieldName;
        }

        private bool ShouldExcludeParameter(string fieldName)
        {
            // Only non-numeric / non-slidable types need exclusion for Save/Load.
            // The visibility gate is the per-parameter enabled flag, checked in CreateSettingItems.
            if (fieldName.Contains("mapsize")) return true;
            if (fieldName.Contains("hash")) return true;
            return false;
        }

        // =====================================================================
        // Open / Close
        // =====================================================================

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

                FixScrollRect();

                // Rebuild per-parameter settings before creating UI items
                BuildParameterSettingsList();

                // Configure SettingsContent VLG per inspector parameters
                if (settingsContent != null)
                {
                    VerticalLayoutGroup contentVLG = settingsContent.GetComponent<VerticalLayoutGroup>();
                    if (contentVLG != null)
                    {
                        contentVLG.spacing = itemItemSpacing;
                        contentVLG.padding = new RectOffset(
                            Mathf.RoundToInt(labelLeftPadding),
                            Mathf.RoundToInt(labelLeftPadding * 0.5f),
                            Mathf.RoundToInt(labelLeftPadding * 0.5f),
                            Mathf.RoundToInt(labelLeftPadding * 0.5f)
                        );
                        contentVLG.childAlignment = TextAnchor.UpperLeft;
                        contentVLG.childControlWidth = true;
                        contentVLG.childControlHeight = true;
                        contentVLG.childForceExpandWidth = true;
                        contentVLG.childForceExpandHeight = false;
                        Debug.Log($"[SettingsManager] Configured SettingsContent VLG: spacing={itemItemSpacing}, " +
                            $"padding.left={labelLeftPadding}, forceExpandWidth=true");
                    }
                }

                // Force Viewport VLG to compute its final width BEFORE children are created
                ScrollRect srForViewport = settingsPanel.GetComponentInChildren<ScrollRect>(true);
                if (srForViewport != null && srForViewport.viewport != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(srForViewport.viewport.GetComponent<RectTransform>());
                    Debug.Log("[SettingsManager] Viewport VLG rebuilt.");
                }

                LoadSettings();

                foreach (Transform child in settingsContent)
                    Destroy(child.gameObject);

                parameterSliders.Clear();
                parameterLabels.Clear();

                CreateSettingItems();

                // Force settingsContent VLG to recompute children
                if (settingsContent != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(settingsContent.GetComponent<RectTransform>());
                    Debug.Log("[SettingsManager] SettingsContent VLG rebuilt.");
                }

                UnityEngine.Canvas.ForceUpdateCanvases();

                // Set content height programmatically
                if (settingsContent is RectTransform contentRtForHeight)
                {
                    float totalContentHeight = 0f;
                    for (int i = 0; i < settingsContent.childCount; i++)
                    {
                        Transform child = settingsContent.GetChild(i);
                        RectTransform childRt = child.GetComponent<RectTransform>();
                        totalContentHeight += (childRt != null ? childRt.sizeDelta.y : 55f);
                        if (i < settingsContent.childCount - 1)
                            totalContentHeight += itemItemSpacing;
                    }
                    totalContentHeight += labelLeftPadding * 0.5f * 2f; // VLG padding top + bottom

                    contentRtForHeight.sizeDelta = new Vector2(contentRtForHeight.sizeDelta.x, totalContentHeight);
                    Debug.Log($"[SettingsManager] Set content height to {totalContentHeight:F0}.");

                    if (settingsPanel != null)
                    {
                        ScrollRect scrollForRebuild = settingsPanel.GetComponentInChildren<ScrollRect>(true);
                        if (scrollForRebuild != null)
                        {
                            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollForRebuild.GetComponent<RectTransform>());
                        }
                    }
                }

                settingsPanel.SetActive(true);
                isPanelOpen = true;
            }

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

            if (scroll.horizontal)
            {
                scroll.horizontal = false;
                Debug.Log("[FixScrollRect] Fixed: horizontal = false");
            }

            if (scroll.movementType != ScrollRect.MovementType.Clamped)
            {
                scroll.movementType = ScrollRect.MovementType.Clamped;
                Debug.Log($"[FixScrollRect] Fixed: movementType = {scroll.movementType}");
            }

            if (scroll.viewport != null)
            {
                Image vpImage = scroll.viewport.GetComponent<Image>();
                if (vpImage != null && vpImage.color.a <= 0.01f)
                {
                    vpImage.color = new Color(vpImage.color.r, vpImage.color.g, vpImage.color.b, 1f);
                    Debug.Log("[FixScrollRect] Fixed: Viewport Image alpha was 0 → set to 1");
                }

                Mask vpMask = scroll.viewport.GetComponent<Mask>();
                if (vpMask != null && !vpMask.showMaskGraphic)
                {
                    vpMask.showMaskGraphic = true;
                    Debug.Log("[FixScrollRect] Fixed: Mask.showMaskGraphic = true");
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
            SaveParameterSettings();
            settingsPanel.SetActive(false);
            isPanelOpen = false;
        }

        // =====================================================================
        // Create UI items
        // =====================================================================

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

                if (ShouldExcludeParameter(fieldName)) continue;

                // Per-parameter enabled gate
                if (parameterSettingsBuilt
                    && parameterSettingsByName.TryGetValue(fieldName, out ParameterSetting psCheck)
                    && !psCheck.enabled)
                {
                    skipped++;
                    continue;
                }

                GameObject itemGO = Instantiate(settingItemPrefab, settingsContent);
                itemGO.name = "Setting_" + fieldName;

                // Disable any embedded Canvas on the prefab to avoid rendering conflicts
                Canvas embeddedCanvas = itemGO.GetComponent<Canvas>();
                if (embeddedCanvas != null)
                {
                    embeddedCanvas.enabled = false;
                }

                int uiLayer = LayerMask.NameToLayer("UI");
                if (uiLayer >= 0)
                    itemGO.layer = uiLayer;

                // ---------- root RectTransform ----------
                RectTransform rootRt = itemGO.GetComponent<RectTransform>();
                if (rootRt != null)
                {
                    rootRt.anchorMin = new Vector2(0, 0);
                    rootRt.anchorMax = new Vector2(1, 0);
                    rootRt.pivot = new Vector2(0.5f, 1f);
                    rootRt.anchoredPosition = Vector2.zero;
                }

                LayoutElement rootLe = itemGO.GetComponent<LayoutElement>();
                if (rootLe == null) rootLe = itemGO.AddComponent<LayoutElement>();
                float totalH = itemTotalHeight > 0f ? itemTotalHeight : labelHeight + itemSpacing + sliderHeight;
                rootLe.preferredHeight = totalH;
                rootLe.minHeight = totalH;
                rootLe.flexibleHeight = 0f;
                rootLe.preferredWidth = -1f;
                rootLe.minWidth = 0f;

                VerticalLayoutGroup vlg = itemGO.GetComponent<VerticalLayoutGroup>();
                if (vlg == null) vlg = itemGO.AddComponent<VerticalLayoutGroup>();
                vlg.padding = new RectOffset(0, 0, 0, 0);
                vlg.spacing = itemSpacing;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = false;
                vlg.childForceExpandHeight = false;
                vlg.childAlignment = TextAnchor.UpperLeft;

                // ---------- Label ----------
                if (itemGO.transform.Find("Label") is Transform labelTf
                    && labelTf.GetComponent<RectTransform>() is RectTransform labelRt)
                {
                    labelRt.anchorMin = new Vector2(0f, 0f);
                    labelRt.anchorMax = new Vector2(0f, 0f);
                    labelRt.pivot = new Vector2(0f, 0f);
                    labelRt.anchoredPosition = new Vector2(labelLeftPadding, 0f);

                    LayoutElement labelLe = labelRt.GetComponent<LayoutElement>();
                    if (labelLe == null) labelLe = labelRt.gameObject.AddComponent<LayoutElement>();
                    labelLe.preferredHeight = labelHeight;
                    labelLe.minHeight = labelHeight;
                    labelLe.flexibleHeight = 0f;
                    labelLe.preferredWidth = labelWidth;
                    labelLe.flexibleWidth = 0f;
                }

                // ---------- Slider ----------
                if (itemGO.transform.Find("Slider") is Transform sliderTf
                    && sliderTf.GetComponent<RectTransform>() is RectTransform sliderRt)
                {
                    sliderRt.anchorMin = new Vector2(0f, 0f);
                    sliderRt.anchorMax = new Vector2(1f, 0f);
                    sliderRt.pivot = new Vector2(0.5f, 0f);
                    sliderRt.anchoredPosition = new Vector2(0f, 0f);

                    LayoutElement sliderLe = sliderRt.GetComponent<LayoutElement>();
                    if (sliderLe == null) sliderLe = sliderRt.gameObject.AddComponent<LayoutElement>();
                    sliderLe.preferredHeight = sliderHeight;
                    sliderLe.minHeight = sliderHeight;
                    sliderLe.flexibleHeight = 0f;
                    sliderLe.flexibleWidth = 1f;
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

                label.fontSize = labelFontSize;
                label.fontSizeMin = labelFontSizeMin;
                label.fontSizeMax = labelFontSizeMax;
                label.alignment = TMPro.TextAlignmentOptions.Left | TMPro.TextAlignmentOptions.Top;
                label.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                label.margin = new Vector4(4, 0, 4, 0);
                label.overflowMode = TMPro.TextOverflowModes.Overflow;
                label.enableAutoSizing = false;
                label.autoSizeTextContainer = true;

                // NEW: use displayName from ParameterSetting
                string displayLabel = GetDisplayName(fieldName);

                if (value is int intValue)
                {
                    GetIntRange(fieldName, out int min, out int max);
                    slider.minValue = min;
                    slider.maxValue = max;
                    slider.wholeNumbers = true;
                    slider.value = intValue;
                    label.text = $"{displayLabel}: {intValue}";
                }
                else if (value is float floatValue)
                {
                    GetFloatRange(fieldName, out float min, out float max, out string fmt);
                    slider.minValue = min;
                    slider.maxValue = max;
                    slider.wholeNumbers = false;
                    slider.value = floatValue;
                    label.text = $"{displayLabel}: {floatValue.ToString(fmt)}";
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

        // =====================================================================
        // Parameter change
        // =====================================================================

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
                // NEW: use displayName in label text
                string displayLabel = GetDisplayName(fieldName);

                if (convertedValue is float f)
                {
                    string fmt = GetFloatFormat(fieldName);
                    label.text = $"{displayLabel}: {f.ToString(fmt)}";
                }
                else if (convertedValue is int i)
                {
                    label.text = $"{displayLabel}: {i}";
                }
            }
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

            foreach (var ps in parameterSettings)
            {
                if (string.IsNullOrEmpty(ps.fieldName)) continue;

                var field = config.GetType().GetField(ps.fieldName);
                if (field == null || field.IsInitOnly) continue;

                object value = field.GetValue(config);
                string key = SETTINGS_PREFIX + ps.fieldName;
                string strValue = value.ToString();
                PlayerPrefs.SetString(key, strValue);
            }
            PlayerPrefs.Save();
            Debug.Log("[SettingsManager] Settings saved to PlayerPrefs");
        }

        public void LoadSettings()
        {
            if (config == null) return;

            foreach (var ps in parameterSettings)
            {
                if (string.IsNullOrEmpty(ps.fieldName)) continue;

                var field = config.GetType().GetField(ps.fieldName);
                if (field == null || field.IsInitOnly) continue;

                string key = SETTINGS_PREFIX + ps.fieldName;
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

                        if (converted != null)
                        {
                            field.SetValue(config, converted);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[SettingsManager] Failed to load setting {ps.fieldName}: {e.Message}");
                    }
                }
            }

            Debug.Log("[SettingsManager] Settings loaded from PlayerPrefs");
        }

        public bool IsOpen() => isPanelOpen;
    }
}
