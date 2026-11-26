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
using TMPro;

/// <summary>
/// Sets the target gearbox type.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/RCCP UI Set Gearbox")]
public class RCCP_UI_SetGearbox : RCCP_UIComponent {

    private RCCP_CarController currentCarController;

    private void Awake() {

        RCCP_Events.OnVehicleChangedToVehicle += RCCP_Events_OnVehicleChanged;

    }

    private void RCCP_Events_OnVehicleChanged(RCCP_CarController carController) {

        currentCarController = carController;
        OnEnable();

    }

    private void OnEnable() {

        currentCarController = RCCP_SceneManager.Instance.activePlayerVehicle;

        if (!currentCarController)
            return;

        if (!currentCarController.Gearbox)
            return;

        TMP_Dropdown dropdown = gameObject.GetComponent<TMP_Dropdown>();

        if (dropdown) {

            switch (currentCarController.Gearbox.transmissionType) {

                case RCCP_Gearbox.TransmissionType.Automatic:

                    dropdown.SetValueWithoutNotify(0);
                    break;

                case RCCP_Gearbox.TransmissionType.Automatic_DNRP:

                    dropdown.SetValueWithoutNotify(1);
                    break;

                case RCCP_Gearbox.TransmissionType.Manual:

                    dropdown.SetValueWithoutNotify(2);
                    break;

            }

        }

    }

    /// <summary>
    /// Sets the target gearbox type with index. 0 is auto, 1 is dnrp, 2 is manual.
    /// </summary>
    /// <param name="index"></param>
    public void GetGearbox(int index) {

        currentCarController = RCCP_SceneManager.Instance.activePlayerVehicle;

        if (!currentCarController)
            return;

        if (!currentCarController.Gearbox)
            return;

        switch (index) {

            case 0:
                currentCarController.Gearbox.transmissionType = RCCP_Gearbox.TransmissionType.Automatic;
                break;

            case 1:
                currentCarController.Gearbox.transmissionType = RCCP_Gearbox.TransmissionType.Automatic_DNRP;
                break;

            case 2:
                currentCarController.Gearbox.transmissionType = RCCP_Gearbox.TransmissionType.Manual;
                break;

        }

    }

    private void OnDestroy() {

        RCCP_Events.OnVehicleChangedToVehicle -= RCCP_Events_OnVehicleChanged;

    }

}
