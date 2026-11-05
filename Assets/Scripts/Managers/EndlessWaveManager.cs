using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Splines;

public class EndlessWaveManager : MonoBehaviour
{
    public static EndlessWaveManager Instance { get; private set; }

    [Header("Path")]
    [SerializeField] private SplineContainer splineContainer;

    [Header("Wave Configuration")]
    [Tooltip("Used only when Resources/WaveSettings has no waves; the settings asset holds the real run.")]
    [SerializeField] private List<Wave> waves;

    [Header("Sky Drops")]
    [Tooltip("Where along the path (0 = start, 1 = castle) enemies that drop from the sky can land; " +
             "picked at random between the two.")]
    [SerializeField] private Vector2 skyDropRange = new Vector2(0.1f, 0.5f);
    [Tooltip("How far out a roller's end markers sit, as a fraction of half its length (1 = right at its ends).")]
    [SerializeField, Range(0f, 1f)] private float rollerMarkerSpread = 0.75f;

    [Header("Scaling")]
    [SerializeField] private int enemiesPerWaveIncrement = 2;

    [Header("Starting Weapons")]
    [Tooltip("Weapon pickups whose weapons the player starts the run carrying, in slot order.")]
    [SerializeField] private List<GameObject> weaponPrefabs;

    private int totalWaveCount = 0;
    // The wave the run is on (1 for the first); the last one reached once the run is over
    public int CurrentWave => totalWaveCount;
    // The wave on now is a boss wave
    public bool IsBossWave { get; private set; }
    private int waveLoopIndex = 0;

    private bool skipToNextWave; // debug
    private bool forceBossWave;  // debug

    // Boss waves: the ones still to come before any repeats, how many times each has been played, and
    // the harvest bonus's multiplier for the wave on now (bigger for a boss wave)
    private readonly List<int> bossBag = new();
    private readonly Dictionary<int, int> bossPlays = new();
    private int lastBoss = -1;
    private float bonusMultiplier = 1f;
    private int wavesSinceShop;

    // The harvest bonus timer: seconds left to clear the wave in, counting down from its first spawn while
    // the game's playing and stopping at zero. Clearing it in time pays a share of the grass picked up
    // during the wave, bigger the more time is left
    public float TimeLeft { get; private set; }
    public bool TimerRunning { get; private set; }
    public float BonusPercentNow =>
        WaveSettings.Instance != null && timeLimit > 0f
            ? WaveSettings.Instance.BonusPercent(TimeLeft / timeLimit) * bonusMultiplier : 0f;
    public int GrassThisWave { get; private set; }
    // The harvest bonus earned for the wave just cleared. Whoever listens adds it to the grass (the HUD
    // does once it has flown it to the counter); with no one listening it's added straight away
    public event Action<int> OnHarvestBonus;
    private float timeLimit;

    // Events
    public event Action<int> OnWaveChanged;
    public event Action<EnemyBase> OnEnemySpawned;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable()
    {
        GameStateManager.OnGameStateChanged += HandleGameStateChanged;
        Currency.OnGrassCollected += HandleGrassCollected;
    }

    private void OnDisable()
    {
        GameStateManager.OnGameStateChanged -= HandleGameStateChanged;
        Currency.OnGrassCollected -= HandleGrassCollected;
    }

    // Only what's picked up while a wave's timer is on counts toward its harvest bonus
    private void HandleGrassCollected(int amount)
    {
        if (TimerRunning)
            GrassThisWave += amount;
    }

    private void Start()
    {
        GiveStartingWeapons();
        StartCoroutine(RunEndlessWaves());
    }

    private void GiveStartingWeapons()
    {
        WeaponInventory inventory = Player.Instance != null ? Player.Instance.Inventory : null;
        if (inventory == null) return;

        foreach (GameObject prefab in weaponPrefabs)
        {
            WeaponPickup pickup = prefab != null ? prefab.GetComponent<WeaponPickup>() : null;
            if (pickup != null)
                inventory.Add(pickup.weaponSO);
        }
        inventory.Select(0); // the first one in hand
    }

    private void Update()
    {
        // Debug (the ` key): kill every enemy and go straight to the next wave
        if (Input.GetKeyDown(KeyCode.BackQuote) && GameStateManager.CurrentState == GameState.Playing)
            SkipToNextWave();
        // Debug (the B key): the next wave is a boss wave
        if (Input.GetKeyDown(KeyCode.B))
        {
            forceBossWave = true;
            Debug.Log("Waves: the next wave is a boss wave.");
        }

        TickTimer();
    }

    private void StartTimer(Wave wave, int enemies, WaveSettings settings)
    {
        TimerRunning = settings != null;
        GrassThisWave = 0;
        if (!TimerRunning) return;
        TimeLeft = timeLimit = settings.TimeLimit(wave, enemies);
    }

    private void TickTimer()
    {
        if (!TimerRunning || TimeLeft <= 0f || GameStateManager.CurrentState != GameState.Playing) return;
        TimeLeft = Mathf.Max(0f, TimeLeft - Time.deltaTime);
    }

    // Cleared in time: the bonus share of the grass picked up this wave (not counted as picked up itself)
    private void PayHarvestBonus()
    {
        int bonus = TimerRunning ? Mathf.FloorToInt(GrassThisWave * BonusPercentNow / 100f) : 0;
        TimerRunning = false;
        if (bonus <= 0) return;
        if (OnHarvestBonus != null)
            OnHarvestBonus(bonus);
        else
            Currency.AddGrass(bonus);
    }

    private void SkipToNextWave()
    {
        skipToNextWave = true;
        foreach (EnemyBase enemy in FindObjectsByType<EnemyBase>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (enemy.IsDead) continue;
            if (enemy.gameObject.activeInHierarchy)
                enemy.Kill();
            else
                Destroy(enemy.gameObject); // still waiting to drop from the sky
        }
    }

    // Waits the given seconds, or less if the debug skip is pressed meanwhile
    private IEnumerator Wait(float seconds)
    {
        for (float end = Time.time + seconds; Time.time < end && !skipToNextWave;)
            yield return null;
    }

    private void HandleGameStateChanged(GameState state)
    {
        // Stop spawning once the run is over
        if (state == GameState.CastleDestroyed || state == GameState.PlayerDead)
            StopAllCoroutines();
    }

    private IEnumerator RunEndlessWaves()
    {
        WaveSettings settings = WaveSettings.Instance;
        List<Wave> list = settings != null && settings.waves.Count > 0 ? settings.waves : waves;
        if (list == null || list.Count == 0) yield break;

        if (settings != null)
            yield return Wait(settings.firstWaveDelay);

        int index = 0;
        while (true)
        {
            totalWaveCount++;
            skipToNextWave = false;

            // Every few waves a boss wave comes in between; the others carry on after it
            bool boss = settings != null && settings.bossWaves.Count > 0
                        && (settings.IsBossWave(totalWaveCount) || forceBossWave);
            forceBossWave = false;
            Wave wave;
            BossWave bossWave = null;
            float countScale = 1f;
            if (boss)
            {
                int pick = NextBossWave(settings);
                bossWave = settings.bossWaves[pick];
                int plays = bossPlays.TryGetValue(pick, out int played) ? played : 0;
                bossPlays[pick] = plays + 1;
                countScale = 1f + bossWave.growthPerRepeat * plays;
                wave = bossWave;
            }
            else
            {
                // Played through: carry on from a later wave, a loop harder
                if (index >= list.Count)
                {
                    index = settings != null ? Mathf.Clamp(settings.loopFromWave - 1, 0, list.Count - 1) : 0;
                    waveLoopIndex++;
                }
                wave = list[index++];
            }
            bonusMultiplier = boss ? settings.bossBonusMultiplier : 1f;
            IsBossWave = boss;

            OnWaveChanged?.Invoke(totalWaveCount);
            GameEvents.WaveStarted(totalWaveCount);

            SpikeRain rain = null;
            if (boss)
            {
                // The banner shows (and the dark comes in) before the enemies do
                GameEvents.BossWaveStarted(bossWave);
                if (bossWave.dark)
                    BossDarkness.FadeIn();
                yield return Wait(settings.bossBannerTime);
                GameEvents.BossBannerEnded();
                if (bossWave.spikeRain != null)
                    rain = SpikeRain.Begin(bossWave.spikeRain, bossWave.spikeRainInterval / countScale,
                                           bossWave.spikeRainRadius);
            }

            // Spawn enemies
            yield return SpawnWave(wave, settings, countScale);

            // Wait until all wave-blocking enemies are dead (spike balls don't count)
            yield return new WaitUntil(() => EnemyManager.Instance.GetWaveBlockingEnemyCount() == 0);

            if (rain != null)
                rain.Stop();
            if (boss)
            {
                BossDarkness.FadeOut();
                GameEvents.BossWaveEnded();
            }

            PayHarvestBonus();
            GameEvents.WaveCleared();

            // Every few waves, and after every boss wave, the shop (the game waits while it's open)
            wavesSinceShop++;
            ShopSettings shopSettings = ShopSettings.Instance;
            Shop shop = Shop.Instance;
            bool shopNext = shop != null && shopSettings != null && shopSettings.wavesBetweenShops > 0
                            && (boss || wavesSinceShop >= shopSettings.wavesBetweenShops);
            if (shopNext)
                GameEvents.ShopComing();
            Sounds.Play(shopNext ? Sound.ShopWaveClear : Sound.WaveClear);
            if (shopNext)
            {
                wavesSinceShop = 0;
                yield return new WaitForSeconds(shopSettings.openDelay);
                yield return shop.Visit();
            }

            if (settings != null)
                yield return Wait(settings.breakBetweenWaves);
        }
    }

    // A random boss wave that hasn't been played since they were all last seen (never the one just played)
    private int NextBossWave(WaveSettings settings)
    {
        int count = settings.bossWaves.Count;
        if (bossBag.Count == 0)
        {
            for (int i = 0; i < count; i++)
                if (count == 1 || i != lastBoss)
                    bossBag.Add(i);
        }
        int at = UnityEngine.Random.Range(0, bossBag.Count);
        lastBoss = bossBag[at];
        bossBag.RemoveAt(at);
        return lastBoss;
    }

    // countScale grows every Base Count (a boss wave that's come round again)
    private IEnumerator SpawnWave(Wave wave, WaveSettings settings, float countScale = 1f)
    {
        // Build weighted list for extra/random enemies
        // Entries, not just prefabs, so per-entry settings (e.g. ranged immunity) come along
        List<EnemySpawnEntry> weightedList = new List<EnemySpawnEntry>();
        foreach (var entry in wave.enemies)
        {
            if (entry.enemyPrefab == null) continue; // an enemy that's been removed
            int reps = Mathf.CeilToInt(entry.spawnWeight * 10);
            for (int i = 0; i < reps; i++)
                weightedList.Add(entry);
        }

        // Build combined spawn list
        List<EnemySpawnEntry> spawnList = new List<EnemySpawnEntry>();

        // Add guaranteed base counts (entries that come in a row are added as a block after the shuffle)
        foreach (var entry in wave.enemies)
            if (!entry.inARow && entry.enemyPrefab != null)
                for (int i = 0; i < Count(entry, countScale); i++)
                    spawnList.Add(entry);

        // Add extra enemies: with the settings, none on the first play-through and more each loop;
        // without, a random few that grow each loop
        int extraEnemies;
        if (settings != null)
            extraEnemies = waveLoopIndex * settings.extraEnemiesPerLoop;
        else
        {
            int maxExtra = Mathf.Max(1, waveLoopIndex * enemiesPerWaveIncrement + 1);
            extraEnemies = UnityEngine.Random.Range(1, maxExtra + 1);
        }

        // Skip extras if every entry has spawnWeight 0
        for (int i = 0; i < extraEnemies && weightedList.Count > 0; i++)
        {
            spawnList.Add(weightedList[UnityEngine.Random.Range(0, weightedList.Count)]);
        }

        // Shuffle spawn list
        for (int i = spawnList.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            var temp = spawnList[i];
            spawnList[i] = spawnList[j];
            spawnList[j] = temp;
        }

        // Entries that come in a row (a Konga chain) go in as one unbroken block at a random place
        foreach (var entry in wave.enemies)
        {
            if (!entry.inARow || entry.baseCount <= 0 || entry.enemyPrefab == null) continue;
            int at = UnityEngine.Random.Range(0, spawnList.Count + 1);
            for (int i = 0; i < Count(entry, countScale); i++)
                spawnList.Insert(at, entry);
        }

        // A Checker Slime entry spawns a whole group, counted at its average size; partners count too
        float enemies = 0f;
        foreach (EnemySpawnEntry entry in spawnList)
        {
            if (entry.enemyPrefab is CheckerSlimeEnemy checker && checker.CheckerData != null)
                enemies += entry.groupSize > 0 ? entry.groupSize : checker.CheckerData.AverageGroupSize;
            else
                enemies += 1f;
            if (entry.partner != null)
                enemies += 1f;
        }
        StartTimer(wave, Mathf.RoundToInt(enemies), settings);

        // Spawn enemies (the rest are dropped if the debug skip is pressed)
        for (int i = 0; i < spawnList.Count && !skipToNextWave; i++)
        {
            EnemySpawnEntry entry = spawnList[i];
            SpawnEnemy(entry, settings);
            if (entry.partner != null)
            {
                yield return Wait(entry.partnerGap);
                SpawnEnemy(new EnemySpawnEntry { enemyPrefab = entry.partner, immuneToRanged = entry.immuneToRanged }, settings);
            }

            // One of a row is followed closely by the next
            bool rowContinues = entry.inARow && i + 1 < spawnList.Count && spawnList[i + 1] == entry;
            yield return Wait(rowContinues ? entry.rowGap : SpawnGap(wave, settings));
        }
    }

    private void SpawnEnemy(EnemySpawnEntry entry, WaveSettings settings)
    {
        // Checker Slimes come as a whole group in a line
        if (entry.enemyPrefab is CheckerSlimeEnemy checker && splineContainer != null)
        {
            CheckerSlimeGroup.Spawn(checker, splineContainer, slime =>
            {
                PopIn(slime.transform);
                SetUpSpawned(slime, entry, settings);
            }, entry.groupSize);
            return;
        }

        EnemyBase instance = Instantiate(entry.enemyPrefab);
        instance.AssignSpline(splineContainer);

        if (splineContainer != null && instance.Data != null && instance.Data.dropsFromSky)
        {
            // Off until it lands on a marked spot along the path
            float progress = UnityEngine.Random.Range(skyDropRange.x, skyDropRange.y);
            instance.PlaceOnPath(progress);
            instance.FaceAlongPath();
            instance.gameObject.SetActive(false);
            SkyDrop.Begin(instance, instance.transform.position, DropMarkers(instance, progress));
        }
        else
        {
            if (splineContainer != null)
                instance.transform.position = splineContainer.EvaluatePosition(splineContainer.Spline, 0f);
            PopIn(instance.transform);
            instance.FaceAlongPath();
        }

        SetUpSpawned(instance, entry, settings);
    }

    // One more enemy of the wave on now, made by another along the path (the Slime Queen's brood): it
    // pops in at that spot, as tough as the rest of the wave
    public EnemyBase SpawnAlongPath(EnemyBase prefab, float progress)
    {
        if (prefab == null || splineContainer == null) return null;
        EnemyBase instance = Instantiate(prefab);
        instance.AssignSpline(splineContainer);
        instance.PlaceOnPath(Mathf.Clamp01(progress));
        PopIn(instance.transform);
        instance.FaceAlongPath();
        SetUpSpawned(instance, new EnemySpawnEntry { enemyPrefab = prefab }, WaveSettings.Instance);
        return instance;
    }

    private static int Count(EnemySpawnEntry entry, float scale) => Mathf.RoundToInt(entry.baseCount * scale);

    // Immunity, toughness and bookkeeping for a newly spawned enemy
    private void SetUpSpawned(EnemyBase instance, EnemySpawnEntry entry, WaveSettings settings)
    {
        if (entry.immuneToRanged)
            instance.SetImmuneToRanged(true);

        instance.SetHealthMultiplier(settings != null ? settings.HealthMultiplier(totalWaveCount) : 1f);
        OnEnemySpawned?.Invoke(instance);

        // Unregister on death
        instance.OnDied += () => EnemyManager.Instance.UnregisterEnemy(instance);
    }

    // Where to mark a sky drop, relative to the spot: one in the middle, or for a roller three in a line
    // across the path, where its length will lie (middle and near each end)
    private Vector3[] DropMarkers(EnemyBase enemy, float progress)
    {
        if (enemy is not RollerEnemy roller || roller.HalfLength <= 0f) return null;

        Vector3 along = splineContainer.EvaluateTangent(progress);
        along.y = 0f;
        if (along.sqrMagnitude < 0.0001f) return null;
        Vector3 across = Vector3.Cross(Vector3.up, along.normalized);

        float reach = roller.HalfLength * rollerMarkerSpread;
        return new[] { -across * reach, Vector3.zero, across * reach };
    }

    // Scales up from nothing as it appears
    private static void PopIn(Transform enemy)
    {
        float time = FXSettings.Instance != null ? FXSettings.Instance.spawnPopTime : 0.3f;
        if (time <= 0f) return;

        Vector3 fullSize = enemy.localScale;
        enemy.localScale = Vector3.zero;
        enemy.DOScale(fullSize, time).SetEase(Ease.OutBack).SetLink(enemy.gameObject);
    }

    // Seconds until the next spawn: the wave's range, sometimes halved (a burst), shorter each loop
    private float SpawnGap(Wave wave, WaveSettings settings)
    {
        float interval = UnityEngine.Random.Range(wave.spawnIntervalMin, wave.spawnIntervalMax);
        if (UnityEngine.Random.value < wave.burstChance)
            interval *= 0.5f;
        if (settings != null)
            interval = Mathf.Max(settings.minSpawnGap, interval * Mathf.Pow(settings.spawnGapPerLoop, waveLoopIndex));
        return interval;
    }
}

[System.Serializable]
public class Wave
{
    [Tooltip("Just a note to tell waves apart in the Inspector.")]
    public string name;
    [Tooltip("Seconds to clear this wave in for the harvest bonus (0 = worked out from its enemies by the wave settings).")]
    public float timeLimit;
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
    [Tooltip("Enemies spawned from this entry are immune to ranged attacks, whatever their prefab says.")]
    public bool immuneToRanged;
    [Tooltip("Spawns all of Base Count one right after another instead of mixed in (a Konga chain). " +
             "A Checker Slime entry always spawns a whole group each time; Base Count is how many groups.")]
    public bool inARow;
    [Tooltip("Seconds between the ones in a row.")]
    public float rowGap = 0.35f;
    [Tooltip("For a Checker Slime entry: how many in each group (an even number; 0 = the data's range).")]
    public int groupSize;
    [Tooltip("Optional: another enemy that comes right behind each one of these (a Tibbit for each Frills, " +
             "to ride it).")]
    public EnemyBase partner;
    [Tooltip("Seconds after each one before its partner comes.")]
    public float partnerGap = 0.4f;
}
