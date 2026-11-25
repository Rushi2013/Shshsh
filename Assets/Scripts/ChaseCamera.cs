using UnityEngine;

namespace CityDrive
{
    /// <summary>
    /// Three reliable camera modes: close chase, far chase and driver-seat interior.
    /// One camera only; switching modes never touches vehicle physics or controls.
    /// Exterior modes use damped follow with occlusion protection; interior mode is
    /// rigidly locked to the car transform after a short smooth transition.
    /// </summary>
    public sealed class ChaseCamera : MonoBehaviour
    {
        public enum CameraMode { CloseChase, FarChase, Interior }

        [Header("Targets")]
        public Transform Target;
        public ArcadeCarController Car;

        [Header("Close chase")]
        [SerializeField] private float closeDistance = 7.4f;
        [SerializeField] private float closeHeight = 3.35f;
        [SerializeField] private float closeLookAhead = 7.5f;
        [SerializeField] private float closeFov = 64f;

        [Header("Far chase")]
        [SerializeField] private float farDistance = 14f;
        [SerializeField] private float farHeight = 6.8f;
        [SerializeField] private float farLookAhead = 11f;
        [SerializeField] private float farFov = 70f;

        [Header("Interior / driver seat")]
        [SerializeField] private Vector3 interiorEyeOffset = new Vector3(-0.32f, 0.42f, 0.24f);
        [SerializeField] private float interiorPitch = -1.5f;
        [SerializeField] private float interiorFov = 70f;
        [SerializeField] private float interiorNearClip = 0.06f;

        [Header("Lens & response")]
        [SerializeField] private float positionDamping = 0.11f;
        [SerializeField] private float rotationDamping = 9f;
        [SerializeField] private float speedFovGain = 12f;
        [SerializeField] private float maxSpeedKph = 235f;
        [SerializeField] private float modeBlendTime = 0.32f;

        private int _mode;
        private Vector3 _velocity = Vector3.zero;
        private float _blend = 1f;
        private Vector3 _blendFromPosition;
        private Quaternion _blendFromRotation = Quaternion.identity;
        private readonly RaycastHit[] _hits = new RaycastHit[16];

        public int Mode => _mode;
        public CameraMode ActiveMode => (CameraMode)_mode;

        public void CycleMode()
        {
            BeginBlend((_mode + 1) % 3);
        }

        public void SetMode(int mode)
        {
            BeginBlend(Mathf.Clamp(mode, 0, 2));
        }

        private void BeginBlend(int next)
        {
            if (next == _mode) return;
            _mode = next;
            _blend = 0f;
            _blendFromPosition = transform.position;
            _blendFromRotation = transform.rotation;
            _velocity = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (Target == null) return;

            Camera lens = GetComponent<Camera>();
            float speedRatio = Car != null ? Mathf.Clamp01(Car.SpeedKph / maxSpeedKph) : 0f;
            if (_blend < 1f) _blend = Mathf.Clamp01(_blend + Time.unscaledDeltaTime / modeBlendTime);

            Vector3 forward = Target.forward;
            Vector3 position = Target.position;
            Vector3 desiredPosition;
            Vector3 lookAt;
            float baseFov;

            switch ((CameraMode)_mode)
            {
                case CameraMode.FarChase:
                    desiredPosition = position + Vector3.up * farHeight - forward * farDistance;
                    lookAt = position + Vector3.up * 1.2f + forward * farLookAhead;
                    baseFov = farFov;
                    break;
                case CameraMode.Interior:
                    desiredPosition = Target.TransformPoint(interiorEyeOffset);
                    Quaternion eyeRotation = Target.rotation * Quaternion.Euler(interiorPitch, 0f, 0f);
                    Vector3 eyeLook = desiredPosition + eyeRotation * Vector3.forward * 30f;
                    ApplyInterior(desiredPosition, eyeRotation, eyeLook, lens, speedRatio, baseFov = interiorFov);
                    return;
                default:
                    desiredPosition = position + Vector3.up * closeHeight - forward * closeDistance;
                    lookAt = position + Vector3.up * 1.15f + forward * closeLookAhead;
                    baseFov = closeFov;
                    break;
            }

            if (lens != null) lens.nearClipPlane = 0.25f;
            desiredPosition = AvoidObstacles(lookAt, desiredPosition);

            float damping = positionDamping * Mathf.Lerp(1f, 0.68f, speedRatio);
            Vector3 smoothed = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, damping);
            Vector3 toLook = lookAt - smoothed;
            Quaternion desiredRotation = toLook.sqrMagnitude > 0.01f
                ? Quaternion.LookRotation(toLook.normalized, Vector3.up)
                : transform.rotation;

            float rotate = 1f - Mathf.Exp(-rotationDamping * Time.unscaledDeltaTime);
            Quaternion finalRotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotate);

            if (_blend < 1f)
            {
                float t = Mathf.SmoothStep(0f, 1f, _blend);
                smoothed = Vector3.Lerp(_blendFromPosition, smoothed, t);
                finalRotation = Quaternion.Slerp(_blendFromRotation, finalRotation, t);
            }

            transform.SetPositionAndRotation(smoothed, finalRotation);
            SetFov(lens, baseFov, speedRatio);
        }

        private void ApplyInterior(Vector3 eye, Quaternion rotation, Vector3 lookAt,
            Camera lens, float speedRatio, float fov)
        {
            if (lens != null) lens.nearClipPlane = interiorNearClip;

            if (_blend < 1f)
            {
                float t = Mathf.SmoothStep(0f, 1f, _blend);
                Vector3 position = Vector3.Lerp(_blendFromPosition, eye, t);
                Quaternion facing = Quaternion.Slerp(_blendFromRotation, rotation, t);
                transform.SetPositionAndRotation(position, facing);
            }
            else
            {
                // Rigid driver-seat view: exact car movement and rotation, no lag/shake.
                transform.SetPositionAndRotation(eye, rotation);
            }

            SetFov(lens, fov, speedRatio * 0.35f);
            if (lookAt == Vector3.zero) return; // keeps the eye rotation explicit for tuning
        }

        private Vector3 AvoidObstacles(Vector3 lookAt, Vector3 desired)
        {
            Vector3 line = desired - lookAt;
            float distance = line.magnitude;
            if (distance < 0.01f) return desired;
            Vector3 direction = line / distance;

            int count = Physics.RaycastNonAlloc(lookAt, direction, _hits, distance,
                ~0, QueryTriggerInteraction.Ignore);
            float closest = distance;
            for (int i = 0; i < count; i++)
            {
                if (_hits[i].collider == null || _hits[i].collider.transform.root == Target.root) continue;
                if (_hits[i].distance < closest) closest = _hits[i].distance;
            }
            return closest < distance
                ? lookAt + direction * Mathf.Max(1.3f, closest - 0.45f)
                : desired;
        }

        private void SetFov(Camera lens, float baseFov, float speedRatio)
        {
            if (lens == null) return;
            float target = baseFov + speedFovGain * speedRatio;
            lens.fieldOfView = Mathf.Lerp(lens.fieldOfView, target,
                1f - Mathf.Exp(-3f * Time.unscaledDeltaTime));
        }
    }
}
