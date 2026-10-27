using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// A Crimson Weed planted beside the path: spits shots at the closest ground enemy in range and grows a
// stage after dealing enough damage (shooting faster, further and harder, three at once when full grown),
// so it grows however the kills are shared out and big enemies feed it more than small ones. Its damage
// grows with the wave number, keeping up with tougher waves. It can't reach high flyers and doesn't waste
// shots on enemies they'd bounce off. A roller passing close squashes it flat for a few seconds.
// Everything about it is set on this component in its prefab (Prefabs/Shop/CrimsonWeed), which
// ShopSettings points to; WeedPlanter reads the planting rules from it too.
public class CrimsonWeed : MonoBehaviour
{
    private static readonly List<CrimsonWeed> all = new();
    public static IReadOnlyList<CrimsonWeed> All => all;

    [SerializeField] private SpriteRenderer sprite;
    [Tooltip("Where the shots come from; half a unit up from the roots if empty.")]
    [SerializeField] private Transform muzzle;
    [Tooltip("The three growth stages.")]
    [SerializeField] private Sprite[] stageSprites = new Sprite[3];

    [Header("Shooting (one value per stage)")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private ProjectileDataSO projectileData;
    [SerializeField] private float[] range = { 4.5f, 5.5f, 6.5f };
    [Tooltip("Shots per second.")]
    [SerializeField] private float[] fireRate = { 1f, 1.3f, 1.6f };
    [Tooltip("Damage of each shot on wave 1 (whatever the projectile's own damage is).")]
    [SerializeField] private float[] shotDamage = { 12f, 20f, 28f };
    [Tooltip("Extra damage per wave after the first, as a fraction (0.05 = +5% a wave, double by wave 21).")]
    [SerializeField] private float damagePerWave = 0.05f;
    [Tooltip("Shots fired at once, spread out.")]
    [SerializeField] private int[] shots = { 1, 1, 3 };
    [SerializeField] private float spreadAngle = 15f;

    [Header("Growing")]
    [Tooltip("Damage it has to deal to reach stage 2, then (counting again from 0) stage 3. Counted at its " +
             "wave-1 damage, so the wave bonus makes it hit harder but not grow faster: only how busy its " +
             "spot is does.")]
    [SerializeField] private float[] damageToGrow = { 500f, 1500f };

    [Header("Planting")]
    [Tooltip("Closest it can be planted to the middle of the path.")]
    [SerializeField] private float minPathDistance = 1.3f;
    [Tooltip("Furthest from the path it can be planted (it should be beside it).")]
    [SerializeField] private float maxPathDistance = 4f;
    [Tooltip("Closest to another weed.")]
    [SerializeField] private float spacing = 1.5f;
    [Tooltip("Closest to the castle (the end of the path).")]
    [SerializeField] private float castleClearance = 4f;
    [SerializeField] private Color validColor = new(0.55f, 0.9f, 0.5f, 0.9f);
    [SerializeField] private Color invalidColor = new(0.95f, 0.4f, 0.4f, 0.9f);

    [Header("Squashed by Rollers")]
    [Tooltip("A roller passing this close (beyond its own half length) flattens it.")]
    [SerializeField] private float squashMargin = 0.5f;
    [SerializeField] private float squashTime = 2.5f;

    public float MinPathDistance => minPathDistance;
    public float MaxPathDistance => maxPathDistance;
    public float Spacing => spacing;
    public float CastleClearance => castleClearance;
    public Color ValidColor => validColor;
    public Color InvalidColor => invalidColor;
    public float Range => Stat(range, stage);

    private int stage;
    private float damageDealt; // toward the next stage
    private float lastShot;
    private float squashedUntil = -1f;

    private static EndlessWaveManager waves;

    // The prefab the shop plants
    public static CrimsonWeed Prefab => ShopSettings.Instance != null ? ShopSettings.Instance.crimsonWeed : null;

    public static void Plant(Vector3 groundPoint)
    {
        CrimsonWeed weed = Spawn(groundPoint);
        if (weed == null) return;
        weed.lastShot = Time.time;

        Transform root = weed.transform;
        root.localScale = Vector3.zero;
        root.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetLink(root.gameObject);
        SporeCloud.Play(groundPoint + Vector3.up * 0.2f, new Color(0.45f, 0.3f, 0.2f), new Color(0.6f, 0.15f, 0.2f));
    }

    // A new one from the prefab, at its first stage
    public static CrimsonWeed Spawn(Vector3 groundPoint)
    {
        if (Prefab == null)
        {
            Debug.LogWarning("Crimson Weed: no prefab set in ShopSettings (run Tools > Create Shop).");
            return null;
        }

        CrimsonWeed weed = Instantiate(Prefab, groundPoint, Quaternion.identity);
        if (weed.sprite == null)
            weed.sprite = weed.GetComponentInChildren<SpriteRenderer>();
        if (weed.sprite != null)
            WorldProp.SetSprite(weed.sprite, weed.StageSprite(0));
        return weed;
    }

    private void OnEnable() => all.Add(this);
    private void OnDisable() => all.Remove(this);

    private void Update()
    {
        if (GameStateManager.CurrentState != GameState.Playing) return;

        if (squashedUntil >= 0f)
        {
            if (Time.time >= squashedUntil)
                StandBackUp();
            return;
        }
        if (RollerNearby())
        {
            Squash();
            return;
        }

        float rate = Mathf.Max(0.01f, Stat(fireRate, stage));
        if (Time.time < lastShot + 1f / rate) return;

        EnemyBase target = FindTarget(Range);
        if (target != null)
        {
            lastShot = Time.time;
            Shoot(target);
        }
    }

    private EnemyBase FindTarget(float radius)
    {
        EnemyBase closest = null;
        float closestDistance = float.MaxValue;

        foreach (Collider c in Physics.OverlapSphere(transform.position, radius))
        {
            var enemy = c.GetComponentInParent<EnemyBase>();
            if (enemy == null || enemy.IsDead || enemy.IsFlying || enemy.ImmuneToRanged) continue;
            if (enemy is TibbitEnemy tibbit && !tibbit.IsVulnerable) continue;

            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = enemy;
            }
        }
        return closest;
    }

    private void Shoot(EnemyBase target)
    {
        if (projectilePrefab == null || projectileData == null) return;

        Vector3 from = muzzle != null ? muzzle.position : transform.position + Vector3.up * 0.5f;
        Vector3 aim = target.transform.position - from;
        aim.y = 0f; // flat, like the ground guns
        if (aim.sqrMagnitude < 0.0001f) return;
        aim.Normalize();

        int count = Mathf.Max(1, Stat(shots, stage));
        // The projectile multiplies its own damage, so ask for the share that makes this shot's damage
        float damage = ShotDamage();
        float baseDamage = Stat(shotDamage, stage); // what growing counts
        float multiplier = damage / Mathf.Max(0.01f, projectileData.damage);
        for (int i = 0; i < count; i++)
        {
            float angle = count > 1 ? Mathf.Lerp(-spreadAngle, spreadAngle, i / (count - 1f)) : 0f;
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * aim;
            Projectile shot = Instantiate(projectilePrefab, from, Quaternion.identity);
            shot.Initialize(projectileData, direction, multiplier, enemy => HandleHit(enemy, baseDamage, damage));
        }

        // Recoil: a quick squash back
        transform.DOKill(true);
        transform.DOPunchScale(new Vector3(0.18f, -0.18f, 0f), 0.15f, 4, 0.5f).SetLink(gameObject);
    }

    private float ShotDamage()
    {
        if (waves == null)
            waves = FindAnyObjectByType<EndlessWaveManager>();
        int wave = waves != null ? Mathf.Max(1, waves.CurrentWave) : 1;
        return Stat(shotDamage, stage) * (1f + damagePerWave * (wave - 1));
    }

    // One of its shots is about to hurt the enemy: its wave-1 damage counts toward growing, cut down in
    // proportion when the shot is more than the health left (finishing off a nearly dead enemy)
    private void HandleHit(EnemyBase enemy, float baseDamage, float damage)
    {
        if (this == null || enemy == null) return; // gone before the shot landed
        damageDealt += baseDamage * Mathf.Min(1f, enemy.CurrentHealth / Mathf.Max(0.01f, damage));

        if (stage >= 2 || damageToGrow == null || stage >= damageToGrow.Length) return;
        if (damageDealt >= damageToGrow[stage])
        {
            damageDealt = 0f;
            Grow();
        }
    }

    private void Grow()
    {
        stage++;
        WorldProp.SetSprite(sprite, StageSprite(stage));
        if (TryGetComponent(out SpriteFX fx))
            fx.Flash(0.2f);

        transform.DOKill(true);
        transform.localScale = new Vector3(0.6f, 1.4f, 1f);
        transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetLink(gameObject);
        SparkBurst.Play(transform.position + Vector3.up * 0.5f);
    }

    private bool RollerNearby()
    {
        if (EnemyManager.Instance == null) return false;

        foreach (IEnemy enemy in EnemyManager.Instance.ActiveEnemies)
        {
            if (!(enemy is RollerEnemy roller) || roller.IsDead) continue;
            Vector3 offset = roller.transform.position - transform.position;
            offset.y = 0f;
            if (offset.magnitude <= roller.HalfLength + squashMargin)
                return true;
        }
        return false;
    }

    private void Squash()
    {
        squashedUntil = Time.time + squashTime;
        transform.DOKill(true);
        transform.DOScale(new Vector3(1.4f, 0.2f, 1f), 0.08f).SetEase(Ease.OutQuad).SetLink(gameObject);
        SparkBurst.Play(transform.position + Vector3.up * 0.1f);
    }

    private void StandBackUp()
    {
        squashedUntil = -1f;
        lastShot = Time.time;
        transform.DOKill(true);
        transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetLink(gameObject);
    }

    private Sprite StageSprite(int index) =>
        stageSprites != null && stageSprites.Length > 0
            ? stageSprites[Mathf.Clamp(index, 0, stageSprites.Length - 1)]
            : null;

    private static float Stat(float[] values, int index) =>
        values != null && values.Length > 0 ? values[Mathf.Clamp(index, 0, values.Length - 1)] : 1f;

    private static int Stat(int[] values, int index) =>
        values != null && values.Length > 0 ? values[Mathf.Clamp(index, 0, values.Length - 1)] : 1;
}
