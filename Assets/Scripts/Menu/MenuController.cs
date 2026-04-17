using UnityEngine;

// Single-scene architecture — no scene loads.
// Call StartBoss(1) / StartBoss(2) from MenuBox.onSliced to teleport the player
// and activate the right boss.  Call ReturnToMenu() after a boss is defeated.
public class MenuController : MonoBehaviour
{
    public static MenuController Instance;

    [Header("Menu Objects")]
    public GameObject   menuSphere;
    public GameObject[] menuBoxes;

    [Header("Spawn Points")]
    [Tooltip("Where the player stands inside the menu sphere")]
    public Transform menuSpawnPoint;
    [Tooltip("Where the player spawns for Boss 1")]
    public Transform boss1SpawnPoint;
    [Tooltip("Where the player spawns for Boss 2")]
    public Transform boss2SpawnPoint;

    [Header("Player")]
    [Tooltip("Root GameObject that has the CharacterController")]
    public GameObject player;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        bool showMenu = BossManager.Instance == null || BossManager.Instance.StartingBoss == 0;

        SetMenuVisible(showMenu);

        if (showMenu)
            TeleportPlayer(menuSpawnPoint);
        else
            TeleportPlayer(BossSpawnPoint(BossManager.Instance.StartingBoss));
    }

    // Wire this to MenuBox.onSliced via the Inspector (use int parameter 1 or 2)
    public void StartBoss(int bossIndex)
    {
        SetMenuVisible(false);
        TeleportPlayer(BossSpawnPoint(bossIndex));
        BossManager.Instance?.SetActiveBoss(bossIndex);
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
        BossManager.Instance?.SetActiveBoss(0);
        SetMenuVisible(true);
        TeleportPlayer(menuSpawnPoint);

        // Re-enable slicing on all boxes
        foreach (var box in menuBoxes)
            if (box != null) box.GetComponent<MenuBox>()?.ResetSlice();
    }

    // -----------------------------------------------
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

    void TeleportPlayer(Transform target)
    {
        if (player == null || target == null) return;
    
        var cc = player.GetComponent<CharacterController>();
        var movement = player.GetComponent<PlayerMovement>();
    
        // 1. Disable CC to allow manual position override
        if (cc != null) cc.enabled = false;
    
        // 2. Reset movement variables (Gravity/Dash)
        if (movement != null) movement.SyncTeleport();
    
        // 3. Move the player to the target
        player.transform.SetPositionAndRotation(target.position, target.rotation);
    
        // 4. IMPORTANT: Force physics engine to see the new position BEFORE enabling CC
        Physics.SyncTransforms();
    
        // 5. Re-enable CC
        if (cc != null) cc.enabled = true;
    }
}
