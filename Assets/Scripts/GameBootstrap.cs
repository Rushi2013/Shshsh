using System.Collections;
using UnityEngine;

namespace CityDrive
{
    /// <summary>
    /// Entry point. Builds the town from the supplied GLB, spawns the BMW with full
    /// rigidbody physics, attaches the cameras and creates the touch HUD at runtime.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        private const string CarResource = "Vehicles/BMW/2005 BMW M3 GTR E46 - Empty";

        private IEnumerator Start()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            QualitySettings.shadowDistance = 90f;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            GameObject cityHost = new GameObject("City Loader");
            CityBuilder city = cityHost.AddComponent<CityBuilder>();
            yield return StartCoroutine(city.Build());

            Vector3 spawn = city.FindSpawnPoint();
            GameObject car = SpawnCar(spawn);

            Camera cameraComponent = Camera.main;
            if (cameraComponent == null)
            {
                GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                cameraComponent = cameraObject.GetComponent<Camera>();
            }
            cameraComponent.fieldOfView = 65f;
            cameraComponent.farClipPlane = 1200f;
            cameraComponent.transform.position = spawn + new Vector3(0f, 5f, -10f);

            ChaseCamera chase = cameraComponent.gameObject.GetComponent<ChaseCamera>();
            if (chase == null) chase = cameraComponent.gameObject.AddComponent<ChaseCamera>();
            chase.Target = car.transform;
            chase.Car = car.GetComponent<ArcadeCarController>();

            GameObject controlsHost = new GameObject("Controls");
            MobileControls controls = controlsHost.AddComponent<MobileControls>();
            controls.Car = chase.Car;
            controls.Chase = chase;
        }

        private static GameObject SpawnCar(Vector3 spawn)
        {
            GameObject prefab = Resources.Load<GameObject>(CarResource);
            GameObject car;

            if (prefab != null)
            {
                car = Instantiate(prefab, spawn, Quaternion.identity);
            }
            else
            {
                // Fallback so the game still runs if the model folder was removed.
                Debug.LogWarning("[CityDrive] BMW prefab missing; using a placeholder body.");
                car = GameObject.CreatePrimitive(PrimitiveType.Cube);
                car.transform.position = spawn;
                car.transform.localScale = new Vector3(1.9f, 1.2f, 4.4f);
            }

            car.name = "BMW M3 GTR - Player";
            RepairMaterials(car);

            BoxCollider collider = car.GetComponent<BoxCollider>();
            if (collider == null) collider = car.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.62f, 0f);
            collider.size = new Vector3(1.86f, 1.24f, 4.35f);

            Rigidbody body = car.GetComponent<Rigidbody>();
            if (body == null) body = car.AddComponent<Rigidbody>();
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
        /// The original asset uses a few custom shaders that are not bundled. Anything
        /// that failed to resolve is swapped for Unity's built-in Standard shader.
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
