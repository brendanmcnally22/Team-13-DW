using UnityEngine;
using UnityEngine.UI;

public class BoostMeterSprites : MonoBehaviour
{
    [SerializeField] Image img;

    [Tooltip("Order must be EMPTY -> FULL (boost_0, boost_1, ... boost_full)")]
    [SerializeField] Sprite[] frames;

    [Tooltip("If true, only changes when crossing frame thresholds (classic meter steps).")]
    [SerializeField] bool stepped = true;

    void Awake()
    {
        if (!img) img = GetComponent<Image>();
    }

    public void Set01(float t01)
    {
        if (!img || frames == null || frames.Length == 0) return;

        t01 = Mathf.Clamp01(t01);

        int idx;
        if (stepped)
        {
            // 5 frames => changes at 0.2, 0.4, 0.6, 0.8
            idx = Mathf.FloorToInt(t01 * frames.Length);
        }
        else
        {
            // slightly “more responsive” with few frames
            idx = Mathf.RoundToInt(t01 * (frames.Length - 1));
        }

        idx = Mathf.Clamp(idx, 0, frames.Length - 1);
        img.sprite = frames[idx];
    }
}
