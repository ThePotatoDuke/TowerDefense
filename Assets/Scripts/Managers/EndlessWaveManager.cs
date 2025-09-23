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

    [Header("Player Reference")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Vector3 dropOffset = new Vector3(1f, 0f, 0f);

    private int totalWaveCount = 0;
    private int waveLoopIndex = 0;

    private Coroutine currentWaveCoroutine;

    // Events
    public event Action<int> OnWaveChanged;
    public event Action<EnemyBase> OnEnemySpawned;

    private void Start()
    {
        StartCoroutine(RunEndlessWaves());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && currentWaveCoroutine != null)
        {
            StopCoroutine(currentWaveCoroutine);
            Wave currentWave = waves[totalWaveCount - 1];
            currentWaveCoroutine = StartCoroutine(SpawnWave(currentWave, true));
        }
    }
    private IEnumerator RunEndlessWaves()
    {
        while (true)
        {
            foreach (var wave in waves)
            {
                totalWaveCount++;
                OnWaveChanged?.Invoke(totalWaveCount);

                // Start spawning enemies
                currentWaveCoroutine = StartCoroutine(SpawnWave(wave));
                yield return currentWaveCoroutine;

                // Wait until all enemies in scene are dead
                yield return new WaitUntil(() => EnemyManager.Instance.GetAllEnemies().Count == 0);

                // Spawn weapon after wave is fully cleared
                SpawnRandomWeapon();
            }

            waveLoopIndex++;
        }
    }


    private IEnumerator SpawnWave(Wave wave, bool spawnAllInstantly = false)
    {
        // Build weighted list for extra/random enemies
        List<EnemyBase> weightedList = new List<EnemyBase>();
        foreach (var entry in wave.enemies)
        {
            int reps = Mathf.CeilToInt(entry.spawnWeight * 10);
            for (int i = 0; i < reps; i++)
                weightedList.Add(entry.enemyPrefab);
        }

        // Build combined spawn list
        List<EnemyBase> spawnList = new List<EnemyBase>();

        // Add guaranteed base counts
        foreach (var entry in wave.enemies)
            for (int i = 0; i < entry.baseCount; i++)
                spawnList.Add(entry.enemyPrefab);

        // Add extra enemies (random amount)
        int minExtra = 1;
        int maxExtra = Mathf.Max(minExtra, waveLoopIndex * enemiesPerWaveIncrement + minExtra);
        int extraEnemies = UnityEngine.Random.Range(minExtra, maxExtra + 1);

        for (int i = 0; i < extraEnemies; i++)
        {
            EnemyBase prefab = weightedList[UnityEngine.Random.Range(0, weightedList.Count)];
            spawnList.Add(prefab);
        }

        // Shuffle spawn list
        for (int i = spawnList.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            var temp = spawnList[i];
            spawnList[i] = spawnList[j];
            spawnList[j] = temp;
        }

        // Spawn enemies
        foreach (var enemy in spawnList)
        {
            SpawnEnemy(enemy);
            if (!spawnAllInstantly)
                yield return new WaitForSeconds(RandomInterval(wave));
        }
    }

    private void SpawnEnemy(EnemyBase prefab)
    {
        EnemyBase instance = Instantiate(prefab);
        instance.AssignSpline(splineContainer);

        if (splineContainer != null)
            instance.transform.position = splineContainer.EvaluatePosition(splineContainer.Spline, 0f);

        instance.SetHealthMultiplier(1f);
        OnEnemySpawned?.Invoke(instance);

        // Unregister on death
        instance.OnDied += () => EnemyManager.Instance.UnregisterEnemy(instance);
    }

    private float RandomInterval(Wave wave)
    {
        float interval = UnityEngine.Random.Range(wave.spawnIntervalMin, wave.spawnIntervalMax);
        if (UnityEngine.Random.value < wave.burstChance)
            interval *= 0.5f;
        return interval;
    }

    private void SpawnRandomWeapon()
    {
        if (weaponPrefabs.Count == 0 || playerTransform == null) return;

        GameObject prefab = weaponPrefabs[UnityEngine.Random.Range(0, weaponPrefabs.Count)];
        Vector3 spawnPosition = playerTransform.position + dropOffset;
        Instantiate(prefab, spawnPosition, Quaternion.identity);
    }
}

[System.Serializable]
public class Wave
{
    public List<EnemySpawnEntry> enemies;
    public float spawnIntervalMin = 0.3f;
    public float spawnIntervalMax = 0.7f;
    [Range(0f, 1f)]
    public float burstChance = 0.2f;
}

[System.Serializable]
public class EnemySpawnEntry
{
    public EnemyBase enemyPrefab;
    [Range(0f, 1f)]
    public float spawnWeight = 1f;
    public int baseCount = 1;
}
