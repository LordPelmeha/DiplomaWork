using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("�������� ������������")]
    public float moveSpeed = 3.5f;

    Rigidbody2D rb;
    //Animator animator; 

    // Input System generated class (���������� PlayerControls ����� Input Actions asset)
    private PlayerControls controls;

    // ������� ������� ������ (-1..1)
    private Vector2 input = Vector2.zero;

    private float correctAngle = Mathf.Atan(0.5f) * Mathf.Rad2Deg;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        //animator = GetComponent<Animator>();

        // �������������� ��������������� �����
        controls = new PlayerControls();
    }

    void OnEnable()
    {
        // �������� ����� (Enable)
        controls.Enable();
    }

    void OnDisable()
    {
        // ���������, ����� �� ��������� ��������
        controls.Disable();
    }

    void Update()
    {
        // ������ ������� �������� Move ����� �� action
        // (������ � ������, �������� � � �����������, � � ���������)
        input = controls.Player.Move.ReadValue<Vector2>();

        // ���� ����������� Animator � ������� �������� ��� Blend Tree / ���������
        //if (animator != null)
        //{
        //    animator.SetFloat("MoveX", input.x);
        //    animator.SetFloat("MoveY", input.y);
        //    animator.SetFloat("Speed", input.sqrMagnitude);
        //}
    }

    void FixedUpdate()
    {
        Vector2 move = input;

        if (Mathf.Abs(input.x) > 0 && Mathf.Abs(input.y) > 0)
        {
            // ���������� �������� � ������� ���� (� ��������)
            float angleDeg;
            if (input.x > 0f && input.y > 0f)        
                angleDeg = correctAngle;
            else if (input.x < 0f && input.y > 0f)   
                angleDeg = 180f - correctAngle;
            else if (input.x < 0f && input.y < 0f)   
                angleDeg = 180f + correctAngle;
            else 
                angleDeg = 360f - correctAngle;

            float rad = angleDeg * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)); 

            // ��������� ���� ����� � �����������
            move = dir * input.magnitude;
        }

        Vector2 newPos = rb.position + move * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPos);
    }
}
