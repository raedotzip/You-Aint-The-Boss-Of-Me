using UnityEngine;

// Spawns several homing projectiles (viruses) that track the player
// TODO: implement — use BulletManager.SpawnBullet with MovementType.Homing (or similar)
public class Boss2VirusSwarmAttack : EnemyBaseState
{
    public int   virusCount   = 6;
    public float spawnDelay   = 0.2f;  // seconds between each virus spawn
    public float bulletSpeed  = 5f;
    public float damage       = 10f;
    public float lifetime     = 5f;

    private float _timer;
    private int   _spawned;

    public override void EnterState(EnemyStateManager state)
    {
        _timer   = 0f;
        _spawned = 0;
    }

    public override void UpdateState(EnemyStateManager state)
    {
        _timer += Time.deltaTime;

        if (_spawned < virusCount && _timer >= _spawned * spawnDelay)
        {
            // TODO: fire one homing bullet toward the player
            // Example using existing FireBullet — swap for a homing AttackData once you have one
            _spawned++;
        }

        if (_spawned >= virusCount && _timer >= virusCount * spawnDelay + 1f)
            ((Boss2StateManager)state).TransitionToNextState();
    }

    public override float OnBossHurt(EnemyStateManager state) => 0f;
}
