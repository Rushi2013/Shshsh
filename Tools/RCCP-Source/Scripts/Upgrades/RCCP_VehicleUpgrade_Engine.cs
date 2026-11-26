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
/// Upgrades engine of the car controller.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Customization/RCCP Vehicle Upgrade Engine")]
public class RCCP_VehicleUpgrade_Engine : RCCP_Component {

    private int _engineLevel = 0;

    /// <summary>
    /// Current engine level. Maximum is 5.
    /// </summary>
    public int EngineLevel {
        get {
            return _engineLevel;
        }
        set {
            if (value <= 5)
                _engineLevel = value;
        }
    }

    /// <summary>
    /// Default engine torque.
    /// </summary>
    [HideInInspector] public float defEngine = -1f;

    /// <summary>
    /// Efficiency of the upgrade.
    /// </summary>
    [Range(1f, 2f)] public float efficiency = 1.2f;

    /// <summary>
    /// Updates engine torque and initializes it.
    /// </summary>
    public void Initialize() {

        if (!CarController.Engine) {

            Debug.LogError("Engine couldn't found in the vehicle. RCCP_VehicleUpgrade_Engine needs it to upgrade the engine level");
            enabled = false;
            return;

        }

        if (defEngine <= 0)
            defEngine = CarController.Engine.maximumTorqueAsNM;

        CarController.Engine.maximumTorqueAsNM = Mathf.Lerp(defEngine, defEngine * efficiency, EngineLevel / 5f);

    }

    /// <summary>
    /// Updates engine torque and save it.
    /// </summary>
    public void UpdateStats() {

        if (!CarController.Engine) {

            Debug.LogError("Engine couldn't found in the vehicle. RCCP_VehicleUpgrade_Engine needs it to upgrade the engine level");
            enabled = false;
            return;

        }

        if (defEngine <= 0)
            defEngine = CarController.Engine.maximumTorqueAsNM;

        CarController.Engine.maximumTorqueAsNM = Mathf.Lerp(defEngine, defEngine * efficiency, EngineLevel / 5f);

    }

    private void Update() {

        if (!CarController.Engine) {

            Debug.LogError("Engine couldn't found in the vehicle. RCCP_VehicleUpgrade_Engine needs it to upgrade the engine level");
            enabled = false;
            return;

        }

    }

    public void Restore() {

        EngineLevel = 0;

        if (defEngine <= 0)
            defEngine = CarController.Engine.maximumTorqueAsNM;

        CarController.Engine.maximumTorqueAsNM = defEngine;

    }

}
