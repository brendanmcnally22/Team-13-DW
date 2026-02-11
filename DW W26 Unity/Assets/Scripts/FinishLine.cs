using UnityEngine;

public class RaceFinishTrigger : MonoBehaviour
{
    [SerializeField] RaceManager race;

    void Reset()
    {
        // try auto-find
        if (race == null) race = FindFirstObjectByType<RaceManager>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (race == null) return;

        // ship might have child colliders
        var ship = other.GetComponentInParent<RaceShip>();
        if (ship == null) return;

        race.PlayerCrossedFinish(ship);
    }
}
