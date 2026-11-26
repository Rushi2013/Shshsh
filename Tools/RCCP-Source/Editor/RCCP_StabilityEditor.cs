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

[CustomEditor(typeof(RCCP_Stability))]
public class RCCP_StabilityEditor : Editor {

    RCCP_Stability prop;
    List<string> errorMessages = new List<string>();
    GUISkin skin;
    private Color guiColor;

    private void OnEnable() {

        guiColor = GUI.color;
        skin = Resources.Load<GUISkin>("RCCP_Gui");

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_Stability)target;
        serializedObject.Update();
        GUI.skin = skin;

        EditorGUILayout.HelpBox("ABS = Anti-skid braking system, ESP = Detects vehicle skidding movements, and actively counteracts them., TCS = Detects if a loss of traction occurs among the vehicle's wheels.", MessageType.Info, true);

        if (BehaviorSelected())
            GUI.color = Color.red;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("ABS"), new GUIContent("ABS", "Anti-skid braking system."));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ESP"), new GUIContent("ESP", "Detects vehicle skidding movements, and actively counteracts them."));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("TCS"), new GUIContent("TCS", "Detects if a loss of traction occurs among the vehicle's wheels."));

        GUI.color = guiColor;

        EditorGUILayout.Space();

        if (prop.ABS)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("engageABSThreshold"), new GUIContent("Engage ABS Threshold", "Sensitivity of the ABS."));
        if (prop.ESP)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("engageESPThreshold"), new GUIContent("Engage ESP Threshold", "Sensitivity of the ESP."));
        if (prop.TCS)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("engageTCSThreshold"), new GUIContent("Engage TCS Threshold", "Sensitivity of the TCS."));

        EditorGUILayout.Space();

        if (prop.ABS)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ABSIntensity"), new GUIContent("ABSIntensity", "ABS Intensity."));
        if (prop.ESP)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ESPIntensity"), new GUIContent("ESPIntensity", "ESP Intensity."));
        if (prop.TCS)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("TCSIntensity"), new GUIContent("TCSIntensity", "TCS Intensity."));

        EditorGUILayout.Space();

        GUI.enabled = false;

        if (prop.ABS)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ABSEngaged"), new GUIContent("ABS Engaged", "ABS engaged now?"));
        if (prop.ESP)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ESPEngaged"), new GUIContent("ESP Engaged", "ESP engaged now?"));
        if (prop.TCS)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("TCSEngaged"), new GUIContent("TCS Engaged", "TCS engaged now?"));

        GUI.enabled = true;

        EditorGUILayout.Space();

        if (BehaviorSelected())
            GUI.color = Color.red;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("steeringHelper"), new GUIContent("Steering Helper", "Steering helper based on steer angle and vehicle velocity."));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("tractionHelper"), new GUIContent("Traction Helper", "Avoids spins by reducing sideway stiffness of the front wheels if vehicle is skidding."));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("angularDragHelper"), new GUIContent("Angular Drag Helper", "Adjusts angular drag based on vehicle speed."));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("driftAngleLimiter"), new GUIContent("Drift Angle Limiter", "Adjusts angular velocity of the vehicle if drift angle exceeds the limit."));

        if (prop.driftAngleLimiter) {

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxDriftAngle"), new GUIContent("Max Drift Angle", "Maximum drift angle."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("driftAngleCorrectionFactor"), new GUIContent("Drift Angle Correction Factor", "Adjusts angular velocity of the vehicle by multiplying this factor with time."));
            EditorGUI.indentLevel--;

        }

        EditorGUILayout.Space();

        if (prop.steeringHelper)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("steerHelperStrength"), new GUIContent("Steering Helper Strength", "Steering helper intensity."));
        if (prop.tractionHelper)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tractionHelperStrength"), new GUIContent("Traction Helper Strength", "Traction helper intensity."));
        if (prop.angularDragHelper)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("angularDragHelperStrength"), new GUIContent("Angular Drag Helper Strength", "Angular drag helper intensity."));

        GUI.color = guiColor;

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

        if (BehaviorSelected())
            EditorGUILayout.HelpBox("Settings with red labels will be overridden by the selected behavior in RCCP_Settings", MessageType.None);

        prop.transform.localPosition = Vector3.zero;
        prop.transform.localRotation = Quaternion.identity;

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

        serializedObject.ApplyModifiedProperties();

    }

    private bool BehaviorSelected() {

        bool state = RCCP_Settings.Instance.overrideBehavior;

        if (prop.GetComponentInParent<RCCP_CarController>(true).ineffectiveBehavior)
            state = false;

        return state;

    }

}
