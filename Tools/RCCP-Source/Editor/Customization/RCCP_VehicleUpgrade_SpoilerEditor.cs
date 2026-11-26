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

[CustomEditor(typeof(RCCP_VehicleUpgrade_SpoilerManager))]
public class RCCP_VehicleUpgrade_SpoilerEditor : Editor {

    RCCP_VehicleUpgrade_SpoilerManager prop;

    public override void OnInspectorGUI() {

        prop = (RCCP_VehicleUpgrade_SpoilerManager)target;
        serializedObject.Update();

        EditorGUILayout.HelpBox("All spoilers can be used under this manager. Each spoiler has target index and paintable renderer. Click 'Get All Spoilers' after editing spoilers.", MessageType.None);

        DrawDefaultInspector();

        EditorGUILayout.BeginVertical(GUI.skin.box);

        if (GUILayout.Button("Get Standard Shader Keyword")) {

            SetShaderID("_Color");
            EditorUtility.SetDirty(prop);

        }

        if (GUILayout.Button("Get RCCP Body Shader Keyword")) {

            SetShaderID("_DiffuseColor");
            EditorUtility.SetDirty(prop);

        }

        if (GUILayout.Button("Get URP Shader Keyword")) {

            SetShaderID("_BaseColor");
            EditorUtility.SetDirty(prop);

        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

        if (GUILayout.Button("Get All Spoilers"))
            prop.GetAllSpoilers();

        serializedObject.ApplyModifiedProperties();

    }

    private void SetShaderID(string shaderID) {

        if (prop.spoilers != null && prop.spoilers.Length > 0) {

            for (int i = 0; i < prop.spoilers.Length; i++) {

                if (prop.spoilers[i] != null)
                    prop.spoilers[i].id = shaderID;

            }

        }

    }

}
