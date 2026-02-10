using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class RaceManager : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] GameObject playerPrefab;        // your ShipPrefab (must have PlayerInput)
    [SerializeField] Transform[] spawnPoints;        // size 2
    [SerializeField] Color[] playerColors;           // size 2

    [Header("UI")]
    [SerializeField] TMP_Text countdownText;         // on Display1 canvas or a shared canvas
    [SerializeField] TMP_Text statusText;            // optional

    [Header("Audio")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip beepClip;

    [Header("Start")]
    [SerializeField] float preRaceCountdown = 5f;    // total seconds (you asked >=5)

    PlayerInput[] players = new PlayerInput[2];

    public void PressPlay()
    {
        // basic safety checks
        if (playerPrefab == null || spawnPoints.Length < 2 || playerColors.Length < 2)
        {
            Debug.LogError("RaceManager: assign playerPrefab, 2 spawnPoints, 2 playerColors.");
            return;
        }

        // find two controllers
        var pads = Gamepad.all;
        if (pads.Count < 2)
        {
            if (statusText) statusText.text = "Need 2 controllers connected.";
            Debug.LogWarning("RaceManager: Need 2 gamepads connected.");
            return;
        }

        // if you press play twice, clean up old ships
        CleanupOldPlayers();

        // spawn both players “at the same time”
        for (int i = 0; i < 2; i++)
        {
            players[i] = PlayerInput.Instantiate(
                playerPrefab,
                playerIndex: i,
                controlScheme: null,
                splitScreenIndex: -1,
                pairWithDevice: pads[i]
            );

            // move to spawn
            players[i].transform.SetPositionAndRotation(spawnPoints[i].position, spawnPoints[i].rotation);

            // assign your custom stuff (same logic as your PlayerSpawn)
            var pc = players[i].GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.AssignPlayerInputDevice(players[i]);
                pc.AssignPlayerNumber(i + 1);
                pc.AssignColor(playerColors[i]);
            }

            // freeze movement until countdown finishes (does NOT modify your controller code)
            var flight = players[i].GetComponent<ShipControllerFlight>();
            if (flight != null) flight.enabled = false;
        }

        StartCoroutine(StartCountdownThenGo());
    }

    IEnumerator StartCountdownThenGo()
    {
        if (countdownText) countdownText.gameObject.SetActive(true);

        // 5..4..3..2..1 then GO
        int seconds = Mathf.CeilToInt(preRaceCountdown);

        for (int t = seconds; t >= 1; t--)
        {
            if (countdownText) countdownText.text = t.ToString();

            // last 3 seconds = beep beep beep
            if (t <= 3 && audioSource && beepClip)
                audioSource.PlayOneShot(beepClip);

            yield return new WaitForSeconds(1f);
        }

        if (countdownText) countdownText.text = "GO!";

        // enable movement
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null) continue;
            var flight = players[i].GetComponent<ShipControllerFlight>();
            if (flight != null) flight.enabled = true;
        }

        yield return new WaitForSeconds(0.6f);
        if (countdownText) countdownText.gameObject.SetActive(false);
    }

    void CleanupOldPlayers()
    {
        // if you had old spawned ships
        var existing = FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);
        foreach (var p in existing)
        {
            // only destroy ones that are using your prefab logic
            if (p != null && p.gameObject.name.Contains(playerPrefab.name))
                Destroy(p.gameObject);
        }
    }
}
