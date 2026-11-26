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
/// Single waypoint for AI.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/AI/RCCP Waypoint")]
public class RCCP_Waypoint : RCCP_GenericComponent {

    /// <summary>
    /// Target speed for AI. 
    /// </summary>
    [Range(0f, 360f)] public float targetSpeed = 100f;

}
