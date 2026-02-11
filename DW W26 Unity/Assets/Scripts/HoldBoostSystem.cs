using UnityEngine;
using UnityEngine.InputSystem;

public class HoldBoostSystem : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] PlayerInput playerInput;
    [SerializeField] string boostActionName = "Boost"; // bind this to X

    [Header("Boost feel")]
    [SerializeField] float boostAccel = 25f;
    [SerializeField] float maxFuel = 3.0f;
    [SerializeField] float drainPerSecond = 1.0f;
    [SerializeField] float regenPerSecond = 0.5f;

    Rigidbody rb;
    InputAction boostAction;
    float fuel;

    int slot = -1;
    public void SetSlot(int s) => slot = s;

    void Awake()
    {
        if (!playerInput) playerInput = GetComponent<PlayerInput>();
        rb = GetComponentInChildren<Rigidbody>(true);
        fuel = maxFuel;
    }

    void OnEnable()
    {
        boostAction = (playerInput && playerInput.actions != null)
            ? playerInput.actions.FindAction(boostActionName, false)
            : null;

        if (boostAction == null)
            Debug.LogWarning($"{name}: Boost action '{boostActionName}' not found. Check Input Actions.");
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        bool boosting = boostAction != null && boostAction.IsPressed();

        if (boosting && fuel > 0f)
        {
            rb.AddForce(transform.forward * boostAccel, ForceMode.Acceleration);
            fuel -= drainPerSecond * Time.fixedDeltaTime;
        }
        else
        {
            fuel += regenPerSecond * Time.fixedDeltaTime;
        }

        fuel = Mathf.Clamp(fuel, 0f, maxFuel);

        int useSlot = (slot >= 0) ? slot : (playerInput != null ? playerInput.playerIndex : -1);
        var hud = ShipHUD.Get(useSlot);

        if (hud != null && hud.boostSlider != null)
            hud.boostSlider.value = (maxFuel <= 0f) ? 0f : fuel / maxFuel;
    }
}
