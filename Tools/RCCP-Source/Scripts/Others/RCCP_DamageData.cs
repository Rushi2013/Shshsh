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
using System.Threading.Tasks;
using System.Threading;

[Icon("Assets/Realistic Car Controller Pro/Resources/Editor Icons/RCCP_EditorIcon_Component.png")]
public class RCCP_DamageData {

    public static void SaveDamageRaw(RCCP_CarController carController, string saveName) {

        RCCP_Damage damageComponent = carController.Damage;

        if (damageComponent == null) {

            Debug.LogError("Damage component couldn't found on the vehicle named: " + carController.transform.name + "!");
            return;

        }

        if (damageComponent.damageData == null)
            damageComponent.damageData = new RCCP_Damage.DamageData();

        damageComponent.damageData.Initialize(damageComponent);

        PlayerPrefs.SetString(saveName + "_DamageData", JsonUtility.ToJson(damageComponent.damageData));

        Debug.Log("Damage Saved For " + damageComponent.transform.root.name);

    }

    public static void LoadDamageRaw(RCCP_CarController carController, string saveName) {

        RCCP_Damage damageComponent = carController.Damage;

        if (damageComponent == null) {

            Debug.LogError("Damage component couldn't found on the vehicle named: " + carController.transform.name + "!");
            return;

        }

        RCCP_Damage.DamageData damageData = JsonUtility.FromJson<RCCP_Damage.DamageData>(PlayerPrefs.GetString(saveName + "_DamageData"));

        if (damageData == null) {

            Debug.LogError("Damage data couldn't found on the vehicle named: " + carController.transform.name + "!");
            return;

        }

        if (damageComponent.damageData == null)
            damageComponent.damageData = new RCCP_Damage.DamageData();

        damageComponent.originalMeshData = damageData.originalMeshData;
        damageComponent.originalWheelData = damageData.originalWheelData;
        damageComponent.damagedMeshData = damageData.damagedMeshData;
        damageComponent.damagedWheelData = damageData.damagedWheelData;

        if (damageComponent.lights != null && damageComponent.lights.Length >= 1) {

            for (int i = 0; i < damageData.lightData.Length; i++)
                damageComponent.lights[i].broken = damageData.lightData[i];

        }

        damageComponent.repaired = false;
        damageComponent.repairNow = false;
        damageComponent.deformingNow = true;
        damageComponent.deformed = false;

        damageComponent.CheckDamageRaw();
        Debug.Log("Damage data loaded on vehicle named: " + carController.transform.name);

    }

    ////////////////////////////

    public static async void SaveDamage(RCCP_CarController carController, string saveName) {

        RCCP_Damage damageComponent = carController.Damage;

        if (damageComponent == null) {

            Debug.LogError("Damage component couldn't found on the vehicle named: " + carController.transform.name + "!");
            return;

        }

        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        await Task.Run(() => {

            if (damageComponent.damageData == null)
                damageComponent.damageData = new RCCP_Damage.DamageData();

            damageComponent.damageData.Initialize(damageComponent);

        }, cancellationTokenSource.Token);

        if (cancellationTokenSource.IsCancellationRequested)
            return;

        PlayerPrefs.SetString(saveName + "_DamageData", JsonUtility.ToJson(damageComponent.damageData));

        Debug.Log("Damage Saved For " + damageComponent.transform.root.name);

    }

    public static async void LoadDamage(RCCP_CarController carController, string saveName) {

        RCCP_Damage damageComponent = carController.Damage;

        if (damageComponent == null) {

            Debug.LogError("Damage component couldn't found on the vehicle named: " + carController.transform.name + "!");
            return;

        }

        RCCP_Damage.DamageData damageData = JsonUtility.FromJson<RCCP_Damage.DamageData>(PlayerPrefs.GetString(saveName + "_DamageData"));

        if (damageData == null) {

            Debug.LogError("Damage data couldn't found on the vehicle named: " + carController.transform.name + "!");
            return;

        }

        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        await Task.Run(() => {

            if (damageComponent.damageData == null)
                damageComponent.damageData = new RCCP_Damage.DamageData();

            damageComponent.originalMeshData = damageData.originalMeshData;
            damageComponent.originalWheelData = damageData.originalWheelData;
            damageComponent.damagedMeshData = damageData.damagedMeshData;
            damageComponent.damagedWheelData = damageData.damagedWheelData;

            if (damageComponent.lights != null && damageComponent.lights.Length >= 1) {

                for (int i = 0; i < damageData.lightData.Length; i++)
                    damageComponent.lights[i].broken = damageData.lightData[i];

            }

            damageComponent.repaired = false;
            damageComponent.repairNow = false;
            damageComponent.deformingNow = true;
            damageComponent.deformed = false;

        }, cancellationTokenSource.Token);

        if (cancellationTokenSource.IsCancellationRequested)
            return;

        damageComponent.CheckDamage();
        Debug.Log("Damage data loaded on vehicle named: " + carController.transform.name);

    }

}
