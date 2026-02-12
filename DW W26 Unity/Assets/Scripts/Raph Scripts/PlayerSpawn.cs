using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawn : MonoBehaviour
{
    public static System.Action OnRaceStarted;

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
    [SerializeField] float delayBeforeCountdown = 2f; // <- THIS is the delay you wanted

    [Header("Beep")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip beepClip;

    [Header("Shared Announcer (intro only)")]
    [SerializeField] AudioSource sharedAnnouncerSource; // scene AudioSource
    [SerializeField] AudioClip sharedIntroClip;         // "going around saturn..." etc

    [Header("Race")]
    [SerializeField] RaceManager race;
    [SerializeField] RacePositionUI racePositionUI;

    [Header("Track refs (scene objects)")]
    [SerializeField] Transform saturnCenter;
    [SerializeField] Transform ringPlane;
    [SerializeField] Transform startLineRef;
    [SerializeField] bool clockwise;
    [SerializeField] bool preventBackwardProgress = true;

    [Header("Throttle UI (reads input, NOT speed)")]
    [SerializeField] string throttleFloatAction = "Throttle";
    [SerializeField] string moveVectorAction = "Move";
    [SerializeField] bool invertThrottle = false;

    public int PlayerCount { get; private set; }

    PlayerInput[] joined = new PlayerInput[2];
    bool countdownRunning;
    bool introPlayed;

    void Start()
    {
        var allSpawners = FindObjectsByType<PlayerSpawn>(FindObjectsSortMode.None);
        if (allSpawners.Length > 1)
            Debug.LogWarning($"WARNING: There are {allSpawners.Length} PlayerSpawn objects in the scene. That can cause weird stuff.");

        if (race == null) race = FindFirstObjectByType<RaceManager>();
        if (racePositionUI == null) racePositionUI = FindFirstObjectByType<RacePositionUI>();

        SetCountdownActive(false);
        countdownRunning = false;
        introPlayed = false;

        ValidateSetup();
        UpdateJoinUI();
    }

    void Update()
    {
        // throttle sliders per player (each uses its own PlayerInput + ShipHUD slot)
        for (int slot = 0; slot < 2; slot++)
        {
            var pi = joined[slot];
            if (pi == null) continue;

            var hud = ShipHUD.Get(slot);
            if (hud == null || hud.throttleSlider == null) continue;

            float v = ReadThrottle01(pi);
            if (invertThrottle) v = 1f - v;

            hud.throttleSlider.value = v;
        }
    }

    float ReadThrottle01(PlayerInput pi)
    {
        if (pi == null || pi.actions == null) return 0f;

        var a = pi.actions.FindAction(throttleFloatAction, false);
        if (a != null)
        {
            float raw = a.ReadValue<float>(); // could be 0..1 or -1..1
            return Mathf.InverseLerp(-1f, 1f, raw);
        }

        var mv = pi.actions.FindAction(moveVectorAction, false);
        if (mv != null)
        {
            Vector2 stick = mv.ReadValue<Vector2>();
            return Mathf.InverseLerp(-1f, 1f, stick.y);
        }

        return 0f;
    }

    void ValidateSetup()
    {
        if (SpawnPoints == null || SpawnPoints.Length < 2)
            Debug.LogError($"{name}: SpawnPoints needs size 2 (P1 at [0], P2 at [1]).");

        if (PlayerColors == null || PlayerColors.Length < 2)
            Debug.LogError($"{name}: PlayerColors needs size 2 (P1 at [0], P2 at [1]).");
    }

    public void OnPlayerJoined(PlayerInput playerInput)
    {
        Debug.Log($"[PlayerSpawn] OnPlayerJoined fired: {playerInput.gameObject.name} | playerIndex={playerInput.playerIndex}");

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

        // Force spawn HARD
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

        // Configure progress tracker + wire to UI
        var tracker = playerInput.GetComponent<CircularProgressTracker>();
        if (tracker == null) tracker = playerInput.gameObject.AddComponent<CircularProgressTracker>();

        tracker.Configure(slot, saturnCenter, ringPlane, startLineRef, clockwise, preventBackwardProgress);

        if (racePositionUI != null)
            racePositionUI.AssignTracker(slot, tracker);

        // Wire per-player announcer (spawned at runtime so inspector refs won't magically work)
        var ann = playerInput.GetComponentInChildren<PlayerAnnouncer>(true);
        if (racePositionUI != null && ann != null)
            racePositionUI.AssignAnnouncer(slot, ann);

        // Boost slot (so it writes to the right HUD)
        var boost = playerInput.GetComponent<HoldBoostSystem>();
        if (boost != null) boost.SetSlot(slot);

        UpdateJoinUI();

        if (!countdownRunning && PlayerCount >= requiredPlayers)
            StartCoroutine(CountdownThenGo());
    }

    IEnumerator CountdownThenGo()
    {
        countdownRunning = true;
        SetCountdownActive(true);

        // show "THE RACE STARTS IN" immediately, countdown numbers are below
        UpdateJoinUI();
        SetCountdownText("");

        // play shared intro ONCE when countdown begins
        if (!introPlayed && sharedAnnouncerSource != null && sharedIntroClip != null)
        {
            introPlayed = true;
            sharedAnnouncerSource.PlayOneShot(sharedIntroClip);
        }

        // ✅ actual delay before countdown numbers start
        if (delayBeforeCountdown > 0f)
            yield return new WaitForSeconds(delayBeforeCountdown);

        for (int t = countdownSeconds; t >= 1; t--)
        {
            SetCountdownText(t.ToString());

            if (t <= 3 && audioSource != null && beepClip != null)
                audioSource.PlayOneShot(beepClip);

            yield return new WaitForSeconds(1f);
        }

        SetCountdownText("GO!");

        // enable player scripts
        for (int i = 0; i < 2; i++)
        {
            if (joined[i] == null) continue;

            var flight = joined[i].GetComponent<ShipControllerFlight>();
            if (flight != null) flight.enabled = true;

            var dash = joined[i].GetComponent<ShipDash>();
            if (dash != null) dash.enabled = true;
        }

        if (race != null) race.StartRace();
        OnRaceStarted?.Invoke();

        yield return new WaitForSeconds(0.6f);

        SetCountdownActive(false);
        countdownRunning = false;
        UpdateJoinUI();
    }

    void UpdateJoinUI()
    {
        if (countdownRunning)
        {
            if (joinText1) joinText1.text = "THE RACE STARTS IN";
            if (joinText2) joinText2.text = "THE RACE STARTS IN";
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
            if (joinText1) joinText1.text = "THE RACE STARTS IN";
            if (joinText2) joinText2.text = "THE RACE STARTS IN";
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

    // --- spawn forcing ---

    IEnumerator ForceSpawnRoutine(PlayerInput playerInput, Transform spawn)
    {
        var shipObj = playerInput.gameObject;

        var cols = shipObj.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
            if (cols[i] != null) cols[i].enabled = false;

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

        yield return new WaitForEndOfFrame();
        HardSetPose(shipObj.transform, rb, spawn);

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
