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
        // if you accidentally have 2 spawners in scene, you're gonna have a bad time
        var allSpawners = FindObjectsByType<PlayerSpawn>(FindObjectsSortMode.None);
        if (allSpawners.Length > 1)
            Debug.LogWarning($"WARNING: There are {allSpawners.Length} PlayerSpawn objects in the scene. That can cause double-spawning / overrides.");

        if (race == null) race = FindFirstObjectByType<RaceManager>();

        SetCountdownActive(false);
        countdownRunning = false;

        ValidateSetup();
        UpdateJoinUI();
    }

    void ValidateSetup()
    {
        if (SpawnPoints == null || SpawnPoints.Length < 2)
            Debug.LogError($"{name}: SpawnPoints needs size 2 (P1 at [0], P2 at [1]).");

        if (PlayerColors == null || PlayerColors.Length < 2)
            Debug.LogError($"{name}: PlayerColors needs size 2 (P1 at [0], P2 at [1]).");

        if (SpawnPoints != null && SpawnPoints.Length >= 2)
        {
            if (SpawnPoints[0] == null || SpawnPoints[1] == null)
                Debug.LogError($"{name}: One of your SpawnPoints entries is NULL.");

            if (SpawnPoints[0] != null && SpawnPoints[1] != null && SpawnPoints[0] == SpawnPoints[1])
                Debug.LogWarning($"{name}: SpawnPoints[0] and SpawnPoints[1] are the SAME Transform. Both players will stack.");
        }
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

        int slot = GetNextFreeSlot();
        if (slot < 0)
        {
            Debug.Log($"Already have 2 players. Destroying {playerInput.gameObject.name}.");
            Destroy(playerInput.gameObject);
            return;
        }

        int playerNumber = slot + 1;

        // freeze movement until countdown ends (do it before force-spawn)
        var flight = playerInput.GetComponent<ShipControllerFlight>();
        if (flight != null) flight.enabled = false;

        var dash = playerInput.GetComponent<ShipDash>();
        if (dash != null) dash.enabled = false;

        // Force spawn HARD (beats scripts that reset position in Start/first frame)
        if (SpawnPoints[slot] != null)
            StartCoroutine(ForceSpawnRoutine(playerInput, SpawnPoints[slot]));
        else
            Debug.LogError($"SpawnPoints[{slot}] is NULL. Fix inspector.");

        // Color / setup
        var pc = playerInput.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.AssignPlayerInputDevice(playerInput);
            pc.AssignPlayerNumber(playerNumber);
            pc.AssignColor(PlayerColors[slot]);
        }

        joined[slot] = playerInput;
        PlayerCount = CountJoined();

        // Camera targets
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

        // instantly switch label to "THE RACE WILL START IN"
        UpdateJoinUI();

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

        if (race != null) race.StartRace();

        yield return new WaitForSeconds(0.6f);

        SetCountdownActive(false);
        countdownRunning = false;
        UpdateJoinUI();
    }

    // --- UI ---

    void UpdateJoinUI()
    {
        // during countdown: label becomes the "race will start" text, numbers are separate TMP below
        if (countdownRunning)
        {
            if (joinText1) joinText1.text = "THE RACE WILL START IN";
            if (joinText2) joinText2.text = "THE RACE WILL START IN";
            return;
        }

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
            // both joined, countdown will start immediately (or already did)
            if (joinText1) joinText1.text = "THE RACE WILL START IN";
            if (joinText2) joinText2.text = "THE RACE WILL START IN";
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

    // --- spawn forcing (the thing that finally made it behave lol) ---

    IEnumerator ForceSpawnRoutine(PlayerInput playerInput, Transform spawn)
    {
        var shipObj = playerInput.gameObject;

        // disable colliders so nothing shoves you immediately
        var cols = shipObj.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
            if (cols[i] != null) cols[i].enabled = false;

        // rb might be on a child
        var rb = shipObj.GetComponentInChildren<Rigidbody>(true);

        bool hadRb = rb != null;
        bool oldKinematic = false;
        bool oldDetect = true;

        if (hadRb)
        {
            oldKinematic = rb.isKinematic;
            oldDetect = rb.detectCollisions;

            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        HardSetPose(shipObj.transform, rb, spawn);

        // beat scripts that run in Start / first frame
        yield return new WaitForEndOfFrame();
        HardSetPose(shipObj.transform, rb, spawn);

        // beat physics step
        yield return new WaitForFixedUpdate();
        HardSetPose(shipObj.transform, rb, spawn);

        if (hadRb)
        {
            rb.detectCollisions = oldDetect;
            rb.isKinematic = oldKinematic;
            rb.WakeUp();
        }

        yield return null;
        for (int i = 0; i < cols.Length; i++)
            if (cols[i] != null) cols[i].enabled = true;

        Debug.Log($"Final spawn locked: {shipObj.name} @ {shipObj.transform.position}");
    }

    void HardSetPose(Transform shipRoot, Rigidbody rb, Transform spawn)
    {
        shipRoot.SetPositionAndRotation(spawn.position, spawn.rotation);

        if (rb != null)
        {
            rb.position = spawn.position;
            rb.rotation = spawn.rotation;

#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }

        Physics.SyncTransforms();
    }

    // --- helpers ---

    int GetNextFreeSlot()
    {
        for (int i = 0; i < joined.Length; i++)
            if (joined[i] == null)
                return i;
        return -1;
    }

    int CountJoined()
    {
        int count = 0;
        for (int i = 0; i < joined.Length; i++)
            if (joined[i] != null) count++;
        return count;
    }

    bool HasGamepad(PlayerInput input)
    {
        foreach (var d in input.devices)
            if (d is Gamepad) return true;
        return false;
    }
}
