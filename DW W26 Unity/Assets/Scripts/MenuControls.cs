using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
public class MenuControls : MonoBehaviour
{
    public float selection;
    bool ControlTutorialUp = false;
    bool buttonDown;
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
    [Space(10)]
    [Header("Controls Screen")]
    public GameObject ControlTutorial;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        selection = 1;
        
    }

    // Update is called once per frame
    void Update()
    {
        if (ControlTutorialUp == false)
        {
            ControlTutorial.SetActive(false);
            float verticalInput = Input.GetAxis("Vertical");
            if (verticalInput == 0)
            {
                buttonDown = false;
            }
            // Go down
            if (verticalInput < 0 && buttonDown == false)
            {
                if (selection <= 3)
                {
                    selection++;
                }
                // Loop back around
                if (selection > 3)
                {
                    selection = 1;
                }
                buttonDown = true;
            }
            // Go up
            if (verticalInput > 0 && buttonDown == false)
            {
                if (selection >= 1)
                {
                    selection--;
                }
                // Loop back around
                if (selection < 1)
                {
                    selection = 3;
                }
                buttonDown = true;
            }
        }
        else
        {
            ControlTutorial.SetActive(true);
        }
        // Start is Selected
        if (selection == 1)
        {
            StartSprite.SetActive(false);
            StartSelected.SetActive(true);
            ControlsSprite.SetActive(true);
            ControlsSelected.SetActive(false);
            QuitSprite.SetActive(true);
            QuitSelected.SetActive(false);
            // Start Race
            if (Input.GetButtonDown("Fire1"))
            {
                SceneManager.LoadScene("Dual Monitor Scene");
            }
        }
        // Controls Selected
        if (selection == 2)
        {
            StartSprite.SetActive(true);
            StartSelected.SetActive(false);
            ControlsSprite.SetActive(false);
            ControlsSelected.SetActive(true);
            QuitSprite.SetActive(true);
            QuitSelected.SetActive(false);
            if (Input.GetButtonDown("Fire1"))
            {
                ControlTutorialUp = !ControlTutorialUp;
            }
        }
        // Quit Selected
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
