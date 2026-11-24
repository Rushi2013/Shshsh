using UnityEngine;

namespace CityDrive
{
    public enum DriveTrain { FrontWheelDrive, RearWheelDrive, AllWheelDrive }

    /// <summary>
    /// Full rigidbody vehicle controller: four independent raycast wheels, spring/damper
    /// suspension with anti-roll bars, load-based tyre friction circles, Ackermann
    /// steering, an automatic gearbox with a torque curve, weight transfer, drift via
    /// rear tyre slip and configurable stability assistance. No duplicate physics.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ArcadeCarController : MonoBehaviour
    {
        [Header("Inputs (-1..1 / 0..1), written by MobileControls")]
        public float ThrottleInput;
        public float BrakeInput;
        public float SteerInput;
        public float HandbrakeInput;

        [Header("Vehicle layout")]
        [SerializeField] private DriveTrain driveTrain = DriveTrain.AllWheelDrive;
        [SerializeField] private Vector3[] wheelOffsets =
        {
            new Vector3(-0.75f, 0.10f,  1.42f),
            new Vector3( 0.75f, 0.10f,  1.42f),
            new Vector3(-0.75f, 0.10f, -1.25f),
            new Vector3( 0.75f, 0.10f, -1.25f)
        };
        [SerializeField] private float wheelBase = 2.67f;
        [SerializeField] private float trackWidth = 1.50f;
        [SerializeField] private float centerOfMassHeight = 0.45f;

        [Header("Engine & transmission")]
        [SerializeField] private AnimationCurve torqueCurve = AnimationCurve.EaseInOut(0f, 150f, 1f, 330f);
        [SerializeField] private float idleRpm = 900f;
        [SerializeField] private float maxRpm = 7000f;
        [SerializeField] private float shiftUpRpm = 6300f;
        [SerializeField] private float shiftDownRpm = 2400f;
        [SerializeField] private float[] gearRatios = { 3.45f, 2.15f, 1.48f, 1.05f, 0.78f };
        [SerializeField] private float finalDriveRatio = 3.70f;
        [SerializeField] private float drivetrainEfficiency = 0.86f;
        [SerializeField] private float reverseForce = 8500f;
        [SerializeField] private float brakeForce = 27000f;
        [SerializeField] private float maxSpeedKph = 240f;

        [Header("Suspension")]
        [SerializeField] private float springStrength = 42000f;
        [SerializeField] private float springDamper = 5200f;
        [SerializeField] private float restLength = 0.42f;
        [SerializeField] private float wheelRadius = 0.34f;
        [SerializeField] private float antiRollFront = 9500f;
        [SerializeField] private float antiRollRear = 9500f;

        [Header("Tyres & handling")]
        [SerializeField] private float maxSteerAngle = 36f;
        [SerializeField] private float frontGrip = 1.35f;
        [SerializeField] private float rearGrip = 1.25f;
        [SerializeField] private float driftRearGrip = 0.22f;
        [SerializeField] private float rollingResistance = 12f;
        [SerializeField] private float aerodynamicDrag = 0.75f;
        [SerializeField] private float downforce = 2.4f;
        [SerializeField] private float yawDamping = 1.6f;
        [SerializeField] private float stabilityAssist = 0.85f;
        [SerializeField] private float speedSensitiveSteer = 0.42f;

        private Rigidbody _body;
        private VehicleVisuals _visuals;
        private readonly RaycastHit[] _hits = new RaycastHit[24];
        private readonly bool[] _grounded = new bool[4];
        private readonly float[] _suspensionForce = new float[4];
        private readonly Vector3[] _contactPoints = new Vector3[4];
        private readonly float[] _spinDegrees = new float[4];
        private Vector3 _lastVelocity;

        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private bool _spawnConfigured;
        private float _steerAngle;
        private float _frontLeftSteer;
        private float _frontRightSteer;
        private float _upsideDownTimer;
        private int _groundedWheels;
        private int _gear;
        private float _rpm;

        public float SpeedKph => _body == null ? 0f : _body.velocity.magnitude * 3.6f;
        public int GroundedWheels => _groundedWheels;
        public float SteerAngle => _steerAngle;
        public float FrontLeftSteerAngle => _frontLeftSteer;
        public float FrontRightSteerAngle => _frontRightSteer;
        public int CurrentGear => _gear + 1;
        public float EngineRpm => _rpm;
        public float[] WheelSpinDegrees => _spinDegrees;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.mass = 1280f;
            _body.centerOfMass = new Vector3(0f, -centerOfMassHeight, 0.02f);
            _body.drag = 0.015f;
            _body.angularDrag = 0.75f;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _body.maxAngularVelocity = 3.2f;
            _lastVelocity = _body.velocity;
            _rpm = idleRpm;

            BoxCollider collider = GetComponent<BoxCollider>();
            if (collider == null) collider = gameObject.AddComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = new Vector3(1.9f, 1.4f, 4.3f);

            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;

            if (wheelOffsets == null || wheelOffsets.Length != 4)
            {
                wheelOffsets = new[]
                {
                    new Vector3(-0.75f, 0.10f, 1.42f), new Vector3(0.75f, 0.10f, 1.42f),
                    new Vector3(-0.75f, 0.10f, -1.25f), new Vector3(0.75f, 0.10f, -1.25f)
                };
            }
            if (gearRatios == null || gearRatios.Length == 0) gearRatios = new[] { 3.45f, 2.15f, 1.48f, 1.05f, 0.78f };
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
            _lastVelocity = Vector3.zero;
            _steerAngle = 0f;
            _gear = 0;
            _upsideDownTimer = 0f;
            transform.SetPositionAndRotation(_body.position, _body.rotation);
        }

        private void Update()
        {
            if (_visuals == null) return;
            _visuals.UpdateWheels(_frontLeftSteer, _frontRightSteer, 0f, 0f, _spinDegrees);
        }

        private bool IsDriven(int index)
        {
            if (driveTrain == DriveTrain.AllWheelDrive) return true;
            bool front = index < 2;
            return driveTrain == DriveTrain.FrontWheelDrive ? front : !front;
        }

        /// <summary>Inner wheel turns more than the outer wheel for a given centre angle.</summary>
        private void ComputeAckermann(float centreAngle)
        {
            _steerAngle = centreAngle;
            if (Mathf.Abs(centreAngle) < 0.05f)
            {
                _frontLeftSteer = 0f;
                _frontRightSteer = 0f;
                return;
            }

            float radius = wheelBase / Mathf.Max(0.02f, Mathf.Tan(Mathf.Abs(centreAngle) * Mathf.Deg2Rad));
            float inner = Mathf.Atan(wheelBase / Mathf.Max(0.5f, radius - trackWidth * 0.5f)) * Mathf.Rad2Deg;
            float outer = Mathf.Atan(wheelBase / (radius + trackWidth * 0.5f)) * Mathf.Rad2Deg;
            float sign = Mathf.Sign(centreAngle);

            // Positive angle = right turn: right wheel is inner.
            if (centreAngle > 0f)
            {
                _frontLeftSteer = sign * outer;
                _frontRightSteer = sign * inner;
            }
            else
            {
                _frontLeftSteer = sign * inner;
                _frontRightSteer = sign * outer;
            }
        }

        private void UpdateTransmission(float forwardSpeed)
        {
            float wheelCircumference = 2f * Mathf.PI * wheelRadius;
            float wheelRpm = Mathf.Abs(forwardSpeed) / wheelCircumference * 60f;
            float gearedRpm = wheelRpm * gearRatios[_gear] * finalDriveRatio;
            _rpm = Mathf.Clamp(idleRpm + gearedRpm, idleRpm, maxRpm);

            if (_rpm > shiftUpRpm && _gear < gearRatios.Length - 1 && ThrottleInput > 0.1f)
            {
                _gear++;
                _rpm = shiftDownRpm + 400f;
            }
            else if (_rpm < shiftDownRpm && _gear > 0)
            {
                _gear--;
            }
        }

        private void ApplyAntiRoll(int left, int right, float strength)
        {
            if (!_grounded[left] && !_grounded[right]) return;
            float travelLeft = _grounded[left] ? _suspensionForce[left] : 0f;
            float travelRight = _grounded[right] ? _suspensionForce[right] : 0f;
            float antiRoll = (travelLeft - travelRight) * strength / 10000f;
            if (_grounded[left]) _body.AddForceAtPosition(Vector3.up * antiRoll, _contactPoints[left]);
            if (_grounded[right]) _body.AddForceAtPosition(Vector3.up * -antiRoll, _contactPoints[right]);
        }

        private void FixedUpdate()
        {
            if (_body == null || _body.isKinematic) return;

            float dt = Time.fixedDeltaTime;
            Vector3 acceleration = (_body.velocity - _lastVelocity) / dt;
            _lastVelocity = _body.velocity;
            float speed = _body.velocity.magnitude;
            float forwardSpeed = Vector3.Dot(_body.velocity, transform.forward);
            bool reversing = BrakeInput > 0.15f && forwardSpeed < 1.5f;

            float speedFactor = Mathf.Clamp01(speed / 45f);
            float centreSteer = Mathf.Clamp(SteerInput, -1f, 1f) * maxSteerAngle *
                                Mathf.Lerp(1f, speedSensitiveSteer, speedFactor);
            ComputeAckermann(Mathf.MoveTowards(_steerAngle, centreSteer, 260f * dt));
            UpdateTransmission(forwardSpeed);

            float rayLength = restLength + wheelRadius + 0.12f;
            for (int i = 0; i < 4; i++) _grounded[i] = false;
            _groundedWheels = 0;

            float[] wheelSteer = { _frontLeftSteer, _frontRightSteer, 0f, 0f };
            float drivenCount = 0f;
            for (int i = 0; i < 4; i++) if (IsDriven(i)) drivenCount++;
            drivenCount = Mathf.Max(1f, drivenCount);

            float torque = torqueCurve.Evaluate(Mathf.InverseLerp(idleRpm, maxRpm, _rpm));
            float engineForce = torque * gearRatios[_gear] * finalDriveRatio *
                                drivetrainEfficiency / wheelRadius;
            if (forwardSpeed > maxSpeedKph / 3.6f) engineForce = 0f;

            float longitudinalAccel = Vector3.Dot(acceleration, transform.forward);
            float lateralAccel = Vector3.Dot(acceleration, transform.right);
            float longitudinalTransfer = _body.mass * longitudinalAccel * centerOfMassHeight / wheelBase;
            float lateralTransfer = _body.mass * lateralAccel * centerOfMassHeight / trackWidth;

            for (int i = 0; i < 4; i++)
            {
                Vector3 origin = transform.TransformPoint(wheelOffsets[i]);
                int count = Physics.RaycastNonAlloc(origin, Vector3.down, _hits, rayLength,
                    ~0, QueryTriggerInteraction.Ignore);

                RaycastHit best = default;
                bool hitFound = false;
                float closest = float.MaxValue;
                for (int h = 0; h < count; h++)
                {
                    Collider other = _hits[h].collider;
                    if (other == null || other.transform.root == transform.root) continue;
                    if (_hits[h].normal.y < 0.45f) continue;
                    if (_hits[h].distance < closest)
                    {
                        closest = _hits[h].distance;
                        best = _hits[h];
                        hitFound = true;
                    }
                }

                if (!hitFound)
                {
                    _contactPoints[i] = origin - Vector3.up * wheelRadius;
                    continue;
                }

                _grounded[i] = true;
                _groundedWheels++;
                Vector3 point = best.point;
                Vector3 normal = best.normal;
                _contactPoints[i] = point;

                float compression = rayLength - best.distance;
                Vector3 pointVelocity = _body.GetPointVelocity(point);
                float spring = compression * springStrength -
                               Vector3.Dot(pointVelocity, Vector3.up) * springDamper;
                spring = Mathf.Clamp(spring, 0f, springStrength * 0.55f);
                _suspensionForce[i] = spring;
                _body.AddForceAtPosition(Vector3.up * spring, point);

                Vector3 forward = Quaternion.AngleAxis(wheelSteer[i], Vector3.up) * transform.forward;
                forward = Vector3.ProjectOnPlane(forward, normal).normalized;
                Vector3 right = Vector3.Cross(normal, forward).normalized;

                float longitudinalSpeed = Vector3.Dot(pointVelocity, forward);
                float lateralSpeed = Vector3.Dot(pointVelocity, right);

                // Load transfer estimate -> available tyre grip (friction circle radius).
                bool frontWheel = i < 2;
                bool leftWheel = i % 2 == 0;
                float load = _body.mass * 9.81f * 0.25f;
                load += frontWheel ? -longitudinalTransfer * 0.5f : longitudinalTransfer * 0.5f;
                load += leftWheel ? lateralTransfer * 0.5f : -lateralTransfer * 0.5f;
                load = Mathf.Max(600f, load);

                bool drifting = HandbrakeInput > 0.5f && !frontWheel;
                float gripCoefficient = frontWheel ? frontGrip : (drifting ? driftRearGrip : rearGrip);
                float maxTyreForce = load * gripCoefficient;

                // Longitudinal tyre force.
                float longitudinalForce = 0f;
                if (IsDriven(i) && !reversing && ThrottleInput > 0.01f)
                    longitudinalForce += engineForce * ThrottleInput / drivenCount;
                if (IsDriven(i) && reversing)
                    longitudinalForce -= reverseForce * BrakeInput / drivenCount;
                if (BrakeInput > 0f && !reversing)
                {
                    float stopping = Mathf.Min(brakeForce * BrakeInput / 4f,
                        Mathf.Abs(longitudinalSpeed) * _body.mass / (4f * dt));
                    longitudinalForce -= Mathf.Sign(longitudinalSpeed) * stopping;
                }
                if (HandbrakeInput > 0.5f && !frontWheel) longitudinalForce *= 0.06f;
                longitudinalForce -= longitudinalSpeed * rollingResistance;

                // Lateral tyre force from slip velocity.
                float lateralForce = -lateralSpeed * load * gripCoefficient * 1.35f;

                // Friction circle: combined longitudinal + lateral demand is limited.
                Vector2 demand = new Vector2(longitudinalForce, lateralForce);
                if (demand.magnitude > maxTyreForce) demand = demand.normalized * maxTyreForce;

                _body.AddForceAtPosition(forward * demand.x + right * demand.y, point);

                _spinDegrees[i] += longitudinalSpeed / wheelRadius * Mathf.Rad2Deg * dt;
            }

            ApplyAntiRoll(0, 1, antiRollFront);
            ApplyAntiRoll(2, 3, antiRollRear);

            if (_groundedWheels > 0)
            {
                _body.AddForce(-transform.up * downforce * speed * speed);

                bool drifting = HandbrakeInput > 0.5f;
                float damping = drifting ? yawDamping * 0.35f : yawDamping;
                _body.AddTorque(Vector3.up * -_body.angularVelocity.y * damping * _body.mass, ForceMode.Force);

                if (!drifting)
                {
                    float sideSpeed = Vector3.Dot(_body.velocity, transform.right);
                    _body.AddForce(-transform.right * sideSpeed * stabilityAssist * _body.mass);
                }

                float upright = Vector3.Dot(transform.up, Vector3.up);
                if (upright < 0.97f)
                    _body.AddTorque(Vector3.Cross(transform.up, Vector3.up) * _body.mass * 4.5f, ForceMode.Force);
            }

            _body.AddForce(-_body.velocity.normalized * aerodynamicDrag * speed * speed);

            bool flipped = Vector3.Dot(transform.up, Vector3.up) < 0.1f && speed < 4f;
            _upsideDownTimer = flipped ? _upsideDownTimer + dt : 0f;
            if (transform.position.y < -30f || _upsideDownTimer > 2.5f) ResetCar();
        }
    }
}
