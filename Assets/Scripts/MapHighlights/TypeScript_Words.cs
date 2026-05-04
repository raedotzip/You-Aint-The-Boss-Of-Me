using UnityEngine;
using System.Collections;

public class TypeScript_Words : MonoBehaviour
{
    [SerializeField] private TypeScript writer;

    void Start()
    {
        writer.SetText("");
        StartCoroutine(NextLine());
    }

    private IEnumerator NextLine()
    {
        yield return new WaitForSeconds(2f);
        writer.ReplaceText("Seems like you finally woke up. Follow the line on the floor and DEFEAT THEM ALL!!!");
    }
}