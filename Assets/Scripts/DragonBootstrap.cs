using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Crea por código todo lo necesario para AR (AR Session, XR Origin, cámara, managers)
/// al iniciar la escena. Así la escena puede estar vacía y el proyecto funciona al abrirlo.
/// </summary>
public static class DragonBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (Object.FindFirstObjectByType<XROrigin>() != null) return; // ya hay un rig en la escena

        // Luz
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // AR Session
        var sessionGO = new GameObject("AR Session");
        sessionGO.AddComponent<ARSession>();
        sessionGO.AddComponent<ARInputManager>();

        // XR Origin (se arma desactivado y se activa al final)
        var originGO = new GameObject("XR Origin");
        originGO.SetActive(false);
        var origin = originGO.AddComponent<XROrigin>();

        var offset = new GameObject("Camera Offset");
        offset.transform.SetParent(originGO.transform, false);

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.transform.SetParent(offset.transform, false);
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 30f;
        camGO.AddComponent<ARCameraManager>();
        camGO.AddComponent<ARCameraBackground>();

        // Pose de la cámara (posición/rotación del teléfono)
        var posAction = new InputAction("devicePosition", InputActionType.Value, "<HandheldARInputDevice>/devicePosition");
        posAction.AddBinding("<XRHMD>/centerEyePosition");
        var rotAction = new InputAction("deviceRotation", InputActionType.Value, "<HandheldARInputDevice>/deviceRotation");
        rotAction.AddBinding("<XRHMD>/centerEyeRotation");
        posAction.Enable();
        rotAction.Enable();
        var driver = camGO.AddComponent<TrackedPoseDriver>();
        driver.positionInput = new InputActionProperty(posAction);
        driver.rotationInput = new InputActionProperty(rotAction);

        origin.Camera = cam;
        origin.CameraFloorOffsetObject = offset;

        originGO.AddComponent<ARRaycastManager>();
        originGO.AddComponent<ARPlaneManager>();
        originGO.AddComponent<DragonARGame>();

        originGO.SetActive(true);
    }
}
