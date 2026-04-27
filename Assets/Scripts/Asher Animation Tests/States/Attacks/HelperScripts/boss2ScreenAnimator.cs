using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class boss2ScreenAnimator : MonoBehaviour
{
    public int columns = 3;
    public int rows = 5;
    public float fps = 10f;

    private Renderer rend;
    private int frame;
    private Material targetMat;
    private float t;
    private float xOffsetUnit;
    private float yOffsetUnit;
    private int col;
    private int row;
    Coroutine cycleRoutine;
    private bool isHurt = false;

    void Start()
    {
        xOffsetUnit = 1f / columns;
        yOffsetUnit = 1f / rows;
        rend = GetComponentInChildren<Renderer>();
        Material[] materials = rend.sharedMaterials;
        targetMat = materials[0];
        Debug.Log(targetMat);
        targetMat.SetTextureOffset("_Albedo", new Vector2(0, 0));
        col = 0;
        Debug.Log("ON");
        cycleRoutine = StartCoroutine(CycleScreens());
    }

    void Update()
    {
        Vector2 offset = new Vector2(-col * xOffsetUnit, -row * yOffsetUnit);
        targetMat.SetTextureOffset("_Albedo", offset);
    }

    IEnumerator CycleScreens()
    {
        while (true)
        {
            if (!isHurt)
            {
                NextScreen();
            }
            yield return new WaitForSeconds(Random.Range(0.05f, 1f));
        }
    }

    void NextScreen() {
        //col = boss2.phase;
        Debug.Log("GO");
        row = (int)Random.Range(0, 4);
        //Debug.Log($"{col}, {row}");
    }

    public void ShowHurtScreen(float duration)
    {
        if (isHurt) return;

        isHurt = true;

        StartCoroutine(ShowHurtScreenRoutine(duration));
    }

    IEnumerator ShowHurtScreenRoutine(float duration)
    {
        //col = boss2.phase;
        row = 4;
        Debug.Log("IM HURT");
        yield return new WaitForSeconds(duration);
        isHurt = false;
    }
}
