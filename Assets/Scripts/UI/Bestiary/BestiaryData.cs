using System;
using System.Collections.Generic;
using UnityEngine;

// Every enemy the bestiary documents, in the order it lists them: its name, the frames it's shown with,
// what it does and how to deal with it. The numbers (health, damage) are read from the enemy's own data, so
// they stay right when it's rebalanced. Lives in Resources; a new enemy needs an entry added here.
[CreateAssetMenu(fileName = "Bestiary", menuName = "UI/Bestiary")]
public class BestiaryData : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string displayName;
        [Tooltip("The enemy: its data gives the numbers, and whether it's been met.")]
        public EnemyBase prefab;
        public Sprite[] frames;
        public float framesPerSecond = 8f;
        [TextArea(3, 8)] public string description;
        [Tooltip("Optional: a hint on dealing with it, shown under the description.")]
        [TextArea(2, 4)] public string tip;

        public EnemyDataSO Data => prefab != null ? prefab.Data : null;
    }

    private static BestiaryData instance;
    public static BestiaryData Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<BestiaryData>("Bestiary");
            return instance;
        }
    }

    [Tooltip("For testing: every entry shows, met or not.")]
    public bool showAll;
    public List<Entry> entries = new();
}
