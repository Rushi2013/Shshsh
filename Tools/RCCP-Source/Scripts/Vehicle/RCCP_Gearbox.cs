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
/// Multiplies the received power from the engine --> clutch by x ratio, and transmits it to the differential. Higher ratios = faster accelerations, lower top speeds, lower ratios = slower accelerations, higher top speeds.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Drivetrain/RCCP Gearbox")]
public class RCCP_Gearbox : RCCP_Component {

    /// <summary>
    /// Overrides gears with given values. All calculations will be ignored.
    /// </summary>
    public bool overrideGear = false;

    /// <summary>
    /// Gear ratios. Faster accelerations on higher values, but lower top speeds.
    /// </summary>
    [Min(0.1f)] public float[] gearRatios = new float[] { 4.35f, 2.5f, 1.66f, 1.23f, 1.0f, .85f };

    /// <summary>
    /// Target gear RPMs.
    /// </summary>
    public float[] GearRPMs {

        get {

            gearRPMs = new float[gearRatios.Length];

            for (int i = 0; i < gearRPMs.Length; i++) {

                if (GetComponentInParent<RCCP_CarController>(true).GetComponentInChildren<RCCP_Engine>(true))
                    gearRPMs[i] = GetComponentInParent<RCCP_CarController>(true).GetComponentInChildren<RCCP_Engine>(true).maxEngineRPM / gearRatios[i];

            }

            return gearRPMs;

        }

    }

    /// <summary>
    /// All gear rpms.
    /// </summary>
    public float[] gearRPMs;

    /// <summary>
    /// Current gear.
    /// </summary>
    [Min(0)] public int currentGear = 0;

    /// <summary>
    /// 0 means N, 1 means any gear is in use now.
    /// </summary>
    [Min(0f)] public float gearInput = 0f;

    /// <summary>
    /// Reverse gear engaged now?
    /// </summary>
    public bool reverseGearEngaged = false;

    /// <summary>
    /// Neutral gear engaged now?
    /// </summary>
    public bool neutralGearEngaged = false;

    /// <summary>
    /// Park gear engaged now?
    /// </summary>
    public bool parkGearEngaged = false;

    /// <summary>
    /// Drive gear engaged now?
    /// </summary>
    public bool driveGearEngaged = false;

    public float[] targetSpeeds;

    /// <summary>
    /// Shifting time.
    /// </summary>
    [Min(0f)] public float shiftingTime = .2f;

    /// <summary>
    /// Shifting now?
    /// </summary>
    public bool shiftingNow = false;

    /// <summary>
    /// Don't shift timer if too close to previous one.
    /// </summary>
    public bool dontShiftTimer = true;

    /// <summary>
    /// Timer for don't shift.
    /// </summary>
    [Min(0f)] public float lastTimeShifted = 0f;

    /// <summary>
    /// Automatic transmission.
    /// </summary>
    [System.Obsolete("automaticTransmission in RCCP_Gearbox is obsolete, please use transmissionType instead.")] public bool automaticTransmission = true;

    /// <summary>
    /// Transmission types.
    /// </summary>
    public enum TransmissionType { Manual, Automatic, Automatic_DNRP }
    public TransmissionType transmissionType = TransmissionType.Automatic;

    /// <summary>
    /// Semi automatic gear.
    /// </summary>
    public enum SemiAutomaticDNRPGear { D, N, R, P }
    public SemiAutomaticDNRPGear automaticGearSelector = SemiAutomaticDNRPGear.D;

    /// <summary>
    /// Automatic transmission will shift up late on higher values.
    /// </summary>
    [Range(.1f, .9f)] public float shiftThreshold = .8f;

    /// <summary>
    /// Target engine rpm to shift up.
    /// </summary>
    [Min(0f)] public float shiftUpRPM = 5500f;

    /// <summary>
    /// Target engine rpm to shift down.
    /// </summary>
    [Min(0f)] public float shiftDownRPM = 2750f;

    /// <summary>
    /// Received torque from the component. It should be the clutch in this case.
    /// </summary>
    public float receivedTorqueAsNM = 0f;

    /// <summary>
    /// Produced and delivered torque to the component. It should be the differential in this case.
    /// </summary>
    public float producedTorqueAsNM = 0f;

    /// <summary>
    /// Output with custom class.
    /// </summary>
    public RCCP_Event_Output outputEvent = new RCCP_Event_Output();
    private RCCP_Output output = new RCCP_Output();

    private void Update() {

        //  Setting timer for last shifting.
        if (lastTimeShifted > 0)
            lastTimeShifted -= Time.deltaTime;

        //  Clamping timer.
        lastTimeShifted = Mathf.Clamp(lastTimeShifted, 0f, 10f);

        //  If gear is not neutral, set gear input to 1. Otherwise to 0.
        if (neutralGearEngaged || parkGearEngaged)
            gearInput = 0f;
        else
            gearInput = 1f;

        //if (reverseGearEngaged)
        //    gearInput = -1;

    }

    private void FixedUpdate() {

        if (transmissionType == TransmissionType.Automatic)
            AutomaticTransmission();

        if (transmissionType == TransmissionType.Automatic_DNRP)
            AutomaticTransmissionDNRP();

        Output();

    }

    /// <summary>
    /// Calculates estimated speeds and rpms to shift up / down.
    /// </summary>
    private void AutomaticTransmission() {

        //  Early out if overriding gear is enabled. This means, an external class is adjusting gears.
        if (overrideGear)
            return;

        //  Getting engine rpm.
        float engineRPM = CarController.engineRPM;

        //  Creating float array for target speeds.
        float[] targetSpeeds = FindTargetSpeed();

        //  Creating low and high limits multiplied with threshold value.
        float lowLimit, highLimit;

        //  If current gear is not first gear, there is a low limit.
        if (currentGear > 0)
            lowLimit = targetSpeeds[currentGear - 1];

        //  High limit.
        highLimit = targetSpeeds[currentGear];

        bool canShiftUpNow = false;
      

        //  If reverse gear is not engaged, engine rpm is above shiftup rpm, and wheel & vehicle speed is above the high limit, shift up.
        if (currentGear < gearRatios.Length && !reverseGearEngaged && engineRPM >= shiftUpRPM && CarController.wheelRPM2Speed >= highLimit && CarController.speed >= highLimit)
            canShiftUpNow = true;
       
        if (shiftingNow)
            canShiftUpNow = false;

        bool canShiftDownNow = false;

        //  If reverse gear is not engaged, engine rpm is below shiftdown rpm, and wheel & vehicle speed is below the low limit, shift down.
        if (currentGear > 0 && !reverseGearEngaged && engineRPM <= shiftDownRPM) {

            if (FindEligibleGear() != currentGear)
                canShiftDownNow = true;
            else
                canShiftDownNow = false;

        }

        if (shiftingNow)
            canShiftDownNow = false;

        if (!dontShiftTimer)
            lastTimeShifted = 0f;

        if (!shiftingNow && lastTimeShifted <= .02f) {

            if (canShiftDownNow)
                ShiftToGear(FindEligibleGear());

            if (canShiftUpNow)
                ShiftUp();

        }

    }

    /// <summary>
    /// Calculates estimated speeds and rpms to shift up / down.
    /// </summary>
    private void AutomaticTransmissionDNRP() {

        //  Early out if overriding gear is enabled. This means, an external class is adjusting gears.
        if (overrideGear)
            return;

        switch (automaticGearSelector) {

            case SemiAutomaticDNRPGear.D:

                driveGearEngaged = true;
                neutralGearEngaged = false;
                parkGearEngaged = false;
                reverseGearEngaged = false;

                break;

            case SemiAutomaticDNRPGear.N:

                driveGearEngaged = false;
                neutralGearEngaged = true;
                parkGearEngaged = false;
                reverseGearEngaged = false;

                break;

            case SemiAutomaticDNRPGear.R:

                driveGearEngaged = true;
                neutralGearEngaged = false;
                parkGearEngaged = false;
                reverseGearEngaged = true;

                break;

            case SemiAutomaticDNRPGear.P:

                driveGearEngaged = false;
                neutralGearEngaged = true;
                parkGearEngaged = true;
                reverseGearEngaged = false;

                break;

        }

        //  Getting engine rpm.
        float engineRPM = CarController.engineRPM;

        //  Creating float array for target speeds.
        float[] targetSpeeds = FindTargetSpeed();

        //  Creating low and high limits multiplied with threshold value.
        float lowLimit, highLimit;

        //  If current gear is not first gear, there is a low limit.
        if (currentGear > 0)
            lowLimit = targetSpeeds[currentGear - 1];

        //  High limit.
        highLimit = targetSpeeds[currentGear];

        bool canShiftUpNow = false;

        //  If reverse gear is not engaged, engine rpm is above shiftup rpm, and wheel & vehicle speed is above the high limit, shift up.
        if (currentGear < gearRatios.Length && !reverseGearEngaged && engineRPM >= shiftUpRPM && CarController.wheelRPM2Speed >= highLimit && CarController.speed >= highLimit)
            canShiftUpNow = true;

        if (shiftingNow)
            canShiftUpNow = false;

        bool canShiftDownNow = false;

        //  If reverse gear is not engaged, engine rpm is below shiftdown rpm, and wheel & vehicle speed is below the low limit, shift down.
        if (currentGear > 0 && !reverseGearEngaged && engineRPM <= shiftDownRPM) {

            if (FindEligibleGear() != currentGear)
                canShiftDownNow = true;
            else
                canShiftDownNow = false;

        }

        if (shiftingNow)
            canShiftDownNow = false;

        if (!dontShiftTimer)
            lastTimeShifted = 0f;

        if (!shiftingNow && lastTimeShifted <= .02f) {

            if (canShiftDownNow)
                ShiftToGear(FindEligibleGear());

            if (canShiftUpNow)
                ShiftUp();

        }

    }

    /// <summary>
    /// Received torque from the component.
    /// </summary>
    /// <param name="output"></param>
    public void ReceiveOutput(RCCP_Output output) {

        receivedTorqueAsNM = output.NM;

    }

    /// <summary>
    /// Finds eligible gear depends on the speed.
    /// </summary>
    /// <returns></returns>
    private float[] FindTargetSpeed() {

        //  Creating float array for target speeds.
        targetSpeeds = new float[gearRatios.Length];

        float partition = CarController.maximumSpeed / gearRatios.Length;

        //  Assigning target speeds.
        for (int i = targetSpeeds.Length - 1; i >= 0; i--)
            targetSpeeds[i] = partition * (i + 1) * shiftThreshold;

        return targetSpeeds;

    }

    /// <summary>
    /// Finds eligible gear depends on the speed.
    /// </summary>
    /// <returns></returns>
    private int FindEligibleGear() {

        float[] targetSpeeds = FindTargetSpeed();
        int eligibleGear = 0;

        for (int i = 0; i < targetSpeeds.Length; i++) {

            if (CarController.speed < targetSpeeds[i]) {

                eligibleGear = i;
                break;

            }

        }

        return eligibleGear;

    }

    /// <summary>
    /// Shift up.
    /// </summary>
    public void ShiftUp() {

        if (shiftingNow)
            return;

        if (transmissionType == TransmissionType.Automatic_DNRP && automaticGearSelector != SemiAutomaticDNRPGear.D)
            return;

        if (reverseGearEngaged) {

            StartCoroutine(ShiftTo(0));

        } else {

            if (currentGear < gearRatios.Length - 1)
                StartCoroutine(ShiftTo(currentGear + 1));

        }

        reverseGearEngaged = false;
        neutralGearEngaged = false;

    }

    /// <summary>
    /// Shift down.
    /// </summary>
    public void ShiftDown() {

        if (shiftingNow)
            return;

        if (transmissionType == TransmissionType.Automatic_DNRP && automaticGearSelector != SemiAutomaticDNRPGear.D)
            return;

        reverseGearEngaged = false;
        neutralGearEngaged = false;

        if (currentGear > 0)
            StartCoroutine(ShiftTo(currentGear - 1));
        else
            ShiftReverse();

    }

    /// <summary>
    /// Shift reverse.
    /// </summary>
    public void ShiftReverse() {

        if (shiftingNow)
            return;

        if (transmissionType == TransmissionType.Automatic_DNRP && automaticGearSelector != SemiAutomaticDNRPGear.R)
            return;

        if (CarController.speed > 20f)
            return;

        reverseGearEngaged = true;
        neutralGearEngaged = false;
        currentGear = 0;
        StartCoroutine(ShiftTo(-1));

    }

    /// <summary>
    /// Shift to specific gear.
    /// </summary>
    /// <param name="gear"></param>
    public void ShiftToGear(int gear) {

        if (shiftingNow)
            return;

        reverseGearEngaged = false;
        neutralGearEngaged = false;
        StartCoroutine(ShiftTo(gear));

    }

    /// <summary>
    /// Shift to specific gear.
    /// </summary>
    /// <param name="gear"></param>
    public void ShiftToN() {

        if (shiftingNow)
            return;

        currentGear = 0;
        reverseGearEngaged = false;
        neutralGearEngaged = true;

    }

    /// <summary>
    /// Shift to specific gear with delay.
    /// </summary>
    /// <param name="gear"></param>
    /// <returns></returns>
    private IEnumerator ShiftTo(int gear) {

        shiftingNow = true;
        neutralGearEngaged = true;

        yield return new WaitForSeconds(shiftingTime);

        lastTimeShifted = 1f;

        if (gear == -1)
            reverseGearEngaged = true;
        else
            reverseGearEngaged = false;

        if (gear == -2)
            neutralGearEngaged = true;
        else
            neutralGearEngaged = false;

        gear = Mathf.Clamp(gear, 0, gearRatios.Length - 1);
        currentGear = gear;
        shiftingNow = false;

    }

    /// <summary>
    /// Output.
    /// </summary>
    private void Output() {

        if (output == null)
            output = new RCCP_Output();

        producedTorqueAsNM = receivedTorqueAsNM * gearRatios[currentGear] * gearInput;

        if (reverseGearEngaged)
            producedTorqueAsNM *= -1;

        if (neutralGearEngaged)
            producedTorqueAsNM = 0f;

        output.NM = producedTorqueAsNM / outputEvent.GetPersistentEventCount();
        outputEvent.Invoke(output);

    }

    /// <summary>
    /// Inits the gears.
    /// </summary>
    public void InitGears(int totalGears) {

        //  Creating float array.
        gearRatios = new float[totalGears];

        //  Creating other arrays for specific.
        float[] gearRatio = new float[gearRatios.Length];
        int[] maxSpeedForGear = new int[gearRatios.Length];
        int[] targetSpeedForGear = new int[gearRatios.Length];

        //  Assigning array with preset values.
        if (gearRatios.Length == 1)
            gearRatio = new float[] { 1.0f };

        if (gearRatios.Length == 2)
            gearRatio = new float[] { 2.0f, 1.0f };

        if (gearRatios.Length == 3)
            gearRatio = new float[] { 2.0f, 1.5f, 1.0f };

        if (gearRatios.Length == 4)
            gearRatio = new float[] { 2.86f, 1.62f, 1.0f, .72f };

        if (gearRatios.Length == 5)
            gearRatio = new float[] { 4.23f, 2.52f, 1.66f, 1.22f, 1.0f, };

        if (gearRatios.Length == 6)
            gearRatio = new float[] { 4.35f, 2.5f, 1.66f, 1.23f, 1.0f, .85f };

        if (gearRatios.Length == 7)
            gearRatio = new float[] { 4.5f, 2.5f, 1.66f, 1.23f, 1.0f, .9f, .8f };

        if (gearRatios.Length == 8)
            gearRatio = new float[] { 4.6f, 2.5f, 1.86f, 1.43f, 1.23f, 1.05f, .9f, .72f };

        gearRatios = gearRatio;

    }

    public void OverrideGear(int targetGear, bool targetReverseGear) {

        currentGear = targetGear;
        reverseGearEngaged = targetReverseGear;

    }

    public void Reload() {

        //  Make sure shifting now, and neutral gear engaged is set to false when enabling the vehicle.
        shiftingNow = false;
        driveGearEngaged = false;
        parkGearEngaged = false;
        neutralGearEngaged = false;
        reverseGearEngaged = false;
        lastTimeShifted = 0f;
        currentGear = 0;
        gearInput = 0f;
        producedTorqueAsNM = 0f;

    }

}
