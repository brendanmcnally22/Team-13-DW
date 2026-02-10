using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShipAsteroidSlow : MonoBehaviour
{
    [SerializeField] float speedMultiplierOnHit = 0.55f; // 55% speed
    [SerializeField] float stunTime = 0.35f;             // short “ow” moment

    Rigidbody rb;
    ShipControllerFlight flight;
    bool stunned;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        flight = GetComponent<ShipControllerFlight>();
    }

    void OnCollisionEnter(Collision col)
    {
        if (stunned) return;
        if (!col.collider.CompareTag("Asteroid")) return;

        StartCoroutine(SlowRoutine());
    }

    IEnumerator SlowRoutine()
    {
        stunned = true;

        // cut speed immediately
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity *= speedMultiplierOnHit;
#else
        rb.velocity *= speedMultiplierOnHit;
#endif

        // optional: disable flight briefly to really “feel” the penalty
        if (flight != null) flight.enabled = false;

        yield return new WaitForSeconds(stunTime);

        if (flight != null) flight.enabled = true;
        stunned = false;
    }
}
