using UnityEngine;
using UnityEngine.UI;

public class ShipHUD : MonoBehaviour
{
    [SerializeField] Slider speedSlider;
    [SerializeField] Slider throttleSlider;

    Rigidbody rb;
    ShipControllerFlight flight;

    public void Bind(Transform ship)
    {
        if (!ship) return;
        rb = ship.GetComponent<Rigidbody>();
        flight = ship.GetComponent<ShipControllerFlight>();
    }

    void Update()
    {
        if (rb == null || flight == null) return;

#if UNITY_6000_0_OR_NEWER
        float speed01 = rb.linearVelocity.magnitude / Mathf.Max(0.01f, flight.MaxSpeed);
#else
        float speed01 = rb.velocity.magnitude / Mathf.Max(0.01f, flight.MaxSpeed);
#endif
        if (speedSlider) speedSlider.value = Mathf.Clamp01(speed01);
        if (throttleSlider) throttleSlider.value = flight.Throttle01;
    }
}
