using UnityEngine;
using TMPro;

// Place on the best-time panel in the menu. Hides itself if the player has never
// finished a run. Wire bestTimeText to a TMP_Text child in the Inspector.
public class BestTimeDisplay : MonoBehaviour
{
    private static BestTimeDisplay _instance;

    [Tooltip("TMP text element that shows the formatted best time")]
    public TMP_Text bestTimeText;

    private const string Key = "BestRunTime";

    void Awake()
    {
        _instance = this;
    }

    void Start()
    {
        UpdateDisplay();
    }

    // Called by HUDManager after a new best time is saved
    public static void Refresh()
    {
        if (_instance != null)
            _instance.UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        float stored = PlayerPrefs.GetFloat(Key, -1f);

        if (stored < 0f)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (bestTimeText != null)
            bestTimeText.text = $"Best Time\n{FormatTime(stored)}";
    }

    private static string FormatTime(float t)
    {
        int minutes    = (int)(t / 60f);
        int seconds    = (int)(t % 60f);
        int hundredths = (int)((t % 1f) * 100f);
        return $"{minutes:00}:{seconds:00}.{hundredths:00}";
    }
}
