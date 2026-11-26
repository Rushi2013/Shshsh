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
/// RCCP UI Canvas that manages the event systems, panels, gauges, images and texts related to the vehicle and player.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/RCCP UI Customizer")]
public class RCCP_UI_Customizer : RCCP_UIComponent {

    [Header("Customization Panels")]
    public GameObject paints;        //  Painting panel.
    public GameObject wheels;        //  Wheels panel.
    public GameObject customization;      //  Customization panel.
    public GameObject upgrades;      //  Upgrades panel.
    public GameObject spoilers;       //  Spoilers panel.
    public GameObject sirens;     //  Sirens panel.
    public GameObject decals;     //  Decals panel.
    public GameObject neons;     //  Neons panel.

    [Header("Customization Buttons")]
    public Button paintsButton;        //  Painting button.
    public Button wheelsButton;        //  Wheels button.
    public Button customizationButton;      //  Customization button.
    public Button upgradesButton;      //  Upgrades button.
    public Button spoilersButton;       //  Spoilers button.
    public Button sirensButton;     //  Sirens button.
    public Button decalsButton;     //  Decals button.
    public Button neonsButton;     //  Neons button.

    public void OpenCustomizationPanel(GameObject activeMenu) {

        CloseCustomizationPanels();

        if (activeMenu)
            activeMenu.SetActive(true);

    }

    public void CloseCustomizationPanels() {

        if (paints)
            paints.SetActive(false);

        if (wheels)
            wheels.SetActive(false);

        if (customization)
            customization.SetActive(false);

        if (upgrades)
            upgrades.SetActive(false);

        if (spoilers)
            spoilers.SetActive(false);

        if (sirens)
            sirens.SetActive(false);

        if (decals)
            decals.SetActive(false);

        if (neons)
            neons.SetActive(false);

    }

    private void Update() {

        if (paintsButton)
            paintsButton.interactable = false;

        if (wheelsButton)
            wheelsButton.interactable = false;

        if (customizationButton)
            customizationButton.interactable = false;

        if (upgradesButton)
            upgradesButton.interactable = false;

        if (spoilersButton)
            spoilersButton.interactable = false;

        if (sirensButton)
            sirensButton.interactable = false;

        if (decalsButton)
            decalsButton.interactable = false;

        if (neonsButton)
            neonsButton.interactable = false;

        if (!RCCP_SceneManager.Instance)
            return;

        if (!RCCP_SceneManager.Instance.activePlayerVehicle)
            return;

        if (!RCCP_SceneManager.Instance.activePlayerVehicle.Customizer)
            return;

        if (paintsButton)
            paintsButton.interactable = RCCP_SceneManager.Instance.activePlayerVehicle.Customizer.PaintManager;

        if (wheelsButton)
            wheelsButton.interactable = RCCP_SceneManager.Instance.activePlayerVehicle.Customizer.WheelManager;

        if (customizationButton)
            customizationButton.interactable = RCCP_SceneManager.Instance.activePlayerVehicle.Customizer.CustomizationManager;

        if (upgradesButton)
            upgradesButton.interactable = RCCP_SceneManager.Instance.activePlayerVehicle.Customizer.UpgradeManager;

        if (spoilersButton)
            spoilersButton.interactable = RCCP_SceneManager.Instance.activePlayerVehicle.Customizer.SpoilerManager;

        if (sirensButton)
            sirensButton.interactable = RCCP_SceneManager.Instance.activePlayerVehicle.Customizer.SirenManager;

        if (decalsButton)
            decalsButton.interactable = RCCP_SceneManager.Instance.activePlayerVehicle.Customizer.DecalManager;

        if (neonsButton)
            neonsButton.interactable = RCCP_SceneManager.Instance.activePlayerVehicle.Customizer.NeonManager;

    }

}
