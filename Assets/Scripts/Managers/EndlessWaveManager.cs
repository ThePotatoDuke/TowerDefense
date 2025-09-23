using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;

public class EndlessWaveManager : MonoBehaviour
{
    [Header("Path")]
    [SerializeField] private SplineContainer splineContainer;

    [Header("Wave Configuration")]
    [SerializeField] private List<Wave> waves;

    [Header("Scaling")]
    [SerializeField] private int enemiesPerWaveIncrement = 2;

    [Header("Weapon Drops")]
    [SerializeField] private List<GameObject> weaponPrefabs;


    private int totalWaveCount = 0;
    private int waveLoopIndex = 0;

    // Events
    public event Action<int> OnWaveChanged;
    public event Action<EnemyBase> OnEnemySpawned;

    private void Start()
    {
        StartCoroutine(RunEndlessWaves());
    }

    private IEnumerator RunEndlessWaves()
    {
        while (true)
        {
            foreach (var wave in waves)
            {
                totalWaveCount++;
                OnWaveChanged?.Invoke(totalWaveCount);

                yield return StartCoroutine(SpawnWave(wave));

                // Spawn a random weapon at the end of the wave
                SpawnRandomWeapon();
            }

            waveLoopIndex++;
        }
    }

    private IEnumerator SpawnWave(Wave wave)
    {
        int aliveEnemies = 0;

        // 1️⃣ Build weighted list for extra/random selection
        List<EnemyBase> weightedList = new List<EnemyBase>();
        foreach (var entry in wave.enemies)
        {
            int reps = Mathf.CeilToInt(entry.spawnWeight * 10);
            for (int i = 0; i < reps; i++)
                weightedList.Add(entry.enemyPrefab);
        }

        List<EnemyBase> spawnList = new List<EnemyBase>();

        // Add guaranteed base counts
        foreach (var entry in wave.enemies)
        {
            for (int i = 0; i < entry.baseCount; i++)
                spawnList.Add(entry.enemyPrefab);
        }

        // Add extra enemies (random amount)
        int minExtra = 1; // minimum extras to always spawn
        int maxExtra = Mathf.Max(minExtra, waveLoopIndex * enemiesPerWaveIncrement + minExtra); // scale with waveLoopIndex
        int extraEnemies = UnityEngine.Random.Range(minExtra, maxExtra + 1);

        for (int i = 0; i < extraEnemies; i++)
        {
            EnemyBase prefab = weightedList[UnityEngine.Random.Range(0, weightedList.Count)];
            spawnList.Add(prefab);
        }

        for (int i = spawnList.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            var temp = spawnList[i];
            spawnList[i] = spawnList[j];
            spawnList[j] = temp;
        }

        foreach (var enemy in spawnList)
        {
            SpawnEnemy(enemy, () => aliveEnemies--);
            aliveEnemies++;
            yield return new WaitForSeconds(RandomInterval(wave));
        }

        while (aliveEnemies > 0)
            yield return null;
    }


    private void SpawnEnemy(EnemyBase prefab, Action onDeath)
    {
        EnemyBase instance = Instantiate(prefab);
        instance.AssignSpline(splineContainer);

        if (splineContainer != null)
            instance.transform.position = splineContainer.EvaluatePosition(splineContainer.Spline, 0f);

        instance.SetHealthMultiplier(1f);
        OnEnemySpawned?.Invoke(instance);

        // Subscribe to the enemy's death
        instance.OnDied += onDeath;
    }

    private float RandomInterval(Wave wave)
    {
        float interval = UnityEngine.Random.Range(wave.spawnIntervalMin, wave.spawnIntervalMax);
        if (UnityEngine.Random.value < wave.burstChance)
            interval *= 0.5f; // small burst chance
        return interval;
    }

    [Header("Player Reference")]
    [SerializeField] private Transform playerTransform; // assign your player here
    [SerializeField] private Vector3 dropOffset = new Vector3(1f, 0f, 0f); // offset from player

    private void SpawnRandomWeapon()
    {
        if (weaponPrefabs.Count == 0 || playerTransform == null) return;

        GameObject prefab = weaponPrefabs[UnityEngine.Random.Range(0, weaponPrefabs.Count)];

        // Spawn next to player with some random offset
        Vector3 spawnPosition = playerTransform.position + dropOffset;
        Instantiate(prefab, spawnPosition, Quaternion.identity);
    }
}

[System.Serializable]
public class Wave
{
    [Tooltip("Enemies available for this wave.")]
    public List<EnemySpawnEntry> enemies;

    [Tooltip("Minimum time between each enemy spawn in seconds.")]
    public float spawnIntervalMin = 0.3f;

    [Tooltip("Maximum time between each enemy spawn in seconds.")]
    public float spawnIntervalMax = 0.7f;

    [Range(0f, 1f)]
    [Tooltip("Chance that the next enemy spawns in a small burst (half interval).")]
    public float burstChance = 0.2f;
}

[System.Serializable]
public class EnemySpawnEntry
{
    [Tooltip("Enemy prefab to spawn.")]
    public EnemyBase enemyPrefab;

    [Range(0f, 1f)]
    [Tooltip("Relative likelihood to spawn this enemy. Higher = more likely.")]
    public float spawnWeight = 1f;

    [Tooltip("Number of this enemy to spawn per wave (before scaling).")]
    public int baseCount = 1;
}
