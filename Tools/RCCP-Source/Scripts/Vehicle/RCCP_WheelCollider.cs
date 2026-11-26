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
/// Wheelcolliders.
/// </summary>
[RequireComponent(typeof(WheelCollider))]
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Drivetrain/RCCP WheelCollider")]
public class RCCP_WheelCollider : RCCP_Component {

    /// <summary>
    /// Actual wheelcollider component.
    /// </summary>
    private WheelCollider _wheelCollider;
    public WheelCollider WheelCollider {

        get {

            if (_wheelCollider == null)
                _wheelCollider = GetComponent<WheelCollider>();

            return _wheelCollider;

        }

    }

    /// <summary>
    /// This wheel is connected to this axle.
    /// </summary>
    public RCCP_Axle connectedAxle;

    /// <summary>
    /// Wheelhit, what wheel is actualy hitting.
    /// </summary>
    public WheelHit wheelHit;

    /// <summary>
    /// Align wheel models.
    /// </summary>
    public bool alignWheels = true;

    /// <summary>
    /// Is this wheel grounded?
    /// </summary>
    [Space()] public bool isGrounded = false;

    /// <summary>
    /// Is this wheel skidding?
    /// </summary>
    public bool isSkidding = false;

    /// <summary>
    /// If there is a wheelhit, get corresponding ground material index.
    /// </summary>
    [Min(0)] public int groundIndex = 0;

    /// <summary>
    /// Torques.
    /// </summary>
    [Space()] public float motorTorque = 0f;

    /// <summary>
    /// Torques.
    /// </summary>
    public float brakeTorque = 0f;

    /// <summary>
    /// Steer input.
    /// </summary>
    public float steerInput = 0f;

    /// <summary>
    /// Handbrake input.
    /// </summary>
    [Min(0f)] public float handbrakeInput = 0f;

    /// <summary>
    /// Wheel model.
    /// </summary>
    [Space()] public Transform wheelModel;

    /// <summary>
    /// Width of the wheel used to draw skidmarks.
    /// </summary>
    [Space()] [Min(.1f)] public float width = .25f;

    /// <summary>
    /// Total wheel rotation.
    /// </summary>
    private float wheelRotation = 0f;

    /// <summary>
    /// Camber, caster, and offets.
    /// </summary>
    public float camber, caster, offset = 0f;

    /// <summary>
    /// Total slipage of the forward and sideways.
    /// </summary>
    [Space()] public float totalSlip = 0f;

    /// <summary>
    /// Forward and sideways slip.
    /// </summary>
    public float wheelSlipAmountSideways, wheelSlipAmountForward = 0f;

    /// <summary>
    /// Bump force compared to the old bump force.
    /// </summary>
    [HideInInspector] public float bumpForce, oldForce = 0f;

    /// <summary>
    /// Is this wheel can draw skidmarks?
    /// </summary>
    [Space()] public bool drawSkid = true;

    /// <summary>
    /// When did this wheel draw a skidmark? Index of the last mark.
    /// </summary>
    private int lastSkidmark = -1;

    /// <summary>
    /// //	Forward friction curve.
    /// </summary>
    private WheelFrictionCurve forwardFrictionCurve;

    /// <summary>
    /// Sideways friction curve.
    /// </summary>
    private WheelFrictionCurve sidewaysFrictionCurve;

    /// <summary>
    /// Forward friction curve.
    /// </summary>
    private WheelFrictionCurve forwardFrictionCurve_Def;

    /// <summary>
    /// Sideways friction curve.
    /// </summary>
    private WheelFrictionCurve sidewaysFrictionCurve_Def;

    /// <summary>
    /// Audiosource for skidding.
    /// </summary>
    private AudioSource skidAudioSource;

    /// <summary>
    /// Audiclip for skidding.
    /// </summary>
    private AudioClip skidClip;

    /// <summary>
    /// Maximum volume for tire skid SFX.
    /// </summary>
    [Min(0f)] private float skidVolume = 0f;

    /// <summary>
    /// Feeded by RCCP_Stability. Used to cut the traction torque and applying brake torque.
    /// </summary>
    private float cutTractionESP = 0f;

    /// <summary>
    /// Feeded by RCCP_Stability. Used to cut the traction torque.
    /// </summary>
    private float cutTractionTCS = 0f;

    /// <summary>
    /// Feeded by RCCP_Stability. Used to cut the brake torque.
    /// </summary>
    [Min(0f)] private float cutBrakeABS = 0f;

    /// <summary>
    /// Is this wheel deflated now?
    /// </summary>
    [Space()] public bool deflated = false;

    /// <summary>
    /// Multiplier will be applied to the radius when deflated.
    /// </summary>
    [Min(0f)] public float deflatedRadiusMultiplier = .8f;

    /// <summary>
    /// Multiplier will be applied to the stiffness when deflated.
    /// </summary>
    [Min(0f)] public float deflatedStiffnessMultiplier = .5f;

    /// <summary>
    /// Current radius for deflated or not.
    /// </summary>
    [Min(0f)] private float defRadius = -1f;

    /// <summary>
    ///  Drift mode. When enabled, "Drift()" method will be in use to adjust forward and sideways frictions.
    /// </summary>
    [Space()] public bool driftMode = false;

    /// <summary>
    /// Minimum and maximum stiffness for drift. Drift only.
    /// </summary>
    private readonly float minForwardStiffnessForDrift = .75f;

    /// <summary>
    /// Minimum and maximum stiffness for drift. Drift only.
    /// </summary>
    private readonly float maxForwardStiffnessForDrift = 1.25f;

    /// <summary>
    /// Minimum and maximum stiffness for drift. Drift only.
    /// </summary>
    private readonly float minSidewaysStiffnessForDrift = .45f;

    /// <summary>
    /// Minimum and maximum stiffness for drift. Drift only.
    /// </summary>
    private readonly float maxSidewaysStiffnessForDrift = 1f;

    float sqrVel;

    public override void Awake() {

        base.Awake();

        if (wheelModel == null) {

            Debug.LogError("Wheel model is not selected for " + transform.name + ". Disabling this wheelcollider.");
            enabled = false;
            return;

        }

    }

    public override void Start() {

        base.Start();

        //  Increasing mass of the wheel for more stable handling. Normally it should be 20 - 30, but using these values will
        //  bring you more unstable and hard to control wheels. Increasing the mass of them based on vehicle mass.
        if (RCCPSettings.useFixedWheelColliders) {

            WheelCollider.mass = CarController.Rigid.mass / 25f;
            WheelCollider.ConfigureVehicleSubsteps(5f, 9, 6);

        }

        //  Getting fiction curves from the wheels.
        forwardFrictionCurve = WheelCollider.forwardFriction;
        sidewaysFrictionCurve = WheelCollider.sidewaysFriction;

        //  Getting default friction curves from the wheels.
        forwardFrictionCurve_Def = forwardFrictionCurve;
        sidewaysFrictionCurve_Def = sidewaysFrictionCurve;

        // Creating audiosource for skid SFX.
        skidAudioSource = RCCP_AudioSource.NewAudioSource(CarController.gameObject, "Skid Sound AudioSource", 5f, 50f, 0f, null, true, true, false);

        //	Creating pivot position of the wheel at correct position and rotation.
        GameObject newPivot = new GameObject("Pivot_" + wheelModel.transform.name);

        newPivot.transform.SetPositionAndRotation(RCCP_GetBounds.GetBoundsCenter(wheelModel.transform), transform.rotation);
        newPivot.transform.SetParent(wheelModel.transform.parent, true);

        //	Assigning temporary created wheel to actual wheel.
        wheelModel.SetParent(newPivot.transform, true);
        wheelModel = newPivot.transform;

    }

    private void Update() {

        if (alignWheels)
            WheelAlign();

    }

    private void FixedUpdate() {

        //  If wheelcollider is not enabled yet, return.
        if (!WheelCollider.enabled)
            return;

        MotorTorque();
        BrakeTorque();
        GroundMaterial();
        Frictions();
        SkidMarks();
        Audio();

    }

    /// <summary>
    /// Aligning wheel model position and rotation.
    /// </summary>
    private void WheelAlign() {

        // Return if no wheel model selected.
        if (!wheelModel)
            return;

        //  If wheelcollider is not enabled, deactive the wheel model. Otherwise, activate it.
        wheelModel.gameObject.SetActive(WheelCollider.enabled);

        //  If wheelcollider is not enabled yet, return.
        if (!WheelCollider.enabled)
            return;

        //  Positions and rotations of the wheel.
        Vector3 wheelPosition;
        Quaternion wheelRotation;

        //  Getting position and rotation pose of the wheel.
        WheelCollider.GetWorldPose(out wheelPosition, out wheelRotation);

        //Increase the rotation value.
        this.wheelRotation += WheelCollider.rpm * (360f / 60f) * Time.deltaTime;

        //	Assigning position and rotation to the wheel model.
        wheelModel.transform.SetPositionAndRotation(wheelPosition, transform.rotation * Quaternion.Euler(this.wheelRotation, WheelCollider.steerAngle, 0f));

        //	Adjusting offset by X axis.
        if (transform.localPosition.x < 0f)
            wheelModel.transform.position += (transform.right * offset);
        else
            wheelModel.transform.position -= (transform.right * offset);

        // Adjusting camber angle by Z axis.
        if (transform.localPosition.x < 0f)
            wheelModel.transform.RotateAround(wheelModel.transform.position, transform.forward, -camber);
        else
            wheelModel.transform.RotateAround(wheelModel.transform.position, transform.forward, camber);

        // Adjusting caster angle by X axis.
        if (transform.localPosition.x < 0f)
            wheelModel.transform.RotateAround(wheelModel.transform.position, transform.right, -caster);
        else
            wheelModel.transform.RotateAround(wheelModel.transform.position, transform.right, caster);

    }

    /// <summary>
    /// Converts to splat map coordinate.
    /// </summary>
    /// <returns>The to splat map coordinate.</returns>
    /// <param name="playerPos">Player position.</param>
    private Vector3 ConvertToSplatMapCoordinate(Terrain terrain, Vector3 playerPos) {

        Vector3 vecRet = new Vector3();
        Vector3 terPosition = terrain.transform.position;
        vecRet.x = ((playerPos.x - terPosition.x) / terrain.terrainData.size.x) * terrain.terrainData.alphamapWidth;
        vecRet.z = ((playerPos.z - terPosition.z) / terrain.terrainData.size.z) * terrain.terrainData.alphamapHeight;
        return vecRet;

    }

    /// <summary>
    /// Gets the index of the ground material.
    /// </summary>
    /// <returns>The ground material index.</returns>
    private void GroundMaterial() {

        isGrounded = WheelCollider.GetGroundHit(out wheelHit);

        //  If there are no any contact points, return with 0 index.
        if (!isGrounded || wheelHit.point == Vector3.zero || wheelHit.collider == null) {

            groundIndex = 0;
            return;

        }

        // Contacted any physic material in Configurable Ground Materials yet?
        bool contactedWithAnyMaterialYet = false;

        //  Checking the material of the contact point in the RCCP_GroundMaterials ground frictions.
        for (int i = 0; i < RCCPGroundMaterials.frictions.Length; i++) {

            //  If there is one, assign the index of the material. And set it to true.
            if (wheelHit.collider.sharedMaterial == RCCPGroundMaterials.frictions[i].groundMaterial) {

                contactedWithAnyMaterialYet = true;
                groundIndex = i;

            }

        }

        // If ground pyhsic material is not one of the ground material in Configurable Ground Materials, check if we are on terrain collider...
        if (!contactedWithAnyMaterialYet) {

            //  If terrains are not initialized yet, return.
            if (!RCCP_SceneManager.Instance.terrainsInitialized) {

                groundIndex = 0;
                return;

            }

            //  Checking the material of the contact point in the RCCP_GroundMaterials terrain frictions.
            for (int i = 0; i < RCCPGroundMaterials.terrainFrictions.Length; i++) {

                //  If there is one in the terrain frictions...
                if (wheelHit.collider.sharedMaterial == RCCPGroundMaterials.terrainFrictions[i].groundMaterial) {

                    RCCP_SceneManager.Terrains currentTerrain = null;

                    for (int l = 0; l < RCCP_SceneManager.Instance.terrains.Length; l++) {

                        //  Find the corresponding index by the terrain collider and ground material collier.
                        if (RCCP_SceneManager.Instance.terrains[l].terrainCollider == RCCPGroundMaterials.terrainFrictions[i].groundMaterial) {

                            //  This is the terrain where wheel is hitting.
                            currentTerrain = RCCP_SceneManager.Instance.terrains[l];
                            break;

                        }

                    }

                    //  Once we have that terrain, get exact position in the terrain map coordinate.
                    if (currentTerrain != null) {

                        Vector3 playerPos = transform.position;
                        Vector3 TerrainCord = ConvertToSplatMapCoordinate(currentTerrain.terrain, playerPos);
                        float comp = 0f;

                        //  Finding the right terrain texture around the hit position.
                        for (int k = 0; k < currentTerrain.mNumTextures; k++) {

                            if (comp < currentTerrain.mSplatmapData[(int)TerrainCord.z, (int)TerrainCord.x, k])
                                groundIndex = k;

                        }

                        //  Assign the index of the material.
                        groundIndex = RCCPGroundMaterials.terrainFrictions[i].splatmapIndexes[groundIndex].index;

                    }

                }

            }

        }

    }

    /// <summary>
    /// Skidmarks.
    /// </summary>
    private void SkidMarks() {

        //  If drawing skids are not enabled, return.
        if (!drawSkid)
            return;

        // If slips are bigger than target value...
        if (totalSlip > RCCPGroundMaterials.frictions[groundIndex].slip) {

            //  Getting the exact skid point based on wheel hit and little bit advantage of the vehicle velocity.
            Vector3 skidPoint = wheelHit.point + (CarController.Rigid.velocity * Time.deltaTime);

            //  If vehicle velocity is not 0, and we have a wheelhit, add skidmarks. And increase the lastSkidmark counter. Otherwise set it to -1.
            if (CarController.Rigid.velocity.magnitude > .1f && isGrounded && wheelHit.normal != Vector3.zero && wheelHit.point != Vector3.zero && skidPoint != Vector3.zero && Mathf.Abs(skidPoint.magnitude) >= .1f)
                lastSkidmark = RCCP_SkidmarksManager.Instance.AddSkidMark(skidPoint, wheelHit.normal, totalSlip - RCCPGroundMaterials.frictions[groundIndex].slip, width, lastSkidmark, groundIndex);
            else
                lastSkidmark = -1;

        } else {

            //  That means, slips are not bigger than target value. Setting lastSkidmarks to -1.
            lastSkidmark = -1;

        }

    }

    private void MotorTorque() {

        float torque = motorTorque;

        bool positiveTorque = true;

        if (torque < -1)
            positiveTorque = false;

        if (cutTractionESP != 0f) {

            torque -= Mathf.Clamp(torque * (Mathf.Abs(wheelSlipAmountSideways) * cutTractionESP), 0f, Mathf.Infinity);
            torque = Mathf.Clamp(torque, 0f, Mathf.Infinity);

        }

        if (cutTractionTCS != 0f && Mathf.Abs(wheelSlipAmountForward) > .05f) {

            if (Mathf.Sign(WheelCollider.rpm) >= 0) {

                torque -= Mathf.Clamp(torque * (Mathf.Abs(wheelSlipAmountForward) * cutTractionTCS), 0f, Mathf.Infinity);
                torque = Mathf.Clamp(torque, 0f, Mathf.Infinity);

            } else {

                torque += Mathf.Clamp(-torque * (Mathf.Abs(wheelSlipAmountForward) * cutTractionTCS), 0f, Mathf.Infinity);
                torque = Mathf.Clamp(torque, -Mathf.Infinity, 0f);

            }

        }

        if (positiveTorque)
            torque = Mathf.Clamp(torque, 0f, Mathf.Infinity);
        else
            torque = Mathf.Clamp(torque, -Mathf.Infinity, 0f);

        WheelCollider.motorTorque = torque;

        cutTractionESP = 0f;
        cutTractionTCS = 0f;

        motorTorque = 0f;

    }

    private void BrakeTorque() {

        float torque = brakeTorque;

        if (cutBrakeABS != 0f) {

            torque -= Mathf.Clamp(torque * cutBrakeABS, 0f, Mathf.Infinity);
            torque = Mathf.Clamp(torque, 0f, Mathf.Infinity);

        }

        torque = Mathf.Clamp(torque, 0f, Mathf.Infinity);

        WheelCollider.brakeTorque = torque;

        cutBrakeABS = 0f;

        brakeTorque = 0f;

    }

    /// <summary>
    /// Sets forward and sideways frictions.
    /// </summary>
    private void Frictions() {

        //  If wheel is grounded, get forward and sideways slips. Otherwise, set them to 0.
        if (isGrounded) {

            wheelSlipAmountForward = Mathf.Lerp(wheelSlipAmountForward, wheelHit.forwardSlip, Time.deltaTime * 10f);
            wheelSlipAmountSideways = Mathf.Lerp(wheelSlipAmountSideways, wheelHit.sidewaysSlip, Time.deltaTime * 10f);
            totalSlip = Mathf.Lerp(totalSlip, ((Mathf.Abs(wheelHit.forwardSlip) + Mathf.Abs(wheelHit.sidewaysSlip)) / 2f), Time.deltaTime * 10f);

        } else {

            wheelSlipAmountForward = Mathf.Lerp(wheelSlipAmountForward, 0f, Time.deltaTime * 10f);
            wheelSlipAmountSideways = Mathf.Lerp(wheelSlipAmountSideways, 0f, Time.deltaTime * 10f);
            totalSlip = Mathf.Lerp(totalSlip, 0f, Time.deltaTime * 10f);

        }

        if (totalSlip >= RCCPGroundMaterials.frictions[groundIndex].slip)
            isSkidding = true;
        else
            isSkidding = false;

        // Setting stiffness of the forward and sideways friction curves.
        forwardFrictionCurve.stiffness = RCCPGroundMaterials.frictions[groundIndex].forwardStiffness;
        sidewaysFrictionCurve.stiffness = ((RCCPGroundMaterials.frictions[groundIndex].sidewaysStiffness * (1f - (handbrakeInput / 5f))) * connectedAxle.tractionHelpedSidewaysStiffness);
        handbrakeInput = 0f;

        //  If wheel is deflated, multiple the stiffness with the given multiplier.
        if (deflated) {

            forwardFrictionCurve.stiffness *= deflatedStiffnessMultiplier;
            sidewaysFrictionCurve.stiffness *= deflatedStiffnessMultiplier;

        }

        //  Drift mode.
        if (driftMode)
            Drift();

        // Setting new friction curves to wheels.
        WheelCollider.forwardFriction = forwardFrictionCurve;
        WheelCollider.sidewaysFriction = sidewaysFrictionCurve;

        // Also damp too.
        WheelCollider.wheelDampingRate = RCCPGroundMaterials.frictions[groundIndex].damp;

    }

    /// <summary>
    /// Audio.
    /// </summary>
    private void Audio() {

        // If total slip is high enough...
        if (totalSlip > RCCPGroundMaterials.frictions[groundIndex].slip) {

            // Set audioclip to ground physic material sound.
            skidClip = RCCPGroundMaterials.frictions[groundIndex].groundSound;
            skidVolume = RCCPGroundMaterials.frictions[groundIndex].volume;

            // Assigning corresponding audio clip.
            if (skidAudioSource.clip != skidClip)
                skidAudioSource.clip = skidClip;

            // Playing it.
            if (!skidAudioSource.isPlaying)
                skidAudioSource.Play();

            // If vehicle is moving, set volume and pitch. Otherwise set them to 0.
            if (CarController.Rigid.velocity.magnitude > .1f) {

                skidAudioSource.volume = Mathf.Lerp(0f, skidVolume, totalSlip - RCCPGroundMaterials.frictions[groundIndex].slip);
                skidAudioSource.pitch = Mathf.Lerp(1f, .8f, skidAudioSource.volume);

            } else {

                skidAudioSource.volume = 0f;
                skidAudioSource.pitch = 1f;

            }

        } else {

            skidAudioSource.volume = 0f;
            skidAudioSource.pitch = 1f;

            // If volume is minimal and audio is still playing, stop.
            if (skidAudioSource.volume <= .05f && skidAudioSource.isPlaying)
                skidAudioSource.Stop();

        }

        // Calculating bump force.
        bumpForce = wheelHit.force - oldForce;

        //	If bump force is high enough, play bump SFX.
        if ((bumpForce) >= 5000f) {

            //// Creating and playing audiosource for bump SFX.
            //AudioSource bumpSound = RCCP_NewAudioSource.NewAudioSource(RCCP_Settings.Instance.audioMixer, carController.gameObject, "Bump Sound AudioSource", 5f, 50f, (bumpForce - 5000f) / 3000f, RCCP_Settings.Instance.bumpClip, false, true, true);
            //bumpSound.pitch = Random.Range(.9f, 1.1f);

        }

        oldForce = wheelHit.force;

    }

    /// <summary>
    /// Sets the forward friction curves of the wheel.
    /// </summary>
    /// <param name="extremumSlip"></param>
    /// <param name="extremumValue"></param>
    /// <param name="asymptoteSlip"></param>
    /// <param name="asymptoteValue"></param>
    public void SetFrictionCurvesForward(float extremumSlip, float extremumValue, float asymptoteSlip, float asymptoteValue) {

        WheelFrictionCurve newCurve = new WheelFrictionCurve();
        newCurve.extremumSlip = extremumSlip;
        newCurve.extremumValue = extremumValue;
        newCurve.asymptoteSlip = asymptoteSlip;
        newCurve.asymptoteValue = asymptoteValue;
        forwardFrictionCurve = newCurve;
        forwardFrictionCurve_Def = newCurve;

    }

    /// <summary>
    /// Sets the sideways friction curves of the wheel.
    /// </summary>
    /// <param name="extremumSlip"></param>
    /// <param name="extremumValue"></param>
    /// <param name="asymptoteSlip"></param>
    /// <param name="asymptoteValue"></param>
    public void SetFrictionCurvesSideways(float extremumSlip, float extremumValue, float asymptoteSlip, float asymptoteValue) {

        WheelFrictionCurve newCurve = new WheelFrictionCurve();
        newCurve.extremumSlip = extremumSlip;
        newCurve.extremumValue = extremumValue;
        newCurve.asymptoteSlip = asymptoteSlip;
        newCurve.asymptoteValue = asymptoteValue;
        sidewaysFrictionCurve = newCurve;
        sidewaysFrictionCurve_Def = newCurve;

    }

    /// <summary>
    /// Applies steering angle to the wheel if "isSteering" enabled.
    /// </summary>
    /// <param name="steering"></param>
    public void ApplySteering(float steering) {

        if (!WheelCollider.enabled)
            return;

        //	Ackerman steering formula.
        if (steering > 0f) {

            if (transform.localPosition.x < 0)
                WheelCollider.steerAngle = (Mathf.Deg2Rad * steering * 2.55f) * (Mathf.Rad2Deg * Mathf.Atan(2.55f / (6 + (1.5f / 2))));
            else
                WheelCollider.steerAngle = (Mathf.Deg2Rad * steering * 2.55f) * (Mathf.Rad2Deg * Mathf.Atan(2.55f / (6 - (1.5f / 2))));

        } else if (steering < 0f) {

            if (transform.localPosition.x < 0)
                WheelCollider.steerAngle = (Mathf.Deg2Rad * steering * 2.55f) * (Mathf.Rad2Deg * Mathf.Atan(2.55f / (6 - (1.5f / 2))));
            else
                WheelCollider.steerAngle = (Mathf.Deg2Rad * steering * 2.55f) * (Mathf.Rad2Deg * Mathf.Atan(2.55f / (6 + (1.5f / 2))));

        } else {

            WheelCollider.steerAngle = 0f;

        }

    }

    /// <summary>
    /// Applies motor torque to the wheel as NM.
    /// </summary>
    /// <param name="torque"></param>
    public void AddMotorTorque(float torque) {

        if (!WheelCollider.enabled)
            return;

        motorTorque += torque;

    }

    /// <summary>
    /// Applies brake torque to the wheels as NM.
    /// </summary>
    /// <param name="torque"></param>
    public void AddBrakeTorque(float torque) {

        //  If wheelcollider is not enabled, return.
        if (!WheelCollider.enabled)
            return;

        brakeTorque += torque;

    }

    /// <summary>
    /// Applies handbrake torque to the wheels as NM.
    /// </summary>
    /// <param name="torque"></param>
    public void AddHandbrakeTorque(float torque) {

        //  If wheelcollider is not enabled, return.
        if (!WheelCollider.enabled)
            return;

        brakeTorque += torque;
        handbrakeInput += Mathf.Clamp01(torque / 1000f);

    }

    /// <summary>
    /// Cuts traction torque for the ESP.
    /// </summary>
    /// <param name="_cutTraction"></param>
    public void CutTractionESP(float _cutTraction) {

        //  If wheelcollider is not enabled, return.
        if (!WheelCollider.enabled)
            return;

        cutTractionESP = _cutTraction;

    }

    /// <summary>
    /// Cuts traction torque 
    /// </summary>
    /// <param name="_cutTraction"></param>
    public void CutTractionTCS(float _cutTraction) {

        //  If wheelcollider is not enabled, return.
        if (!WheelCollider.enabled)
            return;

        cutTractionTCS = _cutTraction;

    }

    /// <summary>
    /// Cuts brake torque for the ABS.
    /// </summary>
    /// <param name="_cutBrake"></param>
    public void CutBrakeABS(float _cutBrake) {

        //  If wheelcollider is not enabled, return.
        if (!WheelCollider.enabled)
            return;

        cutBrakeABS = _cutBrake;

    }

    /// <summary>
    /// Deflates the wheel.
    /// </summary>
    public void Deflate() {

        //  If wheelcollider is not enabled, return.
        if (!WheelCollider.enabled)
            return;

        if (deflated)
            return;

        deflated = true;

        if (defRadius == -1)
            defRadius = WheelCollider.radius;

        WheelCollider.radius = defRadius * deflatedRadiusMultiplier;

        CarController.Rigid.AddForceAtPosition(transform.right * UnityEngine.Random.Range(-1f, 1f) * 25f, transform.position, ForceMode.Acceleration);
        CarController.OnWheelDeflated();

    }

    /// <summary>
    /// Inflates the wheel.
    /// </summary>
    public void Inflate() {

        //  If wheelcollider is not enabled, return.
        if (!WheelCollider.enabled)
            return;

        if (!deflated)
            return;

        deflated = false;

        if (defRadius != -1)
            WheelCollider.radius = defRadius;

        CarController.OnWheelInflated();

    }

    /// <summary>
    /// Drift.
    /// </summary>
    private void Drift() {

        Vector3 relativeVelocity = transform.InverseTransformDirection(CarController.Rigid.velocity);
        sqrVel = Mathf.Lerp(sqrVel, (relativeVelocity.x * relativeVelocity.x) / 50f, Time.fixedDeltaTime * 5f);

        if (wheelHit.forwardSlip > 0)
            sqrVel += (Mathf.Abs(wheelHit.forwardSlip) * .5f);

        // Forward
        if (transform.localPosition.z < 0) {

            forwardFrictionCurve.extremumValue = Mathf.Clamp(forwardFrictionCurve_Def.extremumValue - (sqrVel / 1f), minForwardStiffnessForDrift, maxForwardStiffnessForDrift);
            forwardFrictionCurve.asymptoteValue = Mathf.Clamp(forwardFrictionCurve_Def.asymptoteValue + (sqrVel / 1f), minForwardStiffnessForDrift, maxForwardStiffnessForDrift);

        } else {

            forwardFrictionCurve.extremumValue = Mathf.Clamp(forwardFrictionCurve_Def.extremumValue - (sqrVel / .5f), minForwardStiffnessForDrift / 2f, maxForwardStiffnessForDrift);
            forwardFrictionCurve.asymptoteValue = Mathf.Clamp(forwardFrictionCurve_Def.asymptoteValue - (sqrVel / .5f), minForwardStiffnessForDrift / 2f, maxForwardStiffnessForDrift);

        }

        // Sideways
        if (transform.localPosition.z < 0) {

            sidewaysFrictionCurve.extremumValue = Mathf.Clamp(sidewaysFrictionCurve_Def.extremumValue - (sqrVel / 1f), minSidewaysStiffnessForDrift, maxSidewaysStiffnessForDrift);
            sidewaysFrictionCurve.asymptoteValue = Mathf.Clamp(sidewaysFrictionCurve_Def.asymptoteValue - (sqrVel / 1f), minSidewaysStiffnessForDrift, maxSidewaysStiffnessForDrift);

        } else {

            sidewaysFrictionCurve.extremumValue = Mathf.Clamp(sidewaysFrictionCurve_Def.extremumValue - (sqrVel / 1f), minSidewaysStiffnessForDrift, maxSidewaysStiffnessForDrift);
            sidewaysFrictionCurve.asymptoteValue = Mathf.Clamp(sidewaysFrictionCurve_Def.asymptoteValue - (sqrVel / 1f), minSidewaysStiffnessForDrift, maxSidewaysStiffnessForDrift);

        }

    }

    public void Reload() {

        //  Make sure all of them are resetted while enabling / disabling the wheel.
        motorTorque = 0f;
        brakeTorque = 0f;
        steerInput = 0f;
        handbrakeInput = 0f;
        wheelRotation = 0f;
        cutTractionESP = 0f;
        cutTractionTCS = 0f;
        cutBrakeABS = 0f;
        bumpForce = 0f;
        oldForce = 0f;
        wheelRotation = 0f;
        lastSkidmark = -1;
        totalSlip = 0f;
        wheelSlipAmountForward = 0f;
        wheelSlipAmountSideways = 0f;
        skidVolume = 0f;

    }

    private void Reset() {

        WheelCollider wc = GetComponent<WheelCollider>();

        //  Increasing mass of the wheel for more stable handling. Normally it should be 20 - 30, but using these values will
        //  bring you more unstable and hard to control wheels. Increasing the mass of them based on vehicle mass.
        if (RCCPSettings.useFixedWheelColliders)
            wc.mass = GetComponentInParent<RCCP_CarController>(true).Rigid.mass / 25f;

        //  Resetting force app point distance and suspension distances.
        wc.forceAppPointDistance = .15f;
        wc.suspensionDistance = .2f;

        //  Resetting spring force and damper forces.
        JointSpring js = wc.suspensionSpring;
        js.spring = 45000f;
        js.damper = 2500f;
        wc.suspensionSpring = js;

        //  Resetting friction curves.
        WheelFrictionCurve frictionCurveFwd = wc.forwardFriction;
        frictionCurveFwd.extremumSlip = .4f;
        wc.forwardFriction = frictionCurveFwd;

        WheelFrictionCurve frictionCurveSide = wc.sidewaysFriction;
        frictionCurveSide.extremumSlip = .35f;
        wc.sidewaysFriction = frictionCurveSide;

    }

    /// <summary>
    /// Aligns the wheel model with wheelcollider. Used in editor only.
    /// </summary>
    public void AlignWheel() {

        //  If wheelcollider is not enabled, return.
        if (!WheelCollider.enabled)
            return;

        //  If wheel model is not selected, return.
        if (!wheelModel)
            return;

        //  Assigning position, rotation, and radius of the wheelcollider by taking bounds of the wheel model.
        transform.position = RCCP_GetBounds.GetBoundsCenter(wheelModel);
        transform.position += transform.up * (WheelCollider.suspensionDistance * (transform.root.localScale.y * (1f - WheelCollider.suspensionSpring.targetPosition)));
        WheelCollider.radius = RCCP_GetBounds.MaxBoundsExtent(wheelModel) / transform.root.localScale.y;

    }

}
