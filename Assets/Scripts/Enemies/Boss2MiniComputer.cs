using UnityEngine;

public class Boss2MiniComputer : MonoBehaviour
{
    public float maxHealth = 50f;

    [Tooltip("Drag the Boss2StateManager from the main computer here")]
    public Boss2StateManager boss2;

    [Tooltip("Optional particle effect spawned at death position")]
    public GameObject destroyEffect;

    [Tooltip("Optional particle effect spawned when revived")]
    public GameObject reviveEffect;

    [Tooltip("How close the sword blade must be to count as a hit (meters)")]
    public float hitRadius = 0.4f;

    [Tooltip("Minimum sword speed to deal damage")]
    public float minSwordSpeed = 0.8f;

    private float _currentHealth;
    private bool  _dead = false;
    private Sword _sword;
    private float _hitCooldown = 0f;

    void Awake()
    {
        _currentHealth = maxHealth;
    }

    void Start()
    {
        _sword = FindObjectOfType<Sword>();

        if (boss2 != null)
            boss2.RegisterMiniComputer(this);

        var cols = GetComponentsInChildren<Collider>(true);
        Debug.Log($"[MiniComputer] '{gameObject.name}' start pos={transform.position:F2}  colliders={cols.Length}");
        foreach (var c in cols)
            Debug.Log($"  collider: {c.GetType().Name}  enabled={c.enabled}  trigger={c.isTrigger}  bounds={c.bounds.size:F2}");
    }

    void FixedUpdate()
    {
        if (_dead) return;
        if (_sword == null) return;
        if (_hitCooldown > 0f) { _hitCooldown -= Time.fixedDeltaTime; return; }

        Transform bladeBase = _sword.bladeBase;
        Transform bladeTip  = _sword.bladeTip;
        if (bladeBase == null || bladeTip == null) return;

        float dist = DistanceToSegment(transform.position, bladeBase.position, bladeTip.position);

        if (dist < hitRadius && _sword.Velocity.magnitude > minSwordSpeed)
        {
            float swingT     = Mathf.InverseLerp(_sword.minSwingDistance, _sword.maxSwingDistance, dist);
            float multiplier = Mathf.Lerp(_sword.minDamageMultiplier, _sword.maxDamageMultiplier, swingT);
            TakeDamage(_sword.damageAmount * multiplier);
            _hitCooldown = 0.3f;
        }
    }

    public void TakeDamage(float amount)
    {
        if (_dead) return;
        _currentHealth -= amount;
        Debug.Log($"[MiniComputer] '{gameObject.name}' hit for {amount:F1}, health={_currentHealth:F1}");
        if (_currentHealth <= 0f)
            Die();
    }

    public void Revive()
    {
        _currentHealth = maxHealth;
        _dead          = false;
        _hitCooldown   = 0f;
        gameObject.SetActive(true);

        if (reviveEffect != null)
            Instantiate(reviveEffect, transform.position, Quaternion.identity);
    }

    void Die()
    {
        _dead = true;

        if (destroyEffect != null)
            Instantiate(destroyEffect, transform.position, Quaternion.identity);

        if (boss2 != null)
            boss2.OnMiniComputerDestroyed(this);

        gameObject.SetActive(false);
    }

    static float DistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / ab.sqrMagnitude);
        return Vector3.Distance(point, a + t * ab);
    }
}
