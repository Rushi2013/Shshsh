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

// ensure class initializer is called whenever scripts recompile
[InitializeOnLoad]
public static class RCC_CheckBeforePlay {

    // register an event handler when the class is initialized
    static RCC_CheckBeforePlay() {

        EditorApplication.playModeStateChanged += LogPlayModeState;

    }

    private static void LogPlayModeState(PlayModeStateChange state) {

        if (state != PlayModeStateChange.ExitingEditMode)
            return;

        if (EditorPrefs.GetBool("RCCP_IgnorePlatformWarnings", false) == false) {

            int i;

            if (!RCCP_Settings.Instance.mobileControllerEnabled && (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android || EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)) {

                i = EditorUtility.DisplayDialogComplex("Mobile Controller.", "Your target platform is mobile, but it's not enabled in RCCP Settings yet.", "Enable it", "Ignore", "Ignore and don't warn me again");

                switch (i) {

                    case 0:
                        RCCP_Settings.Instance.mobileControllerEnabled = true;
                        break;

                    case 2:
                        EditorPrefs.SetBool("RCCP_IgnorePlatformWarnings", true);
                        break;

                }


            }

            if (RCCP_Settings.Instance.mobileControllerEnabled && (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android && EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)) {

                i = EditorUtility.DisplayDialogComplex("Mobile Controller.", "Your target platform is not mobile, but it's still enabled in RCCP Settings yet.", "Disable it", "Ignore", "Ignore and don't warn me again");

                switch (i) {

                    case 0:
                        RCCP_Settings.Instance.mobileControllerEnabled = false;
                        break;

                    case 2:
                        EditorPrefs.SetBool("RCCP_IgnorePlatformWarnings", true);
                        break;

                }

            }

        }

    }

}