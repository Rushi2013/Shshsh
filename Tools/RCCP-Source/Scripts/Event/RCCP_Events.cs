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
/// RCCP Events. You can listen any events below in your game.
/// </summary>
[Icon("Assets/Realistic Car Controller Pro/Resources/Editor Icons/RCCP_EditorIcon_Component.png")]
public class RCCP_Events {

    /// <summary>
    /// When a vehicle spawns.
    /// </summary>
    /// <param name="rccp"></param>
    public delegate void onRCCPSpawned(RCCP_CarController rccp);
    public static event onRCCPSpawned OnRCCPSpawned;

    /// <summary>
    /// When a vehicle destoyed or disabled.
    /// </summary>
    /// <param name="rccp"></param>
    public delegate void onRCCPDestroyed(RCCP_CarController rccp);
    public static event onRCCPDestroyed OnRCCPDestroyed;

    /// <summary>
    /// When a vehicle spawned with AI component.
    /// </summary>
    /// <param name="rccp"></param>
    public delegate void onRCCPAISpawned(RCCP_CarController rccp);
    public static event onRCCPAISpawned OnRCCPAISpawned;

    /// <summary>
    /// When a vehicle destroyed or disabled with AI component.
    /// </summary>
    /// <param name="rccp"></param>
    public delegate void onRCCPAIDestroyed(RCCP_CarController rccp);
    public static event onRCCPAIDestroyed OnRCCPAIDestroyed;

    /// <summary>
    /// When a vehicle collides.
    /// </summary>
    /// <param name="rccp"></param>
    /// <param name="collision"></param>
    public delegate void onRCCPCollision(RCCP_CarController rccp, Collision collision);
    public static event onRCCPCollision OnRCCPCollision;

    /// <summary>
    /// When RCCP camera spawns.
    /// </summary>
    /// <param name="cam"></param>
    public delegate void onRCCPCameraSpawned(RCCP_Camera cam);
    public static event onRCCPCameraSpawned OnRCCPCameraSpawned;

    /// <summary>
    /// When RCCP UI spawns.
    /// </summary>
    /// <param name="UI"></param>
    public delegate void onRCCPUISpawned(RCCP_UIManager UI);
    public static event onRCCPUISpawned OnRCCPUISpawned;

    /// <summary>
    /// When RCCP UI destoyed or disabled.
    /// </summary>
    /// <param name="UI"></param>
    public delegate void onRCCPUIDestroyed(RCCP_UIManager UI);
    public static event onRCCPUIDestroyed OnRCCPUIDestroyed;

    public delegate void onRCCPUIInformer(string text);
    public static event onRCCPUIInformer OnRCCPUIInformer;

    /// <summary>
    /// Firing an event when main behavior changed.
    /// </summary>
    public delegate void onBehaviorChanged();
    public static event onBehaviorChanged OnBehaviorChanged;

    /// <summary>
    /// Firing an event when player vehicle changed.
    /// </summary>
    public delegate void onVehicleChanged();
    public static event onVehicleChanged OnVehicleChanged;

    /// <summary>
    /// Firing an event when player vehicle changed.
    /// </summary>
    public delegate void onVehicleChangedToVehicle(RCCP_CarController carController);
    public static event onVehicleChangedToVehicle OnVehicleChangedToVehicle;

    /// <summary>
    /// Calls this event when RCCP vehicle spawned as player vehicle.
    /// </summary>
    /// <param name="rccp"></param>
    public static void Event_OnRCCPSpawned(RCCP_CarController rccp) {

        if (OnRCCPSpawned != null)
            OnRCCPSpawned(rccp);

    }
    /// <summary>
    /// Calls this event when RCCP vehicle destroyed as player vehicle.
    /// </summary>
    /// <param name="rccp"></param>
    public static void Event_OnRCCPDestroyed(RCCP_CarController rccp) {

        if (OnRCCPDestroyed != null)
            OnRCCPDestroyed(rccp);

    }

    /// <summary>
    /// Calls this event when RCCP camera spawned.
    /// </summary>
    /// <param name="cam"></param>
    public static void Event_OnRCCPCameraSpawned(RCCP_Camera cam) {

        if (OnRCCPCameraSpawned != null)
            OnRCCPCameraSpawned(cam);

    }

    /// <summary>
    /// Calls this event on RCCP vehicle colliisions.
    /// </summary>
    /// <param name="rccp"></param>
    /// <param name="collision"></param>
    public static void Event_OnRCCPCollision(RCCP_CarController rccp, Collision collision) {

        if (OnRCCPCollision != null)
            OnRCCPCollision(rccp, collision);

    }

    /// <summary>
    /// Calls this event when RCCP UI Canvas spawned.
    /// </summary>
    /// <param name="UI"></param>
    public static void Event_OnRCCPUISpawned(RCCP_UIManager UI) {

        if (OnRCCPUISpawned != null)
            OnRCCPUISpawned(UI);

    }

    /// <summary>
    /// Calls this event when RCCP UI Canvas destroyed.
    /// </summary>
    /// <param name="UI"></param>
    public static void Event_OnRCCPUIDestroyed(RCCP_UIManager UI) {

        if (OnRCCPUIDestroyed != null)
            OnRCCPUIDestroyed(UI);

    }

    /// <summary>
    /// Calls this event when RCCP AI vehicle spawned.
    /// </summary>
    /// <param name="AI"></param>
    public static void Event_OnRCCPAISpawned(RCCP_CarController AI) {

        if (OnRCCPAISpawned != null)
            OnRCCPAISpawned(AI);

    }

    /// <summary>
    /// Calls this event when RCCP AI vehicle destroyed.
    /// </summary>
    /// <param name="AI"></param>
    public static void Event_OnRCCPAIDestroyed(RCCP_CarController AI) {

        if (OnRCCPAIDestroyed != null)
            OnRCCPAIDestroyed(AI);

    }

    /// <summary>
    /// Calls this event when RCCP UI Informed has been used.
    /// </summary>
    /// <param name="info"></param>
    public static void Event_OnRCCPUIInformer(string info) {

        if (OnRCCPUIInformer != null)
            OnRCCPUIInformer(info);

    }

    /// <summary>
    /// Calls this event when behavior changed.
    /// </summary>
    public static void Event_OnBehaviorChanged() {

        if (OnBehaviorChanged != null)
            OnBehaviorChanged();

    }

    /// <summary>
    /// Calls this event when RCCP player vehicle changed.
    /// </summary>
    public static void Event_OnVehicleChanged() {

        if (OnVehicleChanged != null)
            OnVehicleChanged();

    }

    /// <summary>
    /// Calls this event when RCCP player vehicle changed.
    /// </summary>
    public static void Event_OnVehicleChangedToVehicle(RCCP_CarController carController) {

        if (OnVehicleChangedToVehicle != null)
            OnVehicleChangedToVehicle(carController);

    }

}
