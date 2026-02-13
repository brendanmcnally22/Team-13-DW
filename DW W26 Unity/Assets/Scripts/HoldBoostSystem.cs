using UnityEngine;
using UnityEngine.InputSystem;

public class HoldBoostSystem : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] PlayerInput playerInput;
    [SerializeField] string boostActionName = "Boost"; // X (buttonSouth)

    [Header("Boost feel")]
    [SerializeField] float boostAccel = 25f;
    [SerializeField] float maxFuel = 3.0f;
    [SerializeField] float drainPerSecond = 1.0f;
    [SerializeField] float regenPerSecond = 0.5f;

    [Header("Lockout")]
    [SerializeField] bool lockUntilFullAfterEmpty = true;

    [Header("Rumble (while boosting)")]
    [SerializeField] bool rumbleEnabled = true;
    [Range(0f, 1f)][SerializeField] float lowFreq = 0.25f;
    [Range(0f, 1f)][SerializeField] float highFreq = 0.5f;

    [Header("Debug")]
    [SerializeField] bool debugLogs = true;

    Rigidbody rb;
    InputAction boostAction;

    float fuel;
    bool emptyLock;
    bool wasBoosting;

    int slot = -1;
    public void SetSlot(int s) => slot = s;

    BoostLightsIntensity boostLights;
    BoostTrailColor trailColor;

    void Awake()
    {
        // ✅ Fix #1: find PlayerInput even if this script is on a child
        if (!playerInput)
            playerInput = GetComponent<PlayerInput>()
                      ?? GetComponentInParent<PlayerInput>()
                      ?? GetComponentInChildren<PlayerInput>(true);

        rb = GetComponent<Rigidbody>();
        if (!rb) rb = GetComponentInChildren<Rigidbody>(true);

        boostLights = GetComponentInChildren<BoostLightsIntensity>(true);
        trailColor = GetComponentInChildren<BoostTrailColor>(true);

        fuel = maxFuel;

        // If slot not set from PlayerSpawn, try tracker
        if (slot < 0)
        {
            var tracker = GetComponentInParent<CircularProgressTracker>();
            if (tracker == null) tracker = GetComponent<CircularProgressTracker>();
            if (tracker != null) slot = tracker.Slot;
        }

        if (debugLogs)
        {
            Debug.Log($"{name}: HoldBoost Awake | PI={(playerInput ? playerInput.name : "NULL")} | rb={(rb ? rb.name : "NULL")} | maxFuel={maxFuel}", this);
        }
    }

    void OnEnable()
    {
        RefreshAction();
    }

    void OnDisable()
    {
        StopRumble();
        wasBoosting = false;
    }

    void RefreshAction()
    {
        boostAction = null;

        if (playerInput == null || playerInput.actions == null)
        {
            if (debugLogs) Debug.LogWarning($"{name}: PlayerInput/actions missing -> boost cannot work.", this);
            return;
        }

        boostAction = playerInput.actions.FindAction(boostActionName, false);

        if (boostAction == null)
        {
            if (debugLogs)
                Debug.LogWarning($"{name}: Boost action '{boostActionName}' NOT FOUND on {playerInput.actions.name}. Check action name + action map.", this);
            return;
        }

        if (!boostAction.enabled)
            boostAction.Enable();

        if (debugLogs)
            Debug.Log($"{name}: Boost action found + enabled. CurrentMap={(playerInput.currentActionMap != null ? playerInput.currentActionMap.name : "NULL")}", this);
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        if (boostAction == null) RefreshAction();

        bool raceAllowsBoost = (RaceManager.Instance == null) || RaceManager.Instance.RaceActive;

        bool held = false;
        if (raceAllowsBoost && boostAction != null)
            held = boostAction.IsPressed();

        // Lockout logic
        if (lockUntilFullAfterEmpty)
        {
            if (!emptyLock && fuel <= 0f) emptyLock = true;
            if (emptyLock && fuel >= maxFuel) emptyLock = false;
        }
        else emptyLock = false;

        bool canBoost = held && fuel > 0f && !emptyLock;

        if (canBoost)
        {
            rb.AddForce(rb.transform.forward * boostAccel, ForceMode.Acceleration);
            fuel -= drainPerSecond * Time.fixedDeltaTime;
        }
        else
        {
            fuel += regenPerSecond * Time.fixedDeltaTime;
        }

        fuel = Mathf.Clamp(fuel, 0f, maxFuel);

        // Slot resolution
        int useSlot = slot;
        if (useSlot < 0 || useSlot > 1)
            useSlot = playerInput != null ? playerInput.playerIndex : 0;

        float norm = (maxFuel <= 0f) ? 0f : fuel / maxFuel;

        var hud = ShipHUD.Get(useSlot);
        if (hud != null)
        {
            hud.SetBoost01(norm);
            hud.SetBoostActive(canBoost);
        }

        // visuals
        boostLights?.SetBoosting(canBoost);
        trailColor?.SetBoosting(canBoost);

        // rumble
        if (rumbleEnabled)
        {
            if (canBoost && !wasBoosting) StartRumble();
            if (!canBoost && wasBoosting) StopRumble();
        }
        wasBoosting = canBoost;

        // 🔎 Debug only when player is holding boost (so console doesn’t spam)
        if (debugLogs && held)
        {
            Debug.Log($"{name}: held={held} canBoost={canBoost} fuel={fuel:F2}/{maxFuel:F2} emptyLock={emptyLock} slot={useSlot}", this);
        }
    }

    void StartRumble()
    {
        var pad = GetPlayerGamepad();
        if (pad == null) return;
        pad.SetMotorSpeeds(lowFreq, highFreq);
    }

    void StopRumble()
    {
        var pad = GetPlayerGamepad();
        if (pad == null) return;
        pad.SetMotorSpeeds(0f, 0f);
    }

    Gamepad GetPlayerGamepad()
    {
        if (playerInput != null)
        {
            for (int i = 0; i < playerInput.devices.Count; i++)
                if (playerInput.devices[i] is Gamepad gp)
                    return gp;
        }
        return Gamepad.current;
    }
}
