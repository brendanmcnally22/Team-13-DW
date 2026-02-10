using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class ShipReorient : MonoBehaviour
{
    PlayerInput pi;
    ShipControllerFlight flight;
    Gamepad pad;

    void Awake()
    {
        pi = GetComponent<PlayerInput>();
        flight = GetComponent<ShipControllerFlight>();
    }

    void OnEnable()
    {
        pad = null;
        foreach (var d in pi.devices)
            if (d is Gamepad g) { pad = g; break; }
    }

    void Update()
    {
        if (flight == null || pad == null) return;

        // PS5 touchpad click often maps to selectButton. If it doesn’t, bind it in Input Actions.
        if (pad.selectButton.wasPressedThisFrame)
            flight.RecenterAim();
    }
}
