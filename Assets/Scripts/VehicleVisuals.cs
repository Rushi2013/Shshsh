using UnityEngine;

namespace CityDrive
{
    /// <summary>
    /// Keeps the imported sedan wheel meshes in sync with the simulated wheels and
    /// rotates the car's ORIGINAL steering-wheel mesh if one is present. No primitive
    /// interior wheel is created: the supplied 330i GLB has generic interior body
    /// meshes only, so an overlapping fake wheel is deliberately not spawned.
    /// </summary>
    public sealed class VehicleVisuals : MonoBehaviour
    {
        private readonly Transform[] _wheels = new Transform[4]; // 0=FL, 1=FR, 2=RL, 3=RR
        private readonly Quaternion[] _baseRotations = new Quaternion[4];
        private bool _spinAroundLocalZ;

        private Transform _originalInteriorWheel;
        private Quaternion _originalInteriorBaseRotation;
        private Vector3 _originalInteriorAxis = Vector3.forward;

        public bool HasOriginalInteriorWheel => _originalInteriorWheel != null;

        public void FindWheels(Transform modelRoot)
        {
            if (modelRoot == null) return;
            foreach (Transform t in modelRoot.GetComponentsInChildren<Transform>(true))
            {
                int index = WheelIndex(t.name);
                if (index < 0) continue;
                Renderer candidateRenderer = t.GetComponent<Renderer>();
                Renderer currentRenderer = _wheels[index] != null ? _wheels[index].GetComponent<Renderer>() : null;
                if (_wheels[index] != null && (currentRenderer != null || candidateRenderer == null)) continue;
                _wheels[index] = t;
                _baseRotations[index] = t.localRotation;
                if (t.name.StartsWith("polySurface")) _spinAroundLocalZ = true;
            }
        }

        /// <summary>
        /// Looks only for a steering-wheel mesh that belongs to the imported model.
        /// The manual fallback wheel was removed; nothing is created here.
        /// </summary>
        public void FindOriginalInteriorWheel(Transform modelRoot)
        {
            _originalInteriorWheel = null;
            if (modelRoot == null) return;

            foreach (Transform t in modelRoot.GetComponentsInChildren<Transform>(true))
            {
                string name = t.name.ToLowerInvariant();
                if (!name.Contains("steer") || name.Contains("logo")) continue;
                Renderer renderer = t.GetComponent<Renderer>();
                if (renderer == null) continue;
                _originalInteriorWheel = t;
                _originalInteriorBaseRotation = t.localRotation;
                _originalInteriorAxis = ClosestLocalAxis(t, modelRoot.forward);
                Debug.Log("[CityDrive] Original steering wheel synced: " + t.name);
                return;
            }
        }

        private static Vector3 ClosestLocalAxis(Transform node, Vector3 carForward)
        {
            Transform parent = node.parent;
            Vector3 localForward = parent != null ? parent.InverseTransformDirection(carForward) : carForward;
            float ax = Mathf.Abs(localForward.x);
            float ay = Mathf.Abs(localForward.y);
            float az = Mathf.Abs(localForward.z);
            if (ax >= ay && ax >= az) return new Vector3(Mathf.Sign(localForward.x), 0f, 0f);
            if (ay >= az) return new Vector3(0f, Mathf.Sign(localForward.y), 0f);
            return new Vector3(0f, 0f, Mathf.Sign(localForward.z));
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
            }

            // User-supplied GLBs use names such as wheel-fl-lr, wheelFR_3_14 or
            // bone_wheel_FR_rotation. Ignore steering-wheel nodes explicitly.
            string lower = name.ToLowerInvariant();
            if (lower.Contains("steering")) return -1;
            int at = lower.IndexOf("wheel");
            if (at < 0) at = lower.IndexOf("tire");
            if (at < 0) at = lower.IndexOf("tyre");
            if (at < 0) return -1;
            string tail = lower.Substring(at);
            if (tail.Contains("fl")) return 0;
            if (tail.Contains("fr")) return 1;
            if (tail.Contains("rl")) return 2;
            if (tail.Contains("rr")) return 3;
            return -1;
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
                wheel.localRotation = SteerInWheelSpace(wheel, steer[i]) * _baseRotations[i] * roll;
            }

            if (_originalInteriorWheel != null)
            {
                float averageSteer = (frontLeftSteer + frontRightSteer) * 0.5f;
                _originalInteriorWheel.localRotation = _originalInteriorBaseRotation *
                    Quaternion.AngleAxis(averageSteer, _originalInteriorAxis);
            }
        }
    }
}
