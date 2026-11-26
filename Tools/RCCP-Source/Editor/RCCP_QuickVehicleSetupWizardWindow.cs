//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright © 2014 - 2024 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class RCCP_QuickVehicleSetupWizardWindow : EditorWindow {

    private GUISkin skin;
    private string nextStepButtonString;

    private GameObject selectedRoot;

    public GameObject body;
    public GameObject wheel_FL, wheel_FR, wheel_RL, wheel_RR;

    public bool addInputs, addDynamics, addStability, addAudio, addCustomizer, addLights, addDamage, addParticles, addLOD, addOtherAddons;

    public int width = 500;
    public int height = 260;

    public int toolbarIndex = 0;

    public float startTime = 0f;
    public float endTime = 0f;

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Quick Vehicle Setup Wizard", false, -85)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Quick Vehicle Setup Wizard", false, -85)]
    public static void OpenWindow() {

        GetWindow(typeof(RCCP_QuickVehicleSetupWizardWindow), false);

    }

    private void OnEnable() {

        startTime = 0f;
        endTime = 0f;

        minSize = new Vector2(width, height);
        maxSize = minSize;

        skin = (GUISkin)Resources.Load("RCCP_Gui");
        nextStepButtonString = "Next";

    }

    public void OnGUI() {

        if (skin != null)
            GUI.skin = skin;

        EditorGUILayout.BeginVertical(GUI.skin.box);

        switch (toolbarIndex) {

            case 0:
                ToolbarMenu_Welcome();
                break;

            case 1:
                ToolbarMenu_CreateBehavior();
                break;

            case 2:
                ToolbarMenu_CustomizeBehavior();
                break;

            case 3:
                ToolbarMenu_SelectFrontWheels();
                break;

            case 4:
                ToolbarMenu_SelectRearWheels();
                break;

            case 5:
                ToolbarMenu_Collider();
                break;

            case 6:
                ToolbarMenu_Components();
                break;

            case 7:
                ToolbarMenu_Finish();
                break;

        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndVertical();

        Color defGuiColor = GUI.color;
        GUI.color = Color.green;

        if (toolbarIndex == 1 && selectedRoot == null)
            GUI.enabled = false;

        if (toolbarIndex == 3 && (wheel_FL == null || wheel_FR == null))
            GUI.enabled = false;

        if (toolbarIndex == 4 && (wheel_RL == null || wheel_RR == null))
            GUI.enabled = false;

        if (GUILayout.Button(nextStepButtonString))
            Next();

        GUI.enabled = true;
        GUI.color = defGuiColor;

        Repaint();

    }

    private void ToolbarMenu_Welcome() {

        GUILayout.Label("Welcome", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox("Welcome to Quick Vehicle Setup Wizard. You'll be able to create your new vehicle with a few steps. Setup includes these simple steps;\n\n1. Selecting the root of the vehicle gameobject in the scene and adding the main controller.\n2. Selecting the front wheels\n3. Selecting the rear wheels\n4. Adding / checking body collider.\n5. Adding addon components.\n\nPlease proceed to the next step to get started.\n\nThis setup only covers the main essentials, editing lights, damage, audio, and other things can be done by accesing to the corresponding addon component after the setup.", MessageType.None);
        nextStepButtonString = "Next";

        startTime = (float)EditorApplication.timeSinceStartup;
        endTime = 0f;

        selectedRoot = null;
        wheel_FL = null;
        wheel_FR = null;
        wheel_RL = null;
        wheel_RR = null;
        body = null;

    }

    private void ToolbarMenu_CreateBehavior() {

        GUILayout.Label("Add Main Controller", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox("We must add the main controller (RCCP_CarController) to the root of the vehicle gameobject. Don't select multiple gameobjects, or prefabs located in the project directory. \n\n1. Drag and drop the vehicle model, gameobject, or prefab to the scene.\n2. Select the root of your vehicle gameobject in the scene.\n3. And add the main controller by clicking the below green button.", MessageType.None);
        nextStepButtonString = "Yes, I've Selected The Root Of The GameObject, Next";

        EditorGUILayout.Space();

        selectedRoot = null;

        if (Selection.activeGameObject != null) {

            if (Selection.gameObjects.Length == 1 && Selection.activeGameObject.scene.name != null && !EditorUtility.IsPersistent(Selection.activeGameObject))
                selectedRoot = Selection.activeGameObject;

        }

        if (selectedRoot != null) {

            GUILayout.Label("This GameObject Is Root Of My Vehicle", EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(selectedRoot.name);
            EditorGUILayout.EndVertical();

        }

    }

    private void ToolbarMenu_CustomizeBehavior() {

        GUILayout.Label("Customize Controller", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox("All drivetrain components have been added and connected together. Now you can proceed to selecting the wheels step. Doesn't it look like it's weird to have an empty step like this? Oh, I'm too lazy to remove this step right now, maybe I can remove this step in the next update. But hey, seems like you're doing great job, move on :)", MessageType.None);
        nextStepButtonString = "You Talk Too Much, Next";

    }

    private void ToolbarMenu_SelectFrontWheels() {

        GUILayout.Label("Front Wheels", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox("This vehicle has two axes for front wheels and rear wheels. Please select the front left and front right wheels now.\n\nYou can have any amount of axles on your vehicle. Axles can be accesed through the Axles component on the vehicle. You can create / edit / remove any axle. But once you create another new axle, you must select its left and right wheel manually. No worries, editor will guide you.", MessageType.None);
        nextStepButtonString = "Yes, I've Selected Front Left And Right Wheels, Next";

        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(GUI.skin.box);

        wheel_FL = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Front Left Wheel"), wheel_FL, typeof(GameObject), true);
        EditorGUILayout.Space();
        wheel_FR = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Front Right Wheel"), wheel_FR, typeof(GameObject), true);

        EditorGUILayout.EndVertical();

    }

    private void ToolbarMenu_SelectRearWheels() {

        GUILayout.Label("Rear Wheels", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox("This vehicle has two axes for front wheels and rear wheels. Please select the rear left and rear right wheels now.\n\nYou can have any amount of axles on your vehicle. Axles can be accesed through the Axles component on the vehicle. You can create / edit / remove any axle. But once you create another new axle, you must select its left and right wheel manually. No worries, editor will guide you.", MessageType.None);
        nextStepButtonString = "Yes, I've Selected Rear Left And Right Wheels, Next";

        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(GUI.skin.box);

        wheel_RL = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Rear Left Wheel"), wheel_RL, typeof(GameObject), true);
        EditorGUILayout.Space();
        wheel_RR = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Rear Right Wheel"), wheel_RR, typeof(GameObject), true);

        EditorGUILayout.EndVertical();

    }

    private void ToolbarMenu_Collider() {

        GUILayout.Label("Body Collider", EditorStyles.boldLabel);

        Collider[] colliders = selectedRoot.GetComponentsInChildren<Collider>();
        bool colliderFound = false;

        for (int i = 0; i < colliders.Length; i++) {

            if (!(colliders[i] as WheelCollider))
                colliderFound = true;

        }

        if (!colliderFound)
            EditorGUILayout.HelpBox("This vehicle doesn't have a body collider. Please select the main body part of your vehicle, and add a mesh collider.", MessageType.None);
        else
            EditorGUILayout.HelpBox("Few colliders have been found in the vehicle. I'm not sure which one is the body collider, please make sure your vehicle as a body collider.", MessageType.None);

        nextStepButtonString = "Yes, I've Added A Body Collider, Next";

        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(GUI.skin.box);

        body = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Main Body"), body, typeof(GameObject), true);
        EditorGUILayout.Space();

        if (!body || (body && body.GetComponent<MeshCollider>()))
            GUI.enabled = false;

        if (GUILayout.Button("Add MeshCollider To Selected Body")) {

            MeshCollider meshCol = body.AddComponent<MeshCollider>();
            meshCol.convex = true;

        }

        GUI.enabled = true;

        EditorGUILayout.EndVertical();

    }

    private void ToolbarMenu_Components() {

        GUILayout.Label("Adding Addon Components", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox("This vehicle only has drivetrain components. Please select the addon components below you want to have them on your vehicle.", MessageType.None);
        nextStepButtonString = "Yes, I've Selected Addon Components, Next";

        float defaultLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 135f;

        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.BeginHorizontal(GUI.skin.box);
        addInputs = EditorGUILayout.ToggleLeft("Inputs", addInputs);
        addDynamics = EditorGUILayout.ToggleLeft("Dynamics", addDynamics);
        addStability = EditorGUILayout.ToggleLeft("Stability", addStability);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal(GUI.skin.box);
        addCustomizer = EditorGUILayout.ToggleLeft("Customizer", addCustomizer);
        addLights = EditorGUILayout.ToggleLeft("Lights", addLights);
        addDamage = EditorGUILayout.ToggleLeft("Damage", addDamage);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal(GUI.skin.box);
        addParticles = EditorGUILayout.ToggleLeft("Particles", addParticles);
        addLOD = EditorGUILayout.ToggleLeft("LOD", addLOD);
        addAudio = EditorGUILayout.ToggleLeft("Audio", addAudio);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal(GUI.skin.box);
        addOtherAddons = EditorGUILayout.ToggleLeft("Other Addons Manager", addOtherAddons);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUIUtility.labelWidth = defaultLabelWidth;

        EditorGUILayout.BeginHorizontal(GUI.skin.box);

        Color defaultGUIColor = GUI.color;
        GUI.color = Color.cyan;

        if (GUILayout.Button("Select All")) {

            addInputs = true;
            addDynamics = true;
            addStability = true;
            addAudio = true;
            addCustomizer = true;
            addLights = true;
            addDamage = true;
            addParticles = true;
            addLOD = true;
            addOtherAddons = true;

        }

        if (GUILayout.Button("Select None")) {

            addInputs = false;
            addDynamics = false;
            addStability = false;
            addAudio = false;
            addCustomizer = false;
            addLights = false;
            addDamage = false;
            addParticles = false;
            addLOD = false;
            addOtherAddons = false;

        }

        GUI.color = defaultGUIColor;

        EditorGUILayout.EndHorizontal();

    }

    private void ToolbarMenu_Finish() {

        GUILayout.Label("Congratulations!", EditorStyles.boldLabel);

        if (endTime == 0)
            endTime = (float)EditorApplication.timeSinceStartup;

        float timeToCreate = endTime - startTime;
        EditorGUILayout.HelpBox("Congratulations, you've created a new vehicle within " + timeToCreate.ToString("F0") + " seconds!\n\nThis vehicle using default resources, you can change any of them by clicking the corresponding addon component button. Have fun!", MessageType.None);
        nextStepButtonString = "Thanks, See You Next Time...";

    }

    public void Next() {

        if (toolbarIndex == 1 && selectedRoot) {

            RCCP_CarController newVehicle = RCCP_CreateNewVehicle.NewVehicle(selectedRoot);

            if (newVehicle)
                selectedRoot = newVehicle.gameObject;
            else
                Close();

        }

        if (toolbarIndex == 3 && wheel_FL && wheel_FR) {

            RCCP_Axle frontAxle = null;
            RCCP_Axle[] allAxles = selectedRoot.GetComponentsInChildren<RCCP_Axle>(true);

            for (int i = 0; i < allAxles.Length; i++) {

                if (allAxles[i].transform.name == "RCCP_Axle_Front")
                    frontAxle = allAxles[i];

            }

            if (frontAxle)
                RCCP_CreateNewVehicle.AssignWheelsToAxle(frontAxle, wheel_FL, wheel_FR);

        }

        if (toolbarIndex == 4 && wheel_RL && wheel_RR) {

            RCCP_Axle rearAxle = null;
            RCCP_Axle[] allAxles = selectedRoot.GetComponentsInChildren<RCCP_Axle>(true);

            for (int i = 0; i < allAxles.Length; i++) {

                if (allAxles[i].transform.name == "RCCP_Axle_Rear")
                    rearAxle = allAxles[i];

            }

            if (rearAxle)
                RCCP_CreateNewVehicle.AssignWheelsToAxle(rearAxle, wheel_RL, wheel_RR);

        }

        if (toolbarIndex == 6) {

            RCCP_CarController prop = selectedRoot.GetComponent<RCCP_CarController>();

            if (addInputs)
                RCCP_CreateNewVehicle.AddInputs(prop);
            if (addDynamics)
                RCCP_CreateNewVehicle.AddAero(prop);
            if (addStability)
                RCCP_CreateNewVehicle.AddStability(prop);
            if (addAudio)
                RCCP_CreateNewVehicle.AddAudio(prop);
            if (addCustomizer)
                RCCP_CreateNewVehicle.AddCustomizer(prop);

            if (addDamage)
                RCCP_CreateNewVehicle.AddDamage(prop);
            if (addLights)
                RCCP_CreateNewVehicle.AddLights(prop);
            if (addLOD)
                RCCP_CreateNewVehicle.AddLOD(prop);
            if (addOtherAddons)
                RCCP_CreateNewVehicle.AddOtherAddons(prop);
            if (addParticles)
                RCCP_CreateNewVehicle.AddParticles(prop);

        }

        toolbarIndex++;

        if (toolbarIndex > 7) {

            toolbarIndex = 0;
            Close();

        }

    }

    public void ResetToDefault() {

        toolbarIndex = 0;

    }

}
