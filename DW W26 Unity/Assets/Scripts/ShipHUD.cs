using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShipHUD : MonoBehaviour
{
    [Range(0, 1)] public int slot = 0;

    [Header("Assign UI on THIS canvas")]
    public Slider throttleSlider;

    [Tooltip("LEGACY (optional). You can remove this slider once sprites are hooked.")]
    public Slider boostSlider;

    public TMP_Text placeText;

    [Header("NEW Boost Sprites")]
    public BoostMeterSprites boostMeter;     // sprite meter (empty->full)
    public UISpriteFlipbook boostFlame;      // anim while boosting

    [Header("NEW Announcer Sprite")]
    public UISpriteFlipbook announcerTalking; // anim while announcer talks (shared intro OR per-player announcer)

    static ShipHUD[] huds = new ShipHUD[2];

    PlayerAnnouncer boundAnnouncer;

    void Awake()
    {
        huds[slot] = this;

        // Safe defaults
        SetBoostActive(false);
        SetAnnouncerTalking(false);
    }

    void OnDestroy()
    {
        if (huds[slot] == this) huds[slot] = null;
        
    }

    public static ShipHUD Get(int slot)
    {
        if (slot < 0 || slot > 1) return null;
        return huds[slot];
    }

    // ---- Boost ----

    public void SetBoost01(float t)
    {
        if (boostMeter != null) boostMeter.SetBoost01(t);
    }

    public void SetBoostActive(bool active)
    {
        if (boostFlame == null) return;

        if (active) boostFlame.Play();
        else boostFlame.Stop();
    }

    // ---- Announcer ----


    void HandleAnnouncerTalkingChanged(bool talking)
    {
        SetAnnouncerTalking(talking);
    }

    public void SetAnnouncerTalking(bool talking)
    {
        if (announcerTalking == null) return;

        if (talking) announcerTalking.Play();
        else announcerTalking.Stop();
    }
}
