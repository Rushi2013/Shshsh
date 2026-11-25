using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityDrive
{
    /// <summary>
    /// Entry point. Asynchronously loads the supplied GLB assets with real progress,
    /// builds the city and physics car, configures lighting/sky/effects/audio, then
    /// hands control to the showroom. The same UI instance is reused after PLAY.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        private const string CarResource = "Vehicles/BMW330i/bmw330i";
        private const string CityResource = "City/city";
        private const float ModelScale = 90f;

        private IEnumerator Start()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            QualitySettings.shadowDistance = 85f; // PDF guidance: 60–90 units on mobile
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Physics.defaultContactOffset = 0.015f;
            Physics.bounceThreshold = 4f;

            LoadingScreen loading = gameObject.AddComponent<LoadingScreen>();
            loading.Show();
            loading.SetProgress(0.02f, "GAME LOADING", "Reading city and vehicle assets...");

            ResourceRequest cityRequest = Resources.LoadAsync<GameObject>(CityResource);
            ResourceRequest carRequest = Resources.LoadAsync<GameObject>(CarResource);
            while (!cityRequest.isDone || !carRequest.isDone)
            {
                float assetProgress = (cityRequest.progress + carRequest.progress) * 0.5f;
                loading.SetProgress(0.05f + assetProgress * 0.50f,
                    "LOADING ASSETS", "Streaming city textures and sedan model...");
                yield return null;
            }

            GameObject cityPrefab = cityRequest.asset as GameObject;
            GameObject carPrefab = carRequest.asset as GameObject;
            if (cityPrefab == null || carPrefab == null)
                Debug.LogWarning("[CityDrive] One or more GLB prefabs were unavailable; using fallbacks.");

            loading.SetProgress(0.58f, "BUILDING CITY", "Creating roads, pavements and ramps...");
            GameObject cityHost = new GameObject("City Loader");
            CityBuilder city = cityHost.AddComponent<CityBuilder>();
            yield return StartCoroutine(city.Build(cityPrefab));

            CreateGroundFallback(cityHost.transform, city.Bounds);
            SetupEnvironment();

            loading.SetProgress(0.78f, "BUILDING CAR", "Preparing suspension, tyres and audio...");
            Vector3 spawn = city.FindSpawnPoint();
            Quaternion spawnRotation = city.SpawnRotation;
            GameObject car = SpawnCar(spawn, spawnRotation, carPrefab);
            ArcadeCarController controller = car.GetComponent<ArcadeCarController>();
            controller.enabled = false;

            // Separate true 3D cloud geometry; skybox remains only the backdrop.
            GameObject cloudHost = new GameObject("3D Cloud Layer");
            CloudSky clouds = cloudHost.AddComponent<CloudSky>();
            clouds.SetTarget(car.transform);

            Camera cameraComponent = Camera.main;
            if (cameraComponent == null)
            {
                GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                cameraComponent = cameraObject.GetComponent<Camera>();
            }
            cameraComponent.clearFlags = CameraClearFlags.Skybox;
            cameraComponent.depthTextureMode |= DepthTextureMode.Depth; // soft tyre smoke fade
            cameraComponent.allowHDR = true;
            cameraComponent.fieldOfView = 64f;
            cameraComponent.farClipPlane = 1200f;

            ChaseCamera chase = cameraComponent.gameObject.GetComponent<ChaseCamera>();
            if (chase == null) chase = cameraComponent.gameObject.AddComponent<ChaseCamera>();
            chase.Target = car.transform;
            chase.Car = controller;
            chase.SetMode(0);

            VehicleAudio audio = car.GetComponent<VehicleAudio>();
            if (audio != null) audio.CameraRig = chase;

            loading.SetProgress(0.94f, "ALMOST READY", "Showroom is ready...");
            HomeScreen home = gameObject.AddComponent<HomeScreen>();
            home.Show(city, car, controller, chase, cameraComponent, spawn, spawnRotation,
                loading, audio, () =>
                {
                    GameObject controlsHost = new GameObject("Controls");
                    MobileControls controls = controlsHost.AddComponent<MobileControls>();
                    controls.Car = controller;
                    controls.Chase = chase;
                    controls.Audio = audio;
                });

            // Everything is constructed before 100% is reported.
            loading.SetProgress(1f, "READY TO DRIVE", "Welcome to City Drive");
            yield return null;
            loading.Hide();
        }

        /// <summary>Flat safety surface under the city so tyres always have a road.</summary>
        private static void CreateGroundFallback(Transform parent, Bounds cityBounds)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground (fallback under city roads)";
            ground.transform.SetParent(parent, false);
            ground.transform.position = new Vector3(0f, cityBounds.min.y - 0.12f, 0f);
            ground.transform.localScale = new Vector3(
                Mathf.Max(20f, cityBounds.size.x / 10f), 1f,
                Mathf.Max(20f, cityBounds.size.z / 10f));
            Shader shader = Shader.Find("Standard");
            Material material = shader != null ? new Material(shader) : null;
            if (material != null)
            {
                material.color = new Color(0.23f, 0.25f, 0.27f);
                material.SetFloat("_Metallic", 0.05f);
                material.SetFloat("_Glossiness", 0.35f);
                ground.GetComponent<Renderer>().material = material;
            }
        }

        /// <summary>
        /// Applies the PDF environment rules: keep an imported AllSky/daytime skybox
        /// if one is already assigned, otherwise use the built-in realistic fallback.
        /// Ambient/reflections come from the skybox, fog is subtle and distant,
        /// shadows are soft with mobile-friendly resolution.
        /// </summary>
        private static void SetupEnvironment()
        {
            Material sky = RenderSettings.skybox;
            bool skyValid = sky != null && sky.shader != null && sky.shader.isSupported &&
                            !sky.shader.name.Contains("URP") && !sky.shader.name.Contains("HDRP");

            if (!skyValid)
            {
                Shader skyShader = Shader.Find("CityDrive/Realistic Sky") ?? Shader.Find("Skybox/Procedural");
                if (skyShader != null)
                {
                    sky = new Material(skyShader) { name = "City Drive Realistic Sky" };
                    if (sky.HasProperty("_Tint")) sky.SetColor("_Tint", new Color(0.36f, 0.52f, 0.78f));
                    if (sky.HasProperty("_Horizon")) sky.SetColor("_Horizon", new Color(0.74f, 0.82f, 0.90f));
                    if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", 1f);
                    RenderSettings.skybox = sky;
                    skyValid = true;
                }
            }
            else
            {
                // PDF: do not replace the supplied shader/texture; start neutral at 0° rotation.
                if (sky.HasProperty("_Rotation")) sky.SetFloat("_Rotation", 0f);
                if (sky.HasProperty("_Exposure") && sky.GetFloat("_Exposure") <= 0.01f)
                    sky.SetFloat("_Exposure", 1f);
            }

            if (skyValid)
            {
                RenderSettings.ambientMode = AmbientMode.Skybox;
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            }
            else
            {
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.58f, 0.66f, 0.78f);
                RenderSettings.ambientEquatorColor = new Color(0.40f, 0.43f, 0.48f);
                RenderSettings.ambientGroundColor = new Color(0.22f, 0.21f, 0.20f);
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 220f;
            RenderSettings.fogEndDistance = 700f;
            RenderSettings.fogColor = new Color(0.68f, 0.76f, 0.86f);

            Light sun = null;
            foreach (Light light in Object.FindObjectsOfType<Light>())
            {
                if (light.type == LightType.Directional) { sun = light; break; }
            }
            if (sun == null)
            {
                GameObject sunObject = new GameObject("Sun", typeof(Light));
                sunObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
                sun = sunObject.GetComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.color = new Color(1f, 0.96f, 0.90f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            sun.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Medium;
            sun.shadowBias = 0.02f;
            sun.shadowNormalBias = 0.02f;
            RenderSettings.sun = sun;

            if (Object.FindObjectOfType<ReflectionProbe>() == null)
            {
                GameObject probeObject = new GameObject("City Reflection Probe", typeof(ReflectionProbe));
                probeObject.transform.position = Vector3.zero;
                ReflectionProbe probe = probeObject.GetComponent<ReflectionProbe>();
                probe.mode = ReflectionProbeMode.Realtime;
                probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
                probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
                probe.size = new Vector3(90f, 40f, 90f);
                probe.intensity = 0.85f;
                probe.resolution = 128;
                probe.boxProjection = true;
                probe.RenderProbe();
            }
            DynamicGI.UpdateEnvironment();
        }

        private static GameObject SpawnCar(Vector3 spawn, Quaternion rotation, GameObject prefab)
        {
            GameObject car = new GameObject("BMW 330i xDrive - Player");
            car.transform.SetPositionAndRotation(spawn, rotation);

            GameObject visual = null;
            if (prefab != null)
            {
                visual = Instantiate(prefab);
                visual.name = "BMW 330i xDrive (GLB)";
                visual.transform.SetParent(car.transform, false);
                visual.transform.localScale = Vector3.one * ModelScale;
                UpgradeMaterials(visual);
                FitCarVisual(car, visual);
            }
            else
            {
                Debug.LogWarning("[CityDrive] BMW GLB missing; using a placeholder body.");
                GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
                placeholder.transform.SetParent(car.transform, false);
                placeholder.transform.localScale = new Vector3(1.9f, 1.2f, 4.4f);
            }

            BoxCollider collider = car.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0f, 0f);
            collider.size = new Vector3(1.9f, 1.4f, 4.3f);

            Rigidbody body = car.AddComponent<Rigidbody>();
            body.mass = 1420f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            VehicleVisuals visuals = car.AddComponent<VehicleVisuals>();
            visuals.FindWheels(car.transform);
            // No primitive steering wheel: sync only the model's own mesh if it exists.
            visuals.FindOriginalInteriorWheel(visual != null ? visual.transform : car.transform);

            ArcadeCarController controller = car.AddComponent<ArcadeCarController>();
            controller.SetVisuals(visuals);

            VehicleEffects effects = car.AddComponent<VehicleEffects>();
            effects.Car = controller;

            VehicleAudio audio = car.AddComponent<VehicleAudio>();
            audio.Car = controller;
            VehicleLights.Create(car.transform, controller);
            return car;
        }

        /// <summary>
        /// Keeps glTFast textures, upgrades the built-in materials to sensible PBR
        /// values, makes glass transparent and turns the existing lamp materials
        /// emissive. No geometry or texture is replaced.
        /// </summary>
        private static void UpgradeMaterials(GameObject model)
        {
            Shader standard = Shader.Find("Standard");
            if (standard == null) return;

            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    if (material == null)
                    {
                        materials[i] = new Material(standard) { name = "Fallback Body" };
                        changed = true;
                        continue;
                    }
                    if (material.shader == null || !material.shader.isSupported)
                    {
                        material.shader = standard;
                        changed = true;
                    }
                    ConfigurePbr(material);
                }
                if (changed) renderer.sharedMaterials = materials;
            }
        }

        private static void ConfigurePbr(Material material)
        {
            string name = material.name.ToLowerInvariant();
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.55f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.15f);

            if (name.Contains("body") || name.Contains("hood") || name.Contains("carpaint") ||
                name.Contains("bodykit"))
            {
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.72f);
                if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.88f);
            }
            else if (name.Contains("chrome") || name.Contains("rim") || name.Contains("steel") ||
                     name.Contains("pedal"))
            {
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.86f);
                if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.78f);
            }
            else if (name.Contains("tire") || name.Contains("tyre") || name.Contains("plast") ||
                     name.Contains("interior") || name.Contains("leather") || name.Contains("carpet"))
            {
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.02f);
                if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.42f);
            }

            bool glass = name.Contains("glass") || name.Contains("windshield") ||
                         name.Contains("window") || name.Contains("red_glass") || name.Contains("d_glass");
            if (glass)
            {
                material.SetFloat("_Mode", 3f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.EnableKeyword("_ALPHABLEND_ON");
                material.renderQueue = 3000;
                Color c = material.color;
                c.a = Mathf.Min(c.a, 0.42f);
                material.color = c;
                if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.95f);
            }

            if (name.Contains("light") || name.Contains("emissive"))
            {
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                material.SetColor("_EmissionColor", new Color(0.8f, 0.9f, 1f, 1f));
            }
        }

        /// <summary>
        /// Chooses the visual rotation from actual wheel positions: wheels BELOW the
        /// body and the front axle along +Z. Fixes upside-down glTF imports.
        /// </summary>
        private static void FitCarVisual(GameObject car, GameObject visual)
        {
            List<Transform> frontWheels = new List<Transform>();
            List<Transform> rearWheels = new List<Transform>();
            foreach (Transform t in visual.GetComponentsInChildren<Transform>(true))
            {
                string name = t.name;
                if (name == "polySurface3" || name == "polySurface4" ||
                    name == "WheelFL" || name == "WheelFR") frontWheels.Add(t);
                if (name == "polySurface1" || name == "polySurface2" ||
                    name == "WheelRL" || name == "WheelRR") rearWheels.Add(t);
            }

            Quaternion[] candidates =
            {
                Quaternion.identity,
                Quaternion.Euler(0f, 0f, 180f),
                Quaternion.Euler(0f, 180f, 180f),
                Quaternion.Euler(0f, 180f, 0f),
                Quaternion.Euler(180f, 0f, 0f),
                Quaternion.Euler(90f, 0f, 0f),
                Quaternion.Euler(-90f, 0f, 0f)
            };

            float bestScore = float.MaxValue;
            Quaternion bestRotation = Quaternion.identity;
            Bounds bestBounds = new Bounds();

            visual.transform.localPosition = Vector3.zero;
            foreach (Quaternion candidate in candidates)
            {
                visual.transform.localRotation = candidate;
                Physics.SyncTransforms();
                Bounds bounds = BoundsOf(visual);
                Vector3 size = bounds.size;
                float height = size.y;
                float length = Mathf.Max(size.x, size.z);
                float width = Mathf.Min(size.x, size.z);
                float score = Mathf.Abs(height - 1.35f) * 3f +
                              Mathf.Abs(length - 4.23f) +
                              Mathf.Abs(width - 1.89f) * 2f;

                if (frontWheels.Count > 0 && rearWheels.Count > 0)
                {
                    float wheelY = (AverageY(frontWheels) + AverageY(rearWheels)) * 0.5f - car.transform.position.y;
                    float bodyY = bounds.center.y - car.transform.position.y;
                    float frontZ = AverageZ(frontWheels) - car.transform.position.z;
                    float rearZ = AverageZ(rearWheels) - car.transform.position.z;
                    if (wheelY > bodyY - 0.15f) score += 60f;
                    if (frontZ <= rearZ) score += 25f;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    bestRotation = candidate;
                    bestBounds = bounds;
                }
            }

            visual.transform.localRotation = bestRotation;
            Physics.SyncTransforms();
            bestBounds = BoundsOf(visual);
            Vector3 center = bestBounds.center - car.transform.position;
            visual.transform.localPosition = new Vector3(
                -center.x,
                -0.65f - bestBounds.min.y + car.transform.position.y,
                -center.z);
            Debug.Log("[CityDrive] BMW visual fitted, score=" + bestScore +
                      " size=" + bestBounds.size + " rotation=" + bestRotation.eulerAngles);
        }

        private static float AverageY(List<Transform> transforms)
        {
            float total = 0f;
            foreach (Transform t in transforms) total += t.position.y;
            return total / Mathf.Max(1, transforms.Count);
        }

        private static float AverageZ(List<Transform> transforms)
        {
            float total = 0f;
            foreach (Transform t in transforms) total += t.position.z;
            return total / Mathf.Max(1, transforms.Count);
        }

        private static Bounds BoundsOf(GameObject root)
        {
            bool first = true;
            Bounds result = new Bounds();
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || renderer.bounds.size == Vector3.zero) continue;
                if (first)
                {
                    result = renderer.bounds;
                    first = false;
                }
                else result.Encapsulate(renderer.bounds);
            }
            return result;
        }
    }
}
