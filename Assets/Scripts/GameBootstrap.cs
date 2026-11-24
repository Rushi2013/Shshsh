using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CityDrive
{
    /// <summary>
    /// Entry point. Shows a real-time loading screen, builds the city from the supplied
    /// GLB, spawns the BMW 330i xDrive GLB with rigidbody physics, then presents a 3D
    /// home screen. Pressing play drops the same car onto a city road.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        private const string CarResource = "Vehicles/BMW330i/bmw330i";
        private const float ModelScale = 90f;

        private IEnumerator Start()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            QualitySettings.shadowDistance = 90f;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            LoadingScreen loading = gameObject.AddComponent<LoadingScreen>();
            loading.Show();
            loading.SetProgress(0.05f, "GAME LOADING", "Checking game assets...");

            GameObject cityHost = new GameObject("City Loader");
            CityBuilder city = cityHost.AddComponent<CityBuilder>();
            loading.SetProgress(0.20f, "DOWNLOADING ASSETS", "Loading 3D city and textures...");
            yield return StartCoroutine(city.Build());
            loading.SetProgress(0.72f, "BUILDING CITY", "Placing roads and buildings...");
            CreateGroundFallback(cityHost.transform, city.Bounds);

            Vector3 spawn = city.FindSpawnPoint();
            Quaternion spawnRotation = city.SpawnRotation;
            GameObject car = SpawnCar(spawn, spawnRotation);
            ArcadeCarController controller = car.GetComponent<ArcadeCarController>();
            controller.enabled = false;

            Camera cameraComponent = Camera.main;
            if (cameraComponent == null)
            {
                GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                cameraComponent = cameraObject.GetComponent<Camera>();
            }
            cameraComponent.fieldOfView = 65f;
            cameraComponent.farClipPlane = 1200f;

            ChaseCamera chase = cameraComponent.gameObject.GetComponent<ChaseCamera>();
            if (chase == null) chase = cameraComponent.gameObject.AddComponent<ChaseCamera>();
            chase.Target = car.transform;
            chase.Car = controller;

            loading.SetProgress(1f, "READY TO DRIVE", "Welcome to City Drive");
            yield return new WaitForSecondsRealtime(0.15f);
            loading.Hide();

            HomeScreen home = gameObject.AddComponent<HomeScreen>();
            home.Show(city, car, controller, chase, cameraComponent, spawn, spawnRotation, loading, () =>
            {
                GameObject controlsHost = new GameObject("Controls");
                MobileControls controls = controlsHost.AddComponent<MobileControls>();
                controls.Car = controller;
                controls.Chase = chase;
            });
        }

        /// <summary>Flat safety surface just below the city so the tyres always have a road.</summary>
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
                material.color = new Color(0.24f, 0.25f, 0.27f);
                ground.GetComponent<Renderer>().material = material;
            }
        }

        private static GameObject SpawnCar(Vector3 spawn, Quaternion rotation)
        {
            GameObject car = new GameObject("BMW 330i xDrive - Player");
            car.transform.SetPositionAndRotation(spawn, rotation);

            GameObject prefab = Resources.Load<GameObject>(CarResource);
            if (prefab != null)
            {
                GameObject visual = Instantiate(prefab);
                visual.name = "BMW 330i xDrive (GLB)";
                visual.transform.SetParent(car.transform, false);
                visual.transform.localScale = Vector3.one * ModelScale;
                RepairMaterials(visual);
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
            body.mass = 1250f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            VehicleVisuals visuals = car.AddComponent<VehicleVisuals>();
            visuals.FindWheels(car.transform);

            ArcadeCarController controller = car.AddComponent<ArcadeCarController>();
            controller.SetVisuals(visuals);
            return car;
        }

        /// <summary>
        /// Chooses the visual rotation from the actual wheel positions: wheels must
        /// sit BELOW the body and the front wheels must point along +Z of the car.
        /// This avoids the upside-down model regardless of how glTFast imported it.
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

                    // Wheels must be below the body; front axle must be at +Z.
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

        /// <summary>
        /// Keeps the glTFast PBR textures and swaps only shaders that cannot run on
        /// this device for Unity's built-in Standard shader.
        /// </summary>
        private static void RepairMaterials(GameObject model)
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
                    }
                    else if (material.shader == null || !material.shader.isSupported)
                    {
                        material.shader = standard;
                        changed = true;
                    }
                }
                if (changed) renderer.sharedMaterials = materials;
            }
        }
    }
}
