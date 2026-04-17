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

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (bossBarContainer != null) bossBarContainer.SetActive(false);
        if (hudRoot != null) hudRoot.SetActive(false);
    }

    public void ShowHUD(bool show)
    {
        if (hudRoot != null) hudRoot.SetActive(show);
    }

    public void ShowBossBar(bool show)
    {
        if (bossBarContainer != null) bossBarContainer.SetActive(show);
    }

    public void SetBossName(string bossName)
    {
        if (bossNameText != null)
            bossNameText.text = bossName;
    }
}