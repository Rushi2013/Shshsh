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
/// Customization demo used in the demo scene. Enables disables cameras and canvases.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Customization/RCCP Customization Demo")]
public class RCCP_CustomizationDemo : RCCP_GenericComponent {

    private static RCCP_CustomizationDemo instance;

    /// <summary>
    /// Instance of the class.
    /// </summary>
    public static RCCP_CustomizationDemo Instance {

        get {

            if (instance == null)
                instance = FindObjectOfType<RCCP_CustomizationDemo>();

            return instance;

        }

    }

    /// <summary>
    /// Current target vehicle to mod.
    /// </summary>
    public RCCP_CarController vehicle;

    /// <summary>
    /// Showroom camera to show the target vehicle while customizing.
    /// </summary>
    public RCCP_ShowroomCamera showroomCamera;

    /// <summary>
    /// RCCP Camera.
    /// </summary>
    public RCCP_Camera RCCCamera;

    /// <summary>
    /// Modification UI Canvas.
    /// </summary>
    public GameObject modificationCanvas;

    /// <summary>
    /// Modification location that will be used to transport the target vehicle.
    /// </summary>
    public Transform location;

    public void EnableCustomization(RCCP_CarController carController) {

        vehicle = carController;

        if (RCCCamera)
            RCCCamera.gameObject.SetActive(false);

        if (showroomCamera)
            showroomCamera.gameObject.SetActive(true);

        if (modificationCanvas)
            modificationCanvas.SetActive(true);

        if (location)
            RCCP.Transport(vehicle, location.position, location.rotation);

        RCCP.SetControl(vehicle, false);

    }

    public void DisableCustomization() {

        if (RCCCamera)
            RCCCamera.gameObject.SetActive(true);

        if (showroomCamera)
            showroomCamera.gameObject.SetActive(false);

        if (modificationCanvas)
            modificationCanvas.SetActive(false);

        if (vehicle)
            RCCP.SetControl(vehicle, true);

        vehicle = null;

    }

}
