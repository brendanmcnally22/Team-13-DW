using UnityEngine;
using UnityEngine.InputSystem;

public class HoldBoostSystem : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] PlayerInput playerInput;
    [SerializeField] string boostActionName = "Boost"; // bind to X (buttonSouth)

    [Header("Boost feel")]
    [SerializeField] float boostAccel = 25f; // extra accel while held
    [SerializeField] float maxFuel = 3.0f;   // seconds of boost
    [SerializeField] float drainPerSecond = 1.0f;
    [SerializeField] float regenPerSecond = 0.5f;

    [Header("Debug")]
    [SerializeField] bool logIfMissingAction = true;

    Rigidbody rb;
    InputAction boostAction;
    float fuel;

    int slot = -1;
    public void SetSlot(int s) => slot = s;

    bool canBoost; // becomes true ONLY after GO

    void Awake()
    {
        if (!playerInput) playerInput = GetComponent<PlayerInput>();

        rb = GetComponentInChildren<Rigidbody>(true);
        if (rb == null) rb = GetComponent<Rigidbody>();

        fuel = maxFuel;
        canBoost = false;

        UpdateHUD(false); // show full meter right away
    }

    void OnEnable()
    {
        PlayerSpawn.OnRaceStarted += HandleRaceStarted;
        canBoost = (RaceManager.Instance != null && RaceManager.Instance.RaceActive);

        RefreshAction();
        UpdateHUD(false);
    }

    void OnDisable()
    {
        PlayerSpawn.OnRaceStarted -= HandleRaceStarted;
    }

    void HandleRaceStarted()
    {
        // GO moment
        canBoost = true;
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

        // if action didn't exist at OnEnable (map swap etc), try again
        if (boostAction == null) RefreshAction();

        // HARD GATE: no boost before race starts
        if (!canBoost || (RaceManager.Instance != null && !RaceManager.Instance.RaceActive))
        {
            // still regen + keep UI nice
            fuel += regenPerSecond * Time.fixedDeltaTime;
            fuel = Mathf.Clamp(fuel, 0f, maxFuel);
            UpdateHUD(false);
            return;
        }

        bool boosting = false;

        if (boostAction != null)
        {
            boosting = boostAction.IsPressed();

            // fallback for float actions
            if (!boosting)
            {
                float v = 0f;
                try { v = boostAction.ReadValue<float>(); } catch { }
                boosting = v > 0.5f;
            }
        }

        if (boosting && fuel > 0f)
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
        UpdateHUD(boosting && fuel > 0f);
    }

    void UpdateHUD(bool boostActive)
    {
        int useSlot = (slot >= 0) ? slot : (playerInput != null ? playerInput.playerIndex : -1);
        var hud = ShipHUD.Get(useSlot);
        if (hud == null) return;

        float norm = (maxFuel <= 0f) ? 0f : fuel / maxFuel;

        // NEW sprite meter + flame anim
        hud.SetBoost01(norm);
        hud.SetBoostActive(boostActive);

        // Legacy slider support (optional, safe to delete slider later)
        if (hud.boostSlider != null)
            hud.boostSlider.value = norm;
    }
}
