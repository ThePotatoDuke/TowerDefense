using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Plant Border Trees: makes Prefabs/Environment/Tree.prefab (the pine sprite, set up like the
// bushes) and plants pines along the bush border in the open scene, under Borders > Trees. One pine every
// few bushes, just behind them (toward the ground's edge) so the bushes stay in front, nudged and resized a
// little at random so the line looks natural. The side nearest the camera is left without trees, so they
// don't hide the playing field. Running it again replaces the trees it planted; Ctrl+Z takes them out.
public static class BorderTreeSetup
{
    private const string PrefabPath = "Assets/Prefabs/Environment/Tree.prefab";
    private const string BushPrefabPath = "Assets/Prefabs/Environment/Bush.prefab";
    private const string SpritePath = "Assets/3rd Party/Sprites/Environment/pine_tree.png";

    private const int EveryNthBush = 4;            // a pine next to one bush in this many
    private const float BehindBushes = 0.35f;      // how far behind the bush line, toward the edge
    private const float SidewaysJitter = 0.3f;     // random shift along the border
    private static readonly Vector2 SizeRange = new Vector2(0.9f, 1.15f);
    private const bool SkipNearSide = true;        // keep the side closest to the camera clear
    private const int Seed = 4242;

    [MenuItem("Tools/Plant Border Trees")]
    private static void Plant()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
            prefab = CreatePrefab();
        if (prefab == null) return;

        GameObject borders = GameObject.Find("Borders");
        if (borders == null)
        {
            Debug.LogWarning("Border trees: no Borders object in the open scene.");
            return;
        }

        // The bushes, in the order they run round the edge
        var bushes = new List<Transform>();
        foreach (Transform child in borders.transform)
            if (child.name.StartsWith("Bush"))
                bushes.Add(child);
        if (bushes.Count == 0)
        {
            Debug.LogWarning("Border trees: no bushes under Borders.");
            return;
        }

        // Replace trees planted before
        Transform old = borders.transform.Find("Trees");
        if (old != null)
            Undo.DestroyObjectImmediate(old.gameObject);
        var trees = new GameObject("Trees");
        Undo.RegisterCreatedObjectUndo(trees, "Plant border trees");
        trees.transform.SetParent(borders.transform, false);

        // The bush ring's extent, to tell which edge each bush is on
        Bounds ring = new Bounds(bushes[0].position, Vector3.zero);
        foreach (Transform bush in bushes)
            ring.Encapsulate(bush.position);

        Camera view = Camera.main;
        Vector3 viewForward = view != null ? view.transform.forward : Vector3.forward;
        viewForward.y = 0f;
        viewForward.Normalize();
        // Facing the camera, like the rest of the flat sprites
        Quaternion facing = Quaternion.Euler(0f, view != null ? view.transform.eulerAngles.y : 0f, 0f);

        Random.State previous = Random.state;
        Random.InitState(Seed);

        int planted = 0;
        for (int i = 0; i < bushes.Count; i += EveryNthBush)
        {
            Vector3 position = bushes[i].position;
            Vector3 outward = OutwardFromRing(position, ring, out Vector3 along);

            // The near side's outward direction points back toward the camera
            if (SkipNearSide && Vector3.Dot(outward, viewForward) < -0.5f) continue;

            position += outward * BehindBushes + along * Random.Range(-SidewaysJitter, SidewaysJitter);

            var tree = (GameObject)PrefabUtility.InstantiatePrefab(prefab, trees.transform);
            tree.name = $"Tree ({planted})";
            tree.transform.SetPositionAndRotation(position, facing);
            tree.transform.localScale = Vector3.one * Random.Range(SizeRange.x, SizeRange.y);
            planted++;
        }

        Random.state = previous;
        EditorSceneManager.MarkSceneDirty(borders.scene);
        Debug.Log($"Border trees: planted {planted} under Borders > Trees. Save the scene to keep them.");
    }

    // Which way is "out" for a point on the ring (toward the nearest edge), and the direction along that edge
    private static Vector3 OutwardFromRing(Vector3 point, Bounds ring, out Vector3 along)
    {
        float left = point.x - ring.min.x, right = ring.max.x - point.x;
        float back = point.z - ring.min.z, front = ring.max.z - point.z;
        float nearest = Mathf.Min(Mathf.Min(left, right), Mathf.Min(back, front));

        if (nearest == left) { along = Vector3.forward; return Vector3.left; }
        if (nearest == right) { along = Vector3.forward; return Vector3.right; }
        if (nearest == back) { along = Vector3.right; return Vector3.back; }
        along = Vector3.right;
        return Vector3.forward;
    }

    // Like the bush prefab: a root at the ground with the sprite on a child, using the bushes' material
    private static GameObject CreatePrefab()
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (sprite == null)
        {
            Debug.LogWarning($"Border trees: no sprite at {SpritePath} (is it imported as a Sprite?)");
            return null;
        }

        var root = new GameObject("Tree");
        var spriteObject = new GameObject("Sprite");
        spriteObject.transform.SetParent(root.transform, false); // the sprite's pivot is at the trunk's base
        var renderer = spriteObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;

        GameObject bush = AssetDatabase.LoadAssetAtPath<GameObject>(BushPrefabPath);
        SpriteRenderer bushRenderer = bush != null ? bush.GetComponentInChildren<SpriteRenderer>() : null;
        if (bushRenderer != null)
        {
            renderer.sharedMaterial = bushRenderer.sharedMaterial;
            renderer.sortingLayerID = bushRenderer.sortingLayerID;
            renderer.sortingOrder = bushRenderer.sortingOrder;
        }
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }
}
