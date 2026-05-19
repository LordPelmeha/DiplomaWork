using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Diploma.UI
{
    /// <summary>
    /// Pause menu manager.
    /// Subscribes directly to UI/Cancel action (Escape by default),
    /// bypassing PlayerInput SendMessages mode.
    /// State is read from pauseMenuPanel.activeSelf — no separate bool flag.
    /// </summary>
    public class PauseMenuManager : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Pause menu panel (child of PauseCanvas)")]
        public GameObject pauseMenuPanel;

        [Tooltip("Back to main menu button")]
        public Button mainMenuButton;

        [Tooltip("Quit game button")]
        public Button exitButton;

        [Header("Input")]
        [Tooltip("PlayerInput component on this GameObject")]
        public PlayerInput playerInput;

        [Header("Settings")]
        [Tooltip("Main menu scene name")]
        public string menuSceneName = "MainMenu";

        private InputAction _cancelAction;

        private void Awake()
        {
            // Force PauseCanvas to Screen Space Overlay with high sort order
            // so it always renders on top regardless of scene canvas config.
            if (pauseMenuPanel != null)
            {
                Transform root = pauseMenuPanel.transform;
                while (root.parent != null) root = root.parent;

                Canvas[] allCanvases = root.GetComponentsInChildren<Canvas>(true);
                foreach (Canvas c in allCanvases)
                {
                    c.renderMode = RenderMode.ScreenSpaceOverlay;
                    c.overrideSorting = true;
                    c.sortingOrder = 1000;
                    c.pixelPerfect = false;
                }
            }

            if (pauseMenuPanel != null)
            {
                Transform root = pauseMenuPanel.transform;
                while (root.parent != null) root = root.parent;
                root.gameObject.SetActive(false);
                pauseMenuPanel.SetActive(false);
            }

            if (playerInput != null && playerInput.actions != null)
            {
                Debug.Log("[PauseMenu] playerInput OK, maps=" + playerInput.actions.actionMaps.Count);
                _cancelAction = playerInput.actions.FindAction("Cancel", true);
                if (_cancelAction != null)
                {
                    _cancelAction.performed += OnCancelPerformed;
                    Debug.Log("[PauseMenu] Cancel.performed subscribed, enabled=" + _cancelAction.enabled);
                }
            }
        }

        private void Start()
        {
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitGameClicked);
        }

        private void OnDestroy()
        {
            if (mainMenuButton != null)
                mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            if (exitButton != null)
                exitButton.onClick.RemoveListener(OnExitGameClicked);
            if (_cancelAction != null)
                _cancelAction.performed -= OnCancelPerformed;
        }

        private void OnCancelPerformed(InputAction.CallbackContext ctx)
        {
            if (ctx.phase != InputActionPhase.Performed) return;
            TogglePause();
        }

        private void Update()
        {
            if (_cancelAction == null && Keyboard.current != null && Keyboard.current[Key.Escape].wasPressedThisFrame)
            {
                TogglePause();
            }
        }

        /// <summary>
        /// Toggle: if panel is active → resume, else → pause.
        /// Source of truth = pauseMenuPanel.activeSelf.
        /// </summary>
        public void TogglePause()
        {
            if (pauseMenuPanel == null) return;

            if (pauseMenuPanel.activeSelf)
                Resume();
            else
                Pause();
        }

        public void Pause()
        {
            Time.timeScale = 0f;

            if (pauseMenuPanel != null)
            {
                Transform root = pauseMenuPanel.transform;
                while (root.parent != null) root = root.parent;
                root.gameObject.SetActive(true);
                pauseMenuPanel.SetActive(true);

                // Full Canvas diagnostics
                Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
                Debug.Log("[PauseMenu] === CANVAS DIAGNOSTICS ===");
                foreach (Canvas c in canvases)
                {
                    Debug.Log($"  Canvas '{c.gameObject.name}': active={c.gameObject.activeSelf} " +
                        $"renderMode={c.renderMode} sortingOrder={c.sortingOrder} " +
                        $"overrideSorting={c.overrideSorting} " +
                        $"planeDistance={c.planeDistance} worldCamera={(c.worldCamera != null ? c.worldCamera.name : "NULL")} " +
                        $"pixelPerfect={c.pixelPerfect}");
                }

                CanvasGroup[] cgs = root.GetComponentsInChildren<CanvasGroup>(true);
                if (cgs.Length > 0)
                {
                    foreach (CanvasGroup cg in cgs)
                    {
                        Debug.Log($"  CanvasGroup '{cg.gameObject.name}': alpha={cg.alpha} " +
                            $"interactable={cg.interactable} blocksRaycasts={cg.blocksRaycasts} " +
                            $"ignoreParentGroups={cg.ignoreParentGroups}");
                    }
                }
                else
                {
                    Debug.Log("  No CanvasGroup found in subtree.");
                }

                Debug.Log("[PauseMenu] Pause() — root='" + root.name + "' panelActive=" + pauseMenuPanel.activeSelf);
            }

            Debug.Log("[PauseMenu] Game paused");
        }

        public void Resume()
        {
            if (pauseMenuPanel == null)
            {
                Time.timeScale = 1f;
                return;
            }

            // Guard: never touch timeScale or hide if panel is already hidden
            if (!pauseMenuPanel.activeSelf)
            {
                Debug.LogWarning("[PauseMenu] Resume() SKIPPED — panel already hidden. Removing stale subscription side-effect.");
                Time.timeScale = 1f;
                return;
            }

            Transform root = pauseMenuPanel.transform;
            while (root.parent != null) root = root.parent;

            // Full Canvas diagnostics BEFORE hiding
            Canvas[] canvasesBefore = root.GetComponentsInChildren<Canvas>(true);
            Debug.Log("[PauseMenu] === CANVAS DIAG BEFORE RESUME ===");
            foreach (Canvas c in canvasesBefore)
            {
                Debug.Log($"  Canvas '{c.gameObject.name}': active={c.gameObject.activeSelf} " +
                    $"renderMode={c.renderMode} sortingOrder={c.sortingOrder} " +
                    $"overrideSorting={c.overrideSorting} worldCamera={(c.worldCamera != null ? c.worldCamera.name : "NULL")}");
            }

            Time.timeScale = 1f;
            pauseMenuPanel.SetActive(false);
            root.gameObject.SetActive(false);

            Debug.Log("[PauseMenu] RESUMED — panel=" + pauseMenuPanel.activeSelf + " root=" + root.gameObject.activeSelf);
        }

        private void OnMainMenuClicked()
        {
            Resume();
            SceneManager.LoadScene(menuSceneName);
        }

        private void OnExitGameClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
