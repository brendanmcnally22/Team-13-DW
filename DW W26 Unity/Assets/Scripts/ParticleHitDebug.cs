using UnityEngine;

public class ParticleHitDebug : MonoBehaviour
{
    void OnParticleCollision(GameObject other)
    {
        Debug.Log($"{name} got hit by particles from {other.name}");
    }
}
