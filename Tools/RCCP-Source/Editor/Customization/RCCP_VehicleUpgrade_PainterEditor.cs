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

[CustomEditor(typeof(RCCP_VehicleUpgrade_Paint))]
public class RCCP_VehicleUpgrade_PainterEditor : Editor {

    RCCP_VehicleUpgrade_Paint prop;

    public override void OnInspectorGUI() {

        prop = (RCCP_VehicleUpgrade_Paint)target;
        serializedObject.Update();

        EditorGUILayout.HelpBox("All painters have target renderers and material index. If your vehicle has multiple paintable renderers, create new painter for each renderer and set their target material indexes. Click 'Get All Paints' after editing painters.", MessageType.None);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("paintMaterial"), new GUIContent("Paint Material"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("id"), new GUIContent("Shader Keyword"));

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Get Standard Shader Keyword"))
            prop.id = "_Color";

        if (GUILayout.Button("Get RCCP Body Shader Keyword"))
            prop.id = "_DiffuseColor";

        if (GUILayout.Button("Get URP Shader Keyword"))
            prop.id = "_BaseColor";

        EditorGUILayout.EndHorizontal();

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

        serializedObject.ApplyModifiedProperties();

    }

}
