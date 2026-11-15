using System.Collections.Generic;
using UnityEngine;

// Which enemies the player has met (an enemy counts once it's appeared in a run), kept between runs
// (PlayerPrefs). The bestiary shows the met ones and hides the rest as dark shapes.
public static class BestiaryProgress
{
    private const string Prefix = "Bestiary_";
    private static readonly HashSet<string> seen = new();

    public static bool IsSeen(EnemyDataSO data)
    {
        if (data == null) return false;
        if (seen.Contains(data.name)) return true;
        if (PlayerPrefs.GetInt(Prefix + data.name, 0) != 1) return false;
        seen.Add(data.name);
        return true;
    }

    public static void MarkSeen(EnemyDataSO data)
    {
        if (data == null || IsSeen(data)) return;
        seen.Add(data.name);
        PlayerPrefs.SetInt(Prefix + data.name, 1);
        PlayerPrefs.Save();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize() => seen.Clear();
}
