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

[CustomEditor(typeof(RCCP_AeroDynamics))]
public class RCCP_AeroDynamicsEditor : Editor {

    RCCP_AeroDynamics prop;
    GUISkin skin;

    private void OnEnable() {

        skin = Resources.Load<GUISkin>("RCCP_Gui");

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_AeroDynamics)target;
        serializedObject.Update();
        GUI.skin = skin;

        Transform com = prop.COM;

        EditorGUILayout.HelpBox("Manages the dynamics of the vehicle.", MessageType.Info, true);

        if (GUILayout.Button(new GUIContent("COM", "Centre of mass. Must be placed correctly. You can google it for vehicles to see which locations are suitable.")))
            Selection.activeGameObject = prop.COM.gameObject;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("dynamicCOM"), new GUIContent("Dynamic COM", "Dynamic COM position."));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("downForce"), new GUIContent("Downforce", "Downforce will be applied to the vehicle related with vehicle speed."));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("airResistance"), new GUIContent("Air Resistance", "Decceleration will be applied to the vehicle with speed related. Vehicle will slow down on higher values."));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("wheelResistance"), new GUIContent("Wheel Resistance", "Decceleration will be applied to the vehicle with speed related. Vehicle will slow down on higher values."));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoReset"), new GUIContent("Auto Reset", "Auto resets the vehicle if upside down."));

        if (prop.autoReset)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoResetTime"), new GUIContent("Auto Reset Timer", "Auto reset timer limit. Vehicle will reset itself within this time."));

        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.EndVertical();

        if (!EditorUtility.IsPersistent(prop)) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            if (GUILayout.Button("Back"))
                Selection.activeGameObject = prop.GetComponentInParent<RCCP_CarController>(true).gameObject;

            EditorGUILayout.EndVertical();

        }

        prop.transform.localPosition = Vector3.zero;
        prop.transform.localRotation = Quaternion.identity;

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

        serializedObject.ApplyModifiedProperties();

    }

}
