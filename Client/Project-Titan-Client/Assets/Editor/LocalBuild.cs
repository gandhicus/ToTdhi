using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class LocalBuild
{
    private const string WindowsBuildPath = "Builds/Windows/TrialsOfTitan.exe";

    [MenuItem("Local/Build Windows Client")]
    public static void BuildWindowsFromMenu()
    {
        BuildWindows();
    }

    public static void BuildWindows()
    {
        var scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
            throw new InvalidOperationException("No enabled scenes found in EditorBuildSettings.");

        var directory = Path.GetDirectoryName(WindowsBuildPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = WindowsBuildPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });

        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception($"Windows build failed: {report.summary.result}");

        UnityEngine.Debug.Log($"Windows build created at {WindowsBuildPath}");
    }
}
