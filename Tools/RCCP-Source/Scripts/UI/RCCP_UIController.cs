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
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UI input (float) receiver from UI Button. 
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/Mobile/RCCP UI Controller")]
public class RCCP_UIController : RCCP_UIComponent, IPointerDownHandler, IPointerUpHandler {

    /// <summary>
    /// UI button for this controller.
    /// </summary>
    private Button button;

    /// <summary>
    /// Input value between 0f, and 1f.
    /// </summary>
    [Range(0f, 1f)]public float input = 0f;

    /// <summary>
    /// Sensitivity of the input. Will reach target input soon on higher values.
    /// </summary>
    [Min(0f)] public float sensitivity = 5f;

    /// <summary>
    /// Gravity of the input. Will reach to 0 soon on higher values. This value will be used when player doesn't push the button.
    /// </summary>
    [Min(0f)] public float gravity = 5f;

    /// <summary>
    /// Current pressing the button?
    /// </summary>
    public bool isPressing = false;

    private void Awake() {

        //  Getting button.
        button = GetComponent<Button>();

    }

    private void OnEnable() {

        //  Make sure to reset them.
        input = 0f;
        isPressing = false;

    }

    /// <summary>
    /// When down the button.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerDown(PointerEventData eventData) {

        isPressing = true;

    }

    /// <summary>
    /// When up the button.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerUp(PointerEventData eventData) {

        isPressing = false;

    }

    private void Update() {

        //  If button is not interactable, return wtih 0 input.
        if (button && !button.interactable) {

            isPressing = false;
            input = 0f;
            return;

        }

        //  If current pressing the button, increase it with sensitivity, and decrease it with gravity.
        if (isPressing)
            input += Time.deltaTime * sensitivity;
        else
            input -= Time.deltaTime * gravity;

        //  Clamping input between 0 - 1.
        if (input < 0f)
            input = 0f;

        if (input > 1f)
            input = 1f;

    }

    private void OnDisable() {

        //  Make sure to reset them.
        input = 0f;
        isPressing = false;

    }

}
