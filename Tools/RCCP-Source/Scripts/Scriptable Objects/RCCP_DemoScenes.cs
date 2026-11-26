//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright © 2014 - 2024 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// All demo scenes.
/// </summary>
[Icon("Assets/Realistic Car Controller Pro/Resources/Editor Icons/RCCP_EditorIcon_Manager.png")]
public class RCCP_DemoScenes : ScriptableObject {

    public int instanceId = 0;

    #region singleton
    private static RCCP_DemoScenes instance;
    public static RCCP_DemoScenes Instance { get { if (instance == null) instance = Resources.Load("RCCP_DemoScenes") as RCCP_DemoScenes; return instance; } }
    #endregion

    public Object demo_protototype;
    public Object city_AIO;
    public Object demo_City;
    public Object demo_CarSelection;
    public Object demo_APIBlank;
    public Object demo_BlankMobile;
    public Object demo_Damage;
    public Object demo_Customization;
    public Object demo_OverrideInputs;
    public Object demo_Transport;

    public string path_demo_protototype;
    public string path_city_AIO;
    public string path_demo_City;
    public string path_demo_CarSelection;
    public string path_demo_APIBlank;
    public string path_demo_BlankMobile;
    public string path_demo_Damage;
    public string path_demo_Customization;
    public string path_demo_OverrideInputs;
    public string path_demo_Transport;

    public void GetPaths() {

        if (demo_protototype != null)
            path_demo_protototype = RCCP_GetAssetPath.GetAssetPath(demo_protototype);
        else
            path_demo_protototype = "";

        if (city_AIO != null)
            path_city_AIO = RCCP_GetAssetPath.GetAssetPath(city_AIO);
        else
            path_city_AIO = "";

        if (demo_City != null)
            path_demo_City = RCCP_GetAssetPath.GetAssetPath(demo_City);
        else
            path_demo_City = "";

        if (demo_CarSelection != null)
            path_demo_CarSelection = RCCP_GetAssetPath.GetAssetPath(demo_CarSelection);
        else
            path_demo_CarSelection = "";

        if (demo_APIBlank != null)
            path_demo_APIBlank = RCCP_GetAssetPath.GetAssetPath(demo_APIBlank);
        else
            path_demo_APIBlank = "";

        if (demo_BlankMobile != null)
            path_demo_BlankMobile = RCCP_GetAssetPath.GetAssetPath(demo_BlankMobile);
        else
            path_demo_BlankMobile = "";

        if (demo_Damage != null)
            path_demo_Damage = RCCP_GetAssetPath.GetAssetPath(demo_Damage);
        else
            path_demo_Damage = "";

        if (demo_Customization != null)
            path_demo_Customization = RCCP_GetAssetPath.GetAssetPath(demo_Customization);
        else
            path_demo_Customization = "";

        if (demo_OverrideInputs != null)
            path_demo_OverrideInputs = RCCP_GetAssetPath.GetAssetPath(demo_OverrideInputs);
        else
            path_demo_OverrideInputs = "";

        if (demo_Transport != null)
            path_demo_Transport = RCCP_GetAssetPath.GetAssetPath(demo_Transport);
        else
            path_demo_Transport = "";

    }

    public void Clean() {

        city_AIO = null;
        demo_City = null;
        demo_CarSelection = null;
        demo_APIBlank = null;
        demo_BlankMobile = null;
        demo_Damage = null;
        demo_Customization = null;
        demo_OverrideInputs = null;
        demo_Transport = null;

        path_demo_protototype = "";
        path_city_AIO = "";
        path_demo_City = "";
        path_demo_CarSelection = "";
        path_demo_APIBlank = "";
        path_demo_BlankMobile = "";
        path_demo_Damage = "";
        path_demo_Customization = "";
        path_demo_OverrideInputs = "";
        path_demo_Transport = "";

    }

}
