using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TMP_Text timerUI;

    [Header("Session")]
    [SerializeField] float maxSeconds = 300f; // 5 minutes

    float elapsed;
    bool running;

    void Awake()
    {
        if (!timerUI) timerUI = GetComponent<TMP_Text>();
        ResetTimer();
        UpdateText();
    }

    void OnEnable()
    {
        // this event comes from PlayerSpawn (patch below)
        PlayerSpawn.OnRaceStarted += StartTimer;
    }

    void OnDisable()
    {
        PlayerSpawn.OnRaceStarted -= StartTimer;
    }

    void Update()
    {
        if (!running) return;

        elapsed += Time.deltaTime;

        if (elapsed >= maxSeconds)
        {
            elapsed = maxSeconds;
            running = false;
        }

        UpdateText();
    }

    public void StartTimer()
    {
        elapsed = 0f;
        running = true;
        UpdateText();
    }

    public void StopTimer()
    {
        running = false;
    }

    public void ResetTimer()
    {
        elapsed = 0f;
        running = false;
    }

    void UpdateText()
    {
        if (!timerUI) return;

        int total = Mathf.FloorToInt(elapsed);
        int minutes = total / 60;
        int seconds = total % 60;

        timerUI.text = $"Time {minutes}:{seconds:00}";
    }
}
