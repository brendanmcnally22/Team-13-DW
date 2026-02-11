using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ParticleAsteroidHit : MonoBehaviour
{
    [SerializeField] float slowMultiplier = 0.85f;
    [SerializeField] float pushBack = 1.8f;
    [SerializeField] float hitCooldown = 0.08f;

    Rigidbody rb;
    ShipControllerFlight flight;

    float nextHitTime;

    static readonly List<ParticleCollisionEvent> events = new List<ParticleCollisionEvent>(64);

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        flight = GetComponent<ShipControllerFlight>();
    }

    void OnParticleCollision(GameObject other)
    {
        if (Time.time < nextHitTime) return;

        var ps = other.GetComponent<ParticleSystem>();
        if (ps == null) return;

        int count = ps.GetCollisionEvents(gameObject, events);
        if (count <= 0) return;

        nextHitTime = Time.time + hitCooldown;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity *= slowMultiplier;
#else
        rb.velocity *= slowMultiplier;
#endif

        // shove away from average hit point
        Vector3 avg = Vector3.zero;
        for (int i = 0; i < count; i++) avg += events[i].intersection;
        avg /= count;

        Vector3 away = (transform.position - avg).normalized;
        rb.AddForce(away * pushBack, ForceMode.VelocityChange);

        if (flight != null)
            flight.AddExternalRumbleBurst(0.8f, 0.12f);
    }
}
