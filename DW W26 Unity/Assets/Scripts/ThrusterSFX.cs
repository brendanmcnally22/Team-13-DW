using UnityEngine;
using UnityEngine.InputSystem;

public class ThrusterSFX : MonoBehaviour
{
    [SerializeField] PlayerInput playerInput;

    [Header("Action names")]
    [SerializeField] string throttleFloatAction = "Throttle";
    [SerializeField] string moveVectorAction = "Move";

    [Header("Audio")]
    [SerializeField] AudioSource src;
    [SerializeField] AudioClip thrusterLoop;

    [Header("Tuning")]
    [SerializeField] float minVol = 0.05f;
    [SerializeField] float maxVol = 0.9f;
    [SerializeField] float minPitch = 0.85f;
    [SerializeField] float maxPitch = 1.25f;
    [SerializeField] float smooth = 10f;

    float curVol, curPitch;

    void Awake()
    {
        if (!playerInput) playerInput = GetComponent<PlayerInput>();

        if (!src) src = GetComponent<AudioSource>();
        if (!src) src = gameObject.AddComponent<AudioSource>();

        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 1f; // 3D ship sound

        if (thrusterLoop) src.clip = thrusterLoop;
    }

    void OnEnable()
    {
        if (src != null && src.clip != null && !src.isPlaying)
            src.Play();
    }

    void Update()
    {
        if (!playerInput || playerInput.actions == null || src == null || src.clip == null) return;

        float t = ReadThrottle01();

        float targetVol = Mathf.Lerp(minVol, maxVol, t);
        float targetPitch = Mathf.Lerp(minPitch, maxPitch, t);

        curVol = Mathf.Lerp(curVol, targetVol, smooth * Time.deltaTime);
        curPitch = Mathf.Lerp(curPitch, targetPitch, smooth * Time.deltaTime);

        src.volume = curVol;
        src.pitch = curPitch;
    }

    float ReadThrottle01()
    {
        var a = playerInput.actions.FindAction(throttleFloatAction, false);
        if (a != null)
        {
            float raw = a.ReadValue<float>();
            return Mathf.InverseLerp(-1f, 1f, raw);
        }

        var mv = playerInput.actions.FindAction(moveVectorAction, false);
        if (mv != null)
        {
            Vector2 stick = mv.ReadValue<Vector2>();
            return Mathf.InverseLerp(-1f, 1f, stick.y);
        }

        return 0f;
    }
}
