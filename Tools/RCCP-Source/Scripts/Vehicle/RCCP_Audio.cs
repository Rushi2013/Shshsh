//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright © 2014 - 2024 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;

/// <summary>
/// Audio system for engine, brake, crashes, transmission, and other stuff.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Addons/RCCP Audio")]
public class RCCP_Audio : RCCP_Component {

    /// <summary>
    /// Connected audiomixer group.
    /// </summary>
    public AudioMixerGroup audioMixer;

    [System.Serializable]
    public class EngineSound {

        [HideInInspector] public AudioSource audioSourceOn;
        public AudioClip audioClipOn;

        [HideInInspector] public AudioSource audioSourceOff;
        public AudioClip audioClipOff;
        public Vector3 localPosition = new Vector3(0f, 0f, 1.5f);

        [Min(0f)] public float minPitch = .1f;
        [Min(0f)] public float maxPitch = 1f;

        [Min(0f)] public float minRPM = 600f;
        [Min(0f)] public float maxRPM = 8000f;

        [Min(0f)] public float minDistance = 10f;
        [Min(0f)] public float maxDistance = 200f;

        [Min(0f)] public float minVolume = 0f;
        [Min(0f)] public float maxVolume = 1f;

    }

    /// <summary>
    /// Engine sounds.
    /// </summary>
    public EngineSound[] engineSounds = new EngineSound[4];

    [System.Serializable]
    public class EngineStart {

        [HideInInspector] public AudioSource audioSource;
        public AudioClip audioClips;
        public Vector3 localPosition = new Vector3(0f, 0f, 1.5f);

        [Min(0f)] public float minDistance = 10f;
        [Min(0f)] public float maxDistance = 100f;

        [Min(0f)] public float maxVolume = 1f;

    }

    /// <summary>
    /// Engine start sound.
    /// </summary>
    public EngineStart engineStart = new EngineStart();

    [System.Serializable]
    public class GearboxSound {

        [HideInInspector] public AudioSource audioSource;
        public AudioClip[] audioClips;
        public Vector3 localPosition = new Vector3(0f, 0f, 0f);

        [Min(0f)] public float minDistance = 1f;
        [Min(0f)] public float maxDistance = 10f;

        [Min(0f)] public float maxVolume = 1f;

    }

    /// <summary>
    /// Gearbox sounds.
    /// </summary>
    public GearboxSound gearboxSound = new GearboxSound();
    private int lastGear = 0;

    [System.Serializable]
    public class CrashSound {

        [HideInInspector] public AudioSource audioSource;
        public AudioClip[] audioClips;
        public Vector3 localPosition = new Vector3(0f, 0f, 0f);

        [Min(0f)] public float minDistance = 10f;
        [Min(0f)] public float maxDistance = 100f;

        [Min(0f)] public float maxVolume = 1f;

    }

    /// <summary>
    /// Crash sounds.
    /// </summary>
    public CrashSound crashSound = new CrashSound();

    [System.Serializable]
    public class ReverseSound {

        [HideInInspector] public AudioSource audioSource;
        public AudioClip audioClips;
        public Vector3 localPosition = new Vector3(0f, 0f, 0f);

        [Min(0f)] public float minDistance = 10f;
        [Min(0f)] public float maxDistance = 100f;

        [Min(0f)] public float minPitch = .8f;
        [Min(0f)] public float maxPitch = 1f;

        [Min(0f)] public float maxVolume = 1f;

    }

    /// <summary>
    /// Gearbox reverse sounds.
    /// </summary>
    public ReverseSound reverseSound = new ReverseSound();

    [System.Serializable]
    public class WindSound {

        [HideInInspector] public AudioSource audioSource;
        public AudioClip audioClips;
        public Vector3 localPosition = new Vector3(0f, 0f, 0f);

        [Min(0f)] public float minDistance = 10f;
        [Min(0f)] public float maxDistance = 100f;

        [Min(0f)] public float maxVolume = .1f;

    }

    /// <summary>
    /// Wind sounds.
    /// </summary>
    public WindSound windSound = new WindSound();

    [System.Serializable]
    public class BrakeSound {

        [HideInInspector] public AudioSource audioSource;
        public AudioClip audioClips;
        public Vector3 localPosition = new Vector3(0f, 0f, 0f);

        [Min(0f)] public float minDistance = 10f;
        [Min(0f)] public float maxDistance = 100f;

        [Min(0f)] public float maxVolume = .25f;

    }

    /// <summary>
    /// Brake sounds.
    /// </summary>
    public BrakeSound brakeSound = new BrakeSound();

    [System.Serializable]
    public class NosSound {

        [HideInInspector] public AudioSource audioSource;
        public AudioClip audioClips;
        public Vector3 localPosition = new Vector3(0f, 0f, 0f);

        [Min(0f)] public float minDistance = 10f;
        [Min(0f)] public float maxDistance = 100f;

        [Min(0f)] public float maxVolume = 1f;

    }

    /// <summary>
    /// Nos sounds.
    /// </summary>
    public NosSound nosSound = new NosSound();

    [System.Serializable]
    public class TurboSound {

        [HideInInspector] public AudioSource audioSource;
        public AudioClip audioClips;
        public Vector3 localPosition = new Vector3(0f, 0f, 1.5f);

        [Min(0f)] public float minDistance = 10f;
        [Min(0f)] public float maxDistance = 100f;

        [Min(0f)] public float maxVolume = 1f;

    }

    /// <summary>
    /// Turbo sounds.
    /// </summary>
    public TurboSound turboSound = new TurboSound();

    [System.Serializable]
    public class ExhaustFlameSound {

        [HideInInspector] public AudioSource audioSource;
        public AudioClip[] audioClips;
        public Vector3 localPosition = new Vector3(0f, -.5f, -2f);

        [Min(0f)] public float minDistance = 10f;
        [Min(0f)] public float maxDistance = 100f;

        [Min(0f)] public float maxVolume = 1f;

    }

    /// <summary>
    /// Exhaust flame sounds.
    /// </summary>
    public ExhaustFlameSound exhaustFlameSound = new ExhaustFlameSound();

    [System.Serializable]
    public class BlowSound {

        [HideInInspector] public AudioSource audioSource;
        public AudioClip[] audioClips;
        public Vector3 localPosition = new Vector3(0f, 0f, -1.5f);

        [Min(0f)] public float minDistance = 1f;
        [Min(0f)] public float maxDistance = 20f;

        [Min(0f)] public float maxVolume = .2f;

    }

    /// <summary>
    /// Turbo blow sounds.
    /// </summary>
    public BlowSound blowSound = new BlowSound();

    [System.Serializable]
    public class DeflateSound {

        [HideInInspector] public AudioSource audioSource;
        public AudioClip audioClips;
        public Vector3 localPosition = new Vector3(0f, 0f, 1f);

        [Min(0f)] public float minDistance = 10f;
        [Min(0f)] public float maxDistance = 20f;

        [Min(0f)] public float maxVolume = 1f;

    }

    /// <summary>
    /// Wheel deflate sounds.
    /// </summary>
    public DeflateSound wheelDeflateSound = new DeflateSound();

    [System.Serializable]
    public class InflateSound {

        [HideInInspector] public AudioSource audioSource;
        public AudioClip audioClips;
        public Vector3 localPosition = new Vector3(0f, 0f, 0f);

        [Min(0f)] public float minDistance = 10f;
        [Min(0f)] public float maxDistance = 100f;

        [Min(0f)] public float maxVolume = 1f;
        public bool lastInflate = true;

    }

    /// <summary>
    /// Wheel inflate sounds.
    /// </summary>
    public InflateSound wheelInflateSound = new InflateSound();

    [System.Serializable]
    public class FlatSound {

        [HideInInspector] public AudioSource audioSource;
        public AudioClip audioClips;
        public Vector3 localPosition = new Vector3(0f, 0f, 0f);

        [Min(0f)] public float minDistance = 10f;
        [Min(0f)] public float maxDistance = 100f;

        [Min(0f)] public float maxVolume = 1f;

    }

    /// <summary>
    /// Wheel flat sounds.
    /// </summary>
    public FlatSound wheelFlatSound;

    private AudioSource[] allAudioSources;
    private bool[] audioStatesBeforeDisabling;

    public override void Start() {

        base.Start();
        StartCoroutine(GetAllAudioSources());

    }

    public override void OnEnable() {

        base.OnEnable();

        if (allAudioSources == null)
            return;

        if (allAudioSources.Length < 1)
            return;

        if (allAudioSources.Length != audioStatesBeforeDisabling.Length)
            return;

        for (int i = 0; i < audioStatesBeforeDisabling.Length; i++) {

            if (allAudioSources[i] != null)
                allAudioSources[i].gameObject.SetActive(audioStatesBeforeDisabling[i]);

        }

    }

    public IEnumerator GetAllAudioSources() {

        yield return new WaitForSeconds(.1f);

        allAudioSources = GetComponentsInChildren<AudioSource>(true);

    }

    /// <summary>
    /// On collision.
    /// </summary>
    /// <param name="collision"></param>
    public void OnCollision(Collision collision) {

        if (crashSound != null && crashSound.audioClips.Length >= 1) {

            int randomClip = UnityEngine.Random.Range(0, crashSound.audioClips.Length);
            float volume = Mathf.InverseLerp(0f, 20000f, collision.impulse.magnitude);
            volume *= crashSound.maxVolume;

            crashSound.audioSource = RCCP_AudioSource.NewAudioSource(audioMixer, gameObject, crashSound.audioClips[randomClip].name, crashSound.minDistance, crashSound.maxDistance, volume, crashSound.audioClips[randomClip], false, true, true);
            crashSound.audioSource.transform.position = collision.GetContact(0).point;

        }

    }


    /// <summary>
    /// Engine sounds.
    /// </summary>
    private void Engine() {

        if (!CarController.Engine)
            return;

        if (engineStart != null && engineStart.audioClips != null) {

            if (!engineStart.audioSource) {

                engineStart.audioSource = RCCP_AudioSource.NewAudioSource(audioMixer, gameObject, engineStart.audioClips.name, engineStart.minDistance, engineStart.maxDistance, engineStart.maxVolume, engineStart.audioClips, false, false, false);
                engineStart.audioSource.transform.localPosition = engineStart.localPosition;

            }

            if (engineStart.audioSource && !engineStart.audioSource.isPlaying && CarController.Engine.engineStarting)
                engineStart.audioSource.Play();

        }

        if (engineSounds != null && engineSounds != null && engineSounds.Length >= 1) {

            for (int i = 0; i < engineSounds.Length; i++) {

                if (engineSounds[i] != null) {

                    if (!engineSounds[i].audioSourceOn && engineSounds[i].audioClipOn) {

                        engineSounds[i].audioSourceOn = RCCP_AudioSource.NewAudioSource(audioMixer, gameObject, engineSounds[i].audioClipOn.name, engineSounds[i].minDistance, engineSounds[i].maxDistance, 0f, engineSounds[i].audioClipOn, true, false, false);
                        engineSounds[i].audioSourceOn.transform.localPosition = engineSounds[i].localPosition;

                    }

                    if (engineSounds[i].audioSourceOn && !engineSounds[i].audioSourceOn.isPlaying)
                        engineSounds[i].audioSourceOn.Play();

                    if (!engineSounds[i].audioSourceOff && engineSounds[i].audioClipOff) {

                        engineSounds[i].audioSourceOff = RCCP_AudioSource.NewAudioSource(audioMixer, gameObject, engineSounds[i].audioClipOff.name, engineSounds[i].minDistance, engineSounds[i].maxDistance, 0f, engineSounds[i].audioClipOff, true, false, false);
                        engineSounds[i].audioSourceOff.transform.localPosition = engineSounds[i].localPosition;

                    }

                    if (engineSounds[i].audioSourceOff && !engineSounds[i].audioSourceOff.isPlaying)
                        engineSounds[i].audioSourceOff.Play();

                    if (engineSounds[i].audioSourceOn && engineSounds[i].audioSourceOff) {

                        float minPitchPoint = Mathf.Lerp(engineSounds[i].minPitch, engineSounds[i].maxPitch, (CarController.engineRPM) / (CarController.maxEngineRPM));
                        float maxPitchPoint = Mathf.Lerp(minPitchPoint, engineSounds[i].minPitch, (CarController.engineRPM) / (CarController.maxEngineRPM));

                        engineSounds[i].audioSourceOn.pitch = minPitchPoint;

                        float minVolPoint = Mathf.Lerp(engineSounds[i].minVolume, engineSounds[i].maxVolume, (CarController.engineRPM - engineSounds[i].minRPM) / (engineSounds[i].maxRPM - engineSounds[i].minRPM));
                        float maxVolPoint = Mathf.Lerp(minVolPoint, 0f, (CarController.engineRPM - engineSounds[i].maxRPM) / (engineSounds[i].maxRPM - engineSounds[i].minRPM));

                        if (CarController.engineRPM < engineSounds[i].maxRPM)
                            engineSounds[i].audioSourceOn.volume = minVolPoint;
                        else
                            engineSounds[i].audioSourceOn.volume = maxVolPoint;

                        engineSounds[i].audioSourceOff.pitch = engineSounds[i].audioSourceOn.pitch;
                        engineSounds[i].audioSourceOff.volume = engineSounds[i].audioSourceOn.volume;

                        engineSounds[i].audioSourceOn.volume *= CarController.fuelInput_V;
                        engineSounds[i].audioSourceOff.volume *= (1f - CarController.fuelInput_V);

                    }

                }

            }

        }

        if (turboSound != null && turboSound.audioClips != null) {

            if (!turboSound.audioSource) {

                turboSound.audioSource = RCCP_AudioSource.NewAudioSource(audioMixer, gameObject, turboSound.audioClips.name, turboSound.minDistance, turboSound.maxDistance, 0f, turboSound.audioClips, true, true, false);
                turboSound.audioSource.transform.localPosition = turboSound.localPosition;

            } else {

                turboSound.audioSource.volume = Mathf.Lerp(0f, turboSound.maxVolume, CarController.Engine.turboChargePsi / CarController.Engine.maxTurboChargePsi);

            }

        }

        if (blowSound != null && blowSound.audioClips != null && blowSound.audioClips.Length >= 1f) {

            if (!blowSound.audioSource) {

                blowSound.audioSource = RCCP_AudioSource.NewAudioSource(audioMixer, gameObject, blowSound.audioClips[0].name, blowSound.minDistance, blowSound.maxDistance, blowSound.maxVolume, blowSound.audioClips[0], false, false, false);
                blowSound.audioSource.transform.localPosition = blowSound.localPosition;

            } else {

                if (CarController.Engine.turboBlowOut && !blowSound.audioSource.isPlaying) {

                    blowSound.audioSource.clip = blowSound.audioClips[UnityEngine.Random.Range(0, blowSound.audioClips.Length)];
                    blowSound.audioSource.Play();

                } else if (!blowSound.audioSource.isPlaying) {

                    blowSound.audioSource.Stop();

                }

            }

        }

    }

    /// <summary>
    /// Gearbox sounds.
    /// </summary>
    private void Gearbox() {

        if (!CarController.Gearbox)
            return;

        if (gearboxSound != null && gearboxSound.audioClips != null && gearboxSound.audioClips.Length >= 1) {

            if (lastGear != CarController.currentGear) {

                int randomClip = UnityEngine.Random.Range(0, gearboxSound.audioClips.Length);

                gearboxSound.audioSource = RCCP_AudioSource.NewAudioSource(audioMixer, gameObject, gearboxSound.audioClips[randomClip].name, gearboxSound.minDistance, gearboxSound.maxDistance, gearboxSound.maxVolume, gearboxSound.audioClips[randomClip], false, true, true);
                gearboxSound.audioSource.transform.localPosition = gearboxSound.localPosition;

            }

            lastGear = CarController.currentGear;

        }

        if (reverseSound != null && reverseSound.audioClips != null) {

            if (!reverseSound.audioSource) {

                reverseSound.audioSource = RCCP_AudioSource.NewAudioSource(audioMixer, gameObject, reverseSound.audioClips.name, reverseSound.minDistance, reverseSound.maxDistance, 0f, reverseSound.audioClips, true, true, false);
                reverseSound.audioSource.transform.localPosition = reverseSound.localPosition;

            } else {

                reverseSound.audioSource.volume = Mathf.InverseLerp(0f, -40f, CarController.speed) * reverseSound.maxVolume;
                reverseSound.audioSource.pitch = Mathf.Lerp(reverseSound.minPitch, reverseSound.maxPitch, Mathf.InverseLerp(0f, -40f, CarController.speed));

            }

        }

    }

    /// <summary>
    /// Wheel sounds.
    /// </summary>
    private void Wheel() {

        if (CarController.AllWheelColliders != null && CarController.AllWheelColliders.Length > 0) {

            if (brakeSound != null && brakeSound.audioClips != null && brakeSound.audioClips.length > 0) {

                if (!brakeSound.audioSource) {

                    brakeSound.audioSource = RCCP_AudioSource.NewAudioSource(audioMixer, gameObject, brakeSound.audioClips.name, brakeSound.minDistance, brakeSound.maxDistance, 0f, brakeSound.audioClips, true, true, false);
                    brakeSound.audioSource.transform.localPosition = brakeSound.localPosition;

                }

                RCCP_Axle frontAxle = CarController.FrontAxle;

                if (frontAxle != null && frontAxle.leftWheelCollider && frontAxle.rightWheelCollider)
                    brakeSound.audioSource.volume = Mathf.Lerp(0f, brakeSound.maxVolume, Mathf.Clamp01((frontAxle.leftWheelCollider.WheelCollider.brakeTorque + frontAxle.rightWheelCollider.WheelCollider.brakeTorque) / (frontAxle.maxBrakeTorque * 2f)) * Mathf.Lerp(0f, 1f, ((frontAxle.leftWheelCollider.WheelCollider.rpm + frontAxle.rightWheelCollider.WheelCollider.rpm) / 2f) / 50f));

            }

            if (wheelFlatSound != null && wheelFlatSound.audioClips != null) {

                bool deflated = false;

                for (int i = 0; i < CarController.AllWheelColliders.Length; i++) {

                    if (CarController.AllWheelColliders[i].deflated)
                        deflated = true;

                }

                if (deflated) {

                    if (wheelFlatSound.audioSource == null) {

                        wheelFlatSound.audioSource = RCCP_AudioSource.NewAudioSource(audioMixer, gameObject, wheelFlatSound.audioClips.name, 1f, 15f, 0f, wheelFlatSound.audioClips, true, false, false);
                        wheelFlatSound.audioSource.transform.localPosition = wheelFlatSound.localPosition;

                    } else {

                        wheelFlatSound.audioSource.volume = Mathf.Clamp01(Mathf.Abs(CarController.tractionWheelRPM2EngineRPM * .001f));
                        wheelFlatSound.audioSource.volume *= CarController.IsGrounded ? 1f : 0f;

                        if (!wheelFlatSound.audioSource.isPlaying)
                            wheelFlatSound.audioSource.Play();

                    }

                } else {

                    if (wheelFlatSound.audioSource != null && wheelFlatSound.audioSource.isPlaying)
                        wheelFlatSound.audioSource.Stop();

                }

            }

        }

    }

    /// <summary>
    /// Exhaust Sounds.
    /// </summary>
    private void Exhaust() {

        if (CarController.OtherAddonsManager) {

            if (CarController.OtherAddonsManager.Nos != null) {

                if (nosSound != null && nosSound.audioClips != null) {

                    if (!nosSound.audioSource) {

                        nosSound.audioSource = RCCP_AudioSource.NewAudioSource(audioMixer, gameObject, nosSound.audioClips.name, nosSound.minDistance, nosSound.maxDistance, 0f, nosSound.audioClips, true, true, false);
                        nosSound.audioSource.transform.localPosition = nosSound.localPosition;

                    } else {

                        nosSound.audioSource.volume = (CarController.OtherAddonsManager.Nos.nosInUse ? 1f : 0f) * nosSound.maxVolume;

                    }

                }

            }

            if (CarController.OtherAddonsManager.Exhausts != null && CarController.OtherAddonsManager.Exhausts.Exhaust.Length >= 1 && CarController.OtherAddonsManager.Exhausts.Exhaust[0] != null) {

                if (exhaustFlameSound != null && exhaustFlameSound.audioClips != null && exhaustFlameSound.audioClips.Length > 0) {

                    if (!exhaustFlameSound.audioSource) {

                        AudioClip randomExhaustClip = exhaustFlameSound.audioClips[Random.Range(0, exhaustFlameSound.audioClips.Length)];

                        exhaustFlameSound.audioSource = RCCP_AudioSource.NewAudioSource(audioMixer, gameObject, randomExhaustClip.name, exhaustFlameSound.minDistance, exhaustFlameSound.maxDistance, 0f, randomExhaustClip, true, true, false);
                        exhaustFlameSound.audioSource.transform.localPosition = exhaustFlameSound.localPosition;

                    } else {

                        if (!exhaustFlameSound.audioSource.isPlaying && CarController.OtherAddonsManager.Exhausts.Exhaust[0].popping) {

                            exhaustFlameSound.audioSource.clip = exhaustFlameSound.audioClips[Random.Range(0, exhaustFlameSound.audioClips.Length)];
                            exhaustFlameSound.audioSource.volume = exhaustFlameSound.maxVolume;
                            exhaustFlameSound.audioSource.Play();

                        }

                        if (exhaustFlameSound.audioSource.isPlaying && !CarController.OtherAddonsManager.Exhausts.Exhaust[0].popping) {

                            exhaustFlameSound.audioSource.Stop();

                        }

                    }

                }

            }

        }

    }

    /// <summary>
    /// Other sounds.
    /// </summary>
    private void Others() {

        if (windSound != null && windSound.audioClips != null) {

            if (!windSound.audioSource) {

                windSound.audioSource = RCCP_AudioSource.NewAudioSource(audioMixer, gameObject, windSound.audioClips.name, windSound.minDistance, windSound.maxDistance, 0f, windSound.audioClips, true, true, false);
                windSound.audioSource.transform.localPosition = windSound.localPosition;

            } else {

                windSound.audioSource.volume = Mathf.InverseLerp(0f, 200f, Mathf.Abs(Mathf.Abs(CarController.speed))) * windSound.maxVolume * .2f;

            }

        }

    }

    private void Update() {

        Engine();
        Gearbox();
        Wheel();
        Exhaust();
        Others();

    }

    /// <summary>
    /// Deflate wheel sound.
    /// </summary>
    public void DeflateWheel() {

        if (wheelDeflateSound != null && wheelDeflateSound.audioClips != null) {

            wheelDeflateSound.audioSource = RCCP_AudioSource.NewAudioSource(audioMixer, gameObject, wheelDeflateSound.audioClips.name, 5f, 50f, 1f, wheelDeflateSound.audioClips, false, true, true);
            wheelDeflateSound.audioSource.transform.localPosition = wheelDeflateSound.localPosition;

        }

    }

    /// <summary>
    /// Inflate wheel sound.
    /// </summary>
    public void InflateWheel() {

        if (wheelInflateSound != null && wheelInflateSound.audioClips != null) {

            wheelInflateSound.audioSource = RCCP_AudioSource.NewAudioSource(audioMixer, gameObject, wheelInflateSound.audioClips.name, 5f, 50f, 1f, wheelInflateSound.audioClips, false, true, true);
            wheelInflateSound.audioSource.transform.localPosition = wheelInflateSound.localPosition;

        }

    }

    public void DisableEngineSounds() {

        if (engineSounds == null)
            return;

        for (int i = 0; i < engineSounds.Length; i++) {

            if (engineSounds[i] != null) {

                if (engineSounds[i].audioSourceOn)
                    Destroy(engineSounds[i].audioSourceOn.gameObject);

                if (engineSounds[i].audioSourceOff)
                    Destroy(engineSounds[i].audioSourceOff.gameObject);

            }

        }

        engineSounds = null;


    }

    public void Reload() {



    }

    public override void OnDisable() {

        base.OnDisable();

        audioStatesBeforeDisabling = new bool[0];

        if (allAudioSources == null)
            return;

        if (allAudioSources.Length < 1)
            return;

        audioStatesBeforeDisabling = new bool[allAudioSources.Length];

        for (int i = 0; i < allAudioSources.Length; i++) {

            if (allAudioSources[i] != null) {

                audioStatesBeforeDisabling[i] = allAudioSources[i].gameObject.activeSelf;
                allAudioSources[i].gameObject.SetActive(false);

            }

        }

    }

    private void Reset() {

        engineSounds = new EngineSound[4];

        for (int i = 0; i < engineSounds.Length; i++) {

            engineSounds[i] = new EngineSound();

            engineSounds[i].minDistance = 10f;
            engineSounds[i].maxDistance = 200f;

            engineSounds[i].minPitch = .85f;
            engineSounds[i].maxPitch = 1.65f;

        }

        engineSounds[0].minRPM = 0f;
        engineSounds[0].maxRPM = 3000f;
        engineSounds[0].maxVolume = .55f;

        engineSounds[1].minRPM = 2000f;
        engineSounds[1].maxRPM = 6000f;
        engineSounds[1].maxVolume = .65f;

        engineSounds[2].minRPM = 6000f;
        engineSounds[2].maxRPM = 8000f;
        engineSounds[2].maxVolume = .75f;

        engineSounds[3].minRPM = 0f;
        engineSounds[3].maxRPM = 800f;
        engineSounds[3].maxVolume = .75f;

        engineSounds[0].audioClipOn = RCCPSettings.engineLowClipOn;
        engineSounds[0].audioClipOff = RCCPSettings.engineLowClipOff;

        engineSounds[1].audioClipOn = RCCPSettings.engineMedClipOn;
        engineSounds[1].audioClipOff = RCCPSettings.engineMedClipOff;

        engineSounds[2].audioClipOn = RCCPSettings.engineHighClipOn;
        engineSounds[2].audioClipOff = RCCPSettings.engineHighClipOff;

        engineSounds[3].audioClipOn = RCCPSettings.engineIdleClipOn;
        engineSounds[3].audioClipOff = RCCPSettings.engineIdleClipOff;

        gearboxSound = new GearboxSound();
        gearboxSound.minDistance = 1f;
        gearboxSound.maxDistance = 10f;
        gearboxSound.maxVolume = 1f;

        crashSound = new CrashSound();
        crashSound.minDistance = 10f;
        crashSound.maxDistance = 100f;
        crashSound.maxVolume = 1f;

        engineStart = new EngineStart();

        if (RCCPSettings.engineStartClip)
            engineStart.audioClips = RCCPSettings.engineStartClip;

        gearboxSound = new GearboxSound();

        if (RCCPSettings.gearClips != null)
            gearboxSound.audioClips = RCCPSettings.gearClips;

        crashSound = new CrashSound();

        if (RCCPSettings.crashClips != null)
            crashSound.audioClips = RCCPSettings.crashClips;

        reverseSound = new ReverseSound();

        if (RCCPSettings.reversingClip != null)
            reverseSound.audioClips = RCCPSettings.reversingClip;

        windSound = new WindSound();

        if (RCCPSettings.windClip != null)
            windSound.audioClips = RCCPSettings.windClip;

        brakeSound = new BrakeSound();

        if (RCCPSettings.brakeClip != null)
            brakeSound.audioClips = RCCPSettings.brakeClip;

        nosSound = new NosSound();

        if (RCCPSettings.NOSClip != null)
            nosSound.audioClips = RCCPSettings.NOSClip;

        exhaustFlameSound = new ExhaustFlameSound();

        if (RCCPSettings.exhaustFlameClips != null)
            exhaustFlameSound.audioClips = RCCPSettings.exhaustFlameClips;

        turboSound = new TurboSound();

        if (RCCPSettings.turboClip != null)
            turboSound.audioClips = RCCPSettings.turboClip;

        blowSound = new BlowSound();

        if (RCCPSettings.blowoutClip != null)
            blowSound.audioClips = RCCPSettings.blowoutClip;

        wheelDeflateSound = new DeflateSound();

        if (RCCPSettings.wheelDeflateClip != null)
            wheelDeflateSound.audioClips = RCCPSettings.wheelDeflateClip;

        wheelInflateSound = new InflateSound();

        if (RCCPSettings.wheelInflateClip != null)
            wheelInflateSound.audioClips = RCCPSettings.wheelInflateClip;

        wheelFlatSound = new FlatSound();

        if (RCCPSettings.wheelFlatClip != null)
            wheelFlatSound.audioClips = RCCPSettings.wheelFlatClip;

    }

}
