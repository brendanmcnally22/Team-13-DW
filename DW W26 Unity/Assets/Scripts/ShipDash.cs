using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class ShipDash : MonoBehaviour
{
    [SerializeField] float dashImpulse = 10f;     // sideways kick
    [SerializeField] float dashCooldown = 0.6f;
    [SerializeField] float dashLockTime = 0.12f;  // keep this (commit)

    [Header("Optional")]
    [SerializeField] float dashRumble = 0.85f;
    [SerializeField] float dashRumbleTime = 0.10f;

    Rigidbody rb;
    PlayerInput pi;
    Gamepad pad;
    float nextDashTime;
    bool dashLocked;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pi = GetComponent<PlayerInput>();
    }

    void OnEnable()
    {
        // only use the paired device (prevents overlap)
        pad = null;
        foreach (var d in pi.devices)
            if (d is Gamepad g) { pad = g; break; }
    }

    void Update()
    {
        if (pad == null) return;
        if (dashLocked) return;
        if (Time.time < nextDashTime) return;

        float dir = 0f;
        if (pad.dpad.left.wasPressedThisFrame) dir = -1f;
        if (pad.dpad.right.wasPressedThisFrame) dir = 1f;

        if (dir != 0f)
        {
            nextDashTime = Time.time + dashCooldown;
            StartCoroutine(DashRoutine(dir));
        }
    }

    IEnumerator DashRoutine(float dir)
    {
        dashLocked = true;

        // Tell flight "don't kill my sideways velocity for a moment"
        var flight = GetComponent<ShipControllerFlight>();
        if (flight != null)
        {
            flight.BeginDash(dashLockTime);
            flight.AddExternalRumbleBurst(dashRumble, dashRumbleTime);
        }

        // IMPORTANT: do NOT disable flight here
        rb.AddForce(transform.right * dir * dashImpulse, ForceMode.VelocityChange);

        yield return new WaitForSeconds(dashLockTime);
        dashLocked = false;
    }
}
