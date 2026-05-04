using System.Collections;
using UnityEngine;
using Valve.VR;

// Single-scene architecture — no scene loads.
// Menu buttons send the player to the lab; arena triggers handle boss activation.
// Call ReturnToMenu() after all bosses are defeated.
public class MenuController : MonoBehaviour
{
    public static MenuController Instance;

    [Header("Menu Objects")]
    public GameObject   menuSphere;
    public GameObject[] menuBoxes;

    [Header("Spawn Points")]
    [Tooltip("Where the player stands inside the menu sphere")]
    public Transform menuSpawnPoint;
    [Tooltip("Where the player spawns in the lab")]
    public Transform labSpawnPoint;
    [Tooltip("Where the player spawns for Boss 1")]
    public Transform boss1SpawnPoint;
    [Tooltip("Where the player spawns for Boss 2")]
    public Transform boss2SpawnPoint;
    [Header("Player")]
    [Tooltip("Root GameObject that has the CharacterController")]
    public GameObject player;

    [Header("Fade")]
    [Tooltip("Duration in seconds for the fade-to-black and fade-in during teleport")]
    public float fadeDuration = 0.4f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        bool showMenu = BossManager.Instance == null || BossManager.Instance.StartingBoss == 0;

        SetMenuVisible(showMenu);

        if (showMenu)
        {
            TeleportPlayer(menuSpawnPoint);
            HUDManager.Instance?.ShowHUD(false);
        }
        else
        {
            TeleportPlayer(BossSpawnPoint(BossManager.Instance.StartingBoss));
            HUDManager.Instance?.ShowHUD(true);
        }
    }

    // Wire this to MenuBox.onSliced via the Inspector — sends the player to the lab
    // so they can walk to any boss arena. The bossIndex parameter is unused but kept
    // so existing Inspector UnityEvent wiring doesn't break.
    public void StartBoss(int bossIndex)
    {
        StartCoroutine(FadeAndGoToLab());
    }

    // Parameterless helpers used by the Editor setup tool for UnityEvent wiring
    public void StartBoss1() => StartBoss(1);
    public void StartBoss2() => StartBoss(2);

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Call this when a boss is defeated to bring the player back to the menu
    public void ReturnToMenu()
    {
        StartCoroutine(FadeAndReturnToMenu());
    }

    // Called after a boss is defeated — sends the player to the lab to walk to the
    // next arena, or returns to menu after the final boss.
    public void AdvanceToNextBoss(int completedBossIndex)
    {
        BossManager.Instance?.MarkBossDefeated(completedBossIndex);
        if (completedBossIndex < 2)
            BossManager.Instance?.SetActiveBoss(0);
        else
            StartCoroutine(DelayedReturnToMenu(3f));
    }

    // -----------------------------------------------
    IEnumerator DelayedReturnToMenu(float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToMenu();
    }

    IEnumerator FadeAndGoToLab()
    {
        SteamVR_Fade.View(Color.black, fadeDuration);
        yield return new WaitForSeconds(fadeDuration);

        SetMenuVisible(false);
        BossManager.Instance?.SetActiveBoss(0);
        TeleportPlayer(labSpawnPoint);
        HUDManager.Instance?.ShowHUD(true);

        SteamVR_Fade.View(Color.clear, fadeDuration);
    }

    IEnumerator FadeAndReturnToMenu()
    {
        SteamVR_Fade.View(Color.black, fadeDuration);
        yield return new WaitForSeconds(fadeDuration);

        foreach (var boss in FindObjectsOfType<EnemyStateManager>())
        {
            boss.StopBossMusic();
            boss.ResetBoss();
        }

        BossManager.Instance?.SetActiveBoss(0);
        BossManager.Instance?.ResetRun();          // clears defeated flags and resets arena triggers
        HUDManager.Instance?.ShowHUD(false);
        SetMenuVisible(true);
        TeleportPlayer(menuSpawnPoint);

        foreach (var box in menuBoxes)
            if (box != null) box.GetComponent<MenuBox>()?.ResetSlice();

        SteamVR_Fade.View(Color.clear, fadeDuration);
    }

    void SetMenuVisible(bool visible)
    {
        if (menuSphere != null) menuSphere.SetActive(visible);
        foreach (var box in menuBoxes)
            if (box != null) box.SetActive(visible);
    }

    Transform BossSpawnPoint(int index)
    {
        if (index == 1) return boss1SpawnPoint;
        if (index == 2) return boss2SpawnPoint;
        return menuSpawnPoint;
    }

    public void HandlePlayerDeath()
    {
        StartCoroutine(FadeAndRespawn());
    }

    IEnumerator FadeAndRespawn()
    {
        SteamVR_Fade.View(Color.black, fadeDuration);
        yield return new WaitForSeconds(fadeDuration);

        foreach (var boss in FindObjectsOfType<EnemyStateManager>())
        {
            boss.StopBossMusic();
            boss.ResetBoss();
        }

        BossManager.Instance?.SetActiveBoss(0);
        BossManager.Instance?.ResetRun();
        HUDManager.Instance?.PauseTimer();
        player.GetComponent<PlayerHealth>()?.Respawn();
        SetMenuVisible(true);
        TeleportPlayer(menuSpawnPoint);
        HUDManager.Instance?.ShowHUD(false);

        foreach (var box in menuBoxes)
            if (box != null) box.GetComponent<MenuBox>()?.ResetSlice();

        SteamVR_Fade.View(Color.clear, fadeDuration);
    }

    public void TeleportPlayer(Transform target)
    {
        if (player == null || target == null) return;

        var cc = player.GetComponent<CharacterController>();
        var movement = player.GetComponent<PlayerMovement>();

        if (cc != null) cc.enabled = false;
        if (movement != null) movement.SyncTeleport();

        player.transform.SetPositionAndRotation(target.position, target.rotation);
        Physics.SyncTransforms();

        if (cc != null) cc.enabled = true;
    }
}
