using UnityEngine;

public class SimpleFollowCam : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 4, -12);
    public float smooth = 8f;

    void LateUpdate()
    {
        if (!target) return;
        Vector3 desired = target.position + target.rotation * offset;
        transform.position = Vector3.Lerp(transform.position, desired, smooth * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(target.forward, Vector3.up),
            smooth * Time.deltaTime);
    }
}
