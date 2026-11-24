using UnityEngine;

namespace CityDrive
{
    /// <summary>Chase / bonnet / top-down camera with speed-based FOV and wall clipping protection.</summary>
    public sealed class ChaseCamera : MonoBehaviour
    {
        public Transform Target;
        public ArcadeCarController Car;

        private int _mode;
        private Vector3 _velocity = Vector3.zero;
        private readonly RaycastHit[] _hits = new RaycastHit[16];

        public int Mode => _mode;

        public void CycleMode()
        {
            _mode = (_mode + 1) % 3;
        }

        private void LateUpdate()
        {
            if (Target == null) return;

            Vector3 forward = Target.forward;
            Vector3 position = Target.position;
            Vector3 desiredPosition;
            Vector3 lookAt;

            switch (_mode)
            {
                case 1: // bonnet
                    desiredPosition = position + Vector3.up * 1.25f + forward * 1.85f;
                    lookAt = position + Vector3.up * 1.1f + forward * 30f;
                    break;
                case 2: // top / bird's eye
                    desiredPosition = position + Vector3.up * 22f - forward * 6f;
                    lookAt = position + forward * 8f;
                    break;
                default: // chase
                    desiredPosition = position + Vector3.up * 4.2f - forward * 8.6f;
                    lookAt = position + Vector3.up * 1.2f + forward * 7f;
                    break;
            }

            if (_mode == 0)
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
                    if (closest < distance) desiredPosition = lookAt + direction * Mathf.Max(1.2f, closest - 0.4f);
                }
            }

            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, 0.12f);

            Vector3 toLook = lookAt - transform.position;
            if (toLook.sqrMagnitude > 0.01f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(toLook.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation,
                    1f - Mathf.Exp(-9f * Time.deltaTime));
            }

            Camera cameraComponent = GetComponent<Camera>();
            if (cameraComponent != null && Car != null)
            {
                float speedRatio = Mathf.Clamp01(Car.SpeedKph / 235f);
                float targetFov = 64f + speedRatio * 16f;
                cameraComponent.fieldOfView = Mathf.Lerp(cameraComponent.fieldOfView, targetFov,
                    Time.deltaTime * 2.5f);
            }
        }
    }
}
