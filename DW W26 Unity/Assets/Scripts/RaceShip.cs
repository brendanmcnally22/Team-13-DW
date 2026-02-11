using UnityEngine;

public class RaceShip : MonoBehaviour
{
    public int Slot { get; private set; }          // 0 = P1, 1 = P2
    public bool Finished { get; private set; }
    public bool Won { get; private set; }

    public void Init(int slot)
    {
        Slot = slot;
        Finished = false;
        Won = false;
    }

    public void MarkFinished(bool won)
    {
        Finished = true;
        Won = won;
    }
}
