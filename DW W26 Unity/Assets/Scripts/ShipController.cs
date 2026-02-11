using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class ShipControllerFlight : MonoBehaviour
{
    [Header("Aim (DO NOT set this to the camera)")]
    [SerializeField] Transform aimTransform;

    [Header("Speed")]
    [SerializeField] float maxSpeed = 55f;
    [SerializeField] float acceleration = 35f;
    [SerializeField] float brakePower = 4f;

    [Header("Throttle Ramp (prevents instant full throttle)")]
    [SerializeField] float throttleRampUp = 1.8f;
    [SerializeField] float throttleRampDown = 3.5f;

    [Header("Brake Hard Stop")]
    [SerializeField] float hardStopStrength = 10f;

    [Header("Arcade Drift Kill")]
    [SerializeField] float lateralDamp = 4.5f;
    [SerializeField] float verticalDamp = 2.0f;

    [Header("Turn Rates")]
    [SerializeField] float yawRate = 1.4f;
    [SerializeField] float pitchRate = 1.1f;
    [SerializeField] float rollRate = 1.6f;

    [Header("Turn Spring")]
    [SerializeField] float turnSpring = 22f;
    [SerializeField] float turnDamp = 8.5f;
    [SerializeField] float maxAngVel = 5.0f;

    [Header("Input")]
    [SerializeField] float inputSmooth = 10f;
    [SerializeField] float stickDeadzone = 0.12f;
    [SerializeField] float triggerDeadzone = 0.08f;

    [Header("Right Stick Look (aim pivot)")]
    [SerializeField] float lookYawSpeed = 70f;
    [SerializeField] float lookPitchSpeed = 55f;
    [SerializeField] float maxLookYaw = 60f;
    [SerializeField] float minLookPitch = -18f;
    [SerializeField] float maxLookPitch = 28f;
    [SerializeField] float aimReturnSpeed = 4.0f;

    [Header("Aim Steers Ship")]
    [SerializeField] float aimSteerStrength = 1.0f;
    [SerializeField] float aimPitchStrength = 1.0f;

    [Header("Rumble (PS5 controller)")]
    [SerializeField] float maxRumble = 0.6f;
    [SerializeField] float brakeRumble = 0.75f;
    [SerializeField] float rumbleSmooth = 10f;

    [Header("Rumble Burst")]
    [SerializeField] float throttleBurst = 0.9f;
    [SerializeField] float burstDuration = 0.12f;

    [Header("Dash Support")]
    [SerializeField] float dashLateralDampMultiplier = 0.05f; // while dashing, lateral damp is reduced

    Rigidbody rb;
    PlayerInput pi;
    Gamepad pad;

    InputAction moveAction;
    InputAction lookAction;
    InputAction throttleAction;
    InputAction brakeAction;
    InputAction rollLAction;
    InputAction rollRAction;

    float throttle;
    float throttleTarget;
    float brake;

    float yawSm;
    float rollSm;

    float rumbleCurrent;

    float lookYaw;
    float lookPitch;

    float prevThrottleTarget;
    float burstTimer;

    float dashTimer;

    float externalBurstTimer;
    float externalBurstIntensity;

    public float Throttle01 => throttle;
    public float Brake01 => brake;
    public float MaxSpeed => maxSpeed;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pi = GetComponent<PlayerInput>();

        rb.useGravity = false;

#if UNITY_6000_0_OR_NEWER
        rb.linearDamping = 0.6f;
        rb.angularDamping = 0f;
#else
        rb.drag = 0.6f;
        rb.angularDrag = 0f;
#endif

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (aimTransform == null)
        {
            GameObject go = new GameObject("AimPivot");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            aimTransform = go.transform;
        }
    }

    void OnEnable()
    {
        if (pi != null && pi.actions != null)
            pi.actions.Enable();

        moveAction = FindAction("Player/Move", "Move");
        lookAction = FindAction("Player/Look", "Look");
        throttleAction = FindAction("Player/Throttle", "Throttle");
        brakeAction = FindAction("Player/Crouch", "Player/Brake", "Brake");
        rollLAction = FindAction("Player/Previous", "RollLeft", "Previous");
        rollRAction = FindAction("Player/Next", "RollRight", "Next");

        // ONLY paired device (prevents overlap)
        pad = null;
        foreach (var d in pi.devices)
            if (d is Gamepad g) { pad = g; break; }

        if (pad == null)
            Debug.LogWarning($"{name}: No gamepad paired. Rumble disabled.");
    }

    InputAction FindAction(params string[] names)
    {
        if (pi == null || pi.actions == null) return null;
        for (int i = 0; i < names.Length; i++)
        {
            var a = pi.actions.FindAction(names[i], false);
            if (a != null) return a;
        }
        return null;
    }

    void Update()
    {
        if (dashTimer > 0f) dashTimer -= Time.deltaTime;
        if (externalBurstTimer > 0f) externalBurstTimer -= Time.deltaTime;

        // --- Read inputs (actions). If actions are missing, you’ll get 0s. ---
        Vector2 move = (moveAction != null) ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        Vector2 look = (lookAction != null) ? lookAction.ReadValue<Vector2>() : Vector2.zero;

        float rawThrottle = (throttleAction != null) ? throttleAction.ReadValue<float>() : 0f;
        float rawBrake = (brakeAction != null) ? brakeAction.ReadValue<float>() : 0f;

        float rollIn = 0f;
        if (rollLAction != null && rollLAction.IsPressed()) rollIn -= 1f;
        if (rollRAction != null && rollRAction.IsPressed()) rollIn += 1f;

        // deadzones
        if (Mathf.Abs(move.x) < stickDeadzone) move.x = 0f;
        if (Mathf.Abs(look.x) < stickDeadzone) look.x = 0f;
        if (Mathf.Abs(look.y) < stickDeadzone) look.y = 0f;

        rawThrottle = Mathf.Clamp01(rawThrottle);
        rawBrake = Mathf.Clamp01(rawBrake);

        if (rawThrottle < triggerDeadzone) rawThrottle = 0f;
        if (rawBrake < triggerDeadzone) rawBrake = 0f;

        throttleTarget = rawThrottle;
        brake = rawBrake;

        // ramp
        float rate = (throttleTarget > throttle) ? throttleRampUp : throttleRampDown;
        throttle = Mathf.MoveTowards(throttle, throttleTarget, rate * Time.deltaTime);

        // burst
        if (throttleTarget > 0.05f && prevThrottleTarget <= 0.05f)
            burstTimer = burstDuration;
        prevThrottleTarget = throttleTarget;

        // smooth yaw/roll
        yawSm = Mathf.Lerp(yawSm, move.x, inputSmooth * Time.deltaTime);
        rollSm = Mathf.Lerp(rollSm, rollIn, inputSmooth * Time.deltaTime);

        UpdateAimFromRightStick(look);
        UpdateRumble();
    }

    void UpdateAimFromRightStick(Vector2 look)
    {
        if (aimTransform == null) return;

        bool hasLook = look.sqrMagnitude > 0.0001f;

        if (hasLook)
        {
            lookYaw += look.x * lookYawSpeed * Time.deltaTime;
            lookPitch -= look.y * lookPitchSpeed * Time.deltaTime;
        }
        else
        {
            lookYaw = Mathf.Lerp(lookYaw, 0f, aimReturnSpeed * Time.deltaTime);
            lookPitch = Mathf.Lerp(lookPitch, 0f, aimReturnSpeed * Time.deltaTime);
        }

        lookYaw = Mathf.Clamp(lookYaw, -maxLookYaw, maxLookYaw);
        lookPitch = Mathf.Clamp(lookPitch, minLookPitch, maxLookPitch);

        aimTransform.localRotation = Quaternion.Euler(lookPitch, lookYaw, 0f);
    }

    void FixedUpdate()
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearDamping = 0.6f + brake * brakePower;
#else
        rb.drag = 0.6f + brake * brakePower;
#endif

        if (throttle > 0.01f)
            rb.AddForce(transform.forward * throttle * acceleration, ForceMode.Acceleration);

#if UNITY_6000_0_OR_NEWER
        Vector3 worldVel = rb.linearVelocity;
#else
        Vector3 worldVel = rb.velocity;
#endif

        if (brake > 0.01f && hardStopStrength > 0.01f)
            worldVel = Vector3.Lerp(worldVel, Vector3.zero, brake * hardStopStrength * Time.fixedDeltaTime);

        Vector3 localVel = transform.InverseTransformDirection(worldVel);

        // ---- THIS is the dash fix: don’t delete lateral velocity during dash ----
        float lateral = (dashTimer > 0f) ? (lateralDamp * dashLateralDampMultiplier) : lateralDamp;

        localVel.x = Mathf.Lerp(localVel.x, 0f, lateral * Time.fixedDeltaTime);
        localVel.y = Mathf.Lerp(localVel.y, 0f, verticalDamp * Time.fixedDeltaTime);

        worldVel = transform.TransformDirection(localVel);

        if (worldVel.magnitude > maxSpeed)
            worldVel = worldVel.normalized * maxSpeed;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = worldVel;
#else
        rb.velocity = worldVel;
#endif

        // turning
        Vector3 targetLocalAngVel = Vector3.zero;
        targetLocalAngVel.y += yawSm * yawRate;

        if (aimTransform != null)
        {
            Vector3 desiredLocal = transform.InverseTransformDirection(aimTransform.forward);

            float aimYaw = Mathf.Clamp(desiredLocal.x, -1f, 1f) * yawRate * aimSteerStrength;
            float aimPitch = Mathf.Clamp(desiredLocal.y, -1f, 1f) * pitchRate * aimPitchStrength;

            targetLocalAngVel.y += aimYaw;
            targetLocalAngVel.x += -aimPitch;
        }

        targetLocalAngVel.z += rollSm * rollRate;

        Vector3 currentLocalAngVel = transform.InverseTransformDirection(rb.angularVelocity);
        Vector3 error = targetLocalAngVel - currentLocalAngVel;

        Vector3 localTorque = (error * turnSpring) - (currentLocalAngVel * turnDamp);
        Vector3 worldTorque = transform.TransformDirection(localTorque);

        rb.AddTorque(worldTorque, ForceMode.Acceleration);

        if (rb.angularVelocity.magnitude > maxAngVel)
            rb.angularVelocity = rb.angularVelocity.normalized * maxAngVel;
    }

    void UpdateRumble()
    {
        if (pad == null) return;

        float baseRumble = Mathf.Max(throttle * maxRumble, brake * brakeRumble);

        float burst = 0f;
        if (burstTimer > 0f)
        {
            burst = throttleBurst;
            burstTimer -= Time.deltaTime;
        }

        float ext = (externalBurstTimer > 0f) ? externalBurstIntensity : 0f;

        float target = Mathf.Clamp01(Mathf.Max(baseRumble, burst, ext));
        rumbleCurrent = Mathf.Lerp(rumbleCurrent, target, rumbleSmooth * Time.deltaTime);

        if (rumbleCurrent < 0.01f)
        {
            pad.SetMotorSpeeds(0f, 0f);
            return;
        }

        pad.SetMotorSpeeds(rumbleCurrent * 0.7f, rumbleCurrent);
    }

    void OnDisable()
    {
        if (pad != null)
            pad.SetMotorSpeeds(0f, 0f);
    }

    public void RecenterAim()
    {
        lookYaw = 0f;
        lookPitch = 0f;
        if (aimTransform != null)
            aimTransform.localRotation = Quaternion.identity;
    }

    // Called by ShipDash so lateral damping doesn’t murder the dash
    public void BeginDash(float lockSeconds)
    {
        dashTimer = Mathf.Max(dashTimer, lockSeconds);
    }

    // Called by dash / collisions
    public void AddExternalRumbleBurst(float intensity, float duration)
    {
        externalBurstIntensity = Mathf.Clamp01(Mathf.Max(externalBurstIntensity, intensity));
        externalBurstTimer = Mathf.Max(externalBurstTimer, duration);
    }
}
