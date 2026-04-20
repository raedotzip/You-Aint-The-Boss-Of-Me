using UnityEngine;

// Attach this to the same GameObject as the lava pit BoxCollider (lavaPitCenter).
// The BoxCollider must be set as a Trigger. Every frame the player overlaps it,
// they are pushed back to the nearest point on the pit edge so they can't fall in.
[RequireComponent(typeof(BoxCollider))]
public class LavaPitBarrier : MonoBehaviour
{
    [Tooltip("Tag used to identify the player root object")]
    public string playerTag = "Player";

    private BoxCollider _pitCollider;

    private void Awake()
    {
        _pitCollider = GetComponent<BoxCollider>();
        _pitCollider.isTrigger = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        CharacterController cc = other.GetComponentInParent<CharacterController>();
        if (cc == null) cc = other.GetComponent<CharacterController>();
        if (cc == null) return;

        // Find the closest point on the pit box surface in world space
        Vector3 playerPos  = cc.transform.position;
        Vector3 closestPt  = _pitCollider.ClosestPointOnBounds(playerPos);

        // Push the player to that edge point (keep their current Y so gravity still works)
        Vector3 safePos    = closestPt;
        safePos.y          = playerPos.y;

        // Move only the XZ offset — nudge them out of the trigger
        Vector3 delta = safePos - playerPos;
        delta.y       = 0f;

        if (delta.sqrMagnitude > 0.0001f)
            cc.Move(delta);
    }
}
