using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Set Up Music: adds a Music object with the MusicManager to the open scene (or fills in the one
// there), with the gameplay track, the special wave track and the shop's levels from Assets/Audio/Music. Clips
// already set on it are kept.
public static class MusicSetup
{
    private const string GameplayPath = "Assets/Audio/Music/gameplay.ogg";
    private const string SpecialWavePath = "Assets/Audio/Music/special_wave.ogg";
    private const string ShopLevelPath = "Assets/Audio/Music/Shop/shop_lvl_{0}.ogg";

    [MenuItem("Tools/Set Up Music")]
    private static void Create()
    {
        var manager = Object.FindAnyObjectByType<MusicManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            var gameObject = new GameObject("Music");
            Undo.RegisterCreatedObjectUndo(gameObject, "Set Up Music");
            manager = gameObject.AddComponent<MusicManager>();
        }

        var serialized = new SerializedObject(manager);
        SerializedProperty gameplay = serialized.FindProperty("gameplay");
        if (gameplay.objectReferenceValue == null)
            gameplay.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(GameplayPath);
        SerializedProperty specialWave = serialized.FindProperty("specialWave");
        if (specialWave.objectReferenceValue == null)
            specialWave.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(SpecialWavePath);

        SerializedProperty levels = serialized.FindProperty("shopLevels");
        if (levels.arraySize == 0)
        {
            for (int i = 0; ; i++)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(string.Format(ShopLevelPath, i));
                if (clip == null) break;
                levels.arraySize = i + 1;
                levels.GetArrayElementAtIndex(i).objectReferenceValue = clip;
            }
        }
        serialized.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        Selection.activeObject = manager.gameObject;
        Debug.Log($"Music: set up with {levels.arraySize} shop levels. Save the scene to keep it.");
    }
}
