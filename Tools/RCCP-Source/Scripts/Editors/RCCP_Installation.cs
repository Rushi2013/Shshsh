//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright © 2014 - 2024 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[Icon("Assets/Realistic Car Controller Pro/Resources/Editor Icons/RCCP_EditorIcon_Manager.png")]
public class RCCP_Installation {

    public static void CheckProjectLayers() {

        bool layer_RCC;
        bool layer_RCC_WheelCollider;
        bool layer_RCC_DetachablePart;
        bool layer_RCC_Prop;

        string[] missingLayers = new string[4];

        layer_RCC = LayerExists(RCCP_Settings.Instance.RCCPLayer);
        layer_RCC_WheelCollider = LayerExists(RCCP_Settings.Instance.RCCPWheelColliderLayer);
        layer_RCC_DetachablePart = LayerExists(RCCP_Settings.Instance.RCCPDetachablePartLayer);
        layer_RCC_Prop = LayerExists(RCCP_Settings.Instance.RCCPPropLayer);

        if (!layer_RCC)
            missingLayers[0] = RCCP_Settings.Instance.RCCPLayer;

        if (!layer_RCC_WheelCollider)
            missingLayers[1] = RCCP_Settings.Instance.RCCPWheelColliderLayer;

        if (!layer_RCC_DetachablePart)
            missingLayers[2] = RCCP_Settings.Instance.RCCPDetachablePartLayer;

        if (!layer_RCC_Prop)
            missingLayers[3] = RCCP_Settings.Instance.RCCPPropLayer;

        if (!layer_RCC || !layer_RCC_DetachablePart || !layer_RCC_WheelCollider || !layer_RCC_Prop) {

            if (EditorUtility.DisplayDialog("Found Missing Layers For Realistic Car Controller Pro", "These layers will be added to the Tags and Layers\n\n" + missingLayers[0] + "\n" + missingLayers[1] + "\n" + missingLayers[2] + "\n" + missingLayers[3], "Add")) {

                CheckLayer(RCCP_Settings.Instance.RCCPLayer);
                CheckLayer(RCCP_Settings.Instance.RCCPWheelColliderLayer);
                CheckLayer(RCCP_Settings.Instance.RCCPDetachablePartLayer);
                CheckLayer(RCCP_Settings.Instance.RCCPPropLayer);

            }

        }

    }

    public static void CheckDemoVehicleLayers() {

        if (RCCP_Settings.Instance.RCCPLayer != null) {

            foreach (RCCP_CarController vehicle in RCCP_DemoVehicles.Instance.vehicles) {

                if (vehicle != null) {

                    if (!CheckPrefabHasMissingScripts(vehicle.gameObject)) {

                        vehicle.gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPLayer);

                        foreach (Transform transform in vehicle.GetComponentsInChildren<Transform>(true))
                            transform.gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPLayer);

                        if (RCCP_Settings.Instance.RCCPWheelColliderLayer != null) {

                            foreach (WheelCollider wc in vehicle.GetComponentsInChildren<WheelCollider>(true))
                                wc.transform.gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPWheelColliderLayer);

                        }

                        if (RCCP_Settings.Instance.RCCPDetachablePartLayer != null) {

                            foreach (RCCP_DetachablePart part in vehicle.GetComponentsInChildren<RCCP_DetachablePart>(true))
                                part.transform.gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPDetachablePartLayer);

                        }

                    }

                }

            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

        }

        CollisionMatrix();

    }

    public static bool CheckPrefabHasMissingScripts(GameObject prefab) {

        // Get all components in the prefab
        Component[] components = prefab.GetComponentsInChildren<Component>(true);

        foreach (Component component in components) {

            // Check if the component is null, which indicates a missing script
            if (component == null) {

                Debug.LogWarning("Prefab '" + prefab.name + "' has missing script.");
                return true;

            }

        }

        return false;

    }

    public static void CollisionMatrix() {

        bool layer_RCC;
        bool layer_RCC_WheelCollider;
        bool layer_RCC_DetachablePart;
        bool layer_RCC_Prop;

        layer_RCC = LayerExists("RCCP_Vehicle");
        layer_RCC_WheelCollider = LayerExists("RCCP_WheelCollider");
        layer_RCC_DetachablePart = LayerExists("RCCP_DetachablePart");
        layer_RCC_Prop = LayerExists("RCCP_Prop");

        if (layer_RCC && layer_RCC_DetachablePart)
            Physics.IgnoreLayerCollision(LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPLayer), LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPDetachablePartLayer), true);

        if (layer_RCC_WheelCollider && layer_RCC_DetachablePart)
            Physics.IgnoreLayerCollision(LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPWheelColliderLayer), LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPDetachablePartLayer), true);

        if (layer_RCC_WheelCollider && layer_RCC_Prop)
            Physics.IgnoreLayerCollision(LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPWheelColliderLayer), LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPPropLayer), true);

    }

    public static bool CheckTag(string tagName) {

        if (TagExists(tagName))
            return true;

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        if (!PropertyExists(tagsProp, 0, tagsProp.arraySize, tagName)) {

            int index = tagsProp.arraySize;

            tagsProp.InsertArrayElementAtIndex(index);
            SerializedProperty sp = tagsProp.GetArrayElementAtIndex(index);

            sp.stringValue = tagName;
            Debug.Log("Tag: " + tagName + " has been added.");

            tagManager.ApplyModifiedProperties();

            return true;

        }

        return false;

    }

    public static string NewTag(string name) {

        CheckTag(name);

        if (name == null || name == "")
            name = "Untagged";

        return name;

    }

    public static bool RemoveTag(string tagName) {

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        if (PropertyExists(tagsProp, 0, tagsProp.arraySize, tagName)) {

            SerializedProperty sp;

            for (int i = 0, j = tagsProp.arraySize; i < j; i++) {

                sp = tagsProp.GetArrayElementAtIndex(i);

                if (sp.stringValue == tagName) {

                    tagsProp.DeleteArrayElementAtIndex(i);
                    Debug.Log("Tag: " + tagName + " has been removed.");
                    tagManager.ApplyModifiedProperties();
                    return true;

                }

            }

        }

        return false;

    }

    public static bool TagExists(string tagName) {

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        return PropertyExists(tagsProp, 0, 10000, tagName);

    }

    public static bool CheckLayer(string layerName) {

        if (LayerExists(layerName))
            return true;

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");

        if (!PropertyExists(layersProp, 0, 31, layerName)) {

            SerializedProperty sp;

            for (int i = 8, j = 31; i < j; i++) {

                sp = layersProp.GetArrayElementAtIndex(i);

                if (sp.stringValue == "") {

                    sp.stringValue = layerName;
                    Debug.Log("Layer: " + layerName + " has been added.");
                    tagManager.ApplyModifiedProperties();
                    return true;

                }

                if (i == j)
                    Debug.Log("All allowed layers have been filled.");

            }

        }

        return false;

    }

    public static string NewLayer(string name) {

        if (name != null || name != "")
            CheckLayer(name);

        return name;

    }

    public static bool RemoveLayer(string layerName) {

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");

        if (PropertyExists(layersProp, 0, layersProp.arraySize, layerName)) {

            SerializedProperty sp;

            for (int i = 0, j = layersProp.arraySize; i < j; i++) {

                sp = layersProp.GetArrayElementAtIndex(i);

                if (sp.stringValue == layerName) {

                    sp.stringValue = "";
                    Debug.Log("Layer: " + layerName + " has been removed.");
                    // Save settings
                    tagManager.ApplyModifiedProperties();
                    return true;

                }

            }

        }

        return false;

    }

    public static bool LayerExists(string layerName) {

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");
        return PropertyExists(layersProp, 0, 31, layerName);

    }

    private static bool PropertyExists(SerializedProperty property, int start, int end, string value) {

        for (int i = start; i < end; i++) {

            SerializedProperty t = property.GetArrayElementAtIndex(i);

            if (t.stringValue.Equals(value))
                return true;

        }

        return false;

    }

}
#endif