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
using UnityEditor.SceneManagement;

public class RCCP_WelcomeWindow : EditorWindow {

    public class ToolBar {

        public string title;
        public UnityEngine.Events.UnityAction Draw;

        /// <summary>
        /// Create New Toolbar
        /// </summary>
        /// <param name="title">Title</param>
        /// <param name="onDraw">Method to draw when toolbar is selected</param>
        public ToolBar(string title, UnityEngine.Events.UnityAction onDraw) {

            this.title = title;
            this.Draw = onDraw;

        }

        public static implicit operator string(ToolBar tool) {

            return tool.title;

        }

    }

    /// <summary>
    /// Index of selected toolbar.
    /// </summary>
    public int toolBarIndex = 0;

    /// <summary>
    /// List of Toolbars
    /// </summary>
    public ToolBar[] toolBars = new ToolBar[]{

        new ToolBar("Welcome", WelcomePageContent),
        new ToolBar("Demos", DemosPageContent),
        new ToolBar("Addons", Addons),
        new ToolBar("Shaders", ShadersContent),
        new ToolBar("Keys", Keys),
        new ToolBar("Updates", UpdatePageContent),
        new ToolBar("DOCS", Documentations)

    };

    public static Texture2D bannerTexture = null;

    private GUISkin skin;

    private const int windowWidth = 640;
    private const int windowHeight = 720;

    Vector2 scrollView;

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Welcome Window", false, 100)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Welcome Window", false, 100)]
    public static void OpenWindow() {

        GetWindow<RCCP_WelcomeWindow>(true);

    }

    private void OnEnable() {

        titleContent = new GUIContent("Realistic Car Controller Pro");
        minSize = new Vector2(windowWidth, windowHeight);

        InitStyle();

    }

    private void InitStyle() {

        if (!skin)
            skin = Resources.Load("RCCP_Gui") as GUISkin;

        bannerTexture = (Texture2D)Resources.Load("Editor Icons/RCCP_Banner", typeof(Texture2D));

    }

    private void OnGUI() {

        GUI.skin = skin;

        DrawHeader();
        DrawMenuButtons();
        DrawToolBar();
        DrawFooter();

        if (!EditorApplication.isPlaying)
            Repaint();

    }

    private void DrawHeader() {

        GUILayout.Label(bannerTexture, GUILayout.Height(120));

    }

    private void DrawMenuButtons() {

        GUILayout.Space(-6);
        toolBarIndex = GUILayout.Toolbar(toolBarIndex, ToolbarNames());

    }

    #region ToolBars

    public static void WelcomePageContent() {

        GUILayout.Label("<size=18><color=#FF9500>Welcome!</color></size>");
        EditorGUILayout.BeginHorizontal(GUI.skin.box);
        GUILayout.Label("Thank you for purchasing and using Realistic Car Controller Pro. Please read the documentations before use. Also check out the online documentations for updated info. Have fun :)");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Separator();

#if !RCCP_DEMO

        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.HelpBox("Demo assets are not included. If you want to use demo assets (vehicles, scenes, examples, etc...), you must import demo assets to your project. You can import them from ''Addons'' tab.", MessageType.Warning, true);

        if (GUILayout.Button("Open Prototype Demo Scene")) {

            RCCP_DemoScenes.Instance.GetPaths();
            EditorSceneManager.OpenScene(RCCP_DemoScenes.Instance.path_demo_protototype, OpenSceneMode.Single);

        }

        EditorGUILayout.EndVertical();

        GUI.enabled = false;

#endif

        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.Space();

        if (GUILayout.Button("Add Demo Scenes To Build Settings"))
            AddDemoScenesToBuildSettings();

        EditorGUILayout.Separator();
        EditorGUILayout.HelpBox("If you want to add Photon PUN2 scenes, import and install Photon PUN2 & integration first. Then click again to add those scenes to your Build Settings.", MessageType.Info, true);
        EditorGUILayout.HelpBox("If you want to add Enter / Exit scenes, import BCG Shared Assets to your project first. Then click again to add those scenes to your Build Settings.", MessageType.Info, true);
        EditorGUILayout.Separator();

        EditorGUILayout.EndVertical();

        GUI.color = Color.red;

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Delete all demo contents from the project")) {

            if (EditorUtility.DisplayDialog("Warning", "You are about to delete all demo contents such as vehicle models, vehicle prefabs, vehicle textures, all scenes, scene models, scene prefabs, scene textures!", "Delete", "Cancel"))
                DeleteDemoContent();

        }

        GUI.color = Color.white;
        GUI.enabled = true;

    }

    public static void UpdatePageContent() {

        GUILayout.Label("<size=18><color=#FF9500>Updates</color></size>");

        EditorGUILayout.BeginHorizontal(GUI.skin.box);
        GUILayout.Label("<b>Installed Version: </b>" + RCCP_Version.version.ToString());
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(6);

        EditorGUILayout.BeginHorizontal(GUI.skin.box);
        GUILayout.Label("<b>1</b>- Always backup your project before updating RCCP or any asset in your project!");
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(6);

        EditorGUILayout.BeginHorizontal(GUI.skin.box);
        GUILayout.Label("<b>2</b>- If you have own assets such as prefabs, audioclips, models, scripts in Realistic Car Controller Pro folder, keep your own asset outside from Realistic Car Controller Pro folder.");
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(6);

        EditorGUILayout.BeginHorizontal(GUI.skin.box);
        GUILayout.Label("<b>3</b>- Delete Realistic Car Controller Pro folder, and import latest version to your project.");
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(6);

        if (GUILayout.Button("Check Updates"))
            Application.OpenURL(RCCP_AssetPaths.assetStorePath);

        GUILayout.Space(6);

        GUILayout.FlexibleSpace();

    }

    public static void ShadersContent() {

        GUILayout.Label("<size=18><color=#FF9500>Shaders [Builtin - URP - HDRP]</color></size>");

        EditorGUILayout.BeginHorizontal(GUI.skin.box);
        GUILayout.Label("RCCP comes with builtin render pipeline shaders. If your project is running on URP or HDRP, you'll need to convert demo materials of the RCCP and import URP shaders. There are four simple steps to do this process. \n\n1. Importing URP shaders\n\n2. Selecting all demo materials of the RCCP in the project, and converting them through Edit --> Rendering --> Convert menu." +
            "\n\n3. Converting car body shader. All demo vehicles are using a custom shader for their bodies. If you're going to use this shader, you'll need to convert.\n\n4. Remove the builtin shaders. You won't need them anymore.");
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(6);

        EditorGUILayout.BeginHorizontal(GUI.skin.box);
        GUILayout.Label("<b><color=#FF9500>Important</color></b> - Always backup your project before doing this!");
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(6);

        EditorGUILayout.BeginHorizontal(GUI.skin.box);
        GUILayout.Label("All these steps can be done through the Tools --> BCG --> RCCP --> URP --> To URP / To Builtin menu, or welcome window.");
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(6);

        GUILayout.FlexibleSpace();

    }

    public static void DemosPageContent() {

        GUILayout.Label("<size=18><color=#FF9500>Demo Scenes</color></size>");

        EditorGUILayout.BeginVertical(GUI.skin.box);

        GUILayout.Label("<b>Demo Content</b>");

#if !RCCP_DEMO

        bool decisionToImportDemo = false;

        if (GUILayout.Button("Import Demo Content"))
            decisionToImportDemo = EditorUtility.DisplayDialog("Import Demo Content", "Do you want to import demo assets to your project? Be adviced, this will increase your build size even if you don't use them. You can always remove the demo assets from your project by using 'Delete Demo Content From Project' button in welcome window.", "Yes, import demo assets", "No");

        if (decisionToImportDemo)
            AssetDatabase.ImportPackage(RCCP_AddonPackages.Instance.GetAssetPath(RCCP_AddonPackages.Instance.demoPackage), true);

#else

        EditorGUILayout.HelpBox("Installed RCCP Demo Assets, You can open demo scenes and use demo content now.", MessageType.Info);
        GUILayout.Space(6);

#endif

        EditorGUILayout.EndVertical();

#if !RCCP_DEMO
        EditorGUILayout.HelpBox("Demo assets are not included. You can import them from ''Addons'' tab.", MessageType.Info, true);
        GUI.enabled = false;
#endif

        EditorGUILayout.Separator();
        EditorGUILayout.HelpBox("All scenes must be in your Build Settings to run AIO demo.", MessageType.Warning, true);
        EditorGUILayout.Separator();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.BeginHorizontal(GUI.skin.box);

        if (GUILayout.Button("RCCP City AIO")) {

            RCCP_DemoScenes.Instance.GetPaths();
            EditorSceneManager.OpenScene(RCCP_DemoScenes.Instance.path_city_AIO, OpenSceneMode.Single);
            EditorUtility.SetDirty(RCCP_DemoScenes.Instance);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

        }

        if (GUILayout.Button("RCCP City")) {

            RCCP_DemoScenes.Instance.GetPaths();
            EditorSceneManager.OpenScene(RCCP_DemoScenes.Instance.path_demo_City, OpenSceneMode.Single);
            EditorUtility.SetDirty(RCCP_DemoScenes.Instance);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

        }

        if (GUILayout.Button("RCCP City Car Selection")) {

            RCCP_DemoScenes.Instance.GetPaths();
            EditorSceneManager.OpenScene(RCCP_DemoScenes.Instance.path_demo_CarSelection, OpenSceneMode.Single);
            EditorUtility.SetDirty(RCCP_DemoScenes.Instance);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal(GUI.skin.box);

        if (GUILayout.Button("RCCP Blank API")) {

            RCCP_DemoScenes.Instance.GetPaths();
            EditorSceneManager.OpenScene(RCCP_DemoScenes.Instance.path_demo_APIBlank, OpenSceneMode.Single);
            EditorUtility.SetDirty(RCCP_DemoScenes.Instance);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

        }

        if (GUILayout.Button("RCCP Blank")) {

            RCCP_DemoScenes.Instance.GetPaths();
            EditorSceneManager.OpenScene(RCCP_DemoScenes.Instance.path_demo_BlankMobile, OpenSceneMode.Single);
            EditorUtility.SetDirty(RCCP_DemoScenes.Instance);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

        }

        if (GUILayout.Button("RCCP Damage")) {

            RCCP_DemoScenes.Instance.GetPaths();
            EditorSceneManager.OpenScene(RCCP_DemoScenes.Instance.path_demo_Damage, OpenSceneMode.Single);
            EditorUtility.SetDirty(RCCP_DemoScenes.Instance);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal(GUI.skin.box);

        if (GUILayout.Button("RCCP Customization")) {

            RCCP_DemoScenes.Instance.GetPaths();
            EditorSceneManager.OpenScene(RCCP_DemoScenes.Instance.path_demo_Customization, OpenSceneMode.Single);
            EditorUtility.SetDirty(RCCP_DemoScenes.Instance);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

        }

        if (GUILayout.Button("RCCP Override Inputs")) {

            RCCP_DemoScenes.Instance.GetPaths();
            EditorSceneManager.OpenScene(RCCP_DemoScenes.Instance.path_demo_OverrideInputs, OpenSceneMode.Single);
            EditorUtility.SetDirty(RCCP_DemoScenes.Instance);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

        }

        if (GUILayout.Button("RCCP Transport")) {

            RCCP_DemoScenes.Instance.GetPaths();
            EditorSceneManager.OpenScene(RCCP_DemoScenes.Instance.path_demo_Transport, OpenSceneMode.Single);
            EditorUtility.SetDirty(RCCP_DemoScenes.Instance);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.Separator();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        GUI.enabled = true;

#if BCG_ENTEREXIT

        EditorGUILayout.BeginHorizontal(GUI.skin.box);

        if (BCG_DemoScenes.Instance.demo_CityFPS && GUILayout.Button("RCCP Blank Enter-Exit FPS")) {

            BCG_DemoScenes.Instance.GetPaths();
            EditorSceneManager.OpenScene(BCG_DemoScenes.Instance.path_demo_BlankFPS, OpenSceneMode.Single);
            EditorUtility.SetDirty(BCG_DemoScenes.Instance);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

        }

        if (BCG_DemoScenes.Instance.demo_CityTPS && GUILayout.Button("RCCP Blank Enter-Exit TPS")) {

            BCG_DemoScenes.Instance.GetPaths();
            EditorSceneManager.OpenScene(BCG_DemoScenes.Instance.path_demo_BlankTPS, OpenSceneMode.Single);
            EditorUtility.SetDirty(BCG_DemoScenes.Instance);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

        }

        EditorGUILayout.EndHorizontal();

#if RCCP_DEMO

        EditorGUILayout.BeginHorizontal(GUI.skin.box);

        if (BCG_DemoScenes.Instance.demo_CityFPS && GUILayout.Button("RCCP City Enter-Exit FPS")) {

            BCG_DemoScenes.Instance.GetPaths();
            EditorSceneManager.OpenScene(BCG_DemoScenes.Instance.path_demo_CityFPS, OpenSceneMode.Single);
            EditorUtility.SetDirty(BCG_DemoScenes.Instance);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

        }

        if (BCG_DemoScenes.Instance.demo_CityTPS && GUILayout.Button("RCCP City Enter-Exit TPS")) {

            BCG_DemoScenes.Instance.GetPaths();
            EditorSceneManager.OpenScene(BCG_DemoScenes.Instance.path_demo_CityTPS, OpenSceneMode.Single);
            EditorUtility.SetDirty(BCG_DemoScenes.Instance);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

        }

        EditorGUILayout.EndHorizontal();

#endif

#else

        EditorGUILayout.HelpBox("If you want to add enter exit scenes, you have to import latest BCG Shared Assets to your project first.", MessageType.Warning);

        if (GUILayout.Button("Download and import BCG Shared Assets"))
            AssetDatabase.ImportPackage(RCCP_AddonPackages.Instance.GetAssetPath(RCCP_AddonPackages.Instance.BCGSharedAssets), true);

#endif

        EditorGUILayout.EndVertical();

        EditorGUILayout.Separator();

        EditorGUILayout.BeginVertical(GUI.skin.box);

        bool photonInstalled = false;

#if PHOTON_UNITY_NETWORKING
        photonInstalled = true;
#endif

        bool photonAndRCCInstalled = false;

#if RCCP_PHOTON && PHOTON_UNITY_NETWORKING
        photonAndRCCInstalled = true;
#endif

        if (!photonAndRCCInstalled) {

            if (!photonInstalled) {

                EditorGUILayout.HelpBox("If you want to add Photon PUN2 scenes, you have to import latest Photon PUN2 to your project first.", MessageType.Warning);
                EditorGUILayout.HelpBox("You have to import latest Photon PUN2 to your project first.", MessageType.Warning);

                if (GUILayout.Button("Download and import Photon PUN2"))
                    Application.OpenURL(RCCP_AssetPaths.photonPUN2);

            } else {

                EditorGUILayout.HelpBox("Found Photon PUN2, You can import integration package and open Photon demo scenes now.", MessageType.Info);

                if (GUILayout.Button("Import Photon PUN2 Integration"))
                    AssetDatabase.ImportPackage(RCCP_AddonPackages.Instance.GetAssetPath(RCCP_AddonPackages.Instance.PhotonPUN2), true);

            }

        } else if (photonInstalled) {

#if RCCP_PHOTON && PHOTON_UNITY_NETWORKING

            EditorGUILayout.BeginHorizontal(GUI.skin.box);

            if (GUILayout.Button("RCCP Lobby Photon")) {

                RCCP_DemoScenes_Photon.Instance.GetPaths();
                EditorSceneManager.OpenScene(RCCP_DemoScenes_Photon.Instance.path_demo_PUN2Lobby, OpenSceneMode.Single);

            }

            if (GUILayout.Button("RCCP Blank Photon")) {

                RCCP_DemoScenes_Photon.Instance.GetPaths();
                EditorSceneManager.OpenScene(RCCP_DemoScenes_Photon.Instance.path_demo_PUN2City, OpenSceneMode.Single);

            }

            EditorGUILayout.EndHorizontal();

#endif

        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(GUI.skin.box);

        bool mirrorInstalled = false;

#if MIRROR
        mirrorInstalled = true;
#endif

        bool mirrorAndRCCPInstalled = false;

#if RCCP_MIRROR && MIRROR
        mirrorAndRCCPInstalled = true;
#endif

        if (!mirrorAndRCCPInstalled) {

            if (!mirrorInstalled) {

                EditorGUILayout.HelpBox("If you want to add mirror demo scenes, you have to import latest Mirror to your project first.", MessageType.Warning);
                EditorGUILayout.HelpBox("You have to import latest Mirror to your project first.", MessageType.Warning);

                if (GUILayout.Button("Download and import Mirror"))
                    Application.OpenURL(RCCP_AssetPaths.mirror);

            } else {

                EditorGUILayout.HelpBox("Found Mirror, You can import integration package and open Mirror demo scenes now.", MessageType.Info);

                if (GUILayout.Button("Import Mirror Integration"))
                    AssetDatabase.ImportPackage(RCCP_AddonPackages.Instance.GetAssetPath(RCCP_AddonPackages.Instance.mirror), true);

            }

        } else if (mirrorInstalled) {

#if RCCP_MIRROR && MIRROR

            EditorGUILayout.BeginHorizontal(GUI.skin.box);

            if (GUILayout.Button("RCCP Blank Mirror")) {

                RCCP_DemoScenes_Mirror.Instance.GetPaths();
                EditorSceneManager.OpenScene(RCCP_DemoScenes_Mirror.Instance.path_Demo_Blank_Mirror, OpenSceneMode.Single);

            }

            EditorGUILayout.EndHorizontal();

#endif

        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.EndVertical();
        GUILayout.FlexibleSpace();

    }

    public static void Addons() {

        GUILayout.Label("<size=18><color=#FF9500>Addons</color></size>");

        EditorGUILayout.BeginVertical(GUI.skin.box);

        GUILayout.Label("<b>Demo Content</b>");

#if !RCCP_DEMO

        bool decisionToImportDemo = false;

        if (GUILayout.Button("Import Demo Content"))
            decisionToImportDemo = EditorUtility.DisplayDialog("Import Demo Content", "Do you want to import demo assets to your project? Be adviced, this will increase your build size even if you don't use them. You can always remove the demo assets from your project by using 'Delete Demo Content From Project' button in welcome window.", "Yes, import demo assets", "No");

        if (decisionToImportDemo)
            AssetDatabase.ImportPackage(RCCP_AddonPackages.Instance.GetAssetPath(RCCP_AddonPackages.Instance.demoPackage), true);

#else

        EditorGUILayout.HelpBox("Installed RCCP Demo Assets, You can open demo scenes and use demo content now.", MessageType.Info);
        GUILayout.Space(6);

#endif

        EditorGUILayout.EndVertical();

        EditorGUILayout.Separator();

        EditorGUILayout.BeginVertical(GUI.skin.box);

        GUILayout.Label("<b>Shaders</b>");

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Import Builtin Shaders")) {

            AssetDatabase.ImportPackage(RCCP_GetAssetPath.GetAssetPath(RCCP_DemoContent.Instance.builtinShaderPackage), true);
            AssetDatabase.Refresh();

        }

        if (GUILayout.Button("Import URP Shaders")) {

            AssetDatabase.ImportPackage(RCCP_GetAssetPath.GetAssetPath(RCCP_DemoContent.Instance.URPShaderPackage), true);
            AssetDatabase.Refresh();

        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        GUI.color = Color.red;

        if (GUILayout.Button("Remove Builtin Shaders")) {

            FileUtil.DeleteFileOrDirectory(RCCP_GetAssetPath.GetAssetPath(RCCP_DemoContent.Instance.builtinShadersContent));
            AssetDatabase.Refresh();

        }

        if (GUILayout.Button("Remove URP Shaders")) {

            FileUtil.DeleteFileOrDirectory(RCCP_GetAssetPath.GetAssetPath(RCCP_DemoContent.Instance.URPShadersContent));
            AssetDatabase.Refresh();

        }

        GUI.color = Color.white;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.Separator();

        EditorGUILayout.BeginVertical(GUI.skin.box);

        GUILayout.Label("<b>Photon PUN2</b>");

        bool photonInstalled = false;

#if PHOTON_UNITY_NETWORKING
        photonInstalled = true;
#endif

        bool photonAndRCCInstalled = false;

#if RCCP_PHOTON && PHOTON_UNITY_NETWORKING
        photonAndRCCInstalled = true;
#endif

        if (!photonAndRCCInstalled) {

            if (!photonInstalled) {

                EditorGUILayout.HelpBox("You have to import latest Photon PUN2 to your project first.", MessageType.Warning);

                if (GUILayout.Button("Download and import Photon PUN2"))
                    Application.OpenURL(RCCP_AssetPaths.photonPUN2);

            } else {

                EditorGUILayout.HelpBox("Found Photon PUN2, You can import integration package and open Photon demo scenes now.", MessageType.Info);

                if (GUILayout.Button("Import Photon PUN2 Integration"))
                    AssetDatabase.ImportPackage(RCCP_AddonPackages.Instance.GetAssetPath(RCCP_AddonPackages.Instance.PhotonPUN2), true);

            }

        } else if (photonInstalled) {

            EditorGUILayout.HelpBox("Installed Photon PUN2 with RCCP, You can open Photon demo scenes now.", MessageType.Info);
            EditorGUILayout.HelpBox("If you want to remove Photon PUN2 integration from the project, delete the ''Photon PUN2'' folder inside the Addons/Installed folder. After that, you need to remove ''RCCP_PHOTON'' scripting define symbol in your player settings. In order to do that, go to Edit --> Project Settings --> Player Settings --> Other Settings, and remove the scripting symbol from the list.", MessageType.Warning, true);

        }

#if RCCP_PHOTON && PHOTON_UNITY_NETWORKING
        if (photonInstalled) {

            EditorGUILayout.LabelField("Photon PUN2 Version: " + System.Reflection.Assembly.GetAssembly(typeof(ExitGames.Client.Photon.PhotonPeer)).GetName().Version.ToString(), EditorStyles.centeredGreyMiniLabel);
            GUILayout.Space(6);

        }
#endif

        EditorGUILayout.EndVertical();

        bool BCGInstalled = false;

#if BCG_ENTEREXIT
        BCGInstalled = true;
#endif

        EditorGUILayout.Separator();

        EditorGUILayout.BeginVertical(GUI.skin.box);

        GUILayout.Label("<b>BCG Shared Assets (Enter / Exit)</b>");

        if (!BCGInstalled) {

            EditorGUILayout.HelpBox("You have to import latest BCG Shared Assets to your project first.", MessageType.Warning);

            if (GUILayout.Button("Download and import BCG Shared Assets"))
                AssetDatabase.ImportPackage(RCCP_AddonPackages.Instance.GetAssetPath(RCCP_AddonPackages.Instance.BCGSharedAssets), true);

        } else {

            EditorGUILayout.HelpBox("Found BCG Shared Assets, You can open Enter / Exit demo scenes now.", MessageType.Info);
            EditorGUILayout.HelpBox("If you want to remove BCG Shared Assets integration from the project, delete the ''BoneCracker Games Shared Assets'' folder. After that, you need to remove ''BCG_ENTEREXIT'' scripting define symbol in your player settings. In order to do that, go to Edit --> Project Settings --> Player Settings --> Other Settings, and remove the scripting symbol from the list.", MessageType.Warning, true);

#if BCG_ENTEREXIT
            EditorGUILayout.LabelField("BCG Shared Assets Version: " + BCG_Version.version, EditorStyles.centeredGreyMiniLabel);
#endif
            GUILayout.Space(6);

        }

        EditorGUILayout.EndVertical();




        EditorGUILayout.Separator();

        EditorGUILayout.BeginVertical(GUI.skin.box);

        GUILayout.Label("<b>Mirror</b>");

        bool mirrorInstalled = false;

#if MIRROR
        mirrorInstalled = true;
#endif

        bool mirrorAndRCCPInstalled = false;

#if RCCP_MIRROR && MIRROR
        mirrorAndRCCPInstalled = true;
#endif

        if (!mirrorAndRCCPInstalled) {

            if (!mirrorInstalled) {

                EditorGUILayout.HelpBox("You have to import latest Mirror to your project first.", MessageType.Warning);

                if (GUILayout.Button("Download and import Mirror"))
                    Application.OpenURL(RCCP_AssetPaths.mirror);

            } else {

                EditorGUILayout.HelpBox("Found Mirror, You can import integration package and open Mirror demo scenes now.", MessageType.Info);

                if (GUILayout.Button("Import Mirror Integration"))
                    AssetDatabase.ImportPackage(RCCP_AddonPackages.Instance.GetAssetPath(RCCP_AddonPackages.Instance.mirror), true);

            }

        } else if (mirrorInstalled) {

            EditorGUILayout.HelpBox("Installed Mirror with RCCP, You can open Mirror demo scenes now.", MessageType.Info);
            EditorGUILayout.HelpBox("If you want to remove Mirror integration from the project, delete the ''Mirror'' folder inside the Addons/Installed folder. After that, you need to remove ''RCCP_MIRROR'' scripting define symbol in your player settings. In order to do that, go to Edit --> Project Settings --> Player Settings --> Other Settings, and remove the scripting symbol from the list.", MessageType.Warning, true);

        }

        EditorGUILayout.EndVertical();





        //EditorGUILayout.Separator();

        //EditorGUILayout.BeginVertical(GUI.skin.box);

        //GUILayout.Label("<b>Logitech</b>");

        //EditorGUILayout.BeginHorizontal();

        //if (GUILayout.Button("Download and import Logitech SDK"))
        //    Application.OpenURL(RCCP_AssetPaths.logitech);

        //if (GUILayout.Button("Import Logitech Integration"))
        //    AssetDatabase.ImportPackage(RCCP_AssetPaths.LogiAssetsPath, true);

        //EditorGUILayout.EndHorizontal();

        //EditorGUILayout.EndVertical();

        EditorGUILayout.Separator();

        EditorGUILayout.BeginVertical(GUI.skin.box);

        GUILayout.Label("<b>ProFlares</b>");

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Download and import ProFlares"))
            Application.OpenURL(RCCP_AssetPaths.proFlares);

        if (GUILayout.Button("Import ProFlares Integration"))
            AssetDatabase.ImportPackage(RCCP_AddonPackages.Instance.GetAssetPath(RCCP_AddonPackages.Instance.ProFlare), true);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace();

    }

    public static void Keys() {

        GUILayout.Label("<size=18><color=#FF9500>Scripting Define Symbols</color></size>");

        EditorGUILayout.Separator();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("RCCP uses scripting define symbols to work with other addon packages. These packages are; \n\nDemo Content\nBoneCracker Shared Assets (Enter / Exit)\nPhoton Integration\nMirror Integration\n\nIf you attempt to import these addon packages, corresponding scripting symbol will be added to your build settings. But if you remove these addon packages, scripting symbol will still exists in the build settings and throw errors.");
        EditorGUILayout.EndVertical();
        EditorGUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("After removing any addon packages, please remove the corresponding scripting symbol in your build settings.\n\nPlease don't attempt to remove the key if package is still existing in the project. Remove the package first, after that you can remove the key.");
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(GUI.skin.box);

        GUILayout.Label("<b>Installed Scripting Symbols</b>");

        EditorGUILayout.BeginHorizontal();

        GUI.color = Color.red;

        if (EditorApplication.isCompiling)
            GUI.enabled = false;

#if !BCG_ENTEREXIT
        GUI.enabled = false;
#endif

        if (GUILayout.Button("Remove BCG_ENTEREXIT"))
            RCCP_SetScriptingSymbol.SetEnabled("BCG_ENTEREXIT", false);

        if (!EditorApplication.isCompiling)
            GUI.enabled = true;

#if !RCCP_DEMO
        GUI.enabled = false;
#endif

        if (GUILayout.Button("Remove RCCP_DEMO"))
            RCCP_SetScriptingSymbol.SetEnabled("RCCP_DEMO", false);

        if (!EditorApplication.isCompiling)
            GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();

#if !RCCP_PHOTON
        GUI.enabled = false;
#endif

        if (GUILayout.Button("Remove RCCP_PHOTON"))
            RCCP_SetScriptingSymbol.SetEnabled("RCCP_PHOTON", false);

        GUI.enabled = true;

        if (EditorApplication.isCompiling)
            GUI.enabled = false;

#if !RCCP_MIRROR
        GUI.enabled = false;
#endif

        if (GUILayout.Button("Remove RCCP_MIRROR"))
            RCCP_SetScriptingSymbol.SetEnabled("RCCP_MIRROR", false);

        GUI.color = Color.white;
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.Separator();

        GUILayout.FlexibleSpace();

    }

    public static void Documentations() {

        GUILayout.Label("<size=18><color=#FF9500>Documentation</color></size>");

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.HelpBox("Offline documentations can be found in the documentations folder.", MessageType.Info);

        if (GUILayout.Button("Online Documentations"))
            Application.OpenURL(RCCP_AssetPaths.documentations);

        if (GUILayout.Button("Youtube Tutorial Videos"))
            Application.OpenURL(RCCP_AssetPaths.YTVideos);

        if (GUILayout.Button("Other Assets"))
            Application.OpenURL(RCCP_AssetPaths.otherAssets);

        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace();

    }

    #endregion

    private string[] ToolbarNames() {

        string[] names = new string[toolBars.Length];

        for (int i = 0; i < toolBars.Length; i++)
            names[i] = toolBars[i];

        return names;

    }

    private void DrawToolBar() {

        GUILayout.BeginArea(new Rect(4, 150, position.width - 8, position.height - 190));

        scrollView = EditorGUILayout.BeginScrollView(scrollView, false, false);

        toolBars[toolBarIndex].Draw();

        EditorGUILayout.EndScrollView();

        GUILayout.EndArea();
        GUILayout.FlexibleSpace();

    }

    private void DrawFooter() {

        EditorGUILayout.BeginHorizontal(GUI.skin.box);

        EditorGUILayout.LabelField("BoneCracker Games", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.LabelField("Realistic Car Controller Pro " + RCCP_Version.version, EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.LabelField("Ekrem Bugra Ozdoganlar", EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.EndHorizontal();

    }

    private static void ImportPackage(string package) {

        try {
            AssetDatabase.ImportPackage(package, true);
        }
        catch (Exception) {
            Debug.LogError("Failed to import package: " + package);
            throw;
        }

    }

    private static void DeleteDemoContent() {

        Debug.LogWarning("Deleting demo contents...");

        foreach (var item in RCCP_DemoContent.Instance.contents) {

            if (item != null)
                FileUtil.DeleteFileOrDirectory(RCCP_GetAssetPath.GetAssetPath(item));

        }

        RCCP_DemoVehicles.Instance.vehicles = new RCCP_CarController[1];
        RCCP_DemoVehicles.Instance.vehicles[0] = RCCP_PrototypeContent.Instance.vehicles[0];
        RCCP_DemoMaterials.Instance.demoMaterials = new RCCP_DemoMaterials.MaterialStructure[0];
        RCCP_DemoMaterials.Instance.vehicleBodyMaterials = new Material[0];
        RCCP_DemoScenes.Instance.Clean();

        EditorUtility.SetDirty(RCCP_DemoVehicles.Instance);
        EditorUtility.SetDirty(RCCP_DemoMaterials.Instance);
        EditorUtility.SetDirty(RCCP_DemoScenes.Instance);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        RCCP_SetScriptingSymbol.SetEnabled("RCCP_DEMO", false);

        Debug.LogWarning("Deleted demo contents!");
        EditorUtility.DisplayDialog("Deleted Demo Contents", "All demo contents have been deleted!", "Ok");

    }

    private static void AddDemoScenesToBuildSettings() {

        RCCP_DemoScenes.Instance.GetPaths();
        EditorUtility.SetDirty(RCCP_DemoScenes.Instance);

#if BCG_ENTEREXIT
        BCG_DemoScenes.Instance.GetPaths();
        EditorUtility.SetDirty(BCG_DemoScenes.Instance);
#endif

#if RCCP_PHOTON
        RCCP_DemoScenes_Photon.Instance.GetPaths();
        EditorUtility.SetDirty(RCCP_DemoScenes_Photon.Instance);
#endif

#if RCCP_MIRROR
        RCCP_DemoScenes_Mirror.Instance.GetPaths();
        EditorUtility.SetDirty(RCCP_DemoScenes_Mirror.Instance);
#endif

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        List<string> demoScenePaths = new List<string>();

        demoScenePaths.Add(RCCP_DemoScenes.Instance.path_city_AIO);
        demoScenePaths.Add(RCCP_DemoScenes.Instance.path_demo_City);
        demoScenePaths.Add(RCCP_DemoScenes.Instance.path_demo_CarSelection);
        demoScenePaths.Add(RCCP_DemoScenes.Instance.path_demo_APIBlank);
        demoScenePaths.Add(RCCP_DemoScenes.Instance.path_demo_BlankMobile);
        demoScenePaths.Add(RCCP_DemoScenes.Instance.path_demo_Damage);
        demoScenePaths.Add(RCCP_DemoScenes.Instance.path_demo_Customization);
        demoScenePaths.Add(RCCP_DemoScenes.Instance.path_demo_OverrideInputs);
        demoScenePaths.Add(RCCP_DemoScenes.Instance.path_demo_Transport);

#if BCG_ENTEREXIT

        demoScenePaths.Add(BCG_DemoScenes.Instance.path_demo_BlankFPS);
        demoScenePaths.Add(BCG_DemoScenes.Instance.path_demo_BlankTPS);

#if RCCP_DEMO

        demoScenePaths.Add(BCG_DemoScenes.Instance.path_demo_CityFPS);
        demoScenePaths.Add(BCG_DemoScenes.Instance.path_demo_CityTPS);

#endif

#endif

#if RCCP_PHOTON && PHOTON_UNITY_NETWORKING

        demoScenePaths.Add(RCCP_DemoScenes_Photon.Instance.path_demo_PUN2Lobby);
        demoScenePaths.Add(RCCP_DemoScenes_Photon.Instance.path_demo_PUN2City);

#endif

#if RCCP_MIRROR && MIRROR

        demoScenePaths.Add(RCCP_DemoScenes_Mirror.Instance.path_Demo_Blank_Mirror);

#endif

        // Find valid Scene paths and make a list of EditorBuildSettingsScene
        List<EditorBuildSettingsScene> editorBuildSettingsScenes = new List<EditorBuildSettingsScene>();

        foreach (string path in demoScenePaths) {

            if (!string.IsNullOrEmpty(path))
                editorBuildSettingsScenes.Add(new EditorBuildSettingsScene(path, true));

        }

        // Set the Build Settings window Scene list
        EditorBuildSettings.scenes = editorBuildSettingsScenes.ToArray();

        EditorUtility.DisplayDialog("Demo Scenes", "All demo scenes have been added to the Build Settings. For Photon and Enter / Exit scenes, you have to import and intregrate them first (Addons).", "Ok");

    }

}
