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
        private MeshCollider _roadCollider;
        private bool _hasRoadSpawn;
        private readonly Dictionary<Material, Material> _cityMaterials = new Dictionary<Material, Material>();
        private Vector3 _roadSpawn;
        private readonly List<Vector3> _lanes = new List<Vector3>();
        private readonly List<List<int>> _neighbors = new List<List<int>>();
        private int _validEdges;

        public Bounds Bounds { get; private set; }
        public bool IsReady { get; private set; }
        public bool HasSafeSpawn => _hasRoadSpawn;
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
            // CIT is Y-up in the source: the roads lie at y≈3 and building
            // vertices rise above them. Do not score rotations against the sky
            // billboard's huge bounds: that can turn the whole city on its side.
            _city.transform.rotation = Quaternion.identity;
            Physics.SyncTransforms();
            Transform road = FindRoadSurface();
            if (road == null) return;
            float roadY = road.GetComponent<Renderer>().bounds.center.y;
            int above = 0, below = 0;
            foreach (Renderer r in _city.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || r.transform == road || IsSky(r.name)) continue;
                if (r.bounds.center.y > roadY + 1f) above++;
                if (r.bounds.center.y < roadY - 1f) below++;
            }
            if (below > above)
                _city.transform.rotation = Quaternion.Euler(180f, 0f, 0f);
            Physics.SyncTransforms();
            Debug.Log("[CityDrive] City orientation " + _city.transform.rotation.eulerAngles);
        }

        private static bool IsSky(string name)
        {
            string lower = name.ToLowerInvariant();
            return lower.Contains("skies") || lower.Contains("hdri");
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
                if (renderer == null || IsSky(renderer.name) || renderer.bounds.size == Vector3.zero) continue;
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
            _roadTransform = FindRoadSurface();
            if (_roadTransform != null) OrientRoadFacesUp(_roadTransform.GetComponent<MeshFilter>());
            foreach (MeshFilter filter in _city.GetComponentsInChildren<MeshFilter>(true))
            {
                GameObject go = filter.gameObject;
                string name = go.name.ToLowerInvariant();
                if (name.Contains("skies") || name.Contains("hdri")) continue; // sky is not ground
                Collider existing = go.GetComponent<Collider>();
                if (existing != null)
                {
                    if (go.transform == _roadTransform) _roadCollider = existing as MeshCollider;
                    continue;
                }
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
                if (go.transform == _roadTransform) _roadCollider = collider;
                mesh++;
            }
            Debug.Log("[CityDrive] New city colliders: " + mesh + " mesh; skipped " + skipped +
                      " oversized building aggregates.");
        }

        // The source road contains downward-facing triangles. Unity physics normally
        // raycasts only front faces, so reverse the road mesh when its winding points
        // down. Keep all other city geometry and materials intact.
        private void OrientRoadFacesUp(MeshFilter filter)
        {
            if (filter == null || filter.sharedMesh == null || !filter.sharedMesh.isReadable) return;
            Mesh source = filter.sharedMesh;
            int[] triangles = source.triangles;
            Vector3[] vertices = source.vertices;
            double orientation = 0;
            for (int i = 0; i + 2 < triangles.Length; i += 3)
            {
                Vector3 a = filter.transform.TransformPoint(vertices[triangles[i]]);
                Vector3 b = filter.transform.TransformPoint(vertices[triangles[i + 1]]);
                Vector3 c = filter.transform.TransformPoint(vertices[triangles[i + 2]]);
                orientation += Vector3.Cross(b - a, c - a).y;
            }
            if (orientation >= 0) return;
            Mesh corrected = Instantiate(source);
            corrected.name = source.name + " (upward road)";
            for (int sub = 0; sub < corrected.subMeshCount; sub++)
            {
                int[] indices = corrected.GetTriangles(sub);
                for (int i = 0; i + 2 < indices.Length; i += 3)
                { int temp = indices[i + 1]; indices[i + 1] = indices[i + 2]; indices[i + 2] = temp; }
                corrected.SetTriangles(indices, sub);
            }
            corrected.RecalculateNormals();
            filter.sharedMesh = corrected;
            Debug.Log("[CityDrive] Corrected downward-facing road triangles.");
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

            Physics.SyncTransforms();
            FindRoadMeshSpawn();
            TextAsset graph = Resources.Load<TextAsset>("CityNew/RoadGraph");
            if (graph == null)
            {
                Debug.LogError("[CityDrive] Missing Resources/CityNew/RoadGraph.txt; no road lane graph.");
                return;
            }

            Physics.SyncTransforms();
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
            }

            // OBJ import can mirror an axis; select the conversion with the most
            // node hits on the actual road collider rather than assuming one.
            Vector2[] signs = { new Vector2(1, 1), new Vector2(-1, 1),
                                new Vector2(1, -1), new Vector2(-1, -1) };
            int bestHits = -1;
            Vector2 bestSign = signs[0];
            foreach (Vector2 sign in signs)
            {
                int hits = 0;
                for (int i = 0; i < nodeCount; i++)
                {
                    Vector3 point = new Vector3(local[i].x * sign.x, local[i].y, local[i].z * sign.y);
                    if (HitsRoad(_roadTransform.TransformPoint(point), out _)) hits++;
                }
                if (hits > bestHits) { bestHits = hits; bestSign = sign; }
            }
            for (int i = 0; i < nodeCount; i++)
            {
                Vector3 point = new Vector3(local[i].x * bestSign.x, local[i].y, local[i].z * bestSign.y);
                Vector3 world = _roadTransform.TransformPoint(point);
                valid[i] = HitsRoad(world, out Vector3 surface);
                _lanes.Add(valid[i] ? surface : world);
                if (valid[i] && !_hasRoadSpawn) { _roadSpawn = surface; _hasRoadSpawn = true; }
            }
            Debug.Log("[CityDrive] Road graph coordinate mapping " + bestSign + ", hits " + bestHits);

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

        private void FindRoadMeshSpawn()
        {
            if (_roadTransform == null || _roadCollider == null) return;
            MeshFilter filter = _roadTransform.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null || !filter.sharedMesh.isReadable) return;
            Vector3[] vertices = filter.sharedMesh.vertices;
            int[] triangles = filter.sharedMesh.triangles;
            for (int i = 0; i + 2 < triangles.Length; i += 3)
            {
                Vector3 local = (vertices[triangles[i]] + vertices[triangles[i + 1]] + vertices[triangles[i + 2]]) / 3f;
                if (!HitsRoad(_roadTransform.TransformPoint(local), out Vector3 point)) continue;
                _roadSpawn = point;
                _hasRoadSpawn = true;
                return;
            }
        }

        private bool HitsRoad(Vector3 world, out Vector3 surface)
        {
            surface = world;
            if (_roadCollider == null) return false;
            Vector3 origin = world + Vector3.up * 30f;
            if (!_roadCollider.Raycast(new Ray(origin, Vector3.down), out RaycastHit hit, 80f)) return false;
            if (hit.normal.y < 0.5f) return false;
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
                ApplyApproximateCityMaterials(renderer);
                bool sizeable = renderer.bounds.size.sqrMagnitude > 0.25f;
                renderer.shadowCastingMode = sizeable ? ShadowCastingMode.On : ShadowCastingMode.Off;
                renderer.receiveShadows = sizeable;
            }
        }

        // The supplied MTL has no road/building image links. These are visibly
        // approximate replacements, not a claim to recover the original mapping.
        private void ApplyApproximateCityMaterials(Renderer renderer)
        {
            Material[] materials = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < materials.Length; i++)
            {
                Material original = materials[i];
                if (original == null) continue;
                string id = original.name.ToLowerInvariant();
                string texture = null;
                if (id.Contains("material.007") || id.Contains("material.006") || id.Contains("wall"))
                    texture = "TexturesCom_ConcreteBare0321_1_seamless_S";
                else if (id.Contains("build3.001")) texture = "TexturesCom_BuildingsHighRise0504_2_M";
                else if (id.Contains("build3.002")) texture = "TexturesCom_HighRiseResidential0139_1_seamless_S";
                else if (id.Contains("build3")) texture = "TexturesCom_BuildingsHighRise0368_1_seamless_S";
                else if (id.Contains("material.004")) texture = "TexturesCom_HighRiseResidential0103_1_seamless_S";
                if (texture == null) continue;
                if (!_cityMaterials.TryGetValue(original, out Material replacement))
                {
                    Texture2D image = Resources.Load<Texture2D>("CityNew/" + texture);
                    if (image == null) continue;
                    Shader shader = Shader.Find("Standard");
                    if (shader == null) continue;
                    replacement = new Material(shader) { name = original.name + " (approximate texture)" };
                    replacement.mainTexture = image;
                    replacement.color = Color.white;
                    replacement.SetFloat("_Glossiness", 0.15f);
                    _cityMaterials.Add(original, replacement);
                }
                materials[i] = replacement;
                changed = true;
            }
            if (changed) renderer.sharedMaterials = materials;
        }

        private void OnDestroy()
        {
            foreach (Material material in _cityMaterials.Values)
                if (material != null) Destroy(material);
            _cityMaterials.Clear();
        }

        public Vector3 FindSpawnPoint()
        {
            for (int i = 0; i < _neighbors.Count; i++)
            {
                if (_neighbors[i].Count == 0) continue;
                return _lanes[i] + Vector3.up * 1.1f;
            }
            if (_hasRoadSpawn) return _roadSpawn + Vector3.up * 1.1f;
            Debug.LogError("[CityDrive] No validated road node for player spawn. Check road collider/import.");
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
