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
using System.Threading.Tasks;

/// <summary>
/// Scene manager that contains current player vehicle, current player camera, current player UI, current player character, recording/playing mechanim, and other vehicles as well.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/RCCP Scene Manager")]
public class RCCP_SceneManager : RCCP_Singleton<RCCP_SceneManager> {

    /// <summary>
    /// Current active player vehicle.
    /// </summary>
    public RCCP_CarController activePlayerVehicle;

    /// <summary>
    /// Current active player camera as RCCP Camera.
    /// </summary>
    public RCCP_Camera activePlayerCamera;

    /// <summary>
    /// Current active UI canvas.
    /// </summary>
    public RCCP_UIManager activePlayerCanvas;

    /// <summary>
    /// Current active main camera.
    /// </summary>
    public Camera activeMainCamera;

    /// <summary>
    /// Last selected player vehicle.
    /// </summary>
    private RCCP_CarController lastActivePlayerVehicle;

    /// <summary>
    /// Registers the lastly spawned vehicle as player vehicle.
    /// </summary>
    public bool registerLastVehicleAsPlayer = true;

    /// <summary>
    /// Disables the UI when there is no any player vehicle.
    /// </summary>
    public bool disableUIWhenNoPlayerVehicle = false;

    /// <summary>
    /// Multithreading is supported on this platform?
    /// </summary>
    [HideInInspector] public bool mutlithreadingSupported = false;

    /// <summary>
    /// All vehicles on the scene.
    /// </summary>
    public List<RCCP_CarController> allVehicles = new List<RCCP_CarController>();

    /// <summary>
    /// All terrains on the scene.
    /// </summary>
    public Terrain[] allTerrains;

    public class Terrains {

        //	Terrain data.
        public Terrain terrain;
        public TerrainData mTerrainData;
        public PhysicMaterial terrainCollider;
        public int alphamapWidth;
        public int alphamapHeight;

        public float[,,] mSplatmapData;
        public float mNumTextures;

    }

    public Terrains[] terrains;     //  All collected terrains with custom class.
    [HideInInspector] public bool terrainsInitialized = false;        //  All terrains are initialized yet?

    private bool asyncAttempted = false;
    private bool asyncReceived = false;

    private void Awake() {

        //  Listening events.
        RCCP_Events.OnRCCPCameraSpawned += RCCP_Events_OnRCCPCameraSpawned;
        RCCP_Events.OnRCCPSpawned += RCCP_Events_OnRCCPSpawned;
        RCCP_Events.OnRCCPAISpawned += RCCP_Events_OnRCCPAISpawned;
        RCCP_Events.OnRCCPUISpawned += RCCP_Events_OnRCCPUISpawned;
        RCCP_Events.OnRCCPDestroyed += RCCP_Events_OnRCCPPlayerDestroyed;
        RCCP_Events.OnRCCPAIDestroyed += RCCP_Events_OnRCCPAIDestroyed;

        //  Instantiate telemetry UI if it's enabled in RCCP Settings.
        if (RCCPSettings.useTelemetry)
            Instantiate(RCCPSettings.RCCPTelemetry, Vector3.zero, Quaternion.identity);

        // Overriding Fixed TimeStep.
        if (RCCPSettings.overrideFixedTimeStep)
            Time.fixedDeltaTime = RCCPSettings.fixedTimeStep;

        // Overriding FPS.
        if (RCCPSettings.overrideFPS)
            Application.targetFrameRate = RCCPSettings.maxFPS;

        //  Rigidbody Auto Sync Transforms.
        if (RCCPSettings.overrideAutoSyncTransforms)
            Physics.autoSyncTransforms = RCCPSettings.autoSyncTransforms;

        if (RCCPSettings.autoSaveLoadInputRebind)
            RCCP_RebindSaveLoad.Load();

    }

    #region ONSPAWNED

    /// <summary>
    /// When RCCP vehicle is spawned.
    /// </summary>
    /// <param name="RCCP"></param>
    private void RCCP_Events_OnRCCPSpawned(RCCP_CarController RCCP) {

        //  If all vehicles list doesn't contain spawned vehicle, add it to the list.
        if (!allVehicles.Contains(RCCP))
            allVehicles.Add(RCCP);

        //  Registers the last spawned vehicle as player vehicle.
        if (registerLastVehicleAsPlayer)
            RegisterPlayer(RCCP);

    }

    private void RCCP_Events_OnRCCPAISpawned(RCCP_CarController AI) {

        //  If all vehicles list doesn't contain spawned vehicle, add it to the list.
        if (!allVehicles.Contains(AI))
            allVehicles.Add(AI);

    }

    /// <summary>
    /// When RCCP Camera spawned.
    /// </summary>
    /// <param name="BCGCamera"></param>
    private void RCCP_Events_OnRCCPCameraSpawned(RCCP_Camera cam) {

        activePlayerCamera = cam;

    }

    /// <summary>
    /// When RCCP Canvas spawned.
    /// </summary>
    /// <param name="UI"></param>
    private void RCCP_Events_OnRCCPUISpawned(RCCP_UIManager UI) {

        activePlayerCanvas = UI;

    }

    #endregion

    #region ONDESTROYED

    /// <summary>
    /// When a vehicle destroyed.
    /// </summary>
    /// <param name="RCCP"></param>
    private void RCCP_Events_OnRCCPPlayerDestroyed(RCCP_CarController RCCP) {

        if (allVehicles.Contains(RCCP))
            allVehicles.Remove(RCCP);

    }

    /// <summary>
    /// When an ai vehicle destroyed.
    /// </summary>
    /// <param name="RCCP"></param>
    private void RCCP_Events_OnRCCPAIDestroyed(RCCP_CarController AI) {

        if (allVehicles.Contains(AI))
            allVehicles.Remove(AI);

    }

    #endregion

    private void Start() {

        //  Getting all terrains.
        StartCoroutine(GetAllTerrains());

        //  Checking mutlithreading.
        StartCoroutine(CheckMT());

#if BCG_URP
        Invoke(nameof(CheckURPCamera), .5f);
#endif

    }

#if BCG_URP
    private void CheckURPCamera() {

        if (activeMainCamera != null) {

            UnityEngine.Rendering.Universal.UniversalAdditionalCameraData cameraData = activeMainCamera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();

            if (cameraData != null && cameraData.renderPostProcessing == false)
                cameraData.renderPostProcessing = true;

            if (cameraData == null)
                Debug.LogError("'UniversalAdditionalCameraData' component couldn't found on the RCCP_Camera! Please select the 'actual camera' of the RCCP_Camera in your editor, it will add the missing component to the camera. Otherwise you can't see the lensflares along with post processing effects.");

        }

    }
#endif

    private IEnumerator CheckMT() {

        if (!RCCP_Settings.Instance.multithreading) {

            asyncAttempted = false;
            asyncReceived = false;
            mutlithreadingSupported = false;
            yield break;

        }

        asyncAttempted = false;
        asyncReceived = false;

        CheckingMT();

        float timer = 1f;

        while (timer > 0) {

            timer -= Time.deltaTime;
            yield return null;

        }

        if (asyncAttempted && asyncReceived)
            mutlithreadingSupported = true;
        else
            mutlithreadingSupported = false;

        if (!mutlithreadingSupported)
            Debug.LogWarning("Multithreading is disabled on this platform, async can't be used with it. Regular methods will be used.");

        yield return null;

    }

    private async void CheckingMT() {

        asyncAttempted = true;
        asyncReceived = false;

        await Task.Run(() => { });

        asyncReceived = true;

    }

    /// <summary>
    /// Getting all terrains.
    /// </summary>
    /// <returns></returns>
    public IEnumerator GetAllTerrains() {

        yield return new WaitForFixedUpdate();
        allTerrains = Terrain.activeTerrains;
        yield return new WaitForFixedUpdate();

        //  If terrains found...
        if (allTerrains != null && allTerrains.Length >= 1) {

            terrains = new Terrains[allTerrains.Length];

            for (int i = 0; i < allTerrains.Length; i++) {

                if (allTerrains[i].terrainData == null) {

                    Debug.LogError("Terrain data of the " + allTerrains[i].transform.name + " is missing! Check the terrain data...");
                    yield return null;

                }

            }

            //  Initializing terrains.
            for (int i = 0; i < terrains.Length; i++) {

                terrains[i] = new Terrains();
                terrains[i].terrain = allTerrains[i];
                terrains[i].mTerrainData = allTerrains[i].terrainData;
                terrains[i].terrainCollider = allTerrains[i].GetComponent<TerrainCollider>().sharedMaterial;
                terrains[i].alphamapWidth = allTerrains[i].terrainData.alphamapWidth;
                terrains[i].alphamapHeight = allTerrains[i].terrainData.alphamapHeight;

                terrains[i].mSplatmapData = allTerrains[i].terrainData.GetAlphamaps(0, 0, terrains[i].alphamapWidth, terrains[i].alphamapHeight);
                terrains[i].mNumTextures = terrains[i].mSplatmapData.Length / (terrains[i].alphamapWidth * terrains[i].alphamapHeight);

            }

            terrainsInitialized = true;

        }

    }

    private void Update() {

        //  When player vehicle changed...
        if (activePlayerVehicle) {

            if (activePlayerVehicle != lastActivePlayerVehicle)
                RCCP_Events.Event_OnVehicleChanged();

            if (activePlayerVehicle != lastActivePlayerVehicle)
                RCCP_Events.Event_OnVehicleChangedToVehicle(activePlayerVehicle);

            lastActivePlayerVehicle = activePlayerVehicle;

        }

        //  Checking UI canvas.
        if (disableUIWhenNoPlayerVehicle && activePlayerCanvas)
            CheckCanvas();

        //  Getting main camera.
        if (Camera.main != null)
            activeMainCamera = Camera.main;

    }

    /// <summary>
    /// Registers the target vehicle as player vehicle.
    /// </summary>
    /// <param name="playerVehicle"></param>
    public void RegisterPlayer(RCCP_CarController playerVehicle) {

        activePlayerVehicle = playerVehicle;

        if (activePlayerCamera)
            activePlayerCamera.SetTarget(activePlayerVehicle);

    }

    /// <summary>
    /// Registers the target vehicle as player vehicle. Also sets controllable state of the vehicle.
    /// </summary>
    /// <param name="playerVehicle"></param>
    /// <param name="isControllable"></param>
    public void RegisterPlayer(RCCP_CarController playerVehicle, bool isControllable) {

        activePlayerVehicle = playerVehicle;
        activePlayerVehicle.SetCanControl(isControllable);

        if (activePlayerCamera)
            activePlayerCamera.SetTarget(activePlayerVehicle);

    }

    /// <summary>
    /// Registers the target vehicle as player vehicle. Also sets controllable state and engine state of the vehicle.
    /// </summary>
    /// <param name="playerVehicle"></param>
    /// <param name="isControllable"></param>
    /// <param name="engineState"></param>
    public void RegisterPlayer(RCCP_CarController playerVehicle, bool isControllable, bool engineState) {

        activePlayerVehicle = playerVehicle;
        activePlayerVehicle.SetCanControl(isControllable);
        activePlayerVehicle.SetEngine(engineState);

        if (activePlayerCamera)
            activePlayerCamera.SetTarget(activePlayerVehicle);

    }

    /// <summary>
    /// Deregisters the player vehicle.
    /// </summary>
    public void DeRegisterPlayer() {

        if (activePlayerVehicle)
            activePlayerVehicle.SetCanControl(false);

        activePlayerVehicle = null;

        if (activePlayerCamera)
            activePlayerCamera.RemoveTarget();

    }

    /// <summary>
    /// Checks UI canvas.
    /// </summary>
    public void CheckCanvas() {

        //if (!activePlayerVehicle || !activePlayerVehicle.canControl || !activePlayerVehicle.gameObject.activeInHierarchy || !activePlayerVehicle.enabled) {

        //    activePlayerCanvas.SetDisplayType(RCC_UIDashboardDisplay.DisplayType.Off);

        //    return;

        //}

        //if (activePlayerCanvas.displayType != RCC_UIDashboardDisplay.DisplayType.Customization)
        //    activePlayerCanvas.displayType = RCC_UIDashboardDisplay.DisplayType.Full;

    }

    ///<summary>
    /// Sets new behavior.
    ///</summary>
    public static void SetBehavior(int behaviorIndex) {

        RCCP_Settings.Instance.overrideBehavior = true;
        RCCP_Settings.Instance.behaviorSelectedIndex = behaviorIndex;

        RCCP_Events.Event_OnBehaviorChanged();

    }

    /// <summary>
    /// Changes current camera mode.
    /// </summary>
    public void ChangeCamera() {

        if (activePlayerCamera)
            activePlayerCamera.ChangeCamera();

    }

    /// <summary>
    /// Transport player vehicle the specified position and rotation.
    /// </summary>
    /// <param name="position">Position.</param>
    /// <param name="rotation">Rotation.</param>
    public void Transport(Vector3 position, Quaternion rotation) {

        if (activePlayerVehicle) {

            activePlayerVehicle.Rigid.velocity = Vector3.zero;
            activePlayerVehicle.Rigid.angularVelocity = Vector3.zero;

            activePlayerVehicle.transform.SetPositionAndRotation(position, rotation);

            for (int i = 0; i < activePlayerVehicle.AllWheelColliders.Length; i++)
                activePlayerVehicle.AllWheelColliders[i].WheelCollider.motorTorque = 0f;

        }

    }

    /// <summary>
    /// Transport target vehicle the specified position and rotation.
    /// </summary>
    /// <param name="vehicle"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    public void Transport(RCCP_CarController vehicle, Vector3 position, Quaternion rotation) {

        if (vehicle) {

            vehicle.Rigid.velocity = Vector3.zero;
            vehicle.Rigid.angularVelocity = Vector3.zero;

            vehicle.transform.SetPositionAndRotation(position, rotation);

            for (int i = 0; i < vehicle.AllWheelColliders.Length; i++)
                vehicle.AllWheelColliders[i].WheelCollider.motorTorque = 0f;

        }

    }

    public void Transport(RCCP_CarController vehicle, Vector3 position, Quaternion rotation, bool resetVelocity) {

        if (vehicle) {

            if (resetVelocity) {

                vehicle.Rigid.velocity = Vector3.zero;
                vehicle.Rigid.angularVelocity = Vector3.zero;

                for (int i = 0; i < vehicle.AllWheelColliders.Length; i++)
                    vehicle.AllWheelColliders[i].WheelCollider.motorTorque = 0f;

            }

            vehicle.transform.SetPositionAndRotation(position, rotation);

        }

    }

    private void OnDisable() {

        if (RCCPSettings.autoSaveLoadInputRebind)
            RCCP_RebindSaveLoad.Save();

    }

    private void OnDestroy() {

        RCCP_Events.OnRCCPCameraSpawned -= RCCP_Events_OnRCCPCameraSpawned;
        RCCP_Events.OnRCCPSpawned -= RCCP_Events_OnRCCPSpawned;
        RCCP_Events.OnRCCPAISpawned -= RCCP_Events_OnRCCPAISpawned;
        RCCP_Events.OnRCCPUISpawned -= RCCP_Events_OnRCCPUISpawned;
        RCCP_Events.OnRCCPDestroyed -= RCCP_Events_OnRCCPPlayerDestroyed;
        RCCP_Events.OnRCCPAIDestroyed -= RCCP_Events_OnRCCPAIDestroyed;

    }

}
