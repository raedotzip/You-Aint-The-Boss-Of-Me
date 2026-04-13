using UnityEngine;
using System.Collections;

public class TypeScriptWords : MonoBehaviour
{
    [SerializeField] private TypeScript writer;

    void Start()
    {
        writer.SetText("Get him back.");

        StartCoroutine(NextLine());
    }

    private IEnumerator NextLine()
    {
        yield return new WaitForSeconds(2f);
        writer.ReplaceText("You have to.");
    }
}