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
/// UI upgrade button.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/Modification/RCCP UI Upgrade Button")]
public class RCCP_UI_Upgrade : RCCP_UIComponent {

    /// <summary>
    /// Upgrader class for this upgrader.
    /// </summary>
    public UpgradeClass upgradeClass = UpgradeClass.Engine;
    public enum UpgradeClass { Engine, Handling, Brake, Speed }

    /// <summary>
    /// Level count will be displayed on this text, if choosen.
    /// </summary>
    public Text levelText;

    private void OnEnable() {

        //  Finding the player vehicle.
        RCCP_CarController playerVehicle = RCCP_SceneManager.Instance.activePlayerVehicle;

        //  If no player vehicle found, return.
        if (!playerVehicle)
            return;

        //  If player vehicle doesn't have the customizer component, return.
        if (!playerVehicle.Customizer)
            return;

        if (!playerVehicle.Customizer.UpgradeManager)
            return;

        if (!levelText)
            return;

        switch (upgradeClass) {

            case UpgradeClass.Engine:
                levelText.text = (playerVehicle.Customizer.UpgradeManager.EngineLevel + 0).ToString();
                break;
            case UpgradeClass.Handling:
                levelText.text = (playerVehicle.Customizer.UpgradeManager.HandlingLevel + 0).ToString();
                break;
            case UpgradeClass.Brake:
                levelText.text = (playerVehicle.Customizer.UpgradeManager.BrakeLevel + 0).ToString();
                break;
            case UpgradeClass.Speed:
                levelText.text = (playerVehicle.Customizer.UpgradeManager.SpeedLevel + 0).ToString();
                break;

        }

    }

    public void OnClick() {

        //  Finding the player vehicle.
        RCCP_CarController playerVehicle = RCCP_SceneManager.Instance.activePlayerVehicle;

        //  If no player vehicle found, return.
        if (!playerVehicle)
            return;

        //  If player vehicle doesn't have the customizer component, return.
        if (!playerVehicle.Customizer)
            return;

        if (!playerVehicle.Customizer.UpgradeManager)
            return;

        switch (upgradeClass) {

            case UpgradeClass.Engine:
                playerVehicle.Customizer.UpgradeManager.UpgradeEngine();
                break;
            case UpgradeClass.Handling:
                playerVehicle.Customizer.UpgradeManager.UpgradeHandling();
                break;
            case UpgradeClass.Brake:
                playerVehicle.Customizer.UpgradeManager.UpgradeBrake();
                break;
            case UpgradeClass.Speed:
                playerVehicle.Customizer.UpgradeManager.UpgradeSpeed();
                break;

        }

        if (!levelText)
            return;

        switch (upgradeClass) {

            case UpgradeClass.Engine:
                levelText.text = (playerVehicle.Customizer.UpgradeManager.EngineLevel + 0).ToString();
                break;
            case UpgradeClass.Handling:
                levelText.text = (playerVehicle.Customizer.UpgradeManager.HandlingLevel + 0).ToString();
                break;
            case UpgradeClass.Brake:
                levelText.text = (playerVehicle.Customizer.UpgradeManager.BrakeLevel + 0).ToString();
                break;
            case UpgradeClass.Speed:
                levelText.text = (playerVehicle.Customizer.UpgradeManager.SpeedLevel + 0).ToString();
                break;

        }

    }

}
