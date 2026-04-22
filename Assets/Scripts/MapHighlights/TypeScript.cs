using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TypeScript : MonoBehaviour
{
    private TMP_Text tmpText;   // we will focus on TMP 
    private Text uiText;        // fallback if ever needed

    private Coroutine typingCoroutine;
    private string currentText = "";

    [Header("Typewriter Settings")]
    [SerializeField] private float delayBeforeStart = 0f;
    [SerializeField] private float timeBtwChars = 0.05f;
    [SerializeField] private float deleteSpeedMultiplier = 0.5f;

    [Header("Cursor (Optional)")]
    [SerializeField] private string cursor = "";
    [SerializeField] private bool showCursorBeforeDelay = false;

    void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        uiText = GetComponent<Text>();

        
        SetDisplayText("");
    }

    
    public void SetText(string newText)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        currentText = newText;

        SetDisplayText("");

        typingCoroutine = StartCoroutine(TypeRoutine());
    }

   
    public void ReplaceText(string newText)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(DeleteAndTypeRoutine(newText));
    }

    // -----------------------------
    // TYPE ROUTINE
    // -----------------------------
    private IEnumerator TypeRoutine()
    {
        if (showCursorBeforeDelay && cursor != "")
            SetDisplayText(cursor);

        yield return new WaitForSeconds(delayBeforeStart);

        string displayed = "";

        foreach (char c in currentText)
        {
            displayed += c;

            if (!string.IsNullOrEmpty(cursor))
                SetDisplayText(displayed + cursor);
            else
                SetDisplayText(displayed);

            yield return new WaitForSeconds(timeBtwChars);
        }

        SetDisplayText(displayed);
    }

    // -----------------------------
    // DELETE + TYPE ROUTINE
    // -----------------------------
    private IEnumerator DeleteAndTypeRoutine(string newText)
    {
        string current = GetCurrentDisplayedText();

        
        while (current.Length > 0)
        {
            current = current.Substring(0, current.Length - 1);
            SetDisplayText(current);
            yield return new WaitForSeconds(timeBtwChars * deleteSpeedMultiplier);
        }

        yield return new WaitForSeconds(0.2f);

        
        currentText = newText;
        yield return StartCoroutine(TypeRoutine());
    }

    // -----------------------------
    // HELPERS
    // -----------------------------
    private void SetDisplayText(string value)
    {
        if (tmpText != null) tmpText.text = value;
        if (uiText != null) uiText.text = value;
    }

    private string GetCurrentDisplayedText()
    {
        if (tmpText != null) return tmpText.text;
        if (uiText != null) return uiText.text;
        return "";
    }
}