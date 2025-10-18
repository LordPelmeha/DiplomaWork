using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;       
    public Vector3 offset;
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    void LateUpdate()
    {
        if (target == null) return;

        transform.position = target.position + offset;
    }
}
