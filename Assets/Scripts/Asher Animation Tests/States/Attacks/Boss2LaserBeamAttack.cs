using UnityEngine;

// Rotating laser beam(s) that sweep the arena
// TODO: implement — add a LineRenderer or cylinder prefab that rotates around the boss
public class Boss2LaserBeamAttack : EnemyBaseState
{
    public float duration    = 3f;
    public float rotateSpeed = 90f;  // degrees per second
    public float damage      = 15f;

    private float _timer;

    public override void EnterState(EnemyStateManager state)
    {
        _timer = 0f;
        // TODO: spawn laser visual(s) and enable them
    }

    public override void UpdateState(EnemyStateManager state)
    {
        _timer += Time.deltaTime;
        // TODO: rotate the laser transform(s) around the boss each frame
        // TODO: raycast along laser direction and deal damage if player is hit

        if (_timer >= duration)
        {
            // TODO: destroy/disable laser visuals
            ((Boss2StateManager)state).TransitionToNextState();
        }
    }

    public override float OnBossHurt(EnemyStateManager state) => 0f;
}
