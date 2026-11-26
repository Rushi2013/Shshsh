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
/// Sets target audio.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/RCCP UI Set Audio")]
public class RCCP_UI_SetAudio : RCCP_UIComponent {

    private TMP_Dropdown dropdown;


    private void OnEnable() {

        if (!dropdown)
            dropdown = GetComponent<TMP_Dropdown>();

        bool audioPaused = AudioListener.pause;

        dropdown.SetValueWithoutNotify(audioPaused ? 1 : 0);

    }

    public void SetAudio(TMP_Dropdown dropdown) {

        AudioListener.pause = dropdown.value == 1 ? true : false;

    }

}
