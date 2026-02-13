using UnityEngine;

public class BoostTrailColor : MonoBehaviour
{
    [SerializeField] TrailRenderer[] trails;

    [Header("Colors")]
    [SerializeField] Color normalStart = Color.white;
    [SerializeField] Color normalEnd = new Color(1f, 1f, 1f, 0f);
    [SerializeField] Color boostStart = Color.cyan;
    [SerializeField] Color boostEnd = new Color(0f, 1f, 1f, 0f);

    [Header("Optional: width on boost")]
    [SerializeField] bool changeWidth = false;
    [SerializeField] float normalWidth = 0.15f;
    [SerializeField] float boostWidth = 0.25f;

    void Awake()
    {
        if (trails == null || trails.Length == 0)
            trails = GetComponentsInChildren<TrailRenderer>(true);

        Apply(false);
    }

    public void SetBoosting(bool boosting)
    {
        Apply(boosting);
    }

    void Apply(bool boosting)
    {
        if (trails == null) return;

        for (int i = 0; i < trails.Length; i++)
        {
            var t = trails[i];
            if (!t) continue;

            t.startColor = boosting ? boostStart : normalStart;
            t.endColor = boosting ? boostEnd : normalEnd;

            if (changeWidth)
                t.widthMultiplier = boosting ? boostWidth : normalWidth;
        }
    }
}
