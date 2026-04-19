using System.Collections;
using UnityEngine;
using TMPro;

// Attach to a GameObject that has (or is assigned) a TMP_Text element on the player's HUD.
// Call ShowLore(message) for a single line, or ShowLoreSequence(messages[]) for a chain.
// Each message types in character by character, holds, then deletes character by character.
public class LoreTyper : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text displayText;

    [Header("Timing")]
    [SerializeField] private float typeSpeed    = 0.05f;   // seconds per character typed
    [SerializeField] private float deleteSpeed  = 0.025f;  // seconds per character deleted (faster than typing)
    [SerializeField] private float holdDuration = 3f;      // how long full text stays before deleting
    [SerializeField] private float gapBetween   = 0.4f;    // silence between messages in a sequence

    [Header("Cursor")]
    [SerializeField] private string cursor      = "_";     // blinking cursor appended while typing
    [SerializeField] private float  cursorBlink = 0.5f;    // blink interval while holding

    private Coroutine _routine;
    private bool      _cursorVisible;

    public bool IsPlaying => _routine != null;

    // -------------------------------------------------------
    // Public API
    // -------------------------------------------------------

    public void ShowLore(string message)
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(TypeAndDeleteRoutine(message));
    }

    public void ShowLoreSequence(string[] messages)
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(SequenceRoutine(messages));
    }

    public void Cancel()
    {
        if (_routine != null) { StopCoroutine(_routine); _routine = null; }
        SetText("");
    }

    // -------------------------------------------------------
    // Core routines
    // -------------------------------------------------------

    private IEnumerator TypeAndDeleteRoutine(string message)
    {
        string built = "";

        // Type in
        foreach (char c in message)
        {
            built += c;
            SetText(built + cursor);
            yield return new WaitForSeconds(typeSpeed);
        }

        // Blink cursor while holding
        float held = 0f;
        _cursorVisible = true;
        while (held < holdDuration)
        {
            SetText(built + (_cursorVisible ? cursor : ""));
            _cursorVisible = !_cursorVisible;
            float step = Mathf.Min(cursorBlink, holdDuration - held);
            yield return new WaitForSeconds(step);
            held += step;
        }
        SetText(built);

        // Delete
        while (built.Length > 0)
        {
            built = built.Substring(0, built.Length - 1);
            SetText(built.Length > 0 ? built + cursor : "");
            yield return new WaitForSeconds(deleteSpeed);
        }

        SetText("");
        _routine = null;
    }

    private IEnumerator SequenceRoutine(string[] messages)
    {
        foreach (string msg in messages)
        {
            yield return StartCoroutine(TypeAndDeleteRoutine(msg));
            if (gapBetween > 0f)
                yield return new WaitForSeconds(gapBetween);
        }
        _routine = null;
    }

    // -------------------------------------------------------
    // Helper
    // -------------------------------------------------------

    private void SetText(string value)
    {
        if (displayText != null) displayText.text = value;
    }
}
