using UnityEngine;

namespace CityDrive
{
    /// <summary>
    /// Skid marks and tyre smoke for the player car. The smoke is no longer a
    /// home-made procedural effect: it is instantiated from the original RCC Pro
    /// v1.44 WheelSlipAsphalt / WheelSlipBurnout particle prefabs. Emission is
    /// driven only by real, grounded wheel slip published by ArcadeCarController,
    /// matching RCCP's "isSkidding" rule (slip >= ground slip threshold).
    /// </summary>
    public sealed class VehicleEffects : MonoBehaviour
    {
        public ArcadeCarController Car;

        [Header("Skid marks")]
        [SerializeField] private int maxMarks = 220;
        [SerializeField] private float markLife = 22f;
        [SerializeField] private float markSpacing = 0.55f;
        [SerializeField] private float markWidth = 0.20f;

        [Header("RCC Pro slip smoke")]
        [Tooltip("Slip threshold mapped to RCCP's asphalt ground-material slip value.")]
        [SerializeField] private float rccpSlipThreshold = 0.15f;
        [SerializeField] private float burnoutSlipThreshold = 0.12f;
        [SerializeField] private float smokeEmissionMultiplier = 1.35f;

        private sealed class Mark
        {
            public GameObject Object;
            public MeshRenderer Renderer;
            public MaterialPropertyBlock Block;
            public float Birth;
            public float Alpha;
            public bool Active;
        }

        /// <summary>One asphalt and one burnout RCCP particle instance per wheel.</summary>
        private sealed class WheelSmoke
        {
            public ParticleSystem[] SlipSystems;
            public ParticleSystem[] BurnoutSystems;
            public float[] SlipRates;
            public float[] BurnoutRates;

            public void Configure(GameObject asphalt, GameObject burnout)
            {
                SlipSystems = Prepare(asphalt);
                BurnoutSystems = Prepare(burnout);
                SlipRates = Rates(SlipSystems);
                BurnoutRates = Rates(BurnoutSystems);
            }

            private static ParticleSystem[] Prepare(GameObject prefab)
            {
                if (prefab == null) return new ParticleSystem[0];
                GameObject instance = UnityEngine.Object.Instantiate(prefab);
                instance.name = prefab.name + " (RCCP)";
                instance.transform.SetParent(null, true);

                ParticleSystem[] systems = instance.GetComponentsInChildren<ParticleSystem>(true);
                foreach (ParticleSystem ps in systems)
                {
                    // RCCP prefabs are saved as short, non-looping bursts. Looping on
                    // the runtime instance lets emission be toggled by real slip;
                    // disabling emission still lets existing particles fade out.
                    ParticleSystem.MainModule main = ps.main;
                    main.loop = true;
                    main.simulationSpace = ParticleSystemSimulationSpace.World;
                    ParticleSystem.EmissionModule emission = ps.emission;
                    emission.enabled = false;

                    ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
                    if (renderer != null)
                    {
                        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        renderer.receiveShadows = false;
                        renderer.sortingFudge = -10f;
                    }
                    ps.Play();
                }
                return systems;
            }

            private static float[] Rates(ParticleSystem[] systems)
            {
                float[] rates = new float[systems.Length];
                for (int i = 0; i < systems.Length; i++)
                    rates[i] = Mathf.Max(1f, systems[i].emission.rateOverTime.scalar);
                return rates;
            }

            public void Apply(bool grounded, bool burnout, float intensity, float multiplier, Vector3 point)
            {
                if (SlipSystems == null) return;
                Set(SlipSystems, SlipRates, grounded && !burnout && intensity > 0.02f,
                    intensity * multiplier, point);
                Set(BurnoutSystems, BurnoutRates, grounded && burnout && intensity > 0.02f,
                    intensity * multiplier, point);
            }

            private static void Set(ParticleSystem[] systems, float[] rates, bool emit, float intensity, Vector3 point)
            {
                if (systems == null || rates == null) return;
                for (int i = 0; i < systems.Length; i++)
                {
                    ParticleSystem ps = systems[i];
                    if (ps == null) continue;
                    if (emit)
                        ps.transform.position = point;
                    ParticleSystem.EmissionModule emission = ps.emission;
                    emission.enabled = emit;
                    if (emit)
                        emission.rateOverTime = rates[i] * Mathf.Clamp01(intensity);
                }
            }

            public void Clear()
            {
                Clear(SlipSystems);
                Clear(BurnoutSystems);
            }

            private static void Clear(ParticleSystem[] systems)
            {
                if (systems == null) return;
                foreach (ParticleSystem ps in systems)
                {
                    if (ps == null) continue;
                    ParticleSystem.EmissionModule emission = ps.emission;
                    emission.enabled = false;
                    ps.Clear();
                }
            }
        }

        private readonly Mark[] _marks = new Mark[220];
        private readonly WheelSmoke[] _smoke = new WheelSmoke[4];
        private readonly float[] _smokeIntensity = new float[4];
        private readonly Vector3[] _lastMarkPoint = new Vector3[4];
        private readonly float[] _lastMarkTime = new float[4];
        private readonly bool[] _hasLastPoint = new bool[4];
        private int _cursor;
        private Mesh _quad;
        private Material _markMaterial;

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

        private void BuildSmoke()
        {
            RCCPSmokeAssets assets = RCCPSmokeAssets.Instance;
            if (assets == null || assets.WheelSlipAsphalt == null || assets.WheelSlipBurnout == null)
            {
                Debug.LogWarning("[CityDrive] RCCP smoke prefabs are missing from Resources/RCCP/RCCP_SmokeAssets.asset.");
                return;
            }

            for (int i = 0; i < 4; i++)
            {
                WheelSmoke smoke = new WheelSmoke();
                smoke.Configure(assets.WheelSlipAsphalt, assets.WheelSlipBurnout);
                _smoke[i] = smoke;
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < 4; i++)
            {
                if (_smoke[i] != null) _smoke[i].Clear();
                _smokeIntensity[i] = 0f;
                _hasLastPoint[i] = false;
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
                float targetSlip = 0f;
                bool burnout = false;

                if (wheel.Grounded)
                {
                    // Same decision RCCP makes: grounded wheel, real forward and
                    // sideways slip above the surface threshold. No button alone
                    // can start emission.
                    float slip = wheel.TotalSlip;
                    if (slip >= rccpSlipThreshold)
                        targetSlip = Mathf.Clamp01((slip - rccpSlipThreshold) / 0.5f);
                    if (wheel.BurnoutSlip >= burnoutSlipThreshold)
                    {
                        burnout = true;
                        targetSlip = Mathf.Max(targetSlip, Mathf.Clamp01(wheel.BurnoutSlip / 0.5f));
                    }
                }

                float follow = !wheel.Grounded ? 10f : targetSlip > _smokeIntensity[i] ? 5f : 1.6f;
                _smokeIntensity[i] = Mathf.MoveTowards(_smokeIntensity[i], targetSlip, dt * follow);
                float intensity = wheel.Grounded ? _smokeIntensity[i] : 0f;

                WheelSmoke smoke = _smoke[i];
                if (smoke != null)
                {
                    Vector3 point = wheel.Grounded
                        ? wheel.ContactPoint + wheel.ContactNormal * 0.08f
                        : Car.transform.position;
                    smoke.Apply(wheel.Grounded, burnout, intensity, smokeEmissionMultiplier, point);
                }

                if (!wheel.Grounded || intensity <= 0.05f) continue;

                bool moved = _hasLastPoint[i] &&
                             Vector3.Distance(wheel.ContactPoint, _lastMarkPoint[i]) >= markSpacing;
                bool burnoutStationary = burnout && Time.time - _lastMarkTime[i] > 0.12f;
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
