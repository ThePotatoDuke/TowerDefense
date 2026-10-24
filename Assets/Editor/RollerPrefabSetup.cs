using UnityEditor;
using UnityEngine;

// One-shot setup that turns the duplicated SpikeBallRed in Prefabs/Enemies/Rollers into the Roller:
// swaps in the spike cylinder model, lines its axis up with RollerVisual's X, swaps the enemy script
// and creates its data asset. Runs by itself after a recompile while the prefab still has a
// SpikeBallEnemy, or from Tools > Setup Roller Prefab. Safe to delete once the prefab is set up.
[InitializeOnLoad]
public static class RollerPrefabSetup
{
    private const string PrefabPath = "Assets/Prefabs/Enemies/Rollers/Roller.prefab";
    private const string ModelPath = "Assets/3rd Party/Models/spike_cylinder.fbx";
    private const string DataFolder = "Assets/ScriptableObjects/Enemies/Rollers";
    private const string DataPath = DataFolder + "/RollerSO.asset";
    private const string MaterialPath = "Assets/3rd Party/Materials/SpikeCylinder_Mat_Black.mat";
    private const float Radius = 0.45f; // cylinder radius including the spikes

    static RollerPrefabSetup()
    {
        EditorApplication.delayCall += () =>
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) return;

            MeshRenderer model = prefab.GetComponentInChildren<MeshRenderer>();
            RollerDataSO data = AssetDatabase.LoadAssetAtPath<RollerDataSO>(DataPath);
            bool needsSetup = prefab.GetComponent<SpikeBallEnemy>() != null
                || data == null
                || !Mathf.Approximately(data.radius, Radius)
                || model == null
                || model.sharedMaterial != AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (needsSetup)
                Run();
        };
    }

    [MenuItem("Tools/Setup Roller Prefab")]
    public static void Run()
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null)
        {
            Debug.LogError($"Roller setup: no model at {ModelPath}");
            return;
        }

        RollerDataSO data = GetOrCreateData();
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            root.name = "Roller";

            Transform visual = root.transform.Find("SpikeBallVisual") ?? root.transform.Find("RollerVisual");
            if (visual == null)
            {
                visual = new GameObject("RollerVisual").transform;
                visual.SetParent(root.transform, false);
            }
            visual.name = "RollerVisual";
            visual.localRotation = Quaternion.identity;
            visual.localScale = Vector3.one;
            visual.localPosition = new Vector3(0f, Radius, 0f);

            for (int i = visual.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(visual.GetChild(i).gameObject);

            var cylinder = (GameObject)PrefabUtility.InstantiatePrefab(model, visual);
            cylinder.name = "SpikeCylinder";
            FitModel(cylinder.transform, visual);

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
            {
                foreach (MeshRenderer meshRenderer in cylinder.GetComponentsInChildren<MeshRenderer>())
                    meshRenderer.sharedMaterial = material;
            }
            else
            {
                Debug.LogWarning($"Roller setup: no material at {MaterialPath}");
            }

            if (root.TryGetComponent(out SpikeBallEnemy spikeBall))
                Object.DestroyImmediate(spikeBall);
            if (!root.TryGetComponent(out RollerEnemy roller))
                roller = root.AddComponent<RollerEnemy>();
            var serialized = new SerializedObject(roller);
            serialized.FindProperty("data").objectReferenceValue = data;
            serialized.FindProperty("rollerVisual").objectReferenceValue = visual;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (root.TryGetComponent(out SphereCollider sphere))
            {
                sphere.isTrigger = true;
                sphere.center = new Vector3(0f, Radius, 0f);
                sphere.radius = Radius;
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("Roller setup: prefab updated. Check that the cylinder rolls forward, not sideways.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // Rotate the cylinder's axis onto the parent's X, scale it to Radius and center it on the pivot
    private static void FitModel(Transform model, Transform parent)
    {
        model.localPosition = Vector3.zero;
        model.localRotation = Quaternion.identity;
        model.localScale = Vector3.one;

        Bounds bounds = GetLocalBounds(model, parent);
        Vector3 size = bounds.size;

        // The two radial dimensions match; the axis is the one that differs most from the others
        float dx = Mathf.Abs(size.x - size.y) + Mathf.Abs(size.x - size.z);
        float dy = Mathf.Abs(size.y - size.x) + Mathf.Abs(size.y - size.z);
        float dz = Mathf.Abs(size.z - size.x) + Mathf.Abs(size.z - size.y);
        Vector3 axis = dx >= dy && dx >= dz ? Vector3.right : dy >= dz ? Vector3.up : Vector3.forward;
        model.localRotation = Quaternion.FromToRotation(axis, Vector3.right);

        bounds = GetLocalBounds(model, parent);
        float radial = Mathf.Max(bounds.extents.y, bounds.extents.z);
        model.localScale = Vector3.one * (Radius / Mathf.Max(0.0001f, radial));

        bounds = GetLocalBounds(model, parent);
        model.localPosition -= bounds.center;
    }

    private static Bounds GetLocalBounds(Transform model, Transform space)
    {
        var bounds = new Bounds();
        bool first = true;
        foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.sharedMesh == null) continue;
            Bounds mesh = filter.sharedMesh.bounds;
            Matrix4x4 toSpace = space.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = mesh.center + Vector3.Scale(mesh.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 point = toSpace.MultiplyPoint3x4(corner);
                if (first) { bounds = new Bounds(point, Vector3.zero); first = false; }
                else bounds.Encapsulate(point);
            }
        }
        return bounds;
    }

    private static RollerDataSO GetOrCreateData()
    {
        RollerDataSO data = AssetDatabase.LoadAssetAtPath<RollerDataSO>(DataPath);
        if (data != null)
        {
            data.radius = Radius; // keep spin speed and bounce distance in step with the model size
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            return data;
        }

        if (!AssetDatabase.IsValidFolder(DataFolder))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects/Enemies", "Rollers");

        data = ScriptableObject.CreateInstance<RollerDataSO>();
        data.enemyName = "Roller";
        data.maxHealth = 150f;
        data.contactDamage = 2f;
        data.radius = Radius;
        AssetDatabase.CreateAsset(data, DataPath);
        AssetDatabase.SaveAssets();
        return data;
    }
}
