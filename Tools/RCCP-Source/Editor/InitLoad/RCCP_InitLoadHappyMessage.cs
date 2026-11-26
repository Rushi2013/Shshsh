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

public class RCCP_InitLoadHappyMessage {

    [InitializeOnLoadMethod]
    public static void InitOnLoad() {

        if (SessionState.GetBool("BCG_HAPPYMESSAGE", false))
            return;

        EditorApplication.delayCall += () => {

            string[] messages = new string[4];

            messages[0] = "BoneCracker Games | Thank you for purchasing and using my assets, hope you're doing well!";
            messages[1] = "BoneCracker Games | Thank you for purchasing and using my assets, happy coding!";
            messages[2] = "BoneCracker Games | Thank you for purchasing and using my assets, hope your day will be perfect!";
            messages[3] = "BoneCracker Games | Thank you for purchasing and using my assets, wish you great success on your project!";

            string randomMessage = messages[UnityEngine.Random.Range(0, 4)];

            Debug.Log("<color=#00FF00>" + randomMessage + "</color>");
            SessionState.SetBool("BCG_HAPPYMESSAGE", true);

        };

    }

}
