using TMPro;
using UnityEngine;

public class RacePositionUI : MonoBehaviour
{
    [Header("Ships (auto-assigned by PlayerSpawn)")]
    public CircularProgressTracker p1;
    public CircularProgressTracker p2;

    [Header("UI (top corner)")]
    public TMP_Text p1PlaceText;
    public TMP_Text p2PlaceText;

    [Header("Optional announcers")]
    public PlayerAnnouncer p1Announcer;
    public PlayerAnnouncer p2Announcer;
    public AudioClip firstClip;
    public AudioClip secondClip;

    int lastLeader = 0; // 0 unknown, 1 p1, 2 p2

    public void AssignTracker(int slot, CircularProgressTracker tracker)
    {
        if (slot == 0) p1 = tracker;
        else if (slot == 1) p2 = tracker;
    }

    void Update()
    {
        if (!p1 || !p2) return;

        int leader = (p1.TotalTurns >= p2.TotalTurns) ? 1 : 2;

        if (leader == 1)
        {
            if (p1PlaceText) p1PlaceText.text = "1ST";
            if (p2PlaceText) p2PlaceText.text = "2ND";
        }
        else
        {
            if (p1PlaceText) p1PlaceText.text = "2ND";
            if (p2PlaceText) p2PlaceText.text = "1ST";
        }

        if (leader != lastLeader)
        {
            lastLeader = leader;

            if (p1Announcer && firstClip && secondClip)
                p1Announcer.Play(leader == 1 ? firstClip : secondClip);

            if (p2Announcer && firstClip && secondClip)
                p2Announcer.Play(leader == 2 ? firstClip : secondClip);
        }
    }
}
