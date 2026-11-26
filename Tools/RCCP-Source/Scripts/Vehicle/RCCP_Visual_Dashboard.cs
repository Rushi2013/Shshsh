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
using UnityEngine.UI;

/// <summary>
/// Receiving inputs from active vehicle on your scene, and feeds visual dashboard needles (Not UI).
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Other Addons/RCCP Visual Dashboard")]
public class RCCP_Visual_Dashboard : RCCP_Component {

    /// <summary>
    /// Driver steering wheel model. In case of if your vehicle has individual steering wheel model in interior.
    /// </summary>
    public Transform steeringWheel;

    /// <summary>
    /// Original rotation of steering wheel.
    /// </summary>
    private Quaternion orgSteeringWheelRot;

    /// <summary>
    /// Rotation axis of steering wheel.
    /// </summary>
    public enum SteeringWheelRotateAround { XAxis, YAxis, ZAxis }

    /// <summary>
    /// Current rotation of steering wheel.
    /// </summary>
    public SteeringWheelRotateAround steeringWheelRotateAround = SteeringWheelRotateAround.ZAxis;

    /// <summary>
    /// Steering angle multiplier.
    /// </summary>
    public float steeringAngleMultiplier = 3f;

    /// <summary>
    /// Steering angle smoother.
    /// </summary>
    [Min(0f)] public float steeringAngleSmoother = 5f;

    [System.Serializable]
    public class RPMDial {

        /// <summary>
        /// Actual dial model.
        /// </summary>
        public GameObject dial;

        /// <summary>
        /// Rotation multiplier.
        /// </summary>
        [Min(0f)] public float multiplier = .05f;

        /// <summary>
        /// Rotation axis.
        /// </summary>
        public RotateAround rotateAround = RotateAround.Z;

        /// <summary>
        /// Rotation of the dial.
        /// </summary>
        private Quaternion dialOrgRotation = Quaternion.identity;

        /// <summary>
        /// Optional text.
        /// </summary>
        public Text text;

        /// <summary>
        /// Initializing the dial.
        /// </summary>
        public void Init() {

            if (dial)
                dialOrgRotation = dial.transform.localRotation;

        }

        /// <summary>
        /// Updates the rotation of the dial.
        /// </summary>
        /// <param name="value"></param>
        public void Update(float value) {

            Vector3 targetAxis = Vector3.forward;

            switch (rotateAround) {

                case RotateAround.X:

                    targetAxis = Vector3.right;
                    break;

                case RotateAround.Y:

                    targetAxis = Vector3.up;
                    break;

                case RotateAround.Z:

                    targetAxis = Vector3.forward;
                    break;

            }

            dial.transform.localRotation = dialOrgRotation * Quaternion.AngleAxis(-multiplier * value, targetAxis);

            if (text)
                text.text = value.ToString("F0");

        }

    }

    [System.Serializable]
    public class InteriorLight {

        public Light lightSource;
        [Range(0f, 10f)] public float intensity = 1f;
        public LightRenderMode renderMode = LightRenderMode.Auto;

        public void Init() {

            if (lightSource)
                lightSource.renderMode = renderMode;

        }

        public void Update(bool state) {

            if (!lightSource)
                return;

            if (!lightSource.enabled)
                lightSource.enabled = true;

            lightSource.intensity = state ? intensity : 0f;

        }

    }

    [Space()]
    public RPMDial rPMDial = new RPMDial();
    [Space()]
    public RPMDial speedDial = new RPMDial();
    [Space()]
    public InteriorLight[] interiorLights = new InteriorLight[0];

    public enum RotateAround { X, Y, Z }

    public override void Start() {

        base.Start();

        //  Initializing dials.
        rPMDial.Init();
        speedDial.Init();

        //  Initializing lights.
        for (int i = 0; i < interiorLights.Length; i++)
            interiorLights[i].Init();

    }

    private void Update() {

        SteeringWheel();
        Dials();
        Lights();

    }

    /// <summary>
    /// Operating the steering wheel.
    /// </summary>
    private void SteeringWheel() {

        //Driver SteeringWheel Transform.
        if (steeringWheel) {

            if (orgSteeringWheelRot.eulerAngles == Vector3.zero)
                orgSteeringWheelRot = steeringWheel.transform.localRotation;

            Quaternion targetRotation = Quaternion.identity;

            switch (steeringWheelRotateAround) {

                case SteeringWheelRotateAround.XAxis:
                    targetRotation = orgSteeringWheelRot * Quaternion.AngleAxis(CarController.steerAngle * steeringAngleMultiplier, -Vector3.right);
                    break;

                case SteeringWheelRotateAround.YAxis:
                    targetRotation = orgSteeringWheelRot * Quaternion.AngleAxis(CarController.steerAngle * steeringAngleMultiplier, -Vector3.up);
                    break;

                case SteeringWheelRotateAround.ZAxis:
                    targetRotation = orgSteeringWheelRot * Quaternion.AngleAxis(CarController.steerAngle * steeringAngleMultiplier, -Vector3.forward);
                    break;

            }

            steeringWheel.transform.localRotation = Quaternion.Slerp(steeringWheel.transform.localRotation, targetRotation, Time.deltaTime * steeringAngleSmoother * 3f);

        }

    }

    /// <summary>
    /// Updates dials rotation.
    /// </summary>
    private void Dials() {

        if (rPMDial.dial != null)
            rPMDial.Update(CarController.engineRPM);

        if (speedDial.dial != null)
            speedDial.Update(Mathf.Abs(CarController.speed));

    }

    /// <summary>
    /// Updates lights of the dash.
    /// </summary>
    private void Lights() {

        //  Return if no lights found.
        if (!CarController.Lights)
            return;

        //  Updating all lights.
        for (int i = 0; i < interiorLights.Length; i++)
            interiorLights[i].Update(CarController.Lights.lowBeamHeadlights);

    }

    public void Reload() {



    }

    private void OnValidate() {

        if (interiorLights != null && interiorLights.Length >= 1) {

            for (int i = 0; i < interiorLights.Length; i++) {

                if (interiorLights[i].intensity < .01f)
                    interiorLights[i].intensity = 1f;

            }

        }

    }

}
