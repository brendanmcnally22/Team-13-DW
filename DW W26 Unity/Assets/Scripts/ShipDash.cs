using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class ShipDash : MonoBehaviour
{
    [SerializeField] float dashImpulse = 10f;   // sideways kick
    [SerializeField] float dashCooldown = 0.6f;
    [SerializeField] float dashLockTime = 0.12f; // tiny “commit” so it feels snappy

    Rigidbody rb;
    PlayerInput pi;
    Gamepad pad;
    float nextDashTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pi = GetComponent<PlayerInput>();
    }

    void OnEnable()
    {
        pad = null;
        foreach (var d in pi.devices)
            if (d is Gamepad g) { pad = g; break; }
    }

    void Update()
    {
        if (pad == null) return;
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
        // optional: briefly disable flight so dash doesn’t get “fought”
        var flight = GetComponent<ShipControllerFlight>();
        if (flight != null) flight.enabled = false;

        rb.AddForce(transform.right * dir * dashImpulse, ForceMode.VelocityChange);

        yield return new WaitForSeconds(dashLockTime);

        if (flight != null) flight.enabled = true;
    }
}
