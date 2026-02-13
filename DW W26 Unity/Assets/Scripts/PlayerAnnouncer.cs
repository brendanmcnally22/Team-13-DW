using System;
using System.Collections;
using UnityEngine;

public class PlayerAnnouncer : MonoBehaviour
{
    public event Action<bool> OnTalkingChanged;

    [Header("Audio")]
    [SerializeField] AudioSource src;

    [Header("Wrong way lines")]
    [SerializeField] AudioClip[] wrongWayLines;
    [SerializeField] float wrongWayCooldown = 2.5f;

    [Header("Asteroid hit lines")]
    [SerializeField] AudioClip[] asteroidHitLines;
    [SerializeField] float hitCooldown = 0.8f;

    int wrongIndex;
    int hitIndex;
    float nextWrongTime;
    float nextHitTime;
    Coroutine talkCo;

    void Awake()
    {
        if (!src) src = GetComponent<AudioSource>();
        if (!src) src = gameObject.AddComponent<AudioSource>();

        src.playOnAwake = false;
        src.spatialBlend = 0f; // 2D voice
    }

    public void Play(AudioClip clip)
    {
        if (!clip || src == null) return;
        src.PlayOneShot(clip);
        KickTalkingFor(clip.length);
    }

    public void PlayWrongWayLine()
    {
        if (Time.time < nextWrongTime) return;
        nextWrongTime = Time.time + wrongWayCooldown;

        if (wrongWayLines == null || wrongWayLines.Length == 0) return;
        var clip = wrongWayLines[wrongIndex % wrongWayLines.Length];
        wrongIndex++;

        if (clip) Play(clip);
    }

    public void PlayAsteroidHitLine()
    {
        if (Time.time < nextHitTime) return;
        nextHitTime = Time.time + hitCooldown;

        if (asteroidHitLines == null || asteroidHitLines.Length == 0) return;
        var clip = asteroidHitLines[hitIndex % asteroidHitLines.Length];
        hitIndex++;

        if (clip) Play(clip);
    }

    void KickTalkingFor(float seconds)
    {
        if (talkCo != null) StopCoroutine(talkCo);
        talkCo = StartCoroutine(TalkRoutine(seconds));
    }

    IEnumerator TalkRoutine(float seconds)
    {
        OnTalkingChanged?.Invoke(true);
        yield return new WaitForSeconds(seconds);
        OnTalkingChanged?.Invoke(false);
        talkCo = null;
    }
}
