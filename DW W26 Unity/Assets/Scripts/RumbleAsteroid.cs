using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class RumbleOnImpact : MonoBehaviour
{
    [SerializeField] PlayerInput playerInput;

    [Header("Tuning")]
    [SerializeField] float minImpact = 2f;
    [SerializeField] float maxImpact = 15f;
    [SerializeField] float rumbleDuration = 0.15f;

    Gamepad pad;
    Coroutine rumbleCo;

    void Awake()
    {
        if (!playerInput) playerInput = GetComponent<PlayerInput>();
        pad = FindPadForThisPlayer();
    }

    Gamepad FindPadForThisPlayer()
    {
        if (!playerInput) return null;

        foreach (var d in playerInput.devices)
            if (d is Gamepad g) return g;

        return null;
    }

    void OnCollisionEnter(Collision c)
    {
        // optional: only rumble on asteroids (tag them "Asteroid" or use layer)
        // if (!c.collider.CompareTag("Asteroid")) return;

        if (pad == null) pad = FindPadForThisPlayer();
        if (pad == null) return;

        float impact = c.relativeVelocity.magnitude;
        if (impact < minImpact) return;

        float t = Mathf.InverseLerp(minImpact, maxImpact, impact);
        float low = Mathf.Lerp(0.1f, 0.6f, t);
        float high = Mathf.Lerp(0.2f, 1.0f, t);

        if (rumbleCo != null) StopCoroutine(rumbleCo);
        rumbleCo = StartCoroutine(Rumble(low, high, rumbleDuration));
    }

    IEnumerator Rumble(float low, float high, float time)
    {
        pad.SetMotorSpeeds(low, high);
        yield return new WaitForSeconds(time);
        pad.SetMotorSpeeds(0f, 0f);
    }

    void OnDisable()
    {
        if (pad != null) pad.SetMotorSpeeds(0f, 0f);
    }
}
