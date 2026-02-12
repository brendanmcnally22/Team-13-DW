using UnityEngine;

public class UIAnnouncerBob : MonoBehaviour
{
    [SerializeField] RectTransform rect;
    [SerializeField] float amplitude = 6f;
    [SerializeField] float speed = 10f;

    Vector2 basePos;
    bool active;

    void Awake()
    {
        if (!rect) rect = GetComponent<RectTransform>();
        basePos = rect.anchoredPosition;
    }

    public void SetActive(bool on)
    {
        active = on;
        if (!active) rect.anchoredPosition = basePos;
    }

    void Update()
    {
        if (!active) return;
        float y = Mathf.Sin(Time.unscaledTime * speed) * amplitude;
        rect.anchoredPosition = basePos + new Vector2(0f, y);
    }
}
