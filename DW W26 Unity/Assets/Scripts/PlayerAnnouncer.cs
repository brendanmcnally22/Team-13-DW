using System;
using UnityEngine;

public class PlayerAnnouncer : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] AudioSource src;

    [Header("Asteroid hit lines (rotate per-player)")]
    [SerializeField] AudioClip[] asteroidHitLines;
    [SerializeField] float hitCooldown = 0.8f;

    public event Action<bool> OnTalkingChanged;

    int hitIndex = 0;
    float nextHitTime = 0f;

    float talkUntil;
    bool talking;

    void Awake()
    {
        if (!src) src = GetComponent<AudioSource>();
        if (!src) src = gameObject.AddComponent<AudioSource>();

        src.playOnAwake = false;
        src.spatialBlend = 0f;
        SetTalking(false);
    }

    void Update()
    {
        if (talking && Time.unscaledTime >= talkUntil)
            SetTalking(false);
    }

    public void Play(AudioClip clip)
    {
        if (!clip || src == null) return;
        src.PlayOneShot(clip);
        BumpTalkingTimer(clip.length);
    }

    public void PlayAsteroidHitLine()
    {
        if (Time.time < nextHitTime) return;
        nextHitTime = Time.time + hitCooldown;

        if (asteroidHitLines == null || asteroidHitLines.Length == 0) return;

        var clip = asteroidHitLines[hitIndex % asteroidHitLines.Length];
        hitIndex++;

        if (!clip || src == null) return;

        src.PlayOneShot(clip);
        BumpTalkingTimer(clip.length);
    }

    void BumpTalkingTimer(float clipLen)
    {
        talkUntil = Mathf.Max(talkUntil, Time.unscaledTime + Mathf.Max(0.05f, clipLen));
        SetTalking(true);
    }

    void SetTalking(bool value)
    {
        if (talking == value) return;
        talking = value;
        OnTalkingChanged?.Invoke(talking);
    }
}
