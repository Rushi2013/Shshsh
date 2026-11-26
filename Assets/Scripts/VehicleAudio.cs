using System;
using UnityEngine;

namespace CityDrive
{
    /// <summary>
    /// Layered, physics-driven vehicle audio. All clips are generated procedurally at
    /// runtime (no copyrighted BMW recordings, no external downloads). Engine pitch
    /// follows real RPM/wheelspin, load follows throttle, turbo follows load+RPM,
    /// tyre layers follow measured wheel slip, and interior view muffles the sources.
    /// </summary>
    public sealed class VehicleAudio : MonoBehaviour
    {
        public ArcadeCarController Car;
        public ChaseCamera CameraRig;

        [Header("Levels")]
        [SerializeField] private float masterVolume = 0.85f;
        [SerializeField] private float engineVolume = 0.42f;
        [SerializeField] private float loadVolume = 0.30f;
        [SerializeField] private float turboVolume = 0.20f;
        [SerializeField] private float tyreVolume = 0.34f;

        private AudioSource _engine;
        private AudioSource _load;
        private AudioSource _turbo;
        private AudioSource _tyre;
        private AudioSource _squeal;
        private AudioSource _wheelspin;
        private AudioSource _brakeSqueal;
        private AudioSource _wind;
        private AudioSource _oneShot;
        private AudioLowPassFilter _engineFilter;
        private AudioLowPassFilter _loadFilter;
        private AudioLowPassFilter _turboFilter;

        private bool _active;
        private float _perspective;
        private float _lastSlip;
        private float _lastSqueal;
        private float _lastWheelspin;
        private float _lastBrakeSlip;
        private float _boost;

        private void Start()
        {
            if (Car == null) Car = GetComponent<ArcadeCarController>();
            if (Car != null)
            {
                Car.GearChanged += OnGearChanged;
                Car.Collided += OnCollided;
                Car.HornRequested += Honk;
            }

            AudioClip engineLoop = ProceduralAudio.EngineLoop();
            AudioClip loadLoop = ProceduralAudio.LoadLoop();
            AudioClip turboLoop = ProceduralAudio.TurboLoop();
            AudioClip tyreLoop = ProceduralAudio.NoiseLoop("Tyre Scrub", 0.55f, 0.42f);
            AudioClip squealLoop = ProceduralAudio.SquealLoop();
            AudioClip wheelspinLoop = ProceduralAudio.WheelspinLoop();
            AudioClip brakeLoop = ProceduralAudio.BrakeSquealLoop();
            AudioClip windLoop = ProceduralAudio.NoiseLoop("Road Wind", 0.35f, 0.75f);

            _engine = MakeLoop("Engine", engineLoop, engineVolume);
            _load = MakeLoop("Engine Load", loadLoop, loadVolume);
            _turbo = MakeLoop("Turbo", turboLoop, turboVolume);
            _tyre = MakeLoop("Tyre Scrub", tyreLoop, tyreVolume);
            _squeal = MakeLoop("Tyre Squeal", squealLoop, tyreVolume * 0.75f);
            _wheelspin = MakeLoop("Wheelspin", wheelspinLoop, tyreVolume * 0.85f);
            _brakeSqueal = MakeLoop("Brake Squeal", brakeLoop, tyreVolume * 0.55f);
            _wind = MakeLoop("Road Noise", windLoop, 0.10f);
            _oneShot = gameObject.AddComponent<AudioSource>();
            _oneShot.playOnAwake = false;
            _oneShot.loop = false;
            _oneShot.spatialBlend = 0f;
            _oneShot.volume = masterVolume * 0.8f;

            _engineFilter = _engine.gameObject.AddComponent<AudioLowPassFilter>();
            _loadFilter = _load.gameObject.AddComponent<AudioLowPassFilter>();
            _turboFilter = _turbo.gameObject.AddComponent<AudioLowPassFilter>();
        }

        private AudioSource MakeLoop(string name, AudioClip clip, float volume)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
            source.priority = name == "Engine" ? 0 : 64;
            return source;
        }

        private void OnDestroy()
        {
            if (Car == null) return;
            Car.GearChanged -= OnGearChanged;
            Car.Collided -= OnCollided;
            Car.HornRequested -= Honk;
        }

        /// <summary>Menu-controlled SFX level; applied by the next Update tick.</summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            if (_oneShot != null) _oneShot.volume = masterVolume * 0.8f;
        }

        /// <summary>Enables the engine soundscape when gameplay begins.</summary>
        public void SetActive(bool active)
        {
            _active = active;
            if (_engine == null) return;

            if (active)
            {
                PlayLoop(_engine);
                PlayLoop(_load);
                PlayLoop(_turbo);
                PlayLoop(_tyre);
                PlayLoop(_squeal);
                PlayLoop(_wheelspin);
                PlayLoop(_brakeSqueal);
                PlayLoop(_wind);
                PlayOneShot(ProceduralAudio.Startup(), 0.55f);
            }
            else
            {
                PlayOneShot(ProceduralAudio.Shutdown(), 0.45f);
                StopAllLoops();
            }
        }

        private void PlayLoop(AudioSource source)
        {
            if (source != null && !source.isPlaying) source.Play();
        }

        private void StopAllLoops()
        {
            AudioSource[] sources = { _engine, _load, _turbo, _tyre, _squeal, _wheelspin, _brakeSqueal, _wind };
            foreach (AudioSource source in sources)
            {
                if (source != null) source.Stop();
            }
        }

        private void Update()
        {
            if (!_active || Car == null || _engine == null) return;
            float dt = Mathf.Min(0.1f, Time.unscaledDeltaTime);

            float rpmNorm = Mathf.InverseLerp(850f, 7000f, Car.EngineRpm);
            float speedNorm = Mathf.Clamp01(Car.SpeedKph / 240f);
            float load = Mathf.Clamp01(Car.EngineLoad);
            float slip = 0f;
            float lateral = 0f;
            float wheelspin = 0f;
            float brakeSlip = 0f;
            if (Car.Wheels != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    WheelTelemetry wheel = Car.Wheels[i];
                    slip = Mathf.Max(slip, wheel.TotalSlip);
                    lateral = Mathf.Max(lateral, Mathf.Abs(wheel.LateralSlip));
                    if (wheel.Grounded && wheel.Driven)
                        wheelspin = Mathf.Max(wheelspin,
                            Mathf.Max(wheel.BurnoutSlip, Mathf.Max(0f, wheel.SlipRatio)));
                    if (wheel.Grounded && Car.BrakeInput > 0.1f)
                        brakeSlip = Mathf.Max(brakeSlip, Mathf.Abs(wheel.SlipRatio));
                }
            }
            _lastSlip = Mathf.Lerp(_lastSlip, slip, dt * 5f);
            _lastSqueal = Mathf.Lerp(_lastSqueal, lateral, dt * 6f);
            _lastWheelspin = Mathf.Lerp(_lastWheelspin, wheelspin, dt * 6f);
            _lastBrakeSlip = Mathf.Lerp(_lastBrakeSlip, brakeSlip, dt * 5f);

            float burnoutBoost = Car.IsBurnout ? 0.3f : 0f;
            float loadTarget = Mathf.Clamp01(load + burnoutBoost + rpmNorm * 0.25f);
            _boost = Mathf.Lerp(_boost, loadTarget, dt * (loadTarget > _boost ? 3.5f : 1.8f));

            _engine.pitch = Mathf.Lerp(0.62f, 2.15f, rpmNorm);
            _engine.volume = masterVolume * engineVolume * Mathf.Lerp(0.55f, 1f, rpmNorm);
            _load.pitch = Mathf.Lerp(0.7f, 2.0f, rpmNorm);
            _load.volume = masterVolume * loadVolume * _boost;
            _turbo.pitch = Mathf.Lerp(0.75f, 2.2f, rpmNorm * 0.6f + _boost * 0.4f);
            _turbo.volume = masterVolume * turboVolume * Mathf.Clamp01(_boost * (0.3f + rpmNorm));
            _tyre.volume = masterVolume * tyreVolume * Mathf.Clamp01((_lastSlip - 0.12f) * 1.1f);
            _tyre.pitch = Mathf.Lerp(0.8f, 1.35f, speedNorm);
            _squeal.volume = masterVolume * tyreVolume * Mathf.Clamp01((_lastSqueal - 0.12f) * 1.6f);
            _squeal.pitch = Mathf.Lerp(1.05f, 1.55f, Mathf.Clamp01(_lastSqueal));
            _wheelspin.volume = masterVolume * tyreVolume * 0.85f *
                                Mathf.Clamp01((_lastWheelspin - 0.10f) * 1.5f);
            _wheelspin.pitch = Mathf.Lerp(0.72f, 1.25f, Mathf.Clamp01(_lastWheelspin));
            _brakeSqueal.volume = masterVolume * tyreVolume * 0.55f *
                                  Mathf.Clamp01((_lastBrakeSlip - 0.18f) * 1.4f) *
                                  Mathf.Clamp01(Car.BrakeInput);
            _brakeSqueal.pitch = Mathf.Lerp(1.15f, 1.75f, speedNorm);
            _wind.volume = masterVolume * 0.10f * speedNorm;
            _wind.pitch = Mathf.Lerp(0.8f, 1.6f, speedNorm);

            // Interior view is muffled; exterior is open. One rig, no camera teleport.
            bool interior = CameraRig != null && CameraRig.ActiveMode == ChaseCamera.CameraMode.Interior;
            _perspective = Mathf.Lerp(_perspective, interior ? 1f : 0f, dt * 5f);
            float cutoff = Mathf.Lerp(9000f, 720f, _perspective);
            if (_engineFilter != null) _engineFilter.cutoffFrequency = cutoff;
            if (_loadFilter != null) _loadFilter.cutoffFrequency = cutoff * 1.2f;
            if (_turboFilter != null) _turboFilter.cutoffFrequency = cutoff * 1.5f;

            // Lift-off: brief load drop is already in _boost; engine layer stays audible.
            if (Car.ThrottleInput < 0.05f && speedNorm > 0.05f)
                _load.volume *= 0.55f;
        }

        private void OnGearChanged(int gear)
        {
            if (!_active) return;
            PlayOneShot(ProceduralAudio.GearShift(), 0.34f);
        }

        private void OnCollided(float intensity)
        {
            if (!_active) return;
            PlayOneShot(ProceduralAudio.Impact(), Mathf.Clamp01(intensity / 12f) * 0.7f);
        }

        public void Honk()
        {
            PlayOneShot(ProceduralAudio.Horn(), 0.55f);
        }

        private void PlayOneShot(AudioClip clip, float volume)
        {
            if (_oneShot == null || clip == null) return;
            _oneShot.PlayOneShot(clip, Mathf.Clamp01(volume) * masterVolume);
        }
    }

    /// <summary>Deterministic, allocation-light procedural waveforms for mobile.</summary>
    internal static class ProceduralAudio
    {
        private const int Rate = 22050;

        public static AudioClip EngineLoop()
        {
            return Clip("i6 Engine Layer", 0.5f, (t, i) =>
            {
                float p = 2f * Mathf.PI * 90f * t;
                return 0.40f * Mathf.Sin(p) + 0.26f * Mathf.Sin(2f * p) +
                       0.17f * Mathf.Sin(3f * p) + 0.08f * Mathf.Sin(4f * p) +
                       0.05f * Noise(i);
            });
        }

        public static AudioClip LoadLoop()
        {
            return Clip("Engine Load", 0.5f, (t, i) =>
            {
                float p = 2f * Mathf.PI * 180f * t;
                return 0.16f * Mathf.Sin(p) + 0.09f * Mathf.Sin(2.5f * p) +
                       0.12f * Noise(i) + 0.06f * Mathf.Sin(5f * p);
            });
        }

        public static AudioClip TurboLoop()
        {
            return Clip("Turbo Spool", 0.5f, (t, i) =>
            {
                float whistle = Mathf.Sin(2f * Mathf.PI * 1400f * t);
                float mod = 0.72f + 0.28f * Mathf.Sin(2f * Mathf.PI * 6f * t);
                return whistle * mod * 0.24f + Noise(i) * 0.05f;
            });
        }

        public static AudioClip SquealLoop()
        {
            return Clip("Tyre Squeal", 0.4f, (t, i) =>
            {
                float p = 2f * Mathf.PI * 900f * t;
                return 0.18f * Mathf.Sin(p) + 0.12f * Mathf.Sin(p * 1.31f) + Noise(i) * 0.09f;
            });
        }

        public static AudioClip WheelspinLoop()
        {
            return Clip("Wheelspin", 0.5f, (t, i) =>
            {
                float rumble = Mathf.Sin(2f * Mathf.PI * 72f * t) * 0.16f;
                float grind = Mathf.Sin(2f * Mathf.PI * 310f * t) * 0.05f;
                return rumble + grind + Noise(i) * 0.24f;
            });
        }

        public static AudioClip BrakeSquealLoop()
        {
            return Clip("Brake Squeal", 0.4f, (t, i) =>
            {
                float p = 2f * Mathf.PI * 1450f * t;
                return Mathf.Sin(p) * 0.12f + Mathf.Sin(p * 1.07f) * 0.07f + Noise(i) * 0.05f;
            });
        }

        public static AudioClip NoiseLoop(string name, float seconds, float brightness)
        {
            return Clip(name, seconds, (t, i) =>
            {
                float edge = Mathf.Min(1f, Mathf.Min(t, seconds - t) * 20f);
                return Noise(i) * brightness * edge;
            });
        }

        public static AudioClip Startup()
        {
            return Clip("Engine Start", 0.85f, (t, i) =>
            {
                float ramp = Mathf.SmoothStep(0f, 1f, t / 0.85f);
                float f = Mathf.Lerp(35f, 95f, ramp);
                float p = 2f * Mathf.PI * f * t;
                return (Mathf.Sin(p) * 0.35f + Noise(i) * 0.18f) * (0.35f + ramp * 0.65f);
            });
        }

        public static AudioClip Shutdown()
        {
            return Clip("Engine Stop", 0.6f, (t, i) =>
            {
                float decay = 1f - t / 0.6f;
                float p = 2f * Mathf.PI * Mathf.Lerp(95f, 32f, t / 0.6f) * t;
                return (Mathf.Sin(p) * 0.3f + Noise(i) * 0.08f) * decay;
            });
        }

        public static AudioClip GearShift()
        {
            return Clip("Gear Shift", 0.16f, (t, i) =>
                Noise(i) * 0.32f * Mathf.Exp(-t * 18f) + Mathf.Sin(2f * Mathf.PI * 260f * t) * 0.08f * Mathf.Exp(-t * 12f));
        }

        public static AudioClip Horn()
        {
            return Clip("Horn", 0.65f, (t, i) =>
            {
                float env = Mathf.Min(1f, t * 25f) * Mathf.Min(1f, (0.65f - t) * 12f);
                return (Mathf.Sin(2f * Mathf.PI * 520f * t) +
                        Mathf.Sin(2f * Mathf.PI * 660f * t)) * 0.18f * env;
            });
        }

        public static AudioClip Impact()
        {
            return Clip("Impact", 0.28f, (t, i) =>
                (Noise(i) * 0.5f + Mathf.Sin(2f * Mathf.PI * 85f * t) * 0.35f) * Mathf.Exp(-t * 12f));
        }

        public static AudioClip LoadingAmbient()
        {
            return Clip("Loading Ambient", 2f, (t, i) =>
            {
                float a = Mathf.Sin(2f * Mathf.PI * 110f * t);
                float b = Mathf.Sin(2f * Mathf.PI * 165f * t);
                float pad = Mathf.Sin(2f * Mathf.PI * 0.5f * t) * 0.5f + 0.5f;
                return (a * 0.055f + b * 0.035f + Noise(i) * 0.012f) * (0.6f + pad * 0.4f);
            });
        }

        private static float Noise(int i)
        {
            uint x = (uint)(i * 1664525 + 1013904223);
            x ^= x >> 13;
            x *= 1274126177;
            x ^= x >> 16;
            return (x / (float)uint.MaxValue) * 2f - 1f;
        }

        private static AudioClip Clip(string name, float seconds, Func<float, int, float> generator)
        {
            int length = Mathf.Max(16, Mathf.CeilToInt(seconds * Rate));
            float[] samples = new float[length];
            for (int i = 0; i < length; i++) samples[i] = Mathf.Clamp(generator(i / (float)Rate, i), -1f, 1f);
            AudioClip clip = AudioClip.Create(name, length, 1, Rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
