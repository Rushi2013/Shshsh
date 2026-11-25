using UnityEngine;

namespace CityDrive
{
    /// <summary>
    /// Physics-driven drift/burnout effects. Smoke emission and skid marks are
    /// produced only from the controller's real per-wheel slip and contact points.
    /// Marks and particles are pooled; no effect is triggered by a UI button alone.
    /// </summary>
    public sealed class VehicleEffects : MonoBehaviour
    {
        public ArcadeCarController Car;

        [Header("Skid marks")]
        [SerializeField] private int maxMarks = 220;
        [SerializeField] private float markLife = 22f;
        [SerializeField] private float markSpacing = 0.55f;
        [SerializeField] private float markWidth = 0.20f;
        [SerializeField] private float slipThreshold = 0.18f;

        [Header("Smoke")]
        [SerializeField] private int smokePerWheel = 22;
        [SerializeField] private float smokeStartSize = 0.28f;
        [SerializeField] private float smokeEndSize = 1.05f;
        [SerializeField] private float smokeLifetime = 1.25f;

        private sealed class Mark
        {
            public GameObject Object;
            public MeshRenderer Renderer;
            public MaterialPropertyBlock Block;
            public float Birth;
            public float Alpha;
            public bool Active;
        }

        private readonly Mark[] _marks = new Mark[220];
        private readonly ParticleSystem[] _smoke = new ParticleSystem[4];
        private readonly Vector3[] _lastMarkPoint = new Vector3[4];
        private readonly float[] _lastMarkTime = new float[4];
        private readonly bool[] _hasLastPoint = new bool[4];
        private int _cursor;
        private Mesh _quad;
        private Material _markMaterial;
        private Material _smokeMaterial;

        private void Start()
        {
            if (Car == null) Car = GetComponent<ArcadeCarController>();
            BuildResources();
            BuildSmoke();
        }

        private void BuildResources()
        {
            _quad = new Mesh { name = "Skid Mark Quad" };
            _quad.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, 0.5f), new Vector3(-0.5f, 0f, 0.5f)
            };
            _quad.normals = new[]
            {
                Vector3.up, Vector3.up, Vector3.up, Vector3.up
            };
            _quad.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 1f), new Vector2(0f, 1f)
            };
            _quad.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            _quad.RecalculateBounds();

            Shader markShader = Shader.Find("Unlit/Transparent");
            if (markShader == null) markShader = Shader.Find("Sprites/Default");
            if (markShader != null)
            {
                _markMaterial = new Material(markShader) { name = "Skid Mark" };
                _markMaterial.renderQueue = 3000;
            }

            Shader smokeShader = Shader.Find("Particles/Standard Unlit");
            if (smokeShader == null) smokeShader = Shader.Find("Particles/Alpha Blended");
            if (smokeShader != null)
            {
                _smokeMaterial = new Material(smokeShader) { name = "Tyre Smoke" };
                _smokeMaterial.mainTexture = CreateSmokeTexture();
                _smokeMaterial.renderQueue = 3010;
            }

            int count = Mathf.Min(maxMarks, _marks.Length);
            for (int i = 0; i < count; i++)
            {
                GameObject go = new GameObject("Skid Mark " + i);
                go.transform.SetParent(transform, false);
                go.SetActive(false);
                MeshFilter filter = go.AddComponent<MeshFilter>();
                filter.sharedMesh = _quad;
                MeshRenderer renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _markMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                _marks[i] = new Mark
                {
                    Object = go,
                    Renderer = renderer,
                    Block = new MaterialPropertyBlock()
                };
            }
        }

        private static Texture2D CreateSmokeTexture()
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Soft Smoke",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f;
                    float dy = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                    float alpha = Mathf.Pow(1f - d, 2.2f) * 0.65f;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void BuildSmoke()
        {
            for (int i = 0; i < 4; i++)
            {
                GameObject go = new GameObject("Tyre Smoke " + i);
                go.transform.SetParent(transform, false);
                ParticleSystem ps = go.AddComponent<ParticleSystem>();

                ParticleSystem.MainModule main = ps.main;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.startLifetime = smokeLifetime;
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.75f);
                main.startSize = new ParticleSystem.MinMaxCurve(smokeStartSize, smokeEndSize);
                main.startColor = new Color(0.78f, 0.80f, 0.83f, 0.4f);
                main.maxParticles = smokePerWheel;
                main.gravityModifier = -0.04f;
                main.loop = true;
                main.playOnAwake = true;

                ParticleSystem.EmissionModule emission = ps.emission;
                emission.rateOverTime = 0f;

                ParticleSystem.ShapeModule shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.16f;

                ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
                color.enabled = true;
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.55f, 0.57f, 0.60f), 1f) },
                    new[] { new GradientAlphaKey(0.35f, 0f), new GradientAlphaKey(0f, 1f) });
                color.color = gradient;

                ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
                if (_smokeMaterial != null) renderer.sharedMaterial = _smokeMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                ps.Play();
                _smoke[i] = ps;
            }
        }

        private void Update()
        {
            if (Car == null || Car.Wheels == null) return;
            WheelTelemetry[] wheels = Car.Wheels;

            for (int i = 0; i < 4; i++)
            {
                WheelTelemetry wheel = wheels[i];
                float intensity = wheel.Grounded ? Mathf.Clamp01((wheel.TotalSlip - slipThreshold) / 0.55f) : 0f;
                ParticleSystem ps = _smoke[i];
                if (ps != null)
                {
                    ps.transform.position = wheel.Grounded
                        ? wheel.ContactPoint + wheel.ContactNormal * 0.12f
                        : Car.transform.TransformPoint(Car.Wheels[i].ContactPoint);
                    ParticleSystem.EmissionModule emission = ps.emission;
                    emission.rateOverTime = intensity * 20f;
                    ParticleSystem.MainModule main = ps.main;
                    main.startColor = new Color(0.76f, 0.78f, 0.81f,
                        Mathf.Lerp(0.18f, 0.58f, intensity));
                }

                if (!wheel.Grounded || intensity <= 0.05f) continue;

                bool moved = _hasLastPoint[i] &&
                             Vector3.Distance(wheel.ContactPoint, _lastMarkPoint[i]) >= markSpacing;
                bool burnoutStationary = wheel.BurnoutSlip > 0.12f &&
                                         Time.time - _lastMarkTime[i] > 0.12f;
                if (moved || !_hasLastPoint[i] || burnoutStationary)
                    SpawnMark(i, wheel, intensity, burnoutStationary);
            }

            for (int i = 0; i < _marks.Length; i++)
            {
                Mark mark = _marks[i];
                if (mark == null || !mark.Active) continue;
                float age = Time.time - mark.Birth;
                if (age >= markLife)
                {
                    mark.Active = false;
                    mark.Object.SetActive(false);
                    continue;
                }
                float alpha = mark.Alpha * (1f - age / markLife);
                mark.Block.SetColor("_Color", new Color(0.035f, 0.035f, 0.04f, alpha));
                mark.Renderer.SetPropertyBlock(mark.Block);
            }
        }

        private void SpawnMark(int wheelIndex, WheelTelemetry wheel, float intensity, bool burnout)
        {
            int index = -1;
            for (int attempt = 0; attempt < _marks.Length; attempt++)
            {
                int candidate = (_cursor + attempt) % _marks.Length;
                if (_marks[candidate] != null && !_marks[candidate].Active)
                {
                    index = candidate;
                    break;
                }
            }
            if (index < 0) index = _cursor; // recycle the oldest slot when saturated
            _cursor = (index + 1) % _marks.Length;
            Mark mark = _marks[index];
            if (mark == null) return;

            Vector3 direction = Vector3.ProjectOnPlane(Car.transform.forward, wheel.ContactNormal);
            if (direction.sqrMagnitude < 0.001f) direction = Vector3.forward;
            direction.Normalize();
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, wheel.ContactNormal) *
                                  Quaternion.Euler(0f, yaw, 0f);
            float length = burnout ? 0.85f : Mathf.Max(0.5f, markSpacing * 1.4f);
            mark.Object.transform.SetPositionAndRotation(
                wheel.ContactPoint + wheel.ContactNormal * 0.018f, rotation);
            mark.Object.transform.localScale = new Vector3(markWidth, 1f, length);
            mark.Alpha = burnout ? 0.92f : Mathf.Lerp(0.18f, 0.62f, intensity);
            mark.Birth = Time.time;
            mark.Active = true;
            mark.Block.SetColor("_Color", new Color(0.035f, 0.035f, 0.04f, mark.Alpha));
            mark.Renderer.SetPropertyBlock(mark.Block);
            mark.Object.SetActive(true);

            _lastMarkPoint[wheelIndex] = wheel.ContactPoint;
            _lastMarkTime[wheelIndex] = Time.time;
            _hasLastPoint[wheelIndex] = true;
        }
    }
}
