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

public class RCCP_EditorWindows : Editor {

    #region Edit Settings
#if RCCP_SHORTCUTS
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Edit RCCP Settings #s", false, -100)]
    public static void OpenRCCSettings() {
        Selection.activeObject = RCCP_Settings.Instance;
    }
#else
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Edit RCCP Settings", false, -100)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Edit RCCP Settings", false, -100)]
    public static void OpenRCCSettings() {
        Selection.activeObject = RCCP_Settings.Instance;
    }
#endif
    #endregion

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Add Main Controller To Vehicle", false, -85)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Add Main Controller To Vehicle", false, -85)]
    public static void AddMainControllerToVehicle() {

        if (Selection.activeGameObject != null) {

            if (Selection.gameObjects.Length == 1 && Selection.activeGameObject.scene.name != null && !EditorUtility.IsPersistent(Selection.activeGameObject))
                RCCP_CreateNewVehicle.NewVehicle(Selection.activeGameObject);
            else
                EditorUtility.DisplayDialog("Selection", "Please select only one vehicle in the scene. Be sure to select root of the vehicle gameobject before adding the main controller", "Care to try again?", "Yesn't");

        }

    }

    #region Configure

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Configure/Ground Materials", false, -65)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Configure/Ground Materials", false, -65)]
    public static void OpenGroundMaterialsSettings() {
        Selection.activeObject = RCCP_GroundMaterials.Instance;
    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Configure/Changable Wheels", false, -65)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Configure/Changable Wheels", false, -65)]
    public static void OpenChangableWheelSettings() {
        Selection.activeObject = RCCP_ChangableWheels.Instance;
    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Configure/Recorded Clips", false, -65)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Configure/Recorded Clips", false, -65)]
    public static void OpenRecordSettings() {
        Selection.activeObject = RCCP_Records.Instance;
    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Configure/Demo/Demo Vehicles", false, -65)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Configure/Demo/Demo Vehicles", false, -65)]
    public static void OpenDemoVehiclesSettings() {
        Selection.activeObject = RCCP_DemoVehicles.Instance;
    }

#if RCCP_PHOTON && PHOTON_UNITY_NETWORKING
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Configure/Demo Vehicles (Photon)", false, -65)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Configure/Demo Vehicles (Photon)", false, -65)]
    public static void OpenPhotonDemoVehiclesSettings() {
        Selection.activeObject = RCCP_DemoVehicles_Photon.Instance;
    }
#endif

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Configure/Demo/Demo Scenes", false, -65)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Configure/Demo/Demo Scenes", false, -65)]
    public static void OpenDemoScenesSettings() {
        Selection.activeObject = RCCP_DemoScenes.Instance;
    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Configure/Demo/Demo Materials", false, -65)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Configure/Demo/Demo Materials", false, -65)]
    public static void OpenDemoMaterialsSettings() {
        Selection.activeObject = RCCP_DemoMaterials.Instance;
    }
    #endregion

    #region Managers

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Create/Managers/Add RCCP Scene Manager To Scene", false, -50)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Create/Managers/Add RCCP Scene Manager To Scene", false, -50)]
    public static void CreateRCCPSceneManager() {
        Selection.activeObject = RCCP_SceneManager.Instance;
    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Create/Managers/Add RCCP Skidmarks Manager To Scene", false, -50)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Create/Managers/Add RCCP Skidmarks Manager To Scene", false, -50)]
    public static void CreateRCCPSkidmarksManager() {
        Selection.activeObject = RCCP_SkidmarksManager.Instance;
    }

    #endregion

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Create/Scene/Add RCCP Camera To Scene", false, -50)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Create/Scene/Add RCCP Camera To Scene", false, -50)]
    public static void CreateRCCCamera() {

        if (FindObjectOfType<RCCP_Camera>(true)) {

            EditorUtility.DisplayDialog("Scene has RCCP Camera already!", "Scene has RCCP Camera already!", "Close");
            Selection.activeGameObject = FindObjectOfType<RCCP_Camera>().gameObject;

        } else {

            GameObject cam = Instantiate(RCCP_Settings.Instance.RCCPMainCamera.gameObject);
            cam.name = RCCP_Settings.Instance.RCCPMainCamera.name;
            Selection.activeGameObject = cam.gameObject;

        }

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Create/Scene/Add RCCP UI Canvas To Scene", false, -50)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Create/Scene/Add RCCP UI Canvas To Scene", false, -50)]
    public static void CreateRCCUICanvas() {

        if (FindObjectOfType<RCCP_UIManager>(true)) {

            EditorUtility.DisplayDialog("Scene has RCCP UI Canvas already!", "Scene has RCCP UI Canvas already!", "Close");
            Selection.activeGameObject = FindObjectOfType<RCCP_UIManager>(true).gameObject;

        } else {

            GameObject cam = Instantiate(RCCP_Settings.Instance.RCCPCanvas.gameObject);
            cam.name = RCCP_Settings.Instance.RCCPCanvas.name;
            Selection.activeGameObject = cam.gameObject;

        }

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Create/Scene/Add AI Waypoints Container To Scene", false, -50)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Create/Scene/Add AI Waypoints Container To Scene", false, -50)]
    public static void CreateRCCAIWaypointManager() {

        GameObject wpContainer = new GameObject("RCCP_AI_WaypointsContainer");
        wpContainer.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        wpContainer.AddComponent<RCCP_AIWaypointsContainer>();
        Selection.activeGameObject = wpContainer;

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Create/Scene/Add AI Brake Zones Container To Scene", false, -50)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Create/Scene/Add AI Brake Zones Container To Scene", false, -50)]
    public static void CreateRCCAIBrakeManager() {

        GameObject bzContainer = new GameObject("RCCP_AI_BrakeZonesContainer");
        bzContainer.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        bzContainer.AddComponent<RCCP_AIBrakeZonesContainer>();
        Selection.activeGameObject = bzContainer;

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/URP/To URP/[Step 1] Import URP Shaders", false, 0)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/URP/To URP/[Step 1] Import URP Shaders", false, 0)]
    public static void URPPackage() {

        EditorUtility.DisplayDialog("Importing URP Shaders", "URP shaders will be imported and builtin custom shaders will be removed.", "Proceed");

        AssetDatabase.ImportPackage(RCCP_AddonPackages.Instance.GetAssetPath(RCCP_DemoContent.Instance.URPShaderPackage), true);
        AssetDatabase.importPackageCompleted += CompletedURPPackageImport;

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/URP/To URP/[Step 2] Convert All Demo Materials To URP", false, 0)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/URP/To URP/[Step 2] Convert All Demo Materials To URP", false, 0)]
    public static void URP() {

        EditorUtility.DisplayDialog("Converting All Demo Materials To URP", "All demo materials will be selected in your project now. After that, you'll need to convert them to URP shaders while they have been selected.\n\nYou can convert them from the Edit --> Render Pipeline --> Universal Render Pipeline --> Convert Selected Materials.", "Select All Demo Materials");

        List<UnityEngine.Object> objects = new List<UnityEngine.Object>();

        for (int i = 0; i < RCCP_DemoMaterials.Instance.demoMaterials.Length; i++) {

            if (RCCP_DemoMaterials.Instance.demoMaterials[i] != null && RCCP_DemoMaterials.Instance.demoMaterials[i].material != null)
                objects.Add(RCCP_DemoMaterials.Instance.demoMaterials[i].material);

        }

        UnityEngine.Object[] orderedObjects = new UnityEngine.Object[objects.Count];

        for (int i = 0; i < orderedObjects.Length; i++)
            orderedObjects[i] = objects[i];

        Selection.objects = orderedObjects;

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/URP/To URP/[Step 3] Convert All Demo Vehicle Body Materials To URP", false, 0)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/URP/To URP/[Step 3] Convert All Demo Vehicle Body Materials To URP", false, 0)]
    public static void URPBodyShader() {

        EditorUtility.DisplayDialog("Converting All Demo Body Materials To URP", "Shaders of the demo vehicles will be converted to URP shader named ''RCCP Car Body Shader URP''.", "Convert");

        for (int i = 0; i < RCCP_DemoMaterials.Instance.vehicleBodyMaterials.Length; i++)
            RCCP_DemoMaterials.Instance.vehicleBodyMaterials[i].shader = Shader.Find("RCCP Car Body Shader URP");

        for (int i = 0; i < RCCP_DemoMaterials.Instance.wheelBlurMaterials.Length; i++)
            RCCP_DemoMaterials.Instance.wheelBlurMaterials[i].shader = Shader.Find("RCCP_WheelBlur_URP");

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/URP/To URP/[Step 4] Remove Builtin Shaders", false, 0)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/URP/To URP/[Step 4] Remove Builtin Shaders", false, 0)]
    public static void RemoveBuiltinShader() {

        if (EditorUtility.DisplayDialog("Removing Builtin Shaders", "Builtin shaders will be removed, otherwise you'll have warnings on your console. You can import them again from welcome window of the RCCP (Tools --> BCG --> RCCP --> Welcome Window)'.", "Remove Builtin Shaders", "Cancel")) {

            FileUtil.DeleteFileOrDirectory(RCCP_GetAssetPath.GetAssetPath(RCCP_DemoContent.Instance.builtinShadersContent));
            AssetDatabase.Refresh();

        }

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/URP/To Builtin/[Step 1] Import Builtin Shaders", false, 0)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/URP/To Builtin/[Step 1] Import Builtin Shaders", false, 0)]
    public static void BuiltinPackage() {

        EditorUtility.DisplayDialog("Importing Builtin Shaders", "Builtin shaders will be imported and builtin custom URP shaders will be removed.", "Proceed");

        AssetDatabase.ImportPackage(RCCP_AddonPackages.Instance.GetAssetPath(RCCP_DemoContent.Instance.builtinShaderPackage), true);
        AssetDatabase.importPackageCompleted += CompletedBuiltinPackageImport;

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/URP/To Builtin/[Step 2] Convert All Demo Materials To Builtin", false, 0)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/URP/To Builtin/[Step 2] Convert All Demo Materials To Builtin", false, 0)]
    public static void Builtin() {

        EditorUtility.DisplayDialog("Converting All Demo Materials To builtin", "All demo materials will be converted to default shaders.", "Convert");

        for (int i = 0; i < RCCP_DemoMaterials.Instance.demoMaterials.Length; i++) {

            if (RCCP_DemoMaterials.Instance.demoMaterials[i] != null && RCCP_DemoMaterials.Instance.demoMaterials[i].material != null) {

                if (RCCP_DemoMaterials.Instance.demoMaterials[i].DefaultShader != null && RCCP_DemoMaterials.Instance.demoMaterials[i].DefaultShader != "")
                    RCCP_DemoMaterials.Instance.demoMaterials[i].material.shader = Shader.Find(RCCP_DemoMaterials.Instance.demoMaterials[i].DefaultShader);

            }

        }

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/URP/To Builtin/[Step 3] Convert All Demo Vehicle Body Materials To Builtin", false, 0)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/URP/To Builtin/[Step 3] Convert All Demo Vehicle Body Materials To Builtin", false, 0)]
    public static void BuiltinBodyShader() {

        EditorUtility.DisplayDialog("Converting All Demo Body Materials To Builtin", "Shaders of the demo vehicles will be converted to builtin shader named ''RCCP Car Body Shader''.", "Convert");

        for (int i = 0; i < RCCP_DemoMaterials.Instance.vehicleBodyMaterials.Length; i++)
            RCCP_DemoMaterials.Instance.vehicleBodyMaterials[i].shader = Shader.Find("RCCP Car Body Shader");

        for (int i = 0; i < RCCP_DemoMaterials.Instance.wheelBlurMaterials.Length; i++)
            RCCP_DemoMaterials.Instance.wheelBlurMaterials[i].shader = Shader.Find("RCCP_WheelBlur");

    }

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/URP/To Builtin/[Step 4] Remove URP Shaders", false, 0)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/URP/To Builtin/[Step 4] Remove URP Shaders", false, 0)]
    public static void RemoveURPShader() {

        if (EditorUtility.DisplayDialog("Removing URP Shaders", "URP shaders will be removed, otherwise you'll have warnings on your console. You can import them again from welcome window of the RCCP (Tools --> BCG --> RCCP --> Welcome Window)'.", "Remove URP Shaders", "Cancel")) {

            FileUtil.DeleteFileOrDirectory(RCCP_GetAssetPath.GetAssetPath(RCCP_DemoContent.Instance.URPShadersContent));
            AssetDatabase.Refresh();

        }

    }

    #region Help
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Help", false, 0)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Help", false, 0)]
    public static void Help() {

        EditorUtility.DisplayDialog("Contact", "Please include your invoice number while sending a contact form. I usually respond within a business day.", "Close");

        string url = "http://www.bonecrackergames.com/contact/";
        Application.OpenURL(url);

    }
    #endregion Help

    //    #region Logitech
    //#if RCC_LOGITECH
    //	[MenuItem("Tools/BoneCracker Games/Realistic Car Controller/Create/Logitech/Logitech Manager", false, -50)]
    //	public static void CreateLogitech() {

    //		RCC_LogitechSteeringWheel logi = RCC_LogitechSteeringWheel.Instance;
    //		Selection.activeGameObject = logi.gameObject;

    //	}
    //#endif
    //    #endregion

    //[MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Export Project Settings", false, 10)]
    //public static void ExportProjectSettings() {

    //    string[] projectContent = new string[] { "ProjectSettings/InputManager.asset" };
    //    AssetDatabase.ExportPackage(projectContent, "RCCP_ProjectSettings.unitypackage", ExportPackageOptions.Interactive | ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);
    //    Debug.Log("Project Exported");

    //}

    private static void CompletedURPPackageImport(string packageName) {

        FileUtil.DeleteFileOrDirectory(RCCP_GetAssetPath.GetAssetPath(RCCP_DemoContent.Instance.builtinShadersContent));
        AssetDatabase.Refresh();

    }

    private static void CompletedBuiltinPackageImport(string packageName) {

        FileUtil.DeleteFileOrDirectory(RCCP_GetAssetPath.GetAssetPath(RCCP_DemoContent.Instance.URPShadersContent));
        AssetDatabase.Refresh();

    }

}
