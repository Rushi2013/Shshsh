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
/// Receiving inputs from UI Joystick.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/Mobile/RCCP UI Joystick")]
public class RCCP_UI_Joystick : RCCP_UIComponent, IDragHandler, IPointerUpHandler, IPointerDownHandler {

    /// <summary>
    /// Background sprite of the joystick.
    /// </summary>
    public RectTransform backgroundSprite;

    /// <summary>
    /// Handle sprite of the joystick.
    /// </summary>
    public RectTransform handleSprite;

    /// <summary>
    /// Current input of the joystick
    /// </summary>
    private Vector2 inputVector = Vector2.zero;

    /// <summary>
    /// Horizontal input
    /// </summary>
    public float inputHorizontal { get { return inputVector.x; } }

    /// <summary>
    /// Vertical input.
    /// </summary>
    public float inputVertical { get { return inputVector.y; } }

    /// <summary>
    /// Joystick position.
    /// </summary>
    private Vector2 joystickPosition = Vector2.zero;

    /// <summary>
    /// Reference camera.
    /// </summary>
    private readonly Camera _refCam;

    private void Start() {

        //  Getting joystick position.
        joystickPosition = RectTransformUtility.WorldToScreenPoint(_refCam, backgroundSprite.position);

    }

    private void OnEnable() {

        //  Make sure to reset position of the handle and input when enabling / disabling the joystick.
        inputVector = Vector2.zero;
        handleSprite.anchoredPosition = Vector2.zero;

    }

    public void OnDrag(PointerEventData eventData) {

        //  Getting direction of the drag and assigning the input as vector 2. And after that, assigning new position of the handle.
        Vector2 direction = eventData.position - joystickPosition;
        inputVector = (direction.magnitude > backgroundSprite.sizeDelta.x / 2f) ? direction.normalized : direction / (backgroundSprite.sizeDelta.x / 2f);
        handleSprite.anchoredPosition = (inputVector * backgroundSprite.sizeDelta.x / 2f) * 1f;

    }

    /// <summary>
    /// When up the joystick.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerUp(PointerEventData eventData) {

        inputVector = Vector2.zero;
        handleSprite.anchoredPosition = Vector2.zero;

    }

    /// <summary>
    /// When down the joystick.
    /// </summary>
    /// <param name="eventData"></param>
    public virtual void OnPointerDown(PointerEventData eventData) {

        //

    }

    private void OnDisable() {

        //  Make sure to reset position of the handle and input when enabling / disabling the joystick.
        inputVector = Vector2.zero;
        handleSprite.anchoredPosition = Vector2.zero;

    }

}
