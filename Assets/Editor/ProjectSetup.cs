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

        QualitySettings.shadowDistance = 85f; // PDF mobile guidance: 60–90 units
        QualitySettings.shadowResolution = ShadowResolution.Medium;
        QualitySettings.shadowCascades = 2;
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.vSyncCount = 0;
        QualitySettings.antiAliasing = 0;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
        QualitySettings.pixelLightCount = 4;

        EnsureScene();
        EnsureSkybox();
    }

    /// <summary>
    /// PDF workflow: if the AllSky Free pack is imported, pick its natural daytime
    /// sky material by inspecting the real imported assets (no guessed name), keep
    /// its shader/texture untouched, and assign it to the scene. Otherwise fall
    /// back to the built-in realistic sky shader shipped with this project.
    /// </summary>
    private static void EnsureSkybox()
    {
        Material sky = FindDaytimeSkyMaterial();
        bool fromAssetPack = sky != null;

        if (!fromAssetPack)
        {
            Shader shader = Shader.Find("CityDrive/Realistic Sky") ?? Shader.Find("Skybox/Procedural");
            if (shader == null) return;

            const string directory = "Assets/Skybox";
            const string path = directory + "/CityDriveSky.mat";
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            sky = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (sky == null)
            {
                sky = new Material(shader) { name = "CityDriveSky" };
                AssetDatabase.CreateAsset(sky, path);
            }
            if (sky.HasProperty("_Tint")) sky.SetColor("_Tint", new Color(0.36f, 0.52f, 0.78f));
            if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", new Color(0.36f, 0.52f, 0.78f));
            if (sky.HasProperty("_Horizon")) sky.SetColor("_Horizon", new Color(0.74f, 0.82f, 0.90f));
            if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", new Color(0.38f, 0.39f, 0.40f));
            if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", 1f);
            EditorUtility.SetDirty(sky);
        }
        else
        {
            // PDF starting values: neutral tint, zero rotation, exposure 1 where exposed.
            if (sky.HasProperty("_Rotation")) sky.SetFloat("_Rotation", 0f);
            if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", 1f);
            if (sky.HasProperty("_Tint")) sky.SetColor("_Tint", Color.white);
        }

        RenderSettings.skybox = sky;
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        AssetDatabase.SaveAssets();

        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.IsValid() && scene.isLoaded && !string.IsNullOrEmpty(scene.path))
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        Debug.Log(fromAssetPack
            ? "[CityDrive] Using imported daytime sky material: " + sky.name
            : "[CityDrive] AllSky material not found; using built-in realistic sky fallback.");
    }

    [MenuItem("CityDrive/Reapply Sky and Environment")]
    public static void ReapplySky()
    {
        EnsureSkybox();
    }

    /// <summary>Scores imported materials instead of guessing an AllSky material name.</summary>
    private static Material FindDaytimeSkyMaterial()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material");
        Material best = null;
        float bestScore = float.MaxValue;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/")) continue;
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null || material.shader == null || !material.shader.isSupported) continue;

            string identity = (material.name + " " + path + " " + material.shader.name).ToLowerInvariant();
            bool skyLike = identity.Contains("sky") || material.shader.name.Contains("Skybox");
            if (!skyLike) continue;
            if (identity.Contains("urp") || identity.Contains("hdrp")) continue;

            float score = 0f;
            if (identity.Contains("allsky")) score -= 40f;
            if (identity.Contains("day") || identity.Contains("blue") ||
                identity.Contains("sunny") || identity.Contains("clear")) score -= 20f;
            if (identity.Contains("night") || identity.Contains("sunset") ||
                identity.Contains("space") || identity.Contains("cartoon") ||
                identity.Contains("stylized") || identity.Contains("pink") ||
                identity.Contains("dawn") || identity.Contains("dusk")) score += 90f;

            if (score < bestScore)
            {
                bestScore = score;
                best = material;
            }
        }

        return bestScore < 50f ? best : null;
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
            sun.color = new Color(1f, 0.96f, 0.90f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            sun.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Medium;
            sun.shadowStrength = 0.88f;
            sun.shadowBias = 0.02f;
            sun.shadowNormalBias = 0.02f;

            new GameObject("GameManager", typeof(GameBootstrap));

            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 220f;
            RenderSettings.fogEndDistance = 700f;
            RenderSettings.fogColor = new Color(0.68f, 0.76f, 0.86f);

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
        // Exact marker GameCI's build validator looks for in the Unity output.
        Debug.Log("Build succeeded!");
    }
}
#endif
