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

        // Optional: make sure sliders are normalized
        if (speedSlider)
        {
            speedSlider.minValue = 0f;
            speedSlider.maxValue = 1f;
        }
        if (throttleSlider)
        {
            throttleSlider.minValue = 0f;
            throttleSlider.maxValue = 1f;
        }
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
        if (throttleSlider) throttleSlider.value = Mathf.Clamp01(flight.Throttle01);
    }
}
