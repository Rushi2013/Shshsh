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

[CustomEditor(typeof(RCCP_Lights))]
public class RCCP_LightsEditor : Editor {

    RCCP_Lights prop;
    List<string> errorMessages = new List<string>();
    GUISkin skin;
    private Color guiColor;

    RCCP_Light.LightType lightType;

    private void OnEnable() {

        guiColor = GUI.color;
        skin = Resources.Load<GUISkin>("RCCP_Gui");

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_Lights)target;
        serializedObject.Update();
        GUI.skin = skin;

        EditorGUILayout.HelpBox("Main light manager of the vehicle. All lights are connected to this manager.", MessageType.Info, true);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("lowBeamHeadlights"), new GUIContent("Low Beam Headlights", "Low beam headlights are on or off?"), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("highBeamHeadlights"), new GUIContent("High Beam Headlights", "High beam headlights are on or off?"), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("indicatorsAll"), new GUIContent("Indicators", "Indicators are set to on, or off?"), false);

        prop.GetAllLights();

        EditorGUILayout.Space();
        GUILayout.Label("Attached Lights", EditorStyles.boldLabel);

        if (prop.lights != null && prop.lights.Count > 0) {

            for (int i = 0; i < prop.lights.Count; i++) {

                if (prop.lights[i] != null) {

                    EditorGUILayout.BeginHorizontal(GUI.skin.box);
                    GUILayout.Label(prop.lights[i].name, GUILayout.Width(250f));
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Edit", GUILayout.Width(100f)))
                        Selection.activeGameObject = prop.lights[i].gameObject;

                    GUI.color = Color.red;

                    if (GUILayout.Button("X")) {

                        DestroyImmediate(prop.lights[i].gameObject);
                        EditorUtility.SetDirty(prop);

                    }

                    GUI.color = guiColor;
                    EditorGUILayout.EndHorizontal();

                }

            }

        } else {

            EditorGUILayout.HelpBox("No lights found. You can create new lights below.", MessageType.Warning);

        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.EndVertical();

        if (!EditorUtility.IsPersistent(prop)) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label("Create New Light", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            lightType = (RCCP_Light.LightType)EditorGUILayout.EnumPopup(lightType);

            GUI.color = Color.green;

            if (GUILayout.Button("Create New Light")) {

                Selection.activeGameObject = CreateNewLight(lightType).gameObject;
                SceneView.FrameLastActiveSceneView();
                EditorUtility.SetDirty(prop);

            }

            GUI.color = guiColor;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.EndVertical();

        if (!EditorUtility.IsPersistent(prop)) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

#if BCG_URP

            if (GUILayout.Button("Convert To URP"))
                ConvertToURP();

#endif

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

    private RCCP_Light CreateNewLight(RCCP_Light.LightType lightType) {

        switch (lightType) {

            case RCCP_Light.LightType.Headlight_LowBeam:

                GameObject newLightSource_Headlight = Instantiate(RCCP_Settings.Instance.headLights_Low, prop.transform, false);
                newLightSource_Headlight.transform.name = RCCP_Settings.Instance.headLights_Low.transform.name + "_D";
                newLightSource_Headlight.transform.localPosition = new Vector3(0f, 0f, 2.5f);
#if BCG_URP
                ConvertToURP();
#endif
                prop.GetAllLights();
                return newLightSource_Headlight.GetComponent<RCCP_Light>();

            case RCCP_Light.LightType.Headlight_HighBeam:

                GameObject newLightSource_Headlight_High = Instantiate(RCCP_Settings.Instance.headLights_High, prop.transform, false);
                newLightSource_Headlight_High.transform.name = RCCP_Settings.Instance.headLights_High.transform.name + "_D";
                newLightSource_Headlight_High.transform.localPosition = new Vector3(0f, 0f, 2.5f);
#if BCG_URP
                ConvertToURP();
#endif
                prop.GetAllLights();
                return newLightSource_Headlight_High.GetComponent<RCCP_Light>();

            case RCCP_Light.LightType.Brakelight:

                GameObject newLightSource_Brakelight = Instantiate(RCCP_Settings.Instance.brakeLights, prop.transform, false);
                newLightSource_Brakelight.transform.name = RCCP_Settings.Instance.brakeLights.transform.name + "_D";
                newLightSource_Brakelight.transform.localPosition = new Vector3(0f, 0f, -2.5f);
#if BCG_URP
                ConvertToURP();
#endif
                prop.GetAllLights();
                return newLightSource_Brakelight.GetComponent<RCCP_Light>();

            case RCCP_Light.LightType.Reverselight:

                GameObject newLightSource_Reverselight = Instantiate(RCCP_Settings.Instance.reverseLights, prop.transform, false);
                newLightSource_Reverselight.transform.name = RCCP_Settings.Instance.reverseLights.transform.name + "_D";
                newLightSource_Reverselight.transform.localPosition = new Vector3(0f, 0f, -2.5f);
#if BCG_URP
                ConvertToURP();
#endif
                prop.GetAllLights();
                return newLightSource_Reverselight.GetComponent<RCCP_Light>();

            case RCCP_Light.LightType.IndicatorLeftLight:

                GameObject newLightSource_IndicatorL = Instantiate(RCCP_Settings.Instance.indicatorLights_L, prop.transform, false);
                newLightSource_IndicatorL.transform.name = RCCP_Settings.Instance.indicatorLights_L.transform.name + "_D";
                newLightSource_IndicatorL.transform.localPosition = new Vector3(-.5f, 0f, -2.5f);
#if BCG_URP
                ConvertToURP();
#endif
                prop.GetAllLights();
                return newLightSource_IndicatorL.GetComponent<RCCP_Light>();

            case RCCP_Light.LightType.IndicatorRightLight:

                GameObject newLightSource_IndicatorR = Instantiate(RCCP_Settings.Instance.indicatorLights_R, prop.transform, false);
                newLightSource_IndicatorR.transform.name = RCCP_Settings.Instance.indicatorLights_R.transform.name + "_D";
                newLightSource_IndicatorR.transform.localPosition = new Vector3(.5f, 0f, -2.5f);
#if BCG_URP
                ConvertToURP();
#endif
                prop.GetAllLights();
                return newLightSource_IndicatorR.GetComponent<RCCP_Light>();

            case RCCP_Light.LightType.Taillight:

                GameObject newLightSource_Taillight = Instantiate(RCCP_Settings.Instance.tailLights, prop.transform, false);
                newLightSource_Taillight.transform.name = RCCP_Settings.Instance.tailLights.transform.name + "_D";
                newLightSource_Taillight.transform.localPosition = new Vector3(0f, 0f, -2.5f);
#if BCG_URP
                ConvertToURP();
#endif
                prop.GetAllLights();
                return newLightSource_Taillight.GetComponent<RCCP_Light>();

        }

        return null;

    }

#if BCG_URP
    private void ConvertToURP() {

        RCCP_Light[] lights = prop.GetComponentsInChildren<RCCP_Light>(true);

        for (int i = 0; i < lights.Length; i++) {

            if (lights[i].TryGetComponent(out LensFlare oldLensFlare))
                DestroyImmediate(oldLensFlare);

            if (!lights[i].TryGetComponent(out UnityEngine.Rendering.LensFlareComponentSRP newLensFlare)) {

                UnityEngine.Rendering.LensFlareComponentSRP srp = lights[i].gameObject.AddComponent<UnityEngine.Rendering.LensFlareComponentSRP>();
                srp.lensFlareData = RCCP_Settings.Instance.lensFlareData;

            }

        }

        if (Camera.main != null && Camera.main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>() != null && Camera.main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing == false) {

            Camera.main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing = true;

        }

    }
#endif

}
