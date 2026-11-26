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
/// Base class for all modular components.
/// </summary>
[Icon("Assets/Realistic Car Controller Pro/Resources/Editor Icons/RCCP_EditorIcon_Component.png")]
public class RCCP_GenericComponent : MonoBehaviour {

    public RCCP_Settings RCCPSettings {

        get {

            if (_RCCPSettings == null)
                _RCCPSettings = RCCP_Settings.Instance;

            return _RCCPSettings;

        }

    }
    private RCCP_Settings _RCCPSettings;

    public RCCP_GroundMaterials RCCPGroundMaterials {

        get {

            if (_RCCPGroundMaterials == null)
                _RCCPGroundMaterials = RCCP_GroundMaterials.Instance;

            return _RCCPGroundMaterials;

        }

    }
    private RCCP_GroundMaterials _RCCPGroundMaterials;

}
