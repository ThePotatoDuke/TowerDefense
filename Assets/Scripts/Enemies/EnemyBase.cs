using System;
using UnityEngine;
using UnityEngine.Splines;
using DG.Tweening;

public abstract class EnemyBase : MonoBehaviour, IHasHealth, IEnemy
{
    // --- Health ---
    protected float currentHealth;
    public abstract EnemyDataSO Data { get; }
    public float CurrentHealth => currentHealth;
    public float MaxHealth => Data.maxHealth;
    public float ContactDamage => Data.contactDamage;

    public event Action<float> OnHealthChanged;
    public event Action OnDied;

    [Header("Visuals")] protected Transform visual;
    private Color originalColor;
    private Renderer meshRenderer;
    private SpriteRenderer spriteRenderer;

    [Header("Spline Movement")]
    [SerializeField] protected SplineContainer splineContainer;
    protected float distancePercentage = 0f;
    public float DistancePercentage => distancePercentage;

    protected bool hasReachedEnd = false;
    protected float attackInterval = 1f; // seconds between attacks
    private float attackTimer = 0f;

    // --- Helpers ---
    public void AssignSpline(SplineContainer container)
    {
        splineContainer = container;
        distancePercentage = 0f;
        hasReachedEnd = false;
    }

    public void SetHealthMultiplier(float multiplier)
    {
        currentHealth = MaxHealth * multiplier;
    }

    // --- Unity ---
    protected virtual void Awake()
    {
        currentHealth = Data.maxHealth;

        // Find visual automatically
        if (visual == null)
        {
            // Try first SpriteRenderer
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                visual = spriteRenderer.transform;
                originalColor = spriteRenderer.color;
            }
            else
            {
                // Try first MeshRenderer (3D)
                meshRenderer = GetComponentInChildren<Renderer>();
                if (meshRenderer != null)
                {
                    visual = meshRenderer.transform;
                    originalColor = meshRenderer.material.color;
                }
            }
        }
    }

    protected virtual void OnEnable()
    {
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.RegisterEnemy(this);
    }

    protected virtual void OnDisable()
    {
        EnemyManager.Instance?.UnregisterEnemy(this);
        StopAllTweens(); // prevent DOTween errors
    }

    // --- Movement ---
    public abstract void Move(); // Each enemy implements its own movement logic

    protected virtual void Update()
    {
        // Update position along spline
        if (splineContainer != null)
        {
            transform.position = splineContainer.EvaluatePosition(distancePercentage);
        }

        // Check if enemy reached end
        if (!hasReachedEnd && distancePercentage >= 0.999f)
        {
            ReachEnd();
        }

        // Handle repeated castle damage
        if (hasReachedEnd)
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackInterval)
            {
                attackTimer = 0f;
                OnEnemyReachedEnd?.Invoke(this);
            }
        }
    }

    // --- Reached end logic ---
    protected void ReachEnd()
    {
        if (hasReachedEnd) return;
        hasReachedEnd = true;
        attackTimer = 0f;
    }

    // --- Health ---
    public virtual void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);
        OnHealthChanged?.Invoke(currentHealth / MaxHealth);

        Flash();

        if (currentHealth <= 0)
            Die();
    }

    // --- Hit flash ---
    public virtual void Flash(float duration = 0.1f)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.DOColor(Color.red, duration)
                         .OnComplete(() => spriteRenderer.color = originalColor);
        }
        else if (meshRenderer != null)
        {
            meshRenderer.material.DOColor(Color.red, duration)
                        .OnComplete(() => meshRenderer.material.color = originalColor);
        }
    }

    protected virtual void Die()
    {
        StopAllTweens();
        OnDied?.Invoke();
        Destroy(gameObject, 0.1f);
    }

    // --- Tween cleanup ---
    protected void StopAllTweens()
    {
        if (transform != null) transform.DOKill();
    }

    // --- Event for castle damage ---
    public static event Action<EnemyBase> OnEnemyReachedEnd;
}
