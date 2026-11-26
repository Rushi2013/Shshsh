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
using TMPro;

/// <summary>
/// Sets the behavior type with index.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/RCCP UI Set Behavior Type")]
public class RCCP_UI_SetBehaviorType : RCCP_UIComponent {

    private void OnEnable() {

        TMP_Dropdown dropdown = gameObject.GetComponent<TMP_Dropdown>();

        if (dropdown) {

            if (RCCPSettings.overrideBehavior)
                dropdown.SetValueWithoutNotify(RCCPSettings.behaviorSelectedIndex);
            else
                dropdown.SetValueWithoutNotify(0);

        }

    }

    /// <summary>
    /// Sets the behavior type with index.
    /// </summary>
    /// <param name="index"></param>
    public void SetBehaviorType(int index) {

        RCCP.SetBehavior(index);

    }

}
