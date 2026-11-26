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
/// Fake wheel blur textures.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Other Addons/RCCP Wheel Blur")]
public class RCCP_WheelBlur : RCCP_Component {

    /// <summary>
    /// Wheel blur renderers with proper material. We'll be using the shaders properties.
    /// </summary>
    private Wheel[] wheelBlurRenderers;

    [System.Serializable]
    public class Wheel {

        public MeshRenderer blurredWheel;
        public RCCP_WheelCollider wheelCollider;
        public float wheelRPM;

    }

    /// <summary>
    /// Target material with proper shader named "RCCP_Wheel_Blur".
    /// </summary>
    public Material targetMaterial;

    /// <summary>
    /// Offset of the texture.
    /// </summary>
    public Vector3 offset = new Vector3(.1f, 0f, 0f);

    /// <summary>
    /// Scale of the texture.
    /// </summary>
    [Range(0f, .2f)] public float scale = .06f;

    /// <summary>
    /// Rotation speed of the texture.
    /// </summary>
    [Range(0f, 5f)] public float rotationSpeed = .25f;

    /// <summary>
    /// Current blur intensity;
    /// </summary>
    private float[] blurIntensity;

    /// <summary>
    /// Smoothness of the intensity.
    /// </summary>
    public float smoothness = 20f;

    public override void Start() {

        base.Start();

        //  Creating all renderers.
        CreateRenderers();

    }

    /// <summary>
    /// Destroying all renderers.
    /// </summary>
    public void DestroyRenderers() {

        foreach (Transform item in transform) {

            if (item != transform)
                Destroy(item.gameObject);

        }

    }

    /// <summary>
    /// Destroying all renderers by an editor script.
    /// </summary>
    public void DestroyRenderersEditor() {

        foreach (Transform item in transform) {

            if (item != transform)
                DestroyImmediate(item.gameObject);

        }

    }

    /// <summary>
    /// Creating renderers at proper positions and rotations.
    /// </summary>
    public void CreateRenderers() {

        //  Destroy if there are any renderers attached to this.
        DestroyRenderers();

        //  Getting all wheelcolliders belongs to the vehicle.
        RCCP_WheelCollider[] wheelColliders = CarController.GetComponentsInChildren<RCCP_WheelCollider>();

        //  If no wheelcolliders found, return.
        if (wheelColliders == null)
            return;

        //  If no wheelcolliders found, return.
        if (wheelColliders.Length < 1)
            return;

        //  Creating a new array for renderers.
        wheelBlurRenderers = new Wheel[wheelColliders.Length];

        //  Creating a new array for blur intensity values.
        blurIntensity = new float[wheelBlurRenderers.Length];

        //  Looping the renderers...
        for (int i = 0; i < wheelColliders.Length; i++) {

            //  Assigning new.
            wheelBlurRenderers[i] = new Wheel();

            //  Instantiating new renderer. Reference will be taken from the RCCP_Settings.
            GameObject instantiatedRenderer = Instantiate(RCCPSettings.wheelBlur, transform, false);

            //  Assigning position and rotation.
            instantiatedRenderer.transform.localPosition = Vector3.zero;
            instantiatedRenderer.transform.localRotation = Quaternion.identity;

            //  Getting mesh renderer of the gameobject and assigning the wheelcollider field.
            wheelBlurRenderers[i].blurredWheel = instantiatedRenderer.GetComponentInChildren<MeshRenderer>();
            wheelBlurRenderers[i].wheelCollider = wheelColliders[i];

            //  If target material is selected, use it.
            if (targetMaterial)
                wheelBlurRenderers[i].blurredWheel.material = targetMaterial;

        }

    }

    private void Update() {

        //  If no wheelcolliders found, return.
        if (wheelBlurRenderers == null)
            return;

        //  If no wheelcolliders found, return.
        if (wheelBlurRenderers.Length < 1)
            return;

        //  Looping the renderers.
        for (int i = 0; i < wheelBlurRenderers.Length; i++) {

            //  If renderer is not null, texture and wheelcollider has been assigned...
            if (wheelBlurRenderers[i] != null && wheelBlurRenderers[i].blurredWheel != null && wheelBlurRenderers[i].wheelCollider != null) {

                //  Rotating the texture.
                wheelBlurRenderers[i].wheelRPM += (wheelBlurRenderers[i].wheelCollider.WheelCollider.rpm * (360f / 60f)) / 5f * Time.deltaTime;

                blurIntensity[i] = Mathf.Lerp(blurIntensity[i], Mathf.Abs(wheelBlurRenderers[i].wheelCollider.WheelCollider.rpm / 2000f), Time.deltaTime * smoothness);

                //  Setting "_BlurIntensity" value of the shader related to the wheel rpm.
                wheelBlurRenderers[i].blurredWheel.material.SetFloat("_BlurIntensity", blurIntensity[i]);

                //  Assigning position and rotation of the texture.
                wheelBlurRenderers[i].blurredWheel.transform.parent.position = wheelBlurRenderers[i].wheelCollider.wheelModel.position;
                wheelBlurRenderers[i].blurredWheel.transform.parent.rotation = CarController.transform.rotation * Quaternion.Euler(wheelBlurRenderers[i].wheelRPM * rotationSpeed, wheelBlurRenderers[i].wheelCollider.WheelCollider.steerAngle, 0f);

                //  If texture is at right side of the vehicle, + offset. Otherwise - offset.
                if (wheelBlurRenderers[i].blurredWheel.transform.parent.localPosition.x > 0) {

                    wheelBlurRenderers[i].blurredWheel.transform.parent.localPosition += offset;
                    wheelBlurRenderers[i].blurredWheel.transform.parent.RotateAround(wheelBlurRenderers[i].blurredWheel.transform.parent.position, CarController.transform.forward, wheelBlurRenderers[i].wheelCollider.camber);
                    wheelBlurRenderers[i].blurredWheel.transform.localRotation = Quaternion.Euler(0f, 180f, 90f);

                } else {

                    wheelBlurRenderers[i].blurredWheel.transform.parent.localPosition -= offset;
                    wheelBlurRenderers[i].blurredWheel.transform.parent.RotateAround(wheelBlurRenderers[i].blurredWheel.transform.parent.position, CarController.transform.forward, -wheelBlurRenderers[i].wheelCollider.camber);
                    wheelBlurRenderers[i].blurredWheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

                }

                //  Setting scale of the texture.
                wheelBlurRenderers[i].blurredWheel.transform.localScale = new Vector3(scale, scale, scale);

            }

        }

    }

    public void Reload() {

        //  If no wheelcolliders found, return.
        if (wheelBlurRenderers == null)
            return;

        //  If no wheelcolliders found, return.
        if (wheelBlurRenderers.Length < 1)
            return;

        //  Looping the renderers.
        for (int i = 0; i < wheelBlurRenderers.Length; i++) {

            //  If renderer is not null, texture and wheelcollider has been assigned...
            if (wheelBlurRenderers[i] != null && wheelBlurRenderers[i].blurredWheel != null && wheelBlurRenderers[i].wheelCollider != null) {

                //  Rotating the texture.
                wheelBlurRenderers[i].wheelRPM = 0f;

                //  Setting "_BlurIntensity" value of the shader related to the wheel rpm.
                wheelBlurRenderers[i].blurredWheel.material.SetFloat("_BlurIntensity", 0f);

                //  Assigning position and rotation of the texture.
                wheelBlurRenderers[i].blurredWheel.transform.parent.position = wheelBlurRenderers[i].wheelCollider.wheelModel.position;
                wheelBlurRenderers[i].blurredWheel.transform.parent.rotation = CarController.transform.rotation * Quaternion.Euler(wheelBlurRenderers[i].wheelRPM * rotationSpeed, wheelBlurRenderers[i].wheelCollider.WheelCollider.steerAngle, 0f);

                //  If texture is at right side of the vehicle, + offset. Otherwise - offset.
                if (wheelBlurRenderers[i].blurredWheel.transform.parent.localPosition.x > 0) {

                    wheelBlurRenderers[i].blurredWheel.transform.parent.localPosition += offset;
                    wheelBlurRenderers[i].blurredWheel.transform.parent.RotateAround(wheelBlurRenderers[i].blurredWheel.transform.parent.position, CarController.transform.forward, wheelBlurRenderers[i].wheelCollider.camber);
                    wheelBlurRenderers[i].blurredWheel.transform.localRotation = Quaternion.Euler(0f, 180f, 90f);

                } else {

                    wheelBlurRenderers[i].blurredWheel.transform.parent.localPosition -= offset;
                    wheelBlurRenderers[i].blurredWheel.transform.parent.RotateAround(wheelBlurRenderers[i].blurredWheel.transform.parent.position, CarController.transform.forward, -wheelBlurRenderers[i].wheelCollider.camber);
                    wheelBlurRenderers[i].blurredWheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

                }

                //  Setting scale of the texture.
                wheelBlurRenderers[i].blurredWheel.transform.localScale = new Vector3(scale, scale, scale);

            }

        }

    }

    /// <summary>
    /// Toggles on / off the textures. Used on editor for preview purposes.
    /// </summary>
    public void Toggle() {

        //  If no wheelcolliders found, return.
        if (wheelBlurRenderers == null)
            return;

        //  If no wheelcolliders found, return.
        if (wheelBlurRenderers.Length < 1)
            return;

        //  Looping the renderers...
        for (int i = 0; i < wheelBlurRenderers.Length; i++) {

            //  If renderer is not null, texture and wheelcollider has been assigned...
            if (wheelBlurRenderers[i] != null && wheelBlurRenderers[i].blurredWheel != null && wheelBlurRenderers[i].wheelCollider != null) {

                //  Assigning position and rotation of the texture.
                wheelBlurRenderers[i].blurredWheel.transform.parent.position = RCCP_GetBounds.GetBoundsCenter(wheelBlurRenderers[i].wheelCollider.wheelModel);
                wheelBlurRenderers[i].blurredWheel.transform.parent.rotation = CarController.transform.rotation * Quaternion.Euler(wheelBlurRenderers[i].wheelRPM, wheelBlurRenderers[i].wheelCollider.WheelCollider.steerAngle, 0f);

                //  If texture is at right side of the vehicle, + offset. Otherwise - offset.
                if (wheelBlurRenderers[i].blurredWheel.transform.parent.localPosition.x > 0) {

                    wheelBlurRenderers[i].blurredWheel.transform.parent.localPosition += offset;
                    wheelBlurRenderers[i].blurredWheel.transform.parent.RotateAround(wheelBlurRenderers[i].blurredWheel.transform.parent.position, CarController.transform.forward, wheelBlurRenderers[i].wheelCollider.camber);
                    wheelBlurRenderers[i].blurredWheel.transform.localRotation = Quaternion.Euler(0f, 180f, 90f);

                } else {

                    wheelBlurRenderers[i].blurredWheel.transform.parent.localPosition -= offset;
                    wheelBlurRenderers[i].blurredWheel.transform.parent.RotateAround(wheelBlurRenderers[i].blurredWheel.transform.parent.position, CarController.transform.forward, -wheelBlurRenderers[i].wheelCollider.camber);
                    wheelBlurRenderers[i].blurredWheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

                }

                //  Setting scale of the texture.
                wheelBlurRenderers[i].blurredWheel.transform.localScale = new Vector3(scale, scale, scale);

            }

        }

    }

}
