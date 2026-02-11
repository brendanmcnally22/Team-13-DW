using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class ShipReorient : MonoBehaviour
{
    PlayerInput pi;
    ShipControllerFlight flight;
    InputAction reorient;

    void Awake()
    {
        pi = GetComponent<PlayerInput>();
        flight = GetComponent<ShipControllerFlight>();
    }

    void OnEnable()
    {
        reorient = pi.actions.FindAction("Reorient", false);
        if (reorient == null)
            Debug.LogWarning($"{name}: No 'Reorient' action found. Add it in Input Actions.");
    }

    void Update()
    {
        if (flight == null || reorient == null) return;
        if (reorient.WasPressedThisFrame())
            flight.RecenterAim();
    }
}
