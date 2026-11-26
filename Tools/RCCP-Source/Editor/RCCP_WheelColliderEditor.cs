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
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(RCCP_WheelCollider))]
[CanEditMultipleObjects]
public class RCCP_WheelColliderEditor : Editor {

    RCCP_WheelCollider prop;
    List<string> errorMessages = new List<string>();
    GUISkin skin;
    private Color guiColor;

    private void OnEnable() {

        guiColor = GUI.color;
        skin = Resources.Load<GUISkin>("RCCP_Gui");

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_WheelCollider)target;
        serializedObject.Update();
        GUI.skin = skin;

        EditorGUILayout.HelpBox("A circular object that revolves on an axle and is fixed below a vehicle or other object to enable it to move easily over the ground. Just kidding :)", MessageType.Info, true);

        DrawDefaultInspector();

        if (BehaviorSelected())
            GUI.color = Color.red;

        GUI.color = guiColor;

        CheckMisconfig();

        if (!EditorUtility.IsPersistent(prop)) {

            if (GUILayout.Button("Back"))
                Selection.activeGameObject = prop.GetComponentInParent<RCCP_CarController>(true).gameObject;

            if (!EditorApplication.isPlaying && prop.connectedAxle != null && prop.connectedAxle.autoAlignWheelColliders)
                prop.AlignWheel();

        }

        if (BehaviorSelected())
            EditorGUILayout.HelpBox("Settings with red labels and frictions of the wheelcolliders will be overridden by the selected behavior in RCCP_Settings", MessageType.None);

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

        serializedObject.ApplyModifiedProperties();

    }

    private void CheckMisconfig() {

        bool completeSetup = true;
        errorMessages.Clear();

        if (!prop.connectedAxle)
            errorMessages.Add("Axle not selected");

        if (!prop.wheelModel)
            errorMessages.Add("Wheel model not selected");

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

    private bool BehaviorSelected() {

        bool state = RCCP_Settings.Instance.overrideBehavior;

        if (prop.GetComponentInParent<RCCP_CarController>(true).ineffectiveBehavior)
            state = false;

        return state;

    }

}
