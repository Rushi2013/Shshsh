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
/// Animation attached to "Animation Pivot" of the Cinematic Camera is feeding FOV float value.
/// </summary>
public class RCCP_FOVForCinematicCamera : RCCP_GenericComponent {

    /// <summary>
    /// Cinematic camera.
    /// </summary>
    private RCCP_CinematicCamera cinematicCamera;

    /// <summary>
    /// Target field of view.
    /// </summary>
    [Min(0f)] public float FOV = 30f;

    private void Awake() {

        //  Getting cinematic camera.
        cinematicCamera = GetComponentInParent<RCCP_CinematicCamera>(true);

    }

    private void Update() {

        if (!cinematicCamera)
            return;

        //  Setting field of view of the cinematic camera.
        cinematicCamera.targetFOV = FOV;

    }

}
