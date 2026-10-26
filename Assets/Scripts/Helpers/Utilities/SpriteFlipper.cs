using UnityEngine;
using DG.Tweening;

public class SpriteFlipper : MonoBehaviour
{
    [SerializeField] private Transform spriteTransform;
    [SerializeField] private float flipDuration;
    [Tooltip("Transform whose movement decides the facing. Defaults to this one; bats use their root so weaving doesn't flip them.")]
    [SerializeField] private Transform motionSource;
    [Tooltip("Slowest sideways movement (on screen, units per second) that turns it; slower drifting doesn't.")]
    [SerializeField] private float minTurnSpeed = 0.2f;

    private Vector3 lastPos;
    private enum Facing { Right, Left }
    private Facing currentFacing;
    private Tween flipTween;

    private void Awake()
    {
        if (motionSource == null)
            motionSource = transform;

        currentFacing = Facing.Right;
        lastPos = motionSource.position;

        if (flipDuration <= 0f)
            flipDuration = Constants.flipDuration;
    }

    private void Update()
    {
        HandleFlip();
    }

    private void HandleFlip()
    {
        Vector3 movement = motionSource.position - lastPos;
        lastPos = motionSource.position;

        // Let a turn finish before deciding on the next one; starting another halfway cuts it off
        if (flipTween != null && flipTween.IsActive()) return;
        if (Time.deltaTime <= 0f) return;

        // Left or right as seen on screen, ignoring tiny sideways drift (fast enemies on bends wobble)
        Camera view = Camera.main;
        Vector3 screenRight = view != null ? view.transform.right : Vector3.right;
        float sidewaysSpeed = Vector3.Dot(movement, screenRight) / Time.deltaTime;
        if (Mathf.Abs(sidewaysSpeed) < minTurnSpeed) return;

        Facing newFacing = sidewaysSpeed > 0f ? Facing.Right : Facing.Left;
        if (newFacing != currentFacing)
        {
            currentFacing = newFacing;
            Flip(currentFacing);
        }
    }

    private void Flip(Facing dir)
    {
        float yRot = (dir == Facing.Right) ? 0f : 180f;
        flipTween = spriteTransform.DOLocalRotate(new Vector3(0, yRot, 0), flipDuration, RotateMode.Fast)
                                   .SetLink(gameObject);
    }
}
