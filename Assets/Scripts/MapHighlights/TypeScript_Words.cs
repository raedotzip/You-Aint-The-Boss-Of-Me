using UnityEngine;
using System.Collections;

public class TypeScript_Words : MonoBehaviour
{
    [SerializeField] private TypeScript writer;

    void Start()
    {
        writer.SetText("Good Morning!");
        StartCoroutine(NextLine());
    }

    private IEnumerator NextLine()
    {
        yield return new WaitForSeconds(2f);
        writer.ReplaceText("To begine your battle please go through the pipe. Good Luck!");
    }
}