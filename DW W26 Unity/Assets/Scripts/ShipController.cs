using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class ShipControllerFlight : MonoBehaviour
{
    [Header("Aim (DO NOT set this to the camera)")]
    [SerializeField] Transform aimTransform; // ship/AimPivot (auto-created if missing)

    [Header("Speed")]
    [SerializeField] float maxSpeed = 55f;
    [SerializeField] float acceleration = 35f;
    [SerializeField] float brakePower = 4f;

    [Header("Arcade Drift Kill (the fun sliders)")]
    [SerializeField] float lateralDamp = 4.5f;
    [SerializeField] float verticalDamp = 2.0f;

    [Header("Turn Rates")]
    [SerializeField] float yawRate = 1.4f;
    [SerializeField] float pitchRate = 1.1f;
    [SerializeField] float rollRate = 1.6f;

    [Header("Turn Spring (tightness)")]
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
    public float MaxSpeed => maxSpeed;
    public float Throttle01 => throttle;

    [Header("Aim Steers Ship")]
    [SerializeField] float aimSteerStrength = 1.0f;
    [SerializeField] float aimPitchStrength = 1.0f;

    [Header("Rumble (PS5 controller)")]
    [SerializeField] float rumbleSmooth = 12f;

    [Header("Throttle Rumble Burst (NOT continuous)")]
    [SerializeField] float throttleBurstIntensity = 0.85f;
    [SerializeField] float throttleBurstTime = 0.12f;

    [Header("Brake Rumble (continuous)")]
    [SerializeField] float brakeRumbleIntensity = 0.75f;

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
    float brake;

    float yawIn;
    float rollIn;

    float yawSm;
    float rollSm;

    float lookYaw;
    float lookPitch;

    float rumbleCurrent;

    float prevThrottle;
    float throttleBurstTimer;

    float externalBurstTimer;
    float externalBurstIntensity;

    float dashTimer;
    [Header("Dash Support")]
    [SerializeField] float dashLateralDampMultiplier = 0.05f;

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

        // IMPORTANT: do NOT use Gamepad.current (that’s how 2 controllers overlap)
        pad = null;
        foreach (var d in pi.devices)
            if (d is Gamepad g) { pad = g; break; }

        if (pad == null)
            Debug.LogWarning($"{name}: No gamepad paired to this PlayerInput. Rumble off.");
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
        if (throttleBurstTimer > 0f) throttleBurstTimer -= Time.deltaTime;

        // ----- Left stick yaw -----
        yawIn = 0f;
        if (moveAction != null)
            yawIn = moveAction.ReadValue<Vector2>().x;
        if (Mathf.Abs(yawIn) < stickDeadzone) yawIn = 0f;

        // ----- Roll (L1/R1) -----
        rollIn = 0f;
        if (rollLAction != null && rollLAction.IsPressed()) rollIn -= 1f;
        if (rollRAction != null && rollRAction.IsPressed()) rollIn += 1f;

        // ----- Triggers -----
        throttle = (throttleAction != null) ? Mathf.Clamp01(throttleAction.ReadValue<float>()) : 0f;
        brake = (brakeAction != null) ? Mathf.Clamp01(brakeAction.ReadValue<float>()) : 0f;

        if (throttle < triggerDeadzone) throttle = 0f;
        if (brake < triggerDeadzone) brake = 0f;

        // Throttle burst: only when first pressing
        if (throttle > 0.05f && prevThrottle <= 0.05f)
            throttleBurstTimer = throttleBurstTime;

        prevThrottle = throttle;

        // Smooth inputs
        yawSm = Mathf.Lerp(yawSm, yawIn, inputSmooth * Time.deltaTime);
        rollSm = Mathf.Lerp(rollSm, rollIn, inputSmooth * Time.deltaTime);

        UpdateAimFromRightStick();
        UpdateRumble();
    }

    void UpdateAimFromRightStick()
    {
        if (aimTransform == null) return;

        Vector2 look = Vector2.zero;
        if (lookAction != null)
            look = lookAction.ReadValue<Vector2>();

        if (Mathf.Abs(look.x) < stickDeadzone) look.x = 0f;
        if (Mathf.Abs(look.y) < stickDeadzone) look.y = 0f;

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
        Vector3 worldVel = rb.linearVelocity;
#else
        rb.drag = 0.6f + brake * brakePower;
        Vector3 worldVel = rb.velocity;
#endif

        // Thrust forward
        if (throttle > 0.01f)
            rb.AddForce(transform.forward * throttle * acceleration, ForceMode.Acceleration);

        // Drift kill
        Vector3 localVel = transform.InverseTransformDirection(worldVel);

        float lateral = (dashTimer > 0f) ? (lateralDamp * dashLateralDampMultiplier) : lateralDamp;
        localVel.x = Mathf.Lerp(localVel.x, 0f, lateral * Time.fixedDeltaTime);
        localVel.y = Mathf.Lerp(localVel.y, 0f, verticalDamp * Time.fixedDeltaTime);

        worldVel = transform.TransformDirection(localVel);

        // Clamp speed
        if (worldVel.magnitude > maxSpeed)
            worldVel = worldVel.normalized * maxSpeed;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = worldVel;
#else
        rb.velocity = worldVel;
#endif

        // Turning
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

        // throttle = burst ONLY
        float throttleBurst = (throttleBurstTimer > 0f) ? throttleBurstIntensity : 0f;

        // brake = continuous (optional)
        float brakeRumble = brake * brakeRumbleIntensity;

        // external bursts (dash / asteroid)
        float ext = (externalBurstTimer > 0f) ? externalBurstIntensity : 0f;

        float target = Mathf.Clamp01(Mathf.Max(throttleBurst, brakeRumble, ext));
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

    // Dash calls this so lateral damping doesn't delete dash speed
    public void BeginDash(float lockSeconds)
    {
        dashTimer = Mathf.Max(dashTimer, lockSeconds);
    }

    // Dash / asteroids can call this for quick “hit” rumble
    public void AddExternalRumbleBurst(float intensity, float duration)
    {
        externalBurstIntensity = Mathf.Clamp01(Mathf.Max(externalBurstIntensity, intensity));
        externalBurstTimer = Mathf.Max(externalBurstTimer, duration);
    }
    public float Speed01
    {
        get
        {
#if UNITY_6000_0_OR_NEWER
            float s = rb != null ? rb.linearVelocity.magnitude : 0f;
#else
        float s = rb != null ? rb.velocity.magnitude : 0f;
#endif
            return Mathf.Clamp01(s / Mathf.Max(0.01f, maxSpeed));
        }
    }

}
