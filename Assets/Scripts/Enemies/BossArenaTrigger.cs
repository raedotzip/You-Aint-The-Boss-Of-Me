using UnityEngine;

// Place this on a trigger collider at the entrance of each boss arena.
// When the player walks in, it starts that boss fight via BossManager.
// Attach one per arena and set bossIndex to 1, 2, or 3.
[RequireComponent(typeof(Collider))]
public class BossArenaTrigger : MonoBehaviour
{
    [Tooltip("Which boss this arena belongs to (1 = Boss 1, 2 = Boss 2, 3 = Boss 3)")]
    public int bossIndex = 1;

    [Tooltip("Tag on the player root GameObject")]
    public string playerTag = "Player";

    private bool _triggered;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!other.CompareTag(playerTag)) return;
        if (BossManager.Instance == null) return;
        if (BossManager.Instance.IsBossDefeated(bossIndex)) return;
        if (BossManager.Instance.ActiveBossIndex == bossIndex) return;

        _triggered = true;
        BossManager.Instance.SetActiveBoss(bossIndex);
    }

    // Called by BossManager.ResetRun() when the player returns to the menu.
    public void ResetTrigger()
    {
        _triggered = false;
    }
}
