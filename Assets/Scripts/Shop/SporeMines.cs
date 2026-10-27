using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// The Spore Mines bought in the shop: mushrooms in front of the castle, one more per level. The first
// enemy to hit the castle sets them all off: they swell, burst into a spore cloud and hurt every enemy
// around (more mushrooms, more damage and a wider blast), then lie wilted until the next shop, where they
// grow back. Spike balls, rollers and Frills neither set them off nor get hurt. The numbers are on the
// SporeMine prefab.
public class SporeMines : MonoBehaviour
{
    private static SporeMines instance;

    private readonly List<SporeMine> mushrooms = new();
    private bool armed = true;
    private bool bursting;

    // The prefab, which holds the numbers for the group
    private static SporeMine Settings => ShopSettings.Instance != null ? ShopSettings.Instance.sporeMine : null;

    // Grows one more mushroom (bought in the shop)
    public static void AddMushroom()
    {
        if (Settings == null)
        {
            Debug.LogWarning("Spore Mines: no prefab set in ShopSettings (run Tools > Create Shop).");
            return;
        }
        if (instance == null)
            instance = new GameObject("SporeMines").AddComponent<SporeMines>();
        instance.Grow();
    }

    private void OnEnable()
    {
        EnemyBase.OnEnemyReachedEnd += HandleEnemyAtCastle;
        Shop.OnClosed += Regrow;
    }

    private void OnDisable()
    {
        EnemyBase.OnEnemyReachedEnd -= HandleEnemyAtCastle;
        Shop.OnClosed -= Regrow;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void Grow()
    {
        SporeMine settings = Settings;
        if (mushrooms.Count >= settings.Spots.Length) return;

        Vector3 spot = CastleGround.Spot(settings.Spots[mushrooms.Count]);
        SporeMine mushroom = Instantiate(settings, spot, Quaternion.identity, transform);
        if (!armed)
            WorldProp.SetSprite(mushroom.Sprite, mushroom.WiltedSprite);
        mushrooms.Add(mushroom);
        PopIn(mushroom.transform);
    }

    // Wilted ones spring back up (after a shop)
    private void Regrow()
    {
        if (armed || bursting) return;
        armed = true;
        foreach (SporeMine mushroom in mushrooms)
        {
            WorldProp.SetSprite(mushroom.Sprite, mushroom.MushroomSprite);
            PopIn(mushroom.transform);
        }
    }

    private static void PopIn(Transform root)
    {
        float time = FXSettings.Instance != null ? FXSettings.Instance.spawnPopTime : 0.3f;
        root.DOKill();
        root.localScale = Vector3.zero;
        root.DOScale(1f, Mathf.Max(time, 0.05f)).SetEase(Ease.OutBack).SetLink(root.gameObject);
    }

    private void HandleEnemyAtCastle(EnemyBase enemy)
    {
        if (!armed || bursting || mushrooms.Count == 0 || !Triggers(enemy)) return;
        StartCoroutine(Burst());
    }

    // Spike balls, rollers and Frills are left out
    private static bool Triggers(EnemyBase enemy) =>
        enemy != null && !(enemy is SpikeBallEnemy || enemy is RollerEnemy || enemy is FrillsEnemy);

    private IEnumerator Burst()
    {
        SporeMine settings = Settings;
        bursting = true;

        // Swell up, so it's seen coming
        foreach (SporeMine mushroom in mushrooms)
        {
            Transform root = mushroom.transform;
            root.DOKill(true);
            root.DOScale(new Vector3(1.25f, 1.4f, 1f), settings.FuseTime).SetEase(Ease.InQuad).SetLink(root.gameObject);
            if (root.TryGetComponent(out SpriteFX fx))
                fx.Flash(settings.FuseTime);
        }
        yield return new WaitForSeconds(settings.FuseTime);

        // Burst: spores from every mushroom, and the blast around the middle of them
        Vector3 middle = Vector3.zero;
        foreach (SporeMine mushroom in mushrooms)
        {
            Transform root = mushroom.transform;
            middle += root.position;
            SporeCloud.Play(root.position + Vector3.up * 0.3f, settings.SporeColorA, settings.SporeColorB);

            WorldProp.SetSprite(mushroom.Sprite, mushroom.WiltedSprite);
            root.DOKill();
            root.localScale = new Vector3(1.3f, 0.6f, 1f);
            root.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetLink(root.gameObject);
        }
        middle /= mushrooms.Count;

        float radius = settings.Radius + settings.RadiusPerMushroom * (mushrooms.Count - 1);
        float damage = settings.DamagePerMushroom * mushrooms.Count;
        HurtEnemiesAround(middle, radius, damage);
        CameraShake.Shake(middle, settings.Shake, 0.3f);

        armed = false;
        bursting = false;
    }

    private static void HurtEnemiesAround(Vector3 center, float radius, float damage)
    {
        if (EnemyManager.Instance == null) return;

        // Copied: enemies dying from the blast leave the list
        foreach (IEnemy enemy in EnemyManager.Instance.GetAllEnemies())
        {
            if (!(enemy is EnemyBase target) || target.IsDead || !Triggers(target)) continue;

            Vector3 offset = target.transform.position - center;
            offset.y = 0f;
            if (offset.magnitude <= radius + target.BodyRadius)
                target.TakeDamage(damage);
        }
    }
}
