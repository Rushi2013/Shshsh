using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

class RCCP_PreBuildChecker : IPreprocessBuildWithReport {

    static Object RCCP_SceneManagerTexture;
    static Object RCCP_AIWaypointsContainerTexture;
    static Object RCCP_CarControllerTexture;
    static Object IRCCP_ComponentTexture;

    public int callbackOrder { get { return 0; } }

    public void OnPreprocessBuild(BuildReport report) {

        RCCP_SceneManagerTexture = Resources.Load("Editor Icons/RCCP_EditorIcon_Manager");
        RCCP_AIWaypointsContainerTexture = Resources.Load("Editor Icons/RCCP_EditorIcon_Other");
        RCCP_CarControllerTexture = Resources.Load("Editor Icons/RCCP_EditorIcon_Vehicle");
        IRCCP_ComponentTexture = Resources.Load("Editor Icons/RCCP_EditorIcon_Component");

        if (RCCP_SceneManagerTexture)
            RCCP_SceneManagerTexture.hideFlags = HideFlags.None;

        if (RCCP_AIWaypointsContainerTexture)
            RCCP_AIWaypointsContainerTexture.hideFlags = HideFlags.None;

        if (RCCP_CarControllerTexture)
            RCCP_CarControllerTexture.hideFlags = HideFlags.None;

        if (IRCCP_ComponentTexture)
            IRCCP_ComponentTexture.hideFlags = HideFlags.None;

    }

}