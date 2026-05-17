using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

namespace Diploma.UI
{
    /// <summary>
    /// UI для отображения статуса генерации мира: прогресс-бар и сообщения.
    /// Показывается во время генерации, скрывается после завершения.
    /// </summary>
    public class GenerationStatusUI : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject statusPanel;
        public Slider progressBar;
        public TMP_Text statusText;
        public TMP_Text titleText;

        [Header("Events")]
        public UnityEvent onGenerationSuccess = new UnityEvent();
        public UnityEvent onGenerationFailed = new UnityEvent();

        private bool isVisible = false;

        private void Awake()
        {
            if (statusPanel != null)
                statusPanel.SetActive(false);
        }

        /// <summary>
        /// Показывает панель статуса с начальным сообщением.
        /// </summary>
        public void Show(string title = "Generating World...")
        {
            if (statusPanel == null) return;

            if (titleText != null)
                titleText.text = title;

            if (progressBar != null)
            {
                progressBar.value = 0f;
                progressBar.gameObject.SetActive(true);
            }

            if (statusText != null)
                statusText.text = "Initializing...";

            statusPanel.SetActive(true);
            isVisible = true;
        }

        /// <summary>
        /// Обновляет прогресс (0-1).
        /// </summary>
        public void SetProgress(float progress, string statusMessage = null)
        {
            if (!isVisible) return;

            if (progressBar != null)
                progressBar.value = Mathf.Clamp01(progress);

            if (statusText != null && !string.IsNullOrEmpty(statusMessage))
                statusText.text = statusMessage;
        }

        /// <summary>
        /// Показывает сообщение об ошибке и скрывает прогресс-бар.
        /// </summary>
        public void ShowError(string errorMessage)
        {
            if (!isVisible) return;

            if (progressBar != null)
                progressBar.gameObject.SetActive(false);

            if (statusText != null)
                statusText.text = $"ERROR:\n{errorMessage}";

            if (titleText != null)
                titleText.text = "Generation Failed";

            onGenerationFailed?.Invoke();
        }

        /// <summary>
        /// Скрывает панель после успешной генерации.
        /// </summary>
        public void Hide()
        {
            if (statusPanel == null) return;

            statusPanel.SetActive(false);
            isVisible = false;
            onGenerationSuccess?.Invoke();
        }

        public bool IsVisible() => isVisible;
    }
}
