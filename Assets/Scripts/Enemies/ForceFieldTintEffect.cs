using System.Collections.Generic;
using UnityEngine;

// Applies a pink tint to any renderer that enters the force field trigger.
// Attach to the ForceFieldVolume GameObject alongside a Collider set to Is Trigger.
[RequireComponent(typeof(Collider))]
public class ForceFieldTintEffect : MonoBehaviour
{
    [ColorUsage(false, true)]
    public Color tintColor = new Color(1f, 0.3f, 0.6f, 1f);

    // How strongly the tint blends onto the object (0 = none, 1 = full)
    [Range(0f, 1f)]
    public float tintStrength = 0.4f;

    static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    static readonly int TintColorID = Shader.PropertyToID("_TintColor");
    static readonly int TintStrengthID = Shader.PropertyToID("_TintStrength");

    // Track property blocks per renderer so we can restore them on exit
    readonly Dictionary<Renderer, Color> originalColors = new();
    readonly Dictionary<Renderer, MaterialPropertyBlock> blocks = new();

    void OnTriggerEnter(Collider other)
    {
        foreach (var r in other.GetComponentsInChildren<Renderer>())
            ApplyTint(r);
    }

    void OnTriggerExit(Collider other)
    {
        foreach (var r in other.GetComponentsInChildren<Renderer>())
            RemoveTint(r);
    }

    void ApplyTint(Renderer r)
    {
        if (originalColors.ContainsKey(r)) return;

        var block = new MaterialPropertyBlock();
        r.GetPropertyBlock(block);
        blocks[r] = block;

        Color baseColor = Color.white;
        // Try to read the existing base color so we blend rather than overwrite
        foreach (var mat in r.sharedMaterials)
        {
            if (mat != null && mat.HasProperty(BaseColorID))
            {
                baseColor = mat.GetColor(BaseColorID);
                break;
            }
        }

        originalColors[r] = baseColor;
        Color blended = Color.Lerp(baseColor, tintColor, tintStrength);
        block.SetColor(BaseColorID, blended);
        r.SetPropertyBlock(block);
    }

    void RemoveTint(Renderer r)
    {
        if (!originalColors.TryGetValue(r, out Color original)) return;

        var block = blocks[r];
        block.SetColor(BaseColorID, original);
        r.SetPropertyBlock(block);

        originalColors.Remove(r);
        blocks.Remove(r);
    }

    // Clean up if the script is disabled mid-fight
    void OnDisable()
    {
        foreach (var kvp in originalColors)
        {
            if (kvp.Key == null) continue;
            var block = blocks[kvp.Key];
            block.SetColor(BaseColorID, kvp.Value);
            kvp.Key.SetPropertyBlock(block);
        }
        originalColors.Clear();
        blocks.Clear();
    }
}
