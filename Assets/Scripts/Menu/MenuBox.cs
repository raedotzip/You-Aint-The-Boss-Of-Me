using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Put this on each menu option box.
// Requires a Rigidbody (kinematic) + Collider on the Menu layer.
// Wire onSliced in the Inspector to MenuController methods.
[RequireComponent(typeof(Rigidbody))]
public class MenuBox : MonoBehaviour
{

    [Header("Slice Settings")]
    [Tooltip("Sphere radius around box center that counts as hovering")]
    public float hoverRadius = 0.45f;

    [Header("Visual")]
    public Renderer boxRenderer;
    public Color normalColor  = new Color(0.04f, 0.04f, 0.14f, 1f);
    public Color hoverColor   = new Color(0.00f, 0.80f, 0.90f, 1f);
    public Color sliceColor   = Color.white;
    public float hoverSmooth  = 10f;

    [Header("Action (wire in Inspector)")]
    public UnityEvent onSliced = new UnityEvent();

    private Sword    _sword;
    private bool     _isHovered;
    private bool     _sliced;
    private Color    _currentColor;
    private Vector3  _baseScale;
    private Material _matInstance;   // per-renderer instance so we don't share state
    private bool     _isURP;         // true → use _BaseColor, false → use _Color

    private static readonly int ColorID     = Shader.PropertyToID("_Color");
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionID  = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        // Make Rigidbody kinematic — box shouldn't fall
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;
    }

    void Start()
    {
        _sword        = FindObjectOfType<Sword>();
        _baseScale    = transform.localScale;
        _currentColor = normalColor;

        if (boxRenderer == null)
            boxRenderer = GetComponentInChildren<Renderer>();

        EnsureMaterial();
        ApplyColor(normalColor, 0f);
    }

    void Update()
    {
        if (_sword == null || _sliced) return;

        Transform tip  = _sword.bladeTip != null ? _sword.bladeTip : _sword.transform;
        float     dist = Vector3.Distance(tip.position, transform.position);
        bool      over = dist <= hoverRadius;

        if (over != _isHovered)
        {
            _isHovered = over;
            ApplyColor(_isHovered ? hoverColor : normalColor, hoverSmooth);
        }

        // Trigger slice when the sword tip enters the hover zone with enough swing speed
        if (over && _sword.Velocity.magnitude >= 1.0f)
            OnSliced();
    }

    // Called by MenuController.ReturnToMenu() so the box can be sliced again
    public void ResetSlice()
    {
        _sliced    = false;
        _isHovered = false;
        ApplyColor(normalColor, 0f);
    }

    // Called by Sword.cs when the blade sweep hits this box
    public void OnSliced()
    {
        if (_sliced) return;
        _sliced = true;
        StartCoroutine(SliceSequence());
    }

    IEnumerator SliceSequence()
    {
        ApplyColor(sliceColor, 0f);
        transform.localScale = _baseScale * 1.25f;

        yield return new WaitForSeconds(0.12f);

        transform.localScale = _baseScale;
        onSliced.Invoke();
    }

    void ApplyColor(Color target, float smooth)
    {
        StopCoroutine(nameof(LerpColor));
        if (smooth > 0f)
            StartCoroutine(LerpColor(target, smooth));
        else
            SetBlock(target);
    }

    IEnumerator LerpColor(Color target, float speed)
    {
        while (Vector4.Distance(_currentColor, target) > 0.005f)
        {
            _currentColor = Color.Lerp(_currentColor, target, Time.deltaTime * speed);
            SetBlock(_currentColor);
            yield return null;
        }
        _currentColor = target;
        SetBlock(target);
    }

    void EnsureMaterial()
    {
        if (boxRenderer == null) return;

        // If the renderer has no material or the error/pink shader, create one automatically
        Material shared = boxRenderer.sharedMaterial;
        bool isBroken   = shared == null
                       || shared.shader == null
                       || shared.shader.name.StartsWith("Hidden/")
                       || shared.shader.name.Contains("Error");

        if (isBroken)
        {
            // Try URP first, then Built-in Standard
            Shader s = Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Standard");
            shared = s != null ? new Material(s) : new Material(Shader.Find("Diffuse"));
            boxRenderer.sharedMaterial = shared;
        }

        // Create a per-instance material so property changes don't affect other boxes
        _matInstance = boxRenderer.material;   // auto-instantiates
        _isURP       = _matInstance.HasProperty(BaseColorID);

        // Enable the emission keyword so _EmissionColor is rendered
        _matInstance.EnableKeyword("_EMISSION");
        _matInstance.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
    }

    void SetBlock(Color c)
    {
        if (_matInstance == null) return;

        if (_isURP)
            _matInstance.SetColor(BaseColorID, c);
        else
            _matInstance.SetColor(ColorID, c);

        if (_matInstance.HasProperty(EmissionID))
            _matInstance.SetColor(EmissionID, c * 0.6f);
    }
}
