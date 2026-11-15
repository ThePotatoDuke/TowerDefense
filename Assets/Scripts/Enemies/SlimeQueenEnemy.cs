using DG.Tweening;
using UnityEngine;

// The Slime Queen boss (see SlimeQueenDataSO): hops like any slime, with a brood thrown out every time she
// loses a share of her health and a bigger one when she dies. Each brood comes with a flash and a wobble,
// so the player sees where they came from.
public class SlimeQueenEnemy : SlimeEnemy
{
    private SlimeQueenDataSO QueenData => Data as SlimeQueenDataSO;
    private int broodsThrown;
    // Her boss wave is hers: no random shield on top
    public override bool CanBeShielded => false;

    public override void TakeDamage(float amount)
    {
        base.TakeDamage(amount);
        SlimeQueenDataSO data = QueenData;
        if (isDead || data == null || data.broodEvery <= 0f) return;

        // One brood for each share of health lost so far that hasn't had one yet
        int due = Mathf.FloorToInt((1f - currentHealth / MaxHealth) / data.broodEvery + 0.0001f);
        while (broodsThrown < due)
        {
            broodsThrown++;
            ThrowBrood(data.broodSize);
        }
    }

    protected override void Die()
    {
        // Before she goes, so the wave doesn't count as cleared in between (not when she's just removed,
        // like by the debug skip)
        if (!isDead && currentHealth <= 0f && QueenData != null)
            ThrowBrood(QueenData.deathBroodSize);
        base.Die();
    }

    private void ThrowBrood(int count)
    {
        SlimeQueenDataSO data = QueenData;
        EndlessWaveManager waves = EndlessWaveManager.Instance;
        if (data == null || data.brood == null || data.brood.Length == 0 || waves == null) return;

        float spread = data.broodSpread / Mathf.Max(0.01f, splineLength);
        for (int i = 0; i < count; i++)
        {
            EnemyBase prefab = data.brood[Random.Range(0, data.brood.Length)];
            waves.SpawnAlongPath(prefab, distancePercentage + Random.Range(-spread, spread));
        }

        // A wobble of her whole body (the sprite's own scale is busy with her hops)
        Flash(0.15f);
        transform.DOComplete();
        transform.DOPunchScale(new Vector3(0.2f, -0.15f, 0.2f), 0.35f, 6, 0.6f).SetLink(gameObject);
    }
}
