using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance { get; private set; }

    public bool RaceStarted => raceStarted;
    public bool RaceActive => raceStarted && !raceEnded;
    public bool RaceEnded => raceEnded;

    public event Action OnRaceEnded;

    [Header("UI (per display)")]
    [SerializeField] TMP_Text resultText1;
    [SerializeField] TMP_Text resultText2;

    [Header("Finish Rules")]
    [SerializeField] float endDelayAfterFirstFinish = 10f;

    [Header("Restart Popup (HUD)")]
    [SerializeField] bool showRestartPopupAtEnd = true;
    [SerializeField] string restartPopupMessage = "HOLD △ TO RESTART";

    [Header("Music")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioClip raceMusic;
    [SerializeField] bool loopMusic = true;

    RaceShip[] ships = new RaceShip[2];

    bool raceStarted;
    bool raceEnded;
    bool firstFinishHappened;
    int firstFinisherSlot = -1;
    Coroutine endRoutine;

    void Awake()
    {
        // safer singleton (prevents weird double state)
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate RaceManager found and destroyed.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterShip(RaceShip ship, int slot)
    {
        if (slot < 0 || slot > 1) return;
        ships[slot] = ship;
        ship.Init(slot);
    }

    public void StartRace()
    {
        if (raceStarted) return;

        // reset state (important if you ever restart / reload)
        raceStarted = true;
        raceEnded = false;
        firstFinishHappened = false;
        firstFinisherSlot = -1;

        SetResult(0, "");
        SetResult(1, "");

        // hide restart popup when race starts
        if (showRestartPopupAtEnd)
        {
            ShipHUD.Get(0)?.ShowRestartPopup(false);
            ShipHUD.Get(1)?.ShowRestartPopup(false);
        }

        if (musicSource != null && raceMusic != null)
        {
            musicSource.clip = raceMusic;
            musicSource.loop = loopMusic;
            musicSource.Play();
        }
    }

    public void PlayerCrossedFinish(RaceShip ship)
    {
        if (!raceStarted || raceEnded) return;
        if (ship == null) return;
        if (ship.Finished) return;

        int slot = ship.Slot;

        // first finisher decides winner
        if (!firstFinishHappened)
        {
            firstFinishHappened = true;
            firstFinisherSlot = slot;

            ship.MarkFinished(true);
            SetResult(slot, "YOU WON!");

            // other player is not finished yet
            int other = 1 - slot;
            if (ships[other] != null && !ships[other].Finished)
                SetResult(other, $"HURRY UP! ({Mathf.CeilToInt(endDelayAfterFirstFinish)}s)");

            if (endRoutine != null) StopCoroutine(endRoutine);
            endRoutine = StartCoroutine(EndAfterDelay());
        }
        else
        {
            // second finisher
            ship.MarkFinished(false);
            SetResult(slot, "YOU LOST!");
        }
    }

    IEnumerator EndAfterDelay()
    {
        float t = endDelayAfterFirstFinish;

        while (t > 0f)
        {
            // optional: show countdown on loser screen
            int loserSlot = 1 - firstFinisherSlot;
            if (ships[loserSlot] != null && !ships[loserSlot].Finished)
                SetResult(loserSlot, $"FINISH! ({Mathf.CeilToInt(t)}s)");

            t -= Time.deltaTime;
            yield return null;
        }

        // if loser didn’t finish in time, mark them lost
        int other = 1 - firstFinisherSlot;
        if (ships[other] != null && !ships[other].Finished)
        {
            ships[other].MarkFinished(false);
            SetResult(other, "YOU LOST!");
        }

        FreezeAllShips();
        StopMusic();

        raceEnded = true;

        // show restart popup on both displays
        if (showRestartPopupAtEnd)
        {
            ShipHUD.Get(0)?.ShowRestartPopup(true, restartPopupMessage);
            ShipHUD.Get(1)?.ShowRestartPopup(true, restartPopupMessage);
        }

        OnRaceEnded?.Invoke();
    }

    void FreezeAllShips()
    {
        for (int i = 0; i < ships.Length; i++)
        {
            if (ships[i] == null) continue;

            var flight = ships[i].GetComponent<ShipControllerFlight>();
            if (flight != null) flight.enabled = false;

            var dash = ships[i].GetComponent<ShipDash>();
            if (dash != null) dash.enabled = false;

            var boost = ships[i].GetComponent<HoldBoostSystem>();
            if (boost != null) boost.enabled = false;

            // ✅ safer: RB is often on a child
            var rb = ships[i].GetComponentInChildren<Rigidbody>(true);
            if (rb != null)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
#else
                rb.velocity = Vector3.zero;
#endif
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }

    void SetResult(int slot, string msg)
    {
        if (slot == 0)
        {
            if (resultText1) resultText1.text = msg;
        }
        else if (slot == 1)
        {
            if (resultText2) resultText2.text = msg;
        }
    }
}
