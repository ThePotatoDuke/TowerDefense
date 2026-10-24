using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Makes the killing hit on the player land hard: a freeze frame, a camera punch-in with a shake,
// a drained, vignetted screen, then slow motion that eases back to normal speed.
// Everything runs on unscaled time so it keeps moving while the game is frozen.
public class PlayerDeathFX : MonoBehaviour
{
    public void Play()
    {
        FXSettings settings = FXSettings.Instance;
        if (settings == null) return;

        // Keep the camera and its shake running while the game is frozen
        CinemachineBrain brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : null;
        if (brain != null)
            brain.IgnoreTimeScale = true;
        CinemachineImpulseManager.Instance.IgnoreTimeScale = true;

        CinemachineCamera vcam = CameraManager.activeCamera;
        if (vcam != null)
            ZoomIn(vcam, settings);
        CameraShake.Shake(transform.position, settings.deathShakeForce, settings.deathShakeDuration);

        DrainScreen(settings);
        FreezeThenSlowMotion(settings);
    }

    private void FreezeThenSlowMotion(FXSettings settings)
    {
        Time.timeScale = 0f;

        DOTween.Sequence()
               .AppendInterval(settings.deathHitStop)
               .AppendCallback(() => Time.timeScale = settings.deathSlowMotion)
               .Append(DOTween.To(() => Time.timeScale, x => Time.timeScale = x, 1f, settings.deathSlowMotionRecovery)
                              .SetEase(Ease.InQuad))
               .SetUpdate(true)
               .SetLink(gameObject);
    }

    private void ZoomIn(CinemachineCamera vcam, FXSettings settings)
    {
        DOTween.To(() => vcam.Lens.FieldOfView, x => vcam.Lens.FieldOfView = x,
                   settings.deathZoomFieldOfView, settings.deathZoomDuration)
               .SetEase(Ease.InOutSine)
               .SetUpdate(true)
               .SetLink(vcam.gameObject);

        // Also move closer, so the player fills more of the frame
        if (!vcam.TryGetComponent(out CinemachineFollow follow)) return;

        Vector3 zoomedOffset = follow.FollowOffset * settings.deathZoomDistance;
        DOTween.To(() => follow.FollowOffset, x => follow.FollowOffset = x, zoomedOffset, settings.deathZoomDuration)
               .SetEase(Ease.InOutSine)
               .SetUpdate(true)
               .SetLink(vcam.gameObject);

        // The gameplay camera has a fixed tilt that looks ahead of the player; turn it to look
        // straight at them. With no aim component, the camera takes its rotation from its transform.
        Transform target = vcam.Target.TrackingTarget;
        if (target == null) return;

        Vector3 focus = GetFocusPoint() - target.position;   // relative to the follow target
        Quaternion lookAtPlayer = Quaternion.LookRotation(focus - zoomedOffset, Vector3.up);
        vcam.transform.DORotateQuaternion(lookAtPlayer, settings.deathZoomDuration)
                      .SetEase(Ease.InOutSine)
                      .SetUpdate(true)
                      .SetLink(vcam.gameObject);
    }

    // Middle of the player's sprite, so they end up centered rather than their feet
    private Vector3 GetFocusPoint()
    {
        SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
        return sprite != null ? sprite.bounds.center : transform.position;
    }

    // A temporary global volume layered over the scene's own post-processing
    private void DrainScreen(FXSettings settings)
    {
        var volumeObject = new GameObject("PlayerDeathVolume");
        volumeObject.transform.SetParent(transform, false);

        var volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f;
        volume.weight = 0f;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.Add<ColorAdjustments>(true).saturation.Override(settings.deathSaturation);
        profile.Add<Vignette>(true).intensity.Override(settings.deathVignette);
        profile.Add<ChromaticAberration>(true).intensity.Override(settings.deathChromaticAberration);
        volume.profile = profile;

        DOTween.To(() => volume.weight, x => volume.weight = x, 1f, settings.deathScreenFadeDuration)
               .SetEase(Ease.OutQuad)
               .SetUpdate(true)
               .SetLink(volumeObject);
    }
}
