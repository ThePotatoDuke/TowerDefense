using UnityEngine;

[CreateAssetMenu(menuName = "Enemies/Roller Data")]
public class RollerDataSO : EnemyDataSO
{
    [Header("Movement")]
    public float moveSpeed = 2.5f;
    [Tooltip("Radius of the cylinder. Sets how fast it spins, how high enemies hop over it, " +
             "and how close two rollers get before bouncing apart.")]
    public float radius = 0.4f;

    [Header("Enemies Vaulting Over It")]
    [Tooltip("How fast enemies leap across the roller (on top of its own speed). Faster means lower, shorter jumps.")]
    public float vaultSpeed = 2.5f;
    [Tooltip("Gravity for the vault. Higher is snappier but needs a higher jump to clear the roller.")]
    public float vaultGravity = 25f;
    [Tooltip("Extra room between the enemy's feet and the top of the roller.")]
    public float vaultClearance = 0.15f;

    [Header("Lifetime")]
    [Tooltip("Number of cleared waves after which the roller destroys itself.")]
    public int wavesToSurvive = 3;
}
