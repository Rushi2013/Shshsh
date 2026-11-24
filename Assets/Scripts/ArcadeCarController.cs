using UnityEngine;

namespace CityDrive
{
    /// <summary>
    /// Arcade-sim car physics: four raycast springs, load-limited tyre friction,
    /// rear-wheel drive, four-wheel braking, speed-sensitive steering and a handbrake
    /// that genuinely removes rear grip so the car slides instead of sticking.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ArcadeCarController : MonoBehaviour
    {
        [Header("Inputs (-1..1 / 0..1), written by MobileControls")]
        public float ThrottleInput;
        public float BrakeInput;
        public float SteerInput;
        public float HandbrakeInput;

        [Header("Engine & brakes")]
        [SerializeField] private float engineForce = 19000f;
        [SerializeField] private float reverseForce = 9000f;
        [SerializeField] private float brakeForce = 26000f;
        [SerializeField] private float maxSpeedKph = 240f;
        [SerializeField] private float rollingResistance = 13f;

        [Header("Steering & grip")]
        [SerializeField] private float maxSteerAngle = 38f;
        [SerializeField] private float frontGrip = 19f;
        [SerializeField] private float rearGrip = 17f;
        [SerializeField] private float driftGrip = 3.2f;
        [SerializeField] private float downforce = 2.4f;

        [Header("Suspension")]
        [SerializeField] private float springStrength = 42000f;
        [SerializeField] private float springDamper = 5200f;
        [SerializeField] private float restLength = 0.42f;
        [SerializeField] private float wheelRadius = 0.34f;

        private struct Wheel
        {
            public Vector3 LocalPosition;
            public bool Front;
            public bool Driven;
        }

        private static readonly Wheel[] Wheels =
        {
            new Wheel { LocalPosition = new Vector3(-0.75f, 0.10f,  1.42f), Front = true,  Driven = false },
            new Wheel { LocalPosition = new Vector3( 0.75f, 0.10f,  1.42f), Front = true,  Driven = false },
            new Wheel { LocalPosition = new Vector3(-0.75f, 0.10f, -1.25f), Front = false, Driven = true  },
            new Wheel { LocalPosition = new Vector3( 0.75f, 0.10f, -1.25f), Front = false, Driven = true  }
        };

        private Rigidbody _body;
        private readonly RaycastHit[] _hits = new RaycastHit[24];
        private VehicleVisuals _visuals;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private bool _spawnConfigured;
        private float _steerAngle;
        private float _wheelSpinDegrees;
        private float _upsideDownTimer;
        private int _groundedWheels;

        public float SpeedKph => _body == null ? 0f : _body.velocity.magnitude * 3.6f;
        public int GroundedWheels => _groundedWheels;
        public float SteerAngle => _steerAngle;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.mass = 1250f;
            _body.centerOfMass = new Vector3(0f, -0.45f, 0.05f);
            _body.drag = 0.08f;
            _body.angularDrag = 0.9f;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _body.maxAngularVelocity = 3.2f;

            BoxCollider collider = GetComponent<BoxCollider>();
            if (collider == null) collider = gameObject.AddComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = new Vector3(1.9f, 1.4f, 4.3f);

            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
        }

        private void Start()
        {
            if (!_spawnConfigured)
            {
                _spawnPosition = transform.position;
                _spawnRotation = transform.rotation;
            }
        }

        public void SetVisuals(VehicleVisuals visuals)
        {
            _visuals = visuals;
        }

        public void ConfigureSpawn(Vector3 position, Quaternion rotation)
        {
            _spawnPosition = position;
            _spawnRotation = rotation;
            _spawnConfigured = true;
        }

        public void ResetCar()
        {
            if (_body == null) return;
            _body.isKinematic = false;
            _body.position = _spawnPosition + Vector3.up * 1.1f;
            _body.rotation = _spawnRotation;
            _body.velocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            _steerAngle = 0f;
            _upsideDownTimer = 0f;
            transform.SetPositionAndRotation(_body.position, _body.rotation);
        }

        private void Update()
        {
            if (_visuals != null) _visuals.UpdateWheels(_steerAngle, _wheelSpinDegrees);
        }

        private void FixedUpdate()
        {
            if (_body == null || _body.isKinematic) return;

            float speed = _body.velocity.magnitude;
            float forwardSpeed = Vector3.Dot(_body.velocity, transform.forward);
            bool reversing = BrakeInput > 0.15f && forwardSpeed < 1.5f;

            float speedFactor = Mathf.Clamp01(speed / 45f);
            float targetSteer = Mathf.Clamp(SteerInput, -1f, 1f) * maxSteerAngle *
                                Mathf.Lerp(1f, 0.42f, speedFactor);
            _steerAngle = Mathf.MoveTowards(_steerAngle, targetSteer, 260f * Time.fixedDeltaTime);

            _groundedWheels = 0;
            float rayLength = restLength + wheelRadius + 0.12f;
            float spinSpeed = 0f;
            int spinSamples = 0;

            foreach (Wheel wheel in Wheels)
            {
                Vector3 origin = transform.TransformPoint(wheel.LocalPosition);
                int count = Physics.RaycastNonAlloc(origin, Vector3.down, _hits, rayLength,
                    ~0, QueryTriggerInteraction.Ignore);

                RaycastHit best = default;
                bool hasHit = false;
                float closest = float.MaxValue;
                for (int i = 0; i < count; i++)
                {
                    Collider other = _hits[i].collider;
                    if (other == null || other.transform.root == transform.root) continue;
                    if (_hits[i].normal.y < 0.45f) continue;
                    if (_hits[i].distance < closest)
                    {
                        closest = _hits[i].distance;
                        best = _hits[i];
                        hasHit = true;
                    }
                }
                if (!hasHit) continue;

                _groundedWheels++;
                Vector3 point = best.point;
                Vector3 normal = best.normal;
                Vector3 wheelVelocity = _body.GetPointVelocity(point);

                // Spring + damper, applied at the contact patch.
                float compression = rayLength - best.distance;
                float suspension = compression * springStrength -
                                    Vector3.Dot(wheelVelocity, Vector3.up) * springDamper;
                suspension = Mathf.Clamp(suspension, 0f, springStrength * 0.55f);
                _body.AddForceAtPosition(Vector3.up * suspension, point);

                // Steered rolling direction projected onto the road surface.
                Vector3 forward = wheel.Front
                    ? Quaternion.AngleAxis(_steerAngle, Vector3.up) * transform.forward
                    : transform.forward;
                forward = Vector3.ProjectOnPlane(forward, normal).normalized;
                Vector3 right = Vector3.Cross(normal, forward).normalized;

                float longitudinalSpeed = Vector3.Dot(wheelVelocity, forward);
                float lateralSpeed = Vector3.Dot(wheelVelocity, right);

                // Longitudinal: engine on rear wheels, brakes on all four.
                float drive = 0f;
                if (wheel.Driven && !reversing && ThrottleInput > 0f &&
                    forwardSpeed < maxSpeedKph / 3.6f)
                {
                    float torqueCurve = Mathf.Lerp(1f, 0.28f, Mathf.Clamp01(speed / 60f));
                    drive = engineForce * ThrottleInput * torqueCurve;
                }
                if (wheel.Driven && reversing) drive = -reverseForce * BrakeInput;

                if (BrakeInput > 0f && !reversing)
                {
                    float stopping = Mathf.Min(brakeForce * BrakeInput,
                        Mathf.Abs(longitudinalSpeed) * _body.mass / Time.fixedDeltaTime);
                    drive -= Mathf.Sign(longitudinalSpeed) * stopping;
                }
                if (HandbrakeInput > 0.5f && wheel.Driven) drive *= 0.05f;

                _body.AddForceAtPosition(forward * drive, point);

                // Lateral grip: kill sideways velocity, limited by the load on this wheel.
                bool drifting = HandbrakeInput > 0.5f && wheel.Driven;
                float gripCoefficient = wheel.Front ? frontGrip : (drifting ? driftGrip : rearGrip);
                float lateral = -lateralSpeed * gripCoefficient * _body.mass / 4f;
                float gripLimit = Mathf.Max(850f, suspension * (drifting ? 0.65f : 1.9f));
                lateral = Mathf.Clamp(lateral, -gripLimit, gripLimit);
                _body.AddForceAtPosition(right * lateral, point);

                // Tyre + rolling resistance so releasing gas slows the car naturally.
                _body.AddForceAtPosition(-forward * longitudinalSpeed * rollingResistance, point);

                spinSpeed += longitudinalSpeed;
                spinSamples++;
            }

            if (spinSamples > 0)
                _wheelSpinDegrees += spinSpeed / spinSamples / wheelRadius *
                                     Mathf.Rad2Deg * Time.fixedDeltaTime;

            if (_groundedWheels > 0)
            {
                _body.AddForce(-transform.up * downforce * speed * speed);

                // A little yaw assist makes turn-in crisp without spinning the car.
                if (Mathf.Abs(SteerInput) > 0.05f && speed > 4f)
                {
                    float assist = Mathf.Clamp(speed, 4f, 28f) * SteerInput * 95f;
                    if (HandbrakeInput > 0.5f) assist *= 1.8f;
                    _body.AddTorque(Vector3.up * assist, ForceMode.Force);
                }

                // Keep the chassis from tipping unnecessarily.
                float upright = Vector3.Dot(transform.up, Vector3.up);
                if (upright < 0.97f)
                {
                    Vector3 correction = Vector3.Cross(transform.up, Vector3.up);
                    _body.AddTorque(correction * _body.mass * 4.5f, ForceMode.Force);
                }
            }

            bool flipped = Vector3.Dot(transform.up, Vector3.up) < 0.1f && speed < 4f;
            _upsideDownTimer = flipped ? _upsideDownTimer + Time.fixedDeltaTime : 0f;
            if (transform.position.y < -30f || _upsideDownTimer > 2.5f) ResetCar();
        }
    }
}
