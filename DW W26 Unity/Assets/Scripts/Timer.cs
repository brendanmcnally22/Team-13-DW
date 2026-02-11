using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    public TMP_Text TimerUI;
    private float seconds = 0;
    private float minutes = 0;
    private float secondsDisplayed = 0;
    // Update is called once per frame
    void Update()
    {
        seconds += Time.deltaTime;
        // Display only seconds rather than microseconds or milliseconds
        secondsDisplayed = Mathf.Floor(seconds);
        // Reset seconds and increase minutes when seconds is 60 or more
        if (secondsDisplayed >= 60)
        {
            secondsDisplayed = 0;
            seconds = 0;
            minutes += 1;
        }
        // If the seconds does not have a 10s place, then display it with a 0
        if (secondsDisplayed < 10)
        {
            TimerUI.text = $"Time {minutes}:0{secondsDisplayed}";
        }
        // Otherwise, show it as normal
        else
        {
            TimerUI.text = $"Time {minutes}:{secondsDisplayed}";
        }
            
    }
}
