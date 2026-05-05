using UnityEngine;
using System.Collections;

public class TypeScript_Words : MonoBehaviour
{
    [SerializeField] private TypeScript writer;

    void Start()
    {
        writer.SetText("");
    }

    public void Type()
    {
        StartCoroutine(NextLine());
    }

    private IEnumerator NextLine()
    {
        yield return new WaitForSeconds(0.05f);
        writer.ReplaceText("Testing: Day 846, 7 AM \n> The pain response tests revealed surprising results... The experiment seemed to show signs of emotion. Specifically, rage. I'm lucky she's trapped behind that glass. Roe Jogan would put an end to her almost immediately, should she escape. We wouldn't want her going down the drains and getting to the mainframe. \nWelp, I'm off to lunch! I sure hope nothing happens while I'm gone! ");
    }
}