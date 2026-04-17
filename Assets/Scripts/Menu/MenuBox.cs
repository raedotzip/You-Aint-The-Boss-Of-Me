using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Put this on each menu option box.
// Requires a Rigidbody (kinematic) + Collider on the Menu layer.
// Wire onSliced in the Inspector to MenuController methods.
[RequireComponent(typeof(Rigidbody))]
public class MenuBox : MonoBehaviour
{
    [Header("Label")]
    [Tooltip("Text shown on the front face — auto-created as a child TextMesh")]
    public string label = "Option";
    public Color  labelColor = Color.white;
    public float  labelSize  = 0.12f; // world-space character height

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

    private Sword   _sword;
    private bool    _isHovered;
    private bool    _sliced;
    private Color   _currentColor;
    private Vector3 _baseScale;

    private static readonly int ColorID    = Shader.PropertyToID("_Color");
    private static readonly int EmissionID = Shader.PropertyToID("_EmissionColor");

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

        ApplyColor(normalColor, 0f);
        BuildLabel();
    }

    void BuildLabel()
    {
        if (string.IsNullOrEmpty(label)) return;

        // Reuse existing label child if already created
        Transform existing = transform.Find("_Label");
        GameObject go = existing != null ? existing.gameObject : new GameObject("_Label");
        go.transform.SetParent(transform, worldPositionStays: false);

        // Sit just in front of the box face (local Z = -0.5 is the front face of a unit cube)
        go.transform.localPosition = new Vector3(0f, 0f, -0.55f);
        go.transform.localRotation = Quaternion.identity;

        // Compensate for the parent's non-uniform scale so the text
        // appears at a consistent world-space size regardless of box dimensions.
        Vector3 ws = transform.lossyScale;
        go.transform.localScale = new Vector3(labelSize / ws.x, labelSize / ws.y, labelSize / ws.z);

        TextMesh tm = go.GetComponent<TextMesh>();
        if (tm == null) tm = go.AddComponent<TextMesh>();
        tm.text      = label;
        tm.color     = labelColor;
        tm.fontSize  = 64;
        tm.alignment = TextAlignment.Center;
        tm.anchor    = TextAnchor.MiddleCenter;
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

    void SetBlock(Color c)
    {
        if (boxRenderer == null) return;
        var block = new MaterialPropertyBlock();
        boxRenderer.GetPropertyBlock(block);
        block.SetColor(ColorID,    c);
        block.SetColor(EmissionID, c * 0.6f);
        boxRenderer.SetPropertyBlock(block);
    }
}
