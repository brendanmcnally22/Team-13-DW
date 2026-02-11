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

    [Header("Brake Hard Stop (extra)")]
    [SerializeField] float hardStopStrength = 10f; // set 0 to disable

    [Header("Arcade Drift Kill (the fun sliders)")]
    [SerializeField] float lateralDamp = 4.5f;   // higher = less side-slip
    [SerializeField] float verticalDamp = 2.0f;  // higher = less up/down drift

    [Header("Turn Rates")]
    [SerializeField] float yawRate = 1.4f;       // left stick X
    [SerializeField] float pitchRate = 1.1f;     // from aimTransform
    [SerializeField] float rollRate = 1.6f;      // L1/R1

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
    [SerializeField] float aimReturnSpeed = 4.0f; // returns aim to center when you stop touching stick

    [Header("Aim Steers Ship")]
    [SerializeField] float aimSteerStrength = 1.0f; // how much right stick helps yaw
    [SerializeField] float aimPitchStrength = 1.0f; // how much right stick helps pitch

    [Header("Rumble (PS5 controller)")]
    [SerializeField] float maxRumble = 0.6f;
    [SerializeField] float brakeRumble = 0.75f;       // NEW
    [SerializeField] float rumbleSmooth = 10f;

    [Header("Rumble Burst")]
    [SerializeField] float throttleBurst = 0.9f;      // NEW
    [SerializeField] float burstDuration = 0.12f;     // NEW

    Rigidbody rb;
    PlayerInput pi;
    Gamepad pad;

    InputAction moveAction;       // left stick
    InputAction lookAction;       // right stick
    InputAction throttleAction;   // R2
    InputAction brakeAction;      // L2
    InputAction rollLAction;      // L1
    InputAction rollRAction;      // R1

    float throttle;
    float brake;

    float yawIn;
    float rollIn;

    float yawSm;
    float rollSm;

    float rumbleCurrent;

    float lookYaw;
    float lookPitch;

    float prevThrottle;
    float burstTimer;

    // Exposed for HUD
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

        // Make an AimPivot if you didn't assign one (this is the safe setup)
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
        brakeAction = FindAction("Player/Crouch", "Player/Brake", "Brake");   // crouch = brake
        rollLAction = FindAction("Player/Previous", "RollLeft", "Previous");
        rollRAction = FindAction("Player/Next", "RollRight", "Next");

        // grab the gamepad paired to THIS PlayerInput
        pad = null;
        foreach (var d in pi.devices)
            if (d is Gamepad g) { pad = g; break; }

        if (pad == null)
            Debug.LogWarning($"{name}: No gamepad paired to this PlayerInput. Rumble disabled.");
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

        // throttle press -> short burst
        if (throttle > 0.05f && prevThrottle <= 0.05f)
            burstTimer = burstDuration;

        prevThrottle = throttle;

        // Smooth inputs (keeps it buttery)
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
        // Brake = more damping (your original behavior)
#if UNITY_6000_0_OR_NEWER
        rb.linearDamping = 0.6f + brake * brakePower;
#else
        rb.drag = 0.6f + brake * brakePower;
#endif

        // Thrust forward
        if (throttle > 0.01f)
            rb.AddForce(transform.forward * throttle * acceleration, ForceMode.Acceleration);

        // Get velocity
#if UNITY_6000_0_OR_NEWER
        Vector3 worldVel = rb.linearVelocity;
#else
        Vector3 worldVel = rb.velocity;
#endif

        // Hard brake stop (extra)
        if (brake > 0.01f && hardStopStrength > 0.01f)
            worldVel = Vector3.Lerp(worldVel, Vector3.zero, brake * hardStopStrength * Time.fixedDeltaTime);

        // Kill sideways/vertical drift so it feels responsive
        Vector3 localVel = transform.InverseTransformDirection(worldVel);

        localVel.x = Mathf.Lerp(localVel.x, 0f, lateralDamp * Time.fixedDeltaTime);
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

        // ----- Turning -----
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

        // base rumble from throttle OR brake
        float baseRumble = Mathf.Max(throttle * maxRumble, brake * brakeRumble);

        // burst rumble when throttle starts
        float burst = 0f;
        if (burstTimer > 0f)
        {
            burst = throttleBurst;
            burstTimer -= Time.deltaTime;
        }

        float target = Mathf.Clamp01(Mathf.Max(baseRumble, burst));
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
}
