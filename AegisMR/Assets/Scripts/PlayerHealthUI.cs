using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space health bar that always faces the player.
/// Creates its own Canvas and UI bar at runtime — no prefab needed.
/// Attach to any GameObject; assign a PlayerHealth reference.
/// </summary>
public class PlayerHealthUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The PlayerHealth to display (auto-found if blank).")]
    public PlayerHealth playerHealth;

    [Header("Display Settings")]
    [Tooltip("Offset from center eye anchor (metres).")]
    public Vector3 offsetFromHead = new Vector3(0f, -0.35f, 1.2f);

    [Tooltip("Scale of the world-space canvas.")]
    public float canvasScale = 0.002f;

    [Header("Colors")]
    public Color healthyColor  = new Color(0.18f, 0.85f, 0.35f);
    public Color damagedColor  = new Color(0.95f, 0.72f, 0.07f);
    public Color criticalColor = new Color(0.9f,  0.15f, 0.15f);

    // ── Internal ──────────────────────────────────────────────────────────────
    private Transform _headAnchor;
    private Image     _fillImage;
    private Text      _hpText;
    private Canvas    _canvas;

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        // Find head anchor
        OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig != null) _headAnchor = rig.centerEyeAnchor;
        if (_headAnchor == null) _headAnchor = Camera.main?.transform;

        // Find health
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        BuildUI();

        // Subscribe to HP changes
        if (playerHealth != null)
        {
            playerHealth.OnDamaged.AddListener(UpdateBar);
            UpdateBar(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnDamaged.RemoveListener(UpdateBar);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Follow head
    // ─────────────────────────────────────────────────────────────────────────

    void LateUpdate()
    {
        if (_headAnchor == null || _canvas == null) return;

        // Position in front of the player
        _canvas.transform.position = _headAnchor.TransformPoint(offsetFromHead);

        // Always face the player
        _canvas.transform.rotation = Quaternion.LookRotation(
            _canvas.transform.position - _headAnchor.position
        );
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Bar update
    // ─────────────────────────────────────────────────────────────────────────

    void UpdateBar(float current, float max)
    {
        if (_fillImage == null) return;

        float fraction    = Mathf.Clamp01(current / max);
        _fillImage.fillAmount = fraction;

        // Color feedback
        if (fraction > 0.6f)        _fillImage.color = healthyColor;
        else if (fraction > 0.3f)   _fillImage.color = damagedColor;
        else                         _fillImage.color = criticalColor;

        if (_hpText != null)
            _hpText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Build UI procedurally (no prefab required)
    // ─────────────────────────────────────────────────────────────────────────

    void BuildUI()
    {
        // ── Canvas ────────────────────────────────────────────────────────────
        GameObject canvasGO = new GameObject("HealthBarCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode  = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        canvasGO.transform.localScale = Vector3.one * canvasScale;
        RectTransform canvasRect  = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta      = new Vector2(300f, 40f);

        // ── Background ────────────────────────────────────────────────────────
        GameObject bg   = new GameObject("Background");
        bg.transform.SetParent(canvasGO.transform, false);
        Image bgImg     = bg.AddComponent<Image>();
        bgImg.color     = new Color(0f, 0f, 0f, 0.6f);
        RectTransform bgRect        = bg.GetComponent<RectTransform>();
        bgRect.anchorMin            = Vector2.zero;
        bgRect.anchorMax            = Vector2.one;
        bgRect.sizeDelta            = Vector2.zero;

        // ── Fill ──────────────────────────────────────────────────────────────
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(canvasGO.transform, false);
        _fillImage      = fill.AddComponent<Image>();
        _fillImage.color = healthyColor;
        _fillImage.type  = Image.Type.Filled;
        _fillImage.fillMethod  = Image.FillMethod.Horizontal;
        _fillImage.fillAmount  = 1f;
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin     = new Vector2(0.01f, 0.08f);
        fillRect.anchorMax     = new Vector2(0.99f, 0.92f);
        fillRect.sizeDelta     = Vector2.zero;

        // ── HP Text ───────────────────────────────────────────────────────────
        GameObject textGO = new GameObject("HPText");
        textGO.transform.SetParent(canvasGO.transform, false);
        _hpText           = textGO.AddComponent<Text>();
        _hpText.alignment = TextAnchor.MiddleCenter;
        _hpText.fontSize  = 18;
        _hpText.color     = Color.white;
        _hpText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin     = Vector2.zero;
        textRect.anchorMax     = Vector2.one;
        textRect.sizeDelta     = Vector2.zero;

        Debug.Log("[PlayerHealthUI] Health bar created.");
    }
}
