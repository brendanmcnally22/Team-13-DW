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

    [Header("Race Gate")]
    [SerializeField] bool requireRaceActive = true;

    [Header("Lockout")]
    [SerializeField] bool lockUntilFullAfterEmpty = true;

    [Header("Rumble (while boosting)")]
    [SerializeField] bool rumbleEnabled = true;
    [Range(0f, 1f)][SerializeField] float lowFreq = 0.25f;
    [Range(0f, 1f)][SerializeField] float highFreq = 0.5f;

    [Header("Debug")]
    [SerializeField] bool debugLogs = false;
    [SerializeField] float debugEverySeconds = 0.35f;

    Rigidbody rb;
    InputAction boostAction;

    float fuel;
    bool emptyLock;
    bool wasBoosting;

    float nextDebugTime;

    int slot = -1;
    public void SetSlot(int s) => slot = s;

    BoostLightsIntensity boostLights;
    BoostTrailColor trailColor;
    BoostSFX boostSfx;

    Gamepad cachedPad;

    void Awake()
    {
        if (!playerInput)
            playerInput = GetComponent<PlayerInput>()
                      ?? GetComponentInParent<PlayerInput>()
                      ?? GetComponentInChildren<PlayerInput>(true);

        rb = GetComponent<Rigidbody>();
        if (!rb) rb = GetComponentInChildren<Rigidbody>(true);

        boostLights = GetComponentInChildren<BoostLightsIntensity>(true);
        trailColor = GetComponentInChildren<BoostTrailColor>(true);
        boostSfx = GetComponentInChildren<BoostSFX>(true);

        fuel = Mathf.Max(0f, maxFuel);

        if (slot < 0)
        {
            var tracker = GetComponentInParent<CircularProgressTracker>();
            if (tracker == null) tracker = GetComponent<CircularProgressTracker>();
            if (tracker != null) slot = tracker.Slot;
        }

        CachePad();

        if (debugLogs)
            Debug.Log($"{name}: HoldBoost Awake | PI={(playerInput ? playerInput.name : "NULL")} | rb={(rb ? rb.name : "NULL")} | maxFuel={maxFuel}", this);
    }

    void OnEnable()
    {
        RefreshAction();
        CachePad();
        SetVisualsAndAudio(false);
        StopRumble();
        wasBoosting = false;
    }

    void OnDisable()
    {
        SetVisualsAndAudio(false);
        StopRumble();
        wasBoosting = false;
    }

    void CachePad()
    {
        cachedPad = null;

        if (playerInput == null) return;

        // ✅ devices is a ReadOnlyArray -> never null; just check Count
        var devices = playerInput.devices;
        for (int i = 0; i < devices.Count; i++)
        {
            if (devices[i] is Gamepad gp)
            {
                cachedPad = gp;
                break;
            }
        }
    }

    void RefreshAction()
    {
        boostAction = null;

        if (playerInput == null || playerInput.actions == null)
        {
            if (debugLogs) Debug.LogWarning($"{name}: PlayerInput/actions missing -> boost cannot work.", this);
            return;
        }

        // prefer current map first
        if (playerInput.currentActionMap != null)
            boostAction = playerInput.currentActionMap.FindAction(boostActionName, false);

        if (boostAction == null)
            boostAction = playerInput.actions.FindAction(boostActionName, false);

        if (boostAction == null)
        {
            if (debugLogs)
                Debug.LogWarning($"{name}: Boost action '{boostActionName}' NOT FOUND. Check action name + map.", this);
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

        bool raceAllowsBoost = true;
        if (requireRaceActive && RaceManager.Instance != null)
            raceAllowsBoost = RaceManager.Instance.RaceActive;

        bool held = false;
        if (raceAllowsBoost && boostAction != null)
            held = boostAction.IsPressed();

        if (lockUntilFullAfterEmpty)
        {
            if (!emptyLock && fuel <= 0f) emptyLock = true;
            if (emptyLock && fuel >= maxFuel) emptyLock = false;
        }
        else emptyLock = false;

        bool canBoost = raceAllowsBoost && held && fuel > 0f && !emptyLock;

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

        int useSlot = slot;
        if (useSlot < 0 || useSlot > 1)
            useSlot = (playerInput != null) ? playerInput.playerIndex : 0;

        float norm = (maxFuel <= 0f) ? 0f : fuel / maxFuel;

        var hud = ShipHUD.Get(useSlot);
        if (hud != null)
        {
            hud.SetBoost01(norm);
            hud.SetBoostActive(canBoost);
        }

        SetVisualsAndAudio(canBoost);

        if (rumbleEnabled)
        {
            if (canBoost && !wasBoosting) StartRumble();
            if (!canBoost && wasBoosting) StopRumble();
        }
        wasBoosting = canBoost;

        if (debugLogs && Time.time >= nextDebugTime)
        {
            nextDebugTime = Time.time + Mathf.Max(0.05f, debugEverySeconds);
            Debug.Log($"{name}: held={held} canBoost={canBoost} fuel={fuel:F2}/{maxFuel:F2} emptyLock={emptyLock} slot={useSlot}", this);
        }
    }

    void SetVisualsAndAudio(bool boosting)
    {
        boostLights?.SetBoosting(boosting);
        trailColor?.SetBoosting(boosting);
        boostSfx?.SetBoosting(boosting);
    }

    void StartRumble()
    {
        if (cachedPad == null) CachePad();
        if (cachedPad == null) return;
        cachedPad.SetMotorSpeeds(lowFreq, highFreq);
    }

    void StopRumble()
    {
        if (cachedPad == null) return;
        cachedPad.SetMotorSpeeds(0f, 0f);
    }
}
