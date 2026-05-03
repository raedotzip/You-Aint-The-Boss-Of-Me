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

    private Renderer[]            _renderers;
    private MaterialPropertyBlock _mpb;
    private Coroutine             _flashRoutine;

    private static readonly int TintID = Shader.PropertyToID("_Tint");
    private static readonly int WhitenessID = Shader.PropertyToID("_Whiteness");

    void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _mpb = new MaterialPropertyBlock();
    }

    public void Flash(float damage)
    {
        float t = Mathf.Clamp01(damage / referenceDamage);
        float strength = Mathf.Lerp(minFlashStrength, maxFlashStrength, t);

        if (_flashRoutine != null)
            StopCoroutine(_flashRoutine);

        _flashRoutine = StartCoroutine(DoFlash(strength));
    }

    private IEnumerator DoFlash(float peakStrength)
    {
        float elapsed = 0f;

        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;

            // Ease out
            float strength = Mathf.Lerp(peakStrength, 0f, elapsed / flashDuration);

            foreach (var r in _renderers)
            {
                if (r == null) continue;

                r.GetPropertyBlock(_mpb);

                _mpb.SetColor(TintID, Color.white);     // force white tint
                _mpb.SetFloat(WhitenessID, strength);   // control blend

                r.SetPropertyBlock(_mpb);
            }

            yield return null;
        }

        // Reset
        _mpb.Clear();
        foreach (var r in _renderers)
        {
            if (r != null)
                r.SetPropertyBlock(_mpb);
        }

        _flashRoutine = null;
    }
}
