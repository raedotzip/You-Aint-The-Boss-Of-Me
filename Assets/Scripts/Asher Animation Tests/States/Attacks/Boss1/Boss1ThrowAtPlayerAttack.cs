/**
 * Throw a big rock or some shit at the player
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss1ThrowAtPlayerAttack : EnemyBaseState
{
    private float throwDelay = 1.5f;
    private float timer = 0f;
    private bool hasThrown = false;

    private float totalDuration = 4f;
    public override void EnterState(EnemyStateManager state)
    {
        timer = 0f;
        hasThrown = false;
    }

    public override void UpdateState(EnemyStateManager state)
    {
        timer += Time.deltaTime;

        if (!hasThrown && timer >= throwDelay)
        {
            ThrowRock(state);
            hasThrown = true;
        }

        if (timer >= totalDuration)
        {
            Boss1StateManager boss = (Boss1StateManager)state;
            boss.TransitionToNextState();
        }
    }

    public override float OnBossHurt(EnemyStateManager state)
    {
        return 0;
    }

    //Probably really wrong 
    private void ThrowRock(EnemyStateManager state)
    {
        Vector3 spawnPos = state.transform.position + Vector3.up * 5f;

        Transform player = GameObject.FindGameObjectWithTag("Player").transform;

        Vector3 targetPos = player.position;

        Vector3 direction = (targetPos - spawnPos).normalized;

        Obstacle o = new Obstacle
        {
            position = spawnPos,
            rotation = Quaternion.identity,

            shapeType = ObstacleShapeType.Cylinder, 
            cylinderHeight = 2f,
            cylinderRadius = 1.5f,

            activeDuration = 5f,

            //Do not know what goes for here pls help 
            //movementType = ObstacleMovementType.Projectile,

           //velocity = direction * 20f,
            //useGravity = true,

           //visualPrefab = state.obstacleData.rockPrefab,
        };

        ObstacleManager.Instance.SpawnObstacle(o);
    }

}