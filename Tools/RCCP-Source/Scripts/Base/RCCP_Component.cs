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

/// <summary>
/// Base class for all modular components.
/// </summary>
[DisallowMultipleComponent]
[Icon("Assets/Realistic Car Controller Pro/Resources/Editor Icons/RCCP_EditorIcon_Vehicle.png")]
public class RCCP_Component : MonoBehaviour, IRCCP_Component {

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

    private RCCP_CarController _carController;

    /// <summary>
    /// Main car controller.
    /// </summary>
    public RCCP_CarController CarController {

        get {

            if (_carController == null) {

                _carController = GetComponentInParent<RCCP_CarController>(true);

                if (_carController != null)
                    Register(_carController, this);

            }

            return _carController;

        }
        set {

            _carController = value;

        }

    }


#if UNITY_EDITOR
    [HideInInspector] public bool completeSetup = false;
    [HideInInspector] public bool checkedSetup = false;
#endif

    /// <summary>
    /// Initializes and registers the target component.
    /// </summary>
    /// <param name="connectedCarController"></param>
    public void Initialize(RCCP_CarController connectedCarController) {

        CarController = connectedCarController;
        Register(CarController, this);

    }

    public virtual void Awake() {

        if (!CarController) {

            if (GetType() != typeof(RCCP_TrailerAttacher)) {

                Debug.LogError("Couldn't found RCCP_CarController in parent, this component named " + name + " is disabled!");
                enabled = false;

            }

        }

    }

    public virtual void Start() {



    }

    public virtual void OnEnable() {

        CheckOnEnableDisable();

    }

    /// <summary>
    /// Initializes and registers the target component.
    /// </summary>
    /// <param name="carController"></param>
    /// <param name="component"></param>
    public void Register(RCCP_CarController carController, IRCCP_Component component) {

        if (!carController) {

            Debug.LogError("This component named " + component.ToString() + " is missing RCCP_CarController on parent. Be sure this component is child object of the RCCP_CarController!");
            enabled = false;
            return;

        }

        CarController = carController;

        switch (component) {

            case RCCP_Engine:

                carController.Engine = component as RCCP_Engine;
                break;

            case RCCP_Clutch:

                carController.Clutch = component as RCCP_Clutch;
                break;

            case RCCP_Gearbox:

                carController.Gearbox = component as RCCP_Gearbox;
                break;

            case RCCP_Differential:

                carController.Differential = component as RCCP_Differential;
                break;

            case RCCP_Axles:

                carController.AxleManager = component as RCCP_Axles;
                break;

            case RCCP_Axle:
                
                if (carController.AxleManager) {

                    if (!carController.AxleManager.Axles.Contains(component as RCCP_Axle))
                        carController.AxleManager.Axles.Add(component as RCCP_Axle);

                } else {

                    enabled = false;

                }

                break;

            case RCCP_WheelCollider:
                break;

            case RCCP_AeroDynamics:

                carController.AeroDynamics = component as RCCP_AeroDynamics;
                break;

            case RCCP_Audio:

                carController.Audio = component as RCCP_Audio;
                break;

            case RCCP_Input:

                carController.Inputs = component as RCCP_Input;
                break;

            case RCCP_Lights:

                carController.Lights = component as RCCP_Lights;
                break;

            case RCCP_Light:

                if (carController.Lights)
                    carController.Lights.RegisterLight(component as RCCP_Light);

                break;

            case RCCP_Stability:

                carController.Stability = component as RCCP_Stability;
                break;

            case RCCP_Damage:

                carController.Damage = component as RCCP_Damage;
                break;

            case RCCP_Particles:

                carController.Particles = component as RCCP_Particles;
                break;

            case RCCP_Customizer:

                carController.Customizer = component as RCCP_Customizer;
                break;

            case RCCP_Lod:

                carController.LOD = component as RCCP_Lod;
                break;

            case RCCP_Recorder:

                if (carController.OtherAddonsManager)
                    carController.OtherAddonsManager.Recorder = component as RCCP_Recorder;
                else
                    enabled = false;

                break;

            case RCCP_OtherAddons:

                carController.OtherAddonsManager = component as RCCP_OtherAddons;
                break;

            case RCCP_Exhausts:

                if (carController.OtherAddonsManager)
                    carController.OtherAddonsManager.Exhausts = component as RCCP_Exhausts;
                else
                    enabled = false;

                break;

            case RCCP_Limiter:

                if (carController.OtherAddonsManager)
                    carController.OtherAddonsManager.Limiter = component as RCCP_Limiter;
                else
                    enabled = false;

                break;

            case RCCP_Nos:

                if (carController.OtherAddonsManager)
                    carController.OtherAddonsManager.Nos = component as RCCP_Nos;
                else
                    enabled = false;

                break;

            case RCCP_TrailerAttacher:

                if (carController.OtherAddonsManager)
                    carController.OtherAddonsManager.TrailAttacher = component as RCCP_TrailerAttacher;
                else
                    enabled = false;

                break;

            case RCCP_Visual_Dashboard:

                if (carController.OtherAddonsManager)
                    carController.OtherAddonsManager.Dashboard = component as RCCP_Visual_Dashboard;
                else
                    enabled = false;

                break;

            case RCCP_Exterior_Cameras:

                if (carController.OtherAddonsManager)
                    carController.OtherAddonsManager.ExteriorCameras = component as RCCP_Exterior_Cameras;
                else
                    enabled = false;

                break;

            case RCCP_AI:

                if (carController.OtherAddonsManager)
                    carController.OtherAddonsManager.AI = component as RCCP_AI;
                else
                    enabled = false;

                break;

            case RCCP_WheelBlur:

                if (carController.OtherAddonsManager)
                    carController.OtherAddonsManager.WheelBlur = component as RCCP_WheelBlur;
                else
                    enabled = false;

                break;

            case RCCP_FuelTank:

                if (carController.OtherAddonsManager)
                    carController.OtherAddonsManager.FuelTank = component as RCCP_FuelTank;
                else
                    enabled = false;

                break;

            case RCCP_BodyTilt:

                if (carController.OtherAddonsManager)
                    carController.OtherAddonsManager.BodyTilt = component as RCCP_BodyTilt;
                else
                    enabled = false;

                break;

        }

    }

    /// <summary>
    /// Checking and reseting component on enable / disable.
    /// </summary>
    public void CheckOnEnableDisable() {

        IRCCP_Component component = this;

        switch (component) {

            case RCCP_Engine:

                RCCP_Engine engine = component as RCCP_Engine;
                engine.Reload();

                break;

            case RCCP_Clutch:

                RCCP_Clutch clutch = component as RCCP_Clutch;
                clutch.Reload();

                break;

            case RCCP_Gearbox:

                RCCP_Gearbox gearbox = component as RCCP_Gearbox;
                gearbox.Reload();

                break;

            case RCCP_Differential:

                RCCP_Differential differential = component as RCCP_Differential;
                differential.Reload();

                break;

            case RCCP_Axle:

                RCCP_Axle axle = component as RCCP_Axle;
                axle.Reload();

                break;

            case RCCP_WheelCollider:

                RCCP_WheelCollider wheelCollider = component as RCCP_WheelCollider;
                wheelCollider.Reload();

                break;

            case RCCP_AeroDynamics:

                RCCP_AeroDynamics Aero = component as RCCP_AeroDynamics;
                Aero.Reload();

                break;

            case RCCP_AI:

                RCCP_AI AI = component as RCCP_AI;
                AI.Reload();

                break;

            case RCCP_Audio:

                RCCP_Audio audio = component as RCCP_Audio;
                audio.Reload();

                break;

            case RCCP_Input:

                RCCP_Input input = component as RCCP_Input;
                input.ResetInputs();

                break;

            case RCCP_Light:

                RCCP_Light light = component as RCCP_Light;
                light.Reload();

                break;

            case RCCP_Lights:

                RCCP_Lights lights = component as RCCP_Lights;
                lights.Reload();

                break;

            case RCCP_Limiter:

                RCCP_Limiter limiter = component as RCCP_Limiter;
                limiter.Reload();

                break;

            case RCCP_Nos:

                RCCP_Nos nos = component as RCCP_Nos;
                nos.Reload();

                break;

            case RCCP_Recorder:

                RCCP_Recorder recorder = component as RCCP_Recorder;
                recorder.Reload();

                break;

            case RCCP_Stability:

                RCCP_Stability stability = component as RCCP_Stability;
                stability.Reload();

                break;

            case RCCP_Visual_Dashboard:

                RCCP_Visual_Dashboard dashboard = component as RCCP_Visual_Dashboard;
                dashboard.Reload();

                break;

            case RCCP_Customizer:

                RCCP_Customizer customizer = component as RCCP_Customizer;
                customizer.Reload();

                break;

            case RCCP_WheelBlur:

                RCCP_WheelBlur wheelBlur = component as RCCP_WheelBlur;
                wheelBlur.Reload();

                break;

            case RCCP_FuelTank:

                RCCP_FuelTank fuelTank = component as RCCP_FuelTank;
                fuelTank.Reload();

                break;

            case RCCP_BodyTilt:

                RCCP_BodyTilt bodyTilt = component as RCCP_BodyTilt;
                bodyTilt.Reload();

                break;

            case RCCP_Lod:

                RCCP_Lod lod = component as RCCP_Lod;
                lod.Reload();

                break;

        }

    }

    public virtual void OnDisable() {

        CheckOnEnableDisable();

    }

}
