using UnityEngine;

namespace CityDrive
{
    /// <summary>
    /// Spins and steers the original BMW wheel meshes that come with the model.
    /// Wheels are matched by the names used by the asset (WheelFL, WheelFR, ...).
    /// </summary>
    public sealed class VehicleVisuals : MonoBehaviour
    {
        private Transform _frontLeft;
        private Transform _frontRight;
        private Transform _rearLeft;
        private Transform _rearRight;

        private Quaternion _frontLeftBase;
        private Quaternion _frontRightBase;
        private Quaternion _rearLeftBase;
        private Quaternion _rearRightBase;
        private bool _spinAroundLocalZ;

        public void FindWheels(Transform modelRoot)
        {
            Transform[] all = modelRoot.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in all)
            {
                switch (t.name)
                {
                    case "WheelFL": _frontLeft = t; break;
                    case "WheelFR": _frontRight = t; break;
                    case "WheelRL": _rearLeft = t; break;
                    case "WheelRR": _rearRight = t; break;
                    // 2025 BMW 330i xDrive GLB node names.
                    case "polySurface4": _frontLeft = t; _spinAroundLocalZ = true; break;
                    case "polySurface3": _frontRight = t; _spinAroundLocalZ = true; break;
                    case "polySurface2": _rearLeft = t; _spinAroundLocalZ = true; break;
                    case "polySurface1": _rearRight = t; _spinAroundLocalZ = true; break;
                }
            }

            if (_frontLeft != null) _frontLeftBase = _frontLeft.localRotation;
            if (_frontRight != null) _frontRightBase = _frontRight.localRotation;
            if (_rearLeft != null) _rearLeftBase = _rearLeft.localRotation;
            if (_rearRight != null) _rearRightBase = _rearRight.localRotation;
        }

        /// <summary>Steers around the car's real up axis, even if the model is mirrored.</summary>
        private static Quaternion SteerInWheelSpace(Transform wheel, float degrees)
        {
            if (wheel == null) return Quaternion.identity;
            Quaternion worldSteer = Quaternion.AngleAxis(degrees, wheel.root.up);
            Transform parent = wheel.parent;
            if (parent == null) return worldSteer;
            return Quaternion.Inverse(parent.rotation) * worldSteer * parent.rotation;
        }

        public void UpdateWheels(float steerDegrees, float spinDegrees)
        {
            Quaternion roll = Quaternion.AngleAxis(spinDegrees,
                _spinAroundLocalZ ? Vector3.forward : Vector3.right);

            if (_frontLeft != null)
            {
                Quaternion steer = SteerInWheelSpace(_frontLeft, steerDegrees);
                _frontLeft.localRotation = steer * _frontLeftBase * roll;
            }
            if (_frontRight != null)
            {
                Quaternion steer = SteerInWheelSpace(_frontRight, steerDegrees);
                _frontRight.localRotation = steer * _frontRightBase * roll;
            }
            if (_rearLeft != null) _rearLeft.localRotation = _rearLeftBase * roll;
            if (_rearRight != null) _rearRight.localRotation = _rearRightBase * roll;
        }
    }
}
