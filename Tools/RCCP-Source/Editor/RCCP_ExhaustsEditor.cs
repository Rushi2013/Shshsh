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

[CustomEditor(typeof(RCCP_Exhausts))]
public class RCCP_ExhaustsEditor : Editor {

    RCCP_Exhausts prop;
    GUISkin skin;
    Color guiColor;

    private void OnEnable() {

        skin = Resources.Load<GUISkin>("RCCP_Gui");
        guiColor = GUI.color;

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_Exhausts)target;
        serializedObject.Update();
        GUI.skin = skin;

        EditorGUILayout.HelpBox("Exhausts.", MessageType.Info, true);

        if (prop.Exhaust != null) {

            for (int i = 0; i < prop.Exhaust.Length; i++) {

                if (prop.Exhaust[i] != null) {

                    EditorGUILayout.BeginHorizontal();

                    GameObject exhaust = prop.Exhaust[i].gameObject;
                    EditorGUILayout.ObjectField(exhaust, typeof(GameObject), true);

                    if (GUILayout.Button("Edit")) {

                        Selection.activeGameObject = exhaust;
                        SceneView.FrameLastActiveSceneView();

                    }

                    EditorGUILayout.EndHorizontal();

                }

            }

        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.EndVertical();

        if (!EditorUtility.IsPersistent(prop)) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            GUI.color = Color.green;

            if (GUILayout.Button("Create Exhaust")) {

                GameObject createdExhaust = CreateExhaust();
                prop.GetComponentInParent<RCCP_Exhausts>().GetAllExhausts();

                EditorUtility.SetDirty(prop);

                Selection.activeGameObject = createdExhaust;
                SceneView.FrameLastActiveSceneView();

            }

            GUI.color = guiColor;

            if (GUILayout.Button("Back"))
                Selection.activeGameObject = prop.GetComponentInParent<RCCP_OtherAddons>(true).gameObject;

            EditorGUILayout.EndVertical();

        }

        prop.transform.localPosition = Vector3.zero;
        prop.transform.localRotation = Quaternion.identity;

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

        serializedObject.ApplyModifiedProperties();

    }

    private GameObject CreateExhaust() {

        GameObject exhaust = Instantiate(RCCP_Settings.Instance.exhaustGas, prop.transform.position, prop.transform.rotation * Quaternion.Euler(0f, 180f, 0f), prop.transform);
        exhaust.name = RCCP_Settings.Instance.exhaustGas.name;
        exhaust.transform.localPosition = new Vector3(0f, 0f, -2f);

        return exhaust;

    }

}
