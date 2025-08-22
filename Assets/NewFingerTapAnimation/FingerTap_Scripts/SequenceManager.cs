using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SequenceManager : MonoBehaviour
{
    public enum Preset { F1, F2, A, B1, B2, C1, C2 }
    public enum Phase { Idle, Instructions, Countdown, Animation, Black, RedDot }

    [Header("Scene References")]
    public VROverlayController overlay;    // VR_OverlayCanvas controller
    public Animator handAnimator;          // Animator on HandTap_Animations
    public GameObject handRoot;            // HandTap_Animations GameObject
    public HandAudioController handAudio;  // script on HandTap_Animations

    [Header("Animator State Names (must match your Animator)")]
    public string state90 = "Tap_90";
    public string state112 = "Tap_112";
    public string state135 = "Tap_135";

    [Header("Timings (seconds)")]
    public float tGetReady = 2f;
    public float tCount3 = 1f;
    public float tCount2 = 1f;
    public float tCount1 = 1f;
    public float tAnim = 6f;
    public float tBlack = 6f;
    public float tRedDot = 1.5f;

    [Header("Instructions (optional)")]
    [TextArea(3, 10)]
    public string instructionsText =
@"1) Keep your right hand relaxed on the desk.
2) Follow the finger movement shown.
3) Keep the arm still.
4) The red dot marks the end of a trial.
5) Wait for the next countdown.";
    public string instructionsTitle = "Instructions";

    // ───── Global 2↔3 alternation across audio-enabled presets ─────
    private bool useThirdNext = false;
    private int NextPeak() { useThirdNext = !useThirdNext; return useThirdNext ? 3 : 2; }

    // ───── Run/timeline state (used by the Operator UI later) ─────
    Coroutine _running;
    public bool IsRunning => _running != null;
    public bool IsPaused { get; private set; }
    public Phase CurrentPhase { get; set; } = Phase.Idle; // <── changed to public setter

    public int CurrentTrialIndex { get; private set; } = 0;
    public int TotalTrialsInBlock { get; private set; } = 0;
    public string CurrentPresetName { get; private set; } = "";

    public float PhaseElapsed { get; private set; } = 0f;
    public float PhaseTotal { get; private set; } = 0f;
    public float BlockElapsed { get; private set; } = 0f;
    public float BlockTotal { get; private set; } = 0f;

    // Optional: pulse countdown exposure (used by Operator UI later)
    public bool HasUpcomingPulse { get; private set; } = false;
    public float PulseTimeRemaining { get; private set; } = 0f;

    // OUT-peak times (sec) per clip for the audible peaks (based on your 60 fps plans)
    Dictionary<string, float[]> peakTimes;

    void Awake()
    {
        peakTimes = new Dictionary<string, float[]>
        {
            // Example timings—tweak if your event frames differ in practice
            { state90,  new float[]{ 0.667f, 2.000f, 3.333f, 4.667f, 6.000f } },
            { state112, new float[]{ 0.533f, 1.600f, 2.683f, 3.750f, 4.817f, 5.900f } },
            { state135, new float[]{ 0.450f, 1.333f, 2.217f, 3.117f, 4.000f, 4.883f, 5.783f } }
        };
    }

    // ─────────── Public controls ───────────
    public void PlayPreset(Preset p)
    {
        if (_running != null) return;
        _running = StartCoroutine(RunBlock(new List<Preset> { p }));
    }

    public void PlayBlock(List<Preset> order)
    {
        if (_running != null) return;
        _running = StartCoroutine(RunBlock(order));
    }

    public void StopCurrent()
    {
        if (_running != null) { StopCoroutine(_running); _running = null; }
        IsPaused = false;
        Time.timeScale = 1f;

        if (overlay) { overlay.gameObject.SetActive(true); overlay.ResetOverlay(); overlay.ShowBlackScreen(false); }
        if (handRoot) handRoot.SetActive(false);

        CurrentPhase = Phase.Idle;
        PhaseElapsed = PhaseTotal = 0f;
        BlockElapsed = BlockTotal = 0f;
        CurrentTrialIndex = 0; TotalTrialsInBlock = 0;
        CurrentPresetName = "";
        HasUpcomingPulse = false; PulseTimeRemaining = 0f;
    }

    public void TogglePause()
    {
        if (!IsRunning) return;
        IsPaused = !IsPaused;
        Time.timeScale = IsPaused ? 0f : 1f;
    }

    // ─────────── Coroutines ───────────
    IEnumerator RunBlock(List<Preset> order)
    {
        TotalTrialsInBlock = order.Count;
        BlockElapsed = 0f; BlockTotal = 0f;
        for (int i = 0; i < order.Count; i++) BlockTotal += PresetDuration(order[i]);

        for (int i = 0; i < order.Count; i++)
        {
            CurrentTrialIndex = i + 1;
            yield return RunSinglePreset(order[i]);
        }

        _running = null;
        CurrentPhase = Phase.Idle;
        PhaseElapsed = PhaseTotal = 0f;
        CurrentPresetName = "";
        HasUpcomingPulse = false; PulseTimeRemaining = 0f;
    }

    IEnumerator RunSinglePreset(Preset p)
    {
        // Ensure overlay visible for countdown (important for F1/F2)
        if (overlay) overlay.gameObject.SetActive(true);

        // Clean state
        overlay.ResetOverlay();
        if (handRoot) handRoot.SetActive(false);
        CurrentPresetName = p.ToString();

        // COUNTDOWN on black
        overlay.ShowBlackScreen(true);
        overlay.ShowRedDot(false);
        CurrentPhase = Phase.Countdown;
        float tCountdown = tGetReady + tCount3 + tCount2 + tCount1;
        yield return PhaseCountdown(tCountdown);

        // Decide behavior
        bool playsAnim = false;
        string state = null;
        int targetPeak = 0;

        switch (p)
        {
            case Preset.F1:
                playsAnim = false; break;

            case Preset.F2:
            case Preset.A:
                playsAnim = true; state = state112; targetPeak = 0; break;

            case Preset.B1:
            case Preset.B2:
                playsAnim = true; state = state135; targetPeak = NextPeak(); break;

            case Preset.C1:
            case Preset.C2:
                playsAnim = true; state = state90; targetPeak = NextPeak(); break;
        }

        // ANIMATION (6 s)
        if (playsAnim)
        {
            if (handAudio) handAudio.targetPeak = targetPeak;

            overlay.ShowBlackScreen(false);
            overlay.ShowRedDot(false);
            overlay.HideCountdown();
            overlay.gameObject.SetActive(false);
            if (handRoot) handRoot.SetActive(true);

            HasUpcomingPulse = (targetPeak > 0) && peakTimes.ContainsKey(state) && (targetPeak <= peakTimes[state].Length);
            PulseTimeRemaining = HasUpcomingPulse ? peakTimes[state][targetPeak - 1] : 0f;

            CurrentPhase = Phase.Animation;
            handAnimator.CrossFadeInFixedTime(state, 0f, 0, 0f);
            yield return PhaseTimer(tAnim);

            HasUpcomingPulse = false;
            if (handRoot) handRoot.SetActive(false);
            overlay.gameObject.SetActive(true);
        }

        // BLACK (6 s)
        overlay.ShowBlackScreen(true);
        overlay.ShowRedDot(false);
        CurrentPhase = Phase.Black;
        yield return PhaseTimer(tBlack);

        // RED DOT (1.5 s)
        overlay.ShowRedDot(true);
        CurrentPhase = Phase.RedDot;
        yield return PhaseTimer(tRedDot);
        overlay.ShowRedDot(false);

        overlay.HideCountdown();
        overlay.ShowBlackScreen(false);

        CurrentPhase = Phase.Idle;
        PhaseElapsed = PhaseTotal = 0f;
        HasUpcomingPulse = false; PulseTimeRemaining = 0f;
    }

    float PresetDuration(Preset p)
    {
        float tCountdown = tGetReady + tCount3 + tCount2 + tCount1;
        bool playsAnim = (p == Preset.F2 || p == Preset.A || p == Preset.B1 || p == Preset.B2 || p == Preset.C1 || p == Preset.C2);
        return tCountdown + (playsAnim ? tAnim : 0f) + tBlack + tRedDot;
    }

    IEnumerator PhaseCountdown(float total)
    {
        PhaseElapsed = 0f; PhaseTotal = total;

        overlay.ShowCountdown("Get Ready");
        yield return PhaseTimer(tGetReady);

        overlay.ShowCountdown("3");
        yield return PhaseTimer(tCount3);

        overlay.ShowCountdown("2");
        yield return PhaseTimer(tCount2);

        overlay.ShowCountdown("1");
        yield return PhaseTimer(tCount1);

        overlay.HideCountdown();
    }

    IEnumerator PhaseTimer(float seconds)
    {
        PhaseElapsed = 0f; PhaseTotal = seconds;

        while (PhaseElapsed < PhaseTotal)
        {
            if (!IsPaused)
            {
                float dt = Time.deltaTime;
                PhaseElapsed += dt;
                BlockElapsed += dt;

                if (CurrentPhase == Phase.Animation && HasUpcomingPulse)
                {
                    PulseTimeRemaining -= dt;
                    if (PulseTimeRemaining <= 0f)
                    {
                        PulseTimeRemaining = 0f;
                        HasUpcomingPulse = false;
                    }
                }
            }
            yield return null;
        }
        PhaseElapsed = PhaseTotal;
    }

    // ───────── Debug context menu (quick tests) ─────────
    [ContextMenu("Play F1")] void _PlayF1() { PlayPreset(Preset.F1); }
    [ContextMenu("Play C1 (90 bpm, alt peak)")] void _PlayC1() { PlayPreset(Preset.C1); }
    [ContextMenu("Play B2 (135 bpm, alt peak)")] void _PlayB2() { PlayPreset(Preset.B2); }

    [ContextMenu("Play Block 1")]
    void _PlayBlock1() { PlayBlock(new List<Preset> { Preset.A, Preset.B1, Preset.C1, Preset.B2, Preset.B2, Preset.C1, Preset.B1, Preset.C2, Preset.C2, Preset.C1, Preset.B2 }); }
    [ContextMenu("Play Block 2")]
    void _PlayBlock2() { PlayBlock(new List<Preset> { Preset.A, Preset.B1, Preset.B1, Preset.B2, Preset.C1, Preset.C1, Preset.B2, Preset.B2, Preset.C1, Preset.C2, Preset.C1 }); }
    [ContextMenu("Play Block 3")]
    void _PlayBlock3() { PlayBlock(new List<Preset> { Preset.A, Preset.C1, Preset.B2, Preset.C1, Preset.B2, Preset.C2, Preset.C1, Preset.B1, Preset.B1, Preset.B2, Preset.C2 }); }
    [ContextMenu("Play Block 4")]
    void _PlayBlock4() { PlayBlock(new List<Preset> { Preset.A, Preset.C2, Preset.C1, Preset.B1, Preset.B1, Preset.B2, Preset.C2, Preset.C1, Preset.B1, Preset.C2, Preset.B2 }); }
}
