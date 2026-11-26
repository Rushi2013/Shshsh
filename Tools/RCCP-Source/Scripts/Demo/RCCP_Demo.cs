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
using UnityEngine.SceneManagement;

#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

/// <summary>
/// A simple manager script for all demo scenes. It has an array of spawnable player vehicles, public methods, setting new behavior modes, restart, and quit application.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/RCCP Demo Manager")]
public class RCCP_Demo : RCCP_GenericComponent {

    /// <summary>
    /// An integer index value used for spawning a new vehicle.
    /// </summary>
    [Min(0)] public int selectedVehicleIndex = 0;

    /// <summary>
    /// Camera mode.
    /// </summary>
    public RCCP_Camera.CameraMode cameraMode = RCCP_Camera.CameraMode.TPS;

    /// <summary>
    /// An integer index value used for spawning a new vehicle.
    /// </summary>
    /// <param name="index"></param>
    public void SelectVehicle(int index) {

        selectedVehicleIndex = index;

    }

    /// <summary>
    /// Spawns the player vehicle.
    /// </summary>
    public void Spawn() {

        if (RCCP_SceneManager.Instance.activePlayerCamera)
            RCCP_SceneManager.Instance.activePlayerCamera.cameraMode = cameraMode;

        // Last known position and rotation of last active vehicle.
        Vector3 lastKnownPos = new Vector3();
        Quaternion lastKnownRot = new Quaternion();

        RCCP_CarController currentVehicle = RCCP_SceneManager.Instance.activePlayerVehicle;

        // Checking if there is a player vehicle on the scene.
        if (currentVehicle) {

            lastKnownPos = currentVehicle.transform.position;
            lastKnownRot = currentVehicle.transform.rotation;

        }

        RCCP_Camera currentCamera = RCCP_SceneManager.Instance.activePlayerCamera;

        // If last known position and rotation is not assigned, camera's position and rotation will be used.
        if (lastKnownPos == Vector3.zero) {

            if (currentCamera) {

                lastKnownPos = currentCamera.transform.position;
                lastKnownRot = currentCamera.transform.rotation;

            }

        }

        // We don't need X and Z rotation angle. Just Y.
        lastKnownRot.x = 0f;
        lastKnownRot.z = 0f;

#if BCG_ENTEREXIT

        if (currentVehicle) {

            BCG_EnterExitVehicle enterExitVehicle = currentVehicle.GetComponent<BCG_EnterExitVehicle>();

            if (enterExitVehicle) {

                CheckBCGEnterExitVehicles();

            } else {

                // If we have controllable vehicle by player on scene, destroy it.
                Destroy(currentVehicle.gameObject);

                // Here we are creating our new vehicle.
                RCCP_CarController spawnedVehicle = RCCP.SpawnRCC(RCCP_DemoVehicles.Instance.vehicles[selectedVehicleIndex], lastKnownPos, lastKnownRot, true, true, true);

            }

        } else {

            // Here we are creating our new vehicle.
            RCCP_CarController spawnedVehicle = RCCP.SpawnRCC(RCCP_DemoVehicles.Instance.vehicles[selectedVehicleIndex], lastKnownPos, lastKnownRot, true, true, true);

        }

#else

        // If we have controllable vehicle by player on scene, destroy it.
        if (currentVehicle)
            Destroy(currentVehicle.gameObject);

        // Here we are creating our new vehicle.
        RCCP_CarController spawnedVehicle = RCCP.SpawnRCC(RCCP_DemoVehicles.Instance.vehicles[selectedVehicleIndex], lastKnownPos, lastKnownRot, true, true, true);

#endif

    }

#if PHOTON_UNITY_NETWORKING && RCCP_PHOTON
    /// <summary>
    /// Spawns the player vehicle.
    /// </summary>
    public void SpawnPhoton() {

        if (RCCP_SceneManager.Instance.activePlayerCamera)
            RCCP_SceneManager.Instance.activePlayerCamera.cameraMode = cameraMode;

        // Last known position and rotation of last active vehicle.
        Vector3 lastKnownPos = new Vector3();
        Quaternion lastKnownRot = new Quaternion();

        RCCP_CarController currentVehicle = RCCP_SceneManager.Instance.activePlayerVehicle;

        // Checking if there is a player vehicle on the scene.
        if (currentVehicle) {

            lastKnownPos = currentVehicle.transform.position;
            lastKnownRot = currentVehicle.transform.rotation;

        }

        RCCP_Camera currentCamera = RCCP_SceneManager.Instance.activePlayerCamera;

        // If last known position and rotation is not assigned, camera's position and rotation will be used.
        if (lastKnownPos == Vector3.zero) {

            if (currentCamera) {

                lastKnownPos = currentCamera.transform.position;
                lastKnownRot = currentCamera.transform.rotation;

            }

        }

        // We don't need X and Z rotation angle. Just Y.
        lastKnownRot.x = 0f;
        lastKnownRot.z = 0f;

        // Is there any last vehicle?
        RCCP_CarController lastVehicle = RCCP_SceneManager.Instance.activePlayerVehicle;

#if BCG_ENTEREXIT

        BCG_EnterExitVehicle lastEnterExitVehicle = null;

        if (lastVehicle) {

            lastEnterExitVehicle = lastVehicle.GetComponentInChildren<BCG_EnterExitVehicle>();

            if (lastEnterExitVehicle && lastEnterExitVehicle.driver) {

                BCG_EnterExitManager.Instance.waitTime = 10f;
                lastEnterExitVehicle.driver.GetOut();

            }

        }

#endif

        // If we have controllable vehicle by player on scene, destroy it.
        if (lastVehicle)
            PhotonNetwork.Destroy(lastVehicle.gameObject);

        // Here we are creating our new vehicle.
#if RCCP_DEMO
        RCCP_CarController spawnedVehicle = PhotonNetwork.Instantiate(RCCP_DemoVehicles_Photon.Instance.vehicles[selectedVehicleIndex].transform.name, lastKnownPos, lastKnownRot).GetComponent<RCCP_CarController>();
        RCCP.RegisterPlayerVehicle(spawnedVehicle, true, true);
#else
        RCCP_CarController spawnedVehicle = PhotonNetwork.Instantiate(RCCP_Prototype_Photon.Instance.vehicles[selectedVehicleIndex].transform.name, lastKnownPos, lastKnownRot).GetComponent<RCCP_CarController>();
        RCCP.RegisterPlayerVehicle(spawnedVehicle, true, true);
#endif

#if BCG_ENTEREXIT

        if (lastEnterExitVehicle) {

            lastEnterExitVehicle = null;
            lastEnterExitVehicle = RCCP_SceneManager.Instance.activePlayerVehicle.GetComponentInChildren<BCG_EnterExitVehicle>();

            if (!lastEnterExitVehicle)
                lastEnterExitVehicle = RCCP_SceneManager.Instance.activePlayerVehicle.gameObject.AddComponent<BCG_EnterExitVehicle>();

            if (BCG_EnterExitManager.Instance.activePlayer && lastEnterExitVehicle.driver == null) {

                BCG_EnterExitManager.Instance.waitTime = 10f;
                BCG_EnterExitManager.Instance.activePlayer.GetIn(lastEnterExitVehicle);

            }

        }

#endif

    }
#endif

    /// <summary>
    /// Simply restarting the current scene.
    /// </summary>
    public void RestartScene() {

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    }

    /// <summary>
    /// Simply quit application. Not working on Editor.
    /// </summary>
    public void Quit() {

        Application.Quit();

    }

#if BCG_ENTEREXIT
    private void CheckBCGEnterExitVehicles() {

        // Last known position and rotation of last active vehicle.
        Vector3 lastKnownPos = new Vector3();
        Quaternion lastKnownRot = new Quaternion();

        RCCP_CarController currentVehicle = RCCP_SceneManager.Instance.activePlayerVehicle;

        // Checking if there is a player vehicle on the scene.
        lastKnownPos = currentVehicle.transform.position;
        lastKnownRot = currentVehicle.transform.rotation;

        BCG_EnterExitVehicle lastEnterExitVehicle = null;
        lastEnterExitVehicle = currentVehicle.GetComponentInChildren<BCG_EnterExitVehicle>();

        if (lastEnterExitVehicle) {

            if (lastEnterExitVehicle.driver) {

                BCG_EnterExitManager.Instance.waitTime = 10f;
                lastEnterExitVehicle.driver.GetOutImmediately();

            }

        }

        // If we have controllable vehicle by player on scene, destroy it.
        Destroy(currentVehicle.gameObject);

        // Here we are creating our new vehicle.
        RCCP_CarController spawnedVehicle = RCCP.SpawnRCC(RCCP_DemoVehicles.Instance.vehicles[selectedVehicleIndex], lastKnownPos, lastKnownRot, true, true, true);

        lastEnterExitVehicle = spawnedVehicle.GetComponentInChildren<BCG_EnterExitVehicle>();

        if (!lastEnterExitVehicle)
            lastEnterExitVehicle = spawnedVehicle.gameObject.AddComponent<BCG_EnterExitVehicle>();

        if (lastEnterExitVehicle) {

            if (BCG_EnterExitManager.Instance.activePlayer && lastEnterExitVehicle.driver == null) {

                BCG_EnterExitManager.Instance.waitTime = 10f;
                BCG_EnterExitManager.Instance.activePlayer.GetIn(lastEnterExitVehicle);

            }

        }

    }
#endif

}
