//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright © 2014 - 2024 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Receives float from UI Slider, and displays the value as a text.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/RCCP UI Slider Text Reader")]
public class RCCP_UISliderTextReader : RCCP_UIComponent {

    /// <summary>
    /// UI Slider component.
    /// </summary>
    public Slider slider;

    /// <summary>
    /// UI Text to display slider value if choosen.
    /// </summary>
    public Text text;

    private void Awake() {

        if (!slider)
            slider = GetComponentInParent<Slider>();

        if (!text)
            text = GetComponentInChildren<Text>();

    }

    private void Update() {

        if (!slider || !text)
            return;

        text.text = slider.value.ToString("F1");

    }

}
