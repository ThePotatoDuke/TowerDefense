using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }
    [Header("Settings")]
    [SerializeField] private float movementMultiplier = 5f;
    [SerializeField] private GameInput gameInput;

    private Vector2 inputVector;
    private void Awake()
    {
        Instance = this;
    }
    public bool IsWalking()
    {
        return inputVector != Vector2.zero;
    }
    void Update()
    {
        HandleMovement();
    }
    public void HandleMovement()
    {
        // get normalized input
        inputVector = gameInput.GetMovementVectorNormalized();

        Vector3 moveDir = new Vector3(inputVector.x, 0, inputVector.y);

        float playerRadius = 0.7f;
        float playerHeight = 2f;
        float moveDistance = movementMultiplier * Time.deltaTime;

        bool canMove = !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight, playerRadius, moveDir, moveDistance);

        if (canMove) // move the player
        {
            transform.position += moveDistance * moveDir;
        }
        else
        {
            //TRY TO MOVE ONLY ON THE HORIZONTAL
            Vector3 moveDirX = new Vector3(moveDir.x, 0, 0).normalized;
            canMove = !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight, playerRadius, moveDirX, moveDistance);
            if (canMove)
            {
                transform.position += moveDistance * moveDirX;
            }
            else // CAN'T MOVE HORIZONTAL ONLY TRY VERTICAL ONLY
            {
                Vector3 moveDirZ = new Vector3(0, 0, moveDir.z).normalized;
                canMove = !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight, playerRadius, moveDirX, moveDistance);
                if (canMove)
                {
                    transform.position += moveDistance * moveDirZ;
                }
            }

        }
    }
}
