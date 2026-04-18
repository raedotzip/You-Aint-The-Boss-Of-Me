using UnityEditor;
using UnityEngine;

public static class AddMapColliders
{
    [MenuItem("Tools/Add MeshColliders to Selected")]
    static void AddToSelected()
    {
        int collidersAdded = 0;
        int meshesFixed = 0;

        foreach (GameObject root in Selection.gameObjects)
        {
            foreach (MeshRenderer mr in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                MeshFilter mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;

                // Ensure mesh is readable — required for non-convex MeshCollider
                string assetPath = AssetDatabase.GetAssetPath(mf.sharedMesh);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                    if (importer != null && !importer.isReadable)
                    {
                        importer.isReadable = true;
                        importer.SaveAndReimport();
                        meshesFixed++;
                    }
                }

                if (mr.GetComponent<Collider>() != null) continue;
                Undo.AddComponent<MeshCollider>(mr.gameObject);
                collidersAdded++;
            }
        }

        Debug.Log($"[AddMapColliders] Added {collidersAdded} MeshCollider(s), fixed {meshesFixed} mesh(es) to be readable.");
    }

    [MenuItem("Tools/Diagnose Map Colliders on Selected")]
    static void DiagnoseSelected()
    {
        foreach (GameObject root in Selection.gameObjects)
        {
            foreach (MeshRenderer mr in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                MeshFilter mf = mr.GetComponent<MeshFilter>();
                Collider col = mr.GetComponent<Collider>();
                bool readable = mf != null && mf.sharedMesh != null && mf.sharedMesh.isReadable;
                bool hasCollider = col != null;
                bool isTrigger = col != null && col.isTrigger;

                if (!hasCollider || !readable || isTrigger)
                    Debug.LogWarning($"[MapColliders] {mr.gameObject.name}: collider={hasCollider} readable={readable} isTrigger={isTrigger}", mr.gameObject);
            }
        }
        Debug.Log("[MapColliders] Diagnosis complete — warnings show problem objects.");
    }
}
