using UnityEditor;
using UnityEditor.Events;
using UnityEngine;

public static class MenuSphereSetup
{
    [MenuItem("Tools/Add Menu Sphere to Scene")]
    static void AddMenuSphere()
    {
        // -----------------------------------------------
        // Sphere wall material
        // -----------------------------------------------
        string shaderPath   = "Assets/Shader/MenuSphereWall.shader";
        string materialPath = "Assets/Shader/MenuSphereWall.mat";

        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
        if (shader == null)
        {
            Debug.LogError("[MenuSphereSetup] Could not find MenuSphereWall.shader at " + shaderPath);
            return;
        }

        Material wallMat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (wallMat == null)
        {
            wallMat = new Material(shader);
            wallMat.SetColor("_GridColor",    new Color(0.00f, 0.85f, 0.90f, 1f));
            wallMat.SetColor("_BgColor",      new Color(0.01f, 0.01f, 0.07f, 1f));
            wallMat.SetColor("_GlowColor",    new Color(0.00f, 0.40f, 1.00f, 1f));
            wallMat.SetFloat("_GridScale",    22f);
            wallMat.SetFloat("_LineWidth",    0.028f);
            wallMat.SetFloat("_GlowStrength", 1.6f);
            wallMat.SetFloat("_PulseSpeed",   0.45f);
            wallMat.SetFloat("_PulseWidth",   0.07f);
            AssetDatabase.CreateAsset(wallMat, materialPath);
            AssetDatabase.SaveAssets();
        }

        // -----------------------------------------------
        // Root sphere
        // -----------------------------------------------
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "MenuSphere";
        sphere.transform.localScale = new Vector3(20f, 20f, 20f);
        sphere.transform.position   = Vector3.zero;
        Object.DestroyImmediate(sphere.GetComponent<SphereCollider>());
        sphere.GetComponent<MeshRenderer>().sharedMaterial = wallMat;

        // -----------------------------------------------
        // Floor material
        // -----------------------------------------------
        string floorMatPath = "Assets/Shader/MenuFloor.mat";
        Material floorMat = AssetDatabase.LoadAssetAtPath<Material>(floorMatPath);
        if (floorMat == null)
        {
            floorMat = new Material(Shader.Find("Standard"));
            floorMat.SetFloat("_Mode", 3);
            floorMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            floorMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            floorMat.SetInt("_ZWrite", 0);
            floorMat.DisableKeyword("_ALPHATEST_ON");
            floorMat.EnableKeyword("_ALPHABLEND_ON");
            floorMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            floorMat.renderQueue = 3000;
            floorMat.color = new Color(0.00f, 0.75f, 0.85f, 0.18f);
            floorMat.SetColor("_EmissionColor", new Color(0.00f, 0.25f, 0.35f));
            floorMat.EnableKeyword("_EMISSION");
            AssetDatabase.CreateAsset(floorMat, floorMatPath);
            AssetDatabase.SaveAssets();
        }

        // Floor — Plane primitive is 10×10, local scale 0.09 inside a ×20 sphere = 18 unit diameter
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "MenuFloor";
        floor.transform.SetParent(sphere.transform, worldPositionStays: false);
        floor.transform.localPosition = Vector3.zero;
        floor.transform.localScale    = new Vector3(0.09f, 1f, 0.09f);
        floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat;

        // -----------------------------------------------
        // Box material
        // -----------------------------------------------
        string boxMatPath = "Assets/Shader/MenuBox.mat";
        Material boxMat = AssetDatabase.LoadAssetAtPath<Material>(boxMatPath);
        if (boxMat == null)
        {
            boxMat = new Material(Shader.Find("Standard"));
            boxMat.color = new Color(0.04f, 0.04f, 0.14f, 1f);
            boxMat.SetColor("_EmissionColor", new Color(0.00f, 0.20f, 0.30f));
            boxMat.EnableKeyword("_EMISSION");
            AssetDatabase.CreateAsset(boxMat, boxMatPath);
            AssetDatabase.SaveAssets();
        }

        // -----------------------------------------------
        // MenuController (create if not already in scene)
        // -----------------------------------------------
        MenuController mc = Object.FindObjectOfType<MenuController>();
        if (mc == null)
        {
            GameObject mcGo = new GameObject("MenuController");
            mc = mcGo.AddComponent<MenuController>();
        }
        mc.menuSphere = sphere;

        // -----------------------------------------------
        // Layer — warns if "Menu" layer hasn't been created yet
        // -----------------------------------------------
        int menuLayer = LayerMask.NameToLayer("Menu");
        if (menuLayer < 0)
        {
            Debug.LogWarning("[MenuSphereSetup] No layer named 'Menu' found. " +
                "Go to Edit > Project Settings > Tags and Layers, add 'Menu', " +
                "then re-run this tool or manually set the layer on the two box GameObjects.");
            menuLayer = 0;
        }

        // -----------------------------------------------
        // Create boxes
        // -----------------------------------------------
        GameObject startBox = CreateBox("MenuBox_StartGame", "START GAME",
            new Vector3(-1.1f, 1.5f, 2.5f), menuLayer, boxMat, sphere.transform);

        GameObject exitBox  = CreateBox("MenuBox_ExitGame",  "EXIT GAME",
            new Vector3( 1.1f, 1.5f, 2.5f), menuLayer, boxMat, sphere.transform);

        // -----------------------------------------------
        // Wire UnityEvents
        // -----------------------------------------------
        MenuBox startMB = startBox.GetComponent<MenuBox>();
        MenuBox exitMB  = exitBox .GetComponent<MenuBox>();

        UnityEventTools.AddVoidPersistentListener(startMB.onSliced, mc.StartBoss1);
        UnityEventTools.AddVoidPersistentListener(exitMB .onSliced, mc.QuitGame);

        // Register both boxes in the controller
        mc.menuBoxes = new GameObject[] { startBox, exitBox };

        // -----------------------------------------------
        // Finish
        // -----------------------------------------------
        Undo.RegisterCreatedObjectUndo(sphere, "Add Menu Sphere");
        Selection.activeGameObject = sphere;

        Debug.Log("[MenuSphereSetup] Done. Assign MenuController's spawn point fields and the Player reference in the Inspector.");
    }

    // -----------------------------------------------
    // Helper
    // -----------------------------------------------
    static GameObject CreateBox(string name, string label, Vector3 worldPos,
                                int layer, Material mat, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.layer = layer;
        go.transform.SetParent(parent, worldPositionStays: false);

        // Box is parented to the ×20 sphere — convert world position to local
        go.transform.localPosition = worldPos / 20f;
        go.transform.localScale    = new Vector3(0.035f, 0.0125f, 0.0025f); // 0.7 × 0.25 × 0.05 world

        go.GetComponent<MeshRenderer>().sharedMaterial = mat;

        // MenuBox auto-adds a kinematic Rigidbody via RequireComponent
        MenuBox mb  = go.AddComponent<MenuBox>();
        mb.label    = label;

        return go;
    }
}
