using DG.Tweening;
using UnityEngine;


[CreateAssetMenu(menuName = "Enemies/SpikeBall Data")]
public class SpikeBallDataSO : EnemyDataSO
{
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (offsetUpdateInterval < offsetTweenDuration + 0.01f)
        {
            offsetUpdateInterval = offsetTweenDuration + 0.01f;
            UnityEditor.EditorUtility.SetDirty(this); // marks SO dirty so it saves
        }
    }
#endif

    [Header("Spring Settings")]
    [Tooltip("How hard the ball is pulled toward its target, per unit of distance along the path.")]
    public float springK = 5f;
    [Tooltip("How quickly it settles. Low values overshoot and swing back and forth; " +
             "about 2 x the square root of Spring K stops without overshooting.")]
    public float damping = 4f;

    [Header("Movement")]
    public float ballRadius = 0.5f;

    [Tooltip("Ease used for the bouncy offset tween.")]
    public Ease bouncyEase = Ease.OutBack;
    [Tooltip("Duration (seconds) of the tween to the new target.")]
    public float offsetTweenDuration = 0.2f;
    [Tooltip("How often (seconds) the target offset is updated. Must be smaller than offsetUpdateInterval")]
    public float offsetUpdateInterval = 0.5f;

    [Header("Bouncy Offset")]
    [Tooltip("Small random offset added to average enemy progress. Typically between -0.05 and 0.05.")]
    public float bouncyOffset = 0.03f;
    [Tooltip("Top speed along the path, in units per second.")]
    public float maxSpeed = 2.5f;

    [Header("Lifetime")]
    [Tooltip("Number of cleared waves after which the spike ball destroys itself.")]
    public int wavesToSurvive = 2;

    [Header("Spike Burst (when destroyed)")]
    [Tooltip("Spikes fired at equal angles when the ball is destroyed by damage. 0 = no burst.")]
    public int deathSpikeCount = 0;
    public Sprite spikeSprite;
    [Tooltip("Material for the spike sprites (the default sprite material).")]
    public Material spikeMaterial;
    public float spikeSpeed = 7f;
    [Tooltip("How far the spikes fly before vanishing.")]
    public float spikeRange = 5f;
    [Tooltip("Damage to the player when a spike hits them.")]
    public float spikeDamage = 1f;
    [Tooltip("Seconds the warning lines show where the spikes will fly before they're fired.")]
    public float spikeWarningTime = 0.8f;
    [ColorUsage(false, true)] public Color spikeWarningColor = new Color(2f, 0.3f, 0.2f, 1f);
}

