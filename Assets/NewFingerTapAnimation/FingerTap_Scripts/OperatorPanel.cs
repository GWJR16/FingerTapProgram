using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class OperatorPanel : MonoBehaviour
{
    [Header("Core refs")]
    public SequenceManager     seq;      // drag GameManager (with SequenceManager)
    public VROverlayController overlay;  // drag VR_OverlayCanvas (with VROverlayController)

    [Header("Operator Buttons")]
    public Button showInstrBtn;
    public Button playF1Btn;
    public Button playF2Btn;
    public Button playBlock1Btn;
    public Button playBlock2Btn;
    public Button playBlock3Btn;
    public Button playBlock4Btn;
    public Button pauseBtn;
    public Button stopBtn;

    [Header("Readout UI")]
    public Slider   phaseSlider;   // visual only
    public Slider   blockSlider;   // visual only
    public TMP_Text phaseLabel;
    public TMP_Text trialLabel;
    public TMP_Text presetLabel;
    public TMP_Text timeLabel;
    public TMP_Text pulseLabel;

    [Header("Calibration (optional)")]
    public Toggle   calibrationModeToggle; // if you use calibration locking

    void Awake()
    {
        // ---- SHOW INSTRUCTIONS ----
        if (showInstrBtn) showInstrBtn.onClick.AddListener(() =>
        {
            if (!overlay)
            {
                Debug.LogWarning("OperatorPanel: overlay is not assigned.");
                return;
            }

            // Ensure overlay canvas is visible and clean
            overlay.gameObject.SetActive(true);
            overlay.ResetOverlay();

            // Turn on black background
            overlay.ShowBlackScreen(true);

            // OPTION A: use whatever text is currently in the canvas TMP
            string text = (overlay.instructionsText != null) ? overlay.instructionsText.text : "";
            overlay.ShowInstructions(text);

            Debug.Log("[OperatorPanel] Instructions shown from canvas TMP.");
        });

        // ---- PLAY PRESETS ----
        if (playF1Btn) playF1Btn.onClick.AddListener(() =>
        {
            if (!seq || !overlay) return;
            overlay.HideInstructions();           // ensure instructions disappear
            seq.PlayPreset(SequenceManager.Preset.F1);
        });

        if (playF2Btn) playF2Btn.onClick.AddListener(() =>
        {
            if (!seq || !overlay) return;
            overlay.HideInstructions();
            seq.PlayPreset(SequenceManager.Preset.F2);
        });

        if (playBlock1Btn) playBlock1Btn.onClick.AddListener(() =>
        {
            if (!seq || !overlay) return;
            overlay.HideInstructions();
            seq.PlayBlock(new List<SequenceManager.Preset>{
                SequenceManager.Preset.A, SequenceManager.Preset.B1, SequenceManager.Preset.C1,
                SequenceManager.Preset.B2, SequenceManager.Preset.B2, SequenceManager.Preset.C1,
                SequenceManager.Preset.B1, SequenceManager.Preset.C2, SequenceManager.Preset.C2,
                SequenceManager.Preset.C1, SequenceManager.Preset.B2
            });
        });

        if (playBlock2Btn) playBlock2Btn.onClick.AddListener(() =>
        {
            if (!seq || !overlay) return;
            overlay.HideInstructions();
            seq.PlayBlock(new List<SequenceManager.Preset>{
                SequenceManager.Preset.A, SequenceManager.Preset.B1, SequenceManager.Preset.B1,
                SequenceManager.Preset.B2, SequenceManager.Preset.C1, SequenceManager.Preset.C1,
                SequenceManager.Preset.B2, SequenceManager.Preset.B2, SequenceManager.Preset.C1,
                SequenceManager.Preset.C2, SequenceManager.Preset.C1
            });
        });

        if (playBlock3Btn) playBlock3Btn.onClick.AddListener(() =>
        {
            if (!seq || !overlay) return;
            overlay.HideInstructions();
            seq.PlayBlock(new List<SequenceManager.Preset>{
                SequenceManager.Preset.A, SequenceManager.Preset.C1, SequenceManager.Preset.B2,
                SequenceManager.Preset.C1, SequenceManager.Preset.B2, SequenceManager.Preset.C2,
                SequenceManager.Preset.C1, SequenceManager.Preset.B1, SequenceManager.Preset.B1,
                SequenceManager.Preset.B2, SequenceManager.Preset.C2
            });
        });

        if (playBlock4Btn) playBlock4Btn.onClick.AddListener(() =>
        {
            if (!seq || !overlay) return;
            overlay.HideInstructions();
            seq.PlayBlock(new List<SequenceManager.Preset>{
                SequenceManager.Preset.A, SequenceManager.Preset.C2, SequenceManager.Preset.C1,
                SequenceManager.Preset.B1, SequenceManager.Preset.B1, SequenceManager.Preset.B2,
                SequenceManager.Preset.C2, SequenceManager.Preset.C1, SequenceManager.Preset.B1,
                SequenceManager.Preset.C2, SequenceManager.Preset.B2
            });
        });

        // ---- PAUSE / RESUME ----
        if (pauseBtn) pauseBtn.onClick.AddListener(() =>
        {
            if (!seq) return;
            seq.TogglePause();
            var t = pauseBtn.GetComponentInChildren<TMP_Text>();
            if (t) t.text = seq.IsPaused ? "Resume" : "Pause";
        });

        // ---- STOP ----
        if (stopBtn) stopBtn.onClick.AddListener(() =>
        {
            if (!seq || !overlay) return;
            seq.StopCurrent();
            overlay.HideInstructions();      // hide instruction text
            overlay.ShowBlackScreen(false);  // turn off black; remove this line if you want to stay on black
            var t = pauseBtn ? pauseBtn.GetComponentInChildren<TMP_Text>() : null;
            if (t) t.text = "Pause";
        });
    }

    void Update()
    {
        if (!seq) return;

        // Progress sliders (visual only)
        if (phaseSlider)
            phaseSlider.value = (seq.PhaseTotal > 0f) ? Mathf.Clamp01(seq.PhaseElapsed / seq.PhaseTotal) : 0f;
        if (blockSlider)
            blockSlider.value = (seq.BlockTotal > 0f) ? Mathf.Clamp01(seq.BlockElapsed / seq.BlockTotal) : 0f;

        // Labels
        if (phaseLabel)  phaseLabel.text  = $"Phase: {seq.CurrentPhase}";
        if (trialLabel)  trialLabel.text  = (seq.TotalTrialsInBlock > 0)
            ? $"Trial {seq.CurrentTrialIndex} / {seq.TotalTrialsInBlock}"
            : "No Block";
        if (presetLabel) presetLabel.text = $"Preset: {(string.IsNullOrEmpty(seq.CurrentPresetName) ? "-" : seq.CurrentPresetName)}";

        if (timeLabel)
        {
            float pRemain = Mathf.Max(0f, seq.PhaseTotal - seq.PhaseElapsed);
            float bRemain = Mathf.Max(0f, seq.BlockTotal - seq.BlockElapsed);
            timeLabel.text = $"Phase {seq.PhaseElapsed:0.0}s / {seq.PhaseTotal:0.0}s  •  " +
                             $"Block {seq.BlockElapsed:0.0}s / {seq.BlockTotal:0.0}s  " +
                             $"(Remain {pRemain:0.0}s / {bRemain:0.0}s)";
        }

        // Pulse countdown (full time, then 3/2/1/Pulse)
        if (pulseLabel)
        {
            if (seq.HasUpcomingPulse)
            {
                if (seq.PulseTimeRemaining > 3f)
                {
                    pulseLabel.text = $"Pulse in: {seq.PulseTimeRemaining:0.0}s";
                }
                else
                {
                    int whole = Mathf.CeilToInt(seq.PulseTimeRemaining);
                    if (whole >= 3)      pulseLabel.text = "Pulse in: 3";
                    else if (whole == 2) pulseLabel.text = "Pulse in: 2";
                    else if (whole == 1) pulseLabel.text = "Pulse in: 1";
                    else                 pulseLabel.text = "Pulse!";
                }
            }
            else pulseLabel.text = "Pulse: -";
        }

        // Lock play buttons while running or in calibration
        bool lockButtons = seq.IsRunning || (calibrationModeToggle && calibrationModeToggle.isOn);
        SetInteract(playF1Btn,      !lockButtons);
        SetInteract(playF2Btn,      !lockButtons);
        SetInteract(playBlock1Btn,  !lockButtons);
        SetInteract(playBlock2Btn,  !lockButtons);
        SetInteract(playBlock3Btn,  !lockButtons);
        SetInteract(playBlock4Btn,  !lockButtons);
        // Pause/Stop/ShowInstr remain usable
    }

    void SetInteract(Button b, bool on) { if (b) b.interactable = on; }
}
