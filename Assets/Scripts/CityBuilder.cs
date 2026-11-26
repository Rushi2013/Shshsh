using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityDrive
{
    /// <summary>
    /// Loads the user's NEW city model (CIT.obj) and derives its lane graph from
    /// the actual flat road-surface mesh in that file. The graph was extracted
    /// offline from the 208 flat quads of the source mesh (RoadGraph.txt); at
    /// runtime every node/edge is raycast against the imported road collider
    /// before it is published to traffic. Old city/fallback ground are not used.
    /// </summary>
    public sealed class CityBuilder : MonoBehaviour
    {
        private GameObject _city;
        private Transform _roadTransform;
        private readonly List<Vector3> _lanes = new List<Vector3>();
        private readonly List<List<int>> _neighbors = new List<List<int>>();
        private int _validEdges;

        public Bounds Bounds { get; private set; }
        public bool IsReady { get; private set; }
        public GameObject Root => _city;
        public Quaternion SpawnRotation { get; private set; } = Quaternion.identity;
        public Vector3[] LaneWaypoints => _lanes.ToArray();
        public int[] NeighborsOf(int node) => node >= 0 && node < _neighbors.Count
            ? _neighbors[node].ToArray() : new int[0];
        public float LaneWidth { get; private set; } = 12f;

        public IEnumerator Build(GameObject prefab = null)
        {
            if (prefab == null) prefab = Resources.Load<GameObject>("CityNew/CIT");
            if (prefab == null)
            {
                Debug.LogError("[CityDrive] New city CIT.obj was not imported under Resources/CityNew.");
                Bounds = new Bounds(Vector3.zero, new Vector3(300f, 10f, 300f));
                IsReady = true;
                yield break;
            }

            _city = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            _city.name = "New City (CIT.obj)";
            OrientCity();
            yield return null;

            ScaleAndGround();
            yield return null;
            AddColliders();
            yield return null;
            BuildRoadGraph();
            SpawnRotation = SpawnRotationForFirstLane();
            ConfigureVisuals();
            IsReady = true;
        }

        private void OrientCity()
        {
            Quaternion[] candidates =
            {
                Quaternion.identity,
                Quaternion.Euler(0f, 180f, 0f),
                Quaternion.Euler(90f, 0f, 0f),
                Quaternion.Euler(-90f, 0f, 0f)
            };
            float best = float.MaxValue;
            Quaternion chosen = Quaternion.identity;
            foreach (Quaternion candidate in candidates)
            {
                _city.transform.rotation = candidate;
                Physics.SyncTransforms();
                Bounds b = ComputeBounds();
                // Buildings define the vertical axis; the sky plane is ignored later.
                float score = Mathf.Abs(b.size.y - 60f) * 0.4f + Mathf.Abs(b.size.x - b.size.z) * 0.05f;
                if (score < best) { best = score; chosen = candidate; }
            }
            _city.transform.rotation = chosen;
            Physics.SyncTransforms();
            Debug.Log("[CityDrive] New city orientation " + chosen.eulerAngles + " score " + best.ToString("F1"));
        }

        private void ScaleAndGround()
        {
            Bounds raw = ComputeBounds();
            float footprint = Mathf.Max(raw.size.x, raw.size.z);
            // Sky plane inflates the raw OBJ bounds; keep the city close to source scale.
            float scale = footprint > 1f ? Mathf.Clamp(600f / footprint, 0.45f, 1.5f) : 1f;
            _city.transform.localScale *= scale;
            Physics.SyncTransforms();

            Bounds scaled = ComputeBounds();
            _city.transform.position += new Vector3(-scaled.center.x, -scaled.min.y, -scaled.center.z);
            Physics.SyncTransforms();
            RecalculateBounds();
        }

        private Bounds ComputeBounds()
        {
            Renderer[] renderers = _city.GetComponentsInChildren<Renderer>(true);
            bool first = true;
            Bounds result = new Bounds(Vector3.zero, Vector3.zero);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || renderer.bounds.size == Vector3.zero) continue;
                if (first) { result = renderer.bounds; first = false; }
                else result.Encapsulate(renderer.bounds);
            }
            if (first) result = new Bounds(Vector3.zero, new Vector3(300f, 10f, 300f));
            return result;
        }

        private void RecalculateBounds() => Bounds = ComputeBounds();

        private void AddColliders()
        {
            int mesh = 0;
            int skipped = 0;
            foreach (MeshFilter filter in _city.GetComponentsInChildren<MeshFilter>(true))
            {
                GameObject go = filter.gameObject;
                string name = go.name.ToLowerInvariant();
                if (name.Contains("skies") || name.Contains("hdri")) continue; // sky is not ground
                if (go.GetComponent<Collider>() != null) continue;
                Mesh meshAsset = filter.sharedMesh;
                if (meshAsset == null) continue;

                // The source has a few very long building aggregate meshes. Single
                // box colliders around these would seal the roads, so skip them;
                // road and individual building meshes get accurate mesh colliders.
                Bounds local = meshAsset.bounds;
                bool largeAggregate = !name.Contains("plane") &&
                    (local.size.x > 140f || local.size.z > 140f) && meshAsset.triangles.Length / 3 > 20000;
                if (largeAggregate) { skipped++; continue; }

                MeshCollider collider = go.AddComponent<MeshCollider>();
                collider.sharedMesh = meshAsset;
                collider.convex = false;
                mesh++;
            }
            Debug.Log("[CityDrive] New city colliders: " + mesh + " mesh; skipped " + skipped +
                      " oversized building aggregates.");
        }

        /// <summary>
        /// Finds the flat surface mesh that carries the road grid. Name alone is not
        /// enough: it must have many vertices, span the city, and be almost perfectly
        /// flat. This matches the measured Plane.001 source geometry (208 flat quads).
        /// </summary>
        private Transform FindRoadSurface()
        {
            Transform best = null;
            float bestScore = float.MaxValue;
            foreach (MeshFilter filter in _city.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null || mesh.vertexCount < 150) continue;
                Bounds b = mesh.bounds;
                if (b.size.x < 150f || b.size.z < 150f || b.size.y > 8f) continue;
                float score = b.size.y + (filter.gameObject.name.ToLowerInvariant().Contains("plane.001") ? 0f : 4f);
                if (score < bestScore) { bestScore = score; best = filter.transform; }
            }
            return best;
        }

        private void BuildRoadGraph()
        {
            _lanes.Clear();
            _neighbors.Clear();
            _validEdges = 0;
            _roadTransform = FindRoadSurface();
            if (_roadTransform == null)
            {
                Debug.LogError("[CityDrive] Could not identify the flat road mesh in CIT.obj; " +
                               "player spawn and traffic are disabled until road geometry is authored.");
                return;
            }

            TextAsset graph = Resources.Load<TextAsset>("CityNew/RoadGraph");
            if (graph == null)
            {
                Debug.LogError("[CityDrive] Missing Resources/CityNew/RoadGraph.txt; no road lane graph.");
                return;
            }

            string[] lines = graph.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2 || !lines[0].StartsWith("NODES ")) return;
            string[] header = lines[0].Split(' ');
            int nodeCount = int.Parse(header[1], CultureInfo.InvariantCulture);
            int edgeCount = int.Parse(header[2], CultureInfo.InvariantCulture);
            Vector3[] local = new Vector3[nodeCount];
            bool[] valid = new bool[nodeCount];

            for (int i = 0; i < nodeCount && i + 1 < lines.Length; i++)
            {
                string[] parts = lines[i + 1].Split(' ');
                if (parts.Length < 4 || parts[0] != "N") continue;
                float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
                float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
                float z = float.Parse(parts[3], CultureInfo.InvariantCulture);
                local[i] = new Vector3(x, y, z);
                Vector3 world = _roadTransform.TransformPoint(local[i]);
                valid[i] = HitsRoad(world, out Vector3 surface);
                if (valid[i])
                {
                    // Keep the extracted road height, corrected to the actual collider.
                    _lanes.Add(new Vector3(world.x, surface.y, world.z));
                }
                else _lanes.Add(world); // stable index mapping; node is filtered via valid[]
            }

            for (int i = 0; i < nodeCount; i++) _neighbors.Add(new List<int>());
            int edgeIndex = 1 + nodeCount;
            for (int e = 0; e < edgeCount && edgeIndex + e < lines.Length; e++)
            {
                string[] parts = lines[edgeIndex + e].Split(' ');
                if (parts.Length < 3 || parts[0] != "E") continue;
                int a = int.Parse(parts[1], CultureInfo.InvariantCulture);
                int b = int.Parse(parts[2], CultureInfo.InvariantCulture);
                if (a < 0 || b < 0 || a >= nodeCount || b >= nodeCount || !valid[a] || !valid[b]) continue;
                Vector3 mid = Vector3.Lerp(_lanes[a], _lanes[b], 0.5f);
                if (!HitsRoad(mid, out Vector3 surface)) continue;
                // A lane segment must stay on the road surface, not jump across a gap.
                if (Mathf.Abs(surface.y - mid.y) > 1.5f) continue;
                _neighbors[a].Add(b);
                _neighbors[b].Add(a);
                _validEdges++;
            }

            int usable = 0;
            for (int i = 0; i < nodeCount; i++) if (valid[i] && _neighbors[i].Count > 0) usable++;
            Debug.Log("[CityDrive] Road graph: " + usable + " validated nodes / " + _validEdges +
                      " validated edges on " + _roadTransform.name + ".");
        }

        private bool HitsRoad(Vector3 world, out Vector3 surface)
        {
            surface = world;
            if (_roadTransform == null) return false;
            Vector3 origin = world + Vector3.up * 30f;
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 80f, ~0, QueryTriggerInteraction.Ignore))
                return false;
            Transform t = hit.collider.transform;
            if (t != _roadTransform && !t.IsChildOf(_roadTransform)) return false;
            surface = hit.point;
            return true;
        }

        private void ConfigureVisuals()
        {
            foreach (Renderer renderer in _city.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null) continue;
                string name = renderer.gameObject.name.ToLowerInvariant();
                if (name.Contains("skies") || name.Contains("hdri"))
                {
                    renderer.enabled = false; // UniStorm owns the sky
                    continue;
                }
                bool sizeable = renderer.bounds.size.sqrMagnitude > 0.25f;
                renderer.shadowCastingMode = sizeable ? ShadowCastingMode.On : ShadowCastingMode.Off;
                renderer.receiveShadows = sizeable;
            }
        }

        public Vector3 FindSpawnPoint()
        {
            for (int i = 0; i < _neighbors.Count; i++)
            {
                if (_neighbors[i].Count == 0) continue;
                return _lanes[i] + Vector3.up * 1.1f;
            }
            Debug.LogError("[CityDrive] No validated road node for player spawn.");
            return Vector3.up * 2f;
        }

        public Quaternion SpawnRotationForFirstLane()
        {
            for (int i = 0; i < _neighbors.Count; i++)
            {
                if (_neighbors[i].Count == 0) continue;
                Vector3 direction = _lanes[_neighbors[i][0]] - _lanes[i];
                direction.y = 0f;
                return direction.sqrMagnitude > 0.01f
                    ? Quaternion.LookRotation(direction.normalized, Vector3.up) : Quaternion.identity;
            }
            return Quaternion.identity;
        }
    }
}
