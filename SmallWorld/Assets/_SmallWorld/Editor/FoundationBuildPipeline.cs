using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SmallWorld.Editor
{
    public static class FoundationBuildPipeline
    {
        private const string ProductExecutableName = "SmallWorld.exe";
        private static readonly NamedBuildTarget WindowsBuildTarget = NamedBuildTarget.Standalone;

        [MenuItem("Small World/Build/Windows/Development")]
        public static void BuildDevelopment()
        {
            BuildWindows(
                "Development",
                BuildOptions.Development | BuildOptions.AllowDebugging,
                ScriptingImplementation.Mono2x);
        }

        [MenuItem("Small World/Build/Windows/Test")]
        public static void BuildTest()
        {
            BuildWindows(
                "Test",
                BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.IncludeTestAssemblies,
                ScriptingImplementation.Mono2x);
        }

        [MenuItem("Small World/Build/Windows/Release")]
        public static void BuildRelease()
        {
            BuildWindows("Release", BuildOptions.None, ScriptingImplementation.IL2CPP);
        }

        private static void BuildWindows(
            string profileName,
            BuildOptions options,
            ScriptingImplementation profileBackend)
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new BuildFailedException(
                    "No enabled scenes were found in Editor Build Settings. " +
                    "Enable Assets/Scenes/SampleScene.unity before building.");
            }

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                ?? throw new BuildFailedException("Could not determine the Unity project root.");
            string outputDirectory = Path.Combine(projectRoot, "Builds", "Windows", profileName);
            string outputPath = Path.Combine(outputDirectory, ProductExecutableName);
            Directory.CreateDirectory(outputDirectory);

            ScriptingImplementation originalBackend =
                PlayerSettings.GetScriptingBackend(WindowsBuildTarget);

            try
            {
                PlayerSettings.SetScriptingBackend(WindowsBuildTarget, profileBackend);

                Debug.Log(
                    $"Starting {profileName} Windows x86_64 build. " +
                    $"Backend: {PlayerSettings.GetScriptingBackend(WindowsBuildTarget)}, " +
                    $"Options: {options}, Output: {outputPath}");

                var buildPlayerOptions = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = outputPath,
                    target = BuildTarget.StandaloneWindows64,
                    targetGroup = BuildTargetGroup.Standalone,
                    options = options
                };

                BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
                BuildSummary summary = report.summary;

                if (summary.result != BuildResult.Succeeded)
                {
                    throw new BuildFailedException(
                        $"{profileName} build failed with result {summary.result}. " +
                        $"Errors: {summary.totalErrors}, Warnings: {summary.totalWarnings}.");
                }

                Debug.Log(
                    $"{profileName} build succeeded: {summary.outputPath} " +
                    $"({summary.totalSize} bytes, {summary.totalTime}).");
            }
            catch (Exception exception) when (!(exception is BuildFailedException))
            {
                throw new BuildFailedException(
                    $"{profileName} build could not be completed: {exception}");
            }
            finally
            {
                if (PlayerSettings.GetScriptingBackend(WindowsBuildTarget) != originalBackend)
                {
                    PlayerSettings.SetScriptingBackend(WindowsBuildTarget, originalBackend);
                }
            }
        }
    }
}
