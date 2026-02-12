using UnityEngine;
using TMPro;

public class WaitingAnimation : MonoBehaviour
{
    public TMP_Text WaitText;
    float timeElapsed = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        WaitText.text = "Waiting for Player 1";
    }

    // Update is called once per frame
    void Update()
    {
        timeElapsed += Time.deltaTime;
        if (timeElapsed <= 0.2f)
        {
            WaitText.text = "Waiting for Player 1";
        }
        if (timeElapsed > 0.2f && timeElapsed <= 0.4f)
        {
            WaitText.text = "Waiting for Player 1.";
        }
        if (timeElapsed > 0.4f && timeElapsed <= 0.6f)
        {
            WaitText.text = "Waiting for Player 1. .";
        }
        if (timeElapsed > 0.6f && timeElapsed <= 0.8f)
        {
            WaitText.text = "Waiting for Player 1. . .";
        }
        if (timeElapsed > 0.8f)
        {
            timeElapsed = 0;
        }
    }
}
