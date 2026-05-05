/**
 * Hit the ground and cause obstacles to appear making the map
 * temporarily smaller for the player
 */

using UnityEngine;

public class Boss1GroundSlamShockwaveAttack : EnemyBaseState
{
    private float activeTime    = 10f;
    private float mapRadius     = 30f;
    private float ringThickness = 0.5f;
    private float ringWidth     = 3f;
    private int   ringCount     = 3;
    private float ringDelay     = 1.5f;

    private float spawnTimer    = 0f;
    private int   ringsSpawned  = 0;
    private bool  attackDone    = false;

    // Total duration before transitioning — all rings spawned plus time for last ring to expand
    private float totalDuration  => ringDelay * ringCount + activeTime;
    private float totalTimer     = 0f;

    public override void EnterState(EnemyStateManager state)
    {
        spawnTimer  = 0f;
        totalTimer  = 0f;
        ringsSpawned = 0;
        attackDone  = false;

        SpawnShockwave(state, ringsSpawned);
        ringsSpawned++;
    }

    public override void UpdateState(EnemyStateManager state)
    {
        if (attackDone)
            return;

        totalTimer += Time.deltaTime;

        // Spawn remaining rings with a delay between each
        if (ringsSpawned < ringCount)
        {
            spawnTimer += Time.deltaTime;

            if (spawnTimer >= ringDelay)
            {
                spawnTimer = 0f;
                SpawnShockwave(state, ringsSpawned);
                ringsSpawned++;
            }
        }

        // Wait for all rings to finish expanding before transitioning
        if (totalTimer >= totalDuration)
        {
            attackDone = true;
            Boss1StateManager boss = (Boss1StateManager)state;
            boss.TransitionToNextState();
        }
    }

    public override float OnBossHurt(EnemyStateManager state)
    {
        return 0;
    }

    private void SpawnShockwave(EnemyStateManager state, int ringIndex)
    {
        Vector3 spawnPos = state.transform.position;
        spawnPos.y       = 0f;

        float startRadius = ringIndex * ringWidth * 1.5f;

        Obstacle o = new Obstacle
        {
            position        = spawnPos,
            rotation        = Quaternion.identity,

            shapeType       = ObstacleShapeType.Cylinder,
            cylinderHeight  = ringThickness,
            cylinderRadius  = startRadius,
            isHollow        = true,
            innerRadius     = Mathf.Max(0f, startRadius - ringWidth),

            warningDuration = 0f,
            activeDuration  = activeTime,

            movementType    = ObstacleMovementType.Stationary,

            scalesOverTime  = true,
            initialScale    = new Vector3(startRadius * 2f, ringThickness, startRadius * 2f),
            finalScale      = new Vector3(mapRadius * 2f,   ringThickness, mapRadius * 2f),

            visualPrefab    = state.obstacleData.shockwavePrefab,
        };

        ObstacleManager.Instance.SpawnObstacle(o);
    }
}