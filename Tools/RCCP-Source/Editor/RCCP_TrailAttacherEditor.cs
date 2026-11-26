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
using UnityEditor.Events;
using UnityEngine.Events;

[CustomEditor(typeof(RCCP_TrailerAttacher))]
public class RCCP_TrailAttacherEditor : Editor {

    RCCP_TrailerAttacher prop;
    GUISkin skin;

    private void OnEnable() {

        skin = Resources.Load<GUISkin>("RCCP_Gui");

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_TrailerAttacher)target;
        serializedObject.Update();
        GUI.skin = skin;

        EditorGUILayout.HelpBox("It's a trigger box collider only. If this box collider triggers with another box collider with this component, Configurable Joint of the target gameobject will be connected to the other one.", MessageType.Info, true);

        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.EndVertical();

        if (prop.GetComponentInParent<RCCP_CarController>(true) && !EditorUtility.IsPersistent(prop)) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            if (GUILayout.Button("Back"))
                Selection.activeGameObject = prop.GetComponentInParent<RCCP_OtherAddons>(true).gameObject;

            EditorGUILayout.EndVertical();

        }

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

        serializedObject.ApplyModifiedProperties();

    }

}
