//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright © 2014 - 2024 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

#pragma warning disable 0414

using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Mobile UI Drag used for orbiting RCCP Camera.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/Mobile/RCCP UI Drag")]
public class RCCP_UI_Drag : RCCP_UIComponent, IDragHandler, IEndDragHandler {

    /// <summary>
    /// While dragging.
    /// </summary>
    /// <param name="data"></param>
    public void OnDrag(PointerEventData data) {

        RCCP_Camera camera = RCCP_SceneManager.Instance.activePlayerCamera;

        if (camera) {

            camera.OnDrag(data);
            camera.orbitHolding = true;

        }

    }

    public void OnEndDrag(PointerEventData data) {

        RCCP_Camera camera = RCCP_SceneManager.Instance.activePlayerCamera;

        if (camera)
            camera.orbitHolding = false;

    }

}
