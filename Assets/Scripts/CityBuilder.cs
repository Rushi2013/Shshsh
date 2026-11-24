using System.Collections;
using UnityEngine;

namespace CityDrive
{
    /// <summary>
    /// Loads the supplied Sketchfab city GLB (imported by glTFast at edit time),
    /// centres it on the origin and gives every useful mesh a collider so the car
    /// can drive on roads and crash into buildings.
    /// </summary>
    public sealed class CityBuilder : MonoBehaviour
    {
        private GameObject _city;

        public Bounds Bounds { get; private set; }
        public bool IsReady { get; private set; }
        public GameObject Root => _city;
        public Quaternion SpawnRotation { get; private set; } = Quaternion.identity;

        public IEnumerator Build()
        {
            GameObject prefab = Resources.Load<GameObject>("City/city");
            if (prefab == null)
            {
                Debug.LogError("[CityDrive] Assets/Resources/City/city.glb could not be loaded. " +
                               "Make sure com.unity.cloud.gltfast is installed so the GLB imports as a prefab.");
                Bounds = new Bounds(Vector3.zero, new Vector3(200f, 10f, 200f));
                IsReady = true;
                yield break;
            }

            _city = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            _city.name = "City (GLB)";
            // Sketchfab's root node converts Z-up to glTF Y-up. glTFast mirrors the
            // scene into Unity's left-handed space, which leaves the town lying on
            // its side. Rotating +90 degrees on X puts the buildings upright again.
            _city.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            yield return null; // let the imported hierarchy settle for one frame

            RecalculateBounds();

            // Put the ground at y = 0 and the town centre at (0, 0, 0).
            Vector3 offset = new Vector3(-Bounds.center.x, -Bounds.min.y, -Bounds.center.z);
            _city.transform.position += offset;
            yield return null;

            RecalculateBounds();
            AddColliders();
            IsReady = true;
        }

        private void RecalculateBounds()
        {
            Renderer[] renderers = _city.GetComponentsInChildren<Renderer>(true);
            bool first = true;
            Bounds result = new Bounds(Vector3.zero, Vector3.zero);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || renderer.bounds.size == Vector3.zero) continue;
                if (first)
                {
                    result = renderer.bounds;
                    first = false;
                }
                else
                {
                    result.Encapsulate(renderer.bounds);
                }
            }

            if (first) result = new Bounds(Vector3.zero, new Vector3(200f, 10f, 200f));
            Bounds = result;
        }

        private static bool LooksLikeRoad(string name)
        {
            string lower = name.ToLowerInvariant();
            return lower.Contains("road") || lower.Contains("street") || lower.Contains("ground") ||
                   lower.Contains("floor") || lower.Contains("asphalt") || lower.Contains("pavement") ||
                   lower.Contains("sidewalk") || lower.Contains("terrain") || lower.Contains("surface");
        }

        private void AddColliders()
        {
            MeshFilter[] filters = _city.GetComponentsInChildren<MeshFilter>(true);
            int meshColliders = 0;
            int boxColliders = 0;

            foreach (MeshFilter filter in filters)
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null) continue;

                GameObject go = filter.gameObject;
                if (go.GetComponent<Collider>() != null) continue;

                Bounds local = mesh.bounds;
                float biggest = Mathf.Max(local.size.x, local.size.y, local.size.z);
                if (biggest < 0.3f) continue; // tiny decoration, not worth a collider

                int triangles = mesh.triangles.Length / 3;
                bool road = LooksLikeRoad(go.name) ||
                            (go.transform.parent != null && LooksLikeRoad(go.transform.parent.name));
                bool readable = mesh.isReadable;

                if (readable && ((road && triangles <= 200000) || triangles <= 6000))
                {
                    MeshCollider collider = go.AddComponent<MeshCollider>();
                    collider.sharedMesh = mesh;
                    collider.convex = false;
                    meshColliders++;
                }
                else if (triangles <= 200000 || biggest > 4f)
                {
                    // Cheap approximation for detailed buildings/statues.
                    BoxCollider collider = go.AddComponent<BoxCollider>();
                    collider.center = local.center;
                    collider.size = new Vector3(
                        Mathf.Max(local.size.x, 0.05f),
                        Mathf.Max(local.size.y, 0.1f),
                        Mathf.Max(local.size.z, 0.05f));
                    boxColliders++;
                }

                go.isStatic = true;
            }

            Debug.Log("[CityDrive] City colliders: " + meshColliders + " mesh / " + boxColliders + " box.");
        }

        /// <summary>Picks a real road mesh near the town centre and spawns the car on it.</summary>
        public Vector3 FindSpawnPoint()
        {
            Vector3 centre = Bounds.center;
            Renderer[] renderers = _city.GetComponentsInChildren<Renderer>(true);
            Renderer bestRoad = null;
            float bestDistance = float.MaxValue;

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || renderer.bounds.size.y > 8f) continue;
                string name = renderer.gameObject.name.ToLowerInvariant();
                if (!(name.Contains("road") || name.Contains("street") || name.Contains("asphalt"))) continue;
                if (name.Contains("sidewalk")) continue;
                float distance = Vector2.Distance(
                    new Vector2(renderer.bounds.center.x, renderer.bounds.center.z),
                    new Vector2(centre.x, centre.z));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestRoad = renderer;
                }
            }

            if (bestRoad != null)
            {
                Vector3 origin = bestRoad.bounds.center + Vector3.up * 20f;
                RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 60f,
                    ~0, QueryTriggerInteraction.Ignore);
                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider == null || hit.normal.y < 0.9f) continue;
                    string name = hit.collider.gameObject.name.ToLowerInvariant();
                    if (!(name.Contains("road") || name.Contains("street") || name.Contains("asphalt"))) continue;
                    Vector3 size = bestRoad.bounds.size;
                    SpawnRotation = size.x > size.z
                        ? Quaternion.Euler(0f, 90f, 0f)
                        : Quaternion.identity;
                    Debug.Log("[CityDrive] Spawning on road: " + hit.collider.gameObject.name);
                    return hit.point + Vector3.up * 1.1f;
                }
            }

            Debug.LogWarning("[CityDrive] Road mesh not found; falling back to a raycast near the centre.");
            Vector3[] offsets =
            {
                Vector3.zero,
                new Vector3(10f, 0f, 0f), new Vector3(-10f, 0f, 0f),
                new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, -10f)
            };
            foreach (Vector3 offset in offsets)
            {
                Vector3 origin = new Vector3(centre.x + offset.x, Bounds.max.y + 10f, centre.z + offset.z);
                RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, Bounds.size.y + 30f,
                    ~0, QueryTriggerInteraction.Ignore);
                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider != null && hit.normal.y > 0.75f && hit.point.y > -1f)
                        return hit.point + Vector3.up * 1.1f;
                }
            }

            return new Vector3(centre.x, 2f, centre.z);
        }
    }
}
