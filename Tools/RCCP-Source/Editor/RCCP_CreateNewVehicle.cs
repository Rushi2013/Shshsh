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
using UnityEngine.Events;
using System;
using UnityEditor.Events;

public class RCCP_CreateNewVehicle {

    public static RCCP_CarController NewVehicle(GameObject vehicle) {

        if (vehicle == null)
            return null;

        if (vehicle.GetComponentInParent<RCCP_CarController>(true) != null) {

            EditorUtility.DisplayDialog("Already Has RCCP_CarController", "Selected vehicle already has RCCP_CarController. Are you sure you didn't pick the wrong house, oh vehicle?", "Close");
            return null;

        }

        if (EditorUtility.IsPersistent(Selection.activeGameObject)) {

            EditorUtility.DisplayDialog("Please select a vehicle in the scene", "Please select a vehicle in the scene, not in the project. Drag and drop the vehicle model to the scene, and try again.", "Close");
            return null;

        }

        bool isPrefab = PrefabUtility.IsAnyPrefabInstanceRoot(Selection.activeGameObject);

        if (isPrefab) {

            bool isModelPrefab = PrefabUtility.IsPartOfModelPrefab(Selection.activeGameObject);
            bool unpackPrefab = EditorUtility.DisplayDialog("Unpack Prefab", "This gameobject is connected to a " + (isModelPrefab ? "model" : "") + " prefab. Would you like to unpack the prefab completely? If you don't unpack it, you won't be able to move, reorder, or delete any children instance of the prefab.", "Unpack", "Don't Unpack");

            if (unpackPrefab)
                PrefabUtility.UnpackPrefabInstance(Selection.activeGameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        }

        bool foundRigids = false;

        if (Selection.activeGameObject.GetComponentInChildren<Rigidbody>(true))
            foundRigids = true;

        if (foundRigids) {

            bool removeRigids = EditorUtility.DisplayDialog("Rigidbodies Found", "Additional rigidbodies found in your vehicle. Additional rigidbodies will affect vehicle behavior directly.", "Remove Them", "Leave Them");

            if (removeRigids) {

                foreach (Rigidbody rigidbody in Selection.activeGameObject.GetComponentsInChildren<Rigidbody>(true))
                    UnityEngine.Object.DestroyImmediate(rigidbody);

            }

        }

        bool foundWheelColliders = false;

        if (Selection.activeGameObject.GetComponentInChildren<WheelCollider>(true))
            foundWheelColliders = true;

        if (foundWheelColliders) {

            bool removeWheelColliders = EditorUtility.DisplayDialog("WheelColliders Found", "Additional wheelcolliders found in your vehicle.", "Remove Them", "Leave Them");

            if (removeWheelColliders) {

                foreach (WheelCollider wc in Selection.activeGameObject.GetComponentsInChildren<WheelCollider>(true))
                    UnityEngine.Object.DestroyImmediate(wc);

            }

        }

        bool fixPivot = EditorUtility.DisplayDialog("Fix Pivot Position Of The Vehicle", "Would you like to fix pivot position of the vehicle? If your vehicle has correct pivot position, select no.", "Fix", "No");

        if (fixPivot) {

            GameObject pivot = new GameObject(Selection.activeGameObject.name);
            pivot.transform.position = RCCP_GetBounds.GetBoundsCenter(Selection.activeGameObject.transform);
            pivot.transform.rotation = Selection.activeGameObject.transform.rotation;

            pivot.AddComponent<RCCP_CarController>();

            Selection.activeGameObject.transform.SetParent(pivot.transform);
            Selection.activeGameObject = pivot;

        } else {

            GameObject selectedVehicle = Selection.activeGameObject;

            selectedVehicle.AddComponent<RCCP_CarController>();
            Selection.activeGameObject = selectedVehicle;

        }

        Rigidbody rigid = Selection.activeGameObject.GetComponent<Rigidbody>();
        rigid.mass = 1350f;
        rigid.drag = .01f;
        rigid.angularDrag = .25f;
        rigid.interpolation = RigidbodyInterpolation.Interpolate;
        rigid.collisionDetectionMode = CollisionDetectionMode.Discrete;

        RCCP_CarController newVehicle = Selection.activeGameObject.GetComponent<RCCP_CarController>();

        int answer = EditorUtility.DisplayDialogComplex("Adding Components", "Would you like to add all components (engine, clutch, gearbox, differential, and axle) automatically?", "Yes", "No", "");

        if (answer == 0)
            AddAllComponents(Selection.activeGameObject.GetComponent<RCCP_CarController>());

        return newVehicle;

    }

    public static void AssignWheelsToAxle(RCCP_Axle axle, GameObject wheel_L, GameObject wheel_R) {

        axle.leftWheelModel = wheel_L.transform;
        axle.rightWheelModel = wheel_R.transform;

        if (axle.leftWheelCollider) {

            axle.leftWheelCollider.wheelModel = axle.leftWheelModel;
            axle.leftWheelCollider.AlignWheel();

        }

        if (axle.rightWheelCollider) {

            axle.rightWheelCollider.wheelModel = axle.rightWheelModel;
            axle.rightWheelCollider.AlignWheel();

        }

    }

    public static void AddAllComponents(RCCP_CarController prop) {

        AddEngine(prop);
        AddClutch(prop);
        AddGearbox(prop);
        AddAxles(prop);
        AddDifferential(prop);
        AddEngineToClutchListener(prop);
        AddClutchToGearboxListener(prop);
        AddGearboxToDifferentialListener(prop);
        AddDifferentialToAxle(prop);

    }

    public static void AddEngine(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        if (prop.gameObject.GetComponentInChildren<RCCP_Engine>(true))
            return;

        GameObject subject = new GameObject("RCCP_Engine");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(0);
        subject.AddComponent<RCCP_Engine>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddClutch(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        if (prop.gameObject.GetComponentInChildren<RCCP_Clutch>(true))
            return;

        GameObject subject = new GameObject("RCCP_Clutch");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(1);
        subject.gameObject.AddComponent<RCCP_Clutch>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddGearbox(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        if (prop.gameObject.GetComponentInChildren<RCCP_Gearbox>(true))
            return;

        GameObject subject = new GameObject("RCCP_Gearbox");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(2);
        subject.gameObject.AddComponent<RCCP_Gearbox>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddDifferential(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        GameObject subject = new GameObject("RCCP_Differential");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(3);
        subject.gameObject.AddComponent<RCCP_Differential>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddAxles(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        if (prop.gameObject.GetComponentInChildren<RCCP_Axles>(true))
            return;

        GameObject subject = new GameObject("RCCP_Axles");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(4);
        subject.gameObject.AddComponent<RCCP_Axles>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddAxle(RCCP_CarController prop) {

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

    public static void AddInputs(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        if (prop.gameObject.GetComponentInChildren<RCCP_Input>(true))
            return;

        GameObject subject = new GameObject("RCCP_Inputs");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(5);
        subject.gameObject.AddComponent<RCCP_Input>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddAero(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        if (prop.gameObject.GetComponentInChildren<RCCP_AeroDynamics>(true))
            return;

        GameObject subject = new GameObject("RCCP_Aero");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(6);
        subject.gameObject.AddComponent<RCCP_AeroDynamics>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddAudio(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        if (prop.gameObject.GetComponentInChildren<RCCP_Audio>(true))
            return;

        GameObject subject = new GameObject("RCCP_Audio");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(7);
        subject.gameObject.AddComponent<RCCP_Audio>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddCustomizer(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        if (prop.gameObject.GetComponentInChildren<RCCP_Customizer>(true))
            return;

        GameObject subject = new GameObject("RCCP_Customizer");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(8);
        subject.gameObject.AddComponent<RCCP_Customizer>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddStability(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        if (prop.gameObject.GetComponentInChildren<RCCP_Stability>(true))
            return;

        GameObject subject = new GameObject("RCCP_Stability");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(9);
        subject.gameObject.AddComponent<RCCP_Stability>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddLights(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        if (prop.gameObject.GetComponentInChildren<RCCP_Lights>(true))
            return;

        GameObject subject = new GameObject("RCCP_Lights");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(10);
        subject.gameObject.AddComponent<RCCP_Lights>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddDamage(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        if (prop.gameObject.GetComponentInChildren<RCCP_Damage>(true))
            return;

        GameObject subject = new GameObject("RCCP_Damage");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(11);
        subject.gameObject.AddComponent<RCCP_Damage>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddParticles(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        if (prop.gameObject.GetComponentInChildren<RCCP_Particles>(true))
            return;

        GameObject subject = new GameObject("RCCP_Particles");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(12);
        subject.gameObject.AddComponent<RCCP_Particles>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddLOD(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        if (prop.gameObject.GetComponentInChildren<RCCP_Lod>(true))
            return;

        GameObject subject = new GameObject("RCCP_LOD");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(13);
        subject.gameObject.AddComponent<RCCP_Lod>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddOtherAddons(RCCP_CarController prop) {

        if (EditorUtility.IsPersistent(prop))
            return;

        if (prop.gameObject.GetComponentInChildren<RCCP_OtherAddons>(true))
            return;

        GameObject subject = new GameObject("RCCP_OtherAddons");
        subject.transform.SetParent(prop.transform, false);
        subject.transform.SetSiblingIndex(13);
        subject.gameObject.AddComponent<RCCP_OtherAddons>();

        EditorUtility.SetDirty(prop);

    }

    public static void AddEngineToClutchListener(RCCP_CarController prop) {

        RCCP_Engine engine = prop.GetComponentInChildren<RCCP_Engine>(true);
        RCCP_Clutch clutch = prop.GetComponentInChildren<RCCP_Clutch>(true);

        engine.outputEvent = new RCCP_Event_Output();

        var targetinfo = UnityEvent.GetValidMethodInfo(clutch,
"ReceiveOutput", new Type[] { typeof(RCCP_Output) });

        var methodDelegate = Delegate.CreateDelegate(typeof(UnityAction<RCCP_Output>), clutch, targetinfo) as UnityAction<RCCP_Output>;
        UnityEventTools.AddPersistentListener(engine.outputEvent, methodDelegate);

    }

    public static void AddClutchToGearboxListener(RCCP_CarController prop) {

        RCCP_Gearbox gearbox = prop.GetComponentInChildren<RCCP_Gearbox>(true);
        RCCP_Clutch clutch = prop.GetComponentInChildren<RCCP_Clutch>(true);

        clutch.outputEvent = new RCCP_Event_Output();

        var targetinfo = UnityEvent.GetValidMethodInfo(gearbox,
"ReceiveOutput", new Type[] { typeof(RCCP_Output) });

        var methodDelegate = Delegate.CreateDelegate(typeof(UnityAction<RCCP_Output>), gearbox, targetinfo) as UnityAction<RCCP_Output>;
        UnityEventTools.AddPersistentListener(clutch.outputEvent, methodDelegate);

    }

    public static void AddGearboxToDifferentialListener(RCCP_CarController prop) {

        RCCP_Gearbox gearbox = prop.GetComponentInChildren<RCCP_Gearbox>(true);
        RCCP_Differential differential = prop.GetComponentInChildren<RCCP_Differential>(true);

        gearbox.outputEvent = new RCCP_Event_Output();

        var targetinfo = UnityEvent.GetValidMethodInfo(differential,
"ReceiveOutput", new Type[] { typeof(RCCP_Output) });

        var methodDelegate = Delegate.CreateDelegate(typeof(UnityAction<RCCP_Output>), differential, targetinfo) as UnityAction<RCCP_Output>;
        UnityEventTools.AddPersistentListener(gearbox.outputEvent, methodDelegate);

    }

    public static void AddDifferentialToAxle(RCCP_CarController prop) {

        RCCP_Axles axles = prop.GetComponentInChildren<RCCP_Axles>(true);
        RCCP_Differential differential = prop.GetComponentInChildren<RCCP_Differential>(true);

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

}
