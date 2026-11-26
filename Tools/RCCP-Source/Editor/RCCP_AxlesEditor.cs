//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright © 2014 - 2024 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(RCCP_Axles))]
public class RCCP_AxlesEditor : Editor {

    RCCP_Axles prop;
    List<string> errorMessages = new List<string>();
    GUISkin skin;
    private Color guiColor;

    private void OnEnable() {

        guiColor = GUI.color;
        skin = Resources.Load<GUISkin>("RCCP_Gui");

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_Axles)target;
        serializedObject.Update();
        GUI.skin = skin;

        EditorGUILayout.HelpBox("All axles will be connected to this manager. Create / remove / edit any axle.", MessageType.Info, true);

        RCCP_Axle[] allAxles = prop.GetComponentsInChildren<RCCP_Axle>(true);

        if (allAxles != null && allAxles.Length > 0) {

            for (int i = 0; i < allAxles.Length; i++) {

                if (allAxles[i] != null) {

                    if (GUILayout.Button(allAxles[i].transform.name))
                        Selection.activeGameObject = allAxles[i].gameObject;

                }

            }

        }

        EditorGUILayout.Space();

        GUI.color = Color.green;

        if (GUILayout.Button("Create New Axle")) {

            bool decision = EditorUtility.DisplayDialog("Creating a new axle", "Are you sure want to create a new axle?", "Yes", "No");

            if (decision) {

                CreateNewAxle();
                EditorUtility.SetDirty(prop);

            }

        }

        GUI.color = guiColor;

        EditorGUILayout.Space();

        CheckMisconfig();

        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.EndVertical();

        if (!EditorUtility.IsPersistent(prop)) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            if (GUILayout.Button("Back"))
                Selection.activeGameObject = prop.GetComponentInParent<RCCP_CarController>(true).gameObject;

            if (prop.GetComponentInParent<RCCP_CarController>(true).checkComponents) {

                prop.GetComponentInParent<RCCP_CarController>(true).checkComponents = false;

                if (errorMessages.Count > 0) {

                    if (EditorUtility.DisplayDialog("Errors found", errorMessages.Count + " Errors found!", "Cancel", "Check"))
                        Selection.activeGameObject = prop.GetComponentInParent<RCCP_CarController>(true).gameObject;

                } else {

                    Selection.activeGameObject = prop.GetComponentInParent<RCCP_CarController>(true).gameObject;
                    Debug.Log("No errors found");

                }

            }

            EditorGUILayout.EndVertical();

        }

        prop.transform.localPosition = Vector3.zero;
        prop.transform.localRotation = Quaternion.identity;

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

        serializedObject.ApplyModifiedProperties();

    }

    private void CreateNewAxle() {

        GameObject newAxle = new GameObject("RCCP_Axle_New");
        newAxle.transform.SetParent(prop.transform, false);
        newAxle.AddComponent<RCCP_Axle>();

    }

    private void CheckMisconfig() {

        bool completeSetup = true;
        errorMessages.Clear();

        RCCP_Axle[] allAxles = prop.GetComponentsInChildren<RCCP_Axle>(true);

        for (int i = 0; i < allAxles.Length; i++) {

            if (allAxles[i] != null) {

                if (allAxles[i].leftWheelCollider == null || allAxles[i].leftWheelModel == null || allAxles[i].rightWheelCollider == null || allAxles[i].rightWheelModel == null)
                    errorMessages.Add("Wheel models or colliders are not selected for " + allAxles[i].gameObject.name + "!");

            }

        }

        if (errorMessages.Count > 0)
            completeSetup = false;

        prop.completeSetup = completeSetup;

        if (!completeSetup)
            EditorGUILayout.HelpBox("Errors found!", MessageType.Error, true);

        GUI.color = Color.red;

        for (int i = 0; i < errorMessages.Count; i++) {

            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(errorMessages[i]);
            EditorGUILayout.EndVertical();

        }

        GUI.color = guiColor;

    }

}
