using UnityEngine;

namespace CityDrive
{
    /// <summary>
    /// Four camera modes: chase, bonnet, top-down and a rigid driver-seat interior
    /// view. Follows the actual car transform/Rigidbody with configurable damping,
    /// look-ahead, speed-responsive FOV and occlusion protection.
    /// </summary>
    public sealed class ChaseCamera : MonoBehaviour
    {
        public enum CameraMode { Chase, Bonnet, TopDown, Interior }

        [Header("Targets")]
        public Transform Target;
        public ArcadeCarController Car;

        [Header("Chase camera")]
        [SerializeField] private float chaseDistance = 8.6f;
        [SerializeField] private float chaseHeight = 4.2f;
        [SerializeField] private float chaseLookAhead = 7f;
        [SerializeField] private float positionDamping = 0.12f;
        [SerializeField] private float rotationDamping = 9f;

        [Header("Other exterior cameras")]
        [SerializeField] private float bonnetHeight = 1.25f;
        [SerializeField] private float bonnetForward = 1.85f;
        [SerializeField] private float topHeight = 22f;
        [SerializeField] private float topBack = 6f;

        [Header("Interior / driver seat")]
        [SerializeField] private Vector3 interiorEyeOffset = new Vector3(-0.34f, 0.46f, 0.12f);
        [SerializeField] private float interiorPitch = -2f;
        [SerializeField] private float interiorFov = 68f;
        [SerializeField] private float interiorNearClip = 0.06f;

        [Header("Lens")]
        [SerializeField] private float baseFov = 64f;
        [SerializeField] private float speedFovGain = 16f;
        [SerializeField] private float maxSpeedKph = 235f;

        private int _mode;
        private Vector3 _velocity = Vector3.zero;
        private readonly RaycastHit[] _hits = new RaycastHit[16];

        public int Mode => _mode;
        public CameraMode ActiveMode => (CameraMode)_mode;

        public void CycleMode()
        {
            _mode = (_mode + 1) % 4;
            _velocity = Vector3.zero;
        }

        public void SetMode(int mode)
        {
            _mode = Mathf.Clamp(mode, 0, 3);
            _velocity = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (Target == null) return;

            Camera cameraComponent = GetComponent<Camera>();
            float speedRatio = Car != null ? Mathf.Clamp01(Car.SpeedKph / maxSpeedKph) : 0f;

            if (_mode == (int)CameraMode.Interior)
            {
                // Rigidly parented view: no lag, no shaking, exact vehicle rotation.
                transform.SetPositionAndRotation(
                    Target.TransformPoint(interiorEyeOffset),
                    Target.rotation * Quaternion.Euler(interiorPitch, 0f, 0f));
                if (cameraComponent != null)
                {
                    cameraComponent.nearClipPlane = interiorNearClip;
                    cameraComponent.fieldOfView = interiorFov;
                }
                return;
            }

            if (cameraComponent != null) cameraComponent.nearClipPlane = 0.25f;

            Vector3 forward = Target.forward;
            Vector3 position = Target.position;
            Vector3 desiredPosition;
            Vector3 lookAt;

            switch (_mode)
            {
                case (int)CameraMode.Bonnet:
                    desiredPosition = position + Vector3.up * bonnetHeight + forward * bonnetForward;
                    lookAt = position + Vector3.up * 1.1f + forward * 30f;
                    break;
                case (int)CameraMode.TopDown:
                    desiredPosition = position + Vector3.up * topHeight - forward * topBack;
                    lookAt = position + forward * 8f;
                    break;
                default:
                    desiredPosition = position + Vector3.up * chaseHeight - forward * chaseDistance;
                    lookAt = position + Vector3.up * 1.2f + forward * chaseLookAhead;
                    break;
            }

            if (_mode == (int)CameraMode.Chase)
            {
                Vector3 line = desiredPosition - lookAt;
                float distance = line.magnitude;
                if (distance > 0.01f)
                {
                    Vector3 direction = line / distance;
                    int count = Physics.RaycastNonAlloc(lookAt, direction, _hits, distance,
                        ~0, QueryTriggerInteraction.Ignore);
                    float closest = distance;
                    for (int i = 0; i < count; i++)
                    {
                        if (_hits[i].collider == null || _hits[i].collider.transform.root == Target.root) continue;
                        if (_hits[i].distance < closest) closest = _hits[i].distance;
                    }
                    if (closest < distance)
                        desiredPosition = lookAt + direction * Mathf.Max(1.2f, closest - 0.4f);
                }
            }

            float damping = positionDamping * Mathf.Lerp(1f, 0.65f, speedRatio);
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, damping);

            Vector3 toLook = lookAt - transform.position;
            if (toLook.sqrMagnitude > 0.01f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(toLook.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation,
                    1f - Mathf.Exp(-rotationDamping * Time.deltaTime));
            }

            if (cameraComponent != null)
            {
                float targetFov = baseFov + speedRatio * speedFovGain;
                cameraComponent.fieldOfView = Mathf.Lerp(cameraComponent.fieldOfView, targetFov,
                    Time.deltaTime * 2.5f);
            }
        }
    }
}
