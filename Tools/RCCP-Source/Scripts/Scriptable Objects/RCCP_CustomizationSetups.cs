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
using UnityEngine;

/// <summary>
/// All demo vehicles.
/// </summary>
[Icon("Assets/Realistic Car Controller Pro/Resources/Editor Icons/RCCP_EditorIcon_Manager.png")]
public class RCCP_CustomizationSetups : ScriptableObject {

    public GameObject customization;
    public GameObject decals;
    public GameObject neons;
    public GameObject paints;
    public GameObject sirens;
    public GameObject spoilers;
    public GameObject upgrades;
    public GameObject wheels;

    #region singleton
    private static RCCP_CustomizationSetups instance;
    public static RCCP_CustomizationSetups Instance { get { if (instance == null) instance = Resources.Load("RCCP_CustomizationSetups") as RCCP_CustomizationSetups; return instance; } }
    #endregion

}
