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

    [Header("Look At Player")]
    [Tooltip("Drag the topRackSpin bone/transform here — it will rotate to face the player")]
    public Transform topRackSpin;
    [Tooltip("How quickly the rack turns to face the player (higher = snappier)")]
    public float lookAtSpeed = 3f;

    public float currentHealth;
    private bool       _dead = false;
    private Sword      _sword;
    private float      _hitCooldown = 0f;
    private GameObject _activeDestroyEffect;
    private Transform  _player;
    private boss2ScreenAnimator _screenAnimator;
    private Animator _animator;

    void Awake()
    {
        _currentHealth = maxHealth;
        _screenAnimator = GetComponent<boss2ScreenAnimator>();
        _animator = GetComponent<Animator>();
    }

    void Start()
    {
        _sword = FindObjectOfType<Sword>();

        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;

        if (boss2 != null)
            boss2.RegisterMiniComputer(this);

        var cols = GetComponentsInChildren<Collider>(true);
        Debug.Log($"[MiniComputer] '{gameObject.name}' start pos={transform.position:F2}  colliders={cols.Length}");
        foreach (var c in cols)
            Debug.Log($"  collider: {c.GetType().Name}  enabled={c.enabled}  trigger={c.isTrigger}  bounds={c.bounds.size:F2}");
    }

    void Update()
    {
        if (_dead || topRackSpin == null || _player == null) return;

        // Rotate only on the Y axis so the rack spins horizontally toward the player
        Vector3 toPlayer = _player.position - topRackSpin.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > 0.001f)
        {
            Quaternion target = Quaternion.LookRotation(toPlayer);
            topRackSpin.rotation = Quaternion.Slerp(topRackSpin.rotation, target, lookAtSpeed * Time.deltaTime);
        }
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
        {
            Die();
        }
        else
        {
            _animator.SetTrigger("Hurt");
            _screenAnimator.ShowHurtScreen(0.2f);
        }
    }

    public void Revive()
    {
        if (_activeDestroyEffect != null)
        {
            Destroy(_activeDestroyEffect);
            _activeDestroyEffect = null;
        }

        _currentHealth = maxHealth;
        _dead          = false;
        _hitCooldown   = 0f;

        if (reviveEffect != null)
            Instantiate(reviveEffect, transform.position, Quaternion.identity);
    }

    void Die()
    {
        _dead = true;

        _animator.SetBool("Destroyed", true);

        if (destroyEffect != null)
            _activeDestroyEffect = Instantiate(destroyEffect, transform.position, Quaternion.identity);

        if (boss2 != null)
            boss2.OnMiniComputerDestroyed(this);
    }

    static float DistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / ab.sqrMagnitude);
        return Vector3.Distance(point, a + t * ab);
    }
}
