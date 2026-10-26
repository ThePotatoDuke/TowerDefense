using UnityEditor;
using UnityEngine;

// Tools > Add Tibbit Perches: gives every enemy prefab a tibbit can ride a "TibbitPerch" child on its
// sprite, at the top middle of the sprite, ready to be dragged onto the head in the prefab. Enemies that
// already have one are left alone, as are the ones tibbits never ride (rollers, spike balls, tibbits).
public static class TibbitPerchSetup
{
    [MenuItem("Tools/Add Tibbit Perches")]
    private static void Run()
    {
        int added = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Enemies" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            var enemy = root.GetComponent<EnemyBase>();
            bool rideable = enemy != null && !(enemy is TibbitEnemy || enemy is RollerEnemy || enemy is SpikeBallEnemy);
            if (rideable && root.GetComponentInChildren<TibbitPerch>(true) == null)
            {
                // The enemy's own sprite (the health bar's shield starts switched off, so it isn't found)
                SpriteRenderer sprite = root.GetComponentInChildren<SpriteRenderer>();
                if (sprite != null && sprite.sprite != null)
                {
                    var perch = new GameObject("TibbitPerch", typeof(TibbitPerch));
                    perch.transform.SetParent(sprite.transform, false);
                    Bounds bounds = sprite.sprite.bounds;
                    perch.transform.localPosition = new Vector3(bounds.center.x, bounds.max.y, 0f);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    added++;
                }
                else
                {
                    Debug.LogWarning($"Tibbit perches: {path} has no sprite to put a perch on.");
                }
            }
            PrefabUtility.UnloadPrefabContents(root);
        }
        Debug.Log($"Tibbit perches: added {added}. Open each enemy prefab and move its TibbitPerch onto the head.");
    }
}
