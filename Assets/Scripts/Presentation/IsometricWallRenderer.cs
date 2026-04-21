using UnityEngine;
using UnityEngine.Tilemaps;

namespace Diploma.Presentation
{
    /// <summary>
    /// Настраивает рендерер стен для корректного отображения в изометрии.
    /// Для изометрических тайлмапов важно использовать правильный режим рендеринга.
    /// </summary>
    [RequireComponent(typeof(Tilemap))]
    [RequireComponent(typeof(TilemapRenderer))]
    public sealed class IsometricWallRenderer : MonoBehaviour
    {
        [Header("Sorting")]
        [Tooltip("Базовый sorting layer")]
        public string sortingLayerName = "Default";
        
        [Tooltip("Order in layer для базового рендерера")]
        public int baseOrderInLayer = 10;

        [Header("Tilemap Settings")]
        [Tooltip("Автоматически обновлять sorting order при старте")]
        public bool refreshOnStart = true;

        private Tilemap _tilemap;
        private TilemapRenderer _renderer;

        private void Awake()
        {
            _tilemap = GetComponent<Tilemap>();
            _renderer = GetComponent<TilemapRenderer>();

            // Настраиваем рендерер для изометрических стен
            _renderer.sortingLayerName = sortingLayerName;
            _renderer.sortingOrder = baseOrderInLayer;
        }

        private void Start()
        {
            if (refreshOnStart)
            {
                Refresh();
            }
        }

        /// <summary>
        /// Обновляет тайлы и настройки рендерера.
        /// Вызывать после изменения тайлмапа.
        /// </summary>
        public void Refresh()
        {
            if (_tilemap == null || _renderer == null)
                return;

            // Обновляем все тайлы для применения настроек сортировки
            _tilemap.RefreshAllTiles();
        }

        /// <summary>
        /// Устанавливает кастомный sorting order.
        /// Можно вызывать для динамического изменения порядка отрисовки.
        /// </summary>
        public void SetCustomSortingOrder(int order)
        {
            if (_renderer != null)
            {
                _renderer.sortingOrder = order;
            }
        }
    }
}
