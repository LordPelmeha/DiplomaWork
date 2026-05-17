using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Diploma.UI
{
    /// <summary>
    /// Диалоговое окно для отображения ошибок и сообщений.
    /// Простое OK-окно с заголовком и текстом.
    /// </summary>
    public class ErrorDialog : MonoBehaviour
    {
        public GameObject dialogPanel;
        public TMP_Text titleText;
        public TMP_Text messageText;
        public Button okButton;

        public Image background; // затемнение фона

        private System.Action onClose;

        private void Awake()
        {
            if (dialogPanel != null)
                dialogPanel.SetActive(false);

            if (okButton != null)
                okButton.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            if (okButton != null)
                okButton.onClick.RemoveListener(Close);
        }

        /// <summary>
        /// Показывает диалог с ошибкой.
        /// </summary>
        public void ShowError(string message, System.Action onClose = null)
        {
            Show("Error", message, onClose);
        }

        /// <summary>
        /// Показывает диалог с сообщением.
        /// </summary>
        public void Show(string title, string message, System.Action onClose = null)
        {
            if (dialogPanel == null) return;

            this.onClose = onClose;

            if (titleText != null)
                titleText.text = title;

            if (messageText != null)
                messageText.text = message;

            if (background != null)
                background.enabled = true;

            dialogPanel.SetActive(true);
        }

        /// <summary>
        /// Закрывает диалог.
        /// </summary>
        public void Close()
        {
            if (dialogPanel != null)
                dialogPanel.SetActive(false);

            onClose?.Invoke();
            onClose = null;
        }
    }
}
