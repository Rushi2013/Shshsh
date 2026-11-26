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
/// Manager for upgradable wheels.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Customization/RCCP Vehicle Upgrade Wheel Manager")]
public class RCCP_VehicleUpgrade_WheelManager : RCCP_UpgradeComponent, IRCCP_UpgradeComponent {

    /// <summary>
    /// Current wheel index.
    /// </summary>
    [Min(-1)] public int wheelIndex = -1;

    /// <summary>
    /// Default wheel.
    /// </summary>
    private GameObject DefaultWheelObj {

        get {

            //  Getting default wheelmodel.
            if (defaultWheelObj == null) {

                RCCP_WheelCollider foundWheel = CarController.GetComponentInChildren<RCCP_WheelCollider>();
                GameObject defaultWheelRef = null;

                if (foundWheel != null && foundWheel.wheelModel != null)
                    defaultWheelRef = CarController.GetComponentInChildren<RCCP_WheelCollider>().wheelModel.gameObject;

                if (defaultWheelRef != null) {

                    defaultWheelObj = Instantiate(defaultWheelRef, transform);
                    defaultWheelObj.transform.localPosition = Vector3.zero;
                    defaultWheelObj.transform.localRotation = Quaternion.identity;
                    defaultWheelObj.transform.localScale = Vector3.one;
                    defaultWheelObj.SetActive(false);

                }

            }

            return defaultWheelObj;

        }

    }

    private GameObject defaultWheelObj;

    /// <summary>
    /// Initializing.
    /// </summary>
    public void Initialize() {

        GameObject defaultWheel = DefaultWheelObj;
        defaultWheel.SetActive(false);

        // If last selected wheel found, change the wheel.
        wheelIndex = Loadout.wheel;

        if (wheelIndex != -1)
            ChangeWheels(RCCP_ChangableWheels.Instance.wheels[wheelIndex].wheel, true);

    }

    /// <summary>
    /// Changes the wheel with the target wheel index.
    /// </summary>
    /// <param name="wheelIndex"></param>
    public void UpdateWheel(int index) {

        //  Setting wheel index.
        wheelIndex = index;

        //  Return if wheel index is not set.
        if (wheelIndex == -1)
            return;

        //  Checking the RCCP_ChangableWheels for selected wheel index.
        if (RCCP_ChangableWheels.Instance.wheels[wheelIndex] == null) {

            Debug.LogError("RCCP_ChangableWheels doesn't have that wheelIndex numbered " + wheelIndex.ToString());
            return;

        }

        //  Changing the wheels.
        ChangeWheels(RCCP_ChangableWheels.Instance.wheels[wheelIndex].wheel, true);

        //  Refreshing the loadout.
        Refresh(this);

        //  Saving the loadout.
        if (CarController.Customizer.autoSave)
            Save();

    }

    /// <summary>
    /// Changes the wheel with the target wheel index.
    /// </summary>
    /// <param name="wheelIndex"></param>
    public void UpdateWheelWithoutSave(int index) {

        //  Setting wheel index.
        wheelIndex = index;

        //  Return if wheel index is not set.
        if (wheelIndex == -1)
            return;

        //  Checking the RCCP_ChangableWheels for selected wheel index.
        if (RCCP_ChangableWheels.Instance.wheels[wheelIndex] == null) {

            Debug.LogError("RCCP_ChangableWheels doesn't have that wheelIndex numbered " + wheelIndex.ToString());
            return;

        }

        //  Changing the wheels.
        ChangeWheels(RCCP_ChangableWheels.Instance.wheels[wheelIndex].wheel, true);

    }

    /// <summary>
    /// Change wheel models. You can find your wheel models array in Tools --> BCG --> RCCP --> Configure Changable Wheels.
    /// </summary>
    public void ChangeWheels(GameObject wheel, bool applyRadius) {

        //  Return if no wheel or wheel is deactivated.
        if (!wheel || (wheel && !wheel.activeSelf))
            return;

        //  Return if no any wheelcolliders found.
        if (CarController.AllWheelColliders == null)
            return;

        //  Return if no any wheelcolliders found.
        if (CarController.AllWheelColliders.Length < 1)
            return;

        //  Looping all wheelcolliders.
        for (int i = 0; i < CarController.AllWheelColliders.Length; i++) {

            RCCP_WheelCollider wheelCollider = CarController.AllWheelColliders[i];

            if (wheelCollider != null && wheelCollider.wheelModel != null) {

                //  Disabling all child models of the wheel.
                foreach (Transform t in wheelCollider.wheelModel.GetComponentInChildren<Transform>())
                    t.gameObject.SetActive(false);

                //  Instantiating new wheel model.
                GameObject newWheel = Instantiate(wheel, wheelCollider.transform.position, wheelCollider.transform.rotation, wheelCollider.wheelModel);
                newWheel.transform.localPosition = Vector3.zero;
                newWheel.transform.localRotation = Quaternion.identity;
                newWheel.SetActive(true);

                //  If wheel is at right side, multiply scale X by -1 for symetry.
                if (wheelCollider.transform.localPosition.x > 0f)
                    newWheel.transform.localScale = new Vector3(newWheel.transform.localScale.x * -1f, newWheel.transform.localScale.y, newWheel.transform.localScale.z);

                //  If apply radius is set to true, calculate the radius.
                if (applyRadius)
                    wheelCollider.WheelCollider.radius = RCCP_GetBounds.MaxBoundsExtent(wheel.transform);

            }

        }

    }

    /// <summary>
    /// Restores the settings to default.
    /// </summary>
    public void Restore() {

        wheelIndex = 0;

        //  Changing the wheels.
        if (DefaultWheelObj == null) {

            ChangeWheels(RCCP_ChangableWheels.Instance.wheels[wheelIndex].wheel, true);

        } else {

            DefaultWheelObj.SetActive(true);

            foreach (Transform t in DefaultWheelObj.transform)
                t.gameObject.SetActive(true);

            ChangeWheels(DefaultWheelObj, true);

            DefaultWheelObj.SetActive(false);
        }

    }

}
