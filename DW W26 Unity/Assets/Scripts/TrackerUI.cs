using TMPro;
using UnityEngine;

public class RacePositionUI : MonoBehaviour
{
    [Header("Ships")]
    public CircularProgressTracker p1;
    public CircularProgressTracker p2;

    [Header("UI (top corner)")]
    public TMP_Text p1PlaceText;
    public TMP_Text p2PlaceText;

    [Header("Announcers (per-player, assigned at runtime)")]
    public PlayerAnnouncer p1Announcer;
    public PlayerAnnouncer p2Announcer;

    [Header("Lead change callouts (optional)")]
    public AudioClip firstClip;
    public AudioClip secondClip;

    [Header("Anti-flicker")]
    [SerializeField] float leadDeadzoneTurns = 0.01f; // ~3.6 degrees around the ring

    int lastLeader = 0; // 0 unknown, 1 p1, 2 p2

    public void AssignTracker(int slot, CircularProgressTracker tracker)
    {
        if (slot == 0) p1 = tracker;
        if (slot == 1) p2 = tracker;
    }

    public void AssignAnnouncer(int slot, PlayerAnnouncer ann)
    {
        if (slot == 0) p1Announcer = ann;
        if (slot == 1) p2Announcer = ann;
    }

    void Update()
    {
        if (!p1 || !p2) return;

        // Placement is based on TotalTurns:
        // TotalTurns goes up as your angle around Saturn increases (full loop = +1.0).
        float diff = p1.TotalTurns - p2.TotalTurns;

        int leader;
        if (Mathf.Abs(diff) < leadDeadzoneTurns && lastLeader != 0)
        {
            // basically tied -> keep last leader so UI doesn't flicker
            leader = lastLeader;
        }
        else
        {
            leader = (diff >= 0f) ? 1 : 2;
        }

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

        // play callout only when the lead changes
        if (leader != lastLeader)
        {
            lastLeader = leader;

            if (firstClip && secondClip)
            {
                if (p1Announcer) p1Announcer.Play(leader == 1 ? firstClip : secondClip);
                if (p2Announcer) p2Announcer.Play(leader == 2 ? firstClip : secondClip);
            }
        }
    }
}
