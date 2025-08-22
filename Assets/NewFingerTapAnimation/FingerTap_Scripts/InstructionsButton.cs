using UnityEngine;

public class InstructionsButton : MonoBehaviour
{
    public VROverlayController overlay;
    public SequenceManager     seq;          // optional: used for Stop
    [TextArea(3,10)]
    public string instructionsFallback;      // leave empty to use seq.instructionsText

    // Call this from your Instructions button OnClick
    public void ShowInstructions()
    {
        if (!overlay) return;

        overlay.ResetOverlay();
        overlay.ShowBlackScreen(true);

        // Prefer SequenceManager.instructionsText if set; else use local fallback
        string text = (seq != null && !string.IsNullOrEmpty(seq.instructionsText))
                        ? seq.instructionsText
                        : instructionsFallback;

        overlay.ShowInstructions(text);
    }

    // Call this from your Stop button OnClick (replaces/augments your current Stop)
    public void StopAndHide()
    {
        if (seq != null) seq.StopCurrent();     // optional: stop the sequence
        if (!overlay) return;

        overlay.HideInstructions();
        overlay.ShowBlackScreen(false);         // or leave true if you want to stay on black
    }

    // If you just need to hide without stopping, wire this instead
    public void HideOnly()
    {
        if (!overlay) return;
        overlay.HideInstructions();
        overlay.ShowBlackScreen(false);
    }
}
