using UnityEngine;

public class CircularProgressTracker : MonoBehaviour
{
    public int Slot { get; private set; }
    public float TotalTurns { get; private set; }   // 0..1 = one loop
    public float CurrentAngle { get; private set; } // debug

    Transform center;
    Transform ringPlane;
    Transform startLine;

    bool clockwise;
    bool preventBackwardProgress;

    float lastAngle;
    bool initialized;

    public void Configure(int slot, Transform center, Transform ringPlane, Transform startLine, bool clockwise, bool preventBackward)
    {
        Slot = slot;
        this.center = center;
        this.ringPlane = ringPlane;
        this.startLine = startLine;
        this.clockwise = clockwise;
        this.preventBackwardProgress = preventBackward;

        TotalTurns = 0f;
        CurrentAngle = 0f;
        initialized = false;
    }

    void Update()
    {
        if (!center || !ringPlane || !startLine) return;

        float angle = ComputeAngle0To360();

        if (!initialized)
        {
            initialized = true;
            lastAngle = angle;
            CurrentAngle = angle;
            return;
        }

        float delta = angle - lastAngle;

        // unwrap across 0/360 boundary
        if (delta < -180f) delta += 360f;
        else if (delta > 180f) delta -= 360f;

        if (preventBackwardProgress && delta < 0f)
            delta = 0f;

        TotalTurns += delta / 360f;

        lastAngle = angle;
        CurrentAngle = angle;
    }

    float ComputeAngle0To360()
    {
        Vector3 normal = ringPlane.up;

        Vector3 startRadial = Vector3.ProjectOnPlane(startLine.position - center.position, normal).normalized;
        Vector3 shipRadial = Vector3.ProjectOnPlane(transform.position - center.position, normal).normalized;

        float ang = Vector3.SignedAngle(startRadial, shipRadial, normal); // -180..180
        if (ang < 0f) ang += 360f;

        if (clockwise)
            ang = (360f - ang) % 360f;

        return ang;
    }
}
