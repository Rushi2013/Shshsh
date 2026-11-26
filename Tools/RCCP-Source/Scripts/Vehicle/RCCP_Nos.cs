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
/// NOS / Boost used to multiply engine torque for a limited time.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Other Addons/RCCP Nos")]
public class RCCP_Nos : RCCP_Component {

    /// <summary>
    /// Nos is currently in use now?
    /// </summary>
    [HideInInspector] public bool nosInUse = false;

    /// <summary>
    /// Engine torque multiplier.
    /// </summary>
    [Min(0f)] public float torqueMultiplier = 1.5f;

    /// <summary>
    /// Amount of nos in range between 0 - 1.
    /// </summary>
    [Range(0f, 1f)] public float amount = 1f;

    /// <summary>
    /// Maximum duration of the nos in seconds.
    /// </summary>
    [Min(0f)] public float durationTime = 3f;

    /// <summary>
    /// Current timer.
    /// </summary>
    [Min(0f)] public float timer = 3f;

    /// <summary>
    /// Regenerates the nos after this seconds.
    /// </summary>
    [Min(0f)] public float regenerateTime = 2f;

    /// <summary>
    /// Current timer to regenerate.
    /// </summary>
    [Min(0f)] private float regenerateTimer = 1f;

    /// <summary>
    /// Regenerate rate. Nos will be filled up more faster on higher values.
    /// </summary>
    [Min(0f)] public float regenerateRate = 1f;

    private void Update() {

        //  If no engine found, return.
        if (!CarController.Engine)
            return;

        //  If nos input is above 0.5, enable nos. Otherwise disable.
        nosInUse = CarController.nosInput_P >= .5f ? true : false;

        //  If engine is not running, set it to off.
        if (!CarController.Engine.engineRunning)
            nosInUse = false;

        //  If throttle input is not high enough, set it to off.
        if (CarController.throttleInput_V < .5f)
            nosInUse = false;

        //  If timer is not enough, set it to off.
        if (timer <= .1f)
            nosInUse = false;

        //  If nos is currently in use now, decrease the timer and multiply the enngine torque.
        if (nosInUse) {

            regenerateTimer = 0f;

            timer -= Time.deltaTime;
            timer = Mathf.Clamp(timer, 0f, Mathf.Infinity);

            CarController.Engine.Multiply(torqueMultiplier);

        }

        //  Regenerating the nos with timer.
        if (regenerateTimer < regenerateTime)
            regenerateTimer += Time.deltaTime;

        if (regenerateTimer >= regenerateTime)
            timer += Time.deltaTime * regenerateRate;

        timer = Mathf.Clamp(timer, 0f, durationTime);
        amount = timer / durationTime;

    }

    public void Reload() {

        //  Make sure nos in use is disabled when enabling / disabling the vehicle.
        nosInUse = false;

    }

}
