using System;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private GameObject hasHealthGameobject;

    private IHasHealth hasHealth;
    [SerializeField] private Image barImage; // drag Bar child here
    [Tooltip("The bar with its outline; the shield sits just left of it.")]
    [SerializeField] private GameObject barRoot;
    [Tooltip("Shown, shining, beside the bar when the owner is immune to ranged attacks.")]
    [SerializeField] private SpriteRenderer shield;
    [Tooltip("Gap between the shield and the left end of the bar.")]
    [SerializeField] private float shieldGap = 0.06f;
    [Tooltip("Seconds the bar and shield stay up after a ranged attack bounces off an unhurt owner.")]
    [SerializeField] private float deflectRevealTime = 1.5f;

    private float healthRatio = 1f;
    private EnemyBase shieldedEnemy;

    // The bar stays in the same camera-facing plane as its owner's sprite. Both lean back toward the
    // camera, but around different pivots (the sprite's feet, the bar's middle), so left alone the bar
    // sits a little in front of or behind the sprite, and with the perspective camera it drifts against
    // it as they move across the screen. Measured from the sprite's pivot as laid out in the editor.
    private Vector3 pivotInParent;  // the sprite's pivot, in the parent's space
    private Vector3 offsetFromPivot; // where the bar sits relative to it, in world units, before leaning

    private void Awake()
    {
        Transform parent = transform.parent;
        if (parent == null) return;

        // The owner's camera-facing visual: the first one under the parent that isn't this bar
        Transform pivot = parent;
        foreach (LookAtCamera billboard in parent.GetComponentsInChildren<LookAtCamera>(true))
        {
            if (billboard.transform.IsChildOf(transform)) continue;
            pivot = billboard.transform;
            break;
        }
        pivotInParent = parent.InverseTransformPoint(pivot.position);
        offsetFromPivot = transform.position - pivot.position;
    }

    // Shield just left of the bar for owners that ranged attacks bounce off, with the pickup shine
    private void SetUpShield()
    {
        if (shield == null) return;

        GameObject owner = hasHealthGameobject != null ? hasHealthGameobject : transform.parent != null ? transform.parent.gameObject : null;
        var enemy = owner != null ? owner.GetComponentInParent<EnemyBase>() : null;
        bool immune = enemy != null && enemy.ImmuneToRanged;
        shield.gameObject.SetActive(immune);
        if (!immune) return;

        // Bounced shots show the bar and shield for a moment, hurt or not
        shieldedEnemy = enemy;
        shieldedEnemy.OnDeflected += RevealAfterDeflect;

        if (barRoot != null && barRoot.transform is RectTransform bar && shield.sprite != null)
        {
            float barLeft = bar.localPosition.x - bar.rect.width * bar.pivot.x;
            float shieldHalfWidth = shield.sprite.bounds.extents.x * shield.transform.localScale.x;
            shield.transform.localPosition = new Vector3(barLeft - shieldGap - shieldHalfWidth, bar.localPosition.y, 0f);
        }
        shield.gameObject.GetOrAddComponent<SpriteFX>().SetShine(true);
    }

    private void LateUpdate()
    {
        Transform parent = transform.parent;
        Camera camera = Camera.main;
        if (parent == null || camera == null) return;

        // Up the leaning plane by the same height, instead of straight up
        Vector3 pivot = parent.TransformPoint(pivotInParent);
        Vector3 across = new Vector3(offsetFromPivot.x, 0f, offsetFromPivot.z);
        transform.position = pivot + across + camera.transform.up * offsetFromPivot.y;
    }

    void Start()
    {
        // The owner set in the Inspector, or else whatever this bar sits on
        hasHealth = hasHealthGameobject != null ? hasHealthGameobject.GetComponent<IHasHealth>() : GetComponentInParent<IHasHealth>();

        if (hasHealth == null)
        {
            Debug.LogError("Game Object " + hasHealthGameobject + "does not have a component that implements IHasHealth");
            return;
        }

        hasHealth.OnHealthChanged += HasProgress_OnHealthChanged;
        // In Start, so a spawner has had the chance to make the owner immune
        SetUpShield();
        Hide();
    }

    private void OnDestroy()
    {
        if (shieldedEnemy != null)
            shieldedEnemy.OnDeflected -= RevealAfterDeflect;
    }

    private void RevealAfterDeflect()
    {
        if (healthRatio <= 0f) return;

        Show();
        CancelInvoke(nameof(HideIfUnhurt));
        Invoke(nameof(HideIfUnhurt), deflectRevealTime);
    }

    // Back down after a bounce, unless it has taken damage meanwhile
    private void HideIfUnhurt()
    {
        if (healthRatio >= 1f)
            Hide();
    }

    private void HasProgress_OnHealthChanged(float healthRatio)
    {
        this.healthRatio = healthRatio;
        barImage.fillAmount = healthRatio;
        if (healthRatio == 0 || healthRatio == 1)
        {
            Hide();
        }
        else
        {
            Show();
        }
        barImage.fillAmount = healthRatio;
    }
    // The shield, if any, comes and goes with the bar
    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
