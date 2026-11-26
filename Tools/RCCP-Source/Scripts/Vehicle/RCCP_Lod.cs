//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright © 2014 - 2024 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// LOD but not visual LOD. Enables or disables the most heavier processes of the vehicle controller relative to main camera distance.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Addons/RCCP LOD")]
[DefaultExecutionOrder(10)]
public class RCCP_Lod : RCCP_Component {

    /// <summary>
    /// All heavier components.
    /// </summary>
    private RCCP_WheelCollider[] wheelColliders;
    private RCCP_Lights lights;
    private RCCP_Audio audios;
    private RCCP_AeroDynamics aeroDynamics;
    private RCCP_Stability stability;
    private RCCP_Damage damage;
    private RCCP_Particles particles;
    private RCCP_WheelBlur wheelBlur;

    /// <summary>
    /// Force to the first level, which is 0.
    /// </summary>
    public bool forceToFirstLevel = false;

    /// <summary>
    /// Force to the last level, which is 2.
    /// </summary>
    public bool forceToLatestLevel = false;

    /// <summary>
    /// Current lod level.
    /// </summary>
    [Range(0, 3)] public int currentLODLevel = 0;

    /// <summary>
    /// Old lod level to detect when the value changed.
    /// </summary>
    private int currentLODLevel_Old = 0;

    /// <summary>
    /// LOD factor.
    /// </summary>
    [Range(.1f, 1f)] public float lodFactor = .8f;

    /// <summary>
    /// LOD factor used in method.
    /// </summary>
    private float LODFactor {

        get {

            return .5f + Mathf.InverseLerp(.1f, 1f, lodFactor);

        }

    }

    public override void Awake() {

        base.Awake();

        //  Resetting the lod level on awake.
        currentLODLevel = 0;
        currentLODLevel_Old = 0;

        //  Getting all components on awake.
        wheelColliders = CarController.AllWheelColliders.ToArray();
        lights = CarController.Lights;
        audios = CarController.Audio;
        aeroDynamics = CarController.AeroDynamics;
        stability = CarController.Stability;
        damage = CarController.Damage;
        particles = CarController.Particles;

        if (CarController.OtherAddonsManager)
            wheelBlur = CarController.OtherAddonsManager.WheelBlur;

    }

    public override void OnEnable() {

        base.OnEnable();

        //  Force to refresh on enable.
        LODLevelChanged();

    }

    private int GetLODLevel() {

        //  If scene has a main camera, proceed.
        if (RCCP_SceneManager.Instance.activeMainCamera) {

            //  Distance to the main camera.
            float distanceToCamera = Vector3.Distance(transform.position, RCCP_SceneManager.Instance.activeMainCamera.transform.position);

            //  Calculating the lod level depending on the distance.
            if (distanceToCamera < (50f * LODFactor))
                return 0;
            else if (distanceToCamera < (60f * LODFactor))
                return 1;
            else if (distanceToCamera < (75f * LODFactor))
                return 2;
            else if (distanceToCamera < (100f * LODFactor))
                return 3;

            return 3;

        }

        return 0;

    }

    private void Update() {

        //  Force the LOD level if these options are enabled.
        if (forceToFirstLevel || forceToLatestLevel) {

            if (forceToFirstLevel)
                currentLODLevel = 0;
            else if (forceToLatestLevel)
                currentLODLevel = 2;

            LODLevelChanged();

        } else {

            currentLODLevel = GetLODLevel();

        }

        //  When lod level changed.
        if (currentLODLevel != currentLODLevel_Old)
            LODLevelChanged();

        //  Setting old lod value.
        currentLODLevel_Old = currentLODLevel;

    }

    /// <summary>
    /// When LOD level changed.
    /// </summary>
    private void LODLevelChanged() {

        //  Enabling / disabling the components depending on the lod level number. 
        switch (currentLODLevel) {

            case 0:

                for (int i = 0; i < wheelColliders.Length; i++) {

                    if (wheelColliders[i] != null) {

                        wheelColliders[i].alignWheels = true;
                        wheelColliders[i].drawSkid = true;

                    }

                }

                if (lights)
                    lights.enabled = true;

                if (audios)
                    audios.enabled = true;

                if (aeroDynamics)
                    aeroDynamics.enabled = true;

                if (stability)
                    stability.enabled = true;

                if (damage)
                    damage.enabled = true;

                if (particles)
                    particles.enabled = true;

                if (wheelBlur)
                    wheelBlur.enabled = true;

                break;

            case 1:

                for (int i = 0; i < wheelColliders.Length; i++) {

                    if (wheelColliders[i] != null) {

                        wheelColliders[i].alignWheels = false;
                        wheelColliders[i].drawSkid = false;

                    }

                }

                if (lights)
                    lights.enabled = false;

                if (audios)
                    audios.enabled = true;

                if (aeroDynamics)
                    aeroDynamics.enabled = false;

                if (stability)
                    stability.enabled = false;

                if (damage)
                    damage.enabled = false;

                if (particles)
                    particles.enabled = false;

                if (wheelBlur)
                    wheelBlur.enabled = false;

                break;

            case 2:

                for (int i = 0; i < wheelColliders.Length; i++) {

                    if (wheelColliders[i] != null) {

                        wheelColliders[i].alignWheels = false;
                        wheelColliders[i].drawSkid = false;

                    }

                }

                if (lights)
                    lights.enabled = false;

                if (audios)
                    audios.enabled = false;

                if (aeroDynamics)
                    aeroDynamics.enabled = false;

                if (stability)
                    stability.enabled = false;

                if (damage)
                    damage.enabled = false;

                if (particles)
                    particles.enabled = false;

                if (wheelBlur)
                    wheelBlur.enabled = false;

                break;


            case 3:

                for (int i = 0; i < wheelColliders.Length; i++) {

                    if (wheelColliders[i] != null) {

                        wheelColliders[i].alignWheels = false;
                        wheelColliders[i].drawSkid = false;

                    }

                }

                if (lights)
                    lights.enabled = false;

                if (audios)
                    audios.enabled = false;

                if (aeroDynamics)
                    aeroDynamics.enabled = false;

                if (stability)
                    stability.enabled = false;

                if (damage)
                    damage.enabled = false;

                if (particles)
                    particles.enabled = false;

                if (wheelBlur)
                    wheelBlur.enabled = false;

                break;

        }

    }

    /// <summary>
    /// Reload.
    /// </summary>
    public void Reload() {

        //  Resetting the lod level on awake.
        currentLODLevel = 0;
        currentLODLevel_Old = 0;

        LODLevelChanged();

    }

}
