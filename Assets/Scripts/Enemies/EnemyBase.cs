using System;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using DG.Tweening;

public abstract class EnemyBase : MonoBehaviour, IHasHealth, IEnemy
{
    // --- Health ---
    protected float currentHealth;
    private float healthMultiplier = 1f;
    protected bool isDead = false;
    public abstract EnemyDataSO Data { get; }
    public float CurrentHealth => currentHealth;
    public float MaxHealth => Data.maxHealth * healthMultiplier;
    public float ContactDamage => Data.contactDamage;
    public bool IsDead => isDead;

    // Support enemies (e.g. spike balls) override these
    public virtual bool BlocksWaveProgress => true;
    protected virtual bool DamagesCastle => true;
    // Flying enemies (bats) can only be hit by weapons that aim up
    public virtual bool IsFlying => false;
    // Enemies hop over rollers in their way; things already in the air or that are rollers themselves don't
    public virtual bool CanDodge => !IsFlying;
    [Tooltip("Ranged attacks bounce off it, flying away upward at a random angle. Shown by the pickup " +
             "shine on the enemy and a shining shield beside its health bar. Wave entries can also turn it on.")]
    [SerializeField] private bool immuneToRanged;
    public bool ImmuneToRanged => immuneToRanged;

    public event Action<float> OnHealthChanged;
    public event Action OnDied;
    // A ranged attack just bounced off it
    public event Action OnDeflected;

    [Header("Visuals")] protected Transform visual;
    private Color originalColor;
    private Renderer meshRenderer;
    private SpriteRenderer spriteRenderer;
    private SpriteFX spriteFX;
    // The sprite (or mesh) that shows this enemy
    public Renderer MainRenderer => spriteRenderer != null ? spriteRenderer : meshRenderer;

    // Direct child holding the visual; lifted when dodging so the spline-following root stays on the path
    private Transform jumpRoot;
    private Sequence dodgeSequence;
    private float dodgePathVelocity; // units per second along the path while vaulting
    public bool IsDodging => dodgeSequence != null && dodgeSequence.IsActive();
    // Seconds after landing before it can jump again; touching a roller in that moment is what hurts
    private const float LandingRecovery = 0.2f;
    private float landTime = float.NegativeInfinity;
    public bool JustLanded => Time.time - landTime < LandingRecovery;

    [Header("Spline Movement")]
    [SerializeField] protected SplineContainer splineContainer;
    protected float distancePercentage = 0f;
    protected float splineLength = 1f;
    private bool splineAssigned;
    private bool isRegistered;
    public float DistancePercentage => distancePercentage;

    // Smoothed speed along the path in units per second (negative when moving back toward the spawn)
    private float lastProgress = -1f;
    public float PathSpeed { get; private set; }
    // Half the enemy's footprint along the ground, from its colliders
    public float BodyRadius { get; private set; } = 0.3f;

    protected bool hasReachedEnd = false;
    public bool HasReachedEnd => hasReachedEnd;
    protected float attackInterval = 1f; // seconds between attacks
    protected float attackTimer = 0f;

    // --- Helpers ---
    public void AssignSpline(SplineContainer container)
    {
        splineContainer = container;
        splineAssigned = true;
        splineLength = container != null ? container.CalculateLength() : 1f;
        distancePercentage = 0f;
        hasReachedEnd = false;
    }

    // Starts it part-way along the path instead of at the start (e.g. dropped from the sky)
    public void PlaceOnPath(float progress)
    {
        distancePercentage = Mathf.Clamp01(progress);
        lastProgress = distancePercentage;
        if (splineContainer != null)
            transform.position = splineContainer.EvaluatePosition(distancePercentage);
    }

    public void SetHealthMultiplier(float multiplier)
    {
        healthMultiplier = multiplier;
        currentHealth = MaxHealth;
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

        // Sprite enemies get shader-driven hit flash, death dissolve and a ground shadow
        if (spriteRenderer != null)
        {
            spriteFX = gameObject.GetOrAddComponent<SpriteFX>();
            gameObject.GetOrAddComponent<BlobShadow>();
        }

        jumpRoot = GetJumpRoot();

        if (immuneToRanged)
            SetImmuneToRanged(true);

        Collider body = GetComponentInChildren<Collider>();
        float bodyRadius = body != null ? Mathf.Max(body.bounds.extents.x, body.bounds.extents.z) : 0f;
        if (bodyRadius > 0.01f) // bounds can be empty this early on some setups; keep the default then
            BodyRadius = bodyRadius;
    }

    public void NotifyDeflected() => OnDeflected?.Invoke();

    // The same shine as weapon pickups; the health bar shows a shield beside it (checked when it starts)
    public void SetImmuneToRanged(bool immune)
    {
        immuneToRanged = immune;
        if (spriteFX != null)
            spriteFX.SetShine(immune);
    }

    // The direct child that's lifted when dodging; by default the one holding the visual
    protected virtual Transform GetJumpRoot() => FindJumpRoot();

    private Transform FindJumpRoot()
    {
        Transform current = visual;
        while (current != null && current.parent != transform)
            current = current.parent;
        return current;
    }

    protected virtual void OnEnable()
    {
        isRegistered = false;
        RegisterIfNeeded();
    }

    // Enemies placed in the scene by hand can be enabled before the EnemyManager exists; they register
    // on their first update instead. Unregistered enemies are invisible to rollers, waves and blasts.
    private void RegisterIfNeeded()
    {
        if (isRegistered || isDead || EnemyManager.Instance == null) return;
        EnemyManager.Instance.RegisterEnemy(this);
        isRegistered = true;
    }

    protected virtual void OnDisable()
    {
        EnemyManager.Instance?.UnregisterEnemy(this);
        StopAllTweens(); // prevent DOTween errors
    }

    // --- Movement ---
    public abstract void Move(); // Each enemy implements its own movement logic

    // Runs after every enemy's own movement this frame
    protected virtual void LateUpdate()
    {
        if (isDead || Time.deltaTime <= 0f) return;

        if (lastProgress >= 0f)
        {
            // Mid-vault the jump decides where the enemy goes, whatever its own movement did this frame
            if (IsDodging)
                distancePercentage = Mathf.Clamp01(lastProgress + dodgePathVelocity * Time.deltaTime / splineLength);

            float speed = (distancePercentage - lastProgress) * splineLength / Time.deltaTime;
            PathSpeed = Mathf.Lerp(PathSpeed, speed, 1f - Mathf.Exp(-12f * Time.deltaTime));
        }
        lastProgress = distancePercentage;
    }

    protected virtual void Update()
    {
        // Frozen in place while the death effect plays
        if (isDead) return;

        RegisterIfNeeded();
        FindPathIfUnassigned();

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
                if (DamagesCastle)
                    OnEnemyReachedEnd?.Invoke(this);
            }
        }
    }

    // Enemies dragged into the scene by hand (for testing) aren't spawned by the wave manager,
    // so they find the path themselves and start from the closest point on it
    private void FindPathIfUnassigned()
    {
        if (splineAssigned) return;
        splineAssigned = true;

        if (splineContainer == null)
            splineContainer = FindAnyObjectByType<SplineContainer>();
        if (splineContainer == null) return;

        splineLength = splineContainer.CalculateLength();
        float3 localPoint = splineContainer.transform.InverseTransformPoint(transform.position);
        SplineUtility.GetNearestPoint(splineContainer.Spline, localPoint, out _, out float t);
        distancePercentage = t;
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
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);
        OnHealthChanged?.Invoke(currentHealth / MaxHealth);

        Flash();

        if (currentHealth <= 0)
            Die();
    }

    // --- Dodging ---
    // Vault: a jump with the given height and air time, moving along the path at pathVelocity meanwhile.
    // Returns false if it can't jump right now (already in the air, or only just landed).
    public bool Dodge(float height, float duration, float pathVelocity)
    {
        if (isDead || !CanDodge || IsDodging || JustLanded || jumpRoot == null) return false;

        dodgePathVelocity = pathVelocity;
        Transform root = jumpRoot;
        float restY = root.localPosition.y;
        dodgeSequence = DOTween.Sequence().SetLink(gameObject)
            .Append(root.DOLocalMoveY(restY + height, duration * 0.5f).SetEase(Ease.OutQuad))
            .Append(root.DOLocalMoveY(restY, duration * 0.5f).SetEase(Ease.InQuad))
            .OnKill(() =>
            {
                landTime = Time.time;
                if (root != null)
                    root.localPosition = new Vector3(root.localPosition.x, restY, root.localPosition.z);
            });
        return true;
    }

    // Pushed by a roller instead of hopping over it or getting hurt; true if this enemy handles that
    public virtual bool TryBump(Vector3 away, float speed) => false;

    // --- Hit flash ---
    public virtual void Flash(float duration = 0.1f)
    {
        if (spriteFX != null && spriteFX.IsReady)
        {
            spriteFX.Flash();
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.DOColor(Color.red, duration)
                         .SetLink(gameObject)
                         .OnComplete(() => spriteRenderer.color = originalColor);
        }
        else if (meshRenderer != null)
        {
            meshRenderer.material.DOColor(Color.red, duration)
                        .SetLink(gameObject)
                        .OnComplete(() => meshRenderer.material.color = originalColor);
        }
    }

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        StopAllTweens();
        OnDied?.Invoke();

        // No more contact damage or hits while the death effect plays
        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        float deathDuration = PlayDeathEffect();
        if (deathDuration >= 0f)
            Destroy(gameObject, deathDuration);
    }

    // Plays the death effect and returns how long it lasts; the enemy is destroyed after that.
    // Return a negative value to destroy the enemy yourself (e.g. after a longer sequence).
    protected virtual float PlayDeathEffect()
    {
        // Sprites dissolve; meshes (spike balls) shrink away
        float deathDuration = spriteFX != null ? spriteFX.Dissolve() : 0f;
        if (deathDuration <= 0f)
        {
            deathDuration = 0.15f;
            if (visual != null)
                visual.DOScale(Vector3.zero, deathDuration).SetLink(gameObject);
        }
        return deathDuration;
    }

    // --- Tween cleanup ---
    protected void StopAllTweens()
    {
        if (transform != null) transform.DOKill();
    }

    // --- Event for castle damage ---
    public static event Action<EnemyBase> OnEnemyReachedEnd;
}
