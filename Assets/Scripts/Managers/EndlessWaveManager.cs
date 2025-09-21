using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class EndlessSplineWaveManager : MonoBehaviour
{
    [Header("Common Path")]
    [Tooltip("All enemies will follow this spline.")]
    // [SerializeField] private SplineContainer splineContainer;

    [Header("Waves Configuration")]
    [SerializeField] private List<Wave> waves;

    [Header("Scaling")]
    [Tooltip("Extra enemies added per wave loop.")]
    [SerializeField] private int enemiesPerWaveIncrement = 2;

    [Tooltip("Optional scaling factor for enemy health per wave loop.")]
    [SerializeField] private float enemyHealthMultiplier = 1.1f;
    [SerializeField] private SplineContainer splineContainer;

    private int waveLoopIndex = 0;

    private void Start()
    {
        StartCoroutine(RunEndlessWaves());
    }

    private IEnumerator RunEndlessWaves()
    {
        while (true)
        {
            foreach (Wave wave in waves)
            {
                int enemyCount = wave.baseEnemyCount + waveLoopIndex * enemiesPerWaveIncrement;

                yield return StartCoroutine(SpawnWave(wave, enemyCount));

                // Wait until all enemies are cleared
                while (EnemyManager.Instance.GetAllEnemies().Count > 0)
                    yield return null;

                Debug.Log($"Wave {waveLoopIndex + 1} completed!");
            }

            waveLoopIndex++; // increment loop counter for scaling
        }
    }

    private IEnumerator SpawnWave(Wave wave, int totalEnemies)
    {
        for (int i = 0; i < totalEnemies; i++)
        {
            // Pick a random enemy from the pool
            EnemyBase enemyPrefab = wave.enemyPool[Random.Range(0, wave.enemyPool.Count)];

            EnemyBase enemyInstance = Instantiate(enemyPrefab);

            // Set enemy position to the start of the spline
            if (splineContainer != null)
            {
                // Evaluate position at t = 0 (start of spline)
                enemyInstance.transform.position = SplineUtility.EvaluatePosition(splineContainer.Spline, 0f);
            }

            // Assign the common spline
            enemyInstance.AssignSpline(splineContainer);

            // Optionally scale health
            // enemyInstance.SetHealthMultiplier(Mathf.Pow(enemyHealthMultiplier, waveLoopIndex));

            yield return new WaitForSeconds(wave.spawnInterval);
        }
    }


}

[System.Serializable]
public class Wave
{
    [Tooltip("Base number of enemies for this wave.")]
    public int baseEnemyCount = 5;

    [Tooltip("Allowed enemies for this wave.")]
    public List<EnemyBase> enemyPool;

    [Tooltip("Interval between enemy spawns in seconds.")]
    public float spawnInterval = 0.5f;
}
