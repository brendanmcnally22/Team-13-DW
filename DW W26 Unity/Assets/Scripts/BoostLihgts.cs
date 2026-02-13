using UnityEngine;
using UnityEngine.UI;

public class BoostLightsIntensity : MonoBehaviour
{
    [Header("UI Graphics (optional)")]
    [SerializeField] Graphic[] uiGraphics;
    [SerializeField] float uiNormalMul = 0.35f;
    [SerializeField] float uiBoostMul = 1.0f;

    [Header("World Lights (optional)")]
    [SerializeField] Light[] worldLights;
    [SerializeField] float lightNormalIntensity = 0.5f;
    [SerializeField] float lightBoostIntensity = 2.0f;

    [Header("Animation")]
    [SerializeField] float lerpSpeed = 12f;

    float target01;
    float current01;
    Color[] baseColors;

    void Awake()
    {
        if (uiGraphics == null || uiGraphics.Length == 0)
            uiGraphics = GetComponentsInChildren<Graphic>(true);

        if (worldLights == null || worldLights.Length == 0)
            worldLights = GetComponentsInChildren<Light>(true);

        baseColors = new Color[uiGraphics.Length];
        for (int i = 0; i < uiGraphics.Length; i++)
            baseColors[i] = uiGraphics[i] != null ? uiGraphics[i].color : Color.white;

        ApplyInstant(0f);
    }

    public void SetBoosting(bool boosting)
    {
        target01 = boosting ? 1f : 0f;
    }

    void Update()
    {
        current01 = Mathf.Lerp(current01, target01, lerpSpeed * Time.unscaledDeltaTime);
        ApplyInstant(current01);
    }

    void ApplyInstant(float t01)
    {
        // UI brightness
        float mul = Mathf.Lerp(uiNormalMul, uiBoostMul, t01);

        for (int i = 0; i < uiGraphics.Length; i++)
        {
            var g = uiGraphics[i];
            if (!g) continue;

            Color c = baseColors[i];

            c.r *= mul;
            c.g *= mul;
            c.b *= mul;

            c.a = Mathf.Clamp01(c.a * Mathf.Lerp(0.7f, 1.0f, t01));
            g.color = c;
        }

        // 3D light intensity
        float inten = Mathf.Lerp(lightNormalIntensity, lightBoostIntensity, t01);

        for (int i = 0; i < worldLights.Length; i++)
        {
            var l = worldLights[i];
            if (!l) continue;

            l.intensity = inten;
        }
    }
}