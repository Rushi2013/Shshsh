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
/// Main inputs of the player.
/// </summary>
[System.Serializable]
[Icon("Assets/Realistic Car Controller Pro/Resources/Editor Icons/RCCP_EditorIcon_Component.png")]
public class RCCP_Inputs {

    [Range(0f, 1f)] public float throttleInput = 0f;
    [Range(0f, 1f)] public float brakeInput = 0f;
    [Range(-1f, 1f)] public float steerInput = 0f;
    [Range(0f, 1f)] public float handbrakeInput = 0f;
    [Range(0f, 1f)] public float clutchInput = 0f;
    [Range(0f, 1f)] public float nosInput = 0f;

    public Vector2 mouseInput = new Vector2(0f, 0f);

    public RCCP_Inputs() { }

    public RCCP_Inputs(float throttleInput, float brakeInput, float steerInput, float handbrakeInput, float clutchInput, float nosInput, Vector2 mouseInput) {

        this.throttleInput = throttleInput;
        this.brakeInput = brakeInput;
        this.steerInput = steerInput;
        this.handbrakeInput = handbrakeInput;
        this.clutchInput = clutchInput;
        this.nosInput = nosInput;
        this.mouseInput = mouseInput;

    }

    public void Clear() {

        throttleInput = 0f;
        brakeInput = 0f;
        steerInput = 0f;
        handbrakeInput = 0f;
        clutchInput = 0f;
        nosInput = 0f;

        mouseInput = Vector2.zero;

    }

}
