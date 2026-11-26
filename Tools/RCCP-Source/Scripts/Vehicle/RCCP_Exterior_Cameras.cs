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
/// Additional camera manager for hood and wheel cameras.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Other Addons/RCCP Exterior Cameras")]
public class RCCP_Exterior_Cameras : RCCP_Component {

    /// <summary>
    /// FPS / hood camera.
    /// </summary>
    public RCCP_HoodCamera _hoodCamera;
    public RCCP_HoodCamera HoodCamera {

        get {

            if (_hoodCamera == null)
                _hoodCamera = GetComponentInChildren<RCCP_HoodCamera>(true);

            return _hoodCamera;

        }

    }

    /// <summary>
    /// Wheel camera.
    /// </summary>
    public RCCP_WheelCamera _wheelCamera;
    public RCCP_WheelCamera WheelCamera {

        get {

            if (_wheelCamera == null)
                _wheelCamera = GetComponentInChildren<RCCP_WheelCamera>(true);

            return _wheelCamera;

        }

    }

}
