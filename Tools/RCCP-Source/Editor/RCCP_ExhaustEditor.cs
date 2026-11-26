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

[CustomEditor(typeof(RCCP_Exhaust))]
public class RCCP_ExhaustEditor : Editor {

    RCCP_Exhaust prop;
    GUISkin skin;
    private Color guiColor;

    private void OnEnable() {

        guiColor = GUI.color;
        skin = Resources.Load<GUISkin>("RCCP_Gui");

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_Exhaust)target;
        serializedObject.Update();
        GUI.skin = skin;

        DrawDefaultInspector();

        if (!EditorUtility.IsPersistent(prop)) {

            if (GUILayout.Button("Duplicate To Other Side")) {

                GameObject duplicated = Instantiate(prop.gameObject, prop.GetComponentInParent<RCCP_CarController>(true).GetComponentInChildren<RCCP_Exhausts>(true).transform);

                duplicated.transform.name = prop.transform.name + "_D";
                duplicated.transform.localPosition = new Vector3(-duplicated.transform.localPosition.x, duplicated.transform.localPosition.y, duplicated.transform.localPosition.z);
                duplicated.transform.localRotation = prop.transform.localRotation;

                prop.GetComponentInParent<RCCP_Exhausts>(true).GetAllExhausts();

                EditorUtility.SetDirty(prop);

                Selection.activeGameObject = duplicated;

            }

            if (GUILayout.Button("Back"))
                Selection.activeGameObject = prop.GetComponentInParent<RCCP_CarController>(true).GetComponentInChildren<RCCP_Exhausts>(true).gameObject;

            if (prop.GetComponentInParent<RCCP_CarController>(true).checkComponents)
                Selection.activeGameObject = prop.GetComponentInParent<RCCP_CarController>(true).gameObject;

        }

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

        serializedObject.ApplyModifiedProperties();

    }

}
