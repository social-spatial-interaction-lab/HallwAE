using UnityEditor;
using UnityEngine;

// Suppresses Android-only plugins when entering Play mode on desktop.
// The Meta XR Audio spatializer has no Linux/Windows native library,
// so we temporarily clear it and restore it on exit.
[InitializeOnLoad]
static class DesktopPlayModeSetup
{
    const string SpatializerKey = "DesktopPlayMode_SavedSpatializer";

    static DesktopPlayModeSetup()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            string current = AudioSettings.GetSpatializerPluginName();
            SessionState.SetString(SpatializerKey, current);
            AudioSettings.SetSpatializerPluginName("");
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            string saved = SessionState.GetString(SpatializerKey, "");
            if (!string.IsNullOrEmpty(saved))
                AudioSettings.SetSpatializerPluginName(saved);
        }
    }
}
