using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildScript
{
    // Android (Quest): IL2CPP + ARM64 always. Default = dev build; -release = optimized.
    public static void BuildAndroid()
    {
        bool release = HasArg("-release");
        BuildAndroidInternal(release);
    }

    // Desktop (Linux): Mono, StandaloneLinux64. Default = dev build; -release = optimized.
    public static void BuildDesktop()
    {
        bool release = HasArg("-release");
        BuildDesktopInternal(release);
    }

    static void BuildAndroidInternal(bool release)
    {
        string outputPath = "/tmp/hallwae-build/HallwAE.apk";
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetIl2CppCodeGeneration(
            NamedBuildTarget.Android,
            release ? Il2CppCodeGeneration.OptimizeSpeed : Il2CppCodeGeneration.OptimizeSize);
        Debug.Log($"Starting Android build -> {outputPath}  [release={release}]");

        RunBuild(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/BasicSceneAR.unity" },
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = release ? BuildOptions.None : BuildOptions.Development
        });
    }

    static void BuildDesktopInternal(bool release)
    {
        string outputPath = "/tmp/hallwae-build/HallwAE-desktop/HallwAE.x86_64";
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        Debug.Log($"Starting Desktop build -> {outputPath}  [release={release}]");

        RunBuild(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/BasicSceneAR.unity" },
            locationPathName = outputPath,
            target = BuildTarget.StandaloneLinux64,
            options = release ? BuildOptions.None : BuildOptions.Development
        });
    }

    static void RunBuild(BuildPlayerOptions opts)
    {
        BuildReport report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log($"Build succeeded: {report.summary.totalSize} bytes");
        else
        {
            Debug.LogError($"Build FAILED: {report.summary.result}");
            EditorApplication.Exit(1);
        }
    }

    static bool HasArg(string arg) =>
        System.Array.Exists(System.Environment.GetCommandLineArgs(), a => a == arg);
}
