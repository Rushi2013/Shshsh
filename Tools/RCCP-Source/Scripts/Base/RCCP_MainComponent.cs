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
using System.Linq;

/// <summary>
/// Base class for main controller (RCCP_CarController).
/// </summary>
[SelectionBase]
[DisallowMultipleComponent]
[Icon("Assets/Realistic Car Controller Pro/Resources/Editor Icons/RCCP_EditorIcon_Vehicle.png")]
public class RCCP_MainComponent : MonoBehaviour {

    public RCCP_Settings RCCPSettings {

        get {

            if (_RCCPSettings == null)
                _RCCPSettings = RCCP_Settings.Instance;

            return _RCCPSettings;

        }

    }
    private RCCP_Settings _RCCPSettings;

    public RCCP_GroundMaterials RCCPGroundMaterials {

        get {

            if (_RCCPGroundMaterials == null)
                _RCCPGroundMaterials = RCCP_GroundMaterials.Instance;

            return _RCCPGroundMaterials;

        }

    }
    private RCCP_GroundMaterials _RCCPGroundMaterials;

    /// <summary>
    /// Main car controller.
    /// </summary>
    public RCCP_CarController CarController {

        get {

            if (_carController == null)
                _carController = GetComponent<RCCP_CarController>();

            return _carController;

        }
        set {

            _carController = value;

        }

    }

    #region COMPONENTS

    public List<IRCCP_Component> components = new List<IRCCP_Component>();
    public IRCCP_UpgradeComponent[] upgradeComponents = new IRCCP_UpgradeComponent[0];

    //  Rigidbody.
    public Rigidbody Rigid {

        get {

            if (_rigid == null)
                _rigid = GetComponent<Rigidbody>();

            return _rigid;

        }

    }

    //  Engine.
    public RCCP_Engine Engine {

        get {

            if (_engine == null)
                _engine = RCCP_TryGetComponentInChildren.Get<RCCP_Engine>(transform);

            return _engine;

        }
        set {

            _engine = value;

        }

    }

    //  Clutch
    public RCCP_Clutch Clutch {

        get {

            if (_clutch == null)
                _clutch = RCCP_TryGetComponentInChildren.Get<RCCP_Clutch>(transform);

            return _clutch;

        }
        set {

            _clutch = value;

        }

    }

    //  Gearbox.
    public RCCP_Gearbox Gearbox {

        get {

            if (_gearbox == null)
                _gearbox = RCCP_TryGetComponentInChildren.Get<RCCP_Gearbox>(transform);

            return _gearbox;

        }
        set {

            _gearbox = value;

        }

    }

    //  Differential.
    public RCCP_Differential Differential {

        get {

            if (_differential == null)
                _differential = RCCP_TryGetComponentInChildren.Get<RCCP_Differential>(transform);

            return _differential;

        }
        set {

            _differential = value;

        }

    }

    //  Axles.
    public RCCP_Axles AxleManager {

        get {

            if (_axles == null)
                _axles = RCCP_TryGetComponentInChildren.Get<RCCP_Axles>(transform);

            return _axles;

        }
        set {

            _axles = value;

        }

    }

    //  Front axle.
    public RCCP_Axle FrontAxle {

        get {

            if (AxleManager == null)
                return null;

            if (AxleManager.Axles == null)
                return null;

            if (AxleManager.Axles.Count < 2)
                return null;

            float[] indexes = new float[AxleManager.Axles.Count];

            for (int i = 0; i < AxleManager.Axles.Count; i++)
                indexes[i] = AxleManager.Axles[i].leftWheelCollider.transform.localPosition.z;

            int biggestIndex = 0;
            int lowestIndex = 0;

            for (int i = 0; i < indexes.Length; i++) {

                if (indexes[i] >= biggestIndex)
                    biggestIndex = i;

                if (indexes[i] <= lowestIndex)
                    lowestIndex = i;

            }

            _axleFront = AxleManager.Axles[biggestIndex];

            return _axleFront;

        }

    }

    //  Rear axle.
    public RCCP_Axle RearAxle {

        get {

            if (AxleManager == null)
                return null;

            if (AxleManager.Axles == null)
                return null;

            if (AxleManager.Axles.Count < 2)
                return null;

            float[] indexes = new float[AxleManager.Axles.Count];

            for (int i = 0; i < AxleManager.Axles.Count; i++)
                indexes[i] = AxleManager.Axles[i].leftWheelCollider.transform.localPosition.z;

            int biggestIndex = 0;
            int lowestIndex = 0;

            for (int i = 0; i < indexes.Length; i++) {

                if (indexes[i] >= biggestIndex)
                    biggestIndex = i;

                if (indexes[i] <= lowestIndex)
                    lowestIndex = i;

            }

            _axleRear = AxleManager.Axles[lowestIndex];

            return _axleRear;

        }

    }

    public List<RCCP_Axle> PoweredAxles {

        get {

            //  Finding powered axles.
            RCCP_Axles am = AxleManager;
            List<RCCP_Axle> axles = new List<RCCP_Axle>();

            if (am)
                axles = am.Axles;

            if (am != null && axles.Count >= 1) {

                _poweredAxles.Clear();

                for (int i = 0; i < axles.Count; i++) {

                    if (axles[i].isPower)
                        _poweredAxles.Add(axles[i]);

                }

            }

            return _poweredAxles;

        }

    }

    public List<RCCP_Axle> BrakedAxles {

        get {

            //  Finding braking axles.
            RCCP_Axles am = AxleManager;
            List<RCCP_Axle> axles = new List<RCCP_Axle>();

            if (am)
                axles = am.Axles;

            if (am != null && axles.Count >= 1) {

                _brakedAxles.Clear();

                for (int i = 0; i < axles.Count; i++) {

                    if (axles[i].isBrake)
                        _brakedAxles.Add(axles[i]);

                }

            }

            return _brakedAxles;

        }

    }

    public List<RCCP_Axle> SteeredAxles {

        get {

            //  Finding steering  axles.
            RCCP_Axles am = AxleManager;
            List<RCCP_Axle> axles = new List<RCCP_Axle>();

            if (am)
                axles = am.Axles;

            if (am != null && axles.Count >= 1) {

                _steeredAxles.Clear();

                for (int i = 0; i < axles.Count; i++) {

                    if (axles[i].isSteer)
                        _steeredAxles.Add(axles[i]);

                }

            }

            return _steeredAxles;

        }

    }

    public List<RCCP_Axle> HandbrakedAxles {

        get {

            //  Finding handbraking axles.
            RCCP_Axles am = AxleManager;
            List<RCCP_Axle> axles = new List<RCCP_Axle>();

            if (am)
                axles = am.Axles;

            if (am != null && axles.Count >= 1) {

                _handbrakedAxles.Clear();

                for (int i = 0; i < axles.Count; i++) {

                    if (axles[i].isHandbrake)
                        _handbrakedAxles.Add(axles[i]);

                }

            }

            return _handbrakedAxles;

        }

    }

    //  All wheelcolliders.
    public RCCP_WheelCollider[] AllWheelColliders {

        get {

            if (_allWheelColliders == null || (_allWheelColliders != null && _allWheelColliders.Length < 1))
                _allWheelColliders = GetComponentsInChildren<RCCP_WheelCollider>(true);

            return _allWheelColliders;

        }
        set {

            _allWheelColliders = value;

        }

    }

    //  Aerodynamics.
    public RCCP_AeroDynamics AeroDynamics {

        get {

            if (_aero == null)
                _aero = RCCP_TryGetComponentInChildren.Get<RCCP_AeroDynamics>(transform);

            return _aero;

        }
        set {

            _aero = value;

        }

    }

    //  Inputs.
    public RCCP_Input Inputs {

        get {

            if (_inputs == null)
                _inputs = RCCP_TryGetComponentInChildren.Get<RCCP_Input>(transform);

            return _inputs;

        }
        set {

            _inputs = value;

        }

    }

    //  Audio.
    public RCCP_Audio Audio {

        get {

            if (_audio == null)
                _audio = RCCP_TryGetComponentInChildren.Get<RCCP_Audio>(transform);

            return _audio;

        }
        set {

            _audio = value;

        }

    }

    //  Lights.
    public RCCP_Lights Lights {

        get {

            if (_lights == null)
                _lights = RCCP_TryGetComponentInChildren.Get<RCCP_Lights>(transform);

            return _lights;

        }
        set {

            _lights = value;

        }

    }

    //  Stability.
    public RCCP_Stability Stability {

        get {

            if (_stability == null)
                _stability = RCCP_TryGetComponentInChildren.Get<RCCP_Stability>(transform);

            return _stability;

        }
        set {

            _stability = value;

        }

    }

    //  Damage.
    public RCCP_Damage Damage {

        get {

            if (_damage == null)
                _damage = RCCP_TryGetComponentInChildren.Get<RCCP_Damage>(transform);

            return _damage;

        }
        set {

            _damage = value;

        }

    }

    //  Particles.
    public RCCP_Particles Particles {

        get {

            if (_particles == null)
                _particles = RCCP_TryGetComponentInChildren.Get<RCCP_Particles>(transform);

            return _particles;

        }
        set {

            _particles = value;

        }

    }

    //  Customizer.
    public RCCP_Customizer Customizer {

        get {

            if (_customizer == null)
                _customizer = RCCP_TryGetComponentInChildren.Get<RCCP_Customizer>(transform);

            return _customizer;

        }
        set {

            _customizer = value;

        }

    }

    //  LOD.
    public RCCP_Lod LOD {

        get {

            if (_LOD == null)
                _LOD = RCCP_TryGetComponentInChildren.Get<RCCP_Lod>(transform);

            return _LOD;

        }
        set {

            _LOD = value;

        }

    }

    //  Other addons.
    public RCCP_OtherAddons OtherAddonsManager {

        get {

            if (_otherAddons == null)
                _otherAddons = RCCP_TryGetComponentInChildren.Get<RCCP_OtherAddons>(transform);

            return _otherAddons;

        }
        set {

            _otherAddons = value;

        }

    }

    //  Private fields for components.
    private RCCP_CarController _carController;
    private Rigidbody _rigid = null;
    private RCCP_Input _inputs = null;
    private RCCP_Engine _engine = null;
    private RCCP_Clutch _clutch = null;
    private RCCP_Gearbox _gearbox = null;
    private RCCP_Differential _differential = null;
    private RCCP_Axles _axles = null;
    private RCCP_Axle _axleFront = null;
    private RCCP_Axle _axleRear = null;
    [SerializeField] private List<RCCP_Axle> _poweredAxles = new List<RCCP_Axle>();
    [SerializeField] private List<RCCP_Axle> _brakedAxles = new List<RCCP_Axle>();
    [SerializeField] private List<RCCP_Axle> _steeredAxles = new List<RCCP_Axle>();
    [SerializeField] private List<RCCP_Axle> _handbrakedAxles = new List<RCCP_Axle>();
    private RCCP_AeroDynamics _aero = null;
    private RCCP_Audio _audio = null;
    private RCCP_Lights _lights = null;
    private RCCP_Stability _stability = null;
    private RCCP_Damage _damage = null;
    private RCCP_Particles _particles = null;
    private RCCP_OtherAddons _otherAddons = null;
    private RCCP_WheelCollider[] _allWheelColliders = null;
    private RCCP_Customizer _customizer = null;
    private RCCP_Lod _LOD = null;

    #endregion

#if UNITY_EDITOR
    [HideInInspector] public bool checkComponents = false;
#endif

    private void Awake() {

        GetAllComponents();

    }

    public void GetAllComponents() {

        //  Finding and initializing all components attached to this vehicle (even if they are disabled).
        CarController = this as RCCP_CarController;

        RCCP_CarController carController = CarController;
        Rigidbody rigid = Rigid;
        RCCP_Engine engine = Engine;
        RCCP_Clutch clutch = Clutch;
        RCCP_Gearbox gearbox = Gearbox;
        RCCP_Differential differential = Differential;
        RCCP_Axles axles = AxleManager;
        RCCP_Axle frontAxle = FrontAxle;
        RCCP_Axle rearAxle = RearAxle;
        RCCP_WheelCollider[] allWheelColliders = AllWheelColliders;
        RCCP_AeroDynamics aeroDynamics = AeroDynamics;
        RCCP_Input inputs = Inputs;
        RCCP_Audio audio = Audio;
        RCCP_Lights lights = Lights;
        RCCP_Stability stability = Stability;
        RCCP_Damage damage = Damage;
        RCCP_Particles particles = Particles;
        RCCP_Customizer customizer = Customizer;
        RCCP_Lod lod = LOD;
        RCCP_OtherAddons otherAddons = OtherAddonsManager;
        List<RCCP_Axle> poweredAxles = PoweredAxles;
        List<RCCP_Axle> brakedAxles = BrakedAxles;
        List<RCCP_Axle> steeringAxles = SteeredAxles;
        List<RCCP_Axle> handbrakingAxles = HandbrakedAxles;

        components = GetComponentsInChildren<IRCCP_Component>(true).ToList();
        upgradeComponents = GetComponentsInChildren<IRCCP_UpgradeComponent>(true);

        foreach (IRCCP_Component item in components)
            item.Initialize(CarController);

        foreach (IRCCP_UpgradeComponent item in upgradeComponents)
            item.Initialize(CarController);

    }

    /// <summary>
    /// Resetting variables to default on enable / disable.
    /// </summary>
    /// <param name="carController"></param>
    public void ResetVehicle() {

        CarController.engineRPM = 0f;
        CarController.currentGear = 0;
        CarController.currentGearRatio = 1f;
        CarController.lastGearRatio = 1f;
        CarController.differentialRatio = 1f;
        CarController.speed = 0f;
        CarController.wheelRPM2Speed = 0f;
        CarController.tractionWheelRPM2EngineRPM = 0f;
        CarController.targetWheelSpeedForCurrentGear = 0f;
        CarController.maximumSpeed = 0f;
        CarController.producedEngineTorque = 0f;
        CarController.producedGearboxTorque = 0f;
        CarController.producedDifferentialTorque = 0f;
        CarController.direction = 1;
        CarController.engineStarting = false;
        CarController.engineRunning = false;
        CarController.shiftingNow = false;
        CarController.reversingNow = false;
        CarController.steerAngle = 0f;
        CarController.fuelInput_V = 0f;
        CarController.throttleInput_V = 0f;
        CarController.brakeInput_V = 0f;
        CarController.steerInput_V = 0f;
        CarController.handbrakeInput_V = 0f;
        CarController.clutchInput_V = 0f;
        CarController.gearInput_V = 0f;
        CarController.nosInput_V = 0f;
        CarController.throttleInput_P = 0f;
        CarController.brakeInput_P = 0f;
        CarController.steerInput_P = 0f;
        CarController.handbrakeInput_P = 0f;
        CarController.clutchInput_P = 0f;
        CarController.nosInput_P = 0f;
        CarController.lowBeamLights = false;
        CarController.highBeamLights = false;
        CarController.indicatorsLeftLights = false;
        CarController.indicatorsRightLights = false;
        CarController.indicatorsAllLights = false;

    }

}
