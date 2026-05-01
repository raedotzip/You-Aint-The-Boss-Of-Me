using UnityEngine;
using TMPro;

public class BossTimerUI : MonoBehaviour
{
    public TextMeshProUGUI timerText;

    private float time;

    private bool running = false;

    void Start()
    {
        StartTimer();
    }

    public void StartTimer()
    {
        time = 0f;
        running = true;
    }

    public void StopTimer()
    {
        running = false;
    }

    public void SetTime(float t)
    {
        time = t;
        UpdateDisplay();
    }

    void Update()
    {
        if (!running) return;

        time += Time.deltaTime;
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int ms = Mathf.FloorToInt((time * 100f) % 100);

        timerText.text = $"{minutes:00}:{seconds:00}.{ms:00}";
    }
}