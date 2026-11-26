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
/// All prototype vehicles.
/// </summary>
[Icon("Assets/Realistic Car Controller Pro/Resources/Editor Icons/RCCP_EditorIcon_Manager.png")]
public class RCCP_PrototypeContent : ScriptableObject {

    public int instanceId = 0;

    /// <summary>
    /// All spawnable vehicles.
    /// </summary>
    public RCCP_CarController[] vehicles;

    #region singleton
    private static RCCP_PrototypeContent instance;
    public static RCCP_PrototypeContent Instance { get { if (instance == null) instance = Resources.Load("RCCP_PrototypeContent") as RCCP_PrototypeContent; return instance; } }
    #endregion

}
