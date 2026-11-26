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
/// All addon packages.
/// </summary>
[Icon("Assets/Realistic Car Controller Pro/Resources/Editor Icons/RCCP_EditorIcon_Manager.png")]
public class RCCP_AddonPackages : ScriptableObject {

    #region singleton
    private static RCCP_AddonPackages instance;
    public static RCCP_AddonPackages Instance { get { if (instance == null) instance = Resources.Load("RCCP_AddonPackages") as RCCP_AddonPackages; return instance; } }
    #endregion

    public Object demoPackage;
    public Object BCGSharedAssets;
    public Object PhotonPUN2;
    public Object ProFlare;
    public Object mirror;

    public string GetAssetPath(Object pathObject) {

        string path = UnityEditor.AssetDatabase.GetAssetPath(pathObject);
        return path;

    }

}
