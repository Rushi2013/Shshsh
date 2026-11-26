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
using UnityEngine.InputSystem;


/// <summary>
/// RCCP InputAction.
/// </summary>
[Icon("Assets/Realistic Car Controller Pro/Resources/Editor Icons/RCCP_EditorIcon_Manager.png")]
public class RCCP_InputActions : ScriptableObject {

    #region singleton
    private static RCCP_InputActions instance;
    public static RCCP_InputActions Instance { get { if (instance == null) instance = Resources.Load("RCCP_InputActions") as RCCP_InputActions; return instance; } }
    #endregion

    public InputActionAsset inputActions;

}
