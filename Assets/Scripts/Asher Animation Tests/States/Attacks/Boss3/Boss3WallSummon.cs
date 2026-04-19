using UnityEngine;

// Summons two flat walls flanking the player to restrict dodge space.
// Walls are oriented perpendicular to the boss-player axis so the player
// is squeezed into a narrow corridor facing the boss.
public class Boss3WallSummon : EnemyBaseState
{
    private float _wallHalfLength   = 5.5f;   // long axis half-extent
    private float _wallHalfHeight   = 2.5f;   // vertical half-extent
    private float _wallHalfThick    = 0.35f;  // thin axis half-extent
    private float _wallOffset       = 5f;     // left/right distance from player
    private float _warningDuration  = 1.0f;
    private float _activeDuration   = 5.5f;
    private float _transitionDelay  = 0.2f;

    private bool  _spawned;
    private bool  _done;
    private float _timer;

    public override void EnterState(EnemyStateManager state)
    {
        _spawned = false;
        _done    = false;
        _timer   = 0f;
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (_done) return;

        _timer += Time.deltaTime;

        if (!_spawned)
        {
            _spawned = true;
            SpawnWalls(state);
        }

        if (_timer >= _transitionDelay)
        {
            _done = true;
            ((Boss3StateManager)state).TransitionToNextState();
        }
    }

    public override float OnBossHurt(EnemyStateManager state) => 0f;

    private void SpawnWalls(EnemyStateManager state)
    {
        Vector3 playerPos = state.player.position;
        Vector3 toBoss    = state.transform.position - playerPos;
        toBoss.y = 0f;
        if (toBoss.sqrMagnitude < 0.01f) toBoss = Vector3.forward;
        toBoss.Normalize();

        // Perpendicular axis — left and right relative to the boss-player line
        Vector3 perp = Vector3.Cross(Vector3.up, toBoss).normalized;

        // Wall faces toward/away from boss (wall's forward = perpendicular to boss-player axis)
        Quaternion wallRot = Quaternion.LookRotation(perp);

        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 wallCenter   = playerPos + perp * (side * _wallOffset);
            wallCenter.y         = playerPos.y + _wallHalfHeight;

            Obstacle o = new Obstacle
            {
                position        = wallCenter,
                rotation        = wallRot,

                shapeType       = ObstacleShapeType.Box,
                boxHalfExtents  = new Vector3(_wallHalfThick, _wallHalfHeight, _wallHalfLength),

                warningDuration = _warningDuration,
                activeDuration  = _activeDuration,

                movementType    = ObstacleMovementType.Stationary,
                visualPrefab    = state.obstacleData.shockwavePrefab,
            };

            ObstacleManager.Instance.SpawnObstacle(o);
        }
    }
}
