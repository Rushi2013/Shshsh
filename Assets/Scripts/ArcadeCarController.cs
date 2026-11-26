using System;
using UnityEngine;

namespace CityDrive
{
    public enum DriveTrain { FrontWheelDrive, RearWheelDrive, AllWheelDrive }

    /// <summary>
    /// Physics-driven sedan controller. One Rigidbody, four independently raycast
    /// wheels with spring/damper suspension, per-wheel angular velocity, slip-based
    /// tyre forces inside a friction circle, anti-roll bars, automatic transmission
    /// and configurable FWD/RWD/AWD. All outputs are derived from real contact and
    /// velocity - no Transform movement and no scripted drift/burnout animation.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ArcadeCarController : MonoBehaviour
    {
        [Header("Inputs (-1..1 / 0..1), written by MobileControls")]
        public float ThrottleInput;
        public float BrakeInput;
        public float ReverseInput;
        public float SteerInput;
        public float HandbrakeInput;

        [Header("Vehicle layout")]
        [SerializeField] private DriveTrain driveTrain = DriveTrain.AllWheelDrive;
        [SerializeField] private Vector3[] wheelOffsets =
        {
            new Vector3(-0.75f, 0.10f, 1.42f),
            new Vector3( 0.75f, 0.10f, 1.42f),
            new Vector3(-0.75f, 0.10f, -1.25f),
            new Vector3( 0.75f, 0.10f, -1.25f)
        };
        [SerializeField] private float wheelBase = 2.67f;
        [SerializeField] private float trackWidth = 1.50f;
        [SerializeField] private float centerOfMassHeight = 0.45f;
        [SerializeField] private float vehicleMass = 1420f;

        [Header("Engine & transmission")]
        [SerializeField] private AnimationCurve torqueCurve = AnimationCurve.EaseInOut(0f, 170f, 1f, 360f);
        [SerializeField] private float idleRpm = 850f;
        [SerializeField] private float maxRpm = 7000f;
        [SerializeField] private float shiftUpRpm = 6300f;
        [SerializeField] private float shiftDownRpm = 2500f;
        [SerializeField] private float[] gearRatios = { 3.45f, 2.15f, 1.48f, 1.05f, 0.78f };
        [SerializeField] private float finalDriveRatio = 3.70f;
        [SerializeField] private float drivetrainEfficiency = 0.86f;
        [SerializeField] private float reverseTorque = 1700f;
        [SerializeField] private float brakeForce = 31000f;
        [SerializeField] private float maxSpeedKph = 245f;

        [Header("Suspension")]
        [SerializeField] private float springStrength = 44000f;
        [SerializeField] private float springDamper = 5400f;
        [SerializeField] private float restLength = 0.42f;
        [SerializeField] private float wheelRadius = 0.34f;
        [SerializeField] private float antiRollFront = 10500f;
        [SerializeField] private float antiRollRear = 9800f;

        [Header("Tyres & handling")]
        [SerializeField] private float maxSteerAngle = 36f;
        [SerializeField] private float frontGrip = 1.35f;
        [SerializeField] private float rearGrip = 1.28f;
        [SerializeField] private float driftRearGrip = 0.24f;
        [SerializeField] private float slipStiffness = 4.2f;
        [SerializeField] private float wheelInertia = 1.45f;
        [SerializeField] private float rollingResistance = 12f;
        [SerializeField] private float aerodynamicDrag = 0.75f;
        [SerializeField] private float downforce = 2.4f;
        [SerializeField] private float yawDamping = 1.35f;
        [SerializeField] private float stabilityAssist = 0.65f;
        [SerializeField] private float speedSensitiveSteer = 0.42f;

        [Header("Burnout (still real tyre slip, not an effect trigger)")]
        [SerializeField] private float burnoutGripMultiplier = 0.52f;
        [SerializeField] private float burnoutBrakeBias = 0.38f;
        [SerializeField] private float burnoutMaxSpeedKph = 18f;

        private Rigidbody _body;
        private VehicleVisuals _visuals;
        private readonly RaycastHit[] _hits = new RaycastHit[24];
        private readonly bool[] _grounded = new bool[4];
        private readonly float[] _suspensionForce = new float[4];
        private readonly Vector3[] _contactPoints = new Vector3[4];
        private readonly WheelTelemetry[] _wheels = new WheelTelemetry[4];
        private readonly float[] _wheelOmega = new float[4];
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
        private bool _burnout;
        private float _engineLoad;

        public event Action<int> GearChanged;
        public event Action<float> Collided;
        public event Action HornRequested;

        public float SpeedKph => _body == null ? 0f : _body.velocity.magnitude * 3.6f;
        public float ForwardSpeedKph => _body == null ? 0f : Vector3.Dot(_body.velocity, transform.forward) * 3.6f;
        public int GroundedWheels => _groundedWheels;
        public float SteerAngle => _steerAngle;
        public float FrontLeftSteerAngle => _frontLeftSteer;
        public float FrontRightSteerAngle => _frontRightSteer;
        public int CurrentGear => _gear + 1;
        public float EngineRpm => _rpm;
        public float EngineLoad => _engineLoad;
        public bool IsBurnout => _burnout;
        public bool IsReversing => ReverseInput > 0.15f && ThrottleInput < 0.15f && ForwardSpeedKph < 5f;
        public float[] WheelSpinDegrees => _spinDegrees;
        public WheelTelemetry[] Wheels => _wheels;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            if (_body == null) _body = gameObject.AddComponent<Rigidbody>();
            _body.mass = vehicleMass;
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
            PhysicMaterial carMaterial = collider.material;
            carMaterial.dynamicFriction = 0.6f;
            carMaterial.staticFriction = 0.65f;
            carMaterial.bounciness = 0f;
            collider.material = carMaterial;

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
            _frontLeftSteer = 0f;
            _frontRightSteer = 0f;
            _gear = 0;
            _rpm = idleRpm;
            _upsideDownTimer = 0f;
            _burnout = false;
            for (int i = 0; i < 4; i++)
            {
                _wheelOmega[i] = 0f;
                _spinDegrees[i] = 0f;
                _grounded[i] = false;
                _wheels[i] = new WheelTelemetry();
            }
            transform.SetPositionAndRotation(_body.position, _body.rotation);
        }

        public void Honk()
        {
            if (HornRequested != null) HornRequested();
        }

        private void Update()
        {
            if (_visuals == null) return;
            _visuals.UpdateWheels(_frontLeftSteer, _frontRightSteer, 0f, 0f, _spinDegrees);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (Collided != null && collision != null && collision.relativeVelocity.magnitude > 1.5f)
                Collided(collision.relativeVelocity.magnitude);
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

            // Positive centre angle = right turn: the right wheel is inner.
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
            float circumference = 2f * Mathf.PI * wheelRadius;
            float roadWheelRpm = Mathf.Abs(forwardSpeed) / circumference * 60f;
            float roadRpm = idleRpm + roadWheelRpm * gearRatios[_gear] * finalDriveRatio;

            float drivenOmegaSum = 0f;
            int drivenWheels = 0;
            for (int i = 0; i < 4; i++)
            {
                if (!IsDriven(i)) continue;
                drivenOmegaSum += Mathf.Abs(_wheelOmega[i]);
                drivenWheels++;
            }
            float spinRpm = drivenWheels > 0
                ? drivenOmegaSum / drivenWheels / (2f * Mathf.PI) * 60f * gearRatios[_gear] * finalDriveRatio
                : 0f;

            // Wheelspin raises the tachometer, but gear changes follow road speed.
            float targetRpm = Mathf.Max(roadRpm, idleRpm + spinRpm * 0.85f);
            _rpm = Mathf.Lerp(_rpm, Mathf.Clamp(targetRpm, idleRpm, maxRpm), Time.fixedDeltaTime * 8f);
            _engineLoad = Mathf.Clamp01(ThrottleInput * (0.45f + 0.55f * Mathf.InverseLerp(idleRpm, maxRpm, _rpm)));

            int previousGear = _gear;
            if (roadRpm > shiftUpRpm && _gear < gearRatios.Length - 1 && ThrottleInput > 0.1f)
            {
                _gear++;
                _rpm = shiftDownRpm + 500f;
            }
            else if (roadRpm < shiftDownRpm && _gear > 0)
            {
                _gear--;
            }
            if (_gear != previousGear && GearChanged != null) GearChanged(_gear + 1);
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
            // Brake + throttle is a burnout request, never mistaken for reverse.
            _burnout = ThrottleInput > 0.35f && BrakeInput > 0.35f &&
                       SpeedKph < burnoutMaxSpeedKph;
            bool reversing = !_burnout && ReverseInput > 0.15f && ThrottleInput < 0.15f &&
                             forwardSpeed < 1.5f;
            // Reverse first brakes a moving-forward car; the brake pedal itself never selects reverse.
            float serviceBrake = Mathf.Max(BrakeInput,
                ReverseInput > 0.15f && forwardSpeed >= 1.5f ? ReverseInput : 0f);

            float speedFactor = Mathf.Clamp01(speed / 45f);
            float centreSteer = Mathf.Clamp(SteerInput, -1f, 1f) * maxSteerAngle *
                                Mathf.Lerp(1f, speedSensitiveSteer, speedFactor);
            ComputeAckermann(Mathf.MoveTowards(_steerAngle, centreSteer, 260f * dt));
            UpdateTransmission(forwardSpeed);

            float rayLength = restLength + wheelRadius + 0.12f;
            for (int i = 0; i < 4; i++)
            {
                _grounded[i] = false;
                _wheels[i] = new WheelTelemetry();
            }
            _groundedWheels = 0;

            float[] wheelSteer = { _frontLeftSteer, _frontRightSteer, 0f, 0f };
            float drivenCount = 0f;
            for (int i = 0; i < 4; i++) if (IsDriven(i)) drivenCount++;
            drivenCount = Mathf.Max(1f, drivenCount);

            float torqueNm = torqueCurve.Evaluate(Mathf.InverseLerp(idleRpm, maxRpm, _rpm));
            float gearMultiplier = gearRatios[_gear] * finalDriveRatio * drivetrainEfficiency;
            float maxOmega = 900f;

            float longitudinalAccel = Vector3.Dot(acceleration, transform.forward);
            float lateralAccel = Vector3.Dot(acceleration, transform.right);
            float longitudinalTransfer = vehicleMass * longitudinalAccel * centerOfMassHeight / wheelBase;
            float lateralTransfer = vehicleMass * lateralAccel * centerOfMassHeight / trackWidth;

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

                float omega = _wheelOmega[i];

                if (!hitFound)
                {
                    // Airborne wheel: engine can spin it freely, but no tyre force exists.
                    if (IsDriven(i) && ThrottleInput > 0.01f && !reversing)
                        omega += torqueNm * gearMultiplier / drivenCount / wheelInertia * dt;
                    omega = Mathf.Clamp(omega * (1f - dt * 0.45f), -maxOmega, maxOmega);
                    _wheelOmega[i] = omega;
                    _spinDegrees[i] += omega * Mathf.Rad2Deg * dt;
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
                float groundOmega = longitudinalSpeed / wheelRadius;

                bool frontWheel = i < 2;
                bool leftWheel = i % 2 == 0;
                bool driven = IsDriven(i);
                bool handbrakeRear = HandbrakeInput > 0.5f && !frontWheel;
                bool burnoutWheel = _burnout && driven;

                float load = vehicleMass * 9.81f * 0.25f;
                load += frontWheel ? -longitudinalTransfer * 0.5f : longitudinalTransfer * 0.5f;
                load += leftWheel ? lateralTransfer * 0.5f : -lateralTransfer * 0.5f;
                load = Mathf.Max(600f, load);

                float grip = frontWheel ? frontGrip : (handbrakeRear ? driftRearGrip : rearGrip);
                if (burnoutWheel) grip *= burnoutGripMultiplier;
                float maxTyreForce = load * grip;

                // Wheel torque -> wheel angular velocity -> longitudinal slip -> tyre force.
                float driveTorque = 0f;
                if (driven && ThrottleInput > 0.01f && !reversing && speed < maxSpeedKph / 3.6f)
                    driveTorque = torqueNm * gearMultiplier * ThrottleInput / drivenCount;
                if (driven && reversing)
                    driveTorque = -reverseTorque * ReverseInput / drivenCount;

                float brakeTorque = brakeForce * serviceBrake * 0.25f * wheelRadius;
                if (burnoutWheel) brakeTorque *= burnoutBrakeBias;
                if (handbrakeRear) brakeTorque *= 0.12f;
                float rollingTorque = longitudinalSpeed * rollingResistance * wheelRadius;

                float slipRatio = (omega * wheelRadius - longitudinalSpeed) /
                                  Mathf.Max(2f, Mathf.Abs(longitudinalSpeed));
                slipRatio = Mathf.Clamp(slipRatio, -1f, 1f);
                float longitudinalForce = Mathf.Clamp(
                    slipRatio * slipStiffness * load, -maxTyreForce, maxTyreForce);

                float reactionTorque = longitudinalForce * wheelRadius;
                float brakeSign = Mathf.Abs(omega) > 0.1f ? Mathf.Sign(omega) : Mathf.Sign(longitudinalSpeed);
                float netTorque = driveTorque - reactionTorque -
                                  brakeSign * brakeTorque - rollingTorque;
                float previousOmega = omega;
                omega += netTorque / wheelInertia * dt;

                // Compare the actual pre/post step, not a second hypothetical step.
                if (serviceBrake > 0.05f && !burnoutWheel && !reversing &&
                    Mathf.Abs(previousOmega) > 0.001f && Mathf.Sign(previousOmega) != Mathf.Sign(omega) &&
                    Mathf.Abs(longitudinalSpeed) < 3f)
                    omega = 0f;

                omega = Mathf.Clamp(omega, -maxOmega, maxOmega);

                // A gripping wheel tracks the ground; a slipping wheel keeps its own omega.
                if (!burnoutWheel && !reversing && ThrottleInput < 0.05f && BrakeInput < 0.05f)
                    omega = Mathf.Lerp(omega, groundOmega, Mathf.Clamp01(dt * 9f));

                _wheelOmega[i] = omega;
                _spinDegrees[i] += omega * Mathf.Rad2Deg * dt;

                slipRatio = Mathf.Clamp(
                    (omega * wheelRadius - longitudinalSpeed) / Mathf.Max(2f, Mathf.Abs(longitudinalSpeed)),
                    -1f, 1f);
                longitudinalForce = Mathf.Clamp(
                    slipRatio * slipStiffness * load, -maxTyreForce, maxTyreForce);

                // Lateral tyre force and combined-slip friction circle.
                float lateralForce = -lateralSpeed * load * grip * 1.5f;
                Vector2 demand = new Vector2(longitudinalForce, lateralForce);
                if (demand.magnitude > maxTyreForce) demand = demand.normalized * maxTyreForce;
                _body.AddForceAtPosition(forward * demand.x + right * demand.y, point);

                float speedRef = Mathf.Max(1f, Mathf.Abs(longitudinalSpeed));
                _wheels[i] = new WheelTelemetry
                {
                    Grounded = true,
                    Driven = driven,
                    ContactPoint = point,
                    ContactNormal = normal,
                    Load = load,
                    SpinRadPerSecond = omega,
                    GroundSpeed = longitudinalSpeed,
                    SlipRatio = slipRatio,
                    LateralSlip = lateralSpeed / speedRef,
                    LongitudinalSlip = slipRatio,
                    BurnoutSlip = burnoutWheel ? Mathf.Abs(slipRatio) : 0f
                };
            }

            ApplyAntiRoll(0, 1, antiRollFront);
            ApplyAntiRoll(2, 3, antiRollRear);

            if (_groundedWheels > 0)
            {
                _body.AddForce(-Vector3.up * downforce * speed * speed);

                bool sliding = HandbrakeInput > 0.5f || _burnout ||
                               AverageTotalSlip() > 0.45f;
                float damping = sliding ? yawDamping * 0.45f : yawDamping;
                _body.AddTorque(Vector3.up * -_body.angularVelocity.y * damping * vehicleMass * 0.35f,
                    ForceMode.Force);

                if (!sliding)
                {
                    float sideSpeed = Vector3.Dot(_body.velocity, transform.right);
                    _body.AddForce(-transform.right * sideSpeed * stabilityAssist * vehicleMass * 0.45f);
                }

                float upright = Vector3.Dot(transform.up, Vector3.up);
                if (upright < 0.97f)
                    _body.AddTorque(Vector3.Cross(transform.up, Vector3.up) * vehicleMass * 4.5f,
                        ForceMode.Force);
            }

            if (speed > 0.05f)
                _body.AddForce(-_body.velocity.normalized * aerodynamicDrag * speed * speed);

            bool flipped = Vector3.Dot(transform.up, Vector3.up) < 0.1f && speed < 4f;
            _upsideDownTimer = flipped ? _upsideDownTimer + dt : 0f;
            if (transform.position.y < -30f || _upsideDownTimer > 2.5f) ResetCar();
        }

        private float AverageTotalSlip()
        {
            float total = 0f;
            for (int i = 0; i < 4; i++) total += _wheels[i].TotalSlip;
            return total * 0.25f;
        }
    }
}
