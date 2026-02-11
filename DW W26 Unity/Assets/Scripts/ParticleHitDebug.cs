using UnityEngine;

public class ShipCollisionDebug : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"TRIGGER ENTER: {other.name} layer={LayerMask.LayerToName(other.gameObject.layer)}", other);
    }

    void OnCollisionEnter(Collision c)
    {
        Debug.Log($"COLLISION ENTER: {c.collider.name} layer={LayerMask.LayerToName(c.collider.gameObject.layer)}", c.collider);
    }

    void OnParticleCollision(GameObject other)
    {
        Debug.Log($"PARTICLE HIT from: {other.name} layer={LayerMask.LayerToName(other.layer)}", other);
    }
}
