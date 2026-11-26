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

[CustomEditor(typeof(RCCP_OtherAddons))]
public class RCCP_OtherAddonsEditor : Editor {

    RCCP_OtherAddons prop;
    GUISkin skin;
    Color guiColor;

    RCCP_Nos nos;
    RCCP_Visual_Dashboard dashboard;
    RCCP_Exterior_Cameras cameras;
    RCCP_Exhausts exhausts;
    RCCP_AI AI;
    RCCP_Recorder recorder;
    RCCP_TrailerAttacher trailAttacher;
    RCCP_Limiter limiter;
    RCCP_WheelBlur wheelBlur;
    RCCP_FuelTank fuelTank;
    RCCP_BodyTilt bodyTilt;

    private void OnEnable() {

        skin = Resources.Load<GUISkin>("RCCP_Gui");

        if (!EditorApplication.isPlaying)
            ReOrderComponents();

    }

    private void ReOrderComponents() {

        int index = 0;

        if (nos) {

            nos.transform.SetSiblingIndex(index);
            index++;

        }

        if (dashboard) {

            dashboard.transform.SetSiblingIndex(index);
            index++;

        }

        if (cameras) {

            cameras.transform.SetSiblingIndex(index);
            index++;

        }

        if (exhausts) {

            exhausts.transform.SetSiblingIndex(index);
            index++;

        }

        if (AI) {

            AI.transform.SetSiblingIndex(index);
            index++;

        }

        if (recorder) {

            recorder.transform.SetSiblingIndex(index);
            index++;

        }

        if (trailAttacher) {

            trailAttacher.transform.SetSiblingIndex(index);
            index++;

        }

        if (limiter) {

            limiter.transform.SetSiblingIndex(index);
            index++;

        }

        if (wheelBlur) {

            wheelBlur.transform.SetSiblingIndex(index);
            index++;

        }

        if (fuelTank) {

            fuelTank.transform.SetSiblingIndex(index);
            index++;

        }

        if (bodyTilt) {

            bodyTilt.transform.SetSiblingIndex(index);
            index++;

        }

    }

    private void GetAllComponents() {

        nos = prop.GetComponentInChildren<RCCP_Nos>(true);
        dashboard = prop.GetComponentInChildren<RCCP_Visual_Dashboard>(true);
        cameras = prop.GetComponentInChildren<RCCP_Exterior_Cameras>(true);
        exhausts = prop.GetComponentInChildren<RCCP_Exhausts>(true);
        AI = prop.GetComponentInChildren<RCCP_AI>(true);
        recorder = prop.GetComponentInChildren<RCCP_Recorder>(true);
        trailAttacher = prop.GetComponentInChildren<RCCP_TrailerAttacher>(true);
        limiter = prop.GetComponentInChildren<RCCP_Limiter>(true);
        wheelBlur = prop.GetComponentInChildren<RCCP_WheelBlur>(true);
        fuelTank = prop.GetComponentInChildren<RCCP_FuelTank>(true);
        bodyTilt = prop.GetComponentInChildren<RCCP_BodyTilt>(true);

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_OtherAddons)target;
        serializedObject.Update();
        GUI.skin = skin;
        guiColor = GUI.color;

        GetAllComponents();

        if (EditorUtility.IsPersistent(prop))
            EditorGUILayout.HelpBox("Double click the prefab to edit settings. Some editor features are disabled in this mode.", MessageType.Warning);

        if (Screen.width < 500)
            EditorGUILayout.HelpBox("Increase width of your inspector panel to see all buttons.", MessageType.Warning);

        EditorGUILayout.HelpBox("Other addons such as nos, exhausts, interior & exterior cameras, mirrors, etc...", MessageType.Info, true);

        EditorGUILayout.BeginHorizontal();

        NOSButton();
        GUILayout.Space(10f);
        InteriorButton();
        GUILayout.Space(10f);
        CamerasButton();
        GUILayout.Space(10f);
        ExhaustsButton();
        GUILayout.Space(10f);

        EditorGUILayout.EndHorizontal();
        GUILayout.Space(10f);
        EditorGUILayout.BeginHorizontal();

        AIButton();
        GUILayout.Space(10f);
        RecorderButton();
        GUILayout.Space(10f);
        TrailerButton();
        GUILayout.Space(10f);
        LimiterButton();
        GUILayout.Space(10f);

        EditorGUILayout.EndHorizontal();
        GUILayout.Space(10f);
        EditorGUILayout.BeginHorizontal();

        WheelBlurButton();
        GUILayout.Space(10f);
        FuelTankButton();
        GUILayout.Space(10f);
        BodyTiltButton();
        GUILayout.Space(10f);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.EndVertical();

        if (!EditorUtility.IsPersistent(prop)) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            if (GUILayout.Button("Back"))
                Selection.activeGameObject = prop.GetComponentInParent<RCCP_CarController>(true).gameObject;

            EditorGUILayout.EndVertical();

        }

        prop.transform.localPosition = Vector3.zero;
        prop.transform.localRotation = Quaternion.identity;

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

        serializedObject.ApplyModifiedProperties();

        if (!EditorApplication.isPlaying)
            Repaint();

    }

    private void NOSButton() {

        EditorGUILayout.BeginVertical(GUI.skin.window);

        if (nos)
            nos.enabled = EditorGUILayout.ToggleLeft("", nos.enabled, GUILayout.Width(15f));

        if (GUILayout.Button("NOS", GUILayout.Width(50f), GUILayout.Height(20f), GUILayout.ExpandWidth(true))) {

            if (nos)
                Selection.activeGameObject = nos.gameObject;
            else
                AddNOS();

        }

        if (!EditorUtility.IsPersistent(prop)) {

            if (nos)
                GUILayout.Label(("<color=#FF9500>Equipped</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));
            else
                GUILayout.Label(("<color=#FF9500>Create</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

            if (nos) {

                GUI.color = Color.red;

                if (GUILayout.Button("Remove"))
                    RemoveNOS();

                GUI.color = guiColor;

            }

        }

        EditorGUILayout.EndVertical();

    }

    private void InteriorButton() {

        EditorGUILayout.BeginVertical(GUI.skin.window);

        if (dashboard)
            dashboard.enabled = EditorGUILayout.ToggleLeft("", dashboard.enabled, GUILayout.Width(15f));

        if (GUILayout.Button("Interior", GUILayout.Width(50f), GUILayout.Height(20f), GUILayout.ExpandWidth(true))) {

            if (dashboard)
                Selection.activeGameObject = dashboard.gameObject;
            else
                AddDashboard();

        }

        if (!EditorUtility.IsPersistent(prop)) {

            if (dashboard)
                GUILayout.Label(("<color=#FF9500>Equipped</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));
            else
                GUILayout.Label(("<color=#FF9500>Create</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

            if (dashboard) {

                GUI.color = Color.red;

                if (GUILayout.Button("Remove"))
                    RemoveDashboard();

                GUI.color = guiColor;

            }

        }

        EditorGUILayout.EndVertical();

    }

    private void CamerasButton() {

        EditorGUILayout.BeginVertical(GUI.skin.window);

        if (cameras)
            cameras.enabled = EditorGUILayout.ToggleLeft("", cameras.enabled, GUILayout.Width(15f));

        if (GUILayout.Button("Cameras", GUILayout.Width(50f), GUILayout.Height(20f), GUILayout.ExpandWidth(true))) {

            if (cameras)
                Selection.activeGameObject = cameras.gameObject;
            else
                AddCameras();

        }

        if (!EditorUtility.IsPersistent(prop)) {

            if (cameras)
                GUILayout.Label(("<color=#FF9500>Equipped</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));
            else
                GUILayout.Label(("<color=#FF9500>Create</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

            if (cameras) {

                GUI.color = Color.red;

                if (GUILayout.Button("Remove"))
                    RemoveCameras();

                GUI.color = guiColor;

            }

        }

        EditorGUILayout.EndVertical();

    }

    private void ExhaustsButton() {

        EditorGUILayout.BeginVertical(GUI.skin.window);

        if (exhausts)
            exhausts.enabled = EditorGUILayout.ToggleLeft("", exhausts.enabled, GUILayout.Width(15f));

        if (GUILayout.Button("Exhausts", GUILayout.Width(50f), GUILayout.Height(20f), GUILayout.ExpandWidth(true))) {

            if (exhausts)
                Selection.activeGameObject = exhausts.gameObject;
            else
                AddExhausts();

        }

        if (!EditorUtility.IsPersistent(prop)) {

            if (exhausts)
                GUILayout.Label(("<color=#FF9500>Equipped</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));
            else
                GUILayout.Label(("<color=#FF9500>Create</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

            if (exhausts) {

                GUI.color = Color.red;

                if (GUILayout.Button("Remove"))
                    RemoveExhausts();

                GUI.color = guiColor;

            }

        }

        EditorGUILayout.EndVertical();

    }

    private void AIButton() {

        EditorGUILayout.BeginVertical(GUI.skin.window);

        if (AI)
            AI.enabled = EditorGUILayout.ToggleLeft("", AI.enabled, GUILayout.Width(15f));

        if (GUILayout.Button("AI", GUILayout.Width(50f), GUILayout.Height(20f), GUILayout.ExpandWidth(true))) {

            if (AI)
                Selection.activeGameObject = AI.gameObject;
            else
                AddAI();

        }

        if (!EditorUtility.IsPersistent(prop)) {

            if (AI)
                GUILayout.Label(("<color=#FF9500>Equipped</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));
            else
                GUILayout.Label(("<color=#FF9500>Create</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

            if (AI) {

                GUI.color = Color.red;

                if (GUILayout.Button("Remove"))
                    RemoveAI();

                GUI.color = guiColor;

            }

        }

        EditorGUILayout.EndVertical();

    }

    private void RecorderButton() {

        EditorGUILayout.BeginVertical(GUI.skin.window);

        if (recorder)
            recorder.enabled = EditorGUILayout.ToggleLeft("", recorder.enabled, GUILayout.Width(15f));

        if (GUILayout.Button("Recorder", GUILayout.Width(50f), GUILayout.Height(20f), GUILayout.ExpandWidth(true))) {

            if (recorder)
                Selection.activeGameObject = recorder.gameObject;
            else
                AddRecorder();

        }

        if (!EditorUtility.IsPersistent(prop)) {

            if (recorder)
                GUILayout.Label(("<color=#FF9500>Equipped</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));
            else
                GUILayout.Label(("<color=#FF9500>Create</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

            if (recorder) {

                GUI.color = Color.red;

                if (GUILayout.Button("Remove"))
                    RemoveRecorder();

                GUI.color = guiColor;

            }

        }

        EditorGUILayout.EndVertical();

    }

    private void TrailerButton() {

        EditorGUILayout.BeginVertical(GUI.skin.window);

        if (trailAttacher)
            trailAttacher.enabled = EditorGUILayout.ToggleLeft("", trailAttacher.enabled, GUILayout.Width(15f));

        if (GUILayout.Button("Trail\nAttacher", GUILayout.Width(50f), GUILayout.Height(40f), GUILayout.ExpandWidth(true))) {

            if (trailAttacher)
                Selection.activeGameObject = trailAttacher.gameObject;
            else
                AddTrailAttacher();

        }

        if (!EditorUtility.IsPersistent(prop)) {

            if (trailAttacher)
                GUILayout.Label(("<color=#FF9500>Equipped</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));
            else
                GUILayout.Label(("<color=#FF9500>Create</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

            if (trailAttacher) {

                GUI.color = Color.red;

                if (GUILayout.Button("Remove"))
                    RemoveTrailAttacher();

                GUI.color = guiColor;

            }

        }

        EditorGUILayout.EndVertical();

    }

    private void LimiterButton() {

        EditorGUILayout.BeginVertical(GUI.skin.window);

        if (limiter)
            limiter.enabled = EditorGUILayout.ToggleLeft("", limiter.enabled, GUILayout.Width(15f));

        if (GUILayout.Button("Limiter", GUILayout.Width(50f), GUILayout.Height(20f), GUILayout.ExpandWidth(true))) {

            if (limiter)
                Selection.activeGameObject = limiter.gameObject;
            else
                AddLimiter();

        }

        if (!EditorUtility.IsPersistent(prop)) {

            if (limiter)
                GUILayout.Label(("<color=#FF9500>Equipped</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));
            else
                GUILayout.Label(("<color=#FF9500>Create</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

            if (limiter) {

                GUI.color = Color.red;

                if (GUILayout.Button("Remove"))
                    RemoveLimiter();

                GUI.color = guiColor;

            }

        }

        EditorGUILayout.EndVertical();

    }

    private void WheelBlurButton() {

        EditorGUILayout.BeginVertical(GUI.skin.window);

        if (wheelBlur)
            wheelBlur.enabled = EditorGUILayout.ToggleLeft("", wheelBlur.enabled, GUILayout.Width(15f));

        if (GUILayout.Button("Wheel\nBlur", GUILayout.Width(50f), GUILayout.Height(40f), GUILayout.ExpandWidth(true))) {

            if (wheelBlur)
                Selection.activeGameObject = wheelBlur.gameObject;
            else
                AddWheelBlur();

        }

        if (!EditorUtility.IsPersistent(prop)) {

            if (wheelBlur)
                GUILayout.Label(("<color=#FF9500>Equipped</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));
            else
                GUILayout.Label(("<color=#FF9500>Create</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

            if (wheelBlur) {

                GUI.color = Color.red;

                if (GUILayout.Button("Remove"))
                    RemoveWheelBlur();

                GUI.color = guiColor;

            }

        }

        EditorGUILayout.EndVertical();

    }

    private void FuelTankButton() {

        EditorGUILayout.BeginVertical(GUI.skin.window);

        if (fuelTank)
            fuelTank.enabled = EditorGUILayout.ToggleLeft("", fuelTank.enabled, GUILayout.Width(15f));

        if (GUILayout.Button("Fuel\nTank", GUILayout.Width(50f), GUILayout.Height(40f), GUILayout.ExpandWidth(true))) {

            if (fuelTank)
                Selection.activeGameObject = fuelTank.gameObject;
            else
                AddFuelTank();

        }

        if (!EditorUtility.IsPersistent(prop)) {

            if (fuelTank)
                GUILayout.Label(("<color=#FF9500>Equipped</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));
            else
                GUILayout.Label(("<color=#FF9500>Create</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

            if (fuelTank) {

                GUI.color = Color.red;

                if (GUILayout.Button("Remove"))
                    RemoveFuelTank();

                GUI.color = guiColor;

            }

        }

        EditorGUILayout.EndVertical();

    }

    private void BodyTiltButton() {

        EditorGUILayout.BeginVertical(GUI.skin.window);

        if (bodyTilt)
            bodyTilt.enabled = EditorGUILayout.ToggleLeft("", bodyTilt.enabled, GUILayout.Width(15f));

        if (GUILayout.Button("<color=#FF0000>[Experimental]</color>\nBody Tilt", GUILayout.Width(50f), GUILayout.Height(40f), GUILayout.ExpandWidth(true))) {

            if (bodyTilt) {

                Selection.activeGameObject = bodyTilt.gameObject;

            } else {

                EditorApplication.delayCall += () => {

                    if (EditorUtility.DisplayDialog("Experimental Addon", "This is a experimental addon which may lead to unexpected results. Use it with caution, because it will make changes in the vehicle hierarchy. Backup your vehicle prefab before using it.\n\nDo you want to add this addon?", "Yes", "No"))
                        AddBodyTilt();

                };

            }

        }

        if (!EditorUtility.IsPersistent(prop)) {

            if (bodyTilt)
                GUILayout.Label(("<color=#FF9500>Equipped</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));
            else
                GUILayout.Label(("<color=#FF9500>Create</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

            if (bodyTilt) {

                GUI.color = Color.red;

                if (GUILayout.Button("Remove"))
                    RemoveBodyTilt();

                GUI.color = guiColor;

            }

        }

        EditorGUILayout.EndVertical();

    }

    private void AddNOS() {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_NOS");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(0);
        nos = subject.gameObject.AddComponent<RCCP_Nos>();

        EditorUtility.SetDirty(prop);

    }

    private void AddDashboard() {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_Dashboard");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(0);
        dashboard = subject.gameObject.AddComponent<RCCP_Visual_Dashboard>();

        EditorUtility.SetDirty(prop);

    }

    private void AddCameras() {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_ExteriorCameras");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(0);
        cameras = subject.gameObject.AddComponent<RCCP_Exterior_Cameras>();

        EditorUtility.SetDirty(prop);

    }

    private void AddExhausts() {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_Exhausts");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(0);
        exhausts = subject.gameObject.AddComponent<RCCP_Exhausts>();

        EditorUtility.SetDirty(prop);

    }

    private void AddAI() {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_AI");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(0);
        AI = subject.gameObject.AddComponent<RCCP_AI>();

        EditorUtility.SetDirty(prop);

    }

    private void AddRecorder() {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_Recorder");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(0);
        recorder = subject.gameObject.AddComponent<RCCP_Recorder>();

        EditorUtility.SetDirty(prop);

    }

    private void AddTrailAttacher() {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_TrailAttacher");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(0);
        trailAttacher = subject.gameObject.AddComponent<RCCP_TrailerAttacher>();

        EditorUtility.SetDirty(prop);

    }

    private void AddLimiter() {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_Limiter");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(0);
        limiter = subject.gameObject.AddComponent<RCCP_Limiter>();

        EditorUtility.SetDirty(prop);

    }

    private void AddWheelBlur() {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_WheelBlur");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(0);
        wheelBlur = subject.gameObject.AddComponent<RCCP_WheelBlur>();

        EditorUtility.SetDirty(prop);

    }

    private void AddFuelTank() {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_FuelTank");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(0);
        fuelTank = subject.gameObject.AddComponent<RCCP_FuelTank>();

        EditorUtility.SetDirty(prop);

    }

    private void AddBodyTilt() {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_BodyTilt");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(0);
        bodyTilt = subject.gameObject.AddComponent<RCCP_BodyTilt>();

        EditorUtility.SetDirty(prop);

    }

    private void RemoveNOS() {

        bool isPrefab = PrefabUtility.IsPartOfAnyPrefab(Selection.activeGameObject);

        if (isPrefab) {

            bool disconnectPrefabConnection = (EditorUtility.DisplayDialog("Unpacking Prefab", "This gameobject is connected to a prefab. In order to do remove this component, you'll need to unpack the prefab connection first. After removing the component, you can override your existing prefab with this gameobject.", "Disconnect", "Cancel"));

            if (!disconnectPrefabConnection)
                return;

            PrefabUtility.UnpackPrefabInstance(Selection.activeGameObject.transform.root.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        }

        EditorApplication.delayCall += () => {

            bool answer = (EditorUtility.DisplayDialog("Removing Component", "Are you sure want to remove this component? You can't undo this operation.", "Remove", "Cancel"));

            if (answer) {

                DestroyImmediate(nos.gameObject);
                nos = null;

                EditorUtility.SetDirty(prop);

            }

        };

    }

    private void RemoveDashboard() {

        bool isPrefab = PrefabUtility.IsPartOfAnyPrefab(Selection.activeGameObject);

        if (isPrefab) {

            bool disconnectPrefabConnection = (EditorUtility.DisplayDialog("Unpacking Prefab", "This gameobject is connected to a prefab. In order to do remove this component, you'll need to unpack the prefab connection first. After removing the component, you can override your existing prefab with this gameobject.", "Disconnect", "Cancel"));

            if (!disconnectPrefabConnection)
                return;

            PrefabUtility.UnpackPrefabInstance(Selection.activeGameObject.transform.root.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        }

        EditorApplication.delayCall += () => {

            bool answer = (EditorUtility.DisplayDialog("Removing Component", "Are you sure want to remove this component? You can't undo this operation.", "Remove", "Cancel"));

            if (answer) {

                DestroyImmediate(dashboard.gameObject);
                dashboard = null;

                EditorUtility.SetDirty(prop);

            }

        };

    }

    private void RemoveCameras() {

        bool isPrefab = PrefabUtility.IsPartOfAnyPrefab(Selection.activeGameObject);

        if (isPrefab) {

            bool disconnectPrefabConnection = (EditorUtility.DisplayDialog("Unpacking Prefab", "This gameobject is connected to a prefab. In order to do remove this component, you'll need to unpack the prefab connection first. After removing the component, you can override your existing prefab with this gameobject.", "Disconnect", "Cancel"));

            if (!disconnectPrefabConnection)
                return;

            PrefabUtility.UnpackPrefabInstance(Selection.activeGameObject.transform.root.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        }

        EditorApplication.delayCall += () => {

            bool answer = (EditorUtility.DisplayDialog("Removing Component", "Are you sure want to remove this component? You can't undo this operation.", "Remove", "Cancel"));

            if (answer) {

                DestroyImmediate(cameras.gameObject);
                cameras = null;

                EditorUtility.SetDirty(prop);

            }

        };

    }

    private void RemoveExhausts() {

        bool isPrefab = PrefabUtility.IsPartOfAnyPrefab(Selection.activeGameObject);

        if (isPrefab) {

            bool disconnectPrefabConnection = (EditorUtility.DisplayDialog("Unpacking Prefab", "This gameobject is connected to a prefab. In order to do remove this component, you'll need to unpack the prefab connection first. After removing the component, you can override your existing prefab with this gameobject.", "Disconnect", "Cancel"));

            if (!disconnectPrefabConnection)
                return;

            PrefabUtility.UnpackPrefabInstance(Selection.activeGameObject.transform.root.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        }

        EditorApplication.delayCall += () => {

            bool answer = (EditorUtility.DisplayDialog("Removing Component", "Are you sure want to remove this component? You can't undo this operation.", "Remove", "Cancel"));

            if (answer) {

                DestroyImmediate(exhausts.gameObject);
                exhausts = null;

                EditorUtility.SetDirty(prop);

            }

        };

    }

    private void RemoveAI() {

        bool isPrefab = PrefabUtility.IsPartOfAnyPrefab(Selection.activeGameObject);

        if (isPrefab) {

            bool disconnectPrefabConnection = (EditorUtility.DisplayDialog("Unpacking Prefab", "This gameobject is connected to a prefab. In order to do remove this component, you'll need to unpack the prefab connection first. After removing the component, you can override your existing prefab with this gameobject.", "Disconnect", "Cancel"));

            if (!disconnectPrefabConnection)
                return;

            PrefabUtility.UnpackPrefabInstance(Selection.activeGameObject.transform.root.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        }

        EditorApplication.delayCall += () => {

            bool answer = (EditorUtility.DisplayDialog("Removing Component", "Are you sure want to remove this component? You can't undo this operation.", "Remove", "Cancel"));

            if (answer) {

                DestroyImmediate(AI.gameObject);
                AI = null;

                EditorUtility.SetDirty(prop);

            }

        };

    }

    private void RemoveRecorder() {

        bool isPrefab = PrefabUtility.IsPartOfAnyPrefab(Selection.activeGameObject);

        if (isPrefab) {

            bool disconnectPrefabConnection = (EditorUtility.DisplayDialog("Unpacking Prefab", "This gameobject is connected to a prefab. In order to do remove this component, you'll need to unpack the prefab connection first. After removing the component, you can override your existing prefab with this gameobject.", "Disconnect", "Cancel"));

            if (!disconnectPrefabConnection)
                return;

            PrefabUtility.UnpackPrefabInstance(Selection.activeGameObject.transform.root.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        }

        EditorApplication.delayCall += () => {

            bool answer = (EditorUtility.DisplayDialog("Removing Component", "Are you sure want to remove this component? You can't undo this operation.", "Remove", "Cancel"));

            if (answer) {

                DestroyImmediate(recorder.gameObject);
                recorder = null;

                EditorUtility.SetDirty(prop);

            }

        };

    }

    private void RemoveTrailAttacher() {

        bool isPrefab = PrefabUtility.IsPartOfAnyPrefab(Selection.activeGameObject);

        if (isPrefab) {

            bool disconnectPrefabConnection = (EditorUtility.DisplayDialog("Unpacking Prefab", "This gameobject is connected to a prefab. In order to do remove this component, you'll need to unpack the prefab connection first. After removing the component, you can override your existing prefab with this gameobject.", "Disconnect", "Cancel"));

            if (!disconnectPrefabConnection)
                return;

            PrefabUtility.UnpackPrefabInstance(Selection.activeGameObject.transform.root.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        }

        EditorApplication.delayCall += () => {

            bool answer = (EditorUtility.DisplayDialog("Removing Component", "Are you sure want to remove this component? You can't undo this operation.", "Remove", "Cancel"));

            if (answer) {

                DestroyImmediate(trailAttacher.gameObject);
                trailAttacher = null;

                EditorUtility.SetDirty(prop);

            }

        };

    }

    private void RemoveLimiter() {

        bool isPrefab = PrefabUtility.IsPartOfAnyPrefab(Selection.activeGameObject);

        if (isPrefab) {

            bool disconnectPrefabConnection = (EditorUtility.DisplayDialog("Unpacking Prefab", "This gameobject is connected to a prefab. In order to do remove this component, you'll need to unpack the prefab connection first. After removing the component, you can override your existing prefab with this gameobject.", "Disconnect", "Cancel"));

            if (!disconnectPrefabConnection)
                return;

            PrefabUtility.UnpackPrefabInstance(Selection.activeGameObject.transform.root.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        }

        EditorApplication.delayCall += () => {

            bool answer = (EditorUtility.DisplayDialog("Removing Component", "Are you sure want to remove this component? You can't undo this operation.", "Remove", "Cancel"));

            if (answer) {

                DestroyImmediate(limiter.gameObject);
                limiter = null;

                EditorUtility.SetDirty(prop);

            }

        };

    }

    private void RemoveWheelBlur() {

        bool isPrefab = PrefabUtility.IsPartOfAnyPrefab(Selection.activeGameObject);

        if (isPrefab) {

            bool disconnectPrefabConnection = (EditorUtility.DisplayDialog("Unpacking Prefab", "This gameobject is connected to a prefab. In order to do remove this component, you'll need to unpack the prefab connection first. After removing the component, you can override your existing prefab with this gameobject.", "Disconnect", "Cancel"));

            if (!disconnectPrefabConnection)
                return;

            PrefabUtility.UnpackPrefabInstance(Selection.activeGameObject.transform.root.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        }

        EditorApplication.delayCall += () => {

            bool answer = (EditorUtility.DisplayDialog("Removing Component", "Are you sure want to remove this component? You can't undo this operation.", "Remove", "Cancel"));

            if (answer) {

                DestroyImmediate(wheelBlur.gameObject);
                wheelBlur = null;

                EditorUtility.SetDirty(prop);

            }

        };

    }

    private void RemoveFuelTank() {

        bool isPrefab = PrefabUtility.IsPartOfAnyPrefab(Selection.activeGameObject);

        if (isPrefab) {

            bool disconnectPrefabConnection = (EditorUtility.DisplayDialog("Unpacking Prefab", "This gameobject is connected to a prefab. In order to do remove this component, you'll need to unpack the prefab connection first. After removing the component, you can override your existing prefab with this gameobject.", "Disconnect", "Cancel"));

            if (!disconnectPrefabConnection)
                return;

            PrefabUtility.UnpackPrefabInstance(Selection.activeGameObject.transform.root.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        }

        EditorApplication.delayCall += () => {

            bool answer = (EditorUtility.DisplayDialog("Removing Component", "Are you sure want to remove this component? You can't undo this operation.", "Remove", "Cancel"));

            if (answer) {

                DestroyImmediate(fuelTank.gameObject);
                fuelTank = null;

                EditorUtility.SetDirty(prop);

            }

        };

    }

    private void RemoveBodyTilt() {

        bool isPrefab = PrefabUtility.IsPartOfAnyPrefab(Selection.activeGameObject);

        if (isPrefab) {

            bool disconnectPrefabConnection = (EditorUtility.DisplayDialog("Unpacking Prefab", "This gameobject is connected to a prefab. In order to do remove this component, you'll need to unpack the prefab connection first. After removing the component, you can override your existing prefab with this gameobject.", "Disconnect", "Cancel"));

            if (!disconnectPrefabConnection)
                return;

            PrefabUtility.UnpackPrefabInstance(Selection.activeGameObject.transform.root.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        }

        EditorApplication.delayCall += () => {

            bool answer = (EditorUtility.DisplayDialog("Removing Component", "Are you sure want to remove this component? You can't undo this operation.", "Remove", "Cancel"));

            if (answer) {

                DestroyImmediate(bodyTilt.gameObject);
                bodyTilt = null;

                EditorUtility.SetDirty(prop);

            }

        };

    }

}
