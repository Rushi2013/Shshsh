using System.Collections;
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
        /// Picks the X rotation that makes the imported GLB a normal upright car, then
        /// centres it on the physics body and puts its wheels near the spring raycasts.
        /// </summary>
        private static void FitCarVisual(GameObject car, GameObject visual)
        {
            float[] angles = { 90f, 0f, -90f, 180f };
            float bestScore = float.MaxValue;
            float bestAngle = 90f;
            Bounds bestBounds = new Bounds();

            visual.transform.localPosition = Vector3.zero;
            foreach (float angle in angles)
            {
                visual.transform.localRotation = Quaternion.Euler(angle, 0f, 0f);
                Bounds bounds = BoundsOf(visual);
                Vector3 size = bounds.size;
                float height = size.y;
                float length = Mathf.Max(size.x, size.z);
                float width = Mathf.Min(size.x, size.z);
                float score = Mathf.Abs(height - 1.35f) * 3f +
                              Mathf.Abs(length - 4.23f) +
                              Mathf.Abs(width - 1.89f) * 2f;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestAngle = angle;
                    bestBounds = bounds;
                }
            }

            visual.transform.localRotation = Quaternion.Euler(bestAngle, 0f, 0f);
            Vector3 center = bestBounds.center - car.transform.position;
            visual.transform.localPosition = new Vector3(
                -center.x,
                -0.65f - bestBounds.min.y + car.transform.position.y,
                -center.z);
            Debug.Log("[CityDrive] BMW visual fitted at X=" + bestAngle +
                      " size=" + bestBounds.size);
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
