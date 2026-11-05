using UnityEngine;

// The Slime Queen: a huge slime that hops slowly down the path like any slime, and each time she loses a
// share of her health she throws out a brood of slimes onto the path around her; a last, bigger brood
// bursts out when she dies.
[CreateAssetMenu(fileName = "SlimeQueenData", menuName = "Enemies/Slime Queen Data")]
public class SlimeQueenDataSO : SlimeDataSO
{
    [Header("Brood")]
    [Tooltip("Slimes she can throw out, picked at random for each one.")]
    public EnemyBase[] brood;
    [Tooltip("She throws out a brood every time she loses this share of her health (0.2 = at 80%, 60%, 40% " +
             "and 20% left).")]
    [Range(0.05f, 0.5f)] public float broodEvery = 0.2f;
    [Tooltip("How many in each brood.")]
    public int broodSize = 3;
    [Tooltip("How many burst out when she dies.")]
    public int deathBroodSize = 6;
    [Tooltip("How far along the path from her the brood lands, at most (world units, either way).")]
    public float broodSpread = 1.5f;
}
