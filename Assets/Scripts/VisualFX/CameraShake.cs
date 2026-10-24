using Unity.Cinemachine;
using UnityEngine;

// One-off camera shakes through Cinemachine's impulse system, on the active gameplay camera.
public static class CameraShake
{
    private const int ImpulseChannel = 1;

    // A sharp jolt in a random, mostly sideways direction
    public static void Shake(Vector3 origin, float force, float duration)
    {
        CinemachineCamera vcam = CameraManager.activeCamera;
        if (vcam == null || force <= 0f) return;

        EnsureListener(vcam);

        var impulse = new CinemachineImpulseDefinition
        {
            ImpulseChannel = ImpulseChannel,
            ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Explosion,
            ImpulseDuration = duration,
            ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform,
            DissipationDistance = 100f,
            DissipationRate = 0.25f,
            PropagationSpeed = 343f
        };

        Vector2 direction = Random.insideUnitCircle.normalized;
        Vector3 velocity = new Vector3(direction.x, direction.y * 0.6f, 0f) * force;
        impulse.CreateEvent(origin, velocity);
    }

    private static void EnsureListener(CinemachineCamera vcam)
    {
        if (vcam.TryGetComponent(out CinemachineImpulseListener _)) return;

        // Added from code, so set what the Inspector's Reset() would
        var listener = vcam.gameObject.AddComponent<CinemachineImpulseListener>();
        listener.ApplyAfter = CinemachineCore.Stage.Noise;
        listener.ChannelMask = ImpulseChannel;
        listener.Gain = 1f;
        listener.UseCameraSpace = true;
        listener.SignalCombinationMode = CinemachineImpulseListener.SignalCombinationModes.Additive;
    }
}
