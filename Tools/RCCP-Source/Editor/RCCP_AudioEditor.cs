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

[CustomEditor(typeof(RCCP_Audio))]
public class RCCP_AudioEditor : Editor {

    RCCP_Audio prop;
    List<string> errorMessages = new List<string>();
    GUISkin skin;
    private Color guiColor;

    private void OnEnable() {

        guiColor = GUI.color;
        skin = Resources.Load<GUISkin>("RCCP_Gui");

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_Audio)target;
        serializedObject.Update();
        GUI.skin = skin;

        EditorGUILayout.HelpBox("Audio system for engine, brake, crashes, transmission, and other stuff. Engine on / off sounds mean under compression, and free compression. Modern games are using multiple engine sounds for idle, low, medium, and high rpm audioclips. Check the demo audioclips of on / off versions to understand the difference. ", MessageType.Info, true);

        DrawDefaultInspector();

        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Create\n1 Engine Sound")) {

            prop.engineSounds = new RCCP_Audio.EngineSound[1];
            prop.engineSounds[0] = new RCCP_Audio.EngineSound();

            prop.engineSounds[0].minRPM = 0f;
            prop.engineSounds[0].maxRPM = 8000f;
            prop.engineSounds[0].maxVolume = .75f;
            prop.engineSounds[0].minPitch = .85f;
            prop.engineSounds[0].maxPitch = 1.65f;

            EditorUtility.SetDirty(prop);

        }

        if (GUILayout.Button("Create 2\nEngine Sounds")) {

            prop.engineSounds = new RCCP_Audio.EngineSound[2];
            prop.engineSounds[0] = new RCCP_Audio.EngineSound();
            prop.engineSounds[1] = new RCCP_Audio.EngineSound();

            prop.engineSounds[0].minRPM = 0f;
            prop.engineSounds[0].maxRPM = 4000f;
            prop.engineSounds[0].maxVolume = .6f;
            prop.engineSounds[0].minPitch = .85f;
            prop.engineSounds[0].maxPitch = 1.65f;

            prop.engineSounds[1].minRPM = 3000f;
            prop.engineSounds[1].maxRPM = 8000f;
            prop.engineSounds[1].maxVolume = .75f;
            prop.engineSounds[1].minPitch = .85f;
            prop.engineSounds[1].maxPitch = 1.65f;

            EditorUtility.SetDirty(prop);

        }

        if (GUILayout.Button("Create 3\nEngine Sounds")) {

            prop.engineSounds = new RCCP_Audio.EngineSound[3];
            prop.engineSounds[0] = new RCCP_Audio.EngineSound();
            prop.engineSounds[1] = new RCCP_Audio.EngineSound();
            prop.engineSounds[2] = new RCCP_Audio.EngineSound();

            prop.engineSounds[0].minRPM = 0f;
            prop.engineSounds[0].maxRPM = 3000f;
            prop.engineSounds[0].maxVolume = .55f;
            prop.engineSounds[0].minPitch = .85f;
            prop.engineSounds[0].maxPitch = 1.65f;

            prop.engineSounds[1].minRPM = 2000f;
            prop.engineSounds[1].maxRPM = 6000f;
            prop.engineSounds[1].maxVolume = .65f;
            prop.engineSounds[1].minPitch = .85f;
            prop.engineSounds[1].maxPitch = 1.65f;

            prop.engineSounds[2].minRPM = 6000f;
            prop.engineSounds[2].maxRPM = 8000f;
            prop.engineSounds[2].maxVolume = .75f;
            prop.engineSounds[2].minPitch = .85f;
            prop.engineSounds[2].maxPitch = 1.65f;

            EditorUtility.SetDirty(prop);

        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

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
