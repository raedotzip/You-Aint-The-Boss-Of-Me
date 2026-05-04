using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LoreTrigger : MonoBehaviour
{
    [Tooltip("The LoreTyper on your HUD")]
    public LoreTyper loreTyper;

    [Tooltip("Lines to display. Multiple = sequence.")]
    public string[] messages;

    [Tooltip("Tag on the player root GameObject")]
    public string playerTag = "Player";

    [Tooltip("Allow this trigger to fire again after reset")]
    private bool _fired;

    private void Awake() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (_fired || !other.CompareTag(playerTag)) return;
        _fired = true;

        if (messages == null || messages.Length == 0) return;

        if (messages.Length == 1)
            loreTyper.ShowLore(messages[0]);
        else
            loreTyper.ShowLoreSequence(messages);
    }

    public void ResetTrigger() => _fired = false;
}
