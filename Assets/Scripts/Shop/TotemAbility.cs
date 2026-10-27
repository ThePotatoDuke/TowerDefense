using System;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

// The Totem bought in the shop: while owned it stands by the castle and its icon shows on screen. Holding
// T channels: the player stands still, gathering blue sparks, and the totem shines; held long enough, the
// player warps to the totem, using it up. Letting go or getting hurt cancels the warp but keeps the totem.
// One can be owned at a time. Lives on the player; where it stands and the channel time are on the Totem
// prefab.
public class TotemAbility : MonoBehaviour
{
    public static TotemAbility Instance { get; private set; }

    private static readonly Color ChargeColorA = new(0.35f, 0.7f, 1f);
    private static readonly Color ChargeColorB = new(0.75f, 0.92f, 1f);

    private Player player;
    private GameInput input;
    private Totem totem; // the one standing by the castle
    private ParticleSystem charge;
    private float channelStart = -1f;

    public bool HasTotem => totem != null;
    public bool IsChannelling => channelStart >= 0f;
    // 0 to 1 through the channel
    public float ChannelProgress => IsChannelling ? Mathf.Clamp01((Time.time - channelStart) / ChannelTime) : 0f;

    // Owned or lost, and channelling started or stopped
    public event Action OnChanged;

    // The prefab, which holds the numbers
    private static Totem Settings => ShopSettings.Instance != null ? ShopSettings.Instance.totem : null;
    private float ChannelTime => Settings != null ? Mathf.Max(0.05f, Settings.ChannelTime) : 0.8f;

    public void Setup(Player owner, GameInput gameInput)
    {
        Instance = this;
        player = owner;
        input = gameInput;
        player.OnHurt += Cancel;
    }

    private void OnDestroy()
    {
        if (player != null)
            player.OnHurt -= Cancel;
        if (Instance == this)
            Instance = null;
    }

    // Bought in the shop: it appears by the castle
    public void Give()
    {
        if (HasTotem) return;
        if (Settings == null)
        {
            Debug.LogWarning("Totem: no prefab set in ShopSettings (run Tools > Create Shop).");
            return;
        }

        totem = Instantiate(Settings, CastleGround.Spot(Settings.Spot), Quaternion.identity);
        Transform root = totem.transform;
        root.localScale = Vector3.zero;
        root.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetLink(root.gameObject);
        OnChanged?.Invoke();
    }

    // T pressed: starts channelling, which carries on while it's held
    public void Use()
    {
        if (!HasTotem || IsChannelling || player == null || player.IsDead()) return;

        channelStart = Time.time;
        SetCharging(true);
        OnChanged?.Invoke();
    }

    private void Update()
    {
        if (!IsChannelling) return;
        if (player == null || player.IsDead() || GameStateManager.CurrentState != GameState.Playing ||
            (input != null && !input.IsTotemHeld))
        {
            Cancel();
            return;
        }
        if (Time.time - channelStart >= ChannelTime)
            Warp();
    }

    private void Cancel()
    {
        if (!IsChannelling) return;
        channelStart = -1f;
        SetCharging(false);
        OnChanged?.Invoke();
    }

    private void Warp()
    {
        channelStart = -1f;
        SetCharging(false);
        Transform root = totem.transform;

        // Arrive just in front of the totem, on the path side
        Vector3 from = player.transform.position;
        Vector3 to = root.position + (CastleGround.End - root.position).normalized * 0.8f;
        to.y = from.y;

        SparkBurst.Play(from + Vector3.up * 0.4f);
        player.Teleport(to);
        CinemachineCore.OnTargetObjectWarped(player.transform, to - from);
        SparkBurst.Play(to + Vector3.up * 0.4f);

        // The totem is used up
        root.DOKill();
        root.DOScale(0f, 0.25f).SetEase(Ease.InBack).SetLink(root.gameObject).OnComplete(() => Destroy(root.gameObject));
        totem = null;
        OnChanged?.Invoke();
    }

    private void SetCharging(bool on)
    {
        if (totem != null && totem.TryGetComponent(out SpriteFX totemFX))
            totemFX.SetShine(on);

        if (on)
        {
            if (charge == null)
                charge = CreateCharge();
            if (charge != null)
                charge.Play();
        }
        else if (charge != null)
        {
            charge.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    // Blue pixel sparks rising around the player's feet, following them
    private ParticleSystem CreateCharge()
    {
        FXSettings settings = FXSettings.Instance;
        if (settings == null || settings.SparkMaterial == null) return null;

        var chargeObject = new GameObject("TotemCharge");
        chargeObject.transform.SetParent(player.transform, false);
        chargeObject.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // shape faces up

        var system = chargeObject.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = system.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(settings.sparkSize * 0.6f, settings.sparkSize);
        main.startColor = new ParticleSystem.MinMaxGradient(ChargeColorA, ChargeColorB);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 80;

        var emission = system.emission;
        emission.rateOverTime = 45f;

        // A ring round the feet, rising straight up
        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 0f;
        shape.radius = 0.5f;
        shape.radiusThickness = 0.2f;

        var sizeOverLifetime = system.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        var particleRenderer = chargeObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = settings.SparkMaterial;
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;
        return system;
    }
}
