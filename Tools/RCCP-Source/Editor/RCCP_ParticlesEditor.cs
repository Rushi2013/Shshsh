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

[CustomEditor(typeof(RCCP_Particles))]
public class RCCP_ParticlesEditor : Editor {

    RCCP_Particles prop;
    List<string> errorMessages = new List<string>();
    GUISkin skin;
    GUISkin orgskin;

    private void OnEnable() {

        skin = Resources.Load<GUISkin>("RCCP_Gui");

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_Particles)target;
        serializedObject.Update();

        if (orgskin == null)
            orgskin = GUI.skin;

        GUI.skin = skin;

        EditorGUILayout.HelpBox("Particles.", MessageType.Info, true);

        GUI.skin = orgskin;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("collisionFilter"), new GUIContent("Collision Filter", "Contact particles will be enabled on these layers."));
        GUI.skin = skin;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("contactSparklePrefab"), new GUIContent("Contact Sparkle Prefab", "Contact sparkle prefab will be used."));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("scratchSparklePrefab"), new GUIContent("Scratch Sparkle Prefab", "Scratch sparkle prefab will be used on scratches."));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("wheelSparklePrefab"), new GUIContent("Wheel Sparkle Prefab", "Wheel sparkle prefab will be used on flat wheels."));

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

}
