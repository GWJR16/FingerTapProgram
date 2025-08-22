using UnityEngine;

public class OverlayTest : MonoBehaviour
{
    public VROverlayController overlay;

    void Start()
    {
        // Show black background
        overlay.ShowBlackScreen(true);

        // Show countdown text "Get Ready"
        overlay.ShowCountdown("Get Ready");

        // Show red dot after 3 seconds
        Invoke(nameof(ShowDot), 3f);
    }

    void ShowDot()
    {
        overlay.ShowRedDot(true);
    }
}
