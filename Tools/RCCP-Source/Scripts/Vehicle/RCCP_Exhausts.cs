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
/// Exhaust manager. All exhausts must be connected to this manager.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Other Addons/RCCP Exhausts")]
public class RCCP_Exhausts : RCCP_Component {

    /// <summary>
    /// All exhausts attached to the vehicle.
    /// </summary>
    public RCCP_Exhaust[] _exhausts;
    public RCCP_Exhaust[] Exhaust {

        get {

            _exhausts = GetComponentInParent<RCCP_CarController>(true).GetComponentsInChildren<RCCP_Exhaust>(true);
            return _exhausts;

        }

    }

    public void GetAllExhausts() {

        _exhausts = GetComponentsInChildren<RCCP_Exhaust>(true);

    }

}
