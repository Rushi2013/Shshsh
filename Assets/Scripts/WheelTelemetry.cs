using UnityEngine;

namespace CityDrive
{
    /// <summary>
    /// Read-only per-wheel physics state published by ArcadeCarController for the
    /// effects, audio and HUD systems. All values come from the real suspension ray
    /// and tyre solver, never from button presses or scripted animations.
    /// </summary>
    public struct WheelTelemetry
    {
        public bool Grounded;
        public bool Driven;
        public Vector3 ContactPoint;
        public Vector3 ContactNormal;
        public float Load;
        public float SpinRadPerSecond;
        public float GroundSpeed;
        public float SlipRatio;
        public float LateralSlip;
        public float LongitudinalSlip;
        public float BurnoutSlip;

        public float TotalSlip => Mathf.Clamp01(
            Mathf.Abs(SlipRatio) * 0.6f + Mathf.Abs(LateralSlip) * 0.8f + Mathf.Abs(LongitudinalSlip) * 0.4f);
    }
}
