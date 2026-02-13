using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class RestartAtRaceEnd : MonoBehaviour
{
    [SerializeField] string restartActionName = "Restart"; // bind to Triangle (buttonNorth)
    [SerializeField] float holdSeconds = 1.0f;

    float holdTimer;

    void Update()
    {
        // Only allow restart once race ended
        if (RaceManager.Instance == null || !RaceManager.Instance.RaceEnded)
        {
            holdTimer = 0f;
            return;
        }

        bool anyHolding = IsAnyPlayerHoldingRestart();

        if (anyHolding)
            holdTimer += Time.unscaledDeltaTime;
        else
            holdTimer = 0f;

        float t01 = (holdSeconds <= 0f) ? 1f : Mathf.Clamp01(holdTimer / holdSeconds);

        // Update popup progress on both monitors
        ShipHUD.Get(0)?.SetRestartProgress01(t01);
        ShipHUD.Get(1)?.SetRestartProgress01(t01);

        if (holdTimer >= holdSeconds)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    bool IsAnyPlayerHoldingRestart()
    {
        // Prefer PlayerInput actions
        foreach (var pi in PlayerInput.all)
        {
            if (pi == null || pi.actions == null) continue;

            var a = pi.actions.FindAction(restartActionName, false);
            if (a == null) continue;
            if (!a.enabled) a.Enable();

            if (a.IsPressed())
                return true;
        }

        return false;
    }
}
