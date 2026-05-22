using Unity.XR.CoreUtils;
using UnityEngine;

namespace XRMultiplayer
{
    static class DesktopModeActivator
    {
        static readonly string[] k_VROnlyNames =
        {
            "OVRCameraRig",
            "TunnelingVignette",
            "Hand Menu With Button Activation",
            "Affordance Callout Grab",
            "Affordance Callout Quick Turn",
            "Affordance Callout UI Press",
            "Affordance Callout Move",
            "Affordance Callout Menu",
            "Affordance Callout Teleport",
        };

        // AfterSceneLoad: disable VR-only objects, kill any spawned audio components,
        // ensure the camera is active, and inject the FPS controller.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Activate()
        {
            if (Application.platform == RuntimePlatform.Android) return;

            // Disable VR-only GameObjects by name
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                foreach (var vrName in k_VROnlyNames)
                    if (go.name == vrName)
                        go.SetActive(false);

            // Disable MetaXRAudioRoomAcousticProperties if it was spawned despite cleared spatializer
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (mb.GetType().Name == "MetaXRAudioRoomAcousticProperties")
                    mb.enabled = false;

            // Find XR Origin and ensure its camera is active and tagged MainCamera.
            // XROrigin.Camera is null without XR running, so fall back to a hierarchy search.
            var origin = Object.FindFirstObjectByType<XROrigin>();
            if (origin != null)
            {
                Camera cam = origin.Camera
                    ?? origin.GetComponentInChildren<Camera>(includeInactive: true)
                    ?? Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);

                if (cam != null)
                {
                    cam.gameObject.SetActive(true);
                    cam.tag = "MainCamera";
                    Debug.Log($"[DesktopMode] Main camera set to: {cam.gameObject.name}");
                }
                else
                {
                    Debug.LogWarning("[DesktopMode] No camera found in scene.");
                }

                origin.gameObject.AddComponent<DesktopCameraController>();
            }
            else
            {
                Debug.LogWarning("[DesktopMode] No XROrigin found.");
            }
        }
    }
}
