using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class FPVController : MonoBehaviour
{
    // =========================
    // Input
    // =========================
    [Header("Input Axis Names (Old Input Manager)")]
    public string axisThrottle = "Throttle";
    public string axisRoll = "Roll";
    public string axisPitch = "Pitch";
    public string axisYaw = "Yaw";

    public enum ThrottleInputMode
    {
        ZeroToOne,
        MinusOneToOne,
        CenteredStick
    }

    [Header("Throttle Mapping")]
    public ThrottleInputMode throttleMode = ThrottleInputMode.CenteredStick;
    public bool invertThrottle = false;

    [Header("Axis Fix (swap & invert)")]
    [Tooltip("若 Roll/Pitch 互相控制錯了才勾選此項。")]
    public bool swapRollPitch = false; // 你目前改回 false

    [Tooltip("若向左變向右，勾選此項。")]
    public bool invertRoll = true;

    [Tooltip("若推桿向前變成抬頭(後退)，勾選此項修正。")]
    public bool invertPitch = true;   // 你目前預設開啟

    public bool invertYaw = false;

    [Header("Deadzone / Smoothing")]
    [Range(0f, 0.25f)] public float deadzone = 0.08f;
    [Range(0f, 0.5f)] public float inputSmoothing = 0.06f;

    // =========================
    // ACRO: Rates (Betaflight-like)
    // =========================
    [Header("ACRO Rates - Roll")]
    [Range(0.1f, 3.0f)] public float rollRcRate = 1.2f;
    [Range(0.0f, 0.95f)] public float rollSuperRate = 0.7f;
    [Range(0.0f, 1.0f)] public float rollExpo = 0.35f;

    [Header("ACRO Rates - Pitch")]
    [Range(0.1f, 3.0f)] public float pitchRcRate = 1.2f;
    [Range(0.0f, 0.95f)] public float pitchSuperRate = 0.7f;
    [Range(0.0f, 1.0f)] public float pitchExpo = 0.35f;

    [Header("ACRO Rates - Yaw")]
    [Range(0.1f, 3.0f)] public float yawRcRate = 1.0f;
    [Range(0.0f, 0.95f)] public float yawSuperRate = 0.5f;
    [Range(0.0f, 1.0f)] public float yawExpo = 0.20f;

    // =========================
    // Thrust
    // =========================
    [Header("Throttle / Thrust Feel")]
    [Range(0f, 1f)] public float hoverThrottle = 0.5f;
    public float thrustMultiplier = 1.6f;
    [Range(0f, 1f)] public float throttleExpo = 0.25f;
    [Range(0f, 1f)] public float takeoffMinThrottle = 0.0f;

    // =========================
    // Rate tracking
    // =========================
    [Header("Rate Tracking")]
    public float angVelResponse = 4.0f;
    public float angVelDamping = 0.9f;

    // =========================
    // Ground suppression
    // =========================
    [Header("Ground / Takeoff Suppression")]
    public float groundRayDistance = 0.25f;
    public LayerMask groundMask = ~0;
    [Range(0f, 1f)] public float groundedTorqueScale = 0.15f;
    public float groundedAngVelDampingBoost = 1.2f;
    public float takeoffUnlockHeight = 0.18f;
    public float takeoffUnlockUpVel = 0.6f;

    // =========================
    // Rigidbody override
    // =========================
    [Header("Rigidbody Override")]
    public bool overrideRigidbodySettings = false;
    public float overrideMass = 1.0f;
    public float overrideDrag = 0.0f;
    public float overrideAngularDrag = 0.0f;

    [Header("Physics Safety")]
    public float maxAngularVelocity = 25f;

    [Header("Debug")]
    public bool showDebug = false;

    // =========================
    // Internals
    // =========================
    Rigidbody rb;
    bool airborneLatched = false;
    bool userEverMoved = false;
    Vector4 smoothedInputs = Vector4.zero;
    static HashSet<string> warnedAxes = new HashSet<string>();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.maxAngularVelocity = maxAngularVelocity;

        if (overrideRigidbodySettings)
        {
            rb.mass = overrideMass;
            rb.drag = overrideDrag;
            rb.angularDrag = overrideAngularDrag;
        }
    }

    void FixedUpdate()
    {
        // 1. Read Raw
        float tRaw = SafeGetAxis(axisThrottle);
        float rawRollInput = SafeGetAxis(axisRoll);
        float rawPitchInput = SafeGetAxis(axisPitch);
        float yRaw = SafeGetAxis(axisYaw);

        // 2. Map Throttle
        float throttle01 = MapThrottle01(tRaw);
        if (invertThrottle) throttle01 = 1f - throttle01;
        throttle01 = Mathf.Clamp01(throttle01);

        // 3. Swap Logic (若需要交換軸向)
        float rollInputForCalc;
        float pitchInputForCalc;

        if (swapRollPitch)
        {
            rollInputForCalc = rawPitchInput;
            pitchInputForCalc = rawRollInput;
        }
        else
        {
            rollInputForCalc = rawRollInput;
            pitchInputForCalc = rawPitchInput;
        }

        // 4. Deadzone
        float rollStick = ApplyDeadzone(rollInputForCalc, deadzone);
        float pitchStick = ApplyDeadzone(pitchInputForCalc, deadzone);
        float yawStick = ApplyDeadzone(yRaw, deadzone);

        // 5. Invert Logic (修正方向)
        if (invertRoll) rollStick *= -1f;
        if (invertPitch) pitchStick *= -1f;
        if (invertYaw) yawStick *= -1f;

        // 6. Smoothing
        if (inputSmoothing > 0.0001f)
        {
            float a = 1f - Mathf.Exp(-Time.fixedDeltaTime / Mathf.Max(0.0001f, inputSmoothing));
            smoothedInputs.x = Mathf.Lerp(smoothedInputs.x, rollStick, a);
            smoothedInputs.y = Mathf.Lerp(smoothedInputs.y, pitchStick, a);
            smoothedInputs.z = Mathf.Lerp(smoothedInputs.z, yawStick, a);
            smoothedInputs.w = Mathf.Lerp(smoothedInputs.w, throttle01, a);

            rollStick = smoothedInputs.x;
            pitchStick = smoothedInputs.y;
            yawStick = smoothedInputs.z;
            throttle01 = smoothedInputs.w;
        }

        // 7. Latch
        if (!userEverMoved)
        {
            if (Mathf.Abs(rollStick) > 0.001f || Mathf.Abs(pitchStick) > 0.001f || Mathf.Abs(yawStick) > 0.001f || throttle01 > deadzone)
                userEverMoved = true;
        }

        if (userEverMoved && takeoffMinThrottle > 0f)
            throttle01 = Mathf.Max(throttle01, takeoffMinThrottle);

        // 8. Ground Check
        bool grounded = IsGrounded(out float groundDist);
        if (!airborneLatched)
        {
            float upVel = Vector3.Dot(rb.velocity, transform.up);
            if (groundDist > takeoffUnlockHeight || upVel > takeoffUnlockUpVel)
                airborneLatched = true;
        }

        // 9. Physics
        ApplyThrust(throttle01);

        // ACRO only
        Vector3 targetAngVelLocal = BuildAcroTargetAngVelLocal(rollStick, pitchStick, yawStick);

        ApplyRateTrackingTorque(targetAngVelLocal, grounded);

        if (showDebug && Time.frameCount % 10 == 0)
        {
            Debug.Log($"[FPV] Thr:{throttle01:F2} R:{rollStick:F2} P:{pitchStick:F2} Y:{yawStick:F2}");
        }
    }

    Vector3 BuildAcroTargetAngVelLocal(float rollStick, float pitchStick, float yawStick)
    {
        float rollRateDeg = ApplyBetaflightRates(rollStick, rollRcRate, rollSuperRate, rollExpo);
        float pitchRateDeg = ApplyBetaflightRates(pitchStick, pitchRcRate, pitchSuperRate, pitchExpo);
        float yawRateDeg = ApplyBetaflightRates(yawStick, yawRcRate, yawSuperRate, yawExpo);

        // local x=pitch, y=yaw, z=roll
        return new Vector3(pitchRateDeg * Mathf.Deg2Rad, yawRateDeg * Mathf.Deg2Rad, rollRateDeg * Mathf.Deg2Rad);
    }

    void ApplyRateTrackingTorque(Vector3 targetAngVelLocal, bool grounded)
    {
        Vector3 angVelLocal = transform.InverseTransformDirection(rb.angularVelocity);
        Vector3 error = targetAngVelLocal - angVelLocal;

        bool suppressGround = grounded && !airborneLatched;
        float damp = angVelDamping + (suppressGround ? groundedAngVelDampingBoost : 0f);

        Vector3 torqueLocal = error * angVelResponse - angVelLocal * damp;
        if (suppressGround) torqueLocal *= groundedTorqueScale;

        rb.AddTorque(transform.TransformDirection(torqueLocal), ForceMode.Acceleration);
    }

    void ApplyThrust(float throttle01)
    {
        float hoverForce = rb.mass * Mathf.Abs(Physics.gravity.y);
        float tShaped = ApplyThrottleExpo(throttle01, throttleExpo);
        float centered = (tShaped - hoverThrottle);
        float thrust = hoverForce * (1f + centered * thrustMultiplier);

        thrust = Mathf.Max(0f, thrust);
        rb.AddForce(transform.up * thrust, ForceMode.Force);
    }

    bool IsGrounded(out float groundDist)
    {
        if (Physics.Raycast(transform.position + transform.up * 0.05f, -transform.up, out RaycastHit hit, 2.0f, groundMask, QueryTriggerInteraction.Ignore))
        {
            groundDist = hit.distance;
            return hit.distance <= groundRayDistance;
        }
        groundDist = 999f;
        return false;
    }

    float MapThrottle01(float tRaw)
    {
        if (throttleMode == ThrottleInputMode.MinusOneToOne) return Mathf.InverseLerp(-1f, 1f, tRaw);
        if (throttleMode == ThrottleInputMode.CenteredStick) return Mathf.Clamp01(tRaw);
        return tRaw;
    }

    float ApplyThrottleExpo(float t01, float expo)
    {
        float x = Mathf.Clamp01(t01);
        float centered = x - 0.5f;
        return Mathf.Clamp01((1f - expo) * centered + expo * centered * centered * centered * 4f + 0.5f);
    }

    float SafeGetAxis(string axisName)
    {
        if (string.IsNullOrWhiteSpace(axisName)) return 0f;
        try { return Input.GetAxis(axisName); }
        catch (System.ArgumentException)
        {
            if (!warnedAxes.Contains(axisName))
            {
                warnedAxes.Add(axisName);
                Debug.LogWarning($"Axis '{axisName}' not set.");
            }
            return 0f;
        }
    }

    float ApplyDeadzone(float x, float dz)
    {
        if (Mathf.Abs(x) < dz) return 0f;
        return Mathf.Sign(x) * ((Mathf.Abs(x) - dz) / (1f - dz));
    }

    float ApplyBetaflightRates(float stick, float rcRate, float superRate, float expo)
    {
        float xExpo = stick * stick * stick * expo + stick * (1f - expo);
        float rate = 200f * rcRate * xExpo;
        if (superRate > 0f) rate *= (1f / Mathf.Max(0.01f, 1f - Mathf.Abs(xExpo) * superRate));
        return rate;
    }
}
