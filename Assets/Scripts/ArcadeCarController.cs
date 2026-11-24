using UnityEngine;

namespace CityDrive
{
    /// <summary>
    /// Raycast-suspension vehicle physics: four independent springs, tyre slip with a
    /// friction circle, rear wheel drive, speed-sensitive steering and a handbrake
    /// that lowers rear grip so the car drifts. Inputs are driven by MobileControls.
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
        [SerializeField] private float engineForce = 11500f;
        [SerializeField] private float reverseForce = 5200f;
        [SerializeField] private float brakeForce = 15000f;
        [SerializeField] private float maxSpeedKph = 235f;
        [SerializeField] private float rollingResistance = 9f;

        [Header("Steering & grip")]
        [SerializeField] private float maxSteerAngle = 33f;
        [SerializeField] private float grip = 13f;
        [SerializeField] private float driftGrip = 3.8f;
        [SerializeField] private float downforce = 1.6f;

        [Header("Suspension")]
        [SerializeField] private float springStrength = 31000f;
        [SerializeField] private float springDamper = 4100f;
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
        private float _steerAngle;
        private float _wheelSpinDegrees;
        private float _upsideDownTimer;
        private int _groundedWheels;

        public float SpeedKph => _body == null ? 0f : _body.velocity.magnitude * 3.6f;
        public int GroundedWheels => _groundedWheels;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            _body.mass = 1250f;
            _body.centerOfMass = new Vector3(0f, -0.35f, 0.05f);
            _body.drag = 0.02f;
            _body.angularDrag = 0.55f;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _body.maxAngularVelocity = 3.5f;

            BoxCollider collider = GetComponent<BoxCollider>();
            if (collider == null) collider = gameObject.AddComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = new Vector3(1.9f, 1.4f, 4.3f);

            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
        }

        public void SetVisuals(VehicleVisuals visuals)
        {
            _visuals = visuals;
        }

        public void ResetCar()
        {
            _body.position = _spawnPosition + Vector3.up * 1.2f;
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
            if (_body == null) return;

            float speed = _body.velocity.magnitude;
            float forwardSpeed = Vector3.Dot(_body.velocity, transform.forward);
            bool reversing = BrakeInput > 0.15f && forwardSpeed < 1.2f;

            float speedFactor = Mathf.Clamp01(speed / 45f);
            float targetSteer = Mathf.Clamp(SteerInput, -1f, 1f) * maxSteerAngle * Mathf.Lerp(1f, 0.42f, speedFactor);
            _steerAngle = Mathf.MoveTowards(_steerAngle, targetSteer, 190f * Time.fixedDeltaTime);

            _groundedWheels = 0;
            float rayLength = restLength + wheelRadius + 0.12f;

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
                    if (_hits[i].normal.y < 0.5f) continue;
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
                Vector3 wheelVelocity = _body.GetPointVelocity(point);

                // Spring + damper.
                float compression = rayLength - best.distance;
                float suspension = compression * springStrength -
                                   Vector3.Dot(wheelVelocity, Vector3.up) * springDamper;
                suspension = Mathf.Clamp(suspension, 0f, springStrength * 0.5f);
                _body.AddForceAtPosition(Vector3.up * suspension, point);

                Vector3 forward = wheel.Front
                    ? Quaternion.AngleAxis(_steerAngle, Vector3.up) * transform.forward
                    : transform.forward;
                forward = Vector3.ProjectOnPlane(forward, best.normal).normalized;
                Vector3 right = Vector3.Cross(best.normal, forward).normalized;

                float longitudinalSpeed = Vector3.Dot(wheelVelocity, forward);
                float lateralSpeed = Vector3.Dot(wheelVelocity, right);

                // Engine / reverse / braking (rear wheel drive).
                if (wheel.Driven)
                {
                    float drive = 0f;
                    if (reversing)
                    {
                        drive = -reverseForce * BrakeInput;
                    }
                    else if (ThrottleInput > 0f && forwardSpeed < maxSpeedKph / 3.6f)
                    {
                        drive = engineForce * ThrottleInput;
                    }

                    if (BrakeInput > 0f && !reversing)
                    {
                        float stopping = Mathf.Min(brakeForce * BrakeInput,
                            Mathf.Abs(longitudinalSpeed) * _body.mass / Time.fixedDeltaTime);
                        drive -= Mathf.Sign(longitudinalSpeed) * stopping;
                    }
                    if (HandbrakeInput > 0.5f) drive *= 0.15f;

                    _body.AddForceAtPosition(forward * drive * 0.5f, point);
                }

                // Lateral tyre force, limited by the available grip of this wheel.
                bool drifting = HandbrakeInput > 0.5f && !wheel.Front;
                float lateral = -lateralSpeed * (drifting ? driftGrip : grip) * _body.mass / 4f;
                float gripLimit = Mathf.Max(500f, suspension * 1.35f);
                lateral = Mathf.Clamp(lateral, -gripLimit, gripLimit);
                _body.AddForceAtPosition(right * lateral, point);

                // Rolling resistance.
                _body.AddForceAtPosition(-forward * longitudinalSpeed * rollingResistance, point);

                _wheelSpinDegrees += longitudinalSpeed / wheelRadius * Mathf.Rad2Deg * Time.fixedDeltaTime;
            }

            if (_groundedWheels > 0)
            {
                _body.AddForce(-transform.up * downforce * speed * speed);
            }

            // Recover from a crash/flip or falling out of the world.
            bool flipped = Vector3.Dot(transform.up, Vector3.up) < 0.1f && speed < 4f;
            _upsideDownTimer = flipped ? _upsideDownTimer + Time.fixedDeltaTime : 0f;
            if (transform.position.y < -30f || _upsideDownTimer > 2.5f) ResetCar();
        }
    }
}
