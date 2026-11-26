//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright © 2014 - 2024 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Main RCCP Camera controller. Includes 6 different camera modes with many customizable settings. It doesn't use different cameras on your scene like *other* assets. Simply it parents the camera to their positions that's all. No need to be Einstein.
/// Also supports collision detection.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Camera/RCCP Camera")]
public class RCCP_Camera : RCCP_GenericComponent {

    /// <summary>
    /// Camera target with custom class.
    /// </summary>
    [System.Serializable]
    public class CameraTarget {

        public RCCP_CarController playerVehicle;

        private RCCP_HoodCamera _hoodCamera;
        public RCCP_HoodCamera HoodCamera {

            get {

                if (!playerVehicle)
                    return null;

                if (!_hoodCamera)
                    _hoodCamera = playerVehicle.GetComponentInChildren<RCCP_HoodCamera>();

                return _hoodCamera;

            }

        }

        private RCCP_WheelCamera _wheelCamera;
        public RCCP_WheelCamera WheelCamera {

            get {

                if (!playerVehicle)
                    return null;

                if (!_wheelCamera)
                    _wheelCamera = playerVehicle.GetComponentInChildren<RCCP_WheelCamera>();

                return _wheelCamera;

            }

        }

    }

    /// <summary>
    /// Actual camera target with custom class.
    /// </summary>
    public CameraTarget cameraTarget = new CameraTarget();

    /// <summary>
    /// Currently rendering?
    /// </summary>
    public bool isRendering = true;

    /// <summary>
    /// Camera is not attached to this main gameobject. Camera is parented to pivot gameobject. Therefore, we can apply additional position and rotation changes.
    /// </summary>
    public Camera actualCamera;

    /// <summary>
    /// Pivot center of the camera. Used for making offsets and collision movements.
    /// </summary>
    public GameObject pivot;

    /// <summary>
    /// Received inputs from the player.
    /// </summary>
    public RCCP_Inputs inputs;

    /// <summary>
    /// Camera Modes.
    /// </summary>
    public enum CameraMode { TPS, FPS, WHEEL, FIXED, CINEMATIC, TOP }

    /// <summary>
    /// Camera Modes.
    /// </summary>
    public CameraMode cameraMode = CameraMode.TPS;

    /// <summary>
    /// Last camera mode before this frame, to check when camera changed or not.
    /// </summary>
    private CameraMode lastCameraMode = CameraMode.TPS;

    /// <summary>
    /// Uses TPS2 function instead of TPS. It's more stable.
    /// </summary>
    public enum TPSMode { TPS1, TPS2 }
    public TPSMode tPSMode = TPSMode.TPS2;

    /// <summary>
    /// Additional camera systems.
    /// </summary>
    private RCCP_FixedCamera FixedCamera { get { return RCCP_FixedCamera.Instance; } }

    /// <summary>
    /// Additional camera systems.
    /// </summary>
    private RCCP_CinematicCamera CinematicCamera { get { return RCCP_CinematicCamera.Instance; } }

    /// <summary>
    /// Locks X angle to vehicle's X.
    /// </summary>
    public bool TPSLockX = true;

    /// <summary>
    /// Locks Y angle to vehicle's Y.
    /// </summary>
    public bool TPSLockY = true;

    /// <summary>
    /// Locks Z angle to vehicle's Z.
    /// </summary>
    public bool TPSLockZ = false;

    /// <summary>
    /// Camera rotation won't track vehicle if it's not grounded.
    /// </summary>
    public bool TPSFreeFall = true;

    /// <summary>
    /// Use dynamic distance, height, and pitch angle related to vehicle rigidbody velocity.
    /// </summary>
    public bool TPSDynamic = false;

    /// <summary>
    /// Shall we use top camera mode?
    /// </summary>
    public bool useTopCameraMode = false;

    /// <summary>
    /// Shall we use hood camera mode?
    /// </summary>
    public bool useHoodCameraMode = true;

    /// <summary>
    /// Shall we use orbit control in TPS camera mode?
    /// </summary>
    public bool useOrbitInTPSCameraMode = true;

    /// <summary>
    /// Shall we use orbit control in hood camera mode?
    /// </summary>
    public bool useOrbitInHoodCameraMode = true;

    /// <summary>
    /// Shall we use orbit control while pressing mouse button / touch.
    /// </summary>
    public bool useOrbitOnlyHolding = true;

    /// <summary>
    /// Currently holding for orbit?
    /// </summary>
    public bool orbitHolding = false;

    /// <summary>
    /// Shall we use wheel camera mode?
    /// </summary>
    public bool useWheelCameraMode = true;

    /// <summary>
    /// Shall we use fixed camera mode?
    /// </summary>
    public bool useFixedCameraMode = true;

    /// <summary>
    /// Shall we use cinematic camera mode?
    /// </summary>
    public bool useCinematicCameraMode = true;

    /// <summary>
    /// Shall we use ortho in top camera mode?
    /// </summary>
    public bool useOrthoForTopCamera = false;

    /// <summary>
    /// Shall we use camera occlusion?
    /// </summary>
    public bool useOcclusion = true;

    /// <summary>
    /// Camera will be ocluded by these layers.
    /// </summary>
    public LayerMask occlusionLayerMask = -1;

    /// <summary>
    /// Currently occluding?
    /// </summary>
    private bool occluded = false;

    /// <summary>
    /// Shall we change camera mode by auto? (For cinematics)
    /// </summary>
    public bool useAutoChangeCamera = false;

    /// <summary>
    /// Timer for changing the camera mode auto.
    /// </summary>
    public float autoChangeCameraTimer = 0f;

    /// <summary>
    /// We will use this Vector3 angle for top camera mode if it's enabled.
    /// </summary>
    public Vector3 topCameraAngle = new Vector3(45f, 45f, 0f);

    /// <summary>
    /// Top camera height / distance.
    /// </summary>
    public float topCameraDistance = 100f;

    /// <summary>
    /// Distance offset for top camera mode. Related with vehicle speed. If vehicle speed is higher, camera will move to front of the vehicle.
    /// </summary>
    [Min(0f)] public float maximumZDistanceOffset = 10f;
    [Min(0f)] private float topCameraDistanceOffset = 0f;

    /// <summary>
    /// Target position.
    /// </summary>
    private Vector3 targetPosition = Vector3.zero;

    /// <summary>
    /// Used for resetting orbit values when direction of the vehicle has been changed.
    /// </summary>
    [Min(-1)] private int direction = 1;

    /// <summary>
    /// The distance for TPS camera mode.
    /// </summary>
    [Range(0f, 20f)] public float TPSDistance = 6.5f;

    /// <summary>
    /// The height we want the camera to be above the target for TPS camera mode.
    /// </summary>
    [Range(0f, 10f)] public float TPSHeight = 1.5f;

    /// <summary>
    /// Rotation movement damper.
    /// </summary>
    [Range(0f, 1f)] public float TPSRotationDamping = .5f;

    /// <summary>
    /// Maximum tilt angle related with rigidbody local velocity.
    /// </summary>
    [Range(0f, 25f)] public float TPSTiltMaximum = 15f;

    /// <summary>
    /// Tilt angle multiplier.
    /// </summary>
    [Range(0f, 1.5f)] public float TPSTiltMultiplier = 1f;

    /// <summary>
    /// Yaw angle.
    /// </summary>
    [Range(-45f, 45f)] public float TPSYaw = 0f;

    /// <summary>
    /// Pitch angle.
    /// </summary>
    [Range(-45f, 45f)] public float TPSPitch = 7.5f;

    /// <summary>
    /// Auto focus to player vehicle. Adjusts distance and height depends on vehicle bounds.
    /// </summary>
    public bool TPSAutoFocus = true;

    /// <summary>
    /// Auto reverse when player vehicle is at reverse gear.
    /// </summary>
    public bool TPSAutoReverse = true;

    /// <summary>
    /// TPS position offset.
    /// </summary>
    public Vector3 TPSOffset = new Vector3(0f, 0f, .2f);

    /// <summary>
    /// Rotation of the camera will be this when game starts.
    /// </summary>
    public Vector3 TPSStartRotation = new Vector3(0f, 0f, 0f);

    /// <summary>
    /// Last rotation of the camera.
    /// </summary>
    private Quaternion TPSLastRotation = Quaternion.identity;

    /// <summary>
    /// Current tilt angle.
    /// </summary>
    private float TPSTiltAngle = 0f;

    /// <summary>
    /// Camera will adapt its field of view to this target field of view. All field of views below this line will feed this value.
    /// </summary>
    [Min(0f)] public float targetFieldOfView = 60f;

    /// <summary>
    /// Minimum field of view related with vehicle speed.
    /// </summary>
    [Range(10f, 90f)] public float TPSMinimumFOV = 40f;

    /// <summary>
    /// Maximum field of view related with vehicle speed.
    /// </summary>
    [Range(10f, 160f)] public float TPSMaximumFOV = 60f;

    /// <summary>
    /// Hood field of view.
    /// </summary>
    [Range(10f, 160f)] public float hoodCameraFOV = 60f;

    /// <summary>
    /// Wheel field of view.
    /// </summary>
    [Range(10f, 160f)] public float wheelCameraFOV = 60f;

    /// <summary>
    /// Minimum ortho size related with vehicle speed.
    /// </summary>
    [Min(0f)] public float minimumOrtSize = 10f;

    /// <summary>
    /// Maximum ortho size related with vehicle speed.
    /// </summary>
    [Min(0f)] public float maximumOrtSize = 20f;

    /// <summary>
    /// Used in switch case for running corresponding camera mode method.
    /// </summary>
    [Min(0)] internal int cameraSwitchCount = 0;

    /// <summary>
    /// Velocity values for MoveTowards.
    /// </summary>
    private float xVelocity, yVelocity, zVelocity = 0f;
    private Vector3 accelerationVelocity = Vector3.zero;

    public Vector3 acceleration = Vector3.zero;
    public Vector3 lastVelocity = Vector3.zero;
    public Vector3 acceleration_Smoothed = Vector3.zero;

    /// <summary>
    /// Collision direction.
    /// </summary>
    private Vector3 collisionDirection = Vector3.zero;

    /// <summary>
    /// Collision position.
    /// </summary>
    private Vector3 collisionPos = Vector3.zero;

    /// <summary>
    /// Collision rotation.
    /// </summary>
    private Quaternion collisionRot = Quaternion.identity;

    [Range(.5f, 10f)] public float zoomScrollMultiplier = 5f;
    private float zoomScroll = 0;
    [Min(0f)] public float minimumScroll = 0f;
    [Min(0f)] public float maximumScroll = 5f;

    /// <summary>
    /// Raw Orbit X and Y inputs.
    /// </summary>
    private float orbitX, orbitY = 0f;

    /// <summary>
    /// Smooth Orbit X and Y inputs.
    /// </summary>
    private float orbitX_Smoothed, orbitY_Smoothed = 0f;

    /// <summary>
    /// Minimum and maximum Orbit X, Y degrees.
    /// </summary>
    public float minOrbitY = -15f;

    /// <summary>
    /// Minimum and maximum Orbit X, Y degrees.
    /// </summary>
    public float maxOrbitY = 70f;

    /// <summary>
    /// Orbit X and Y speeds.
    /// </summary>
    [Min(0f)] public float orbitXSpeed = 100f;

    /// <summary>
    /// Orbit X and Y speeds.
    /// </summary>
    [Min(0f)] public float orbitYSpeed = 100f;

    /// <summary>
    /// Orbit smoothing.
    /// </summary>
    [Min(0f)] public float orbitSmooth = 40f;

    /// <summary>
    /// Orbit while pressing and holding the left mouse button?
    /// </summary>
    public bool orbitWhileHolding = false;

    /// <summary>
    /// Resetting orbits.
    /// </summary>
    public bool orbitReset = true;
    private float orbitResetTimer = 0f;
    private float oldOrbitX, oldOrbitY = 0f;

    /// <summary>
    /// Camera can look back now?
    /// </summary>
    public bool lookBackNow = false;

    private Transform TPSAccelerationPoint;

    private void Awake() {

        // Getting Camera.
        actualCamera = GetComponentInChildren<Camera>();

        //  Creating pivot position of the camera.
        if (!pivot) {

            pivot = transform.Find("Pivot").gameObject;

            if (!pivot)
                pivot = new GameObject("Pivot");

            pivot.transform.SetParent(transform);
            pivot.transform.localPosition = Vector3.zero;
            pivot.transform.localRotation = Quaternion.identity;
            actualCamera.transform.SetParent(pivot.transform, true);

        }

    }

    private void OnEnable() {

        // Calling this event when BCG Camera spawned.
        RCCP_Events.Event_OnRCCPCameraSpawned(this);

        // Listening player vehicle collisions for crashing effects.
        RCCP_InputManager.OnChangedCamera += RCCP_InputManager_OnChangedCamera;
        RCCP_InputManager.OnLookBackCamera += RCCP_InputManager_OnLookBackCamera;
        RCCP_InputManager.OnHoldOrbitCamera += RCCP_InputManager_OnHoldOrbitCamera;

    }

    /// <summary>
    /// Look back or not on TPS mode.
    /// </summary>
    /// <param name="state"></param>
    private void RCCP_InputManager_OnLookBackCamera(bool state) {

        lookBackNow = state;

    }

    /// <summary>
    /// Look back or not on TPS mode.
    /// </summary>
    /// <param name="state"></param>
    private void RCCP_InputManager_OnHoldOrbitCamera(bool state) {

        orbitHolding = state;

    }

    /// <summary>
    /// When player pushes change camera button.
    /// </summary>
    private void RCCP_InputManager_OnChangedCamera() {

        ChangeCamera();

    }

    /// <summary>
    /// Sets target vehicle of the camera.
    /// </summary>
    /// <param name="player"></param>
    public void SetTarget(RCCP_CarController player) {

        //  Setting target vehicle.
        cameraTarget = new CameraTarget {
            playerVehicle = player
        };

        //  If auto focus is enabled, adjust distance and height of the camera automatically.
        if (TPSAutoFocus)
            StartCoroutine(AutoFocus());

        //  And reset the camera modes.
        ResetCamera();

        TPSLastRotation = player.transform.rotation;

    }

    /// <summary>
    /// Removes target vehicle.
    /// </summary>
    public void RemoveTarget() {

        transform.SetParent(null);
        cameraTarget.playerVehicle = null;

    }

    private void Update() {

        // If it's active, enable the camera. If it's not, disable the camera.
        if (!isRendering) {

            if (actualCamera.gameObject.activeSelf)
                actualCamera.gameObject.SetActive(false);

        } else {

            if (!actualCamera.gameObject.activeSelf)
                actualCamera.gameObject.SetActive(true);

        }

        if (!IsCameraActive())
            return;

        // Early out if we don't have the player vehicle.
        if (!cameraTarget.playerVehicle)
            return;

        //  Receive inputs.
        Inputs();

        // Lerping current field of view to target field of view.
        actualCamera.fieldOfView = Mathf.Lerp(actualCamera.fieldOfView, targetFieldOfView, Time.deltaTime * 5f);

        if (TPSDynamic) {

            if (TPSAccelerationPoint == null) {

                GameObject TPSAccelerationPointGO = new GameObject("TPSAccelerationPoint");
                TPSAccelerationPointGO.hideFlags = HideFlags.HideInHierarchy;
                TPSAccelerationPoint = TPSAccelerationPointGO.transform;

            }

            acceleration_Smoothed = Vector3.SmoothDamp(acceleration_Smoothed, acceleration, ref accelerationVelocity, .3f);

            if (cameraTarget.playerVehicle) {

                TPSAccelerationPoint.position = cameraTarget.playerVehicle.transform.position;
                TPSAccelerationPoint.rotation = cameraTarget.playerVehicle.transform.rotation;
                TPSAccelerationPoint.position -= TPSAccelerationPoint.rotation * (acceleration_Smoothed * 2f);

            }

        } else {

            acceleration_Smoothed = Vector3.zero;

        }

    }

    private void LateUpdate() {

        if (!IsCameraActive())
            return;

        // Early out if we don't have the player vehicle.
        if (!cameraTarget.playerVehicle)
            return;

        // Even if we have the player vehicle and it's disabled, return.
        if (!cameraTarget.playerVehicle.gameObject.activeSelf)
            return;

        if (Time.timeScale <= 0)
            return;

        // Run the corresponding method with choosen camera mode.
        switch (cameraMode) {

            case CameraMode.TPS:

                if (useOrbitInTPSCameraMode)
                    ORBIT();

                if (tPSMode == TPSMode.TPS1)
                    TPS();
                else
                    TPS2();

                break;

            case CameraMode.FPS:

                if (useOrbitInHoodCameraMode)
                    ORBIT();

                FPS();

                break;

            case CameraMode.WHEEL:
                WHEEL();
                break;

            case CameraMode.FIXED:
                FIXED();
                break;

            case CameraMode.CINEMATIC:
                CINEMATIC();
                break;

            case CameraMode.TOP:
                TOP();
                break;

        }

        //  If camera mode has been changed in last frame, reset the modes.
        if (lastCameraMode != cameraMode)
            ResetCamera();

        lastCameraMode = cameraMode;

        if (useAutoChangeCamera)
            autoChangeCameraTimer += Time.deltaTime;
        else
            autoChangeCameraTimer = 0f;

        //  If auto change camera is enabled, change the camera mode each 10 seconds.
        if (useAutoChangeCamera && autoChangeCameraTimer >= 10) {

            autoChangeCameraTimer = 0f;
            ChangeCamera();

        }

    }

    private void FixedUpdate() {

        if (!IsCameraActive())
            return;

        // Early out if we don't have the player vehicle.
        if (!cameraTarget.playerVehicle)
            return;

        // Even if we have the player vehicle and it's disabled, return.
        if (!cameraTarget.playerVehicle.gameObject.activeSelf)
            return;

        CheckIfOccluded();

        Vector3 currentSpeed = cameraTarget.playerVehicle.transform.InverseTransformDirection(cameraTarget.playerVehicle.Rigid.velocity);
        acceleration = currentSpeed - lastVelocity;
        lastVelocity = currentSpeed;

        acceleration = Vector3.ClampMagnitude(acceleration, .1f);

        acceleration.x = currentSpeed.x * .04f * (1f - Mathf.InverseLerp(0f, 200f, cameraTarget.playerVehicle.absoluteSpeed));
        acceleration.y = 0f;
        acceleration.z *= 1.75f * (1f - Mathf.InverseLerp(0f, 200f, cameraTarget.playerVehicle.absoluteSpeed));

    }

    /// <summary>
    /// Receiving player inputs for orbiting the camera.
    /// </summary>
    private void Inputs() {

        //  Receiving player inputs
        inputs = RCCP_InputManager.Instance.GetInputs();

        if (!useOrbitOnlyHolding) {
            
            //  Setting orbits.
            orbitX += inputs.mouseInput.x;
            orbitY -= inputs.mouseInput.y;

        }

        // Clamping orbit Y.
        orbitY = Mathf.Clamp(orbitY, minOrbitY, maxOrbitY);

        //  Smoothing orbits.
        orbitX_Smoothed = Mathf.Lerp(orbitX_Smoothed, orbitX, Time.deltaTime * orbitSmooth);
        orbitY_Smoothed = Mathf.Lerp(orbitY_Smoothed, orbitY, Time.deltaTime * orbitSmooth);

    }

    /// <summary>
    /// Change camera by increasing camera switch counter.
    /// </summary>
    public void ChangeCamera() {

        // Increasing camera switch counter at each camera changing.
        cameraSwitchCount++;

        // We have 6 camera modes at total. If camera switch counter is greater than maximum, set it to 0.
        if (cameraSwitchCount >= 6)
            cameraSwitchCount = 0;

        switch (cameraSwitchCount) {

            case 0:
                cameraMode = CameraMode.TPS;
                break;

            case 1:
                if (useHoodCameraMode && cameraTarget.HoodCamera)
                    cameraMode = CameraMode.FPS;
                else
                    ChangeCamera();
                break;

            case 2:
                if (useWheelCameraMode && cameraTarget.WheelCamera)
                    cameraMode = CameraMode.WHEEL;
                else
                    ChangeCamera();
                break;

            case 3:
                if (useFixedCameraMode && FixedCamera)
                    cameraMode = CameraMode.FIXED;
                else
                    ChangeCamera();
                break;

            case 4:
                if (useCinematicCameraMode && CinematicCamera)
                    cameraMode = CameraMode.CINEMATIC;
                else
                    ChangeCamera();
                break;

            case 5:
                if (useTopCameraMode)
                    cameraMode = CameraMode.TOP;
                else
                    ChangeCamera();
                break;

        }

    }

    /// <summary>
    /// Change camera by directly setting it to specific mode.
    /// </summary>
    /// <param name="mode"></param>
    public void ChangeCamera(CameraMode mode) {

        cameraMode = mode;

    }

    /// <summary>
    /// FPS mode.
    /// </summary>
    private void FPS() {

        // Assigning orbit rotation, and transform rotation.
        if (useOrbitInHoodCameraMode)
            transform.rotation = cameraTarget.playerVehicle.transform.rotation * Quaternion.Euler(orbitY_Smoothed, orbitX_Smoothed, 0f);
        else
            transform.rotation = cameraTarget.playerVehicle.transform.rotation;

    }

    /// <summary>
    /// Wheel mode.
    /// </summary>
    private void WHEEL() {

        if (useOcclusion && occluded)
            ChangeCamera(CameraMode.TPS);

    }

    /// <summary>
    /// TPS mode.
    /// </summary>
    private void TPS() {

        //  Setting rotation of the camera.
        transform.rotation = TPSLastRotation;
        TPSLastRotation = transform.rotation;

        // If TPS Auto Reverse is enabled and vehicle is moving backwards, reset X and Y orbits when vehicle direction is changed. Camera will look directly rear side of the vehicle.
        direction = cameraTarget.playerVehicle.direction;

        //	Vehicle direction angle used for back side camera angle. 0 means forwards, 180 means backwards.
        int directionAngle = 0;

        float rotDamp = TPSRotationDamping;

        // Calculate the current rotation angles for TPS mode.
        if (TPSAutoReverse)
            directionAngle = (direction == 1 ? 0 : 180);

        //  Looks back if it's enabled by player input.
        if (lookBackNow) {

            directionAngle = 180;
            rotDamp = 1f;

        }

        //  Make sure camera is still checking rotation of the player vehicle.
        if (TPSFreeFall && Time.time >= 1f) {

            if (!cameraTarget.playerVehicle.IsGrounded)
                rotDamp = -10f;

        }

        //  X angle.
        float xAngle = 0f;

        //  If TPS Lock X is enabled, set X angle.
        if (TPSLockX)
            xAngle = Mathf.SmoothDampAngle(transform.eulerAngles.x, cameraTarget.playerVehicle.transform.eulerAngles.x * (directionAngle == 180 ? -1f : 1f), ref xVelocity, 1f - rotDamp);

        if (useOrbitInTPSCameraMode && orbitY != 0)
            xAngle = orbitY_Smoothed;

        //  Y angle.
        float yAngle = 0f;

        //  If TPS Lock Y is enabled, set Y angle.
        if (TPSLockY) {

            //  If orbit for TPS is enabled, process orbit Y. Otherwise process Y of the player vehicle only.
            if (!useOrbitInTPSCameraMode) {

                yAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, cameraTarget.playerVehicle.transform.eulerAngles.y + directionAngle, ref yVelocity, 1f - rotDamp);

            } else {

                if (orbitX != 0)
                    yAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, cameraTarget.playerVehicle.transform.eulerAngles.y + orbitX_Smoothed, ref yVelocity, .025f);
                else
                    yAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, cameraTarget.playerVehicle.transform.eulerAngles.y + directionAngle, ref yVelocity, 1f - rotDamp);

            }

        } else {

            if (useOrbitInTPSCameraMode && orbitX != 0)
                yAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, orbitX_Smoothed, ref yVelocity, .025f);

        }

        //  Z Angle.
        float zAngle = 0f;

        //  If TPS Lock Z is enabled, set Z angle.
        if (TPSLockZ)
            zAngle = Mathf.SmoothDampAngle(transform.eulerAngles.z, cameraTarget.playerVehicle.transform.eulerAngles.z, ref zVelocity, 1f - rotDamp);

        if (float.IsNaN(yAngle) || float.IsNaN(xAngle) || float.IsNaN(zAngle))
            return;

        //  Adjusting zoom scroll with player inputs.
        //zoomScroll += inputs.scroll.y * zoomScrollMultiplier;
        //zoomScroll = Mathf.Clamp(zoomScroll, minimumScroll, maximumScroll);

        // Position at the target.
        Vector3 position = cameraTarget.playerVehicle.transform.position;

        // Rotation at the target.
        Quaternion rotation = Quaternion.Euler(xAngle, yAngle, zAngle);

        // Then offset by distance behind the new angle.
        position += rotation * (-Vector3.forward * (TPSDistance + zoomScroll));
        position += cameraTarget.playerVehicle.transform.rotation * TPSOffset;
        position += Vector3.up * TPSHeight;

        float addTPSPitch = 0f;

        //  If TPS dynamic is enabled, reduce distance and height related to the vehicle speed.
        if (TPSDynamic && TPSAccelerationPoint != null) {

            transform.position -= (cameraTarget.playerVehicle.transform.position - TPSAccelerationPoint.position) * Time.fixedDeltaTime * 50f;
            addTPSPitch = cameraTarget.playerVehicle.transform.InverseTransformDirection(TPSAccelerationPoint.position - cameraTarget.playerVehicle.transform.position).z * Time.fixedDeltaTime * 500f;

        }

        //// Look at the target.
        transform.SetPositionAndRotation(position, rotation);

        // Collision positions and rotations that affects pivot of the camera.
        if (Time.deltaTime != 0) {

            collisionPos = Vector3.Lerp(collisionPos, Vector3.zero, Time.deltaTime * 5f);
            collisionRot = Quaternion.Lerp(collisionRot, Quaternion.identity, Time.deltaTime * 5f);

        }

        // Lerping position and rotation of the pivot to collision.
        pivot.transform.localPosition = Vector3.Lerp(pivot.transform.localPosition, collisionPos, Time.deltaTime * 10f);
        pivot.transform.localRotation = Quaternion.Lerp(pivot.transform.localRotation, collisionRot, Time.deltaTime * 10f);

        // Lerping targetFieldOfView from TPSMinimumFOV to TPSMaximumFOV related to vehicle speed.
        targetFieldOfView = Mathf.Lerp(TPSMinimumFOV, TPSMaximumFOV, Mathf.Abs(cameraTarget.playerVehicle.speed) / 150f);

        // Rotates camera by Z axis for tilt effect.
        TPSTiltAngle = TPSTiltMaximum * (Mathf.Clamp(cameraTarget.playerVehicle.Rigid.velocity.x, -1f, 1f) * Mathf.Abs(cameraTarget.playerVehicle.Rigid.velocity.x) / 250f);
        TPSTiltAngle *= TPSTiltMultiplier;

        //  Checks occlusion if it's enabled.
        if (useOcclusion)
            OccludeRay(cameraTarget.playerVehicle.transform.position);

        //  Assigning last rotation of the camera.
        TPSLastRotation = transform.rotation;

        //  Setting TPS pitch, yaw, and tilt angles.
        transform.rotation *= Quaternion.Euler(TPSPitch + addTPSPitch, 0f, TPSYaw + TPSTiltAngle);

    }

    /// <summary>
    /// TPS mode.
    /// </summary>
    private void TPS2() {

        //  Setting rotation of the camera.
        transform.rotation = TPSLastRotation;

        // If TPS Auto Reverse is enabled and vehicle is moving backwards, reset X and Y orbits when vehicle direction is changed. Camera will look directly rear side of the vehicle.
        direction = cameraTarget.playerVehicle.direction;

        // Calculate the current rotation angles for TPS mode.
        if (!TPSAutoReverse)
            direction = 1;

        // Look back now?
        if (lookBackNow)
            direction *= -1;

        //  Vehicle direction.
        Vector3 playerVehicleDirection = cameraTarget.playerVehicle.transform.forward * direction;

        // Create the desired rotation based on the direction to the target
        Quaternion desiredRotation = Quaternion.LookRotation(playerVehicleDirection, Vector3.up);

        //  Desired vector.
        Vector3 desiredVector = desiredRotation.eulerAngles;

        //  Don't set this axis if it's not locked.
        if (!TPSLockX)
            desiredVector.x = transform.eulerAngles.x;

        //  Don't set this axis if it's not locked.
        if (!TPSLockY)
            desiredVector.y = transform.eulerAngles.y;

        //  Don't set this axis if it's not locked.
        if (!TPSLockZ)
            desiredVector.z = 0f;
        else
            desiredVector.z = cameraTarget.playerVehicle.transform.eulerAngles.z;

        //  Converting euler angles to quaternion.
        desiredRotation = Quaternion.Euler(desiredVector);

        //  Temp value for rotation damping.
        float rotDamp = TPSRotationDamping;

        //  Set rotation damping to 0 if free fall option is enabled and vehicle is not grounded.
        if (TPSFreeFall && Time.time >= 1f) {

            if (!cameraTarget.playerVehicle.IsGrounded)
                rotDamp = 0f;

        }

        // Smoothly rotate the object towards the desired rotation.
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotDamp * 10f * Time.deltaTime);

        //  Getting eulerangles of the camera.
        desiredVector = transform.rotation.eulerAngles;

        //  Applying orbit Y value.
        if (useOrbitInTPSCameraMode && orbitY != 0)
            desiredVector.x = orbitY_Smoothed;

        //  Applying orbit X value.
        if (useOrbitInTPSCameraMode && orbitX != 0)
            desiredVector.y = orbitX_Smoothed;

        //  Force z angle to 0.
        if (useOrbitInTPSCameraMode && (orbitX != 0 || orbitY != 0))
            desiredVector.z = 0f;

        //  Setting rotation of the camera.
        transform.rotation = Quaternion.Euler(desiredVector);

        //  Setting position of the camera.
        transform.position = cameraTarget.playerVehicle.transform.position;
        transform.position += cameraTarget.playerVehicle.transform.rotation * TPSOffset;
        transform.position -= transform.forward * TPSDistance;
        transform.position += Vector3.up * TPSHeight;

        float addTPSPitch = 0f;

        //  If TPS dynamic is enabled, reduce distance and height related to the vehicle speed.
        if (TPSDynamic && TPSAccelerationPoint != null) {

            transform.position -= (cameraTarget.playerVehicle.transform.position - TPSAccelerationPoint.position) * Time.fixedDeltaTime * 50f;
            addTPSPitch = cameraTarget.playerVehicle.transform.InverseTransformDirection(TPSAccelerationPoint.position - cameraTarget.playerVehicle.transform.position).z * Time.fixedDeltaTime * 300f;

        }

        //  Checks occlusion if it's enabled.
        if (useOcclusion)
            OccludeRay(cameraTarget.playerVehicle.transform.position);

        // Collision positions and rotations that affects pivot of the camera.
        if (Time.deltaTime != 0) {

            collisionPos = Vector3.Lerp(collisionPos, Vector3.zero, Time.deltaTime * 5f);
            collisionRot = Quaternion.Lerp(collisionRot, Quaternion.identity, Time.deltaTime * 5f);

        }

        // Lerping position and rotation of the pivot to collision.
        pivot.transform.localPosition = Vector3.Lerp(pivot.transform.localPosition, collisionPos, Time.deltaTime * 10f);
        pivot.transform.localRotation = Quaternion.Lerp(pivot.transform.localRotation, collisionRot, Time.deltaTime * 10f);

        //  Assigning last rotation of the camera.
        TPSLastRotation = transform.rotation;

        // Rotates camera by Z axis for tilt effect.
        TPSTiltAngle = TPSTiltMaximum * (Mathf.Clamp(cameraTarget.playerVehicle.transform.InverseTransformDirection(cameraTarget.playerVehicle.Rigid.velocity).x, -10f, 10f) * .04f);
        TPSTiltAngle *= TPSTiltMultiplier;

        //  Applying tilt angle rotation.
        transform.rotation *= Quaternion.Euler(TPSPitch + addTPSPitch, 0f, TPSYaw + TPSTiltAngle);

        // Lerping targetFieldOfView from TPSMinimumFOV to TPSMaximumFOV related to vehicle speed.
        targetFieldOfView = Mathf.Lerp(TPSMinimumFOV, TPSMaximumFOV, Mathf.Abs(cameraTarget.playerVehicle.speed) / 150f);

    }

    /// <summary>
    /// Fixed mode.
    /// </summary>
    private void FIXED() {

        if (FixedCamera.transform.parent != null)
            FixedCamera.transform.SetParent(null);

        if (useOcclusion && occluded) {

            FixedCamera.ChangePosition();
            occluded = false;

        }

    }

    /// <summary>
    /// Top mode
    /// </summary>
    private void TOP() {

        // Setting ortho or perspective?
        actualCamera.orthographic = useOrthoForTopCamera;

        topCameraDistanceOffset = Mathf.Lerp(0f, maximumZDistanceOffset, Mathf.Abs(cameraTarget.playerVehicle.speed) / 100f);
        targetFieldOfView = Mathf.Lerp(minimumOrtSize, maximumOrtSize, Mathf.Abs(cameraTarget.playerVehicle.speed) / 100f);
        actualCamera.orthographicSize = targetFieldOfView;

        // Setting proper targetPosition for top camera mode.
        targetPosition = cameraTarget.playerVehicle.transform.position;
        targetPosition += cameraTarget.playerVehicle.transform.rotation * Vector3.forward * topCameraDistanceOffset;

        // Assigning position and rotation.
        transform.SetPositionAndRotation(targetPosition, Quaternion.Euler(topCameraAngle));

        // Pivot position.
        pivot.transform.localPosition = new Vector3(0f, 0f, -topCameraDistance);

    }

    /// <summary>
    /// Orbit mode.
    /// </summary>
    private void ORBIT() {

        //  If old orbit is not same right now, set the time limiter to 2 seconds.
        if (oldOrbitX != orbitX) {

            if (oldOrbitX == 0 && orbitX != 0) {

                orbitX = cameraTarget.playerVehicle.transform.eulerAngles.y;
                orbitX_Smoothed = orbitX;

            }

            oldOrbitX = orbitX;
            orbitResetTimer = 2f;

        }

        //  If old orbit is not same right now, set the time limiter to 2 seconds.
        if (oldOrbitY != orbitY) {

            oldOrbitY = orbitY;
            orbitResetTimer = 2f;

        }

        //  Count to 0.
        if (orbitResetTimer > 0)
            orbitResetTimer -= Time.deltaTime;

        //  Limiting the timer.
        Mathf.Clamp(orbitResetTimer, 0f, 2f);

        //  If timer reaches to 0, and vehicle speed is not below 25, reset the orbits.
        if (orbitReset && Mathf.Abs(cameraTarget.playerVehicle.speed) >= 25f && orbitResetTimer <= 0f) {

            orbitX = 0f;
            orbitY = 0f;

        }

    }

    /// <summary>
    /// Used with mobile UI drag panel.
    /// </summary>
    /// <param name="pointerData"></param>
    public void OnDrag(PointerEventData pointerData) {

        if (!useOrbitOnlyHolding)
            return;

        // Receiving drag input from UI.
        orbitX += pointerData.delta.x * orbitXSpeed / 1000f;
        orbitY -= pointerData.delta.y * orbitYSpeed / 1000f;

        orbitResetTimer = 0f;

    }

    /// <summary>
    /// Cinematic mode.
    /// </summary>
    private void CINEMATIC() {

        if (CinematicCamera.transform.parent != null)
            CinematicCamera.transform.SetParent(null);

        targetFieldOfView = CinematicCamera.targetFOV;

        if (useOcclusion && occluded)
            ChangeCamera(CameraMode.TPS);

    }

    /// <summary>
    /// Resetting camera. Useful when switching between camera modes.
    /// </summary>
    public void ResetCamera() {

        if (FixedCamera)
            FixedCamera.canTrackNow = false;

        TPSTiltAngle = 0f;

        collisionPos = Vector3.zero;
        collisionRot = Quaternion.identity;

        actualCamera.transform.localPosition = Vector3.zero;
        actualCamera.transform.localRotation = Quaternion.identity;

        pivot.transform.localPosition = collisionPos;
        pivot.transform.localRotation = collisionRot;

        orbitX = TPSStartRotation.y;
        orbitY = TPSStartRotation.x;
        orbitHolding = false;

        zoomScroll = 0f;

        if (TPSStartRotation != Vector3.zero)
            TPSStartRotation = Vector3.zero;

        actualCamera.orthographic = false;
        occluded = false;

        switch (cameraMode) {

            case CameraMode.TPS:
                transform.SetParent(null);
                targetFieldOfView = TPSMinimumFOV;
                break;

            case CameraMode.FPS:
                transform.SetParent(cameraTarget.HoodCamera.transform, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                targetFieldOfView = hoodCameraFOV;
                cameraTarget.HoodCamera.FixShake();
                break;

            case CameraMode.WHEEL:
                transform.SetParent(cameraTarget.WheelCamera.transform, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                targetFieldOfView = wheelCameraFOV;
                break;

            case CameraMode.FIXED:
                transform.SetParent(FixedCamera.transform, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                targetFieldOfView = 60;
                FixedCamera.canTrackNow = true;
                break;

            case CameraMode.CINEMATIC:
                transform.SetParent(CinematicCamera.pivot.transform, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                targetFieldOfView = 30f;
                break;

            case CameraMode.TOP:
                transform.SetParent(null);
                targetFieldOfView = minimumOrtSize;
                pivot.transform.localPosition = Vector3.zero;
                pivot.transform.localRotation = Quaternion.identity;
                targetPosition = cameraTarget.playerVehicle.transform.position;
                targetPosition += cameraTarget.playerVehicle.transform.rotation * Vector3.forward * topCameraDistanceOffset;
                transform.position = cameraTarget.playerVehicle.transform.position;
                break;

        }

        actualCamera.fieldOfView = targetFieldOfView;

    }

    /// <summary>
    /// Enables or disables the camera.
    /// </summary>
    /// <param name="state"></param>
    public void ToggleCamera(bool state) {

        // Enabling / disabling activity.
        isRendering = state;

    }

    /// <summary>
    /// Checks occlusion.
    /// </summary>
    /// <param name="targetFollow"></param>
    private void OccludeRay(Vector3 targetFollow) {

        //declare a new raycast hit.
        RaycastHit wallHit = new RaycastHit();

        if (Physics.Linecast(targetFollow, transform.position, out wallHit, occlusionLayerMask)) {

            if (!wallHit.collider.isTrigger && !wallHit.transform.IsChildOf(cameraTarget.playerVehicle.transform)) {

                //the x and z coordinates are pushed away from the wall by hit.normal.
                //the y coordinate stays the same.
                Vector3 occludedPosition = new Vector3(wallHit.point.x + wallHit.normal.x * .2f, wallHit.point.y + wallHit.normal.y * .2f, wallHit.point.z + wallHit.normal.z * .2f);

                transform.position = occludedPosition;

            }

        }

    }

    /// <summary>
    /// Checks occlusion.
    /// </summary>
    /// <param name="targetFollow"></param>
    private void CheckIfOccluded() {

        //Declare a new raycast hit.
        RaycastHit wallHit = new RaycastHit();

        if (Physics.Linecast(cameraTarget.playerVehicle.transform.position, transform.position, out wallHit, occlusionLayerMask)) {

            if (!wallHit.collider.isTrigger && !wallHit.transform.IsChildOf(cameraTarget.playerVehicle.transform))
                occluded = true;

        }

    }

    /// <summary>
    /// Autofocus to target vehicle. Calculates distance and height of the camera related to bounds of the target vehicle.
    /// </summary>
    /// <returns></returns>
    public IEnumerator AutoFocus() {

        float timer = 2f;
        float bounds = RCCP_GetBounds.MaxBoundsExtent(cameraTarget.playerVehicle.transform);

        while (timer > 0f) {

            timer -= Time.deltaTime;
            TPSDistance = Mathf.MoveTowards(TPSDistance, bounds * 2.9f, Time.deltaTime * 4f);
            TPSHeight = Mathf.MoveTowards(TPSHeight, bounds * .65f, Time.deltaTime * 4f);
            yield return null;

        }

        TPSDistance = bounds * 2.9f;
        TPSHeight = bounds * .65f;

    }

    /// <summary>
    /// Autofocus to target vehicle. Calculates distance and height of the camera related to bounds of the target vehicle.
    /// </summary>
    /// <returns></returns>
    public IEnumerator AutoFocus(Transform transformBounds) {

        float timer = 2f;
        float bounds = RCCP_GetBounds.MaxBoundsExtent(transformBounds);

        while (timer > 0f) {

            timer -= Time.deltaTime;
            TPSDistance = Mathf.MoveTowards(TPSDistance, bounds * 2.9f, Time.deltaTime * 4f);
            TPSHeight = Mathf.MoveTowards(TPSHeight, bounds * .65f, Time.deltaTime * 4f);
            yield return null;

        }

        TPSDistance = bounds * 2.9f;
        TPSHeight = bounds * .65f;

    }

    /// <summary>
    /// Autofocus to target vehicle. Calculates distance and height of the camera related to bounds of the target vehicle.
    /// </summary>
    /// <returns></returns>
    public IEnumerator AutoFocus(Transform transformBounds1, Transform transformBounds2) {

        float timer = 3f;
        float bounds = (RCCP_GetBounds.MaxBoundsExtent(transformBounds1) + RCCP_GetBounds.MaxBoundsExtent(transformBounds2));

        while (timer > 0f) {

            timer -= Time.deltaTime;
            TPSDistance = Mathf.MoveTowards(TPSDistance, bounds * 2.9f, Time.deltaTime * 6f);
            TPSHeight = Mathf.MoveTowards(TPSHeight, bounds * .65f, Time.deltaTime * 6f);
            yield return null;

        }

        TPSDistance = bounds * 2.9f;
        TPSHeight = bounds * .65f;

    }

    /// <summary>
    /// Autofocus to target vehicle. Calculates distance and height of the camera related to bounds of the target vehicle.
    /// </summary>
    /// <returns></returns>
    public IEnumerator AutoFocus(Transform transformBounds1, Transform transformBounds2, Transform transformBounds3) {

        float timer = 3f;
        float bounds = (RCCP_GetBounds.MaxBoundsExtent(transformBounds1) + RCCP_GetBounds.MaxBoundsExtent(transformBounds2) + RCCP_GetBounds.MaxBoundsExtent(transformBounds3));

        while (timer > 0f) {

            timer -= Time.deltaTime;
            TPSDistance = Mathf.MoveTowards(TPSDistance, bounds * 2.9f, Time.deltaTime * 6f);
            TPSHeight = Mathf.MoveTowards(TPSHeight, bounds * .65f, Time.deltaTime * 6f);
            yield return null;

        }

        TPSDistance = bounds * 2.9f;
        TPSHeight = bounds * .65f;

    }

    public bool IsCameraActive() {

        if (!actualCamera)
            return false;

        if (!actualCamera.gameObject.activeSelf)
            return false;

        if (!actualCamera.isActiveAndEnabled)
            return false;

        return true;

    }

    private void OnDisable() {

        RCCP_InputManager.OnChangedCamera -= RCCP_InputManager_OnChangedCamera;
        RCCP_InputManager.OnLookBackCamera -= RCCP_InputManager_OnLookBackCamera;
        RCCP_InputManager.OnHoldOrbitCamera -= RCCP_InputManager_OnHoldOrbitCamera;

    }

}