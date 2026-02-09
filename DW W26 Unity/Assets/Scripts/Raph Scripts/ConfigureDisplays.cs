using TMPro;
using UnityEngine;

public class ConfigureDisplays : MonoBehaviour
{
    [field: SerializeField] public TMP_Text DisplayText1 { get; private set; }
    [field: SerializeField] public TMP_Text DisplayText2 { get; private set; }

    public static bool HasRun { get; private set; }

    void Start()
    {
        InitializeDisplays();
    }

    private void InitializeDisplays()
    {
        // Main resource: https://docs.unity3d.com/6000.0/Documentation/Manual/MultiDisplay.html

        // Prevent multiple attempts to run this code.
        if (HasRun)
            return;

        {
            // Indicate number of connected displays.
            string msg = $"Connected display count: {Display.displays.Length}";
            Debug.Log(msg);
            // Show display 1 information.
            SetDisplayInfo(0, DisplayText1);
        }

        // Run setup for display 2 if able.
        if (Display.displays.Length > 1)
        {
            // Activate display 2 (zero-indexed).
            Display.displays[1].Activate();
            // Show display 2 information.
            SetDisplayInfo(1, DisplayText2);
        }
        // Soft error if no second monitor detected when run as build (not in Unity Editor).
        else if (!Application.isEditor)
        {
            string msg = $"Could not detect at least one secondary display.";
            Debug.LogError(msg);
            DisplayText1.text = msg;
            return;
        }

        // Prevent code from running again.
        HasRun = true;
    }

    private void SetDisplayInfo(int displayIndex, TMP_Text displayText)
    {
        Display display = Display.displays[displayIndex];
        string msg =
            $"Display {displayIndex + 1}\n" +
            $"Rendering({display.renderingWidth}, {display.renderingHeight})\n" +
            $"System({display.systemWidth}, {display.systemHeight})";
        displayText.text = msg;
    }
}
