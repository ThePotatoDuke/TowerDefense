using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Set Up Selected Ground: for an extra piece of ground (e.g. the patch under the trees at the path's
// start), with it selected. Gives it the same depth as the main ground (sides hanging down from its edges,
// like GroundSides does for the main ground) and lays the texture out in world units on it and on the main
// ground (WorldTiling), so both show the texture at the same size and line up where they meet.
public static class ExtraGroundSetup
{
    private const string MainGroundName = "Ground";
    private const string MainSidesName = "GroundSides";

    [MenuItem("Tools/Set Up Selected Ground")]
    private static void Run()
    {
        GameObject selected = Selection.activeGameObject;
        Renderer ground = selected != null ? selected.GetComponent<MeshRenderer>() : null;
        if (ground == null)
        {
            Debug.LogWarning("Extra ground: select the ground object (one with a MeshRenderer) first.");
            return;
        }

        // Same texture size on both grounds
        AddTiling(ground.gameObject);
        GameObject mainGround = GameObject.Find(MainGroundName);
        if (mainGround != null && mainGround != ground.gameObject)
            AddTiling(mainGround);

        // Sides, set up like the main ground's
        string sidesName = ground.name + " Sides";
        if (GameObject.Find(sidesName) == null)
        {
            var sides = new GameObject(sidesName, typeof(MeshFilter), typeof(MeshRenderer));
            Undo.RegisterCreatedObjectUndo(sides, "Ground sides");

            GameObject mainSides = GameObject.Find(MainSidesName);
            var sidesRenderer = sides.GetComponent<MeshRenderer>();
            if (mainSides != null)
            {
                var template = mainSides.GetComponent<MeshRenderer>();
                sidesRenderer.sharedMaterial = template.sharedMaterial;
                sidesRenderer.shadowCastingMode = template.shadowCastingMode;
                sidesRenderer.receiveShadows = template.receiveShadows;
            }

            var skirt = sides.AddComponent<GroundSkirt>();
            var serialized = new SerializedObject(skirt);
            serialized.FindProperty("ground").objectReferenceValue = ground;
            var mainSkirt = mainSides != null ? mainSides.GetComponent<GroundSkirt>() : null;
            if (mainSkirt != null)
            {
                var mainSerialized = new SerializedObject(mainSkirt);
                serialized.FindProperty("depth").floatValue = mainSerialized.FindProperty("depth").floatValue;
                serialized.FindProperty("tilesPerUnit").floatValue = mainSerialized.FindProperty("tilesPerUnit").floatValue;
            }
            serialized.ApplyModifiedProperties();
        }

        EditorSceneManager.MarkSceneDirty(ground.gameObject.scene);
        Debug.Log($"Extra ground: {ground.name} set up. Save the scene to keep it.");
    }

    private static void AddTiling(GameObject target)
    {
        if (target.GetComponent<WorldTiling>() == null)
            Undo.AddComponent<WorldTiling>(target);
    }
}
