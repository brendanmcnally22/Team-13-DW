using UnityEngine;

public class ThrottleLights : MonoBehaviour
{
    [SerializeField] ShipControllerFlight flight;
    [SerializeField] Light[] lights;

    [Header("Intensity")]
    [SerializeField] float idleIntensity = 1f;
    [SerializeField] float maxIntensity = 8f;

    [Header("Range")]
    [SerializeField] float idleRange = 6f;
    [SerializeField] float maxRange = 14f;

    [Header("Smoothing")]
    [SerializeField] float smooth = 12f;

    float iSm, rSm;

    void Awake()
    {
        if (flight == null) flight = GetComponentInParent<ShipControllerFlight>();
        if (lights == null || lights.Length == 0)
            lights = GetComponentsInChildren<Light>(true);
    }

    void Update()
    {
        if (flight == null || lights == null) return;

        float t = flight.Throttle01; // already ramped/smoothed
        float targetI = Mathf.Lerp(idleIntensity, maxIntensity, t);
        float targetR = Mathf.Lerp(idleRange, maxRange, t);

        iSm = Mathf.Lerp(iSm, targetI, smooth * Time.deltaTime);
        rSm = Mathf.Lerp(rSm, targetR, smooth * Time.deltaTime);

        for (int i = 0; i < lights.Length; i++)
        {
            if (!lights[i]) continue;
            lights[i].intensity = iSm;
            lights[i].range = rSm;
        }
    }
}
