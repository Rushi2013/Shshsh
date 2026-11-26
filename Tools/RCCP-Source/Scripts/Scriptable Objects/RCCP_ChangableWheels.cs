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
/// Changes wheels at runtime. It holds changable wheels as prefab in an array.
/// </summary>
[Icon("Assets/Realistic Car Controller Pro/Resources/Editor Icons/RCCP_EditorIcon_Manager.png")]
[System.Serializable]
public class RCCP_ChangableWheels : ScriptableObject {

    #region singleton
    private static RCCP_ChangableWheels instance;
    public static RCCP_ChangableWheels Instance { get { if (instance == null) instance = Resources.Load("RCCP_ChangableWheels") as RCCP_ChangableWheels; return instance; } }
    #endregion

    [System.Serializable]
    public class ChangableWheels {

        public GameObject wheel;

    }

    /// <summary>
    /// All changable wheels.
    /// </summary>
    public ChangableWheels[] wheels;

}


