using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

namespace Diploma.UI
{
    /// <summary>
    /// Менеджер главного меню.
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Поле ввода seed")]
        public TMP_InputField seedInputField;
        
        [Tooltip("Кнопка рандомизации seed")]
        public Button randomizeButton;
        
        [Tooltip("Кнопка старта игры")]
        public Button startButton;
        
        [Tooltip("Кнопка выхода")]
        public Button exitButton;

        [Header("Settings")]
        [Tooltip("Название сцены с игрой")]
        public string gameSceneName = "Main";
        
        [Tooltip("Seed по умолчанию")]
        public int defaultSeed = 12345;

        private void Awake()
        {
            // Устанавливаем время в паузу (меню не на паузе)
            Time.timeScale = 1f;
        }

        private void Start()
        {
            // Инициализация UI
            InitializeUI();
            
            // Подписка на кнопки
            randomizeButton.onClick.AddListener(OnRandomizeSeedClicked);
            startButton.onClick.AddListener(OnStartGameClicked);
            exitButton.onClick.AddListener(OnExitGameClicked);
        }

        private void OnDestroy()
        {
            // Отписка от кнопок
            randomizeButton.onClick.RemoveListener(OnRandomizeSeedClicked);
            startButton.onClick.RemoveListener(OnStartGameClicked);
            exitButton.onClick.RemoveListener(OnExitGameClicked);
        }

        /// <summary>
        /// Инициализация UI (установка seed по умолчанию).
        /// </summary>
        private void InitializeUI()
        {
            seedInputField.text = SeedUtils.SeedToString(defaultSeed);
        }

        /// <summary>
        /// Кнопка рандомизации seed.
        /// </summary>
        private void OnRandomizeSeedClicked()
        {
            int randomSeed = SeedUtils.GenerateRandomSeed();
            seedInputField.text = SeedUtils.SeedToString(randomSeed);
        }

        /// <summary>
        /// Кнопка старта игры.
        /// </summary>
        private void OnStartGameClicked()
        {
            // Получаем seed из поля (или дефолтный если пусто)
            string seedText = seedInputField.text;
            int seed = SeedUtils.StringToSeed(seedText);
            
            // Сохраняем seed в PlayerPrefs для использования в игре
            PlayerPrefs.SetInt("GameSeed", seed);
            PlayerPrefs.Save();
            
            Debug.Log($"[MainMenu] Starting game with seed: {seed}");
            
            // Загружаем сцену с игрой
            SceneManager.LoadScene(gameSceneName);
        }

        /// <summary>
        /// Кнопка выхода из игры.
        /// </summary>
        private void OnExitGameClicked()
        {
            Debug.Log("[MainMenu] Exiting game...");
            
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }
}
