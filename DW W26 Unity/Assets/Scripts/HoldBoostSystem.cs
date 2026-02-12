using UnityEngine;
using UnityEngine.InputSystem;

public class HoldBoostSystem : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] PlayerInput playerInput;
    [SerializeField] string boostActionName = "Boost"; // bind to X (buttonSouth)

    [Header("Boost feel")]
    [SerializeField] float boostAccel = 25f;
    [SerializeField] float maxFuel = 3.0f;   // seconds of boost
    [SerializeField] float drainPerSecond = 1.0f;
    [SerializeField] float regenPerSecond = 0.5f;

    [Header("Lockout")]
    [Tooltip("When fuel hits 0, boost is locked until it regens back to FULL.")]
    [SerializeField] bool lockUntilFullAfterEmpty = true;

    [Header("Debug")]
    [SerializeField] bool logIfMissingAction = true;

    Rigidbody rb;
    InputAction boostAction;

    [SerializeField] float fuel; // visible in inspector
    bool emptyLock;              // true when we hit 0 and must regen to full

    int slot = -1;
    public void SetSlot(int s) => slot = s;

    void Awake()
    {
        if (!playerInput) playerInput = GetComponent<PlayerInput>();

        rb = GetComponentInChildren<Rigidbody>(true);
        if (rb == null) rb = GetComponent<Rigidbody>();

        fuel = maxFuel;
    }

    void OnEnable()
    {
        RefreshAction();
    }

    void RefreshAction()
    {
        if (playerInput == null || playerInput.actions == null) return;

        boostAction = playerInput.actions.FindAction(boostActionName, false);

        if (boostAction != null && !boostAction.enabled)
            boostAction.Enable();

        if (boostAction == null && logIfMissingAction)
            Debug.LogWarning($"{name}: Boost action '{boostActionName}' not found. Make sure the action name matches EXACTLY.");
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        if (boostAction == null) RefreshAction();

        // If race isn't active, don't allow boosting (still regen fuel + update UI)
        bool raceAllowsBoost = true;
        if (RaceManager.Instance != null && !RaceManager.Instance.RaceActive)
            raceAllowsBoost = false;

        bool inputHeld = false;
        if (raceAllowsBoost && boostAction != null)
        {
            // Button action
            inputHeld = boostAction.IsPressed();

            // In case it’s float-based
            if (!inputHeld)
            {
                float v = 0f;
                try { v = boostAction.ReadValue<float>(); } catch { }
                inputHeld = v > 0.5f;
            }
        }

        // Lockout logic:
        // If we hit 0 fuel, lock until fuel returns to full.
        if (lockUntilFullAfterEmpty)
        {
            if (!emptyLock && fuel <= 0f)
                emptyLock = true;

            if (emptyLock && fuel >= maxFuel)
                emptyLock = false;
        }
        else
        {
            emptyLock = false;
        }

        bool canBoost = inputHeld && fuel > 0f && !emptyLock;

        if (canBoost)
        {
            Vector3 dir = rb.transform.forward;
            rb.AddForce(dir * boostAccel, ForceMode.Acceleration);

            fuel -= drainPerSecond * Time.fixedDeltaTime;
        }
        else
        {
            fuel += regenPerSecond * Time.fixedDeltaTime;
        }

        fuel = Mathf.Clamp(fuel, 0f, maxFuel);

        // Update HUD
        int useSlot = (slot >= 0) ? slot : (playerInput != null ? playerInput.playerIndex : -1);
        var hud = ShipHUD.Get(useSlot);

        float norm = (maxFuel <= 0f) ? 0f : fuel / maxFuel;

        if (hud != null)
        {
            hud.SetBoost01(norm);
            // Flame only while actively boosting (not when locked / empty)
            hud.SetBoostActive(canBoost);
        }
    }
}
