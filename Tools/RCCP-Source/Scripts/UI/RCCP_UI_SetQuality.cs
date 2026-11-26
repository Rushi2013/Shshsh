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
/// Sets the target quality.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/RCCP UI Set Quality")]
public class RCCP_UI_SetQuality : RCCP_UIComponent {

    private TMP_Dropdown dropdown;


    private void OnEnable() {

        if (!dropdown)
            dropdown = GetComponent<TMP_Dropdown>();

        int qualityLevel = QualitySettings.GetQualityLevel();

        dropdown.SetValueWithoutNotify(qualityLevel);

    }

    public void SetQualityLevel(TMP_Dropdown dropdown) {

        QualitySettings.SetQualityLevel(dropdown.value, true);

    }

}
