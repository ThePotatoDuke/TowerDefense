using System;
using UnityEngine;

// Central, Inspector-tunable settings for all visual effects.
// Lives in Resources so it (and the shaders it references) is always included in builds.
[CreateAssetMenu(fileName = "FXSettings", menuName = "Visual FX/FX Settings")]
public class FXSettings : ScriptableObject
{
    private static FXSettings instance;
    public static FXSettings Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<FXSettings>("FXSettings");
            return instance;
        }
    }

    [Header("Shaders")]
    public Shader spriteFXShader;
    public Shader blobShadowShader;
    public Shader unlitShader;
    public Shader pixelImposterShader;
    public Shader pixelTrailShader;
    public Shader shockwaveShader;
    public Shader imposterSourceShader;

    [Header("Hit Flash")]
    [ColorUsage(false, true)] public Color flashColor = new Color(2f, 2f, 2f, 1f);
    public float flashDuration = 0.12f;

    [Header("Death Dissolve")]
    [ColorUsage(false, true)] public Color dissolveEdgeColor = new Color(4f, 1.6f, 0.4f, 1f);
    [Range(0f, 0.5f)] public float dissolveEdgeWidth = 0.12f;
    public float dissolveDuration = 0.45f;

    [Header("Blob Shadow")]
    public Color shadowColor = new Color(0f, 0f, 0f, 0.45f);
    [Tooltip("Shadow diameter relative to the sprite's width.")]
    public float shadowSizeMultiplier = 0.7f;
    [Tooltip("Height above ground at which the shadow reaches its smallest/faintest.")]
    public float shadowMaxHeight = 2f;
    [Tooltip("Where the shadow sits under a sprite: 0 = under its feet, 1 = under its center. Updates live.")]
    [Range(0f, 1f)] public float shadowAnchor = 0.3f;

    [Header("Castle Hit")]
    public float castleShakeDuration = 0.25f;
    public float castleShakeStrength = 0.15f;
    public int castleDebrisCount = 16;
    public float castleDebrisSize = 0.12f;
    public Color castleDebrisColorA = new Color(0.62f, 0.6f, 0.56f, 1f);
    public Color castleDebrisColorB = new Color(0.32f, 0.29f, 0.27f, 1f);
    [ColorUsage(false, true)] public Color castleLowHealthColor = new Color(1.6f, 0.15f, 0.1f, 1f);
    [Range(0f, 1f)] public float castleLowHealthThreshold = 0.3f;

    [Header("Castle Collapse (game over)")]
    [Tooltip("Seconds after the castle falls before it starts collapsing, so the camera arrives first.")]
    public float castleCollapseDelay = 1f;
    [Tooltip("Seconds it rumbles in place before sinking.")]
    public float castleCollapseRumble = 0.7f;
    [Tooltip("Seconds to sink fully into the ground.")]
    public float castleCollapseSinkTime = 2f;
    [Tooltip("How far it snaps side to side while collapsing.")]
    public float castleCollapseShake = 0.08f;
    [Tooltip("Dust and rubble particles per second along the base.")]
    public float castleCollapseDust = 60f;

    // Total collapse time, for timing the game-over popup after it
    public float CastleCollapseDuration => castleCollapseDelay + castleCollapseRumble + castleCollapseSinkTime;

    [Header("Pickup Shine")]
    [ColorUsage(false, true)] public Color shineColor = new Color(2.5f, 2.5f, 2.5f, 1f);
    public float shineSpeed = 40f;
    public float shineSpacing = 60f;
    public float shineWidth = 4f;

    [Header("Projectiles")]
    public float projectileEmission = 2.5f;
    [ColorUsage(true, true)] public Color projectileTrailColor = new Color(3f, 2.2f, 0.8f, 1f);
    public float projectileTrailTime = 0.12f;
    public float projectileTrailWidth = 0.08f;

    [Header("Sword Swoosh")]
    [ColorUsage(true, true)] public Color swooshColor = new Color(2f, 2f, 2f, 1f);
    public float swooshTime = 0.2f;
    [Tooltip("Trail width relative to the blade length.")]
    public float swooshWidth = 0.8f;

    [Header("Bat Trail (color is per bat, in its BatDataSO)")]
    public float batTrailTime = 0.3f;
    public float batTrailWidth = 0.45f;
    [Range(0f, 1f)] public float batTrailAlpha = 0.55f;
    [Tooltip("Trail pixel size in world units; 1/16 matches the 16 PPU enemy sprites.")]
    public float batTrailPixelSize = 0.0625f;
    [Tooltip("Number of opacity levels in the trail's fade.")]
    public int batTrailAlphaSteps = 4;
    [Tooltip("Pixels shed per unit flown, on top of the trail.")]
    public float batPixelDensity = 4f;
    public float batPixelSize = 0.1f;
    public float batPixelLifetime = 0.6f;

    [Header("Player Death")]
    [Tooltip("Real seconds the game freezes on the killing hit.")]
    public float deathHitStop = 0.35f;
    [Tooltip("Game speed right after the freeze; eases back to normal.")]
    [Range(0.05f, 1f)] public float deathSlowMotion = 0.3f;
    [Tooltip("Real seconds to ease from slow motion back to normal speed.")]
    public float deathSlowMotionRecovery = 1.2f;
    public float deathZoomFieldOfView = 45f;
    [Tooltip("Camera distance multiplier while zoomed in on the player.")]
    [Range(0.2f, 1f)] public float deathZoomDistance = 0.7f;
    [Tooltip("Real seconds for the camera to ease in on the player.")]
    public float deathZoomDuration = 1.2f;
    [Tooltip("Real seconds for the drained colors and vignette to fade in.")]
    public float deathScreenFadeDuration = 0.3f;
    public float deathShakeForce = 0.5f;
    public float deathShakeDuration = 0.45f;
    [Range(-100f, 0f)] public float deathSaturation = -70f;
    [Range(0f, 1f)] public float deathVignette = 0.45f;
    [Range(0f, 1f)] public float deathChromaticAberration = 0.6f;

    [Header("Pixelated 3D Models (spike balls)")]
    [Tooltip("An unused layer. Models are moved onto it so only their low-res imposter camera draws them.")]
    public int pixelArtLayer = 9;
    [Tooltip("Imposter resolution; 20 matches the sprites (16 px over 0.8 units).")]
    public float imposterPixelsPerUnit = 20f;
    public Color imposterOutlineColor = new Color(0.08f, 0.06f, 0.1f, 1f);

    [Header("Roller Sparks")]
    public int sparkCount = 45;
    public float sparkSize = 0.2f;
    public float sparkSpeed = 10f;
    [Tooltip("Size of the bright flash at the point of impact.")]
    public float sparkFlashSize = 1.4f;
    public Color sparkColorA = new Color(1f, 0.95f, 0.7f, 1f);
    public Color sparkColorB = new Color(1f, 0.55f, 0.15f, 1f);
    [Tooltip("HDR multiplier on top of the spark colors so they glow with bloom.")]
    [ColorUsage(false, true)] public Color sparkGlow = new Color(3f, 3f, 3f, 1f);

    // Shared runtime materials, created on first use
    [NonSerialized] private Material projectileTrailMaterial;
    [NonSerialized] private Material swooshMaterial;
    [NonSerialized] private Material debrisMaterial;
    [NonSerialized] private Material batTrailMaterial;
    [NonSerialized] private Material batPixelsMaterial;
    [NonSerialized] private Material sparkMaterial;

    public Material ProjectileTrailMaterial => GetUnlitMaterial(ref projectileTrailMaterial, projectileTrailColor, "ProjectileTrail");
    public Material SwooshMaterial => GetUnlitMaterial(ref swooshMaterial, swooshColor, "SwordSwoosh");
    public Material DebrisMaterial => GetUnlitMaterial(ref debrisMaterial, Color.white, "CastleDebris");
    public Material SparkMaterial => GetUnlitMaterial(ref sparkMaterial, sparkGlow, "Sparks");
    // Fraction of the bat trail's width kept empty on each side, so edge blocks aren't clipped
    public const float BatTrailInset = 0.25f;

    // White, so each bat's color comes from vertex colors; both draw before sprites to stay behind them
    public Material BatTrailMaterial
    {
        get
        {
            if (batTrailMaterial == null && pixelTrailShader != null)
            {
                batTrailMaterial = new Material(pixelTrailShader) { name = "BatTrail (Runtime)" };
                batTrailMaterial.SetFloat("_PixelSize", batTrailPixelSize);
                batTrailMaterial.SetFloat("_AlphaSteps", batTrailAlphaSteps);
                batTrailMaterial.SetFloat("_Inset", BatTrailInset);
            }
            return batTrailMaterial;
        }
    }

    public Material BatPixelsMaterial
    {
        get
        {
            Material material = GetUnlitMaterial(ref batPixelsMaterial, Color.white, "BatPixels");
            if (material != null)
                material.renderQueue = 2999; // Transparent-1, like the trail
            return material;
        }
    }

    private Material GetUnlitMaterial(ref Material material, Color color, string label)
    {
        if (material == null && unlitShader != null)
        {
            material = new Material(unlitShader) { name = $"{label} (Runtime)" };
            material.SetColor("_Color", color);
        }
        return material;
    }

    // Opaque at the head, fading out toward the tail
    public static Gradient FadeOutGradient()
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        return gradient;
    }
}
