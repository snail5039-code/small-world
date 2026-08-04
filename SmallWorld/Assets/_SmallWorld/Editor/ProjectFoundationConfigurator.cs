using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace SmallWorld.Editor
{
    /// <summary>
    /// Applies the shared Windows project foundation through Unity's editor APIs.
    /// ProjectSettings files must not be edited by hand.
    /// </summary>
    public static class ProjectFoundationConfigurator
    {
        private const string ProductName = "둘만의 작은 세계";
        private const string ProductVersion = "0.1.0";
        private const string StandaloneApplicationIdentifier = "com.personal.smallworld";
        private const string UnityDefaultCompanyName = "DefaultCompany";
        private const string ProjectSettingsPath = "ProjectSettings/ProjectSettings.asset";

        [MenuItem("Small World/Project/Apply Foundation Settings")]
        public static void ApplyFromMenu()
        {
            try
            {
                ApplyAndValidate();
                Debug.Log("[SmallWorld] Project foundation settings were applied successfully.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        /// <summary>
        /// Batch entry point:
        /// -executeMethod SmallWorld.Editor.ProjectFoundationConfigurator.ApplyFromBatchMode
        /// </summary>
        public static void ApplyFromBatchMode()
        {
            var exitCode = 0;

            try
            {
                ApplyAndValidate();
                Debug.Log("[SmallWorld] Batch project foundation configuration succeeded.");
            }
            catch (Exception exception)
            {
                exitCode = 1;
                Debug.LogException(exception);
            }
            finally
            {
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(exitCode);
                }
            }

            if (exitCode != 0)
            {
                throw new InvalidOperationException(
                    "Project foundation configuration failed. See the preceding Unity editor log.");
            }
        }

        private static void ApplyAndValidate()
        {
            SwitchToWindows64();

            // Owner decision: this is an individual project with no company/publisher name.
            PlayerSettings.companyName = string.Empty;
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = ProductVersion;
            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.Standalone,
                StandaloneApplicationIdentifier);
            PlayerSettings.SetScriptingBackend(
                NamedBuildTarget.Standalone,
                ScriptingImplementation.IL2CPP);

            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(
                BuildTarget.StandaloneWindows64,
                new[] { GraphicsDeviceType.Direct3D11 });

            SetNewInputSystemOnly();
            AssetDatabase.SaveAssets();

            ValidateSettings();
        }

        private static void SwitchToWindows64()
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64)
            {
                return;
            }

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Standalone,
                    BuildTarget.StandaloneWindows64))
            {
                throw new InvalidOperationException(
                    "Could not switch the active build target to Windows x86_64. " +
                    "Verify that Windows Build Support is installed.");
            }
        }

        private static void SetNewInputSystemOnly()
        {
            // Unity serializes Active Input Handling in PlayerSettings but does not expose
            // a stable public setter across all Unity 6 patch releases. SerializedObject is
            // used here so Unity performs the write; the YAML file is never edited directly.
            var settingsObjects = AssetDatabase.LoadAllAssetsAtPath(ProjectSettingsPath);
            if (settingsObjects == null || settingsObjects.Length == 0)
            {
                throw new InvalidOperationException("Could not load Unity PlayerSettings.");
            }

            var serializedSettings = new SerializedObject(settingsObjects[0]);
            var inputHandler = serializedSettings.FindProperty("activeInputHandler");
            if (inputHandler == null)
            {
                throw new InvalidOperationException(
                    "Unity PlayerSettings does not contain 'activeInputHandler'.");
            }

            // 0 = legacy, 1 = Input System package, 2 = both.
            inputHandler.intValue = 1;
            if (!serializedSettings.ApplyModifiedPropertiesWithoutUndo())
            {
                serializedSettings.Update();
                if (inputHandler.intValue != 1)
                {
                    throw new InvalidOperationException("Failed to enable the new Input System.");
                }
            }
        }

        private static void ValidateSettings()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
            {
                throw new InvalidOperationException("Active build target is not Windows x86_64.");
            }

            if (PlayerSettings.productName != ProductName ||
                PlayerSettings.bundleVersion != ProductVersion)
            {
                throw new InvalidOperationException("Product name or version validation failed.");
            }

            if (!string.Equals(
                    PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Standalone),
                    StandaloneApplicationIdentifier,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Standalone application identifier validation failed.");
            }

            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone) !=
                ScriptingImplementation.IL2CPP)
            {
                throw new InvalidOperationException("Standalone scripting backend is not IL2CPP.");
            }

            var graphicsApis = PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64);
            if (graphicsApis.Length == 0 || graphicsApis[0] != GraphicsDeviceType.Direct3D11)
            {
                throw new InvalidOperationException("Direct3D 11 is not the first Windows graphics API.");
            }

            var settingsObjects = AssetDatabase.LoadAllAssetsAtPath(ProjectSettingsPath);
            if (settingsObjects == null || settingsObjects.Length == 0)
            {
                throw new InvalidOperationException("Could not load Unity PlayerSettings for validation.");
            }

            var serializedSettings = new SerializedObject(settingsObjects[0]);
            var serializedCompanyName = serializedSettings.FindProperty("companyName");
            if (serializedCompanyName == null)
            {
                throw new InvalidOperationException(
                    "Unity PlayerSettings does not contain 'companyName'.");
            }

            // Unity normalizes an empty Company Name to its internal DefaultCompany value.
            // Both serialized forms represent the owner's explicit "no company" decision.
            if (!string.IsNullOrEmpty(serializedCompanyName.stringValue) &&
                !string.Equals(
                    serializedCompanyName.stringValue,
                    UnityDefaultCompanyName,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Company Name must be empty or Unity's DefaultCompany for this individual project.");
            }
        }
    }
}
