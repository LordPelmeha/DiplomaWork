using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace Diploma.UI
{
    /// <summary>
    /// Менеджер меню паузы.
    /// </summary>
    public class PauseMenuManager : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Панель меню паузы")]
        public GameObject pauseMenuPanel;
        
        [Tooltip("Кнопка 'В главное меню'")]
        public Button mainMenuButton;
        
        [Tooltip("Кнопка 'Выход из игры'")]
        public Button exitButton;

        [Header("Settings")]
        [Tooltip("Название сцены главного меню")]
        public string menuSceneName = "MainMenu";
        
        [Tooltip("Клавиша паузы")]
        public Key pauseKey = Key.Escape;

        private bool isPaused = false;

        private void Awake()
        {
            // Скрываем меню паузы при старте
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);
        }

        private void Start()
        {
            // Подписка на кнопки
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            exitButton.onClick.AddListener(OnExitGameClicked);
        }

        private void OnDestroy()
        {
            // Отписка от кнопок
            mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            exitButton.onClick.RemoveListener(OnExitGameClicked);
        }

        private void Update()
        {
            // Обработка нажатия клавиши паузы (Input System)
            if (Keyboard.current != null && Keyboard.current[pauseKey].wasPressedThisFrame)
            {
                TogglePause();
            }
        }

        /// <summary>
        /// Переключить состояние паузы.
        /// </summary>
        public void TogglePause()
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }

        /// <summary>
        /// Поставить игру на паузу.
        /// </summary>
        public void Pause()
        {
            isPaused = true;
            Time.timeScale = 0f;
            
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(true);
            
            Debug.Log("[PauseMenu] Game paused");
        }

        /// <summary>
        /// Снять игру с паузы.
        /// </summary>
        public void Resume()
        {
            isPaused = false;
            Time.timeScale = 1f;
            
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);
            
            Debug.Log("[PauseMenu] Game resumed");
        }

        /// <summary>
        /// Кнопка 'В главное меню'.
        /// </summary>
        private void OnMainMenuClicked()
        {
            Debug.Log("[PauseMenu] Returning to main menu...");
            
            // Снимаем паузу
            Resume();
            
            // Загружаем главное меню
            SceneManager.LoadScene(menuSceneName);
        }

        /// <summary>
        /// Кнопка 'Выход из игры'.
        /// </summary>
        private void OnExitGameClicked()
        {
            Debug.Log("[PauseMenu] Exiting game...");
            
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }
}
