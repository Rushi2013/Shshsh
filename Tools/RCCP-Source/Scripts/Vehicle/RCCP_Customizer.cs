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
/// Customization applier for vehicles.
/// 6 Upgrade managers for paints, wheels, upgrades, spoilers, customization, and sirens.
/// </summary>
[DefaultExecutionOrder(10)]
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Addons/RCCP Customizer")]
public class RCCP_Customizer : RCCP_Component {

    /// <summary>
    /// Save file name of the vehicle.
    /// </summary>
    public string saveFileName = "";

    /// <summary>
    /// Auto initializes all managers. Disable it for networked vehicles.
    /// </summary>
    public bool autoInitialize = true;

    /// <summary>
    /// Loads the latest loadout.
    /// </summary>
    public bool autoLoadLoadout = true;

    /// <summary>
    /// Auto save.
    /// </summary>
    public bool autoSave = true;

    /// <summary>
    /// Initialize all managers with this method.
    /// </summary>
    private enum InitializeMethod { Awake, OnEnable, Start, DelayedWithFixedUpdate }

    /// <summary>
    /// Initialize all managers with this method.
    /// </summary>
    [SerializeField] private InitializeMethod initializeMethod = InitializeMethod.Start;

    /// <summary>
    /// Loadout class.
    /// </summary>
    public RCCP_CustomizationLoadout loadout = new RCCP_CustomizationLoadout();

    #region All upgrade managers

    /// <summary>
    /// Paint manager.
    /// </summary>
    private RCCP_VehicleUpgrade_PaintManager _paintManager;
    public RCCP_VehicleUpgrade_PaintManager PaintManager {

        get {

            if (_paintManager == null)
                _paintManager = GetComponentInChildren<RCCP_VehicleUpgrade_PaintManager>(true);

            return _paintManager;

        }

    }

    /// <summary>
    /// Wheel Manager.
    /// </summary>
    private RCCP_VehicleUpgrade_WheelManager _wheelManager;
    public RCCP_VehicleUpgrade_WheelManager WheelManager {

        get {

            if (_wheelManager == null)
                _wheelManager = GetComponentInChildren<RCCP_VehicleUpgrade_WheelManager>(true);

            return _wheelManager;

        }

    }

    /// <summary>
    /// Upgrade Manager.
    /// </summary>
    private RCCP_VehicleUpgrade_UpgradeManager _upgradeManager;
    public RCCP_VehicleUpgrade_UpgradeManager UpgradeManager {

        get {

            if (_upgradeManager == null)
                _upgradeManager = GetComponentInChildren<RCCP_VehicleUpgrade_UpgradeManager>(true);

            return _upgradeManager;

        }

    }

    /// <summary>
    /// Spoiler Manager.
    /// </summary>
    private RCCP_VehicleUpgrade_SpoilerManager _spoilerManager;
    public RCCP_VehicleUpgrade_SpoilerManager SpoilerManager {

        get {

            if (_spoilerManager == null)
                _spoilerManager = GetComponentInChildren<RCCP_VehicleUpgrade_SpoilerManager>(true);

            return _spoilerManager;

        }

    }

    /// <summary>
    /// Siren Manager.
    /// </summary>
    private RCCP_VehicleUpgrade_SirenManager _sirenManager;
    public RCCP_VehicleUpgrade_SirenManager SirenManager {

        get {

            if (_sirenManager == null)
                _sirenManager = GetComponentInChildren<RCCP_VehicleUpgrade_SirenManager>(true);

            return _sirenManager;

        }

    }

    /// <summary>
    /// Customization Manager.
    /// </summary>
    private RCCP_VehicleUpgrade_CustomizationManager _customizationManager;
    public RCCP_VehicleUpgrade_CustomizationManager CustomizationManager {

        get {

            if (_customizationManager == null)
                _customizationManager = GetComponentInChildren<RCCP_VehicleUpgrade_CustomizationManager>(true);

            return _customizationManager;

        }

    }

    /// <summary>
    /// Decal Manager.
    /// </summary>
    private RCCP_VehicleUpgrade_DecalManager _decalManager;
    public RCCP_VehicleUpgrade_DecalManager DecalManager {

        get {

            if (_decalManager == null)
                _decalManager = GetComponentInChildren<RCCP_VehicleUpgrade_DecalManager>(true);

            return _decalManager;

        }

    }

    /// <summary>
    /// Neon Manager.
    /// </summary>
    private RCCP_VehicleUpgrade_NeonManager _neonManager;
    public RCCP_VehicleUpgrade_NeonManager NeonManager {

        get {

            if (_neonManager == null)
                _neonManager = GetComponentInChildren<RCCP_VehicleUpgrade_NeonManager>(true);

            return _neonManager;

        }

    }

    #endregion

    public override void Awake() {

        base.Awake();

        //  Loads the latest loadout.
        if (autoLoadLoadout)
            Load();

        //  Initializes all managers.
        if (initializeMethod == InitializeMethod.Awake)
            Initialize();

    }

    public override void OnEnable() {

        base.OnEnable();

        //  Initializes all managers.
        if (initializeMethod == InitializeMethod.OnEnable)
            Initialize();

    }

    public override void Start() {

        base.Start();

        //  Initializes all managers.
        if (initializeMethod == InitializeMethod.Start)
            Initialize();

        //  Initializes all managers.
        if (initializeMethod == InitializeMethod.DelayedWithFixedUpdate)
            StartCoroutine(Delayed());

    }

    /// <summary>
    /// Initializing all managers delayed.
    /// </summary>
    /// <returns></returns>
    private IEnumerator Delayed() {

        yield return new WaitForFixedUpdate();
        Initialize();

    }

    /// <summary>
    /// Initialize all managers.
    /// </summary>
    public void Initialize() {

        if (loadout == null)
            loadout = new RCCP_CustomizationLoadout();

        //  Initializes paint manager.
        if (PaintManager)
            PaintManager.Initialize();

        //  Initializes wheel manager.
        if (WheelManager)
            WheelManager.Initialize();

        //  Initializes upgrade manager.
        if (UpgradeManager)
            UpgradeManager.Initialize();

        //  Initializes spoiler manager.
        if (SpoilerManager)
            SpoilerManager.Initialize();

        //  Initializes siren manager.
        if (SirenManager)
            SirenManager.Initialize();

        //  Initializes customization manager.
        if (CustomizationManager)
            CustomizationManager.Initialize();

        //  Initializes decal manager.
        if (DecalManager)
            DecalManager.Initialize();

        //  Initializes neon manager.
        if (NeonManager)
            NeonManager.Initialize();

    }

    /// <summary>
    /// Get loadout.
    /// </summary>
    /// <returns></returns>
    public RCCP_CustomizationLoadout GetLoadout() {

        if (loadout != null) {

            return loadout;

        } else {

            loadout = new RCCP_CustomizationLoadout();
            return loadout;

        }

    }

    /// <summary>
    /// Saves the current loadout with Json.
    /// </summary>
    public void Save() {

        if (loadout == null)
            loadout = new RCCP_CustomizationLoadout();

        PlayerPrefs.SetString(saveFileName, JsonUtility.ToJson(loadout));

    }

    /// <summary>
    /// Loads the latest saved loadout with Json.
    /// </summary>
    public void Load() {

        if (PlayerPrefs.HasKey(saveFileName))
            loadout = (RCCP_CustomizationLoadout)JsonUtility.FromJson(PlayerPrefs.GetString(saveFileName), typeof(RCCP_CustomizationLoadout));

    }

    /// <summary>
    /// Deletes the latest saved loadout and restores the vehicle setup.
    /// </summary>
    public void Delete() {

        if (PlayerPrefs.HasKey(saveFileName))
            PlayerPrefs.DeleteKey(PlayerPrefs.GetString(saveFileName));

        loadout = new RCCP_CustomizationLoadout();

        //  Restores paint manager.
        if (PaintManager)
            PaintManager.Restore();

        //  Restores wheel manager.
        if (WheelManager)
            WheelManager.Restore();

        //  Restores upgrade manager.
        if (UpgradeManager)
            UpgradeManager.Restore();

        //  Restores spoiler manager.
        if (SpoilerManager)
            SpoilerManager.Restore();

        //  Restores siren manager.
        if (SirenManager)
            SirenManager.Restore();

        //  Restores customization manager.
        if (CustomizationManager)
            CustomizationManager.Restore();

        //  Restores decal manager.
        if (DecalManager)
            DecalManager.Restore();

        //  Restores neon manager.
        if (NeonManager)
            NeonManager.Restore();

    }

    /// <summary>
    /// Hides all visual upgraders.
    /// </summary>
    public void HideAll() {

        //  Restores spoiler manager.
        if (SpoilerManager)
            SpoilerManager.DisableAll();

        //  Restores siren manager.
        if (SirenManager)
            SirenManager.DisableAll();

        //  Restores decal manager.
        if (DecalManager)
            DecalManager.DisableAll();

        //  Restores neon manager.
        if (NeonManager)
            NeonManager.DisableAll();

    }

    /// <summary>
    /// Shows all visual upgrades.
    /// </summary>
    public void ShowAll() {

        //  Restores spoiler manager.
        if (SpoilerManager)
            SpoilerManager.EnableAll();

        //  Restores siren manager.
        if (SirenManager)
            SirenManager.EnableAll();

        //  Restores decal manager.
        if (DecalManager)
            DecalManager.EnableAll();

        //  Restores neon manager.
        if (NeonManager)
            NeonManager.EnableAll();

    }

    public void Reload() {

        //

    }

    private void Reset() {

        saveFileName = GetComponentInParent<RCCP_CarController>(true).transform.name;

    }

}
