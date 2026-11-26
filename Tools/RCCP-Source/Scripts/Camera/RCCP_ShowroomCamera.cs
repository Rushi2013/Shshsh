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
using UnityEngine.EventSystems;

/// <summary>
/// Showroom camera used on main menu.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Camera/RCCP Showroom Camera")]
public class RCCP_ShowroomCamera : RCCP_GenericComponent {

    /// <summary>
    /// Camera target. Usually our spawn point.
    /// </summary>
    public Transform target;

    /// <summary>
    /// Z Distance.
    /// </summary>
    [Min(0f)] public float distance = 8f;

    /// <summary>
    /// Auto orbiting now?
    /// </summary>
    [Space]
    public bool orbitingNow = true;

    /// <summary>
    /// Auto orbiting speed.
    /// </summary>
    [Min(0f)] public float orbitSpeed = 5f;

    /// <summary>
    /// Smooth orbiting.
    /// </summary>
    [Space]
    public bool smooth = true;

    /// <summary>
    /// Smooth orbiting factor.
    /// </summary>
    [Min(0f)] public float smoothingFactor = 5f;

    /// <summary>
    /// Minimum Y degree.
    /// </summary>
    [Space]
    public float minY = 5f;

    /// <summary>
    /// Maximum Y degree.
    /// </summary>
    public float maxY = 35f;

    /// <summary>
    /// Player is rotating the camera now?
    /// </summary>
    [Space]
    private bool draggingNow = false;

    /// <summary>
    /// Drag speed.
    /// </summary>
    [Min(0f)] public float dragSpeed = 10f;

    /// <summary>
    /// Orbit X.
    /// </summary>
    public float orbitX = 0f;

    /// <summary>
    /// Orbit Y.
    /// </summary>
    public float orbitY = 0f;

    private void Update() {

        // Receiving player inputs for setting orbit X and Y.
        Inputs();

    }

    private void LateUpdate() {

        // If there is no target, return.
        if (!target) {

            Debug.LogWarning("Camera target not found!");
            enabled = false;
            return;

        }

        // If auto orbiting is enabled, increase orbitX slowly with orbitSpeed factor.
        if (orbitingNow)
            orbitX += Time.deltaTime * orbitSpeed;

        //  Clamping orbit Y.
        orbitY = ClampAngle(orbitY, minY, maxY);

        // Calculating rotation and position of the camera.
        Quaternion rotation = Quaternion.Euler(orbitY, orbitX, 0);
        Vector3 position = rotation * new Vector3(0f, 0f, -distance) + target.transform.position;

        // Setting position and rotation of the camera.
        if (!smooth) {

            transform.rotation = rotation;
            transform.position = position;

        } else {

            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.unscaledDeltaTime * 10f);
            transform.position = Vector3.Lerp(transform.position, position, Time.unscaledDeltaTime * 10f);

        }

    }

    /// <summary>
    /// Receiving inputs for dragging the camera.
    /// </summary>
    private void Inputs() {

        RCCP_Inputs inputs = RCCP_InputManager.Instance.inputs;

        if (draggingNow) {

            //orbitX += inputs.orbitX * dragSpeed * Time.deltaTime;
            //orbitY -= inputs.orbitY * dragSpeed * Time.deltaTime;

        }

    }

    public void SetDrag(bool state) {

        draggingNow = state;

    }

    private float ClampAngle(float angle, float min, float max) {

        if (angle < -360)
            angle += 360;
        if (angle > 360)
            angle -= 360;

        return Mathf.Clamp(angle, min, max);

    }

    public void ToggleAutoRotation(bool state) {

        orbitingNow = state;

    }

    public void OnDrag(PointerEventData pointerData) {

        // Receiving drag input from UI.
        orbitX += pointerData.delta.x * dragSpeed * .02f;
        orbitY -= pointerData.delta.y * dragSpeed * .02f;

    }

}
