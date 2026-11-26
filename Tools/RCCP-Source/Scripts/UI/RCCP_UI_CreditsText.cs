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
using TMPro;

public class RCCP_UI_CreditsText : RCCP_UIComponent {

    private TextMeshProUGUI text;

    private void OnEnable() {

        if (!text)
            text = GetComponent<TextMeshProUGUI>();

        if (!text)
            return;

        string creditsText = "Realistic Car Controller Pro " + RCCP_Version.version + "\nBoneCracker Games\nEkrem Bugra Ozdoganlar";

        text.text = creditsText;

    }

}
