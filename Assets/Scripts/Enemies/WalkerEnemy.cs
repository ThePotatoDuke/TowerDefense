using UnityEngine;

public class WalkerEnemy : EnemyBase
{
    [SerializeField] private WalkerDataSO data;
    public override EnemyDataSO Data => data;


    protected override void Update()
    {
        base.Update(); // handle spline position, castle damage, etc.

        Move(); // your custom walking logic
    }

    public override void Move()
    {
        if (hasReachedEnd || splineContainer == null) return;

        float step = data.moveSpeed * Time.deltaTime / splineContainer.CalculateLength();
        distancePercentage = Mathf.Clamp01(distancePercentage + step);
    }
}
