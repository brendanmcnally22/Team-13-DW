using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawn : MonoBehaviour
{
    [field: SerializeField] public Transform[] SpawnPoints { get; private set; }
    [field: SerializeField] public Color[] PlayerColors { get; private set; }

    [Header("Cameras (SimpleFollowCam on each display camera)")]
    [SerializeField] SimpleFollowCam cam1; // Display 1 camera follow script
    [SerializeField] SimpleFollowCam cam2; // Display 2 camera follow script

    [Header("UI (per display)")]
    [SerializeField] TMP_Text joinText1;
    [SerializeField] TMP_Text joinText2;
    [SerializeField] TMP_Text countdownText1;
    [SerializeField] TMP_Text countdownText2;

    [Header("HUD (per display)")]
    [SerializeField] ShipHUD hud1;
    [SerializeField] ShipHUD hud2;

    [Header("Countdown")]
    [SerializeField] int requiredPlayers = 2;
    [SerializeField] int countdownSeconds = 5;

    [Header("Beep")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip beepClip;

    public int PlayerCount { get; private set; }

    PlayerInput[] joined = new PlayerInput[4];
    bool countdownRunning;

    void Start()
    {
        SetCountdownActive(false);
        UpdateJoinUI();
    }

    public void OnPlayerJoined(PlayerInput playerInput)
    {
        int maxPlayerCount = Mathf.Min(
            SpawnPoints != null ? SpawnPoints.Length : 0,
            PlayerColors != null ? PlayerColors.Length : 0
        );

        if (maxPlayerCount < 1)
        {
            Debug.LogError($"Assign SpawnPoints + PlayerColors on {name}.");
            Destroy(playerInput.gameObject);
            return;
        }

        if (PlayerCount >= maxPlayerCount)
        {
            Debug.Log($"Max players reached ({maxPlayerCount}). Destroying {playerInput.gameObject.name}.");
            Destroy(playerInput.gameObject);
            return;
        }

        int slot = PlayerCount;      // 0 = P1, 1 = P2
        int playerNumber = slot + 1;

        // Spawn position
        playerInput.transform.SetPositionAndRotation(
            SpawnPoints[slot].position,
            SpawnPoints[slot].rotation
        );

        // Your player setup
        var pc = playerInput.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.AssignPlayerInputDevice(playerInput);
            pc.AssignPlayerNumber(playerNumber);
            pc.AssignColor(PlayerColors[slot]);
        }

        // Freeze until countdown finishes
        var flight = playerInput.GetComponent<ShipControllerFlight>();
        if (flight != null) flight.enabled = false;

        joined[slot] = playerInput;
        PlayerCount++;

        // Camera targets
        if (slot == 0 && cam1 != null) cam1.target = playerInput.transform;
        if (slot == 1 && cam2 != null) cam2.target = playerInput.transform;

        // HUD binds
        if (slot == 0 && hud1 != null) hud1.Bind(playerInput.transform);
        if (slot == 1 && hud2 != null) hud2.Bind(playerInput.transform);

        UpdateJoinUI();

        if (!countdownRunning && PlayerCount >= requiredPlayers)
            StartCoroutine(CountdownThenGo());
    }

    IEnumerator CountdownThenGo()
    {
        countdownRunning = true;
        SetCountdownActive(true);

        for (int t = countdownSeconds; t >= 1; t--)
        {
            SetCountdownText(t.ToString());

            if (t <= 3 && audioSource != null && beepClip != null)
                audioSource.PlayOneShot(beepClip);

            yield return new WaitForSeconds(1f);
        }

        SetCountdownText("GO!");

        for (int i = 0; i < requiredPlayers; i++)
        {
            if (joined[i] == null) continue;
            var flight = joined[i].GetComponent<ShipControllerFlight>();
            if (flight != null) flight.enabled = true;
        }

        yield return new WaitForSeconds(0.6f);
        SetCountdownActive(false);
        UpdateJoinUI();
    }

    public void OnPlayerLeft(PlayerInput playerInput)
    {
        Debug.Log("Player left...");

        for (int i = 0; i < joined.Length; i++)
            if (joined[i] == playerInput) joined[i] = null;

        countdownRunning = false;
        PlayerCount = Mathf.Max(0, PlayerCount - 1);

        SetCountdownActive(false);
        UpdateJoinUI();
    }

    void UpdateJoinUI()
    {
        if (PlayerCount <= 0) SetJoinText("Player 1: Press X (any button) to join");
        else if (PlayerCount == 1) SetJoinText("Player 2: Press X (any button) to join");
        else SetJoinText("Both players joined! Get ready...");
    }

    void SetJoinText(string msg)
    {
        if (joinText1) joinText1.text = msg;
        if (joinText2) joinText2.text = msg;
    }

    void SetCountdownText(string msg)
    {
        if (countdownText1) countdownText1.text = msg;
        if (countdownText2) countdownText2.text = msg;
    }

    void SetCountdownActive(bool on)
    {
        if (countdownText1) countdownText1.gameObject.SetActive(on);
        if (countdownText2) countdownText2.gameObject.SetActive(on);
    }
}
