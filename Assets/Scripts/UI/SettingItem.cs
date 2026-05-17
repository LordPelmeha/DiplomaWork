using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Простой компонент для префаба элемента настройки.
/// Содержит TMP_Text для отображения имени и значения, и Slider.
/// Используется SettingsManager для динамического создания списка параметров.
/// </summary>
public class SettingItem : MonoBehaviour
{
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Slider valueSlider;

    public TMP_Text LabelText => labelText;
    public Slider ValueSlider => valueSlider;

    /// <summary>
    /// Инициализирует элемент настройки.
    /// </summary>
    public void Initialize(string paramName, float min, float max, float value, bool wholeNumbers = true)
    {
        if (labelText != null)
            labelText.text = $"{paramName}: {value}";

        if (valueSlider != null)
        {
            valueSlider.minValue = min;
            valueSlider.maxValue = max;
            valueSlider.wholeNumbers = wholeNumbers;
            valueSlider.value = value;
        }
    }

    /// <summary>
    /// Обновляет текст значения (вызывается при изменении слайдера).
    /// </summary>
    public void UpdateLabel(string text)
    {
        if (labelText != null)
            labelText.text = text;
    }
}
