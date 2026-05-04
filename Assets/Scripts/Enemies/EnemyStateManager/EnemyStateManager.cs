using UnityEngine;

public abstract class EnemyStateManager : MonoBehaviour
{
    protected EnemyBaseState currentState;
    public Animator animator;
    public Rigidbody rb;
    public Transform player;
    public BossObstacleData obstacleData;
    public BossBulletData bulletData;

    private BossHitFlash _hitFlash;

    // Call from any boss TakeDamage — lazily finds the BossHitFlash component
    public void TriggerHitFlash(float damage)
    {
        if (_hitFlash == null) _hitFlash = GetComponentInChildren<BossHitFlash>(true);
        if (_hitFlash == null) _hitFlash = gameObject.AddComponent<BossHitFlash>();
        _hitFlash?.Flash(damage);
    }

    public virtual void Start()
    {
        animator = GetComponent<Animator>();
        rb       = GetComponent<Rigidbody>();

        if (player == null)
            player = GameObject.FindWithTag("Player").transform;

        currentState = this.currentState;
        currentState.EnterState(this);
    }

    public virtual void Update()
    {
        currentState.UpdateState(this);
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        BossHurt();
    }

    public virtual void BossHurt()
    {
        float damage = currentState.OnBossHurt(this);
    }

    public virtual void StopBossMusic() { }

    public void FireBullet(Vector3 direction, AttackData data)
    {
        Bullet b = new Bullet
        {
            position        = transform.position,
            direction       = direction.normalized,
            speed           = data.bulletSpeed,
            damage          = data.damage,
            maxLifetime     = data.lifetime,
            collisionRadius = data.collisionRadius,
            canBeParried    = data.canBeParried,
            destroyOnParry  = data.destroyOnParry,
            movementType    = data.movementType,
            visual          = data.bulletPrefab,
            visualPrefab    = data.bulletPrefab,
            attackData      = data,
        };

        BulletManager.Instance.SpawnBullet(b);
    }
}