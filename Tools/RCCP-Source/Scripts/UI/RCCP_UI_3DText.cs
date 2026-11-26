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

public class RCCP_UI_3DText : RCCP_UIComponent {

    private void Update() {

        if (!Camera.main)
            return;

        transform.rotation = Camera.main.transform.rotation;

    }

}
