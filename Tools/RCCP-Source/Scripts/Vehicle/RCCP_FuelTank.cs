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
/// Fuel tank. 
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Other Addons/RCCP Fuel Tank")]
public class RCCP_FuelTank : RCCP_Component {

    /// <summary>
    /// Current capacity of the fuel tank.
    /// </summary>
    [Range(0f, 100f)] public float fuelTankCapacity = 60f;

    /// <summary>
    /// Fuel tank fill amount.
    /// </summary>
    [Range(0f, 1f)] public float fuelTankFillAmount = 1f;

    /// <summary>
    /// Maximum fuel consumption rate.
    /// </summary>
    [Range(0f, 100f)] public float fuelConsumption = 20f;

    /// <summary>
    /// Current fuel consumption rate.
    /// </summary>
    [Range(0f, 100f)] public float currentFuelConsumption = 0f;

    /// <summary>
    /// Default capacity of the fuel tank.
    /// </summary>
    [Min(0f)] private float fuelTankCapacityDefault = 60f;

    /// <summary>
    /// Stop the engine on out of fuel.
    /// </summary>
    public bool stopEngine = true;

    public override void Start() {

        base.Start();

        //  Getting original capacity of the fuel tank. We'll be using it while refilling the tank completely.
        fuelTankCapacityDefault = fuelTankCapacity;

    }

    private void Update() {

        //  Setting fuel tank amount. 0 = empty, 1 = full.
        fuelTankFillAmount = Mathf.Lerp(0f, 1f, fuelTankCapacity / fuelTankCapacityDefault);

        //  If no engine found, return.
        if (!CarController.Engine)
            return;

        //  If engine is not running, return.
        if (!CarController.Engine.engineRunning)
            return;

        //  Calculating the current consumption rate depending on the engine load.
        currentFuelConsumption = CarController.Engine.engineRPM * fuelConsumption * .05f * (Time.deltaTime);

        //  Decreasing the fuel tank amount related to the consumption rate.
        fuelTankCapacity -= currentFuelConsumption * .00001f;

        //  If capacity is 0, stop the engine if enabled.
        if (fuelTankCapacity < 0f) {

            fuelTankCapacity = 0f;

            if (stopEngine)
                CarController.Engine.StopEngine();

        }

    }

    /// <summary>
    /// Refills the fuel tank completely.
    /// </summary>
    public void Refill() {

        fuelTankCapacity = fuelTankCapacityDefault;

    }

    /// <summary>
    /// Refills the fuel tank with given amount of fuel.
    /// </summary>
    public void Refill(float amountOfFuel) {

        //  Increasing the tank capacity.
        fuelTankCapacity += amountOfFuel * Time.deltaTime;

        //  And be sure not to exceed the limit.
        if (fuelTankCapacity > fuelTankCapacityDefault)
            fuelTankCapacity = fuelTankCapacityDefault;

    }

    public void Reload() {



    }

}
