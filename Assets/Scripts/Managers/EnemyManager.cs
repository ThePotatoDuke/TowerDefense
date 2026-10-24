using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    // All active enemies in the scene
    private readonly List<IEnemy> activeEnemies = new();
    // Read-only view for per-frame checks that shouldn't allocate a copy
    public IReadOnlyList<IEnemy> ActiveEnemies => activeEnemies;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // --- Register / Unregister enemies ---
    public void RegisterEnemy(IEnemy enemy)
    {
        if (!activeEnemies.Contains(enemy))
            activeEnemies.Add(enemy);
    }

    public void UnregisterEnemy(IEnemy enemy)
    {
        activeEnemies.Remove(enemy);
    }

    // --- Queries ---
    public float GetAverageProgress()
    {
        if (activeEnemies.Count == 0) return 0f;
        return activeEnemies.Average(e => e.DistancePercentage);
    }

    public List<IEnemy> GetAllEnemies()
    {
        return activeEnemies.ToList();
    }

    public int GetWaveBlockingEnemyCount()
    {
        return activeEnemies.Count(e => e.BlocksWaveProgress);
    }

}
