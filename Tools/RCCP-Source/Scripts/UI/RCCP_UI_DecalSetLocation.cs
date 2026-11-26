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
/// UI decal button.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/Modification/RCCP UI Decal Set LocationButton")]
public class RCCP_UI_DecalSetLocation : RCCP_UIComponent {

    /// <summary>
    /// Location of the decal. 0 is front, 1 is back, 2 is left, and 3 is right.
    /// </summary>
    [Min(0)] public int location = 0;

    public void Upgrade() {

        RCCP_UI_Decal[] decalButtons = FindObjectsOfType<RCCP_UI_Decal>(true);

        if (decalButtons == null)
            return;

        if (decalButtons.Length < 1)
            return;

        foreach (RCCP_UI_Decal item in decalButtons)
            item.SetLocation(location);

    }

    /// <summary>
    /// Sets the location of the decal. 0 is front, 1 is back, 2 is left, and 3 is right.
    /// </summary>
    /// <param name="_location"></param>
    public void SetLocation(int _location) {

        location = _location;

    }

}
