//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright © 2014 - 2024 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UI dashboard buttons for mobile / desktop.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/Input/RCCP UI Rebind Input Reset")]
public class RCCP_UI_RebindInputReset : RCCP_UIComponent {

    public RCCP_UI_RebindInput[] rebindInputs;

    public void OnClick() {

        for (int i = 0; i < rebindInputs.Length; i++) {

            if (rebindInputs[i] != null)
                rebindInputs[i].ResetToDefault();

        }

    }

}
