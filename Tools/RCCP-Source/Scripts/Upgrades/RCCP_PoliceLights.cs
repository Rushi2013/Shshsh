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
/// Police siren with operated lights.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Misc/RCCP Police Lights")]
[DisallowMultipleComponent]
public class RCCP_PoliceLights : RCCP_Component {

    /// <summary>
    /// Siren modes. On and Off.
    /// </summary>
    public SirenMode sirenMode = SirenMode.Off;
    public enum SirenMode { Off, On }

    /// <summary>
    /// Red lights.
    /// </summary>
    public Light[] redLights;

    /// <summary>
    /// Blue lights.
    /// </summary>
    public Light[] blueLights;

    private void Update() {

        //  If siren mode is set to off, set all intensity of the lights to 0. Otherwise, set to 1 with timer.
        switch (sirenMode) {

            case SirenMode.Off:

                for (int i = 0; i < redLights.Length; i++)
                    if (redLights[i] != null) redLights[i].intensity = Mathf.Lerp(redLights[i].intensity, 0f, Time.deltaTime * 50f);

                for (int i = 0; i < blueLights.Length; i++)
                    if (blueLights[i] != null) blueLights[i].intensity = Mathf.Lerp(blueLights[i].intensity, 0f, Time.deltaTime * 50f);

                break;

            case SirenMode.On:

                if (Mathf.Approximately((int)(Time.time) % 2, 0) && Mathf.Approximately((int)(Time.time * 20) % 3, 0)) {

                    for (int i = 0; i < redLights.Length; i++)
                        if (redLights[i] != null) redLights[i].intensity = Mathf.Lerp(redLights[i].intensity, 1f, Time.deltaTime * 50f);

                } else {

                    for (int i = 0; i < redLights.Length; i++)
                        if (redLights[i] != null) redLights[i].intensity = Mathf.Lerp(redLights[i].intensity, 0f, Time.deltaTime * 50f);

                    if (Mathf.Approximately((int)(Time.time * 20) % 3, 0)) {

                        for (int i = 0; i < blueLights.Length; i++)
                            if (blueLights[i] != null) blueLights[i].intensity = Mathf.Lerp(blueLights[i].intensity, 1f, Time.deltaTime * 50f);

                    } else {

                        for (int i = 0; i < blueLights.Length; i++)
                            if (blueLights[i] != null) blueLights[i].intensity = Mathf.Lerp(blueLights[i].intensity, 0f, Time.deltaTime * 50f);

                    }

                }

                break;

        }

    }

    /// <summary>
    /// Sets the siren mode to on or off.
    /// </summary>
    /// <param name="state"></param>
    public void SetSiren(bool state) {

        if (state)
            sirenMode = SirenMode.On;
        else
            sirenMode = SirenMode.Off;

    }

}
