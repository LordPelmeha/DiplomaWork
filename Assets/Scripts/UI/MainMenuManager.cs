using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Diploma.UI
{
    /// <summary>
    /// Управляет главным меню:seed ввод, старт игры, выход, открытие настроек.
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        [Header("UI References")]
        public TMP_InputField seedInputField;
        public Button randomizeButton;
        public Button startButton;
        public Button exitButton;
        public Button settingsButton;

        [Header("Settings")]
        public string gameSceneName = "Main";
        public int defaultSeed = 12345;

        [Header("References")]
        public SettingsManager settingsManager;
        public ErrorDialog errorDialog;

        private void Start()
        {
            InitializeUI();
            SubscribeButtons();
        }

        private void OnDestroy()
        {
            UnsubscribeButtons();
        }

        /// <summary>
        /// Инициализирует UI значениями по умолчанию.
        /// </summary>
        private void InitializeUI()
        {
            if (seedInputField != null)
                seedInputField.text = SeedUtils.SeedToString(defaultSeed);
        }

        /// <summary>
        /// Подписывается на события кнопок.
        /// </summary>
        private void SubscribeButtons()
        {
            if (randomizeButton != null)
                randomizeButton.onClick.AddListener(OnRandomizeSeedClicked);
            if (startButton != null)
                startButton.onClick.AddListener(OnStartGameClicked);
            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitGameClicked);
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        /// <summary>
        /// Отписывается от событий кнопок.
        /// </summary>
        private void UnsubscribeButtons()
        {
            if (randomizeButton != null)
                randomizeButton.onClick.RemoveListener(OnRandomizeSeedClicked);
            if (startButton != null)
                startButton.onClick.RemoveListener(OnStartGameClicked);
            if (exitButton != null)
                exitButton.onClick.RemoveListener(OnExitGameClicked);
            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(OnSettingsClicked);
        }

        /// <summary>
        /// Генерация случайного seed.
        /// </summary>
        private void OnRandomizeSeedClicked()
        {
            int randomSeed = SeedUtils.GenerateRandomSeed();
            seedInputField.text = SeedUtils.SeedToString(randomSeed);
        }

        /// <summary>
        /// Открытие панели настроек.
        /// </summary>
        private void OnSettingsClicked()
        {
            if (settingsManager != null)
            {
                settingsManager.OpenSettings();
            }
            else
            {
                Debug.LogWarning("[MainMenu] SettingsManager not assigned!");
            }
        }

        /// <summary>
        /// Старт игры — загрузка сцены Main.
        /// </summary>
        private void OnStartGameClicked()
        {
            int seed = ParseSeed(seedInputField.text);
            PlayerPrefs.SetInt("GameSeed", seed);
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
        }

        /// <summary>
        /// Выход из игры.
        /// </summary>
        private void OnExitGameClicked()
        {
    #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
    #else
            Application.Quit();
    #endif
        }

        /// <summary>
        /// Преобразует текст seed в число.
        /// </summary>
        private int ParseSeed(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return defaultSeed;

            if (int.TryParse(text, out int seed))
                return seed;

            // Fallback: стабильный хеш строки (детерминированный)
            // Используем простой суммативный хеш
            unchecked
            {
                int hash = 0;
                foreach (char c in text)
                    hash = (hash * 31) + c;
                return Mathf.Abs(hash);
            }
        }
    }
}
