using UnityEngine;
using UnityEngine.UI;

// Warning sign at the edge of the screen, on the side the castle is, while the castle is being hit and
// can't be seen. It goes away when the castle comes into view or nothing has hit it for a while. Once the
// castle is low on health (flashing red) the sign flashes faster and pulses in size. Built from code on
// the screen-space UI canvas; added to the castle by CastleHitFX. Settings are under Castle Warning in
// FXSettings.
[RequireComponent(typeof(CastleHealth))]
public class CastleWarning : MonoBehaviour
{
    private CastleHealth castle;
    private Renderer castleSprite;
    private RectTransform canvasRect;
    private Canvas canvas;
    private Image sign;
    private float lastHitTime = float.NegativeInfinity;
    private float healthRatio = 1f;
    private float appear; // 0 hidden, 1 fully shown; it pops in and out

    private void Awake()
    {
        castle = GetComponent<CastleHealth>();
        castleSprite = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable() => castle.OnHealthChanged += HandleHealthChanged;
    private void OnDisable() => castle.OnHealthChanged -= HandleHealthChanged;

    private void Start()
    {
        FXSettings settings = FXSettings.Instance;
        if (settings == null || settings.castleWarningFrames == null || settings.castleWarningFrames.Length == 0) return;

        // The HUD: a screen-space canvas, not one of the world-space health bars
        foreach (Canvas candidate in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
        {
            if (candidate.isRootCanvas && candidate.renderMode != RenderMode.WorldSpace)
            {
                canvas = candidate;
                break;
            }
        }
        if (canvas == null) return;
        canvasRect = (RectTransform)canvas.transform;

        sign = new GameObject("CastleWarning", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        sign.rectTransform.SetParent(canvasRect, false);
        sign.rectTransform.SetAsLastSibling(); // over the rest of the HUD
        sign.rectTransform.anchorMin = sign.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        sign.rectTransform.sizeDelta = Vector2.one * settings.castleWarningSize;
        sign.sprite = settings.castleWarningFrames[0];
        sign.raycastTarget = false;
        sign.gameObject.SetActive(false);
    }

    private void HandleHealthChanged(float ratio)
    {
        if (ratio < healthRatio)
            lastHitTime = Time.time;
        healthRatio = ratio;
    }

    private void LateUpdate()
    {
        if (sign == null) return;
        FXSettings settings = FXSettings.Instance;

        bool underAttack = healthRatio > 0f && Time.time - lastHitTime < settings.castleWarningHideAfter;
        bool offScreen = TryGetEdgePoint(settings, out Vector2 screenPoint);
        bool show = underAttack && offScreen && GameStateManager.CurrentState == GameState.Playing;

        appear = Mathf.MoveTowards(appear, show ? 1f : 0f, Time.deltaTime * 8f);
        sign.gameObject.SetActive(appear > 0f);
        if (appear <= 0f) return;

        if (offScreen && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out Vector2 local))
            sign.rectTransform.anchoredPosition = local;

        // Flashing, faster and pulsing once the castle is low (when it flashes red itself)
        bool urgent = healthRatio <= settings.castleLowHealthThreshold;
        float framesPerSecond = urgent ? settings.castleWarningUrgentFramesPerSecond : settings.castleWarningFramesPerSecond;
        Sprite[] frames = settings.castleWarningFrames;
        sign.sprite = frames[(int)(Time.time * framesPerSecond) % frames.Length];

        float pulse = urgent ? settings.castleWarningUrgentPulse * Mathf.Abs(Mathf.Sin(Time.time * Mathf.PI * settings.castleWarningUrgentPulseSpeed)) : 0f;
        sign.rectTransform.localScale = Vector3.one * (Mathf.SmoothStep(0f, 1f, appear) * (1f + pulse));
    }

    // Where on the screen's edge to put the sign: toward the castle from the middle of the screen. False
    // when the castle is on screen.
    private bool TryGetEdgePoint(FXSettings settings, out Vector2 screenPoint)
    {
        screenPoint = default;
        Camera view = Camera.main;
        if (view == null || castleSprite == null) return false;

        Vector3 onScreen = view.WorldToViewportPoint(castleSprite.bounds.center);
        bool behind = onScreen.z < 0f;
        if (!behind && onScreen.x >= 0f && onScreen.x <= 1f && onScreen.y >= 0f && onScreen.y <= 1f)
            return false;

        // Direction from the middle of the screen, in pixels (flipped if the castle is behind the camera)
        Vector2 direction = new Vector2((onScreen.x - 0.5f) * Screen.width, (onScreen.y - 0.5f) * Screen.height);
        if (behind)
            direction = -direction;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.down;

        // Out along it until it meets the edge, less the margin and half the sign
        float margin = (settings.castleWarningSize * 0.5f + settings.castleWarningEdgeMargin) * canvas.scaleFactor;
        float halfWidth = Mathf.Max(0f, Screen.width * 0.5f - margin);
        float halfHeight = Mathf.Max(0f, Screen.height * 0.5f - margin);
        float scale = Mathf.Min(
            Mathf.Abs(direction.x) > 0.0001f ? halfWidth / Mathf.Abs(direction.x) : float.MaxValue,
            Mathf.Abs(direction.y) > 0.0001f ? halfHeight / Mathf.Abs(direction.y) : float.MaxValue);

        screenPoint = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) + direction * scale;
        return true;
    }
}
