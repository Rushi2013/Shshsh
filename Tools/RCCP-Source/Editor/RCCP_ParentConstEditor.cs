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

[CustomEditor(typeof(RCCP_ParentConst))]
public class RCCP_ParentConstEditor : Editor {

    RCCP_ParentConst prop;

    public override void OnInspectorGUI() {

        prop = (RCCP_ParentConst)target;
        serializedObject.Update();

        EditorGUILayout.HelpBox("Parent Constraint' component will be used to tilt, because this gameobject has been marked as tiltable in the 'RCCP_BodyTilt' component attached to the vehicle.", MessageType.Info);

        DrawDefaultInspector();

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

        serializedObject.ApplyModifiedProperties();

    }

}
