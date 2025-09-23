using UnityEngine;
using DG.Tweening;

public class SpriteFlipper : MonoBehaviour
{
    [SerializeField] private Transform spriteTransform;
    [SerializeField] private float flipDuration;

    private Vector3 lastPos;
    private enum Facing { Right, Left }
    private Facing currentFacing;

    private void Awake()
    {
        currentFacing = Facing.Right;
        lastPos = transform.position;

        if (flipDuration <= 0f)
            flipDuration = Constants.flipDuration;
    }

    private void Update()
    {
        HandleFlip();
    }

    private void HandleFlip()
    {
        Vector3 movement = transform.position - lastPos;
        if (movement.sqrMagnitude > 0.000001f)
        {
            Facing newFacing = movement.x >= 0 ? Facing.Right : Facing.Left;
            if (newFacing != currentFacing)
            {
                currentFacing = newFacing;
                Flip(currentFacing);
            }
        }
        lastPos = transform.position;
    }

    private void Flip(Facing dir)
    {
        float yRot = (dir == Facing.Right) ? 0f : 180f;
        spriteTransform.DOLocalRotate(new Vector3(0, yRot, 0), flipDuration, RotateMode.Fast);
    }
}
