using UnityEngine;

// Place this on a trigger collider at the entrance of each boss arena.
// When the player walks in, it starts that boss fight via BossManager.
// Attach one per arena and set bossIndex to 1 or 2.
[RequireComponent(typeof(BoxCollider))]
public class BossArenaTrigger : MonoBehaviour
{
    [Tooltip("Which boss this arena belongs to (1 = Boss 1, 2 = Boss 2)")]
    public int bossIndex = 1;

    [Tooltip("Tag on the player root GameObject")]
    public string playerTag = "Player";

    private bool _triggered;
    private BoxCollider _col;

    // Pre-allocated buffer — avoids a heap alloc every frame
    private static readonly Collider[] _overlapBuffer = new Collider[8];

    private void Awake()
    {
        _col = GetComponent<BoxCollider>();
        _col.isTrigger = true;
    }

    // Physics events fire for normal movement.
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag)) Activate();
    }

    // OverlapBox poll catches players who dash or teleport through the trigger
    // fast enough that OnTriggerEnter is skipped.
    private void Update()
    {
        if (_triggered) return;

        Vector3 worldCenter = transform.TransformPoint(_col.center);
        Vector3 halfExtents = new Vector3(
            _col.size.x * 0.5f * Mathf.Abs(transform.lossyScale.x),
            _col.size.y * 0.5f * Mathf.Abs(transform.lossyScale.y),
            _col.size.z * 0.5f * Mathf.Abs(transform.lossyScale.z)
        );

        int count = Physics.OverlapBoxNonAlloc(
            worldCenter, halfExtents, _overlapBuffer,
            transform.rotation, ~0, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            if (_overlapBuffer[i].CompareTag(playerTag))
            {
                Activate();
                return;
            }
        }
    }

    private void Activate()
    {
        if (_triggered) return;
        if (BossManager.Instance == null) return;
        if (BossManager.Instance.IsBossDefeated(bossIndex)) return;
        if (BossManager.Instance.ActiveBossIndex == bossIndex) return;
        if (BossManager.Instance.ActiveBossIndex != 0) return; // another fight in progress

        _triggered = true;
        BossManager.Instance.SetActiveBoss(bossIndex);
    }

    // Called by BossManager.ResetRun() when the player returns to the menu.
    public void ResetTrigger()
    {
        _triggered = false;
    }
}
