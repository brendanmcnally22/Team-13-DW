using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    public TMP_Text TimerUI;
    private float timeTotal;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        timeTotal = 0;
    }

    // Update is called once per frame
    void Update()
    {
        timeTotal += Time.deltaTime;
        TimerUI.text = $"Time: {timeTotal}";
    }
}
