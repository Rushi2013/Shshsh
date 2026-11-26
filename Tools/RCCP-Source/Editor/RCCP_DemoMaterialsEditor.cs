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

[CustomEditor(typeof(RCCP_DemoMaterials))]
public class RCCP_DemoMaterialsEditor : Editor {

    RCCP_DemoMaterials prop;

    public override void OnInspectorGUI() {

        prop = (RCCP_DemoMaterials)target;
        serializedObject.Update();

        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("To URP Shaders");

        if (GUILayout.Button("Select All Demo Materials For Converting To URP (Except vehicle body materials)"))
            EditorApplication.ExecuteMenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/URP/To URP/[Step 2] Convert All Demo Materials To URP");

        if (GUILayout.Button("Convert All Demo Vehicle Body Shaders To URP Shaders"))
            EditorApplication.ExecuteMenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/URP/To URP/[Step 3] Convert All Demo Vehicle Body Materials To URP");

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("To Builtin Shaders");

        if (GUILayout.Button("Select All Demo Materials For Converting To Builtin (Except vehicle body materials)"))
            EditorApplication.ExecuteMenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/URP/To Builtin/[Step 2] Convert All Demo Materials To Builtin");

        if (GUILayout.Button("Convert All Demo Vehicle Body Shaders To Builtin Shaders"))
            EditorApplication.ExecuteMenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/URP/To Builtin/[Step 3] Convert All Demo Vehicle Body Materials To Builtin");

        //if (GUILayout.Button("Get Default Shaders")) {

        //    for (int i = 0; i < prop.demoMaterials.Length; i++) {

        //        if (prop.demoMaterials[i] != null && prop.demoMaterials[i].material != null)
        //            Debug.Log(prop.demoMaterials[i].DefaultShader);

        //    }

        //}

        EditorGUILayout.LabelField("Created by Ekrem Bugra Ozdoganlar\nBoneCracker Games", EditorStyles.centeredGreyMiniLabel, GUILayout.MaxHeight(50f));

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

    }

}
