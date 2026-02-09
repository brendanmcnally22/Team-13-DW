using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class ShipController : MonoBehaviour
{
    [SerializeField] float maxSpeed = 40f;
    [SerializeField] float accel = 50f;
    [SerializeField] float brakePower = 35f;

    [SerializeField] float yawTorque = 18f;
    [SerializeField] float yawButtonTorque = 24f;

    [SerializeField] float linearDamping = 1.4f;
    [SerializeField] float angularDamping = 4f;
    [SerializeField] float sideSlipDamping = 9f;

    [SerializeField] float boostForce = 100f;
    [SerializeField] float boostDuration = 0.25f;
    [SerializeField] float boostCooldown = 1f;

    [SerializeField] Transform visual;
    [SerializeField] float visualRoll = 30f;

    Rigidbody rb;
    PlayerInput pi;

    InputAction moveAction;       // left stick
    InputAction lookAction;       // right stick (camera script uses this, not the ship)
    InputAction throttleAction;   // R2
    InputAction brakeAction;      // L2
    InputAction boostAction;      // X
    InputAction yawLeftAction;    // L1
    InputAction yawRightAction;   // R1

    float throttle;
    float brake;
    float steer;

    bool boostQueued;
    float boostTimer;
    float boostCd;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pi = GetComponent<PlayerInput>();

        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void OnEnable()
    {
        if (pi != null && pi.actions != null)
            pi.actions.Enable();

        moveAction = pi.actions.FindAction("Player/Move");
        lookAction = pi.actions.FindAction("Player/Look");

        throttleAction = pi.actions.FindAction("Player/Throttle");
        brakeAction = pi.actions.FindAction("Player/Brake");

        boostAction = pi.actions.FindAction("Player/Boost");

        yawLeftAction = pi.actions.FindAction("Player/YawLeft");
        yawRightAction = pi.actions.FindAction("Player/YawRight");

        Debug.Log($"{name} input enabled. move={(moveAction != null)} look={(lookAction != null)} throttle={(throttleAction != null)} brake={(brakeAction != null)} boost={(boostAction != null)}");
    }

    void Update()
    {
        // left stick steering only
        if (moveAction != null)
            steer = moveAction.ReadValue<Vector2>().x;

        // triggers are analog 0..1
        if (throttleAction != null)
            throttle = Mathf.Clamp01(throttleAction.ReadValue<float>());

        if (brakeAction != null)
            brake = Mathf.Clamp01(brakeAction.ReadValue<float>());

        // boost on X
        if (boostAction != null && boostAction.WasPressedThisFrame())
            boostQueued = true;
    }

    void FixedUpdate()
    {
        if (moveAction == null || throttleAction == null || brakeAction == null)
            return;

        // accelerate forward (R2)
        if (throttle > 0.01f)
            rb.AddForce(transform.forward * (throttle * accel), ForceMode.Acceleration);

        // brake / reverse (L2)
        if (brake > 0.01f)
            rb.AddForce(-transform.forward * (brake * brakePower), ForceMode.Acceleration);

        // yaw from left stick steer + L1/R1 buttons
        float totalYaw = steer * yawTorque;

        if (yawLeftAction != null && yawLeftAction.IsPressed())
            totalYaw -= yawButtonTorque;

        if (yawRightAction != null && yawRightAction.IsPressed())
            totalYaw += yawButtonTorque;

        rb.AddTorque(Vector3.up * totalYaw, ForceMode.Acceleration);

        // keep it grippy (kills sideways drift)
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        localVel.x = Mathf.Lerp(localVel.x, 0f, sideSlipDamping * Time.fixedDeltaTime);
        rb.linearVelocity = transform.TransformDirection(localVel);

        // fake damping so it feels like a racer not a float sim
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, linearDamping * Time.fixedDeltaTime);
        rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, angularDamping * Time.fixedDeltaTime);

        // speed cap
        if (rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;

        HandleBoost();

        // tilt mesh for style only
        if (visual != null)
        {
            float targetRoll = -steer * visualRoll;
            visual.localRotation = Quaternion.Slerp(
                visual.localRotation,
                Quaternion.Euler(0f, 0f, targetRoll),
                12f * Time.fixedDeltaTime
            );
        }
    }

    void HandleBoost()
    {
        if (boostCd > 0f)
            boostCd -= Time.fixedDeltaTime;

        if (boostTimer > 0f)
        {
            boostTimer -= Time.fixedDeltaTime;
            rb.AddForce(transform.forward * boostForce, ForceMode.Acceleration);
            return;
        }

        if (boostQueued)
        {
            boostQueued = false;

            if (boostCd <= 0f)
            {
                boostTimer = boostDuration;
                boostCd = boostCooldown;
            }
        }
    }
}
