using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    public enum Mode
    {
        LookAt,
        LookAtInverted,
        CameraForward,
        CameraForwardInverted
    }
    public Mode mode;

    public float smoothSpeed = 10f; // smoothing factor

    void LateUpdate()
    {
        if (Camera.main == null) return;

        Quaternion targetRot = transform.rotation;

        switch (mode)
        {
            case Mode.LookAt:
                targetRot = Quaternion.LookRotation(Camera.main.transform.position - transform.position);
                break;
            case Mode.LookAtInverted:
                Vector3 dirFromCamera = transform.position - Camera.main.transform.position;
                targetRot = Quaternion.LookRotation(dirFromCamera);
                break;
            case Mode.CameraForward:
                targetRot = Quaternion.LookRotation(Camera.main.transform.forward);
                break;
            case Mode.CameraForwardInverted:
                targetRot = Quaternion.LookRotation(-Camera.main.transform.forward);
                break;
        }

        // Smoothly interpolate so flips don't snap X
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * smoothSpeed);
    }
}

