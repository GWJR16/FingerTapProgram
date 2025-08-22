using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class SkinToneController : MonoBehaviour
{
    [Header("Targets (drag ALL renderers that show the hand/arm skin)")]
    public Renderer[] handRenderers;

    [Header("Operator UI")]
    public TMP_Dropdown skinToneDropdown;

    [Header("Preset tones (0 = Model Default, then Light → Dark)")]
    public Color[] skinTones = new Color[]
    {
        Color.clear,                       // 0: Model Default (restores original)
        new Color(1.00f, 0.86f, 0.74f),    // 1: Light
        new Color(0.95f, 0.79f, 0.64f),    // 2: Medium Light
        new Color(0.83f, 0.64f, 0.50f),    // 3: Medium
        new Color(0.60f, 0.45f, 0.33f),    // 4: Medium Dark
        new Color(0.42f, 0.31f, 0.23f)     // 5: Dark
    };

    // Shader color properties (Built-in/Standard vs URP Lit)
    static readonly int PROP_BASECOLOR = Shader.PropertyToID("_BaseColor");
    static readonly int PROP_COLOR     = Shader.PropertyToID("_Color");

    const string PrefKey = "SkinToneIndex";

    // Per-renderer caches so we can restore Model Default precisely
    private Material[] _instancedMaterials;
    private int[]      _colorPropPerRenderer; // PROP_BASECOLOR / PROP_COLOR / 0 if none
    private Color[]    _originalColors;

    void Awake()
    {
        CacheMaterialsAndOriginals();
    }

    void OnEnable()
    {
        if (skinToneDropdown != null)
            skinToneDropdown.onValueChanged.AddListener(ApplySkinTone);
    }

    void OnDisable()
    {
        if (skinToneDropdown != null)
            skinToneDropdown.onValueChanged.RemoveListener(ApplySkinTone);
    }

    void Start()
    {
        // Load last selection (default to Model Default)
        int idx = PlayerPrefs.GetInt(PrefKey, 0);
        idx = Mathf.Clamp(idx, 0, Mathf.Max(0, skinTones.Length - 1));

        if (skinToneDropdown != null)
            skinToneDropdown.SetValueWithoutNotify(idx);

        ApplySkinTone(idx);
    }

    /// <summary>
    /// Public helper if you want to call it from other scripts (e.g., OperatorPanel).
    /// </summary>
    public void RefreshFromDropdown()
    {
        if (skinToneDropdown != null)
            ApplySkinTone(skinToneDropdown.value);
    }

    /// <summary>
    /// Main apply method (hooked to dropdown).
    /// </summary>
    public void ApplySkinTone(int index)
    {
        if (_instancedMaterials == null || _instancedMaterials.Length == 0)
            CacheMaterialsAndOriginals();

        if (index < 0 || index >= skinTones.Length)
            return;

        // Persist for next run
        PlayerPrefs.SetInt(PrefKey, index);
        PlayerPrefs.Save();

        // Index 0 => Model Default (restore original cached colors)
        if (index == 0)
        {
            for (int i = 0; i < _instancedMaterials.Length; i++)
            {
                var m    = _instancedMaterials[i];
                int prop = _colorPropPerRenderer[i];
                if (!m || prop == 0) continue;

                m.SetColor(prop, _originalColors[i]);
            }
            return;
        }

        // Otherwise, apply the chosen tint
        Color tint = skinTones[index];

        for (int i = 0; i < _instancedMaterials.Length; i++)
        {
            var m    = _instancedMaterials[i];
            int prop = _colorPropPerRenderer[i];
            if (!m || prop == 0) continue;

            m.SetColor(prop, tint);
        }
    }

    /// <summary>
    /// Cache runtime material instances and the original colors so we can restore defaults.
    /// </summary>
    private void CacheMaterialsAndOriginals()
    {
        int n = (handRenderers != null) ? handRenderers.Length : 0;

        _instancedMaterials    = new Material[n];
        _colorPropPerRenderer  = new int[n];
        _originalColors        = new Color[n];

        for (int i = 0; i < n; i++)
        {
            var r = handRenderers[i];
            if (!r)
            {
                _colorPropPerRenderer[i] = 0;
                continue;
            }

            // Use .material to force a per-renderer instance (won’t edit shared asset)
            var m = r.material;
            _instancedMaterials[i] = m;

            if (m.HasProperty(PROP_BASECOLOR))
            {
                _colorPropPerRenderer[i] = PROP_BASECOLOR;
                _originalColors[i]       = m.GetColor(PROP_BASECOLOR);
            }
            else if (m.HasProperty(PROP_COLOR))
            {
                _colorPropPerRenderer[i] = PROP_COLOR;
                _originalColors[i]       = m.GetColor(PROP_COLOR);
            }
            else
            {
                _colorPropPerRenderer[i] = 0; // unsupported shader (no color)
            }
        }
    }
}
