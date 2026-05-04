using UnityEngine;
using TMPro;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;

    [Header("Player UI")]
    public HealthBarUI playerBar;

    [Header("Boss UI")]
    public HealthBarUI bossBar;
    public GameObject bossBarContainer;
    public TMP_Text bossNameText;

    [Header("HUD Root")]
    [Tooltip("Parent GameObject of the entire HUD — hidden while in the spawn/menu area")]
    public GameObject hudRoot;

    [Header("Run Timer")]
    [Tooltip("TMP text element at the top of the HUD showing elapsed run time")]
    public TMP_Text timerText;

    [Header("Lore")]
    public LoreTyper loreTyper;

    [Header("Boss Portrait")]
    public BossPortraitHUD bossPortrait;

    [Header("Testing")]
    [Tooltip("Keep the HUD visible on load — useful when testing directly in-scene without going through the menu flow")]
    public bool showHUDOnStart = false;

    private float _elapsed;
    private bool  _running;
    private bool  _finished;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (bossBarContainer != null) bossBarContainer.SetActive(false);
        if (hudRoot != null && !showHUDOnStart) hudRoot.SetActive(false);
    }

    void Start()
    {
        Debug.Log($"HUD: hudRoot={hudRoot}, playerBar={playerBar}, bossBar={bossBar}, bossBarContainer={bossBarContainer}");

        if (hudRoot != null && Camera.main != null)
        {
            var follow = hudRoot.GetComponent<VRHudFollow>() ?? hudRoot.AddComponent<VRHudFollow>();
            follow.Init(Camera.main.transform);
        }

        // Auto-wire the player health bar if not already assigned in the Inspector
        var playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            if (playerHealth.playerHealthBar == null)
                playerHealth.playerHealthBar = playerBar;
            playerBar?.UpdateHealthPercentage(playerHealth.currentHealth, playerHealth.maxHealth);
        }
    }

    void Update()
    {
        if (!_running || _finished) return;
        _elapsed += Time.deltaTime;
        UpdateTimerDisplay();
    }

    public void ShowHUD(bool show)
    {
        if (hudRoot == null) { Debug.LogError("[HUD] hudRoot is not assigned in the Inspector — HUD cannot show! Re-link it on the HUDManager GameObject.", this); return; }
        hudRoot.SetActive(show);
        Debug.Log($"[HUD] ShowHUD({show}) — hudRoot.activeSelf={hudRoot.activeSelf} activeInHierarchy={hudRoot.activeInHierarchy}", this);
        if (show && !_running && !_finished)
            StartTimer();
        if (!show)
        {
            _elapsed  = 0f;
            _running  = false;
            _finished = false;
            UpdateTimerDisplay();
        }
    }

    public void ShowLore(string message)
    {
        if (loreTyper != null) loreTyper.ShowLore(message);
    }

    public void ShowLoreSequence(string[] messages)
    {
        if (loreTyper != null) loreTyper.ShowLoreSequence(messages);
    }

    public void CancelLore()
    {
        if (loreTyper != null) loreTyper.Cancel();
    }

    public void ShowBossBar(bool show)
    {
        if (bossBarContainer != null) bossBarContainer.SetActive(show);
    }

    public void SetBossPortrait(int bossIndex)
    {
        bossPortrait?.SetActiveBoss(bossIndex);
    }

    public void SetBossName(string bossName)
    {
        if (bossNameText != null)
            bossNameText.text = bossName;
    }

    public void UpdateBossHealth(float current, float max)
    {
        bossBar?.UpdateHealthPercentage(current, max);
    }

    public void StartTimer()
    {
        _elapsed  = 0f;
        _running  = true;
        _finished = false;
        UpdateTimerDisplay();
    }

    public void StopTimer()
    {
        _running  = false;
        _finished = true;
        UpdateTimerDisplay();
        SaveBestTime();
    }

    public void PauseTimer()
    {
        _running = false;
        UpdateTimerDisplay();
    }

    private void SaveBestTime()
    {
        const string key = "BestRunTime";
        float stored = PlayerPrefs.GetFloat(key, -1f);
        if (stored < 0f || _elapsed < stored)
        {
            PlayerPrefs.SetFloat(key, _elapsed);
            PlayerPrefs.Save();
            BestTimeDisplay.Refresh();
        }
    }

    private void UpdateTimerDisplay()
    {
        if (timerText == null) return;
        int minutes  = (int)(_elapsed / 60f);
        int seconds  = (int)(_elapsed % 60f);
        int hundredths = (int)((_elapsed % 1f) * 100f);
        timerText.text = $"{minutes:00}:{seconds:00}.{hundredths:00}";
    }
}