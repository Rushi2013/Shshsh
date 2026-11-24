#if UNITY_EDITOR
using System;
using System.IO;
using CityDrive;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Creates a valid scene and player settings automatically, so the project can be
/// built from a fresh checkout with no Editor interaction (CI / Unity Cloud Build).
/// Also provides the -executeMethod entry point used by the GitHub Actions APK build.
/// </summary>
public static class ProjectSetup
{
    [InitializeOnLoadMethod]
    private static void OnEditorLoaded()
    {
        EnsureProject();
    }

    private const string ScenePath = "Assets/Scenes/CityDrive.unity";
    private const string ApkPath = "build/Android/Android.apk";

    [MenuItem("CityDrive/Setup Project")]
    public static void EnsureProject()
    {
        PlayerSettings.companyName = "Shshsh";
        PlayerSettings.productName = "City Drive";
        PlayerSettings.bundleVersion = "1.0.0";
        SetLegacyInputEnabled();
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.shshsh.citydrive");
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);

        QualitySettings.shadowDistance = 90f;
        QualitySettings.vSyncCount = 0;

        EnsureScene();
    }

    /// <summary>
    /// Uses reflection because the public name of this setting changed between
    /// Unity versions; value 2 means "Both" (old Input manager + Input System).
    /// </summary>
    private static void SetLegacyInputEnabled()
    {
        System.Reflection.PropertyInfo property =
            typeof(PlayerSettings).GetProperty("activeInputHandler") ??
            typeof(PlayerSettings).GetProperty("activeInputHandling");
        if (property == null) return;

        object value = property.PropertyType.IsEnum
            ? Enum.ToObject(property.PropertyType, 2)
            : (object)2;
        property.SetValue(null, value, null);
    }

    private static void EnsureScene()
    {
        Directory.CreateDirectory("Assets/Scenes");

        bool sceneExists = File.Exists(ScenePath);
        bool inBuildSettings = false;
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.path == ScenePath && scene.enabled) inBuildSettings = true;
        }

        if (!sceneExists)
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 6f, -12f);
            cameraObject.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.fieldOfView = 65f;
            camera.farClipPlane = 1200f;
            camera.clearFlags = CameraClearFlags.Skybox;

            GameObject sunObject = new GameObject("Sun", typeof(Light));
            sunObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            Light sun = sunObject.GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;

            new GameObject("GameManager", typeof(GameBootstrap));

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.52f, 0.58f, 0.68f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 250f;
            RenderSettings.fogEndDistance = 900f;
            RenderSettings.fogColor = new Color(0.62f, 0.70f, 0.80f);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        if (!inBuildSettings)
        {
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }

    [MenuItem("CityDrive/Build Android APK")]
    public static void BuildAndroid()
    {
        EnsureProject();

        Directory.CreateDirectory("build/Android");
        if (File.Exists(ApkPath)) File.Delete(ApkPath);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = ApkPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result != BuildResult.Succeeded)
        {
            throw new Exception("[CityDrive] Android build failed: " + summary.result +
                                " (" + summary.totalErrors + " errors)");
        }

        // Keep the name GameCI expects, plus a friendly copy for the artifact upload.
        File.Copy(ApkPath, "build/CityDrive.apk", true);
        Debug.Log("[CityDrive] APK built: " + ApkPath + " (" + summary.totalSize / 1024 / 1024 + " MB)");
    }
}
#endif
