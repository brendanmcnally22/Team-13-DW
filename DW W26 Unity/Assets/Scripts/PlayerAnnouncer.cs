using UnityEngine;

public class PlayerAnnouncer : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] AudioSource src;

    [Header("Asteroid hit lines (rotate per-player)")]
    [SerializeField] AudioClip[] asteroidHitLines;
    [SerializeField] float hitCooldown = 0.8f;

    int hitIndex = 0;
    float nextHitTime = 0f;

    void Awake()
    {
        if (!src) src = GetComponent<AudioSource>();
        if (!src) src = gameObject.AddComponent<AudioSource>();

        src.playOnAwake = false;
        src.spatialBlend = 0f; // voice = 2D
    }

    // used by RacePositionUI + anything else
    public void Play(AudioClip clip)
    {
        if (!clip || src == null) return;
        src.PlayOneShot(clip);
    }

    // used when THIS player hits an asteroid
    public void PlayAsteroidHitLine()
    {
        if (Time.time < nextHitTime) return;
        nextHitTime = Time.time + hitCooldown;

        if (asteroidHitLines == null || asteroidHitLines.Length == 0) return;

        var clip = asteroidHitLines[hitIndex % asteroidHitLines.Length];
        hitIndex++;

        if (clip) src.PlayOneShot(clip);
    }
}
