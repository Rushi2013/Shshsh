using UnityEngine;

namespace CityDrive
{
    /// <summary>
    /// Realistic tyre smoke + skid marks, driven strictly by the controller's real
    /// per-wheel contact and slip data. No button triggers an effect by itself.
    /// Old blocky/radial smoke was removed: soft noise-textured particles with
    /// depth fade, slow rise, expansion and gentle drift now come from each wheel.
    /// </summary>
    public sealed class VehicleEffects : MonoBehaviour
    {
        public ArcadeCarController Car;

        [Header("Skid marks")]
        [SerializeField] private int maxMarks = 220;
        [SerializeField] private float markLife = 22f;
        [SerializeField] private float markSpacing = 0.55f;
        [SerializeField] private float markWidth = 0.20f;
        [SerializeField] private float slipThreshold = 0.15f;

        [Header("Realistic smoke")]
        [SerializeField] private int smokePerWheel = 120;
        [SerializeField] private float smokeLifetime = 3.4f;
        [SerializeField] private float smokeStartSizeMin = 0.28f;
        [SerializeField] private float smokeStartSizeMax = 0.68f;
        [SerializeField] private float emissionMax = 55f;

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
        private readonly float[] _smokeIntensity = new float[4];
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
            _quad.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            _quad.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            _quad.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            _quad.RecalculateBounds();

            Shader markShader = Shader.Find("Unlit/Transparent");
            if (markShader == null) markShader = Shader.Find("Sprites/Default");
            if (markShader != null)
            {
                _markMaterial = new Material(markShader) { name = "Dark Skid Mark" };
                _markMaterial.renderQueue = 2990;
            }

            Shader smokeShader = Shader.Find("CityDrive/Tire Smoke");
            if (smokeShader == null || !smokeShader.isSupported)
                smokeShader = Shader.Find("Particles/Standard Unlit");
            if (smokeShader != null)
            {
                _smokeMaterial = new Material(smokeShader) { name = "Realistic Tyre Smoke" };
                _smokeMaterial.mainTexture = CreateSoftSmokeTexture();
                _smokeMaterial.SetColor("_Color", new Color(0.94f, 0.95f, 0.97f, 0.42f));
                _smokeMaterial.SetFloat("_Softness", 1.2f);
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
                _marks[i] = new Mark { Object = go, Renderer = renderer, Block = new MaterialPropertyBlock() };
            }
        }

        /// <summary>Soft noisy alpha texture: no square edges, no cartoon cloud discs.</summary>
        private static Texture2D CreateSoftSmokeTexture()
        {
            const int size = 256;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Soft Tyre Smoke",
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
                    float radial = 1f - Mathf.SmoothStep(0.18f, 1f, d);
                    float broad = Mathf.PerlinNoise(x * 0.024f + 13.7f, y * 0.024f + 4.2f);
                    float detail = Mathf.PerlinNoise(x * 0.075f + 2.3f, y * 0.075f + 19.1f);
                    float noise = Mathf.Clamp01(broad * 0.68f + detail * 0.32f);
                    float alpha = radial * Mathf.Lerp(0.35f, 1f, noise) * Mathf.Pow(radial, 0.45f);
                    pixels[y * size + x] = new Color(0.95f, 0.96f, 0.98f, alpha * 0.88f);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void BuildSmoke()
        {
            AnimationCurve grow = new AnimationCurve(
                new Keyframe(0f, 0.65f), new Keyframe(0.45f, 1.25f), new Keyframe(1f, 1.95f));
            Gradient fade = new Gradient();
            fade.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.80f, 0.82f, 0.85f), 0f),
                    new GradientColorKey(new Color(0.56f, 0.58f, 0.62f), 1f)
                },
                new[] { new GradientAlphaKey(0.40f, 0f), new GradientAlphaKey(0.16f, 0.45f), new GradientAlphaKey(0f, 1f) });

            for (int i = 0; i < 4; i++)
            {
                GameObject go = new GameObject("Tyre Smoke " + i);
                go.transform.SetParent(transform, false);
                ParticleSystem ps = go.AddComponent<ParticleSystem>();

                ParticleSystem.MainModule main = ps.main;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.duration = 3f;
                main.loop = true;
                main.playOnAwake = true;
                main.startLifetime = new ParticleSystem.MinMaxCurve(smokeLifetime * 0.7f, smokeLifetime * 1.25f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.0f);
                main.startSize = new ParticleSystem.MinMaxCurve(smokeStartSizeMin, smokeStartSizeMax);
                main.startColor = new Color(0.88f, 0.89f, 0.91f, 0.30f);
                main.gravityModifier = -0.12f;
                main.maxParticles = smokePerWheel;

                ParticleSystem.EmissionModule emission = ps.emission;
                emission.rateOverTime = 0f;

                ParticleSystem.ShapeModule shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 18f;
                shape.radius = 0.14f;

                ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
                velocity.enabled = true;
                velocity.space = ParticleSystemSimulationSpace.Local;
                velocity.x = new ParticleSystem.MinMaxCurve(-0.18f, 0.18f);
                velocity.y = new ParticleSystem.MinMaxCurve(0.35f, 0.85f);
                velocity.z = new ParticleSystem.MinMaxCurve(-0.85f, -0.25f); // trails behind the car

                ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, grow);

                ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
                color.enabled = true;
                color.color = fade;

                ParticleSystem.NoiseModule noise = ps.noise;
                noise.enabled = true;
                noise.strength = 0.34f;
                noise.frequency = 0.18f;
                noise.scrollSpeed = 0.12f;
                noise.damping = true;
                noise.quality = ParticleSystemNoiseQuality.Low;

                ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.sharedMaterial = _smokeMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sortingFudge = -10f;
                ps.Play();
                _smoke[i] = ps;
            }
        }

        private void Update()
        {
            if (Car == null || Car.Wheels == null) return;
            WheelTelemetry[] wheels = Car.Wheels;
            float dt = Mathf.Min(0.1f, Time.unscaledDeltaTime);

            for (int i = 0; i < 4; i++)
            {
                WheelTelemetry wheel = wheels[i];
                float raw = 0f;
                if (wheel.Grounded)
                {
                    // Physics only: slip ratio, lateral slip and real wheelspin.
                    raw = Mathf.Clamp01((wheel.TotalSlip - slipThreshold) / 0.52f);
                    if (wheel.BurnoutSlip > 0.05f)
                        raw = Mathf.Max(raw, Mathf.Clamp01(wheel.BurnoutSlip / 0.48f));
                }

                float follow = raw > _smokeIntensity[i] ? 5f : 1.6f;
                _smokeIntensity[i] = Mathf.MoveTowards(_smokeIntensity[i], raw, dt * follow);
                float intensity = _smokeIntensity[i];

                ParticleSystem ps = _smoke[i];
                if (ps != null)
                {
                    if (wheel.Grounded)
                        ps.transform.SetPositionAndRotation(
                            wheel.ContactPoint + wheel.ContactNormal * 0.09f, Car.transform.rotation);
                    ParticleSystem.EmissionModule emission = ps.emission;
                    emission.rateOverTime = intensity > 0.02f ? intensity * emissionMax : 0f;
                    ParticleSystem.MainModule main = ps.main;
                    main.startColor = new Color(0.88f, 0.89f, 0.91f, Mathf.Lerp(0.14f, 0.42f, intensity));
                }

                if (!wheel.Grounded || intensity <= 0.05f) continue;

                bool moved = _hasLastPoint[i] &&
                             Vector3.Distance(wheel.ContactPoint, _lastMarkPoint[i]) >= markSpacing;
                bool burnoutStationary = wheel.BurnoutSlip > 0.12f && Time.time - _lastMarkTime[i] > 0.12f;
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
                mark.Block.SetColor("_Color", new Color(0.028f, 0.028f, 0.032f, alpha));
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
            if (index < 0) index = _cursor;
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
            mark.Alpha = burnout ? 0.92f : Mathf.Lerp(0.16f, 0.62f, intensity);
            mark.Birth = Time.time;
            mark.Active = true;
            mark.Block.SetColor("_Color", new Color(0.028f, 0.028f, 0.032f, mark.Alpha));
            mark.Renderer.SetPropertyBlock(mark.Block);
            mark.Object.SetActive(true);

            _lastMarkPoint[wheelIndex] = wheel.ContactPoint;
            _lastMarkTime[wheelIndex] = Time.time;
            _hasLastPoint[wheelIndex] = true;
        }
    }
}
