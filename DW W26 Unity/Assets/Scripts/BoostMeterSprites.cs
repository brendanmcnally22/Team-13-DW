using UnityEngine;
using UnityEngine.UI;

public class BoostMeterSprites : MonoBehaviour
{
    [SerializeField] Image meterImage;

    [Tooltip("0 = empty, last = full")]
    [SerializeField] Sprite[] levelSprites;

    public void SetBoost01(float t)
    {
        if (!meterImage || levelSprites == null || levelSprites.Length == 0) return;

        int idx = Mathf.RoundToInt(t * (levelSprites.Length - 1));
        idx = Mathf.Clamp(idx, 0, levelSprites.Length - 1);
        meterImage.sprite = levelSprites[idx];
    }
}
