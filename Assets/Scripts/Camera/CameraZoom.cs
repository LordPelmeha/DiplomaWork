using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class CameraZoom : MonoBehaviour
{
    [Header("��������� ����")]
    [Tooltip("�������� ��������� ������� ������ ��� ���������")]
    [SerializeField] private float zoomSpeed = 5f;
    [Tooltip("����������� �������� �����������")]
    [SerializeField] private float minZoom = 3f;
    [Tooltip("������������ �������� ���������")]
    [SerializeField] private float maxZoom = 15f;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        // ��������� ��������� "�������� ����" �� ���� (������������� = zoom in (���������� orthographicSize))
        float zoomDelta = 0f;

        // 1) ���� (�������)
        if (Mouse.current != null)
        {
            Vector2 scroll = Mouse.current.scroll.ReadValue(); // ������ (0, scrollY)
            // � ������ API: newSize = size - scroll * speed, ������ ������������� scroll => ��������� ortho (zoom in)
            if (!Mathf.Approximately(scroll.y, 0f))
            {
                zoomDelta += scroll.y * zoomSpeed;
            }
        }
        if (!Mathf.Approximately(zoomDelta, 0f))
        {
            // ��������� ���������: � ������ �������� size -= scroll * speed
            float newSize = cam.orthographicSize - zoomDelta;
            cam.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
        }
    }
}
