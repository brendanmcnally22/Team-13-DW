using UnityEngine;

public class ShipDragFromTrails : MonoBehaviour
{
    [SerializeField] Rigidbody rb;
    [SerializeField] TrailRenderer[] trails;

    [Header("Drag Values")]
    [SerializeField] float baseDrag = 0f;
    [SerializeField] float baseAngularDrag = 0.05f;

    [SerializeField] float trailsDrag = 1.25f;
    [SerializeField] float trailsAngularDrag = 0.75f;

    void Awake()
    {
        if (!rb) rb = GetComponentInChildren<Rigidbody>(true);
        if (trails == null || trails.Length == 0)
            trails = GetComponentsInChildren<TrailRenderer>(true);

        Apply(false);
    }

    public void SetTrailsActive(bool on)
    {
        if (trails != null)
            for (int i = 0; i < trails.Length; i++)
                if (trails[i] != null) trails[i].emitting = on;

        Apply(on);
    }

    void Apply(bool on)
    {
        if (!rb) return;

        rb.linearDamping = on ? trailsDrag : baseDrag;
        rb.angularDamping = on ? trailsAngularDrag : baseAngularDrag;
    }
}
