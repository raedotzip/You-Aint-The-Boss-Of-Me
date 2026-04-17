// Add this at the top
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Sword : MonoBehaviour
{
    [Header("Blade Points (Used For Melee Only)")]
    public Transform bladeBase;
    public Transform bladeTip;

    [Header("Melee Detection")]
    public float sphereRadius = 0.025f;
    public LayerMask meleeParryLayer;
    public LayerMask menuLayer;
    public float hitCooldown = 0.2f;
    public float damageAmount = 10f; // <-- Damage to boss

    [Header("Swing")]
    public float swingStartThreshold = 1.5f;
    public float swingStopThreshold = 0.4f;
    public float perfectWindow = 0.15f;

    [Header("Swing Damage Scaling")]
    [Tooltip("Tip travel distance (meters) for minimum damage. Wrist flicks will be at or below this.")]
    public float minSwingDistance = 0.08f;
    [Tooltip("Tip travel distance (meters) for maximum damage. A full wide swing reaches this.")]
    public float maxSwingDistance = 0.55f;
    [Tooltip("Damage multiplier at minSwingDistance (flick).")]
    public float minDamageMultiplier = 0.25f;
    [Tooltip("Damage multiplier at maxSwingDistance (full swing).")]
    public float maxDamageMultiplier = 1.5f;

    [Header("Life Steal")]
    [Tooltip("Tip travel distance required before any healing occurs. Must be a wide swing.")]
    public float healSwingThreshold = 0.35f;
    [Tooltip("How much health is restored on a qualifying wide swing hit.")]
    public float healAmount = 5f;
    public PlayerHealth playerHealth;

    [Header("Bullet Parry")]
    public float parryAngle = 70f;
    public float speedMultiplier = 1.5f;

    [Header("Hit Stop")]
    public float hitStopDuration = 0.05f;
    public float hitStopScale = 0.85f;

    public Vector3 Velocity { get; private set; }

    private Vector3 lastSwordPos;
    private Vector3 lastBasePos;
    private Vector3 lastTipPos;

    private float swingStartTime;
    private bool isSwinging;
    private float swingTipDistance = 0f; // arc distance tip has travelled this swing

    private Renderer swordRenderer;
    private Vector3 halfExtents;
    private float boundingSphereRadius;

    private Dictionary<Rigidbody, float> cooldowns = new Dictionary<Rigidbody, float>();

    void Awake()
    {
        swordRenderer = GetComponentInChildren<Renderer>(true);

        if (swordRenderer == null)
        {
            Debug.LogError($"[Sword] No MeshRenderer found in children of {gameObject.name}!");
        }
    }

    void Start()
    {
        lastSwordPos = transform.position;

        if (bladeBase != null) lastBasePos = bladeBase.position;
        if (bladeTip != null) lastTipPos = bladeTip.position;

        if (swordRenderer != null)
        {
            Bounds b = swordRenderer.bounds;
            halfExtents = b.extents;
            boundingSphereRadius = halfExtents.magnitude;
        }
    }

    void Update()
    {
        Velocity = (transform.position - lastSwordPos) / Time.deltaTime;
        lastSwordPos = transform.position;

        float speed = Velocity.magnitude;

        if (!isSwinging && speed > swingStartThreshold)
        {
            isSwinging = true;
            swingStartTime = Time.time;
            swingTipDistance = 0f;
        }

        if (isSwinging)
        {
            if (bladeTip != null)
                swingTipDistance += Vector3.Distance(bladeTip.position, lastTipPos);

            if (speed < swingStopThreshold)
            {
                isSwinging = false;
                swingTipDistance = 0f;
            }
        }
    }

    void FixedUpdate()
    {
        SweepBladeMelee();

        // Always check for bullet contact — sword doesn't need to be swinging to parry
        ParryBullets();

        if (bladeBase != null) lastBasePos = bladeBase.position;
        if (bladeTip != null) lastTipPos = bladeTip.position;
    }

    // =========================================================
    // MELEE (Physics-Based)
    // =========================================================
    void SweepBladeMelee()
    {
        if (bladeBase == null || bladeTip == null) return;
        
        for (int i = 0; i <= 4; i++)
        {
            float t = i / 4f;

            Vector3 current = Vector3.Lerp(bladeBase.position, bladeTip.position, t);
            Vector3 previous = Vector3.Lerp(lastBasePos, lastTipPos, t);

            Vector3 movement = current - previous;
            float distance = movement.magnitude;
            if (distance <= 0f) continue;

            RaycastHit[] hits = Physics.SphereCastAll(
                previous,
                sphereRadius,
                movement.normalized,
                distance,
                meleeParryLayer | menuLayer,
                QueryTriggerInteraction.Ignore
            );

            foreach (var hit in hits)
            {
                Rigidbody rb = hit.rigidbody;
                if (rb == null) continue;
                if (!CanHit(rb)) continue;

                cooldowns[rb] = Time.time;

                // Menu box slice — select the option and skip boss/physics logic
                MenuBox menuBox = hit.collider.GetComponentInParent<MenuBox>();
                if (menuBox != null)
                {
                    menuBox.OnSliced();
                    continue;
                }

                // Boss damage routed through BossManager (works for any boss)
                EnemyStateManager hitBoss = hit.collider.GetComponentInParent<EnemyStateManager>();
                if (hitBoss != null && BossManager.Instance != null)
                {
                    float swingT     = Mathf.InverseLerp(minSwingDistance, maxSwingDistance, swingTipDistance);
                    float multiplier = Mathf.Lerp(minDamageMultiplier, maxDamageMultiplier, swingT);
                    BossManager.Instance.TakeDamageOnActive(damageAmount * multiplier);

                    if (swingTipDistance >= healSwingThreshold && playerHealth != null)
                        playerHealth.Heal(healAmount);
                }

                // Physics reflect — skip kinematic Rigidbodies (menu boxes, static props)
                if (!rb.isKinematic)
                {
                    Vector3 reflectDir = Vector3.Reflect(rb.velocity, hit.normal);
                    rb.velocity = reflectDir;
                }

                StartCoroutine(HitStop());
            }
        }
    }

    bool CanHit(Rigidbody rb)
    {
        if (cooldowns.TryGetValue(rb, out float lastHitTime))
        {
            if (Time.time - lastHitTime < hitCooldown)
                return false;
        }
        return true;
    }

    // =========================================================
    // BULLETS (Optimized Spatial Grid + OBB)
    // =========================================================
    void ParryBullets()
    {
        if (BulletManager.Instance == null) return;
        if (swordRenderer == null) return;

        bool isPerfect = Time.time - swingStartTime <= perfectWindow;

        Vector3 swordCenter = swordRenderer.bounds.center;

        BulletManager.Instance.TryParryBullets(
            swordCenter,
            transform,
            halfExtents,
            boundingSphereRadius,
            Velocity,
            isPerfect ? parryAngle : parryAngle * 0.7f,
            speedMultiplier
        );
    }

    // =========================================================
    // HIT STOP
    // =========================================================
    IEnumerator HitStop()
    {
        float original = Time.timeScale;
        Time.timeScale = hitStopScale;

        yield return new WaitForSecondsRealtime(hitStopDuration);

        Time.timeScale = original;
    }
}