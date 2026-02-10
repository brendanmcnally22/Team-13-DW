using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    public TMP_Text TimerUI;
    private float timeTotal;
    private float timeDisplayed;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        timeTotal = 0;
        timeDisplayed = 0;
    }

    // Update is called once per frame
    void Update()
    {
        timeTotal += Time.deltaTime;
        timeDisplayed = Mathf.RoundToInt(timeTotal);
        TimerUI.text = $"Time: {timeDisplayed}";
    }
}
