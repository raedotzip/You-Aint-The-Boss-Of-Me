using UnityEngine;
/**
 *
 * Singleton that controls which boss fight is currently active.
 * Add this component to a persistent GameObject in your scene.
 * Wire up boss references in the Inspector, then call SetActiveBoss() from
 * your arena trigger, cutscene, or game flow script.
 * @example
 * ```cs
 * // Start Boss 1 fight
 *   BossManager.Instance.SetActiveBoss(1);
 *
 * // After Boss 1 dies, transition to Boss 2
 * BossManager.Instance.SetActiveBoss(2);
 *
 * // Clear all bosses (between arenas, cutscene, etc.)
 * BossManager.Instance.SetActiveBoss(0);
 * ```
 */
public class BossManager : MonoBehaviour
{
    public static BossManager Instance;

    [Header("Boss References")]
    public Boss1StateManager boss1;
    public Boss2StateManager boss2;
    public Boss3StateManager boss3;

    [Header("Starting Boss (0 = none, 1–3 = boss index)")]
    public int startingBoss = 0;

    [Header("Boss Names")]
    public string boss1Name = "Roe Jogan";
    public string boss2Name = "The Mainframe";
    public string boss3Name = "The Overseer";

    [Header("HUD")]
    [Tooltip("Show the health bar immediately when Boss 1 becomes active")]
    public bool boss1ShowsBarImmediately = true;
    [Tooltip("Boss 2's bar is hidden until mini computers are destroyed — leave false")]
    public bool boss2ShowsBarImmediately = false;

    private int _activeBossIndex = 0;

    public int StartingBoss => startingBoss;

    // ===============================
    // UNITY
    // ===============================
    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SetBossActive(boss1, false);
        SetBossActive(boss2, false);
        SetBossActive(boss3, false);

        if (HUDManager.Instance != null)
            HUDManager.Instance.ShowBossBar(false);

        if (startingBoss > 0)
            SetActiveBoss(startingBoss);
    }

    // ===============================
    // PUBLIC API
    // ===============================

    // Call this to transition to a new boss fight.
    // bossIndex: 1 = Boss1, 2 = Boss2, 0 = none
    public void SetActiveBoss(int bossIndex)
    {
        // Shut down the current boss and hide the bar
        if (_activeBossIndex == 1 && boss1 != null) SetBossActive(boss1, false);
        if (_activeBossIndex == 2 && boss2 != null) SetBossActive(boss2, false);
        if (_activeBossIndex == 3 && boss3 != null) SetBossActive(boss3, false);

        if (HUDManager.Instance != null)
            HUDManager.Instance.ShowBossBar(false);

        _activeBossIndex = bossIndex;

        if (bossIndex > 0 && HUDManager.Instance != null)
            HUDManager.Instance.ShowHUD(true);

        if (bossIndex == 1 && boss1 != null)
        {
            if (HUDManager.Instance != null)
            {
                boss1.bossHealthBar = HUDManager.Instance.bossBar;
                boss1.bossHealthBar?.UpdateHealthPercentage(boss1.health, boss1.maxHealth);
                HUDManager.Instance.SetBossName(boss1Name);
            }

            SetBossActive(boss1, true);

            if (boss1ShowsBarImmediately && HUDManager.Instance != null)
                HUDManager.Instance.ShowBossBar(true);
        }

        if (bossIndex == 2 && boss2 != null)
        {
            if (HUDManager.Instance != null)
            {
                boss2.bossHealthBar = HUDManager.Instance.bossBar;
                HUDManager.Instance.SetBossName(boss2Name);
            }

            SetBossActive(boss2, true);

            if (boss2ShowsBarImmediately && HUDManager.Instance != null)
                HUDManager.Instance.ShowBossBar(true);
        }

        if (bossIndex == 3 && boss3 != null)
        {
            if (HUDManager.Instance != null)
            {
                boss3.bossHealthBar = HUDManager.Instance.bossBar;
                boss3.bossHealthBar?.UpdateHealthPercentage(boss3.health, boss3.maxHealth);
                HUDManager.Instance.SetBossName(boss3Name);
            }

            SetBossActive(boss3, true);
            HUDManager.Instance?.ShowBossBar(true);
        }
    }

    // Returns the active boss — used by BulletManager for parry damage targeting
    public EnemyStateManager GetActiveBoss()
    {
        if (_activeBossIndex == 1) return boss1;
        if (_activeBossIndex == 2) return boss2;
        if (_activeBossIndex == 3) return boss3;
        return null;
    }

    // Routes parried bullet damage to whichever boss is active
    public void TakeDamageOnActive(float amount)
    {
        if (_activeBossIndex == 1 && boss1 != null) boss1.TakeDamage(amount);
        if (_activeBossIndex == 2 && boss2 != null) boss2.TakeDamage(amount);
        if (_activeBossIndex == 3 && boss3 != null) boss3.TakeDamage(amount);
    }

    // ===============================
    // PRIVATE
    // ===============================
    private void SetBossActive(EnemyStateManager boss, bool active)
    {
        if (boss == null) return;
        boss.enabled = active;
    }
}
