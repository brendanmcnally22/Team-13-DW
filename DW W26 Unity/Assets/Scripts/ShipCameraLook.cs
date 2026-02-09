using UnityEngine;
using UnityEngine.InputSystem;

public class ShipCameraLook : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float distance = 8f;
    [SerializeField] float height = 3f;
    [SerializeField] float lookSpeed = 140f;

    PlayerInput pi;
    InputAction lookAction;

    float yaw;
    float pitch;

    void Awake()
    {
        pi = GetComponentInParent<PlayerInput>();
    }

    void OnEnable()
    {
        if (pi != null && pi.actions != null)
            lookAction = pi.actions.FindAction("Player/Look");
    }

    void LateUpdate()
    {
        if (target == null || lookAction == null) return;

        Vector2 look = lookAction.ReadValue<Vector2>();

        yaw += look.x * lookSpeed * Time.deltaTime;
        pitch -= look.y * lookSpeed * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, -25f, 45f);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 pos = target.position - rot * Vector3.forward * distance + Vector3.up * height;
        transform.position = pos;
        transform.LookAt(target.position + Vector3.up * 1.2f);
    }

    public void SetTarget(Transform t)
    {
        target = t;
    }
}
