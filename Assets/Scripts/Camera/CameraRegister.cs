using Unity.Cinemachine;
using UnityEngine;

public class CameraRegister : MonoBehaviour
{
    void OnEnable()
    {
        CameraManager.Register(GetComponent<CinemachineCamera>());
    }
    void OnDisable()
    {
        CameraManager.Unregister(GetComponent<CinemachineCamera>());

    }
}
