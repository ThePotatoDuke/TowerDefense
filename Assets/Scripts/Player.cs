using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public class Player : MonoBehaviour, IHasHealth
{
    public static Player Instance { get; private set; }

    public enum PlayerState { Idle, Walking, Attacking, Dead }
    private PlayerState currentState = PlayerState.Idle;

    private float currentHealth;
    public bool IsInvulnerable { get; private set; }

    public event Action<Vector3?> OnTakeDamage;

    public event Action OnDied;
    public event Action<PlayerState> OnStateChanged;
    public event Action<float> OnHealthChanged;

    public WeaponBase currentWeapon;       // Currently equipped weapon

    [Header("Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float maxHealth = 10f;
    [SerializeField] private float iFrameDuration = 1.5f;
    [SerializeField] private float blinkInterval = 0.1f;

    [Header("References")]
    [SerializeField] private GameInput gameInput;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform playerVisualTransform;
    [SerializeField] private Transform pivotBottomTransform;
    [SerializeField] private Transform weaponHand;
    private Rigidbody rb;
    private Vector3 moveDirection;
    private Color originalColor;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        currentHealth = maxHealth;

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        // Assign weaponHand automatically if not assigned
        if (weaponHand == null)
        {
            weaponHand = transform.Find("PlayerHand"); // Replace with actual child name
            if (weaponHand == null)
                Debug.LogWarning("PlayerHand transform not found!");
        }

    }

    private void Update()
    {
        Vector2 inputVector = gameInput.GetMovementVectorNormalized();
        moveDirection = new Vector3(inputVector.x, 0f, inputVector.y);

        if (currentState != PlayerState.Dead)
        {
            // Flip sprite
            if (moveDirection.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (moveDirection.x < -0.01f)
                spriteRenderer.flipX = true;
        }


        // State updates
        if (IsDead())
            SetState(PlayerState.Dead);
        else if (IsAttacking())
            SetState(PlayerState.Attacking);
        else if (IsWalking())
            SetState(PlayerState.Walking);
        else
            SetState(PlayerState.Idle);
    }

    private void FixedUpdate()
    {
        if (!IsDead())
        {

            Vector3 desiredVelocity = moveDirection * moveSpeed;

            Vector3 velocity = new Vector3(desiredVelocity.x, rb.linearVelocity.y, desiredVelocity.z);

            rb.linearVelocity = velocity;
        }
    }



    public void TakeDamage(float amount)
    {
        if (IsInvulnerable || IsDead()) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);


        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(InvulnerabilityRoutine());
    }

    private IEnumerator InvulnerabilityRoutine()
    {
        IsInvulnerable = true;

        float elapsed = 0f;
        bool visible = true;

        while (elapsed < iFrameDuration)
        {
            if (spriteRenderer != null)
            {
                visible = !visible;
                spriteRenderer.color = visible
                    ? originalColor
                    : new Color(originalColor.r, originalColor.g, originalColor.b, 0.3f);
            }

            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        IsInvulnerable = false;
    }

    private void SetState(PlayerState newState)
    {
        if (newState == currentState) return;
        currentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    public bool IsDead() => currentState == PlayerState.Dead;
    public bool IsAttacking() => false;
    public bool IsWalking() => moveDirection.magnitude > 0.01f;

    private void Die()
    {
        OnDied?.Invoke();
        SetState(PlayerState.Dead);

        // Disable LookAtCamera
        var lookAt = playerVisualTransform.GetComponent<LookAtCamera>();
        if (lookAt != null)
            lookAt.enabled = false;

        // Use pivot bottom for rotation
        Transform pivot = pivotBottomTransform;

        Sequence deathSequence = DOTween.Sequence();


        // Spin around Y axis 720°
        deathSequence.Append(pivot.DOLocalRotate(
            new Vector3(0, 720f, 0),
            1f,
            RotateMode.LocalAxisAdd
        ).SetEase(Ease.OutQuart));

        // Assume playerVisualTransform has the idle tilt
        float initialTiltX = playerVisualTransform.localRotation.eulerAngles.x;

        // Final X should be 90° minus the tilt
        float targetX = 90f - initialTiltX;

        // Fall forward to flat
        deathSequence.Append(pivot.DOLocalRotate(
            new Vector3(targetX, 0f, 0f),
            0.5f
        ).SetEase(Ease.OutBounce));

    }

    public void EquipWeapon(GameObject weaponPrefab)
    {
        // Remove old weapon
        if (currentWeapon != null)
            Destroy(currentWeapon.gameObject);

        // Spawn new weapon
        GameObject weaponObj = Instantiate(weaponPrefab, weaponHand.position, weaponHand.rotation, weaponHand);
        currentWeapon = weaponObj.GetComponent<WeaponBase>();
        currentWeapon.Initialize(gameObject, weaponHand); // <-- this is key
    }

}
