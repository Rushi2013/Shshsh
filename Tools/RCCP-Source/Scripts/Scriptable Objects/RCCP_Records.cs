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
/// Recorded clips.
/// </summary>
[Icon("Assets/Realistic Car Controller Pro/Resources/Editor Icons/RCCP_EditorIcon_Manager.png")]
public class RCCP_Records : ScriptableObject {

    #region singleton
    private static RCCP_Records instance;
    public static RCCP_Records Instance { get { if (instance == null) instance = Resources.Load("RCCP_Records") as RCCP_Records; return instance; } }
    #endregion

    /// <summary>
    /// All records.
    /// </summary>
    public List<RCCP_Recorder.RecordedClip> records = new List<RCCP_Recorder.RecordedClip>();

}
