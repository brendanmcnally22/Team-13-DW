using UnityEngine;

public class BoostSFX : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] AudioSource src;
    [SerializeField] AudioClip loopClip;

    [Header("Mix")]
    [Range(0f, 1f)][SerializeField] float targetVolume = 0.8f;
    [SerializeField] float fadeSpeed = 12f;
    [SerializeField] bool spatial3D = true;

    bool boosting;
    float currentVol;

    void Awake()
    {
        if (!src) src = GetComponent<AudioSource>();
        if (!src) src = gameObject.AddComponent<AudioSource>();

        src.playOnAwake = false;
        src.loop = true;
        src.clip = loopClip;

        src.spatialBlend = spatial3D ? 1f : 0f; // 1 = 3D, 0 = 2D
        src.volume = 0f;
        currentVol = 0f;
    }

    void OnDisable()
    {
        boosting = false;
        currentVol = 0f;
        if (src)
        {
            src.volume = 0f;
            if (src.isPlaying) src.Stop();
        }
    }

    public void SetBoosting(bool on)
    {
        boosting = on;
    }

    void Update()
    {
        if (!src || loopClip == null) return;

        // ensure clip is set (in case you assign later)
        if (src.clip != loopClip) src.clip = loopClip;

        float desired = boosting ? targetVolume : 0f;
        currentVol = Mathf.MoveTowards(currentVol, desired, fadeSpeed * Time.unscaledDeltaTime);
        src.volume = currentVol;

        // start/stop cleanly
        if (boosting && !src.isPlaying)
            src.Play();

        if (!boosting && src.isPlaying && currentVol <= 0.001f)
            src.Stop();
    }
}
