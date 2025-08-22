using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class VROverlayController : MonoBehaviour
{
    [Header("Overlay Elements")]
    public GameObject blackBackground;   // Fullscreen Image under VR_OverlayCanvas
    public TMP_Text   countdownText;     // “Get Ready”, “3”, “2”, “1”
    public GameObject redDot;            // Red dot Image
    public TMP_Text   instructionsText;  // << NEW: a TMP text child of BlackBackground

    // ───────── Basic controls ─────────

    public void ResetOverlay()
    {
        if (countdownText)   countdownText.gameObject.SetActive(false);
        if (redDot)          redDot.SetActive(false);
        if (instructionsText) instructionsText.gameObject.SetActive(false);
        // Do NOT force black off; caller decides ShowBlackScreen(true/false)
    }

    public void ShowBlackScreen(bool on)
    {
        if (blackBackground) blackBackground.SetActive(on);
    }

    public void ShowCountdown(string msg)
    {
        if (!countdownText) return;
        countdownText.text = msg;
        countdownText.gameObject.SetActive(true);
    }

    public void HideCountdown()
    {
        if (countdownText) countdownText.gameObject.SetActive(false);
    }

    public void ShowRedDot(bool on)
    {
        if (redDot) redDot.SetActive(on);
    }

    // ───────── Instructions (text on black) ─────────

    /// <summary>Show arbitrary instruction text on the black background.</summary>
    public void ShowInstructions(string text)
    {
        if (!instructionsText) return;

        // Make sure the black backdrop is on
        if (blackBackground) blackBackground.SetActive(true);

        // Hide other overlays while reading
        if (countdownText) countdownText.gameObject.SetActive(false);
        if (redDot)        redDot.SetActive(false);

        instructionsText.text = text ?? "";
        instructionsText.gameObject.SetActive(true);
    }

    /// <summary>Hide instruction text.</summary>
    public void HideInstructions()
    {
        if (instructionsText) instructionsText.gameObject.SetActive(false);
    }
}
