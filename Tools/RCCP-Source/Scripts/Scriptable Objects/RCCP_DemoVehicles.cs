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
/// All demo vehicles.
/// </summary>
[Icon("Assets/Realistic Car Controller Pro/Resources/Editor Icons/RCCP_EditorIcon_Manager.png")]
public class RCCP_DemoVehicles : ScriptableObject {

    public int instanceId = 0;

    /// <summary>
    /// All spawnable vehicles.
    /// </summary>
    public RCCP_CarController[] vehicles;

    #region singleton
    private static RCCP_DemoVehicles instance;
    public static RCCP_DemoVehicles Instance { get { if (instance == null) instance = Resources.Load("RCCP_DemoVehicles") as RCCP_DemoVehicles; return instance; } }
    #endregion

}
