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
using UnityEditor.Events;
using UnityEngine.Events;

[CustomEditor(typeof(RCCP_MainComponent), true)]
public class RCCP_CarControllerEditor : Editor {

    RCCP_CarController prop;
    GUISkin skin;
    Color guiColor;
    List<string> errorMessages = new List<string>();

    static bool addAllComponents;
    static bool statsEnabled;

    RCCP_Engine engine;
    RCCP_Clutch clutch;
    RCCP_Gearbox gearbox;
    RCCP_Differential differential;
    RCCP_Axles axles;
    RCCP_Input inputs;
    RCCP_AeroDynamics aero;
    RCCP_Audio audio;
    RCCP_Lights lights;
    RCCP_Stability stability;
    RCCP_Damage damage;
    RCCP_Particles particles;
    RCCP_Lod lod;
    RCCP_OtherAddons otherAddons;
    RCCP_Customizer customizer;

    private void OnEnable() {

        skin = Resources.Load<GUISkin>("RCCP_Gui");

        if (!EditorApplication.isPlaying)
            ReOrderComponents();

    }

    private void AddAllComponents() {

        addAllComponents = false;

        AddEngine();
        AddClutch();
        AddGearbox();
        AddAxles();
        AddDifferential();
        AddEngineToClutchListener();
        AddClutchToGearboxListener();
        AddGearboxToDifferentialListener();
        AddDifferentialToAxle();

    }

    private void AddEngineToClutchListener() {

        engine.outputEvent = new RCCP_Event_Output();

        var targetinfo = UnityEvent.GetValidMethodInfo(clutch,
"ReceiveOutput", new Type[] { typeof(RCCP_Output) });

        var methodDelegate = Delegate.CreateDelegate(typeof(UnityAction<RCCP_Output>), clutch, targetinfo) as UnityAction<RCCP_Output>;
        UnityEventTools.AddPersistentListener(engine.outputEvent, methodDelegate);

    }

    private void AddClutchToGearboxListener() {

        clutch.outputEvent = new RCCP_Event_Output();

        var targetinfo = UnityEvent.GetValidMethodInfo(gearbox,
"ReceiveOutput", new Type[] { typeof(RCCP_Output) });

        var methodDelegate = Delegate.CreateDelegate(typeof(UnityAction<RCCP_Output>), gearbox, targetinfo) as UnityAction<RCCP_Output>;
        UnityEventTools.AddPersistentListener(clutch.outputEvent, methodDelegate);

    }

    private void AddGearboxToDifferentialListener() {

        gearbox.outputEvent = new RCCP_Event_Output();

        var targetinfo = UnityEvent.GetValidMethodInfo(differential,
"ReceiveOutput", new Type[] { typeof(RCCP_Output) });

        var methodDelegate = Delegate.CreateDelegate(typeof(UnityAction<RCCP_Output>), differential, targetinfo) as UnityAction<RCCP_Output>;
        UnityEventTools.AddPersistentListener(gearbox.outputEvent, methodDelegate);

    }

    private void AddDifferentialToAxle() {

        if (!axles)
            return;

        float[] indexes = new float[axles.GetComponentsInChildren<RCCP_Axle>(true).Length];

        if (indexes.Length < 1)
            return;

        for (int i = 0; i < indexes.Length; i++)
            indexes[i] = axles.GetComponentsInChildren<RCCP_Axle>(true)[i].leftWheelCollider.transform.localPosition.z;

        int biggestIndex = 0;
        int lowestIndex = 0;

        for (int i = 0; i < indexes.Length; i++) {

            if (indexes[i] >= biggestIndex)
                biggestIndex = i;

            if (indexes[i] <= lowestIndex)
                lowestIndex = i;

        }

        RCCP_Axle rearAxle = axles.GetComponentsInChildren<RCCP_Axle>(true)[lowestIndex];

        if (rearAxle)
            differential.connectedAxle = rearAxle;

    }

    private void ReOrderComponents() {

        int index = 0;

        if (engine) {

            engine.transform.SetSiblingIndex(index);
            index++;

        }

        if (clutch) {

            clutch.transform.SetSiblingIndex(index);
            index++;

        }

        if (gearbox) {

            gearbox.transform.SetSiblingIndex(index);
            index++;

        }

        if (differential) {

            differential.transform.SetSiblingIndex(index);
            index++;

        }

        if (axles) {

            axles.transform.SetSiblingIndex(index);
            index++;

        }

        if (inputs) {

            inputs.transform.SetSiblingIndex(index);
            index++;

        }

        if (aero) {

            aero.transform.SetSiblingIndex(index);
            index++;

        }

        if (stability) {

            stability.transform.SetSiblingIndex(index);
            index++;

        }

        if (audio) {

            audio.transform.SetSiblingIndex(index);
            index++;

        }

        if (lights) {

            lights.transform.SetSiblingIndex(index);
            index++;

        }

        if (damage) {

            damage.transform.SetSiblingIndex(index);
            index++;

        }

        if (particles) {

            particles.transform.SetSiblingIndex(index);
            index++;

        }

        if (lod) {

            lod.transform.SetSiblingIndex(index);
            index++;

        }

        if (otherAddons) {

            otherAddons.transform.SetSiblingIndex(index);
            index++;

        }

    }

    private void GetAllComponents() {

        engine = prop.GetComponentInChildren<RCCP_Engine>(true);
        clutch = prop.GetComponentInChildren<RCCP_Clutch>(true);
        gearbox = prop.GetComponentInChildren<RCCP_Gearbox>(true);
        differential = prop.GetComponentInChildren<RCCP_Differential>(true);
        axles = prop.GetComponentInChildren<RCCP_Axles>(true);
        inputs = prop.GetComponentInChildren<RCCP_Input>(true);
        aero = prop.GetComponentInChildren<RCCP_AeroDynamics>(true);
        audio = prop.GetComponentInChildren<RCCP_Audio>(true);
        stability = prop.GetComponentInChildren<RCCP_Stability>(true);
        lights = prop.GetComponentInChildren<RCCP_Lights>(true);
        damage = prop.GetComponentInChildren<RCCP_Damage>(true);
        particles = prop.GetComponentInChildren<RCCP_Particles>(true);
        lod = prop.GetComponentInChildren<RCCP_Lod>(true);
        otherAddons = prop.GetComponentInChildren<RCCP_OtherAddons>(true);
        customizer = prop.GetComponentInChildren<RCCP_Customizer>(true);

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_CarController)target;
        serializedObject.Update();
        GUI.skin = skin;
        guiColor = GUI.color;

        GetAllComponents();

        RCCP_SceneManager sm = RCCP_SceneManager.Instance;

        if (addAllComponents)
            AddAllComponents();

        if (!EditorApplication.isPlaying)
            prop.checkComponents = false;

        CheckMissingAxleManager();

        if (EditorUtility.IsPersistent(prop))
            EditorGUILayout.HelpBox("Double click the prefab to edit settings. Some editor features are disabled in this mode.", MessageType.Warning);

        if (Screen.width < 500)
            EditorGUILayout.HelpBox("Increase width of your inspector panel to see all content.", MessageType.Warning);

        GUILayout.Label("<color=#FF9500>Drivetrain</color>");

        EditorGUILayout.BeginHorizontal();

        DrivetrainButtons();

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10f);

        GUILayout.Label("<color=#FF9500>Addons</color>");

        EditorGUILayout.BeginHorizontal();

        AddonButtons();

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10f);

        EditorGUILayout.BeginHorizontal();

        AddonButtons2();

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10f);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("ineffectiveBehavior"), new GUIContent("Ineffective Behavior", "Selected behavior in RCCP_Settings won't affect this vehicle if this option is enabled."));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("canControl"), new GUIContent("Can Control", "Is this vehicle controllable now? RCCP_Inputs attached to the vehicle will receive inputs when enabled."));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("externalControl"), new GUIContent("External Control", "Is this vehicle controlled by an external controller?"));

        EditorGUILayout.Space();

        statsEnabled = EditorGUILayout.BeginToggleGroup(new GUIContent("Runtime Stats", "Will be updated at runtime."), statsEnabled);

        if (statsEnabled) {

            if (!EditorApplication.isPlaying)
                EditorGUILayout.HelpBox("Stats will be updated at runtime", MessageType.Info);

            GUI.enabled = false;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("engineRPM"), new GUIContent("Engine RPM"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minEngineRPM"), new GUIContent("Minimum Engine RPM"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxEngineRPM"), new GUIContent("Maximum Engine RPM"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("currentGear"), new GUIContent("Current Gear"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("currentGearRatio"), new GUIContent("Current Gear Ratio"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lastGearRatio"), new GUIContent("Last Gear Ratio"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("differentialRatio"), new GUIContent("Differential Ratio"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("speed"), new GUIContent("Physically Speed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("wheelRPM2Speed"), new GUIContent("Wheel RPM 2 Speed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tractionWheelRPM2EngineRPM"), new GUIContent("Wheel RPM 2 Engine RPM"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetWheelSpeedForCurrentGear"), new GUIContent("Target Wheel Speed For Current Gear"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumSpeed"), new GUIContent("Maximum Speed"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("producedEngineTorque"), new GUIContent("Produced Engine Torque as NM"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("producedGearboxTorque"), new GUIContent("Produced Gearbox Torque as NM"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("producedDifferentialTorque"), new GUIContent("Produced Differential Torque as NM"));
            EditorGUILayout.Space();

            if (prop.PoweredAxles != null)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_poweredAxles"), new GUIContent("Power Axles"), true);

            if (prop.BrakedAxles != null)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_brakedAxles"), new GUIContent("Brake Axles"), true);

            if (prop.SteeredAxles != null)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_steeredAxles"), new GUIContent("Steer Axles"), true);

            if (prop.HandbrakedAxles != null)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_handbrakedAxles"), new GUIContent("Handbrake Axles"), true);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("direction"), new GUIContent("Direction"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("shiftingNow"), new GUIContent("Shifting Now"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("reversingNow"), new GUIContent("Reversing Now"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("steerAngle"), new GUIContent("Steer Angle"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("throttleInput_V"), new GUIContent("Vehicle Throttle Input"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("brakeInput_V"), new GUIContent("Vehicle Brake Input"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("steerInput_V"), new GUIContent("Vehicle Steer Input"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("handbrakeInput_V"), new GUIContent("Vehicle Handbrake Input"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("clutchInput_V"), new GUIContent("Vehicle clutch Input"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("nosInput_V"), new GUIContent("Nos Input"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lowBeamLights"), new GUIContent("Low Beam Lights"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("highBeamLights"), new GUIContent("High Beam Lights"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("indicatorsLeftLights"), new GUIContent("Indicator Lights L"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("indicatorsRightLights"), new GUIContent("Indicator Lights R"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("indicatorsAllLights"), new GUIContent("Indicator Lights All"));

            GUI.enabled = true;

        }

        EditorGUILayout.EndToggleGroup();

        if (GUI.changed && !EditorApplication.isPlaying) {

            if (RCCP_Settings.Instance.setLayers)
                SetLayers();

            EditorUtility.SetDirty(prop);

        }

        serializedObject.ApplyModifiedProperties();

        if (!EditorApplication.isPlaying)
            CheckSetup();

        if (!EditorApplication.isPlaying)
            Repaint();

    }

    private void SetLayers() {

        prop.transform.gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPLayer);

        var children = prop.transform.GetComponentsInChildren<Transform>(true);

        if (RCCP_Settings.Instance.RCCPLayer != "") {

            foreach (var child in children)
                child.gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPLayer);

        }

        if (RCCP_Settings.Instance.RCCPWheelColliderLayer != "") {

            foreach (RCCP_WheelCollider item in prop.gameObject.GetComponentsInChildren<RCCP_WheelCollider>(true))
                item.gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPWheelColliderLayer);

        }

        if (RCCP_Settings.Instance.RCCPDetachablePartLayer != "") {

            foreach (RCCP_DetachablePart item in prop.gameObject.GetComponentsInChildren<RCCP_DetachablePart>(true))
                item.gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPDetachablePartLayer);

        }

    }

    private void DrivetrainButtons() {

        EditorGUILayout.BeginHorizontal();

        EngineButton();
        GUILayout.Space(1f);
        ClutchButton();
        GUILayout.Space(1f);
        GearboxButton();
        GUILayout.Space(1f);
        DifferentialButton();
        GUILayout.Space(1f);
        AxlesButton();

        EditorGUILayout.EndHorizontal();

    }

    private void AddonButtons() {

        EditorGUILayout.BeginHorizontal();

        InputsButton();
        GUILayout.Space(1f);
        AeroButton();
        GUILayout.Space(1f);
        StabilityButton();
        GUILayout.Space(1f);
        AudioButton();
        GUILayout.Space(1f);
        CustomizerButton();

        EditorGUILayout.EndHorizontal();

    }

    private void AddonButtons2() {

        EditorGUILayout.BeginHorizontal();

        LightsButton();
        GUILayout.Space(1f);
        DamageButton();
        GUILayout.Space(1f);
        ParticlesButton();
        GUILayout.Space(1f);
        LODButton();
        GUILayout.Space(1f);
        OtherAddonsButton();

        EditorGUILayout.EndHorizontal();

    }

    private void EngineButton() {

        EditorGUILayout.BeginVertical(GUI.skin.window);

        if (engine)
            engine.enabled = EditorGUILayout.ToggleLeft("", engine.enabled, GUILayout.Width(15f));

        GUILayout.Label(("<color=#FF9500>Engine</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

        if (GUILayout.Button(Resources.Load("Editor Icons/Icon_Engine") as Texture, GUILayout.Width(70f), GUILayout.Height(50f), GUILayout.ExpandWidth(true))) {

            if (engine)
                Selection.activeGameObject = engine.gameObject;
            else
                AddEngine();

        }

        if (!EditorUtility.IsPersistent(prop)) {

            if (engine)
                GUILayout.Label(("<color=#FF9500>Equipped</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));
            else
                GUILayout.Label(("<color=#FF9500>Create</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

            if (engine) {

                bool completeSetup = engine.completeSetup;

                if (!completeSetup)
                    GUI.color = Color.red;

                if (engine.checkedSetup)
                    EditorGUILayout.LabelField(completeSetup ? "OK" : "Not OK", GUILayout.Width(50f));

                GUI.color = guiColor;

                if (GUILayout.Button("Check", GUILayout.Width(50f), GUILayout.ExpandWidth(true))) {

                    prop.checkComponents = true;
                    engine.checkedSetup = true;

                    if (engine)
                        Selection.activeGameObject = engine.gameObject;

                }

            }

            GUI.color = Color.red;

            if (engine) {

                if (GUILayout.Button("Remove", GUILayout.Width(50f), GUILayout.ExpandWidth(true)))
                    RemoveEngine();

            }

            GUI.color = guiColor;

        }

        EditorGUILayout.EndVertical();

    }

    private void ClutchButton() {

        EditorGUILayout.BeginVertical(GUI.skin.window);

        if (clutch)
            clutch.enabled = EditorGUILayout.ToggleLeft("", clutch.enabled, GUILayout.Width(15f));

        GUILayout.Label(("<color=#FF9500>Clutch</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

        if (GUILayout.Button(Resources.Load("Editor Icons/Icon_Clutch") as Texture, GUILayout.Width(70f), GUILayout.Height(50f), GUILayout.ExpandWidth(true))) {

            if (clutch)
                Selection.activeGameObject = clutch.gameObject;
            else
                AddClutch();

        }

        if (!EditorUtility.IsPersistent(prop)) {

            if (clutch)
                GUILayout.Label(("<color=#FF9500>Equipped</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));
            else
                GUILayout.Label(("<color=#FF9500>Create</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

            if (clutch) {

                bool completeSetup = clutch.completeSetup;

                if (!completeSetup)
                    GUI.color = Color.red;

                if (clutch.checkedSetup)
                    EditorGUILayout.LabelField(completeSetup ? "OK" : "Not OK", GUILayout.Width(50f));

                GUI.color = guiColor;

                if (GUILayout.Button("Check", GUILayout.Width(50f), GUILayout.ExpandWidth(true))) {

                    prop.checkComponents = true;
                    clutch.checkedSetup = true;

                    if (clutch)
                        Selection.activeGameObject = clutch.gameObject;

                }

            }

            GUI.color = Color.red;

            if (clutch) {

                if (GUILayout.Button("Remove", GUILayout.Width(50f), GUILayout.ExpandWidth(true)))
                    RemoveClutch();

            }

            GUI.color = guiColor;

        }

        EditorGUILayout.EndVertical();

    }

    private void GearboxButton() {

        EditorGUILayout.BeginVertical(GUI.skin.window);

        if (gearbox)
            gearbox.enabled = EditorGUILayout.ToggleLeft("", gearbox.enabled, GUILayout.Width(15f));

        GUILayout.Label(("<color=#FF9500>Gearbox</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

        if (GUILayout.Button(Resources.Load("Editor Icons/Icon_Gearbox") as Texture, GUILayout.Width(70f), GUILayout.Height(50f), GUILayout.ExpandWidth(true))) {

            if (gearbox)
                Selection.activeGameObject = gearbox.gameObject;
            else
                AddGearbox();

        }

        if (!EditorUtility.IsPersistent(prop)) {

            if (gearbox)
                GUILayout.Label(("<color=#FF9500>Equipped</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));
            else
                GUILayout.Label(("<color=#FF9500>Create</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

            if (gearbox) {

                bool completeSetup = gearbox.completeSetup;

                if (!completeSetup)
                    GUI.color = Color.red;

                if (gearbox.checkedSetup)
                    EditorGUILayout.LabelField(completeSetup ? "OK" : "Not OK", GUILayout.Width(50f));

                GUI.color = guiColor;

                if (GUILayout.Button("Check", GUILayout.Width(50f), GUILayout.ExpandWidth(true))) {

                    prop.checkComponents = true;
                    gearbox.checkedSetup = true;

                    if (gearbox)
                        Selection.activeGameObject = gearbox.gameObject;

                }

            }

            GUI.color = Color.red;

            if (gearbox) {

                if (GUILayout.Button("Remove", GUILayout.Width(50f), GUILayout.ExpandWidth(true)))
                    RemoveGearbox();

            }

            GUI.color = guiColor;

        }

        EditorGUILayout.EndVertical();

    }

    private void DifferentialButton() {

        EditorGUILayout.BeginVertical(GUI.skin.window);

        if (differential)
            differential.enabled = EditorGUILayout.ToggleLeft("", differential.enabled, GUILayout.Width(15f));

        GUILayout.Label(("<color=#FF9500>Differential</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

        if (GUILayout.Button(Resources.Load("Editor Icons/Icon_Differential") as Texture, GUILayout.Width(70f), GUILayout.Height(50f), GUILayout.ExpandWidth(true))) {

            if (differential)
                Selection.activeGameObject = differential.gameObject;
            else
                AddDifferential();

        }

        if (!EditorUtility.IsPersistent(prop)) {

            if (differential)
                GUILayout.Label(("<color=#FF9500>Equipped</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));
            else
                GUILayout.Label(("<color=#FF9500>Create</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

            if (differential) {

                bool completeSetup = differential.completeSetup;

                if (!completeSetup)
                    GUI.color = Color.red;

                if (differential.checkedSetup)
                    EditorGUILayout.LabelField(completeSetup ? "OK" : "Not OK", GUILayout.Width(50f));

                GUI.color = guiColor;

                if (GUILayout.Button("Check", GUILayout.Width(50f), GUILayout.ExpandWidth(true))) {

                    prop.checkComponents = true;
                    differential.checkedSetup = true;

                    if (differential)
                        Selection.activeGameObject = differential.gameObject;

                }

            }

            GUI.color = Color.red;

            if (differential) {

                if (GUILayout.Button("Remove", GUILayout.Width(50f), GUILayout.ExpandWidth(true)))
                    RemoveDifferential();

            }

            GUI.color = guiColor;

        }

        EditorGUILayout.EndVertical();

    }

    private void AxlesButton() {

        EditorGUILayout.BeginVertical(GUI.skin.window);

        if (axles)
            axles.enabled = EditorGUILayout.ToggleLeft("", axles.enabled, GUILayout.Width(15f));

        GUILayout.Label(("<color=#FF9500>Axles</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

        if (GUILayout.Button(Resources.Load("Editor Icons/Icon_Axle") as Texture, GUILayout.Width(70f), GUILayout.Height(50f), GUILayout.ExpandWidth(true))) {

            if (axles)
                Selection.activeGameObject = axles.gameObject;
            else
                AddAxles();

        }

        if (!EditorUtility.IsPersistent(prop)) {

            if (axles)
                GUILayout.Label(("<color=#FF9500>Equipped</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));
            else
                GUILayout.Label(("<color=#FF9500>Create</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

            if (axles) {

                bool completeSetup = axles.completeSetup;

                if (!completeSetup)
                    GUI.color = Color.red;

                if (axles.checkedSetup)
                    EditorGUILayout.LabelField(completeSetup ? "OK" : "Not OK", GUILayout.Width(50f));

                GUI.color = guiColor;

                if (GUILayout.Button("Check", GUILayout.Width(50f), GUILayout.ExpandWidth(true))) {

                    prop.checkComponents = true;
                    axles.checkedSetup = true;

                    if (axles)
                        Selection.activeGameObject = axles.gameObject;

                }

            }

            GUI.color = Color.red;

            if (axles) {

                if (GUILayout.Button("Remove", GUILayout.Width(50f), GUILayout.ExpandWidth(true)))
                    RemoveAxle();

            }

            GUI.color = guiColor;

        }

        EditorGUILayout.EndVertical();

    }

    private void InputsButton() {

        EditorGUILayout.BeginVertical(GUI.skin.window);

        if (inputs)
            inputs.enabled = EditorGUILayout.ToggleLeft("", inputs.enabled, GUILayout.Width(15f));

        GUILayout.Label(("<color=#FF9500>Inputs</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

        if (GUILayout.Button(Resources.Load("Editor Icons/Icon_Inputs") as Texture, GUILayout.Width(70f), GUILayout.Height(35f), GUILayout.ExpandWidth(true))) {

            if (inputs)
                Selection.activeGameObject = inputs.gameObject;
            else
                AddInputs();

        }

        if (!EditorUtility.IsPersistent(prop)) {

            if (inputs)
                GUILayout.Label(("<color=#FF9500>Equipped</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));
            else
                GUILayout.Label(("<color=#FF9500>Create</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

            if (inputs) {

                GUI.color = Color.red;

                if (GUILayout.Button("Remove", GUILayout.Width(50f), GUILayout.ExpandWidth(true)))
                    RemoveInputs();

                GUI.color = guiColor;

            }

        }

        EditorGUILayout.EndVertical();

    }

    private void AudioButton() {

        EditorGUILayout.BeginVertical(GUI.skin.window);

        if (audio)
            audio.enabled = EditorGUILayout.ToggleLeft("", audio.enabled, GUILayout.Width(15f));

        GUILayout.Label(("<color=#FF9500>Audio</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

        if (GUILayout.Button(Resources.Load("Editor Icons/Icon_Audio") as Texture, GUILayout.Width(70f), GUILayout.Height(35f), GUILayout.ExpandWidth(true))) {

            if (audio)
                Selection.activeGameObject = audio.gameObject;
            else
                AddAudio();

        }

        if (!EditorUtility.IsPersistent(prop)) {

            if (audio)
                GUILayout.Label(("<color=#FF9500>Equipped</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));
            else
                GUILayout.Label(("<color=#FF9500>Create</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

            if (audio) {

                GUI.color = Color.red;

                if (GUILayout.Button("Remove", GUILayout.Width(50f), GUILayout.ExpandWidth(true)))
                    RemoveAudio();

                GUI.color = guiColor;

            }

        }

        EditorGUILayout.EndVertical();

    }

    private void CustomizerButton() {

        EditorGUILayout.BeginVertical(GUI.skin.window);

        if (customizer)
            customizer.enabled = EditorGUILayout.ToggleLeft("", customizer.enabled, GUILayout.Width(15f));

        GUILayout.Label(("<color=#FF9500>Customizer</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

        if (GUILayout.Button(Resources.Load("Editor Icons/Icon_Customizer") as Texture, GUILayout.Width(70f), GUILayout.Height(35f), GUILayout.ExpandWidth(true))) {

            if (customizer)
                Selection.activeGameObject = customizer.gameObject;
            else
                AddCustomizer();

        }

        if (!EditorUtility.IsPersistent(prop)) {

            if (customizer)
                GUILayout.Label(("<color=#FF9500>Equipped</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));
            else
                GUILayout.Label(("<color=#FF9500>Create</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

            if (customizer) {

                GUI.color = Color.red;

                if (GUILayout.Button("Remove", GUILayout.Width(50f), GUILayout.ExpandWidth(true)))
                    RemoveCustomizer();

                GUI.color = guiColor;

            }

        }

        EditorGUILayout.EndVertical();

    }

    private void AeroButton() {

        EditorGUILayout.BeginVertical(GUI.skin.window);

        if (aero)
            aero.enabled = EditorGUILayout.ToggleLeft("", aero.enabled, GUILayout.Width(15f));

        GUILayout.Label(("<color=#FF9500>Dynamics</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

        if (GUILayout.Button(Resources.Load("Editor Icons/Icon_Aero") as Texture, GUILayout.Width(70f), GUILayout.Height(35f), GUILayout.ExpandWidth(true))) {

            if (aero)
                Selection.activeGameObject = aero.gameObject;
            else
                AddAero();

        }

        if (!EditorUtility.IsPersistent(prop)) {

            if (aero)
                GUILayout.Label(("<color=#FF9500>Equipped</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));
            else
                GUILayout.Label(("<color=#FF9500>Create</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

            if (aero) {

                GUI.color = Color.red;

                if (GUILayout.Button("Remove", GUILayout.Width(50f), GUILayout.ExpandWidth(true)))
                    RemoveAero();

                GUI.color = guiColor;

            }

        }

        EditorGUILayout.EndVertical();

    }

    private void StabilityButton() {

        EditorGUILayout.BeginVertical(GUI.skin.window);

        if (stability)
            stability.enabled = EditorGUILayout.ToggleLeft("", stability.enabled, GUILayout.Width(15f));

        GUILayout.Label(("<color=#FF9500>Stability</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

        if (GUILayout.Button(Resources.Load("Editor Icons/Icon_Stability") as Texture, GUILayout.Width(70f), GUILayout.Height(35f), GUILayout.ExpandWidth(true))) {

            if (stability)
                Selection.activeGameObject = stability.gameObject;
            else
                AddStability();

        }

        if (!EditorUtility.IsPersistent(prop)) {

            if (stability)
                GUILayout.Label(("<color=#FF9500>Equipped</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));
            else
                GUILayout.Label(("<color=#FF9500>Create</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

            if (stability) {

                GUI.color = Color.red;

                if (GUILayout.Button("Remove", GUILayout.Width(50f), GUILayout.ExpandWidth(true)))
                    RemoveStability();

                GUI.color = guiColor;

            }

        }

        EditorGUILayout.EndVertical();

    }

    private void LightsButton() {

        EditorGUILayout.BeginVertical(GUI.skin.window);

        if (lights)
            lights.enabled = EditorGUILayout.ToggleLeft("", lights.enabled, GUILayout.Width(15f));

        GUILayout.Label(("<color=#FF9500>Lights</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

        if (GUILayout.Button(Resources.Load("Editor Icons/Icon_Light") as Texture, GUILayout.Width(70f), GUILayout.Height(35f), GUILayout.ExpandWidth(true))) {

            if (lights)
                Selection.activeGameObject = lights.gameObject;
            else
                AddLights();

        }

        if (!EditorUtility.IsPersistent(prop)) {

            if (lights)
                GUILayout.Label(("<color=#FF9500>Equipped</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));
            else
                GUILayout.Label(("<color=#FF9500>Create</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

            if (lights) {

                GUI.color = Color.red;

                if (GUILayout.Button("Remove", GUILayout.Width(50f), GUILayout.ExpandWidth(true)))
                    RemoveLights();

                GUI.color = guiColor;

            }

        }

        EditorGUILayout.EndVertical();

    }

    private void DamageButton() {

        EditorGUILayout.BeginVertical(GUI.skin.window);

        if (damage)
            damage.enabled = EditorGUILayout.ToggleLeft("", damage.enabled, GUILayout.Width(15f));

        GUILayout.Label(("<color=#FF9500>Damage</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

        if (GUILayout.Button(Resources.Load("Editor Icons/Icon_Damage") as Texture, GUILayout.Width(70f), GUILayout.Height(35f), GUILayout.ExpandWidth(true))) {

            if (damage)
                Selection.activeGameObject = damage.gameObject;
            else
                AddDamage();

        }

        if (!EditorUtility.IsPersistent(prop)) {

            if (damage)
                GUILayout.Label(("<color=#FF9500>Equipped</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));
            else
                GUILayout.Label(("<color=#FF9500>Create</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

            if (damage) {

                GUI.color = Color.red;

                if (GUILayout.Button("Remove", GUILayout.Width(50f), GUILayout.ExpandWidth(true)))
                    RemoveDamage();

                GUI.color = guiColor;

            }

        }

        EditorGUILayout.EndVertical();

    }

    private void ParticlesButton() {

        EditorGUILayout.BeginVertical(GUI.skin.window);

        if (particles)
            particles.enabled = EditorGUILayout.ToggleLeft("", particles.enabled, GUILayout.Width(15f));

        GUILayout.Label(("<color=#FF9500>Particles</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

        if (GUILayout.Button(Resources.Load("Editor Icons/Icon_Particles") as Texture, GUILayout.Width(70f), GUILayout.Height(35f), GUILayout.ExpandWidth(true))) {

            if (particles)
                Selection.activeGameObject = particles.gameObject;
            else
                AddParticles();

        }

        if (!EditorUtility.IsPersistent(prop)) {

            if (particles)
                GUILayout.Label(("<color=#FF9500>Equipped</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));
            else
                GUILayout.Label(("<color=#FF9500>Create</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

            if (particles) {

                GUI.color = Color.red;

                if (GUILayout.Button("Remove", GUILayout.Width(50f), GUILayout.ExpandWidth(true)))
                    RemoveParticles();

                GUI.color = guiColor;

            }

        }

        EditorGUILayout.EndVertical();

    }

    private void LODButton() {

        EditorGUILayout.BeginVertical(GUI.skin.window);

        if (lod)
            lod.enabled = EditorGUILayout.ToggleLeft("", lod.enabled, GUILayout.Width(15f));

        GUILayout.Label(("<color=#FF9500>LOD</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

        if (GUILayout.Button(Resources.Load("Editor Icons/Icon_LOD") as Texture, GUILayout.Width(70f), GUILayout.Height(35f), GUILayout.ExpandWidth(true))) {

            if (lod)
                Selection.activeGameObject = lod.gameObject;
            else
                AddLOD();

        }

        if (!EditorUtility.IsPersistent(prop)) {

            if (lod)
                GUILayout.Label(("<color=#FF9500>Equipped</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));
            else
                GUILayout.Label(("<color=#FF9500>Create</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

            if (lod) {

                GUI.color = Color.red;

                if (GUILayout.Button("Remove", GUILayout.Width(50f), GUILayout.ExpandWidth(true)))
                    RemoveLOD();

                GUI.color = guiColor;

            }

        }

        EditorGUILayout.EndVertical();

    }

    private void OtherAddonsButton() {

        EditorGUILayout.BeginVertical(GUI.skin.window);

        GUILayout.Label(("<color=#FF9500>Other Addons</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

        if (GUILayout.Button(Resources.Load("Editor Icons/Icon_OtherAddons") as Texture, GUILayout.Width(70f), GUILayout.Height(35f), GUILayout.ExpandWidth(true))) {

            if (otherAddons)
                Selection.activeGameObject = otherAddons.gameObject;
            else
                AddOtherAddons();

        }

        if (!EditorUtility.IsPersistent(prop)) {

            if (otherAddons)
                GUILayout.Label(("<color=#FF9500>Equipped</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));
            else
                GUILayout.Label(("<color=#FF9500>Create</color>"), GUILayout.Width(50f), GUILayout.ExpandWidth(true));

            if (otherAddons) {

                GUI.color = Color.red;

                if (GUILayout.Button("Remove", GUILayout.Width(50f), GUILayout.ExpandWidth(true)))
                    RemoveOtherAddons();

                GUI.color = guiColor;

            }

        }

        EditorGUILayout.EndVertical();

    }

    private void AddEngine() {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_Engine");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(0);
        engine = subject.AddComponent<RCCP_Engine>();

        EditorUtility.SetDirty(prop);

    }

    private void AddClutch() {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_Clutch");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(1);
        clutch = subject.gameObject.AddComponent<RCCP_Clutch>();

        EditorUtility.SetDirty(prop);

    }

    private void AddGearbox() {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_Gearbox");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(2);
        gearbox = subject.gameObject.AddComponent<RCCP_Gearbox>();

        EditorUtility.SetDirty(prop);

    }

    private void AddDifferential() {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_Differential");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(3);
        differential = subject.gameObject.AddComponent<RCCP_Differential>();

        EditorUtility.SetDirty(prop);

    }

    private void AddAxles() {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_Axles");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(4);
        axles = subject.gameObject.AddComponent<RCCP_Axles>();

        EditorUtility.SetDirty(prop);

    }

    private void AddAxle() {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_Axle_New");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(4);
        RCCP_Axle axle = subject.gameObject.AddComponent<RCCP_Axle>();
        axle.gameObject.name = "RCCP_Axle_New";
        axle.isBrake = true;
        axle.isHandbrake = true;

        EditorUtility.SetDirty(prop);

    }

    private void AddInputs() {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_Inputs");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(5);
        inputs = subject.gameObject.AddComponent<RCCP_Input>();

        EditorUtility.SetDirty(prop);

    }

    private void AddAero() {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_Aero");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(6);
        aero = subject.gameObject.AddComponent<RCCP_AeroDynamics>();

        EditorUtility.SetDirty(prop);

    }

    private void AddAudio() {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_Audio");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(7);
        audio = subject.gameObject.AddComponent<RCCP_Audio>();

        EditorUtility.SetDirty(prop);

    }

    private void AddCustomizer() {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_Customizer");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(8);
        customizer = subject.gameObject.AddComponent<RCCP_Customizer>();

        EditorUtility.SetDirty(prop);

    }

    private void AddStability() {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_Stability");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(9);
        stability = subject.gameObject.AddComponent<RCCP_Stability>();

        EditorUtility.SetDirty(prop);

    }

    private void AddLights() {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_Lights");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(10);
        lights = subject.gameObject.AddComponent<RCCP_Lights>();

        EditorUtility.SetDirty(prop);

    }

    private void AddDamage() {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_Damage");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(11);
        damage = subject.gameObject.AddComponent<RCCP_Damage>();

        EditorUtility.SetDirty(prop);

    }

    private void AddParticles() {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_Particles");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(12);
        particles = subject.gameObject.AddComponent<RCCP_Particles>();

        EditorUtility.SetDirty(prop);

    }

    private void AddLOD() {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_LOD");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(13);
        lod = subject.gameObject.AddComponent<RCCP_Lod>();

        EditorUtility.SetDirty(prop);

    }

    private void AddOtherAddons() {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_OtherAddons");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(13);
        otherAddons = subject.gameObject.AddComponent<RCCP_OtherAddons>();

        EditorUtility.SetDirty(prop);

    }

    private void RemoveEngine() {

        bool isPrefab = PrefabUtility.IsAnyPrefabInstanceRoot(Selection.activeGameObject);

        if (isPrefab) {

            bool disconnectPrefabConnection = (EditorUtility.DisplayDialog("Unpacking Prefab", "This gameobject is connected to a prefab. In order to do remove this component, you'll need to unpack the prefab connection first. After removing the component, you can override your existing prefab with this gameobject.", "Disconnect", "Cancel"));

            if (!disconnectPrefabConnection)
                return;

            PrefabUtility.UnpackPrefabInstance(Selection.activeGameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        }

        EditorApplication.delayCall += () => {

            bool answer = (EditorUtility.DisplayDialog("Removing Component", "Are you sure want to remove this component? You can't undo this operation.", "Remove", "Cancel"));

            if (answer) {

                DestroyImmediate(engine.gameObject);
                engine = null;

                EditorUtility.SetDirty(prop);

            }

        };

    }

    private void RemoveClutch() {

        bool isPrefab = PrefabUtility.IsAnyPrefabInstanceRoot(Selection.activeGameObject);

        if (isPrefab) {

            bool disconnectPrefabConnection = (EditorUtility.DisplayDialog("Unpacking Prefab", "This gameobject is connected to a prefab. In order to do remove this component, you'll need to unpack the prefab connection first. After removing the component, you can override your existing prefab with this gameobject.", "Disconnect", "Cancel"));

            if (!disconnectPrefabConnection)
                return;

            PrefabUtility.UnpackPrefabInstance(Selection.activeGameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        }

        EditorApplication.delayCall += () => {

            bool answer = (EditorUtility.DisplayDialog("Removing Component", "Are you sure want to remove this component? You can't undo this operation.", "Remove", "Cancel"));

            if (answer) {

                DestroyImmediate(clutch.gameObject);
                clutch = null;

                EditorUtility.SetDirty(prop);

            }

        };

    }

    private void RemoveGearbox() {

        bool isPrefab = PrefabUtility.IsAnyPrefabInstanceRoot(Selection.activeGameObject);

        if (isPrefab) {

            bool disconnectPrefabConnection = (EditorUtility.DisplayDialog("Unpacking Prefab", "This gameobject is connected to a prefab. In order to do remove this component, you'll need to unpack the prefab connection first. After removing the component, you can override your existing prefab with this gameobject.", "Disconnect", "Cancel"));

            if (!disconnectPrefabConnection)
                return;

            PrefabUtility.UnpackPrefabInstance(Selection.activeGameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        }

        EditorApplication.delayCall += () => {

            bool answer = (EditorUtility.DisplayDialog("Removing Component", "Are you sure want to remove this component? You can't undo this operation.", "Remove", "Cancel"));

            if (answer) {

                DestroyImmediate(gearbox.gameObject);
                gearbox = null;

                EditorUtility.SetDirty(prop);

            }

        };

    }

    private void RemoveDifferential() {

        bool isPrefab = PrefabUtility.IsAnyPrefabInstanceRoot(Selection.activeGameObject);

        if (isPrefab) {

            bool disconnectPrefabConnection = (EditorUtility.DisplayDialog("Unpacking Prefab", "This gameobject is connected to a prefab. In order to do remove this component, you'll need to unpack the prefab connection first. After removing the component, you can override your existing prefab with this gameobject.", "Disconnect", "Cancel"));

            if (!disconnectPrefabConnection)
                return;

            PrefabUtility.UnpackPrefabInstance(Selection.activeGameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        }

        EditorApplication.delayCall += () => {

            bool answer = (EditorUtility.DisplayDialog("Removing Component", "Are you sure want to remove this component? You can't undo this operation.", "Remove", "Cancel"));

            if (answer) {

                DestroyImmediate(differential.gameObject);
                differential = null;

                EditorUtility.SetDirty(prop);

            }

        };

    }

    private void RemoveAxle() {

        bool isPrefab = PrefabUtility.IsAnyPrefabInstanceRoot(Selection.activeGameObject);

        if (isPrefab) {

            bool disconnectPrefabConnection = (EditorUtility.DisplayDialog("Unpacking Prefab", "This gameobject is connected to a prefab. In order to do remove this component, you'll need to unpack the prefab connection first. After removing the component, you can override your existing prefab with this gameobject.", "Disconnect", "Cancel"));

            if (!disconnectPrefabConnection)
                return;

            PrefabUtility.UnpackPrefabInstance(Selection.activeGameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        }

        EditorApplication.delayCall += () => {

            bool answer = (EditorUtility.DisplayDialog("Removing Component", "Are you sure want to remove this component? You can't undo this operation.", "Remove", "Cancel"));

            if (answer) {

                DestroyImmediate(axles.gameObject);
                axles = null;

                EditorUtility.SetDirty(prop);

            }

        };

    }

    private void RemoveInputs() {

        bool isPrefab = PrefabUtility.IsAnyPrefabInstanceRoot(Selection.activeGameObject);

        if (isPrefab) {

            bool disconnectPrefabConnection = (EditorUtility.DisplayDialog("Unpacking Prefab", "This gameobject is connected to a prefab. In order to do remove this component, you'll need to unpack the prefab connection first. After removing the component, you can override your existing prefab with this gameobject.", "Disconnect", "Cancel"));

            if (!disconnectPrefabConnection)
                return;

            PrefabUtility.UnpackPrefabInstance(Selection.activeGameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        }

        EditorApplication.delayCall += () => {

            bool answer = (EditorUtility.DisplayDialog("Removing Component", "Are you sure want to remove this component? You can't undo this operation.", "Remove", "Cancel"));

            if (answer) {

                DestroyImmediate(inputs.gameObject);
                inputs = null;

                EditorUtility.SetDirty(prop);

            }

        };

    }

    private void RemoveAero() {

        bool isPrefab = PrefabUtility.IsAnyPrefabInstanceRoot(Selection.activeGameObject);

        if (isPrefab) {

            bool disconnectPrefabConnection = (EditorUtility.DisplayDialog("Unpacking Prefab", "This gameobject is connected to a prefab. In order to do remove this component, you'll need to unpack the prefab connection first. After removing the component, you can override your existing prefab with this gameobject.", "Disconnect", "Cancel"));

            if (!disconnectPrefabConnection)
                return;

            PrefabUtility.UnpackPrefabInstance(Selection.activeGameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        }

        EditorApplication.delayCall += () => {

            bool answer = (EditorUtility.DisplayDialog("Removing Component", "Are you sure want to remove this component? You can't undo this operation.", "Remove", "Cancel"));

            if (answer) {

                DestroyImmediate(aero.gameObject);
                aero = null;

                EditorUtility.SetDirty(prop);

            }

        };

    }

    private void RemoveAudio() {

        bool isPrefab = PrefabUtility.IsAnyPrefabInstanceRoot(Selection.activeGameObject);

        if (isPrefab) {

            bool disconnectPrefabConnection = (EditorUtility.DisplayDialog("Unpacking Prefab", "This gameobject is connected to a prefab. In order to do remove this component, you'll need to unpack the prefab connection first. After removing the component, you can override your existing prefab with this gameobject.", "Disconnect", "Cancel"));

            if (!disconnectPrefabConnection)
                return;

            PrefabUtility.UnpackPrefabInstance(Selection.activeGameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        }

        EditorApplication.delayCall += () => {

            bool answer = (EditorUtility.DisplayDialog("Removing Component", "Are you sure want to remove this component? You can't undo this operation.", "Remove", "Cancel"));

            if (answer) {

                DestroyImmediate(audio.gameObject);
                audio = null;

                EditorUtility.SetDirty(prop);

            }

        };

    }

    private void RemoveCustomizer() {

        bool isPrefab = PrefabUtility.IsAnyPrefabInstanceRoot(Selection.activeGameObject);

        if (isPrefab) {

            bool disconnectPrefabConnection = (EditorUtility.DisplayDialog("Unpacking Prefab", "This gameobject is connected to a prefab. In order to do remove this component, you'll need to unpack the prefab connection first. After removing the component, you can override your existing prefab with this gameobject.", "Disconnect", "Cancel"));

            if (!disconnectPrefabConnection)
                return;

            PrefabUtility.UnpackPrefabInstance(Selection.activeGameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        }

        EditorApplication.delayCall += () => {

            bool answer = (EditorUtility.DisplayDialog("Removing Component", "Are you sure want to remove this component? You can't undo this operation.", "Remove", "Cancel"));

            if (answer) {

                DestroyImmediate(customizer.gameObject);
                customizer = null;

                EditorUtility.SetDirty(prop);

            }

        };

    }

    private void RemoveStability() {

        bool isPrefab = PrefabUtility.IsAnyPrefabInstanceRoot(Selection.activeGameObject);

        if (isPrefab) {

            bool disconnectPrefabConnection = (EditorUtility.DisplayDialog("Unpacking Prefab", "This gameobject is connected to a prefab. In order to do remove this component, you'll need to unpack the prefab connection first. After removing the component, you can override your existing prefab with this gameobject.", "Disconnect", "Cancel"));

            if (!disconnectPrefabConnection)
                return;

            PrefabUtility.UnpackPrefabInstance(Selection.activeGameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        }

        EditorApplication.delayCall += () => {

            bool answer = (EditorUtility.DisplayDialog("Removing Component", "Are you sure want to remove this component? You can't undo this operation.", "Remove", "Cancel"));

            if (answer) {

                DestroyImmediate(stability.gameObject);
                stability = null;

                EditorUtility.SetDirty(prop);

            }

        };

    }

    private void RemoveLights() {

        bool isPrefab = PrefabUtility.IsAnyPrefabInstanceRoot(Selection.activeGameObject);

        if (isPrefab) {

            bool disconnectPrefabConnection = (EditorUtility.DisplayDialog("Unpacking Prefab", "This gameobject is connected to a prefab. In order to do remove this component, you'll need to unpack the prefab connection first. After removing the component, you can override your existing prefab with this gameobject.", "Disconnect", "Cancel"));

            if (!disconnectPrefabConnection)
                return;

            PrefabUtility.UnpackPrefabInstance(Selection.activeGameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        }

        EditorApplication.delayCall += () => {

            bool answer = (EditorUtility.DisplayDialog("Removing Component", "Are you sure want to remove this component? You can't undo this operation.", "Remove", "Cancel"));

            if (answer) {

                DestroyImmediate(lights.gameObject);
                lights = null;

                EditorUtility.SetDirty(prop);

            }

        };

    }

    private void RemoveDamage() {

        bool isPrefab = PrefabUtility.IsAnyPrefabInstanceRoot(Selection.activeGameObject);

        if (isPrefab) {

            bool disconnectPrefabConnection = (EditorUtility.DisplayDialog("Unpacking Prefab", "This gameobject is connected to a prefab. In order to do remove this component, you'll need to unpack the prefab connection first. After removing the component, you can override your existing prefab with this gameobject.", "Disconnect", "Cancel"));

            if (!disconnectPrefabConnection)
                return;

            PrefabUtility.UnpackPrefabInstance(Selection.activeGameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        }

        EditorApplication.delayCall += () => {

            bool answer = (EditorUtility.DisplayDialog("Removing Component", "Are you sure want to remove this component? You can't undo this operation.", "Remove", "Cancel"));

            if (answer) {

                DestroyImmediate(damage.gameObject);
                damage = null;

                EditorUtility.SetDirty(prop);

            }

        };

    }

    private void RemoveParticles() {

        bool isPrefab = PrefabUtility.IsAnyPrefabInstanceRoot(Selection.activeGameObject);

        if (isPrefab) {

            bool disconnectPrefabConnection = (EditorUtility.DisplayDialog("Unpacking Prefab", "This gameobject is connected to a prefab. In order to do remove this component, you'll need to unpack the prefab connection first. After removing the component, you can override your existing prefab with this gameobject.", "Disconnect", "Cancel"));

            if (!disconnectPrefabConnection)
                return;

            PrefabUtility.UnpackPrefabInstance(Selection.activeGameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        }

        EditorApplication.delayCall += () => {

            bool answer = (EditorUtility.DisplayDialog("Removing Component", "Are you sure want to remove this component? You can't undo this operation.", "Remove", "Cancel"));

            if (answer) {

                DestroyImmediate(particles.gameObject);
                particles = null;

                EditorUtility.SetDirty(prop);

            }

        };

    }

    private void RemoveLOD() {

        bool isPrefab = PrefabUtility.IsAnyPrefabInstanceRoot(Selection.activeGameObject);

        if (isPrefab) {

            bool disconnectPrefabConnection = (EditorUtility.DisplayDialog("Unpacking Prefab", "This gameobject is connected to a prefab. In order to do remove this component, you'll need to unpack the prefab connection first. After removing the component, you can override your existing prefab with this gameobject.", "Disconnect", "Cancel"));

            if (!disconnectPrefabConnection)
                return;

            PrefabUtility.UnpackPrefabInstance(Selection.activeGameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        }

        EditorApplication.delayCall += () => {

            bool answer = (EditorUtility.DisplayDialog("Removing Component", "Are you sure want to remove this component? You can't undo this operation.", "Remove", "Cancel"));

            if (answer) {

                DestroyImmediate(lod.gameObject);
                lod = null;

                EditorUtility.SetDirty(prop);

            }

        };

    }

    private void RemoveOtherAddons() {

        bool isPrefab = PrefabUtility.IsAnyPrefabInstanceRoot(Selection.activeGameObject);

        if (isPrefab) {

            bool disconnectPrefabConnection = (EditorUtility.DisplayDialog("Unpacking Prefab", "This gameobject is connected to a prefab. In order to do remove this component, you'll need to unpack the prefab connection first. After removing the component, you can override your existing prefab with this gameobject.", "Disconnect", "Cancel"));

            if (!disconnectPrefabConnection)
                return;

            PrefabUtility.UnpackPrefabInstance(Selection.activeGameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        }

        EditorApplication.delayCall += () => {

            bool answer = (EditorUtility.DisplayDialog("Removing Component", "Are you sure want to remove this component? You can't undo this operation.", "Remove", "Cancel"));

            if (answer) {

                DestroyImmediate(otherAddons.gameObject);
                otherAddons = null;

                EditorUtility.SetDirty(prop);

            }

        };

    }

    private void CheckSetup() {

        errorMessages.Clear();

        Collider colliderFound = null;
        Collider[] allColliders = prop.gameObject.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < allColliders.Length; i++) {

            Collider col = allColliders[i];

            if (!(col is WheelCollider))
                colliderFound = col;

        }

        if (colliderFound == null)
            errorMessages.Add("Body collider not found");

        if (axles != null) {

            bool steerAxlefound = false;

            RCCP_Axle[] allAxles = axles.GetComponentsInChildren<RCCP_Axle>(true);

            if (allAxles != null && allAxles.Length < 2)
                errorMessages.Add("Two axles needed at least. Vehicle has " + allAxles.Length.ToString() + " axle currently.");

            if (allAxles != null) {

                for (int i = 0; i < allAxles.Length; i++) {

                    if (allAxles[i] != null && allAxles[i].isSteer)
                        steerAxlefound = true;

                }

            }

            if (!steerAxlefound)
                errorMessages.Add("Steer axle not found");

        }

        bool missingWheelModelsFound = false;

        if (axles != null) {

            RCCP_Axle[] allAxles = axles.GetComponentsInChildren<RCCP_Axle>(true);

            for (int i = 0; i < allAxles.Length; i++) {

                if (allAxles[i].leftWheelModel == null || allAxles[i].rightWheelModel == null)
                    missingWheelModelsFound = true;

            }

        }

        if (missingWheelModelsFound)
            errorMessages.Add("Missing Wheel Models Found On Axle");

        GUI.color = Color.red;

        for (int i = 0; i < errorMessages.Count; i++) {

            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(errorMessages[i]);
            EditorGUILayout.EndVertical();

        }

        GUI.color = guiColor;

    }

    private void CheckMissingAxleManager() {

        bool axleFound = false;

        RCCP_Axle[] foundAxles = prop.GetComponentsInChildren<RCCP_Axle>(true);

        if (foundAxles.Length >= 1)
            axleFound = true;

        if (axleFound) {

            bool axleManagerFound = false;

            RCCP_Axles foundAxleManager = prop.GetComponentInChildren<RCCP_Axles>(true);

            if (foundAxleManager != null)
                axleManagerFound = true;

            if (!axleManagerFound) {

                GameObject newAxleManager = new GameObject("RCCP_Axles");
                newAxleManager.transform.SetParent(prop.transform, false);
                axles = newAxleManager.AddComponent<RCCP_Axles>();
                Debug.Log("Found missing axle manager on " + prop.transform.name + ". Adding it...");

            } else {

                for (int i = 0; i < foundAxles.Length; i++) {

                    if (foundAxles[i].transform.parent != foundAxleManager.transform)
                        foundAxles[i].transform.SetParent(foundAxleManager.transform, false);

                }

            }

        }

    }

}
