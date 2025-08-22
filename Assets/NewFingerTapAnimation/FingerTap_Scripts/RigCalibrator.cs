using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RigCalibrator : MonoBehaviour
{
    [Header("Scene refs")]
    public SequenceManager     seq;         // GameManager with SequenceManager
    public VROverlayController overlay;     // VR_OverlayCanvas controller
    public Transform           rigRoot;     // OVRCameraRig (or your VR rig root)
    public GameObject          handRoot;    // HandTap_Animations root (to show/hide)
    public Animator            handAnimator;// Animator on HandTap_Animations

    [Header("UI (on Calibration Panel)")]
    public Toggle calibrationToggle;
    public Slider ySlider; public Button yPlusBtn; public Button yMinusBtn;
    public Slider zSlider; public Button zPlusBtn; public Button zMinusBtn;

    [Header("Ranges & steps (meters)")]
    public float yRange = 0.25f;   // +/- 25 cm range
    public float zRange = 0.25f;   // +/- 25 cm range
    public float yStep  = 0.005f;  // 5 mm per click
    public float zStep  = 0.005f;  // 5 mm per click

    [Header("Optional neutral pose (will freeze even without it)")]
    public string neutralStateName = "";    // e.g., "Edit_Tap_112" or leave blank

    // Live state
    public bool   IsCalibrating { get; private set; }
    float baseY, baseZ;     // initial local position of rig root
    float offY, offZ;       // current offsets (local)
    Vector3 baseLocalPos;

    const string PREF_Y = "Calib_OffsetY";
    const string PREF_Z = "Calib_OffsetZ";

    void Awake()
    {
        if (!rigRoot)
        {
            Debug.LogWarning("RigCalibrator: rigRoot not assigned.");
            return;
        }
        baseLocalPos = rigRoot.localPosition;
        baseY = baseLocalPos.y;
        baseZ = baseLocalPos.z;

        offY = PlayerPrefs.GetFloat(PREF_Y, 0f);
        offZ = PlayerPrefs.GetFloat(PREF_Z, 0f);
        ApplyOffsets();

        // Configure sliders min/max once
        if (ySlider) { ySlider.minValue = -yRange; ySlider.maxValue =  yRange; ySlider.wholeNumbers = false; }
        if (zSlider) { zSlider.minValue = -zRange; zSlider.maxValue =  zRange; zSlider.wholeNumbers = false; }
        SyncSliders();
    }

    void Start()
    {
        // Hook UI
        if (calibrationToggle) calibrationToggle.onValueChanged.AddListener(OnCalibrationToggle);

        if (ySlider)   ySlider.onValueChanged.AddListener((v) => { offY = Mathf.Clamp(v, -yRange, yRange); ApplyOffsetsSave(); });
        if (zSlider)   zSlider.onValueChanged.AddListener((v) => { offZ = Mathf.Clamp(v, -zRange, zRange); ApplyOffsetsSave(); });

        if (yPlusBtn)  yPlusBtn.onClick.AddListener(() => { offY = Mathf.Clamp(offY + yStep, -yRange, yRange); SyncSliders(); ApplyOffsetsSave(); });
        if (yMinusBtn) yMinusBtn.onClick.AddListener(() => { offY = Mathf.Clamp(offY - yStep, -yRange, yRange); SyncSliders(); ApplyOffsetsSave(); });
        if (zPlusBtn)  zPlusBtn.onClick.AddListener(() => { offZ = Mathf.Clamp(offZ + zStep, -zRange, zRange); SyncSliders(); ApplyOffsetsSave(); });
        if (zMinusBtn) zMinusBtn.onClick.AddListener(() => { offZ = Mathf.Clamp(offZ - zStep, -zRange, zRange); SyncSliders(); ApplyOffsetsSave(); });

        // If the toggle starts ON in the editor, enforce the state
        if (calibrationToggle && calibrationToggle.isOn) EnterCalibration();
    }

    void OnDestroy()
    {
        if (calibrationToggle) calibrationToggle.onValueChanged.RemoveListener(OnCalibrationToggle);
    }

    // ───────── Calibration flow ─────────

    void OnCalibrationToggle(bool on)
    {
        if (on) EnterCalibration();
        else    ExitCalibration();
    }

    void EnterCalibration()
    {
        IsCalibrating = true;

        // Stop any running sequence and clean overlays
        if (seq) seq.StopCurrent();
        if (overlay)
        {
            overlay.ResetOverlay();
            overlay.ShowBlackScreen(false);
            overlay.ShowRedDot(false);
            overlay.HideCountdown();
            overlay.gameObject.SetActive(true);
        }

        // Show hands, freeze animator at a neutral pose (or current pose)
        if (handRoot) handRoot.SetActive(true);
        if (handAnimator)
        {
            handAnimator.speed = 0f; // freeze
            if (!string.IsNullOrEmpty(neutralStateName))
            {
                int h = Animator.StringToHash(neutralStateName);
                if (handAnimator.HasState(0, h))
                {
                    handAnimator.Play(h, 0, 0f);
                    handAnimator.Update(0f);
                }
                else
                {
                    Debug.LogWarning($"RigCalibrator: Neutral state '{neutralStateName}' not found on Base Layer; freezing current pose.");
                }
            }
        }

        // Ensure offsets are applied & sliders reflect them
        ApplyOffsets();
        SyncSliders();
    }

    void ExitCalibration()
    {
        IsCalibrating = false;

        // Hide hands (match your normal idle overlay state)
        if (handRoot) handRoot.SetActive(false);

        // Unfreeze animator
        if (handAnimator) handAnimator.speed = 1f;

        // Keep overlay in its idle state; SequenceManager will control it next
        if (overlay)
        {
            overlay.ResetOverlay();
            overlay.ShowBlackScreen(false);
        }
    }

    // ───────── Offsets ─────────

    void ApplyOffsets()
    {
        if (!rigRoot) return;
        var p = baseLocalPos;
        p.y = baseY + offY;
        p.z = baseZ + offZ;
        rigRoot.localPosition = p;
    }

    void ApplyOffsetsSave()
    {
        ApplyOffsets();
        PlayerPrefs.SetFloat(PREF_Y, offY);
        PlayerPrefs.SetFloat(PREF_Z, offZ);
        PlayerPrefs.Save();
    }

    void SyncSliders()
    {
        if (ySlider) ySlider.SetValueWithoutNotify(offY);
        if (zSlider) zSlider.SetValueWithoutNotify(offZ);
    }
}
