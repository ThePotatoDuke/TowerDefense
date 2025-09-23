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
    public float springK = 50f;
    public float damping = 18f;

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
    public float maxSpeed;
    public float target;

}

