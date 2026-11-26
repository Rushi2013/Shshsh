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

[CustomEditor(typeof(RCCP_VehicleUpgrade_WheelManager))]
public class RCCP_VehicleUpgrade_WheelEditor : Editor {

    RCCP_VehicleUpgrade_WheelManager prop;

    public override void OnInspectorGUI() {

        prop = (RCCP_VehicleUpgrade_WheelManager)target;
        serializedObject.Update();

        EditorGUILayout.HelpBox("All wheels are stored in the configure wheels section", MessageType.None);

        DrawDefaultInspector();

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

        if (GUILayout.Button("Configure Wheels"))
            Selection.activeObject = RCCP_ChangableWheels.Instance;

        serializedObject.ApplyModifiedProperties();

    }

}
