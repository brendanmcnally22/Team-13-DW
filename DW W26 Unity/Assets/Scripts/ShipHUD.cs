using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShipHUD : MonoBehaviour
{
    [Range(0, 1)] public int slot = 0;

    [Header("Assign UI on THIS canvas")]
    public Slider throttleSlider;
    public Slider boostSlider; // optional legacy
    public TMP_Text placeText;

    [Header("Boost UI")]
    public BoostMeterSprites boostMeter; // value-based sprite swap
    public UISpriteFlipbook boostFlame;  // time-based flame anim (optional)

    [Header("Announcer UI")]
    public UISpriteFlipbook announcerTalking; // optional mouth anim
    public UIAnnouncerBob announcerBob;       // optional bobbing

    [Header("Wrong Way UI")]
    public GameObject wrongWayRoot; // set active when wrong way (Text/Image/etc)

    [Header("Restart UI (end of race)")]
    public GameObject restartPopupRoot; // panel/root object on THIS canvas
    public TMP_Text restartPopupText;   // TMP text inside the popup (optional)

    static ShipHUD[] huds = new ShipHUD[2];

    PlayerAnnouncer boundAnnouncer;

    void Awake()
    {
        slot = Mathf.Clamp(slot, 0, 1);

        // warn if you accidentally set both canvases to same slot
        if (huds[slot] != null && huds[slot] != this)
            Debug.LogWarning($"ShipHUD slot {slot} already assigned. Check both canvases have different slot values (0 and 1).", this);

        huds[slot] = this;

        SetBoostActive(false);
        SetAnnouncerTalking(false);
        SetWrongWay(false);
        ShowRestartPopup(false);
    }

    void OnDestroy()
    {
        if (huds[slot] == this) huds[slot] = null;
        BindAnnouncer(null);
    }

    public static ShipHUD Get(int slot)
    {
        if (slot < 0 || slot > 1) return null;
        return huds[slot];
    }

    // ---- Boost ----
    public void SetBoost01(float t01)
    {
        if (boostMeter != null) boostMeter.Set01(t01);
        if (boostSlider != null) boostSlider.value = Mathf.Clamp01(t01);
    }

    public void SetBoostActive(bool active)
    {
        if (boostFlame != null)
        {
            if (active) boostFlame.Play();
            else boostFlame.Stop();
        }
    }

    // ---- Announcer ----
    public void BindAnnouncer(PlayerAnnouncer announcer)
    {
        if (boundAnnouncer != null)
            boundAnnouncer.OnTalkingChanged -= OnAnnouncerTalkingChanged;

        boundAnnouncer = announcer;

        if (boundAnnouncer != null)
            boundAnnouncer.OnTalkingChanged += OnAnnouncerTalkingChanged;
    }

    void OnAnnouncerTalkingChanged(bool talking)
    {
        SetAnnouncerTalking(talking);
    }

    public void SetAnnouncerTalking(bool talking)
    {
        if (announcerTalking != null)
        {
            if (talking) announcerTalking.Play();
            else announcerTalking.Stop();
        }

        if (announcerBob != null)
            announcerBob.SetActive(talking);
    }

    // ---- Wrong way ----
    public void SetWrongWay(bool wrong)
    {
        if (wrongWayRoot != null)
            wrongWayRoot.SetActive(wrong);
    }

    // ---- Restart popup ----
    public void ShowRestartPopup(bool show, string msg = null)
    {
        if (restartPopupRoot != null)
            restartPopupRoot.SetActive(show);

        if (restartPopupText != null && !string.IsNullOrEmpty(msg))
            restartPopupText.text = msg;
    }

    public void SetRestartProgress01(float t01)
    {
        if (restartPopupText == null) return;

        int pct = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(t01) * 100f), 0, 100);
        restartPopupText.text = $"HOLD △ TO RESTART ({pct}%)";
    }
}
