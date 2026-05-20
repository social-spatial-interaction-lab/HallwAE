using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildScript
{
    public static void BuildAndroid()
    {
        string outputPath = "/tmp/hallwae-build/HallwAE.apk";

        BuildPlayerOptions opts = new BuildPlayerOptions
        {
            scenes = new[]
            {
                "Assets/Scenes/BasicSceneAR.unity"
            },
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        Debug.Log($"Starting Android build -> {outputPath}");
        BuildReport report = BuildPipeline.BuildPlayer(opts);

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded: {report.summary.totalSize} bytes");
        }
        else
        {
            Debug.LogError($"Build FAILED: {report.summary.result}");
            EditorApplication.Exit(1);
        }
    }
}
