using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class CameraZoom : MonoBehaviour
{
    [Header("Настройки зума")]
    [Tooltip("Скорость изменения размера камеры при прокрутке")]
    [SerializeField] private float zoomSpeed = 5f;
    [Tooltip("Минимальное значение приближения")]
    [SerializeField] private float minZoom = 3f;
    [Tooltip("Максимальное значение отдаления")]
    [SerializeField] private float maxZoom = 15f;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        // Суммарное изменение "величины зума" за кадр (положительное = zoom in (уменьшение orthographicSize))
        float zoomDelta = 0f;

        // 1) Мышь (колёсико)
        if (Mouse.current != null)
        {
            Vector2 scroll = Mouse.current.scroll.ReadValue(); // обычно (0, scrollY)
            // В старом API: newSize = size - scroll * speed, значит положительное scroll => уменьшаем ortho (zoom in)
            if (!Mathf.Approximately(scroll.y, 0f))
            {
                zoomDelta += scroll.y * zoomSpeed;
            }
        }
        if (!Mathf.Approximately(zoomDelta, 0f))
        {
            // Применяем изменение: в старом варианте size -= scroll * speed
            float newSize = cam.orthographicSize - zoomDelta;
            cam.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
        }
    }
}
