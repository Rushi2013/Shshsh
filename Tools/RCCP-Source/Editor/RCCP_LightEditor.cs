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
using UnityEngine.Rendering;

[CustomEditor(typeof(RCCP_Light))]
public class RCCP_LightEditor : Editor {

    RCCP_Light prop;
    GUISkin skin;
    private Color guiColor;

    private void OnEnable() {

        guiColor = GUI.color;
        skin = Resources.Load<GUISkin>("RCCP_Gui");

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_Light)target;
        serializedObject.Update();
        GUI.skin = skin;

        DrawDefaultInspector();
        EditorGUILayout.Space();

        if (prop.useLensFlares) {

            EditorGUILayout.HelpBox("When using lensflares, be sure to have correct lensflare system. Builtin renderer pipeline is using 'LensFlare' component, other renderer pilelines are using 'LensFlare (SRP) component.'", MessageType.Info);
            EditorGUILayout.HelpBox("If you are using SRP lensflares, be sure camera on your scene has post process effects option enabled.", MessageType.Info);

            EditorGUILayout.Space();

            if (GUILayout.Button("Create LensFlare")) {

#if BCG_URP

                LensFlareComponentSRP lensFlareComponentSRP = prop.GetComponent<LensFlareComponentSRP>();

                if (lensFlareComponentSRP)
                    return;

                lensFlareComponentSRP = prop.gameObject.AddComponent<LensFlareComponentSRP>();
                lensFlareComponentSRP.lensFlareData = RCCP_Settings.Instance.lensFlareData;

#else

                LensFlare lensFlareComponent = prop.GetComponent<LensFlare>();

                if (lensFlareComponent)
                    return;

                lensFlareComponent = prop.gameObject.AddComponent<LensFlare>();

#endif

            }

        }

        CheckMisconfig();

        if (!EditorUtility.IsPersistent(prop)) {

            if (GUILayout.Button("Duplicate To Other Side")) {

                GameObject duplicated = Instantiate(prop.gameObject, prop.GetComponentInParent<RCCP_CarController>(true).GetComponentInChildren<RCCP_Lights>(true).transform);

                duplicated.transform.name = prop.transform.name + "_D";
                duplicated.transform.localPosition = new Vector3(-duplicated.transform.localPosition.x, duplicated.transform.localPosition.y, duplicated.transform.localPosition.z);
                duplicated.transform.localRotation = prop.transform.localRotation;

                if (prop.lightType == RCCP_Light.LightType.IndicatorLeftLight)
                    duplicated.GetComponent<RCCP_Light>().lightType = RCCP_Light.LightType.IndicatorRightLight;

                if (prop.lightType == RCCP_Light.LightType.IndicatorRightLight)
                    duplicated.GetComponent<RCCP_Light>().lightType = RCCP_Light.LightType.IndicatorLeftLight;

                Selection.activeGameObject = duplicated;

            }

            RCCP_LightBox lightBox = prop.GetComponentInChildren<RCCP_LightBox>(true);

            if (lightBox == null) {

                if (GUILayout.Button("Create LightBox")) {

                    MeshRenderer lightBoxRenderer = Instantiate(RCCP_Settings.Instance.lightBox, prop.transform).GetComponent<MeshRenderer>();
                    lightBoxRenderer.transform.name = RCCP_Settings.Instance.lightBox.name;
                    lightBoxRenderer.transform.rotation = prop.transform.root.transform.rotation;
                    prop.emissiveRenderer = lightBoxRenderer;
                    prop.emissiveMaterialIndex = 0;

                    Selection.activeGameObject = lightBoxRenderer.gameObject;
                    SceneView.FrameLastActiveSceneView();

                }

            } else {

                if (GUILayout.Button("Select LightBox")) {

                    MeshRenderer lightBoxRenderer = lightBox.GetComponent<MeshRenderer>();
                    lightBoxRenderer.transform.name = RCCP_Settings.Instance.lightBox.name;
                    lightBoxRenderer.transform.rotation = prop.transform.root.transform.rotation;
                    prop.emissiveRenderer = lightBoxRenderer;
                    prop.emissiveMaterialIndex = 0;

                    Selection.activeGameObject = lightBoxRenderer.gameObject;
                    SceneView.FrameLastActiveSceneView();

                }

            }

            if (GUILayout.Button("Back"))
                Selection.activeGameObject = prop.GetComponentInParent<RCCP_CarController>(true).GetComponentInChildren<RCCP_Lights>(true).gameObject;

            if (prop.GetComponentInParent<RCCP_CarController>(true).checkComponents)
                Selection.activeGameObject = prop.GetComponentInParent<RCCP_CarController>(true).gameObject;

        }

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

        serializedObject.ApplyModifiedProperties();

    }

    private void CheckMisconfig() {

        if (!prop.gameObject.activeInHierarchy)
            return;

        Vector3 relativePos = prop.GetComponentInParent<RCCP_CarController>(true).transform.InverseTransformPoint(prop.transform.position);

        if (relativePos.z > 0f) {

            if (Mathf.Abs(prop.transform.localRotation.y) > .5f) {

                GUI.color = Color.red;
                EditorGUILayout.HelpBox("Light is facing to wrong direction!", MessageType.Error);
                GUI.color = guiColor;

                GUI.color = Color.green;

                if (GUILayout.Button("Fix Rotation"))
                    prop.transform.localRotation = Quaternion.identity;

                GUI.color = guiColor;

            }

        } else {

            if (Mathf.Abs(prop.transform.localRotation.y) < .5f) {

                GUI.color = Color.red;
                EditorGUILayout.HelpBox("Light is facing to wrong direction!", MessageType.Error);
                GUI.color = guiColor;

                GUI.color = Color.green;

                if (GUILayout.Button("Fix Rotation"))
                    prop.transform.localRotation = Quaternion.Euler(prop.transform.localRotation.x, 180f, prop.transform.localRotation.z);

                GUI.color = guiColor;

            }

        }

        RCCP_Lights lightsManager = prop.GetComponentInParent<RCCP_Lights>(true);

        if (lightsManager)
            lightsManager.GetAllLights();

    }

}
