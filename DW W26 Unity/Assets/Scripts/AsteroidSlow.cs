using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ParticleAsteroidSlow : MonoBehaviour
{
    [SerializeField] float velocityMulOnHit = 0.80f;
    [SerializeField] float minTimeBetweenHits = 0.15f;

    Rigidbody rb;
    float nextTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnParticleCollision(GameObject other)
    {
        if (Time.time < nextTime) return;
        nextTime = Time.time + minTimeBetweenHits;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity *= velocityMulOnHit;
#else
        rb.velocity *= velocityMulOnHit;
#endif
    }
}
