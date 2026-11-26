//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright © 2014 - 2024 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RCCP_VehicleUpgrade_UpgradeManager))]
public class RCCP_VehicleUpgrade_UpgradeManagerEditor : Editor {

    RCCP_VehicleUpgrade_UpgradeManager prop;
    Color guiColor;

    private void OnEnable() {

        guiColor = GUI.color;

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_VehicleUpgrade_UpgradeManager)target;
        serializedObject.Update();

        EditorGUILayout.HelpBox("Upgrade manager that contains engine torque, brake torque, handling stability, and maximum speed.", MessageType.None);

        RCCP_VehicleUpgrade_Engine engine = prop.GetComponentInChildren<RCCP_VehicleUpgrade_Engine>(true);
        RCCP_VehicleUpgrade_Brake brake = prop.GetComponentInChildren<RCCP_VehicleUpgrade_Brake>(true);
        RCCP_VehicleUpgrade_Handling handling = prop.GetComponentInChildren<RCCP_VehicleUpgrade_Handling>(true);
        RCCP_VehicleUpgrade_Speed speed = prop.GetComponentInChildren<RCCP_VehicleUpgrade_Speed>(true);

        EditorGUILayout.BeginVertical(GUI.skin.box);

        GUI.enabled = true;
        GUI.color = guiColor;

        if (engine == null) {

            GUI.enabled = false;
            GUI.color = Color.red;

        }

        if (GUILayout.Button("Engine Upgrade"))
            Selection.activeGameObject = engine.gameObject;

        GUI.enabled = true;
        GUI.color = guiColor;

        if (brake == null) {

            GUI.enabled = false;
            GUI.color = Color.red;

        }

        if (GUILayout.Button("Brake Upgrade"))
            Selection.activeGameObject = brake.gameObject;

        GUI.enabled = true;
        GUI.color = guiColor;

        if (handling == null) {

            GUI.enabled = false;
            GUI.color = Color.red;

        }

        if (GUILayout.Button("Handling Upgrade"))
            Selection.activeGameObject = handling.gameObject;

        GUI.enabled = true;
        GUI.color = guiColor;

        if (speed == null) {

            GUI.enabled = false;
            GUI.color = Color.red;

        }

        if (GUILayout.Button("Speed Upgrade"))
            Selection.activeGameObject = speed.gameObject;

        GUI.enabled = true;
        GUI.color = guiColor;

        EditorGUILayout.EndVertical();

        EditorGUILayout.Separator();

        EditorGUILayout.BeginVertical(GUI.skin.box);

        if (engine == null) {

            if (GUILayout.Button("Create Engine Upgrade"))
                CreateEngineUpgrade();

        }

        if (brake == null) {

            if (GUILayout.Button("Create Brake Upgrade"))
                CreateBrakeUpgrade();

        }

        if (handling == null) {

            if (GUILayout.Button("Create Handling Upgrade"))
                CreateHandlingUpgrade();

        }

        if (speed == null) {

            if (GUILayout.Button("Create Speed Upgrade"))
                CreateSpeedUpgrade();

        }

        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();

    }

    private void CreateEngineUpgrade() {

        RCCP_VehicleUpgrade_Engine newGO = new GameObject("Upgrade_Engine").AddComponent<RCCP_VehicleUpgrade_Engine>();
        newGO.transform.SetParent(prop.transform);
        newGO.transform.localPosition = Vector3.zero;
        newGO.transform.localRotation = Quaternion.identity;

    }

    private void CreateBrakeUpgrade() {

        RCCP_VehicleUpgrade_Brake newGO = new GameObject("Upgrade_Brake").AddComponent<RCCP_VehicleUpgrade_Brake>();
        newGO.transform.SetParent(prop.transform);
        newGO.transform.localPosition = Vector3.zero;
        newGO.transform.localRotation = Quaternion.identity;

    }

    private void CreateHandlingUpgrade() {

        RCCP_VehicleUpgrade_Handling newGO = new GameObject("Upgrade_Handling").AddComponent<RCCP_VehicleUpgrade_Handling>();
        newGO.transform.SetParent(prop.transform);
        newGO.transform.localPosition = Vector3.zero;
        newGO.transform.localRotation = Quaternion.identity;

    }

    private void CreateSpeedUpgrade() {

        RCCP_VehicleUpgrade_Speed newGO = new GameObject("Upgrade_Speed").AddComponent<RCCP_VehicleUpgrade_Speed>();
        newGO.transform.SetParent(prop.transform);
        newGO.transform.localPosition = Vector3.zero;
        newGO.transform.localRotation = Quaternion.identity;

    }

}
