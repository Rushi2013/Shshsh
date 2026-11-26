using System.Collections;
using UnityEngine;
using UniStorm;

namespace CityDrive
{
    /// <summary>
    /// Bridges the imported UniStorm Volumetric Clouds asset into the City Drive
    /// bootstrap. The asset's own mobile prefab, shaders, cloud noise and skybox
    /// are used directly; this class only wires the player/camera and applies
    /// mobile-friendly limits. If the asset is unavailable the bootstrap falls
    /// back to the procedural sky already in the project.
    /// </summary>
    public static class UniStormSkyBridge
    {
        private const string MobilePrefabResource = "Systems/Resources/UniStorm Mobile System";
        private const string FallbackSunName = "CityDrive Sun";

        // UniStorm measures these in real minutes. The previous 2/2 setting made a
        // whole day pass in ~4 minutes; these values give a calm ~24 minute cycle.
        private const int DayLengthMinutes = 14;
        private const int NightLengthMinutes = 10;

        public static bool TryCreate(Transform player, Camera playerCamera)
        {
            if (player == null || playerCamera == null)
            {
                Debug.LogWarning("[CityDrive] UniStorm skipped: player or camera missing.");
                return false;
            }

            GameObject prefab = Resources.Load<GameObject>(MobilePrefabResource);
            if (prefab == null)
            {
                Debug.LogWarning("[CityDrive] UniStorm mobile prefab not found in Resources.");
                return false;
            }

            GameObject instance = Object.Instantiate(prefab);
            instance.name = "UniStorm Mobile System (CityDrive)";

            UniStormSystem system = instance.GetComponent<UniStormSystem>();
            if (system == null)
            {
                Object.Destroy(instance);
                Debug.LogWarning("[CityDrive] UniStorm prefab has no UniStormSystem component.");
                return false;
            }

            // Built-in Render Pipeline + Android: mobile profile, volumetric
            // clouds at medium march cost, no UI/weather generation/VR extras.
            system.PlatformType = UniStormSystem.PlatformTypeEnum.Mobile;
            system.CloudQuality = UniStormSystem.CloudQualityEnum.Medium;
            system.GetPlayerAtRuntime = UniStormSystem.EnableFeature.Disabled;
            system.UseRuntimeDelay = UniStormSystem.EnableFeature.Disabled;
            system.PlayerTransform = player;
            system.PlayerCamera = playerCamera;
            system.UseUniStormMenu = UniStormSystem.EnableFeature.Disabled;
            system.WeatherGeneration = UniStormSystem.EnableFeature.Disabled;
            system.LightningStrikes = UniStormSystem.EnableFeature.Disabled;
            system.SunShaftsEffect = UniStormSystem.EnableFeature.Disabled;
            system.MoonShaftsEffect = UniStormSystem.EnableFeature.Disabled;
            system.CloudShadows = UniStormSystem.EnableFeature.Enabled;
            system.ForceLowClouds = UniStormSystem.EnableFeature.Disabled;
            system.UseDithering = UniStormSystem.EnableFeature.Enabled;

            // Continuous 24-hour cycle: white day clouds, warm orange sunrise/sunset,
            // cool blue clouds at night. UniStorm evaluates these gradients every frame.
            system.TimeFlow = UniStormSystem.EnableFeature.Enabled;
            system.RealWorldTime = UniStormSystem.EnableFeature.Disabled;
            system.StartingHour = 8;
            system.DayLength = DayLengthMinutes;
            system.NightLength = NightLengthMinutes;
            ApplyDayNightColours(system);

            DisableFallbackSun();
            Debug.Log("[CityDrive] UniStorm volumetric sky active.");
            return true;
        }

        /// <summary>
        /// Overrides the imported UniStorm profile gradients with the requested
        /// palette: deep blue night, orange dawn/dusk, bright white midday.
        /// </summary>
        private static void ApplyDayNightColours(UniStormSystem system)
        {
            system.CloudBaseColor = Gradient(
                (0.00f, new Color(0.06f, 0.10f, 0.20f)),
                (0.22f, new Color(0.85f, 0.42f, 0.20f)),
                (0.28f, new Color(0.62f, 0.72f, 0.86f)),
                (0.50f, new Color(0.96f, 0.97f, 1.00f)),
                (0.70f, new Color(0.92f, 0.48f, 0.22f)),
                (0.78f, new Color(0.26f, 0.26f, 0.42f)),
                (1.00f, new Color(0.06f, 0.10f, 0.20f)));

            system.CloudLightColor = Gradient(
                (0.00f, new Color(0.12f, 0.18f, 0.32f)),
                (0.22f, new Color(1.00f, 0.58f, 0.28f)),
                (0.30f, new Color(0.92f, 0.94f, 1.00f)),
                (0.50f, new Color(1.00f, 1.00f, 1.00f)),
                (0.70f, new Color(1.00f, 0.55f, 0.26f)),
                (0.80f, new Color(0.28f, 0.30f, 0.48f)),
                (1.00f, new Color(0.12f, 0.18f, 0.32f)));

            system.SkyColor = Gradient(
                (0.00f, new Color(0.05f, 0.08f, 0.18f)),
                (0.23f, new Color(0.82f, 0.42f, 0.28f)),
                (0.30f, new Color(0.38f, 0.58f, 0.84f)),
                (0.50f, new Color(0.46f, 0.66f, 0.92f)),
                (0.70f, new Color(0.88f, 0.42f, 0.24f)),
                (0.78f, new Color(0.16f, 0.18f, 0.34f)),
                (1.00f, new Color(0.05f, 0.08f, 0.18f)));

            system.SunColor = Gradient(
                (0.00f, new Color(0.10f, 0.13f, 0.24f)),
                (0.23f, new Color(1.00f, 0.46f, 0.18f)),
                (0.30f, new Color(1.00f, 0.88f, 0.70f)),
                (0.50f, new Color(1.00f, 0.96f, 0.86f)),
                (0.70f, new Color(1.00f, 0.48f, 0.18f)),
                (0.78f, new Color(0.28f, 0.22f, 0.32f)),
                (1.00f, new Color(0.10f, 0.13f, 0.24f)));
        }

        private static Gradient Gradient(params (float time, Color color)[] keys)
        {
            Gradient gradient = new Gradient();
            GradientColorKey[] colours = new GradientColorKey[keys.Length];
            for (int i = 0; i < keys.Length; i++)
                colours[i] = new GradientColorKey(keys[i].color, keys[i].time);
            gradient.SetKeys(colours, new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            });
            return gradient;
        }

        /// <summary>UniStorm sets RenderSettings.sun during initialization; apply our shadow rules afterwards.</summary>
        public static IEnumerator ConfigureSunWhenReady()
        {
            yield return null;
            yield return null;

            Light sun = RenderSettings.sun;
            if (sun == null)
            {
                foreach (Light light in Object.FindObjectsOfType<Light>())
                {
                    if (light.type == LightType.Directional && light.enabled && light.gameObject.activeInHierarchy)
                    {
                        sun = light;
                        break;
                    }
                }
            }

            if (sun == null) yield break;
            sun.shadows = LightShadows.Soft;
            sun.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Medium;
            sun.shadowStrength = 0.86f;
            sun.shadowBias = 0.02f;
            sun.shadowNormalBias = 0.02f;
            RenderSettings.sun = sun;
        }

        /// <summary>Avoid two directional lights once UniStorm owns the sky.</summary>
        public static void DisableFallbackSun()
        {
            GameObject fallback = GameObject.Find(FallbackSunName);
            if (fallback != null) fallback.SetActive(false);
        }
    }
}
