using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityDrive
{
    /// <summary>
    /// Built-in-pipeline 3D cloud layer. Real combined cloud meshes at three heights
    /// with slow independent drift, soft noise shader and no shadows/tessellation.
    /// This is deliberately separate from the skybox: a skybox is only a panorama,
    /// while these groups give parallax and depth as the car moves.
    /// </summary>
    public sealed class CloudSky : MonoBehaviour
    {
        [Serializable]
        public sealed class LayerSettings
        {
            public string Name = "Clouds";
            public int Groups = 6;
            [Tooltip("Height above the followed vehicle, in world units")]
            public float Height = 160f;
            [Tooltip("World units per second")]
            public float Speed = 0.4f;
            public float Radius = 360f;
            public float Scale = 70f;
            public int Puffs = 7;
        }

        [Header("Target")]
        public Transform Target;

        [Header("Material")]
        [SerializeField] private Material cloudMaterial;
        [SerializeField] private int seed = 20261125;

        [Header("Layers — values from the cloud guide")]
        [SerializeField] private LayerSettings[] layers =
        {
            new LayerSettings { Name = "High Distant", Groups = 9, Height = 245f, Speed = 0.28f, Radius = 470f, Scale = 95f, Puffs = 8 },
            new LayerSettings { Name = "Main Mid", Groups = 7, Height = 155f, Speed = 0.65f, Radius = 380f, Scale = 78f, Puffs = 7 },
            new LayerSettings { Name = "Sparse Near", Groups = 4, Height = 105f, Speed = 1.05f, Radius = 300f, Scale = 62f, Puffs = 6 }
        };

        private sealed class CloudInstance
        {
            public Transform Transform;
            public float Speed;
            public float Height;
            public float Wrap;
        }

        private readonly List<CloudInstance> _clouds = new List<CloudInstance>();
        private readonly List<Mesh> _generatedMeshes = new List<Mesh>();
        private System.Random _random;
        private Mesh _puffMesh;
        private Material _runtimeMaterial;

        public void SetTarget(Transform target)
        {
            Target = target;
        }

        private void Awake()
        {
            _random = new System.Random(seed);
            if (cloudMaterial == null)
            {
                Shader shader = Shader.Find("CityDrive/Soft Cloud");
                if (shader == null)
                {
                    Debug.LogWarning("[CityDrive] Soft Cloud shader missing; 3D cloud layer disabled.");
                    enabled = false;
                    return;
                }
                _runtimeMaterial = new Material(shader) { name = "Runtime Soft Cloud" };
                cloudMaterial = _runtimeMaterial;
            }

            if (cloudMaterial.shader == null || !cloudMaterial.shader.isSupported ||
                cloudMaterial.shader.name.Contains("URP") || cloudMaterial.shader.name.Contains("HDRP"))
            {
                Debug.LogWarning("[CityDrive] Cloud material is not Built-in compatible; layer disabled.");
                enabled = false;
                return;
            }

            Build();
        }

        private void Build()
        {
            _puffMesh = CreatePuffMesh(10, 6);
            _generatedMeshes.Add(_puffMesh);

            if (layers == null || layers.Length == 0) return;

            foreach (LayerSettings layer in layers)
            {
                if (layer == null || layer.Groups <= 0) continue;
                for (int i = 0; i < layer.Groups; i++)
                {
                    GameObject group = new GameObject(layer.Name + " Cloud " + i);
                    group.transform.SetParent(transform, false);

                    float angle = Range(0f, Mathf.PI * 2f);
                    float distance = Range(layer.Radius * 0.25f, layer.Radius);
                    group.transform.position = new Vector3(
                        Mathf.Cos(angle) * distance,
                        layer.Height,
                        Mathf.Sin(angle) * distance);

                    Mesh combined = BuildCloudMesh(layer, group.name);
                    MeshFilter filter = group.AddComponent<MeshFilter>();
                    filter.sharedMesh = combined;
                    MeshRenderer renderer = group.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = cloudMaterial;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    renderer.lightProbeUsage = LightProbeUsage.Off;
                    renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

                    _clouds.Add(new CloudInstance
                    {
                        Transform = group.transform,
                        Speed = layer.Speed,
                        Height = layer.Height,
                        Wrap = layer.Radius * 1.45f
                    });
                }
            }
        }

        private Mesh BuildCloudMesh(LayerSettings layer, string name)
        {
            int puffs = Mathf.Max(2, layer.Puffs);
            CombineInstance[] parts = new CombineInstance[puffs];
            for (int i = 0; i < puffs; i++)
            {
                float radius = layer.Scale * Range(0.26f, 0.52f);
                Vector3 offset = new Vector3(
                    Range(-0.62f, 0.62f) * layer.Scale,
                    Range(-0.16f, 0.16f) * layer.Scale,
                    Range(-0.48f, 0.48f) * layer.Scale);
                Vector3 scale = new Vector3(
                    radius * Range(0.95f, 1.45f),
                    radius * Range(0.42f, 0.78f),
                    radius * Range(0.80f, 1.25f));
                Quaternion rotation = Quaternion.Euler(
                    Range(-12f, 12f), Range(0f, 360f), Range(-8f, 8f));
                parts[i].mesh = _puffMesh;
                parts[i].transform = Matrix4x4.TRS(offset, rotation, scale);
            }

            Mesh mesh = new Mesh { name = name + " Mesh" };
            mesh.CombineMeshes(parts, true, false);
            mesh.RecalculateBounds();
            _generatedMeshes.Add(mesh);
            return mesh;
        }

        private static Mesh CreatePuffMesh(int segments, int rings)
        {
            int columns = segments + 1;
            Vector3[] vertices = new Vector3[(rings + 1) * columns];
            Vector3[] normals = new Vector3[vertices.Length];
            Vector2[] uv = new Vector2[vertices.Length];

            for (int ring = 0; ring <= rings; ring++)
            {
                float v = ring / (float)rings;
                float phi = v * Mathf.PI;
                for (int segment = 0; segment <= segments; segment++)
                {
                    float u = segment / (float)segments;
                    float theta = u * Mathf.PI * 2f;
                    Vector3 normal = new Vector3(
                        Mathf.Sin(phi) * Mathf.Cos(theta),
                        Mathf.Cos(phi),
                        Mathf.Sin(phi) * Mathf.Sin(theta));
                    int index = ring * columns + segment;
                    vertices[index] = normal * 0.5f;
                    normals[index] = normal;
                    uv[index] = new Vector2(u, v);
                }
            }

            int[] triangles = new int[rings * segments * 6];
            int t = 0;
            for (int ring = 0; ring < rings; ring++)
            {
                for (int segment = 0; segment < segments; segment++)
                {
                    int a = ring * columns + segment;
                    int b = a + columns;
                    triangles[t++] = a; triangles[t++] = b; triangles[t++] = a + 1;
                    triangles[t++] = b; triangles[t++] = b + 1; triangles[t++] = a + 1;
                }
            }

            Mesh mesh = new Mesh { name = "Cloud Puff" };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private void LateUpdate()
        {
            if (Target == null || _clouds.Count == 0) return;
            Vector3 anchor = Target.position;
            float dt = Time.deltaTime;

            for (int i = 0; i < _clouds.Count; i++)
            {
                CloudInstance cloud = _clouds[i];
                Vector3 p = cloud.Transform.position;
                p.x += cloud.Speed * dt;
                p.y = anchor.y + cloud.Height;

                float relativeX = p.x - anchor.x;
                if (relativeX > cloud.Wrap) p.x -= cloud.Wrap * 2f;
                else if (relativeX < -cloud.Wrap) p.x += cloud.Wrap * 2f;

                cloud.Transform.position = p;
            }
        }

        private float Range(float min, float max)
        {
            return min + (float)_random.NextDouble() * (max - min);
        }

        private void OnDestroy()
        {
            foreach (Mesh mesh in _generatedMeshes)
            {
                if (mesh != null) Destroy(mesh);
            }
            if (_runtimeMaterial != null) Destroy(_runtimeMaterial);
        }
    }
}
