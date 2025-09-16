using UnityEngine;

public class WalkerEnemy : EnemyBase
{
    [SerializeField] private WalkerDataSO data;

    public override EnemyDataSO Data => data;

    private void Update()
    {
        // Super simple walker: move forward constantly
        transform.Translate(Vector3.left * data.walkSpeed * Time.deltaTime);


    }
}
