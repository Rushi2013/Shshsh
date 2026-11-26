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
using System.Linq;

/// <summary>
/// Main light manager of the RCCP_Light. All lights must be connected to this manager.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Addons/RCCP Lights")]
public class RCCP_Lights : RCCP_Component {

    /// <summary>
    /// All ligths attached to the vehicle.
    /// </summary>
    public List<RCCP_Light> lights = new List<RCCP_Light>();

    /// <summary>
    /// Headlights
    /// </summary>
    public RCCP_Light[] Headlights {

        get {

            List<RCCP_Light> headlights = new List<RCCP_Light>();

            if (lights != null && lights.Count >= 1) {

                for (int i = 0; i < lights.Count; i++) {

                    if (lights[i].lightType == RCCP_Light.LightType.Headlight_LowBeam || lights[i].lightType == RCCP_Light.LightType.Headlight_HighBeam)
                        headlights.Add(lights[i]);

                }

            }

            return headlights.ToArray();

        }

    }

    /// <summary>
    /// Brake lights.
    /// </summary>
    public RCCP_Light[] Brakelights {

        get {

            List<RCCP_Light> brakelights = new List<RCCP_Light>();

            if (lights != null && lights.Count >= 1) {

                for (int i = 0; i < lights.Count; i++) {

                    if (lights[i].lightType == RCCP_Light.LightType.Brakelight || lights[i].lightType == RCCP_Light.LightType.Taillight)
                        brakelights.Add(lights[i]);

                }

            }

            return brakelights.ToArray();

        }

    }

    /// <summary>
    /// Reverse lights.
    /// </summary>
    public RCCP_Light[] Reverselights {

        get {

            List<RCCP_Light> reverselights = new List<RCCP_Light>();

            if (lights != null && lights.Count >= 1) {

                for (int i = 0; i < lights.Count; i++) {

                    if (lights[i].lightType == RCCP_Light.LightType.Reverselight)
                        reverselights.Add(lights[i]);

                }

            }

            return reverselights.ToArray();

        }

    }

    /// <summary>
    /// Indicator lights.
    /// </summary>
    public RCCP_Light[] Indicatorlights {

        get {

            List<RCCP_Light> indicatorlights = new List<RCCP_Light>();

            if (lights != null && lights.Count >= 1) {

                for (int i = 0; i < lights.Count; i++) {

                    if (lights[i].lightType == RCCP_Light.LightType.IndicatorLeftLight || lights[i].lightType == RCCP_Light.LightType.IndicatorRightLight)
                        indicatorlights.Add(lights[i]);

                }

            }

            return indicatorlights.ToArray();

        }

    }

    /// <summary>
    /// Bools.
    /// </summary>
    public bool lowBeamHeadlights = false;
    public bool highBeamHeadlights = false;
    public bool brakeLights = false;
    public bool reverseLights = false;
    public bool indicatorsLeft = false;
    public bool indicatorsRight = false;
    public bool indicatorsAll = false;
    public float indicatorTimer = 0f;

    public bool tailLightFound = false;
    public bool highBeamLightFound = false;

    public bool highBeamWithLowBeamOnly = false;

    public void GetAllLights() {

        if (lights == null)
            lights = new List<RCCP_Light>();

        lights.Clear();

        lights = GetComponentsInChildren<RCCP_Light>(true).ToList();

    }

    private void Update() {

        CheckLights();
        Inputs();
        IndicatorTimer();

    }

    /// <summary>
    /// Checking for null lights.
    /// </summary>
    private void CheckLights() {

        if (lights != null) {

            for (int i = 0; i < lights.Count; i++) {

                if (lights[i] == null)
                    lights.RemoveAt(i);

            }

        }

    }

    /// <summary>
    /// Registers the target light.
    /// </summary>
    /// <param name="newLight"></param>
    public void RegisterLight(RCCP_Light newLight) {

        //  If list doesn't contain the target light, add it.
        if (!lights.Contains(newLight))
            lights.Add(newLight);

        //  If it's a taillight.
        if (newLight.lightType == RCCP_Light.LightType.Taillight)
            tailLightFound = true;

        //  If it's a high beam light.
        if (newLight.lightType == RCCP_Light.LightType.Headlight_HighBeam)
            highBeamLightFound = true;

    }

    /// <summary>
    /// Vehicle inputs.
    /// </summary>
    private void Inputs() {

        //  If vehicle is braking now.
        if (CarController.brakeInput_V >= .1f)
            brakeLights = true;
        else
            brakeLights = false;

        //  If vehicle is reversing now.
        if (CarController.reversingNow)
            reverseLights = true;
        else
            reverseLights = false;

    }

    /// <summary>
    /// Indicator timer.
    /// </summary>
    private void IndicatorTimer() {

        //  If indicators in use now, increase the timer. Otherwise set it to 0.
        if (indicatorsLeft || indicatorsRight || indicatorsAll)
            indicatorTimer += Time.deltaTime;
        else
            indicatorTimer = 0f;

        //  If indicator timer is 1, set it to 0.
        if (indicatorTimer >= 1f)
            indicatorTimer = 0f;

    }

    public void Reload() {

        indicatorTimer = 0f;

    }

}
