using UnityEditor;

// Enters Play mode automatically when Unity is launched with -autoplay on the command line.
// Usage: Unity -projectPath <path> -autoplay
// Does nothing during normal editor sessions or batch builds.
[InitializeOnLoad]
public static class AutoPlayMode
{
    static AutoPlayMode()
    {
        if (!System.Array.Exists(System.Environment.GetCommandLineArgs(), a => a == "-autoplay"))
            return;

        EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
    }
}
