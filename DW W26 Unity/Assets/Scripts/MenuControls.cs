using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuControls : MonoBehaviour
{
    public float selection;
    [Space(10)]
    [Header("Start")]
    public GameObject StartSprite;
    public GameObject StartSelected;
    [Space(10)]
    [Header("Controls")]
    public GameObject ControlsSprite;
    public GameObject ControlsSelected;
    [Space(10)]
    [Header("Quit")]
    public GameObject QuitSprite;
    public GameObject QuitSelected;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        selection = 1;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (selection <= 3)
            {
                selection++;
            }
            if (selection > 3)
            {
                selection = 1;
            }
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (selection >= 1)
            {
                selection--;
            }
            if (selection < 1)
            {
                selection = 3;
            }
        }
        if (selection == 1)
        {
            StartSprite.SetActive(false);
            StartSelected.SetActive(true);
            ControlsSprite.SetActive(true);
            ControlsSelected.SetActive(false);
            QuitSprite.SetActive(true);
            QuitSelected.SetActive(false);
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SceneManager.LoadScene("Dual Monitor Scene");
            }
        }
        if (selection == 2)
        {
            StartSprite.SetActive(true);
            StartSelected.SetActive(false);
            ControlsSprite.SetActive(false);
            ControlsSelected.SetActive(true);
            QuitSprite.SetActive(true);
            QuitSelected.SetActive(false);
        }
        if (selection == 3)
        {
            StartSprite.SetActive(true);
            StartSelected.SetActive(false);
            ControlsSprite.SetActive(true);
            ControlsSelected.SetActive(false);
            QuitSprite.SetActive(false);
            QuitSelected.SetActive(true);
        }
    }
}
