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
/// All removable demo contents.
/// </summary>
[Icon("Assets/Realistic Car Controller Pro/Resources/Editor Icons/RCCP_EditorIcon_Manager.png")]
public class RCCP_DemoContent : ScriptableObject {

    public int instanceId = 0;

    #region singleton
    private static RCCP_DemoContent instance;
    public static RCCP_DemoContent Instance { get { if (instance == null) instance = Resources.Load("RCCP_DemoContent") as RCCP_DemoContent; return instance; } }
    #endregion

    public bool dontAskDemoContent = false;
    public Object[] contents;

    public Object builtinShaderPackage;
    public Object URPShaderPackage;

    public Object builtinShadersContent;
    public Object URPShadersContent;

}
