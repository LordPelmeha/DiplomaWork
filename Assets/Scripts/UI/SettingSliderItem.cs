using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Компонент для префаба элемента настройки (Slider + Label).
/// Присоединяется к GameObject, содержащему Slider и Text (Label).
/// </summary>
public class SettingSliderItem : MonoBehaviour
{
    [SerializeField] private Text labelText;
    [SerializeField] private Slider slider;

    public string ParameterName => labelText.text.Split(':')[0].Trim();

    public void Initialize(string name, float min, float max, float value, bool wholeNumbers = true)
    {
        if (labelText != null)
            labelText.text = $"{name}: {value}";

        if (slider != null)
        {
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = wholeNumbers;
            slider.value = value;
        }
    }

    public void UpdateLabel(string text)
    {
        if (labelText != null)
            labelText.text = text;
    }
}
