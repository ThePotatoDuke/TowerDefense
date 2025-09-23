using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    [Header("Optional Default Camera")]
    [SerializeField] private CinemachineCamera defaultCamera; // use VirtualCamera

    private static List<CinemachineCamera> cameras = new List<CinemachineCamera>();
    public static CinemachineCamera activeCamera = null;

    private static CinemachineBrain brain;
    private static Action onBlendComplete;

    private void Awake()
    {
        if (brain == null)
            brain = Camera.main.GetComponent<CinemachineBrain>();

        // Reset static state
        cameras = new List<CinemachineCamera>();
        activeCamera = null;
        onBlendComplete = null;

        // Find all virtual cameras in the scene
        var allCams = UnityEngine.Object.FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        foreach (var cam in allCams)
        {
            // Set player/default camera priority to 10, others to 0
            cam.Priority = (cam == defaultCamera) ? 10 : 0;

            Register(cam);
        }

        // Set active camera to defaultCamera
        if (defaultCamera != null)
            activeCamera = defaultCamera;
        else if (allCams.Length > 0)
            activeCamera = allCams[0];
    }


    public static void Register(CinemachineCamera camera)
    {
        if (!cameras.Contains(camera))
            cameras.Add(camera);
    }

    public static void Unregister(CinemachineCamera camera)
    {
        if (cameras.Contains(camera))
            cameras.Remove(camera);
    }

    public static void SwitchCamera(CinemachineCamera newCamera, Action blendCompleteCallback = null)
    {
        if (newCamera == null) return;

        newCamera.Priority = 10;
        activeCamera = newCamera;
        onBlendComplete = blendCompleteCallback;

        // Lower priority of other cameras
        foreach (var cam in cameras)
        {
            if (cam != activeCamera)
                cam.Priority = 0;
        }

        // Start coroutine to detect blend completion
        if (brain != null && blendCompleteCallback != null)
        {
            brain.StartCoroutine(WaitForBlend());
        }
    }

    private static IEnumerator WaitForBlend()
    {
        yield return null;

        while (brain != null && brain.IsBlending)
            yield return null;

        onBlendComplete?.Invoke();
        onBlendComplete = null;
    }
}
