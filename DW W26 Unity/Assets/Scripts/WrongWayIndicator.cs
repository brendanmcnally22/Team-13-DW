using UnityEngine;

public class WrongWayIndicator : MonoBehaviour
{
    [SerializeField] Transform saturnCenter;
    [SerializeField] Transform ringPlane;
    [SerializeField] bool clockwise = true;

    int slot = -1;
    Transform ship;

    public void Configure(int slotIndex, Transform center, Transform plane, bool clockwiseDir)
    {
        slot = slotIndex;
        saturnCenter = center;
        ringPlane = plane;
        clockwise = clockwiseDir;
        ship = transform;
    }

    void Update()
    {
        if (slot < 0 || saturnCenter == null || ringPlane == null) return;

        Vector3 up = ringPlane.up;

        Vector3 toShip = ship.position - saturnCenter.position;
        Vector3 radial = Vector3.ProjectOnPlane(toShip, up).normalized;

        // tangent is perpendicular to radial in the ring plane
        Vector3 tangent = Vector3.Cross(up, radial).normalized;
        if (!clockwise) tangent = -tangent;

        // compare ship forward to tangent
        float dot = Vector3.Dot(ship.forward, tangent);
        bool wrongWay = dot < 0f;

        ShipHUD.Get(slot)?.SetWrongWay(wrongWay);
    }
}
