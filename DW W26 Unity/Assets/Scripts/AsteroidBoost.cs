using UnityEngine;

public class AsteroidProximityBoost : MonoBehaviour
{
    [SerializeField] Rigidbody rb;

    [Header("Detection")]
    [SerializeField] LayerMask asteroidMask;
    [SerializeField] float radius = 12f;

    [Header("Boost")]
    [SerializeField] float accel = 18f; // extra accel while near asteroid

    [Header("Cooldown Voice")]
    [SerializeField] float voiceCooldown = 2f;

    PlayerAnnouncer announcer;
    float nextVoiceTime;

    void Awake()
    {
        if (!rb) rb = GetComponentInChildren<Rigidbody>(true);
        announcer = GetComponentInChildren<PlayerAnnouncer>(true);
    }

    void FixedUpdate()
    {
        if (!rb) return;
        if (RaceManager.Instance != null && !RaceManager.Instance.RaceActive) return;

        bool near = Physics.CheckSphere(rb.position, radius, asteroidMask, QueryTriggerInteraction.Collide);

        if (near)
        {
            rb.AddForce(rb.transform.forward * accel, ForceMode.Acceleration);

            if (announcer != null && Time.time >= nextVoiceTime)
            {
                // optional: use wrong-way lines? better to add a separate array if you want
                nextVoiceTime = Time.time + voiceCooldown;
            }
        }
    }
}
