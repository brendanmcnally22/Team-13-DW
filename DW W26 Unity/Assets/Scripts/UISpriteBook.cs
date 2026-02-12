using UnityEngine;
using UnityEngine.UI;

public class UISpriteFlipbook : MonoBehaviour
{
    [SerializeField] Image image;
    [SerializeField] Sprite idleSprite;
    [SerializeField] Sprite[] frames;
    [SerializeField] float fps = 12f;
    [SerializeField] bool loop = true;

    int frameIndex;
    float timer;
    bool playing;

    void Awake()
    {
        if (!image) image = GetComponent<Image>();
        Stop();
    }

    public void Play()
    {
        if (!image || frames == null || frames.Length == 0) return;

        playing = true;
        frameIndex = 0;
        timer = 0f;
        image.sprite = frames[0];
    }

    public void Stop()
    {
        playing = false;
        timer = 0f;
        frameIndex = 0;

        if (image && idleSprite)
            image.sprite = idleSprite;
    }

    void Update()
    {
        if (!playing || !image || frames == null || frames.Length == 0) return;

        timer += Time.unscaledDeltaTime;
        float frameTime = 1f / Mathf.Max(1f, fps);

        while (timer >= frameTime)
        {
            timer -= frameTime;
            frameIndex++;

            if (frameIndex >= frames.Length)
            {
                if (loop) frameIndex = 0;
                else { Stop(); return; }
            }

            image.sprite = frames[frameIndex];
        }
    }
}
