using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss1PipeAttack : MonoBehaviour
{
    [Header("Pipe Positions (fill these in)")]
    [SerializeField] private Vector3 smallPipe1Position = new Vector3(-12.80793f, 2.794251f, 0.1768607f);
    [SerializeField] private Vector3 smallPipe2Position = new Vector3(-10.45649f, 0.6367635f, -0.7760992f);
    [SerializeField] private Vector3 smallPipe3Position = new Vector3(-10.45649f, 0.6367635f, -5.86663f);

    [Header("Pipe Directions")]
    [SerializeField] private Vector3 pipe1Direction = Vector3.forward;
    [SerializeField] private Vector3 pipe2Direction = Vector3.forward;
    [SerializeField] private Vector3 pipe3Direction = Vector3.forward;

    [Header("Bullet Settings")]
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private float bulletDamage = 1f;
    [SerializeField] private float bulletLifetime = 5f;

    [Header("Firing Settings")]
    [SerializeField] private float fireInterval = 3f; // time between attacks
    [SerializeField] private int bulletsPerPipe = 10;

    private float timer;

    private void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            StartCoroutine(FireAllPipes());
            timer = fireInterval;
        }
    }

    private IEnumerator FireAllPipes()
    {
        FirePipe(smallPipe1Position, pipe1Direction);
        FirePipe(smallPipe2Position, pipe2Direction);
        FirePipe(smallPipe3Position, pipe3Direction);

        yield return null;
    }

    private void FirePipe(Vector3 pipePos, Vector3 pipeDir)
    {
        for (int i = 0; i < bulletsPerPipe; i++)
        {
            Bullet b = new Bullet
            {
                position = pipePos,
                direction = pipeDir.normalized,
                speed = bulletSpeed + Random.Range(-1f, 1f),
                damage = bulletDamage,
                maxLifetime = bulletLifetime,
                collisionRadius = 0.3f,
                canBeParried = true,
                destroyOnParry = true,
                movementType = BulletMovementType.Straight,
                visualPrefab = null // assign your prefab here
            };

            BulletManager.Instance.SpawnBullet(b);
        }
    }
}