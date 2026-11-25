using UnityEngine;

namespace CityDrive
{
    /// <summary>Low-cost headlight/taillight rig. Brake lights brighten from real brake input.</summary>
    public sealed class VehicleLights : MonoBehaviour
    {
        public ArcadeCarController Car;
        public Light[] Headlights;
        public Light[] Taillights;

        [SerializeField] private float headlightIntensity = 3.2f;
        [SerializeField] private float tailIdleIntensity = 0.7f;
        [SerializeField] private float tailBrakeIntensity = 3.4f;

        private void Update()
        {
            if (Car == null) return;
            float brake = Mathf.Clamp01(Car.BrakeInput);
            if (Taillights != null)
            {
                float intensity = Mathf.Lerp(tailIdleIntensity, tailBrakeIntensity, brake);
                for (int i = 0; i < Taillights.Length; i++)
                    if (Taillights[i] != null) Taillights[i].intensity = intensity;
            }
        }

        public void SetActive(bool on)
        {
            if (Headlights != null)
                foreach (Light light in Headlights) if (light != null) light.enabled = on;
            if (Taillights != null)
                foreach (Light light in Taillights) if (light != null) light.enabled = on;
        }

        public static VehicleLights Create(Transform car, ArcadeCarController controller)
        {
            VehicleLights rig = car.gameObject.AddComponent<VehicleLights>();
            rig.Car = controller;
            rig.Headlights = new Light[2];
            rig.Taillights = new Light[2];

            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -0.56f : 0.56f;
                GameObject head = new GameObject("Headlight " + i, typeof(Light));
                head.transform.SetParent(car, false);
                head.transform.localPosition = new Vector3(x, 0.18f, 1.98f);
                head.transform.localRotation = Quaternion.identity;
                Light spot = head.GetComponent<Light>();
                spot.type = LightType.Spot;
                spot.color = new Color(1f, 0.96f, 0.86f);
                spot.intensity = rig.headlightIntensity;
                spot.range = 42f;
                spot.spotAngle = 52f;
                spot.shadows = LightShadows.None;
                rig.Headlights[i] = spot;

                GameObject tail = new GameObject("Taillight " + i, typeof(Light));
                tail.transform.SetParent(car, false);
                tail.transform.localPosition = new Vector3(x, 0.22f, -2.02f);
                Light point = tail.GetComponent<Light>();
                point.type = LightType.Point;
                point.color = new Color(1f, 0.05f, 0.03f);
                point.intensity = rig.tailIdleIntensity;
                point.range = 6f;
                point.shadows = LightShadows.None;
                rig.Taillights[i] = point;
            }
            return rig;
        }
    }
}
