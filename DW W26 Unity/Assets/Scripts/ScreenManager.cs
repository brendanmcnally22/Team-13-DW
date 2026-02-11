using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScreenManager : MonoBehaviour
{
    public void StartButton()
    {
        SceneManager.LoadScene("Dual Monitor Scene");
    }

}

