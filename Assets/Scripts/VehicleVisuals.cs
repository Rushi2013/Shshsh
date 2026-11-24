using UnityEngine;

namespace CityDrive
{
    /// <summary>
    /// Keeps the imported BMW wheel meshes in sync with the simulated wheels.
    /// Supports separate Ackermann angles per front wheel and an optional 3D
    /// interior steering wheel that follows the same steering input.
    /// </summary>
    public sealed class VehicleVisuals : MonoBehaviour
    {
        private readonly Transform[] _wheels = new Transform[4]; // 0=FL, 1=FR, 2=RL, 3=RR
        private readonly Quaternion[] _baseRotations = new Quaternion[4];
        private bool _spinAroundLocalZ;

        private Transform _interiorWheel;
        private Quaternion _interiorBaseRotation;

        public void FindWheels(Transform modelRoot)
        {
            if (modelRoot == null) return;
            foreach (Transform t in modelRoot.GetComponentsInChildren<Transform>(true))
            {
                int index = WheelIndex(t.name);
                if (index < 0) continue;
                _wheels[index] = t;
                _baseRotations[index] = t.localRotation;
                if (t.name.StartsWith("polySurface")) _spinAroundLocalZ = true;
            }
        }

        private static int WheelIndex(string name)
        {
            switch (name)
            {
                case "WheelFL":
                case "polySurface4": return 0;
                case "WheelFR":
                case "polySurface3": return 1;
                case "WheelRL":
                case "polySurface2": return 2;
                case "WheelRR":
                case "polySurface1": return 3;
                default: return -1;
            }
        }

        public void SetInteriorWheel(Transform wheel)
        {
            _interiorWheel = wheel;
            if (wheel != null) _interiorBaseRotation = wheel.localRotation;
        }

        /// <summary>Converts a steering angle around the car's up axis into wheel-local space.</summary>
        private static Quaternion SteerInWheelSpace(Transform wheel, float degrees)
        {
            if (wheel == null || Mathf.Abs(degrees) < 0.001f) return Quaternion.identity;
            Quaternion worldSteer = Quaternion.AngleAxis(degrees, wheel.root.up);
            Transform parent = wheel.parent;
            if (parent == null) return worldSteer;
            return Quaternion.Inverse(parent.rotation) * worldSteer * parent.rotation;
        }

        public void UpdateWheels(float frontLeftSteer, float frontRightSteer,
            float rearLeftSteer, float rearRightSteer, float[] spinDegrees)
        {
            float[] steer = { frontLeftSteer, frontRightSteer, rearLeftSteer, rearRightSteer };

            for (int i = 0; i < 4; i++)
            {
                Transform wheel = _wheels[i];
                if (wheel == null) continue;
                float spin = spinDegrees != null && i < spinDegrees.Length ? spinDegrees[i] : 0f;
                Quaternion roll = Quaternion.AngleAxis(spin,
                    _spinAroundLocalZ ? Vector3.forward : Vector3.right);
                Quaternion localSteer = SteerInWheelSpace(wheel, steer[i]);
                wheel.localRotation = localSteer * _baseRotations[i] * roll;
            }

            if (_interiorWheel != null)
            {
                float averageSteer = (frontLeftSteer + frontRightSteer) * 0.5f;
                _interiorWheel.localRotation = _interiorBaseRotation *
                    Quaternion.AngleAxis(averageSteer, Vector3.forward);
            }
        }
    }
}
