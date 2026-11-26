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

[CustomEditor(typeof(RCCP_Customizer))]
public class RCCP_CustomizerEditor : Editor {

    RCCP_Customizer prop;
    List<string> errorMessages = new List<string>();
    GUISkin skin;
    private Color guiColor;

    RCCP_VehicleUpgrade_SpoilerManager spoilers;
    RCCP_VehicleUpgrade_SirenManager sirens;
    RCCP_VehicleUpgrade_UpgradeManager upgrades;
    RCCP_VehicleUpgrade_PaintManager paints;
    RCCP_VehicleUpgrade_WheelManager wheels;
    RCCP_VehicleUpgrade_CustomizationManager customization;
    RCCP_VehicleUpgrade_DecalManager decals;
    RCCP_VehicleUpgrade_NeonManager neons;

    private void OnEnable() {

        guiColor = GUI.color;
        skin = Resources.Load<GUISkin>("RCCP_Gui");

    }

    private void GetAllComponents() {

        spoilers = prop.GetComponentInChildren<RCCP_VehicleUpgrade_SpoilerManager>(true);
        sirens = prop.GetComponentInChildren<RCCP_VehicleUpgrade_SirenManager>(true);
        upgrades = prop.GetComponentInChildren<RCCP_VehicleUpgrade_UpgradeManager>(true);
        paints = prop.GetComponentInChildren<RCCP_VehicleUpgrade_PaintManager>(true);
        wheels = prop.GetComponentInChildren<RCCP_VehicleUpgrade_WheelManager>(true);
        customization = prop.GetComponentInChildren<RCCP_VehicleUpgrade_CustomizationManager>(true);
        decals = prop.GetComponentInChildren<RCCP_VehicleUpgrade_DecalManager>(true);
        neons = prop.GetComponentInChildren<RCCP_VehicleUpgrade_NeonManager>(true);

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_Customizer)target;
        serializedObject.Update();
        GUI.skin = skin;

        GetAllComponents();

        EditorGUILayout.HelpBox("Customizer system that includes paints, wheels, spoilers, configurations, sirens, upgrades, and more. Customization data will be saved and loaded with json.", MessageType.Info, true);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("initializeMethod"), new GUIContent("Initialize Method", "When should be customizer initialized?"), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("saveFileName"), new GUIContent("Save File Name", "Save File Name."), false);
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoSave"), new GUIContent("Auto Save Loadout", "Saves last changes."), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoLoadLoadout"), new GUIContent("Auto Load Loadout", "Loads all last changes."), false);
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("loadout"), new GUIContent("Loadout", "Loadout."), true);
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Hide All")) {

            prop.HideAll();
            EditorUtility.SetDirty(prop);

        }

        if (GUILayout.Button("Show All")) {

            prop.ShowAll();
            EditorUtility.SetDirty(prop);

        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (!spoilers) {

            EditorGUILayout.HelpBox("Spoiler Manager not found!", MessageType.None);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Create", GUILayout.Width(120f))) {

                GameObject create = Instantiate(RCCP_CustomizationSetups.Instance.spoilers, prop.transform.position, prop.transform.rotation, prop.transform);
                create.transform.SetParent(prop.transform);
                create.transform.localPosition = Vector3.zero;
                create.transform.localRotation = Quaternion.identity;
                create.name = RCCP_CustomizationSetups.Instance.spoilers.name;

                EditorUtility.SetDirty(prop);

            }

        } else {

            EditorGUILayout.HelpBox("Spoiler Manager found!", MessageType.None);

            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Select", GUILayout.Width(120f)))
                Selection.activeGameObject = spoilers.gameObject;

            GUI.color = Color.red;

            if (GUILayout.Button("X", GUILayout.Width(25f))) {

                DestroyImmediate(spoilers.gameObject);
                EditorUtility.SetDirty(prop);

            }

            GUI.color = guiColor;

            EditorGUILayout.EndHorizontal();

        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (!sirens) {

            EditorGUILayout.HelpBox("Siren Manager not found!", MessageType.None);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Create", GUILayout.Width(120f))) {

                GameObject create = Instantiate(RCCP_CustomizationSetups.Instance.sirens, prop.transform.position, prop.transform.rotation, prop.transform);
                create.transform.SetParent(prop.transform);
                create.transform.localPosition = Vector3.zero;
                create.transform.localRotation = Quaternion.identity;
                create.name = RCCP_CustomizationSetups.Instance.sirens.name;

                EditorUtility.SetDirty(prop);

            }

        } else {

            EditorGUILayout.HelpBox("Siren Manager found!", MessageType.None);

            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Select", GUILayout.Width(120f)))
                Selection.activeGameObject = sirens.gameObject;

            GUI.color = Color.red;

            if (GUILayout.Button("X", GUILayout.Width(25f))) {

                DestroyImmediate(sirens.gameObject);
                EditorUtility.SetDirty(prop);

            }

            GUI.color = guiColor;

            EditorGUILayout.EndHorizontal();

        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (!upgrades) {

            EditorGUILayout.HelpBox("Upgrade Manager not found!", MessageType.None);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Create", GUILayout.Width(120f))) {

                GameObject create = Instantiate(RCCP_CustomizationSetups.Instance.upgrades, prop.transform.position, prop.transform.rotation, prop.transform);
                create.transform.SetParent(prop.transform);
                create.transform.localPosition = Vector3.zero;
                create.transform.localRotation = Quaternion.identity;
                create.name = RCCP_CustomizationSetups.Instance.upgrades.name;

                EditorUtility.SetDirty(prop);

            }

        } else {

            EditorGUILayout.HelpBox("Upgrade Manager found!", MessageType.None);

            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Select", GUILayout.Width(120f)))
                Selection.activeGameObject = upgrades.gameObject;

            GUI.color = Color.red;

            if (GUILayout.Button("X", GUILayout.Width(25f))) {

                DestroyImmediate(upgrades.gameObject);
                EditorUtility.SetDirty(prop);

            }

            GUI.color = guiColor;

            EditorGUILayout.EndHorizontal();

        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (!paints) {

            EditorGUILayout.HelpBox("Paint Manager not found!", MessageType.None);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Create", GUILayout.Width(120f))) {

                GameObject create = Instantiate(RCCP_CustomizationSetups.Instance.paints, prop.transform.position, prop.transform.rotation, prop.transform);
                create.transform.SetParent(prop.transform);
                create.transform.localPosition = Vector3.zero;
                create.transform.localRotation = Quaternion.identity;
                create.name = RCCP_CustomizationSetups.Instance.paints.name;

                EditorUtility.SetDirty(prop);

            }

        } else {

            EditorGUILayout.HelpBox("Paint Manager found!", MessageType.None);

            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Select", GUILayout.Width(120f)))
                Selection.activeGameObject = paints.gameObject;

            GUI.color = Color.red;

            if (GUILayout.Button("X", GUILayout.Width(25f))) {

                DestroyImmediate(paints.gameObject);
                EditorUtility.SetDirty(prop);

            }

            GUI.color = guiColor;

            EditorGUILayout.EndHorizontal();

        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (!wheels) {

            EditorGUILayout.HelpBox("Wheel Manager not found!", MessageType.None);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Create", GUILayout.Width(120f))) {

                GameObject create = Instantiate(RCCP_CustomizationSetups.Instance.wheels, prop.transform.position, prop.transform.rotation, prop.transform);
                create.transform.SetParent(prop.transform);
                create.transform.localPosition = Vector3.zero;
                create.transform.localRotation = Quaternion.identity;
                create.name = RCCP_CustomizationSetups.Instance.wheels.name;

                EditorUtility.SetDirty(prop);

            }

        } else {

            EditorGUILayout.HelpBox("Wheel Manager found!", MessageType.None);

            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Select", GUILayout.Width(120f)))
                Selection.activeGameObject = wheels.gameObject;

            GUI.color = Color.red;

            if (GUILayout.Button("X", GUILayout.Width(25f))) {

                DestroyImmediate(wheels.gameObject);
                EditorUtility.SetDirty(prop);

            }

            GUI.color = guiColor;

            EditorGUILayout.EndHorizontal();

        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (!customization) {

            EditorGUILayout.HelpBox("Customization Manager not found!", MessageType.None);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Create", GUILayout.Width(120f))) {

                GameObject create = Instantiate(RCCP_CustomizationSetups.Instance.customization, prop.transform.position, prop.transform.rotation, prop.transform);
                create.transform.SetParent(prop.transform);
                create.transform.localPosition = Vector3.zero;
                create.transform.localRotation = Quaternion.identity;
                create.name = RCCP_CustomizationSetups.Instance.customization.name;

                EditorUtility.SetDirty(prop);

            }

        } else {

            EditorGUILayout.HelpBox("Customization Manager found!", MessageType.None);

            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Select", GUILayout.Width(120f)))
                Selection.activeGameObject = customization.gameObject;

            GUI.color = Color.red;

            if (GUILayout.Button("X", GUILayout.Width(25f))) {

                DestroyImmediate(customization.gameObject);
                EditorUtility.SetDirty(prop);

            }

            GUI.color = guiColor;

            EditorGUILayout.EndHorizontal();

        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (!decals) {

            EditorGUILayout.HelpBox("Decal Manager not found!", MessageType.None);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Create", GUILayout.Width(120f))) {

                GameObject create = Instantiate(RCCP_CustomizationSetups.Instance.decals, prop.transform.position, prop.transform.rotation, prop.transform);
                create.transform.SetParent(prop.transform);
                create.transform.localPosition = Vector3.zero;
                create.transform.localRotation = Quaternion.identity;
                create.name = RCCP_CustomizationSetups.Instance.decals.name;

                EditorUtility.SetDirty(prop);

            }

        } else {

            EditorGUILayout.HelpBox("Decal Manager found!", MessageType.None);

            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Select", GUILayout.Width(120f)))
                Selection.activeGameObject = decals.gameObject;

            GUI.color = Color.red;

            if (GUILayout.Button("X", GUILayout.Width(25f))) {

                DestroyImmediate(decals.gameObject);
                EditorUtility.SetDirty(prop);

            }

            GUI.color = guiColor;

            EditorGUILayout.EndHorizontal();

        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (!neons) {

            EditorGUILayout.HelpBox("Neon Manager not found!", MessageType.None);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Create", GUILayout.Width(120f))) {

                GameObject create = Instantiate(RCCP_CustomizationSetups.Instance.neons, prop.transform.position, prop.transform.rotation, prop.transform);
                create.transform.SetParent(prop.transform);
                create.transform.localPosition = Vector3.zero;
                create.transform.localRotation = Quaternion.identity;
                create.name = RCCP_CustomizationSetups.Instance.neons.name;

                EditorUtility.SetDirty(prop);

            }

        } else {

            EditorGUILayout.HelpBox("Neon Manager found!", MessageType.None);

            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Select", GUILayout.Width(120f)))
                Selection.activeGameObject = neons.gameObject;

            GUI.color = Color.red;

            if (GUILayout.Button("X", GUILayout.Width(25f))) {

                DestroyImmediate(neons.gameObject);
                EditorUtility.SetDirty(prop);

            }

            GUI.color = guiColor;

            EditorGUILayout.EndHorizontal();

        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox("Saving and loading loadouts can be done in the runtime.", MessageType.None);

        EditorGUILayout.BeginHorizontal();

        if (!EditorApplication.isPlaying)
            GUI.enabled = false;

        if (GUILayout.Button("Save Loadout"))
            prop.Save();

        if (GUILayout.Button("Load Loadout"))
            prop.Load();

        if (GUILayout.Button("Apply Loadout"))
            prop.Initialize();

        if (GUILayout.Button("Restore Loadout"))
            prop.Delete();

        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        if (!EditorUtility.IsPersistent(prop)) {

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

        }

        prop.transform.localPosition = Vector3.zero;
        prop.transform.localRotation = Quaternion.identity;

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

        serializedObject.ApplyModifiedProperties();

    }

}
