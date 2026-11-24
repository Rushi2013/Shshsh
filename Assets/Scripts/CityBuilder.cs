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

                if (readable && ((road && triangles <= 50000) || triangles <= 6000))
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
                    collider.size = local.size;
                    boxColliders++;
                }

                go.isStatic = true;
            }

            Debug.Log("[CityDrive] City colliders: " + meshColliders + " mesh / " + boxColliders + " box.");
        }

        /// <summary>Finds a flat surface near the middle of town to spawn the car on.</summary>
        public Vector3 FindSpawnPoint()
        {
            Vector3 centre = Bounds.center;
            Vector3[] offsets =
            {
                Vector3.zero,
                new Vector3(10f, 0f, 0f), new Vector3(-10f, 0f, 0f),
                new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, -10f),
                new Vector3(20f, 0f, 20f), new Vector3(-20f, 0f, -20f)
            };

            RaycastHit[] hits = new RaycastHit[32];
            foreach (Vector3 offset in offsets)
            {
                Vector3 origin = new Vector3(centre.x + offset.x, Bounds.max.y + 10f, centre.z + offset.z);
                int count = Physics.RaycastNonAlloc(origin, Vector3.down, hits,
                    Bounds.size.y + 30f, ~0, QueryTriggerInteraction.Ignore);
                float lowestY = float.MaxValue;
                Vector3 best = Vector3.zero;
                bool found = false;

                for (int i = 0; i < count; i++)
                {
                    RaycastHit hit = hits[i];
                    if (hit.collider == null || hit.normal.y < 0.75f) continue;
                    if (hit.point.y < lowestY && hit.point.y > -1f)
                    {
                        lowestY = hit.point.y;
                        best = hit.point;
                        found = true;
                    }
                }

                if (found) return best + Vector3.up * 1.1f;
            }

            Debug.LogWarning("[CityDrive] No road found under the town centre; using a safe fallback spawn.");
            return new Vector3(centre.x, 2f, centre.z);
        }
    }
}
