//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright © 2014 - 2024 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;

/// <summary>
/// Stored all general shared RCCP settings here.
/// </summary>
[Icon("Assets/Realistic Car Controller Pro/Resources/Editor Icons/RCCP_EditorIcon_Manager.png")]
[System.Serializable]
public class RCCP_Settings : ScriptableObject {

    #region singleton
    private static RCCP_Settings instance;
    public static RCCP_Settings Instance { get { if (instance == null) instance = Resources.Load("RCCP_Settings") as RCCP_Settings; return instance; } }
    #endregion

    /// <summary>
    /// Current behavior.
    /// </summary>
    public BehaviorType SelectedBehaviorType {

        get {

            if (overrideBehavior)
                return behaviorTypes[behaviorSelectedIndex];
            else
                return null;

        }

    }

    /// <summary>
    /// Use multithreading if current platform is supported. Fallback to false if platform is not supported.
    /// </summary>
    public bool multithreading = true;

    /// <summary>
    /// Current selected behavior index.
    /// </summary>
    public int behaviorSelectedIndex = 0;

    /// <summary>
    /// Override FPS?
    /// </summary>
    public bool overrideFPS = true;

    /// <summary>
    /// Override fixed timestep?
    /// </summary>
    public bool overrideFixedTimeStep = true;

    /// <summary>
    /// Overriden fixed timestep value.
    /// </summary>
    [Range(.005f, .06f)] public float fixedTimeStep = .02f;

    /// <summary>
    /// Override autoSyncTransforms in the Project Settings --> Physics.
    /// </summary>
    public bool overrideAutoSyncTransforms = true;

    /// <summary>
    /// Auto sync transform setting in the Project Settings --> Physics.
    /// </summary>
    public bool autoSyncTransforms = true;

    /// <summary>
    /// Maximum angular velocity.
    /// </summary>
    [Range(.5f, 20f)] public float maxAngularVelocity = 6;

    /// <summary>
    /// Maximum FPS.
    /// </summary>
    public int maxFPS = 60;

    /// <summary>
    /// Override the behavior?
    /// </summary>
    public bool overrideBehavior = true;

    /// <summary>
    /// Behavior Types
    /// </summary>
    [System.Serializable]
    public class BehaviorType {

        /// <summary>
        /// Behavior name.
        /// </summary>
        public string behaviorName = "New Behavior";

        //  Driving helpers.
        [Header("Stability")]
        public bool ABS = true;
        public bool ESP = true;
        public bool TCS = true;
        public bool steeringHelper = true;
        public bool tractionHelper = true;
        public bool angularDragHelper = false;

        public bool driftMode = false;
        public bool driftAngleLimiter = false;
        [Range(0f, 90f)] public float driftAngleLimit = 30f;
        [Range(0f, 10f)] public float driftAngleCorrectionFactor = 3f;

        //  Steering.
        [Header("Steering")]
        public AnimationCurve steeringCurve = new AnimationCurve(new Keyframe(0f, 40f), new Keyframe(50f, 20f), new Keyframe(100f, 11f), new Keyframe(150f, 6f), new Keyframe(200f, 5f));
        public float steeringSensitivity = 1f;
        public bool counterSteering = true;
        public bool limitSteering = true;

        [Header("Differential")]
        public RCCP_Differential.DifferentialType differentialType = RCCP_Differential.DifferentialType.Open;

        //  Counter steering limitations.
        [Space()]
        public float counterSteeringMinimum = .5f;
        public float counterSteeringMaximum = 1f;

        //  Steering sensitivity limitations.
        [Space()]
        public float steeringSpeedMinimum = .5f;
        public float steeringSpeedMaximum = 1f;

        //  Steering helper linear velocity limitations.
        [Range(0f, 1f)] public float steeringHelperStrengthMinimum = .1f;
        [Range(0f, 1f)] public float steeringHelperStrengthMaximum = 1f;

        //  Traction helper strength limitations.
        [Range(0f, 1f)] public float tractionHelperStrengthMinimum = .1f;
        [Range(0f, 1f)] public float tractionHelperStrengthMaximum = 1f;

        //  Angular drag limitations.
        [Range(0f, 10f)] public float angularDrag = .1f;
        [Range(0f, 1f)] public float angularDragHelperMinimum = .1f;
        [Range(0f, 1f)] public float angularDragHelperMaximum = 1f;

        //  Anti roll limitations.
        [Space()]
        public float antiRollMinimum = 1000f;

        //  Gear shifting delay limitation.
        [Space()]
        [Range(.1f, .9f)] public float gearShiftingThreshold = .8f;
        [Range(0f, 1f)] public float gearShiftingDelayMinimum = .15f;
        [Range(0f, 1f)] public float gearShiftingDelayMaximum = .5f;

        //  Wheel frictions.
        [Header("Wheel Frictions Forward Front Side")]
        public float forwardExtremumSlip_F = .4f;
        public float forwardExtremumValue_F = 1f;
        public float forwardAsymptoteSlip_F = .8f;
        public float forwardAsymptoteValue_F = .5f;

        [Header("Wheel Frictions Forward Rear Side")]
        public float forwardExtremumSlip_R = .4f;
        public float forwardExtremumValue_R = .95f;
        public float forwardAsymptoteSlip_R = .75f;
        public float forwardAsymptoteValue_R = .5f;

        [Header("Wheel Frictions Sideways Front Side")]
        public float sidewaysExtremumSlip_F = .4f;
        public float sidewaysExtremumValue_F = 1f;
        public float sidewaysAsymptoteSlip_F = .5f;
        public float sidewaysAsymptoteValue_F = .75f;

        [Header("Wheel Frictions Sideways Rear Side")]
        public float sidewaysExtremumSlip_R = .4f;
        public float sidewaysExtremumValue_R = 1.05f;
        public float sidewaysAsymptoteSlip_R = .5f;
        public float sidewaysAsymptoteValue_R = .8f;

    }

    /// <summary>
    /// Behavior Types
    /// </summary>
    public BehaviorType[] behaviorTypes;

    /// <summary>
    /// Fixed wheelcolliders with higher mass will be used.
    /// </summary>
    public bool useFixedWheelColliders = true;      //  

    /// <summary>
    /// All vehicles can be resetted if upside down.
    /// </summary>
    public bool autoReset = true;       //  

    /// <summary>
    /// Information telemetry about current vehicle
    /// </summary>
    public bool useTelemetry = false;
    public bool useInputDebugger = false;

    /// <summary>
    /// Input types
    /// </summary>
    public bool useNewInputSystem = true;

    /// <summary>
    /// Auto saves and loads the rebind map.
    /// </summary>
    public bool autoSaveLoadInputRebind = true;

    /// <summary>
    /// For mobile inputs
    /// </summary>
    public enum MobileController { TouchScreen, Gyro, SteeringWheel, Joystick }

    /// <summary>
    /// For mobile inputs
    /// </summary>
    public MobileController mobileController;

    /// <summary>
    /// Enable / disable the mobile controllers.
    /// </summary>
    public bool mobileControllerEnabled = false;

    /// <summary>
    /// Accelerometer sensitivity
    /// </summary>
    public float gyroSensitivity = 2.5f;

    /// <summary>
    /// Setting layers.
    /// </summary>
    public bool setLayers = true;

    /// <summary>
    /// Layer of the vehicle.
    /// </summary>
    public string RCCPLayer = "RCCP_Vehicle";

    /// <summary>
    /// Wheelcollider layer.
    /// </summary>
    public string RCCPWheelColliderLayer = "RCCP_WheelCollider";

    /// <summary>
    /// Detachable part's layer.
    /// </summary>
    public string RCCPDetachablePartLayer = "RCCP_DetachablePart";

    /// <summary>
    /// Props layer.
    /// </summary>
    public string RCCPPropLayer = "RCCP_Prop";

    /// <summary>
    /// Used for using the lights more efficent and realistic. Vertex is not important, pixel is important.
    /// </summary>
    public bool useHeadLightsAsVertexLights = false;

    /// <summary>
    /// Used for using the lights more efficent and realistic. Vertex is not important, pixel is important.
    /// </summary>
    public bool useBrakeLightsAsVertexLights = true;

    /// <summary>
    /// Used for using the lights more efficent and realistic. Vertex is not important, pixel is important.
    /// </summary>
    public bool useReverseLightsAsVertexLights = true;

    /// <summary>
    /// Used for using the lights more efficent and realistic. Vertex is not important, pixel is important.
    /// </summary>
    public bool useIndicatorLightsAsVertexLights = true;

    /// <summary>
    /// Used for using the lights more efficent and realistic. Vertex is not important, pixel is important.
    /// </summary>
    public bool useOtherLightsAsVertexLights = true;

    #region Setup Prefabs

    // Light prefabs.
    public GameObject headLights_Low;
    public GameObject headLights_High;
    public GameObject brakeLights;
    public GameObject reverseLights;
    public GameObject indicatorLights_L;
    public GameObject indicatorLights_R;
    public GameObject tailLights;
    public GameObject lightBox;

    //  Camera prefabs.
    public RCCP_Camera RCCPMainCamera;
    public GameObject RCCPHoodCamera;
    public GameObject RCCPWheelCamera;
    public GameObject RCCPCinematicCamera;
    public GameObject RCCPFixedCamera;

    //  UI prefabs.
    public GameObject RCCPCanvas;
    public GameObject RCCPTelemetry;

    // Sound FX.
    public AudioMixerGroup audioMixer;
    public AudioClip engineLowClipOn;
    public AudioClip engineLowClipOff;
    public AudioClip engineMedClipOn;
    public AudioClip engineMedClipOff;
    public AudioClip engineHighClipOn;
    public AudioClip engineHighClipOff;
    public AudioClip engineIdleClipOn;
    public AudioClip engineIdleClipOff;
    public AudioClip engineStartClip;
    public AudioClip reversingClip;
    public AudioClip windClip;
    public AudioClip brakeClip;
    public AudioClip wheelDeflateClip;
    public AudioClip wheelInflateClip;
    public AudioClip wheelFlatClip;
    public AudioClip indicatorClip;
    public AudioClip bumpClip;
    public AudioClip NOSClip;
    public AudioClip turboClip;
    public AudioClip[] gearClips;
    public AudioClip[] crashClips;
    public AudioClip[] blowoutClip;
    public AudioClip[] exhaustFlameClips;

    //  Particles
    public GameObject contactParticles;
    public GameObject scratchParticles;
    public GameObject wheelSparkleParticles;

    //  Other prefabs.
    public GameObject exhaustGas;
    public RCCP_SkidmarksManager skidmarksManager;
    public GameObject wheelBlur;
    public LensFlareDataSRP lensFlareData;

    #endregion

    // Used for folding sections of RCCP Settings.
    public bool foldGeneralSettings = false;
    public bool foldBehaviorSettings = false;
    public bool foldControllerSettings = false;
    public bool foldUISettings = false;
    public bool foldWheelPhysics = false;
    public bool foldOptimization = false;
    public bool foldTagsAndLayers = false;
    public bool foldExtensions = false;
    public bool resourcesSettings = false;

}
