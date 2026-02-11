using UnityEngine;

public class PlayerAnnouncer : MonoBehaviour
{
    [SerializeField] AudioSource src;

    void Awake()
    {
        if (!src) src = GetComponent<AudioSource>();
        if (!src) src = gameObject.AddComponent<AudioSource>();

        src.playOnAwake = false;
        src.spatialBlend = 0f; // 2D voice (not positional)
    }

    public void Play(AudioClip clip)
    {
        if (!clip) return;
        src.PlayOneShot(clip);
    }
}
