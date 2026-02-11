using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawn : MonoBehaviour
{
    [field: SerializeField] public Transform[] SpawnPoints { get; private set; }
    [field: SerializeField] public Color[] PlayerColors { get; private set; }

    [Header("Cameras (SimpleFollowCam on each display camera)")]
    [SerializeField] SimpleFollowCam cam1;
    [SerializeField] SimpleFollowCam cam2;

    [Header("UI (per display)")]
    [SerializeField] TMP_Text joinText1;
    [SerializeField] TMP_Text joinText2;
    [SerializeField] TMP_Text countdownText1;
    [SerializeField] TMP_Text countdownText2;

    [Header("Countdown")]
    [SerializeField] int requiredPlayers = 2;
    [SerializeField] int countdownSeconds = 5;

    [Header("Beep")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip beepClip;

    [Header("Race")]
    [SerializeField] RaceManager race;

    public int PlayerCount { get; private set; }

    PlayerInput[] joined = new PlayerInput[2];
    bool countdownRunning;

    void Start()
    {
        if (race == null) race = FindFirstObjectByType<RaceManager>();
        SetCountdownActive(false);
        UpdateJoinUI();
    }

    public void OnPlayerJoined(PlayerInput playerInput)
    {
        if (!HasGamepad(playerInput))
        {
            Debug.Log($"Rejected join from non-gamepad device on {playerInput.gameObject.name}.");
            Destroy(playerInput.gameObject);
            return;
        }

        int maxPlayerCount = Mathf.Min(
            SpawnPoints != null ? SpawnPoints.Length : 0,
            PlayerColors != null ? PlayerColors.Length : 0
        );

        if (maxPlayerCount < 2)
        {
            Debug.LogError($"Need at least 2 SpawnPoints and 2 PlayerColors on {name}.");
            Destroy(playerInput.gameObject);
            return;
        }

        if (PlayerCount >= 2)
        {
            Debug.Log($"Already have 2 players. Destroying {playerInput.gameObject.name}.");
            Destroy(playerInput.gameObject);
            return;
        }

        int slot = PlayerCount; // 0=P1, 1=P2
        int playerNumber = slot + 1;

        // Teleport safely (prevents Saturn shove / only-one-spawnpoint-works weirdness)
        SafeTeleport(playerInput.gameObject, SpawnPoints[slot]);

        Debug.Log($"Spawned P{playerNumber} at {SpawnPoints[slot].position}");

        // Color / setup
        var pc = playerInput.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.AssignPlayerInputDevice(playerInput);
            pc.AssignPlayerNumber(playerNumber);
            pc.AssignColor(PlayerColors[slot]);
        }

        // Freeze movement until countdown ends
        var flight = playerInput.GetComponent<ShipControllerFlight>();
        if (flight != null) flight.enabled = false;

        var dash = playerInput.GetComponent<ShipDash>();
        if (dash != null) dash.enabled = false;

        joined[slot] = playerInput;
        PlayerCount++;

        // Camera target
        if (slot == 0 && cam1 != null) cam1.target = playerInput.transform;
        if (slot == 1 && cam2 != null) cam2.target = playerInput.transform;

        // Register to race manager
        var ship = playerInput.GetComponent<RaceShip>();
        if (ship == null) ship = playerInput.gameObject.AddComponent<RaceShip>();
        if (race != null) race.RegisterShip(ship, slot);

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

        for (int i = 0; i < 2; i++)
        {
            if (joined[i] == null) continue;

            var flight = joined[i].GetComponent<ShipControllerFlight>();
            if (flight != null) flight.enabled = true;

            var dash = joined[i].GetComponent<ShipDash>();
            if (dash != null) dash.enabled = true;
        }

        // start race + music
        if (race != null) race.StartRace();

        yield return new WaitForSeconds(0.6f);
        SetCountdownActive(false);
        UpdateJoinUI();
    }

    void SafeTeleport(GameObject shipObj, Transform spawn)
    {
        if (shipObj == null || spawn == null) return;

        // disable colliders for a frame so physics doesn't shove you inside Saturn
        var cols = shipObj.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;

        shipObj.transform.SetPositionAndRotation(spawn.position, spawn.rotation);

        var rb = shipObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
        }

        StartCoroutine(ReenableCollidersNextFrame(cols));
    }

    IEnumerator ReenableCollidersNextFrame(Collider[] cols)
    {
        yield return null;
        for (int i = 0; i < cols.Length; i++)
            if (cols[i] != null) cols[i].enabled = true;
    }

    bool HasGamepad(PlayerInput input)
    {
        foreach (var d in input.devices)
            if (d is Gamepad) return true;
        return false;
    }

    void UpdateJoinUI()
    {
        if (PlayerCount <= 0)
        {
            if (joinText1) joinText1.text = "P1: Press any button to join";
            if (joinText2) joinText2.text = "Waiting for P1...";
        }
        else if (PlayerCount == 1)
        {
            if (joinText1) joinText1.text = "P1 joined ✅  Waiting for P2...";
            if (joinText2) joinText2.text = "P2: Press any button to join";
        }
        else
        {
            if (joinText1) joinText1.text = "Both players joined!";
            if (joinText2) joinText2.text = "Both players joined!";
        }
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
