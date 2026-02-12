using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShipHUD : MonoBehaviour
{
    [Range(0, 1)] public int slot = 0;

    [Header("Assign UI on THIS canvas")]
    public Slider throttleSlider;
    public Slider boostSlider;
    public TMP_Text placeText;

    static ShipHUD[] huds = new ShipHUD[2];

    void Awake()
    {
        huds[slot] = this;
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
}
