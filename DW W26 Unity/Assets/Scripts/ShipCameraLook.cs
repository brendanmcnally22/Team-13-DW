using UnityEngine;

public class ShipCameraFollow : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] Transform target;        // ship
    [SerializeField] Transform aim;           // ship/AimPivot (recommended)

    [Header("Follow")]
    [SerializeField] float distance = 12f;
    [SerializeField] float height = 4f;

    [Header("Smoothing")]
    [SerializeField] float posSmoothTime = 0.15f;
    [SerializeField] float rotSharpness = 10f;

    [Header("Look At")]
    [SerializeField] Vector3 lookAtOffset = new Vector3(0f, 1.0f, 0f); // look slightly above ship

    Vector3 posVel;

    void LateUpdate()
    {
        if (target == null) return;

        // follow behind where you're aiming (if aim exists), otherwise behind the ship
        Transform dirSource = (aim != null) ? aim : target;

        Vector3 desiredPos = target.position - dirSource.forward * distance + Vector3.up * height;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref posVel, posSmoothTime);

        Vector3 lookTarget = target.position + target.TransformDirection(lookAtOffset);
        Quaternion desiredRot = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);

        // nicer smoothing than raw slerp * dt
        float t = 1f - Mathf.Exp(-rotSharpness * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, t);
    }

    public void SetTarget(Transform t) => target = t;
    public void SetAim(Transform a) => aim = a;
}
