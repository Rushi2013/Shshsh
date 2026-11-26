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
/// Axles manager. All axles must be connected to this manager.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Drivetrain/RCCP Axles")]
public class RCCP_Axles : RCCP_Component {

    /// <summary>
    /// All axles attached to this axle manager.
    /// </summary>
    private List<RCCP_Axle> _axles = new List<RCCP_Axle>();
    public List<RCCP_Axle> Axles {

        get {

            if (_axles == null || _axles.Count < 1) {

                _axles = new List<RCCP_Axle>();

                RCCP_Axle[] axlesArray = GetComponentsInChildren<RCCP_Axle>(true);

                for (int i = 0; i < axlesArray.Length; i++)
                    _axles.Add(axlesArray[i]);

                return _axles;

            }

            return _axles;

        }
        set {

            _axles = value;

        }

    }

    /// <summary>
    /// Creating two new axles if no any axle found.
    /// </summary>
    private void Reset() {

        Axles = new List<RCCP_Axle>();

        if (Axles == null || (Axles != null && Axles.Count == 0)) {

            GameObject subject = new GameObject("RCCP_Axle_Front");
            subject.transform.SetParent(transform, false);
            RCCP_Axle axle_F = subject.AddComponent<RCCP_Axle>();
            axle_F.gameObject.name = "RCCP_Axle_Front";
            axle_F.isSteer = true;
            axle_F.isBrake = true;
            Axles.Add(subject.GetComponent<RCCP_Axle>());

            GameObject subject_R = new GameObject("RCCP_Axle_Rear");
            subject_R.transform.SetParent(transform, false);
            RCCP_Axle axle_R = subject_R.AddComponent<RCCP_Axle>();
            axle_R.gameObject.name = "RCCP_Axle_Rear";
            axle_R.isBrake = true;
            axle_R.isHandbrake = true;
            axle_R.brakeMultiplier = .65f;
            Axles.Add(axle_R);

        }

    }

}
