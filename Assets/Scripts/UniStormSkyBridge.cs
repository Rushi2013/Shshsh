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

            DisableFallbackSun();
            Debug.Log("[CityDrive] UniStorm volumetric sky active.");
            return true;
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
