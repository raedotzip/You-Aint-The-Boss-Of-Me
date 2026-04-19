using System.Collections;
using UnityEngine;

// Add this component to any boss root GameObject.
// Call Flash(damage) to trigger a white hit flash scaled to damage intensity.
public class BossHitFlash : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float flashDuration = 0.15f;

    [Header("Intensity")]
    [Tooltip("How far toward white a light hit pushes the color (0–1)")]
    [SerializeField] private float minFlashStrength = 0.25f;
    [Tooltip("How far toward white a heavy hit pushes the color (0–1)")]
    [SerializeField] private float maxFlashStrength = 0.88f;
    [Tooltip("Damage value that maps to maxFlashStrength. Hits above this are capped.")]
    [SerializeField] private float referenceDamage  = 25f;

    // Shader property IDs — cached so string lookups happen once
    private static readonly int ColorID     = Shader.PropertyToID("_Color");
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    private Renderer[]            _renderers;
    private MaterialPropertyBlock _mpb;
    private Color[]               _originalColors;
    private Coroutine             _flashRoutine;

    void Awake()
    {
        _renderers      = GetComponentsInChildren<Renderer>(true);
        _mpb            = new MaterialPropertyBlock();
        _originalColors = new Color[_renderers.Length];

        for (int i = 0; i < _renderers.Length; i++)
        {
            Material m = _renderers[i].sharedMaterial;
            if (m == null) { _originalColors[i] = Color.white; continue; }

            // Read the base colour from whichever property this shader exposes
            if      (m.HasProperty(ColorID))     _originalColors[i] = m.GetColor(ColorID);
            else if (m.HasProperty(BaseColorID)) _originalColors[i] = m.GetColor(BaseColorID);
            else                                  _originalColors[i] = Color.white;
        }
    }

    // damage — the raw damage value of the hit; drives how white the flash appears
    public void Flash(float damage)
    {
        float t        = Mathf.Clamp01(damage / referenceDamage);
        float strength = Mathf.Lerp(minFlashStrength, maxFlashStrength, t);

        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(DoFlash(strength));
    }

    private IEnumerator DoFlash(float peakStrength)
    {
        float elapsed = 0f;

        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            // Ease out: full flash immediately, then fade back to original
            float strength = Mathf.Lerp(peakStrength, 0f, elapsed / flashDuration);

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                Color c = Color.Lerp(_originalColors[i], Color.white, strength);
                _renderers[i].GetPropertyBlock(_mpb);
                _mpb.SetColor(ColorID,     c);  // Standard shader
                _mpb.SetColor(BaseColorID, c);  // URP Lit
                _renderers[i].SetPropertyBlock(_mpb);
            }

            yield return null;
        }

        // Clear property block so originals are fully restored
        _mpb.Clear();
        foreach (var r in _renderers)
        {
            if (r != null) r.SetPropertyBlock(_mpb);
        }

        _flashRoutine = null;
    }
}
