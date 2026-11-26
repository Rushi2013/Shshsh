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
using TMPro;

/// <summary>
/// UI informer panel with the text.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/RCCP UI Informer")]
public class RCCP_UI_Informer : RCCP_UIComponent {

    private static RCCP_UI_Informer instance;

    /// <summary>
    /// Informer as instance.
    /// </summary>
    public static RCCP_UI_Informer Instance {

        get {

            if (instance == null)
                instance = FindObjectOfType<RCCP_UI_Informer>();

            return instance;

        }

    }

    /// <summary>
    /// Informer text.
    /// </summary>
    public TMP_Text informerText;

    /// <summary>
    /// Canvas group.
    /// </summary>
    public CanvasGroup cGroup;

    /// <summary>
    /// Timer to deactive the canvas.
    /// </summary>
    [Min(0f)] public float timer = 3f;

    /// <summary>
    /// Timer.
    /// </summary>
    [Min(0f)] private float time = 0f;

    private void OnEnable() {

        RCCP_Events.OnRCCPUIInformer += RCCP_Events_OnRCCPUIInformer;

    }

    private void RCCP_Events_OnRCCPUIInformer(string text) {

        Display(text);

    }

    private void Update() {

        //  Timer.
        time -= Time.deltaTime;

        //  Limiting the timer.
        if (time < 0)
            time = 0f;

        //  If timer is 0, disable the canvas group.
        if (time <= 0 && cGroup.gameObject.activeSelf)
            cGroup.gameObject.SetActive(false);

    }

    /// <summary>
    /// Displaying the target string.
    /// </summary>
    /// <param name="textToDisplay"></param>
    public void Display(string textToDisplay) {

        //  If no informer text found, or no canvas group found, return.
        if (!informerText || !cGroup)
            return;

        time = timer;
        cGroup.gameObject.SetActive(true);
        informerText.gameObject.GetComponent<Animator>().Play(0);
        informerText.text = textToDisplay;

    }

    private void OnDisable() {

        RCCP_Events.OnRCCPUIInformer -= RCCP_Events_OnRCCPUIInformer;

    }

}
