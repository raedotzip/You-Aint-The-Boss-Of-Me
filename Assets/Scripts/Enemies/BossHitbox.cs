using UnityEngine;

public class BossHitbox : MonoBehaviour
{
    [Tooltip("Reference to the boss this hitbox belongs to.")]
    public EnemyStateManager boss;

    [Tooltip("Damage multiplier for hits on this limb. Head=2.0, Torso=1.0, Arms=0.75, Legs=0.5")]
    public float damageMultiplier = 1f;
}
