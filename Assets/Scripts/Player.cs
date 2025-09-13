using System;
using DG.Tweening;
using UnityEngine;

public class Player : MonoBehaviour, IHasHealth
{
    public static Player Instance { get; private set; }

    public enum PlayerState { Idle, Walking, Attacking, Dead }
    private PlayerState currentState = PlayerState.Idle;
    private float currentHealth;
    public event Action OnDied;

    [Header("Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float maxHealth = 10f;

    [Header("References")]
    [SerializeField] private GameInput gameInput;
    [SerializeField] private SpriteRenderer spriteRenderer;

    public float CurrentHealth => currentHealth;

    public float MaxHealth => maxHealth;
    public event Action<PlayerState> OnStateChanged;
    public event Action<float> OnHealthChanged;

    private Rigidbody rb;
    private Vector3 moveDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // If an instance already exists (from previous Play session), destroy it
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        currentHealth = maxHealth;
    }

    private void Update()
    {
        // Read input from GameInput
        Vector2 inputVector = gameInput.GetMovementVectorNormalized();

        moveDirection = new Vector3(inputVector.x, 0f, inputVector.y);

        // Flip sprite horizontally based on X movement
        if (moveDirection.x > 0.01f) // moving right
            spriteRenderer.flipX = false;
        else if (moveDirection.x < -0.01f) // moving left
            spriteRenderer.flipX = true;


        if (IsDead())
            SetState(PlayerState.Dead);
        else if (IsAttacking())
            SetState(PlayerState.Attacking);
        else if (IsWalking())
            SetState(PlayerState.Walking);
        else
            SetState(PlayerState.Idle);
    }

    private void SetState(PlayerState newState)
    {
        if (newState == currentState) return;
        currentState = newState;
        OnStateChanged?.Invoke(newState); // fire event
    }

    private void FixedUpdate()
    {
        if (moveDirection.magnitude > 0.01f)
        {
            // Move the player
            Vector3 targetPosition = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(targetPosition);
        }
    }
    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
    }
    // Dummy implementations for now
    public bool IsDead() => currentState == PlayerState.Dead;
    public bool IsAttacking() => false;
    public bool IsWalking() => moveDirection.magnitude > 0.01f;

    private void Die()
    {
        OnDied?.Invoke();  // notify listeners
        SetState(PlayerState.Dead);

        var lookAt = GetComponent<LookAtCamera>();
        if (lookAt != null) lookAt.enabled = false;

        Sequence deathSequence = DOTween.Sequence();

        // Spin 720° around current Y
        deathSequence.Append(transform.DORotate(new Vector3(0, transform.eulerAngles.y + 720f, 0), 1f, RotateMode.FastBeyond360).SetEase(Ease.OutQuart));
        // Spin 720° around current Y

        deathSequence.Append(transform.DORotate(new Vector3(90, transform.eulerAngles.y, 0), 0.5f, RotateMode.FastBeyond360).SetEase(Ease.OutBounce));
    }
}
