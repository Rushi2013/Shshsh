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
using System.Linq;

[CustomEditor(typeof(RCCP_DemoVehicles))]
public class RCCP_DemoVehiclesEditor : Editor {

    RCCP_DemoVehicles prop;

    public override void OnInspectorGUI() {

        prop = (RCCP_DemoVehicles)target;
        serializedObject.Update();

        DrawDefaultInspector();

        if (GUILayout.Button("Check Project For All RCCP Vehicle Prefabs"))
            CheckProjectForPrefabs();

        serializedObject.ApplyModifiedProperties();

    }

    private void CheckProjectForPrefabs() {

        List<RCCP_CarController> foundPrefabs = new List<RCCP_CarController>();

        bool cancelled = false;

        try {

            string[] paths = SearchByFilter("t:prefab").ToArray();
            int progress = 0;

            foreach (string path in paths) {

                if (EditorUtility.DisplayCancelableProgressBar("Searching for RCCP Vehicles..", $"Scanned {progress}/{paths.Length} prefabs. Found {foundPrefabs.Count} new vehicles.", progress / (float)paths.Length)) {

                    cancelled = true;
                    break;

                }

                progress++;

                RCCP_CarController rccp = AssetDatabase.LoadAssetAtPath<RCCP_CarController>(path);

                if (!rccp)
                    continue;

                List<RCCP_CarController> allVehicles = RCCP_DemoVehicles.Instance.vehicles.ToList();

                if (!allVehicles.Contains(rccp))
                    foundPrefabs.Add(rccp);

            }

        }

        finally {

            EditorUtility.ClearProgressBar();

            if (!cancelled) {

                List<RCCP_CarController> allVehicles = RCCP_DemoVehicles.Instance.vehicles.ToList();

                allVehicles.AddRange(foundPrefabs);

                prop.vehicles = allVehicles.ToArray();

                allVehicles.Clear();
                allVehicles = prop.vehicles.ToList();

                allVehicles = allVehicles.OrderBy(go => go.name).ToList();

                prop.vehicles = allVehicles.ToArray();

                EditorUtility.SetDirty(target);

            }

            Resources.UnloadUnusedAssets();

        }

    }

    public static IEnumerable<string> SearchByFilter(string filter) {

        foreach (string guid in AssetDatabase.FindAssets(filter))
            yield return AssetDatabase.GUIDToAssetPath(guid);

    }

}
