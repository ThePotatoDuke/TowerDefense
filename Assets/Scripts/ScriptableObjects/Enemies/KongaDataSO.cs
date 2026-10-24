using UnityEngine;

[CreateAssetMenu(menuName = "Enemies/Konga Data")]
public class KongaDataSO : EnemyDataSO
{
    [Header("Movement")]
    public float moveSpeed = 1.5f;
    [Tooltip("How quickly it changes speed (units per second, per second).")]
    public float acceleration = 25f;

    [Header("Chain")]
    [Tooltip("Gap kept to the Konga ahead, center to center. Kongas of the same kind (same data asset) chain up.")]
    public float spacing = 0.9f;
    [Tooltip("Speed when rushing forward to close a gap left by a destroyed Konga.")]
    public float catchUpSpeed = 7f;
    [Tooltip("A gap this much bigger than Spacing makes the Konga rush forward.")]
    public float catchUpThreshold = 0.4f;
    [Tooltip("Distance over which it brakes before rejoining, so it settles in just behind the one ahead.")]
    public float brakeDistance = 1.2f;
    [Tooltip("Kongas further apart than this don't chain; the one behind leads its own line.")]
    public float maxChainGap = 8f;
}
