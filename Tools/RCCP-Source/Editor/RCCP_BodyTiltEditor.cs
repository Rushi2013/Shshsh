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
using System.Linq;

[CustomEditor(typeof(RCCP_BodyTilt))]
public class RCCP_BodyTiltEditor : Editor {

    RCCP_BodyTilt prop;
    List<string> errorMessages = new List<string>();
    GUISkin skin;
    Color guiColor;

    List<Transform> childrenGameObjects = new List<Transform>();
    List<Transform> selectedGameObjects = new List<Transform>();

    bool[] decs;

    private void OnEnable() {

        prop = (RCCP_BodyTilt)target;
        skin = Resources.Load<GUISkin>("RCCP_Gui");
        guiColor = GUI.color;

        if (childrenGameObjects == null)
            childrenGameObjects = new List<Transform>();

        childrenGameObjects.Clear();

        Transform[] allChildren = prop.transform.GetComponentInParent<RCCP_CarController>(true).transform.GetComponentsInChildren<Transform>(true);
        Transform parentTransform = prop.GetComponentInParent<RCCP_CarController>(true).transform;

        foreach (Transform item in allChildren) {

            if (item.GetComponent<MeshRenderer>() != null && item != parentTransform && item != prop.transform && item != prop.Anchor.transform) {

                childrenGameObjects.Add(item);

            }

        }

        decs = new bool[childrenGameObjects.Count];

        if (prop.Anchor != null && prop.Anchor.targetTransforms != null && prop.Anchor.targetTransforms.Count > 0) {

            for (int i = 0; i < prop.Anchor.targetTransforms.Count; i++) {

                for (int k = 0; k < childrenGameObjects.Count; k++) {

                    if (Equals(prop.Anchor.targetTransforms[i], childrenGameObjects[k]))
                        decs[k] = true;

                }

            }

        }

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_BodyTilt)target;
        serializedObject.Update();
        GUI.skin = skin;

        EditorGUILayout.HelpBox("Tilts the chassis / body of the vehicle depending on the rigidbody velocity.", MessageType.Info, true);
        EditorGUILayout.HelpBox("Be sure checked gameobjects don't have the colliders, wheels, and detachable parts, they need to be placed at root directory of the vehicle.", MessageType.Warning, true);

        PrefabInstanceStatus isConnectedToPrefab = PrefabUtility.GetPrefabInstanceStatus(Selection.activeGameObject);

        if (isConnectedToPrefab == PrefabInstanceStatus.Connected)
            EditorGUILayout.HelpBox("Can't make changes, because this gameobject is linked to a prefab. Please unpack the prefab or open the prefab to make changes on this component!", MessageType.Error);

        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (EditorApplication.isPlaying || isConnectedToPrefab == PrefabInstanceStatus.Connected)
            GUI.enabled = false;

        if (prop.Anchor) {

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Select the gameobjects you want to tilt below...", MessageType.None);
            EditorGUILayout.Space();

            if (selectedGameObjects == null)
                selectedGameObjects = new List<Transform>();

            selectedGameObjects.Clear();

            for (int i = 0; i < childrenGameObjects.Count; i++) {

                if (childrenGameObjects[i] != null) {

                    decs[i] = EditorGUILayout.ToggleLeft(childrenGameObjects[i].name, decs[i]);

                    if (decs[i] == true)
                        selectedGameObjects.Add(childrenGameObjects[i]);

                }

            }

            EditorGUILayout.EndVertical();

        }

        if (!EditorUtility.IsPersistent(prop) && !EditorApplication.isPlaying) {

            for (int i = 0; i < selectedGameObjects.Count; i++) {

                if (prop.Anchor.AddTarget(selectedGameObjects[i])) {

                    EditorUtility.SetDirty(prop);

                    if (prop.Anchor)
                        EditorUtility.SetDirty(prop.Anchor);

                }

            }

            foreach (var item in childrenGameObjects) {

                if (!selectedGameObjects.Contains(item)) {

                    if (prop.Anchor.RemoveTarget(item)) {

                        EditorUtility.SetDirty(prop);

                        if (prop.Anchor)
                            EditorUtility.SetDirty(prop.Anchor);

                    }

                }

            }

            if (prop.Anchor.CheckParentConsts()) {

                EditorUtility.SetDirty(prop);

                if (prop.Anchor)
                    EditorUtility.SetDirty(prop.Anchor);

            }

        }

        if (isConnectedToPrefab == PrefabInstanceStatus.Connected)
            EditorGUILayout.HelpBox("Can't make changes, because this gameobject is linked to a prefab. Please unpack the prefab or open the prefab to make changes on this component!", MessageType.Error);
        else
            CheckMissingRCCP_ParentConst();

        EditorGUILayout.Space();
        CheckMisconfig();
        EditorGUILayout.Space();

        GUI.enabled = true;

        if (!EditorUtility.IsPersistent(prop)) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            if (EditorApplication.isPlaying)
                GUI.enabled = false;

            if (GUILayout.Button("Restore All Configuration")) {

                EditorApplication.delayCall += () => {

                    if (EditorUtility.DisplayDialog("Restoring The Configuration", "All tiltable gameobjects will be restored back to default. Are you sure?", "Restore Back", "No, Keep")) {

                        RCCP_ParentConst[] parentConst = prop.GetComponentInParent<RCCP_CarController>(true).GetComponentsInChildren<RCCP_ParentConst>(true);

                        foreach (RCCP_ParentConst item in parentConst)
                            item.Restore();

                        ClearSelections();

                        Debug.Log("All tiltable gameobjects have been restored back to default");

                    }

                };

            }

            GUI.enabled = true;

            if (GUILayout.Button("Back"))
                Selection.activeGameObject = prop.GetComponentInParent<RCCP_OtherAddons>(true).gameObject;

            EditorGUILayout.EndVertical();

        }

        GUI.enabled = true;

        prop.transform.localPosition = Vector3.zero;
        prop.transform.localRotation = Quaternion.identity;

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

        serializedObject.ApplyModifiedProperties();

    }

    private void CheckMissingRCCP_ParentConst() {

        RCCP_ParentConst[] rCCP_ParentConsts = prop.GetComponentInParent<RCCP_CarController>(true).transform.GetComponentsInChildren<RCCP_ParentConst>(true);

        for (int i = 0; i < rCCP_ParentConsts.Length; i++) {

            if (prop.Anchor != null && prop.Anchor.targetTransforms != null) {

                bool sameFound = false;

                for (int k = 0; k < prop.Anchor.targetTransforms.Count; k++) {

                    if (prop.Anchor.targetTransforms[k] != null) {

                        if (Equals(rCCP_ParentConsts[i].transform.GetChild(0).gameObject, prop.Anchor.targetTransforms[k].gameObject))
                            sameFound = true;

                    }

                }

                for (int k = 0; k < prop.Anchor.targetComponents.Count; k++) {

                    if (prop.Anchor.targetComponents[k] != null) {

                        if (Equals(rCCP_ParentConsts[i].transform.gameObject, prop.Anchor.targetComponents[k].gameObject))
                            sameFound = true;

                    }

                }

                if (!sameFound) {

                    Debug.Log("Found unnecessary parent constraint component on " + rCCP_ParentConsts[i].name + ", destroying the component!");
                    rCCP_ParentConsts[i].Restore();

                    EditorUtility.SetDirty(prop);

                    if (prop.Anchor)
                        EditorUtility.SetDirty(prop.Anchor);

                }

            } else {

                Debug.Log("Found unnecessary parent constraint component on " + rCCP_ParentConsts[i].name + ", destroying the component!");
                rCCP_ParentConsts[i].Restore();

                EditorUtility.SetDirty(prop);

                if (prop.Anchor)
                    EditorUtility.SetDirty(prop.Anchor);

            }

        }

        RCCP_Lights lights = prop.GetComponentInParent<RCCP_CarController>(true).transform.GetComponentInChildren<RCCP_Lights>(true);
        RCCP_Exhausts exhausts = prop.GetComponentInParent<RCCP_CarController>(true).transform.GetComponentInChildren<RCCP_Exhausts>(true);

        if (lights != null && !prop.Anchor.targetComponents.Contains(lights)) {

            prop.Anchor.AddComponentTarget(lights);
            EditorUtility.SetDirty(prop);

        }

        if (exhausts != null && !prop.Anchor.targetComponents.Contains(exhausts)) {

            prop.Anchor.AddComponentTarget(exhausts);
            EditorUtility.SetDirty(prop);

        }

        prop.Anchor.targetTransforms.RemoveAll(item => item == null);
        prop.Anchor.targetComponents.RemoveAll(item => item == null);

    }

    private void CheckMisconfig() {

        bool completeSetup = true;
        errorMessages.Clear();

        bool collFound = false;

        for (int i = 0; i < prop.Anchor.targetTransforms.Count; i++) {

            if (prop.Anchor.targetTransforms[i].GetComponent<RCCP_DetachablePart>() == null && prop.Anchor.targetTransforms[i] != null && prop.Anchor.targetTransforms[i].GetComponentInChildren<Collider>(true)) {

                errorMessages.Add("Collider found on '" + prop.Anchor.targetTransforms[i].name + "', please remove the collider, or detach the collider component from this gameobject.");
                collFound = true;

            }

        }

        if (collFound)
            errorMessages.Add("Colliders found in the stated gameobjects. We don't want to tilt the colliders, it's meant to be visual only. Otherwise, vehicle physics will be unstable.");

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

    private void ClearSelections() {

        if (childrenGameObjects == null)
            childrenGameObjects = new List<Transform>();

        childrenGameObjects.Clear();

        Transform[] allChildren = prop.transform.GetComponentInParent<RCCP_CarController>(true).transform.GetComponentsInChildren<Transform>(true);
        Transform parentTransform = prop.GetComponentInParent<RCCP_CarController>(true).transform;

        foreach (Transform item in allChildren) {

            if (item.GetComponent<MeshRenderer>() != null && item != parentTransform && item != prop.transform && item != prop.Anchor.transform) {

                childrenGameObjects.Add(item);

            }

        }

        prop.Anchor.ClearTargets();

        EditorUtility.SetDirty(prop.Anchor);

        decs = new bool[childrenGameObjects.Count];

        if (prop.Anchor != null && prop.Anchor.targetTransforms != null && prop.Anchor.targetTransforms.Count > 0) {

            for (int i = 0; i < prop.Anchor.targetTransforms.Count; i++) {

                for (int k = 0; k < childrenGameObjects.Count; k++) {

                    if (Equals(prop.Anchor.targetTransforms[i], childrenGameObjects[k]))
                        decs[k] = true;

                }

            }

        }

        EditorUtility.SetDirty(prop);

    }

}
