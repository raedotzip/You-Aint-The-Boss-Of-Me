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

    void Start()
    {
        rend = GetComponentInChildren<Renderer>();
        Material[] materials = rend.sharedMaterials;
        foreach (Material mat in materials)
        {
            Debug.Log(mat.name);
        }
        targetMat = materials[0];
        targetMat.SetTextureOffset("_Albedo", new Vector2(0, 0));
    }

    void Update()
    {
        int totalFrames = columns * (rows - 1);
        frame = (int)(Time.time * fps) % totalFrames;

        int col = frame / (rows - 1);
        int row = frame % (rows - 1);

        Vector2 offset = new Vector2(col / (float)columns, 1f - (row + 1) / (float)(rows-1));

        targetMat.SetTextureOffset("_Albedo", offset);
    }
}
