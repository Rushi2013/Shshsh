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
/// Manager for all upgradable scripts (Engine, Brake, Handling).
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Customization/RCCP CarController Upgrade Customization Manager")]
public class RCCP_VehicleUpgrade_CustomizationManager : RCCP_UpgradeComponent, IRCCP_UpgradeComponent {

    /// <summary>
    /// Current customization data.
    /// </summary>
    public RCCP_CustomizationData customizationData = new RCCP_CustomizationData();

    /// <summary>
    /// Default customization data.
    /// </summary>
    public RCCP_CustomizationData customizationDataDefault = new RCCP_CustomizationData();

    public void Initialize() {

        StartCoroutine(InitializeDelayed());

    }

    private IEnumerator InitializeDelayed() {

        yield return new WaitForEndOfFrame();

        if (customizationData == null)
            customizationData = new RCCP_CustomizationData();

        if (customizationDataDefault == null)
            customizationDataDefault = new RCCP_CustomizationData();

        //  Getting the default values for restoring the vehicle.
        if (!customizationDataDefault.initialized) {

            if (CarController.FrontAxle) {

                customizationDataDefault.cambersFront = CarController.FrontAxle.leftWheelCollider.camber;
                customizationDataDefault.suspensionTargetFront = CarController.FrontAxle.leftWheelCollider.WheelCollider.suspensionSpring.targetPosition;
                customizationDataDefault.suspensionDistanceFront = CarController.FrontAxle.leftWheelCollider.WheelCollider.suspensionDistance;
                customizationDataDefault.suspensionSpringForceFront = CarController.FrontAxle.leftWheelCollider.WheelCollider.suspensionSpring.spring;
                customizationDataDefault.suspensionDamperFront = CarController.FrontAxle.leftWheelCollider.WheelCollider.suspensionSpring.damper;

            }

            if (CarController.RearAxle) {

                customizationDataDefault.cambersRear = CarController.RearAxle.leftWheelCollider.camber;
                customizationDataDefault.suspensionTargetRear = CarController.RearAxle.leftWheelCollider.WheelCollider.suspensionSpring.targetPosition;
                customizationDataDefault.suspensionDistanceRear = CarController.RearAxle.leftWheelCollider.WheelCollider.suspensionDistance;
                customizationDataDefault.suspensionSpringForceRear = CarController.RearAxle.leftWheelCollider.WheelCollider.suspensionSpring.spring;
                customizationDataDefault.suspensionDamperRear = CarController.RearAxle.leftWheelCollider.WheelCollider.suspensionSpring.damper;

            }

            if (CarController.Lights) {

                if (CarController.Lights.Headlights != null && CarController.Lights.Headlights.Length >= 1 && CarController.Lights.Headlights[0] != null)
                    customizationDataDefault.headlightColor = CarController.Lights.Headlights[0].GetLightColor();

            }

            if (CarController.Particles) {

                if (CarController.Particles.wheelParticles != null && CarController.Particles.wheelParticles.Length >= 1) {

                    for (int i = 0; i < CarController.Particles.wheelParticles.Length; i++) {

                        ParticleSystem.MainModule psmain = CarController.Particles.wheelParticles[i].allWheelParticles[0].main;
                        customizationDataDefault.wheelSmokeColor = psmain.startColor.color;

                    }

                }

            }

            customizationDataDefault.initialized = true;

        }

        customizationData = Loadout.customizationData;      //  Getting the customization data from the loadout.

        //  If data is null, return.
        if (customizationData == null)
            yield return null;

        //  If data is new, get customization data from the vehicle.
        if (!customizationData.initialized) {

            if (CarController.FrontAxle) {

                customizationData.cambersFront = CarController.FrontAxle.leftWheelCollider.camber;
                customizationData.suspensionTargetFront = CarController.FrontAxle.leftWheelCollider.WheelCollider.suspensionSpring.targetPosition;
                customizationData.suspensionDistanceFront = CarController.FrontAxle.leftWheelCollider.WheelCollider.suspensionDistance;
                customizationData.suspensionSpringForceFront = CarController.FrontAxle.leftWheelCollider.WheelCollider.suspensionSpring.spring;
                customizationData.suspensionDamperFront = CarController.FrontAxle.leftWheelCollider.WheelCollider.suspensionSpring.damper;

            }

            if (CarController.RearAxle) {

                customizationData.cambersRear = CarController.RearAxle.leftWheelCollider.camber;
                customizationData.suspensionTargetRear = CarController.RearAxle.leftWheelCollider.WheelCollider.suspensionSpring.targetPosition;
                customizationData.suspensionDistanceRear = CarController.RearAxle.leftWheelCollider.WheelCollider.suspensionDistance;
                customizationData.suspensionSpringForceRear = CarController.RearAxle.leftWheelCollider.WheelCollider.suspensionSpring.spring;
                customizationData.suspensionDamperRear = CarController.RearAxle.leftWheelCollider.WheelCollider.suspensionSpring.damper;

            }

            if (CarController.Lights) {

                if (CarController.Lights.Headlights != null && CarController.Lights.Headlights.Length >= 1 && CarController.Lights.Headlights[0] != null)
                    customizationData.headlightColor = CarController.Lights.Headlights[0].GetLightColor();

            }

            if (CarController.Particles) {

                if (CarController.Particles.wheelParticles != null && CarController.Particles.wheelParticles.Length >= 1) {

                    for (int i = 0; i < CarController.Particles.wheelParticles.Length; i++) {

                        ParticleSystem.MainModule psmain = CarController.Particles.wheelParticles[i].allWheelParticles[0].main;
                        customizationData.wheelSmokeColor = psmain.startColor.color;

                    }

                }

            }

            customizationData.initialized = true;

        }

        //  Apply customization data to the vehicle.
        SetHeadlightsColor(customizationData.headlightColor);
        SetSmokeColor(customizationData.wheelSmokeColor);
        SetFrontCambers(customizationData.cambersFront);
        SetRearCambers(customizationData.cambersRear);
        SetFrontSuspensionsTargetPos(customizationData.suspensionTargetFront);
        SetRearSuspensionsTargetPos(customizationData.suspensionTargetRear);
        SetFrontSuspensionsDistances(customizationData.suspensionDistanceFront);
        SetRearSuspensionsDistances(customizationData.suspensionDistanceRear);
        SetFrontSuspensionsSpringForce(customizationData.suspensionSpringForceFront);
        SetRearSuspensionsSpringForce(customizationData.suspensionSpringForceRear);
        SetFrontSuspensionsSpringDamper(customizationData.suspensionDamperFront);
        SetRearSuspensionsSpringDamper(customizationData.suspensionDamperRear);

    }

    /// <summary>
    /// Set Smoke Color.
    /// </summary>
    public void SetSmokeColor(Color color) {

        if (!CarController.Particles)
            return;

        if (CarController.Particles.wheelParticles == null)
            return;

        for (int i = 0; i < CarController.Particles.wheelParticles.Length; i++) {

            if (CarController.Particles.wheelParticles[i] != null) {

                //  And setting color of the particles.
                foreach (ParticleSystem wheelParticle in CarController.Particles.wheelParticles[i].allWheelParticles) {

                    ParticleSystem.MainModule psmain = wheelParticle.main;
                    color.a = .35f;
                    psmain.startColor = color;

                }

            }

        }

        customizationData.wheelSmokeColor = color;

        Refresh(this);

        if (CarController.Customizer.autoSave)
            Save();

    }

    /// <summary>
    /// Set Headlights Color.
    /// </summary>
    public void SetHeadlightsColor(Color color) {

        if (!CarController.Lights)
            return;

        if (CarController.Lights.lights == null)
            return;

        ////  Enabling headlights.
        //CarController.Lights.lowBeamHeadlights = true;

        //  If light is headlight, set color.
        foreach (RCCP_Light l in CarController.Lights.lights) {

            if (l != null) {

                if (l.lightType == RCCP_Light.LightType.Headlight_LowBeam || l.lightType == RCCP_Light.LightType.Headlight_HighBeam)
                    l.SetLightColor(color);

            }

        }

        customizationData.headlightColor = color;

        Refresh(this);

        if (CarController.Customizer.autoSave)
            Save();

    }

    /// <summary>
    /// Set Front Wheel Cambers.
    /// </summary>
    public void SetFrontCambers(float camberAngle) {

        if (!CarController.FrontAxle)
            return;

        if (CarController.FrontAxle.leftWheelCollider)
            CarController.FrontAxle.leftWheelCollider.camber = camberAngle;

        if (CarController.FrontAxle.rightWheelCollider)
            CarController.FrontAxle.rightWheelCollider.camber = camberAngle;

        customizationData.cambersFront = camberAngle;

        Refresh(this);

        if (CarController.Customizer.autoSave)
            Save();

    }

    /// <summary>
    /// Set Rear Wheel Cambers.
    /// </summary>
    public void SetRearCambers(float camberAngle) {

        if (!CarController.RearAxle)
            return;

        if (CarController.RearAxle.leftWheelCollider)
            CarController.RearAxle.leftWheelCollider.camber = camberAngle;

        if (CarController.RearAxle.rightWheelCollider)
            CarController.RearAxle.rightWheelCollider.camber = camberAngle;

        customizationData.cambersRear = camberAngle;

        Refresh(this);

        if (CarController.Customizer.autoSave)
            Save();

    }

    /// <summary>
    /// Set Front Suspension targetPositions. It changes targetPosition of the front WheelColliders.
    /// </summary>
    public void SetFrontSuspensionsTargetPos(float targetPosition) {

        if (!CarController.FrontAxle)
            return;

        //  Sets target position.
        targetPosition = Mathf.Clamp01(targetPosition);

        if (CarController.FrontAxle.leftWheelCollider) {

            JointSpring spring = CarController.FrontAxle.leftWheelCollider.WheelCollider.suspensionSpring;
            spring.targetPosition = 1f - targetPosition;

            CarController.FrontAxle.leftWheelCollider.WheelCollider.suspensionSpring = spring;

        }

        if (CarController.FrontAxle.rightWheelCollider) {

            JointSpring spring = CarController.FrontAxle.rightWheelCollider.WheelCollider.suspensionSpring;
            spring.targetPosition = 1f - targetPosition;

            CarController.FrontAxle.rightWheelCollider.WheelCollider.suspensionSpring = spring;

        }

        customizationData.suspensionTargetFront = targetPosition;

        Refresh(this);

        if (CarController.Customizer.autoSave)
            Save();

    }

    /// <summary>
    /// Set Rear Suspension targetPositions. It changes targetPosition of the rear WheelColliders.
    /// </summary>
    public void SetRearSuspensionsTargetPos(float targetPosition) {

        if (!CarController.RearAxle)
            return;

        //  Sets target position.
        targetPosition = Mathf.Clamp01(targetPosition);

        if (CarController.RearAxle.leftWheelCollider) {

            JointSpring spring = CarController.RearAxle.leftWheelCollider.WheelCollider.suspensionSpring;
            spring.targetPosition = 1f - targetPosition;

            CarController.RearAxle.leftWheelCollider.WheelCollider.suspensionSpring = spring;

        }

        if (CarController.RearAxle.rightWheelCollider) {

            JointSpring spring = CarController.RearAxle.rightWheelCollider.WheelCollider.suspensionSpring;
            spring.targetPosition = 1f - targetPosition;

            CarController.RearAxle.rightWheelCollider.WheelCollider.suspensionSpring = spring;

        }

        customizationData.suspensionTargetRear = targetPosition;

        Refresh(this);

        if (CarController.Customizer.autoSave)
            Save();

    }

    /// <summary>
    /// Set All Suspension targetPositions. It changes targetPosition of the all WheelColliders.
    /// </summary>
    public void SetAllSuspensionsTargetPos(float targetPosition) {

        SetFrontSuspensionsTargetPos(targetPosition);
        SetRearSuspensionsTargetPos(targetPosition);

    }

    /// <summary>
    /// Set Front Suspension Distances.
    /// </summary>
    public void SetFrontSuspensionsDistances(float distance) {

        //  Make sure new distance is not close to 0.
        if (distance <= .01)
            distance = .05f;

        if (!CarController.FrontAxle)
            return;

        //  Setting suspension distance of front wheelcolliders.
        if (CarController.FrontAxle.leftWheelCollider)
            CarController.FrontAxle.leftWheelCollider.WheelCollider.suspensionDistance = distance;

        if (CarController.FrontAxle.rightWheelCollider)
            CarController.FrontAxle.rightWheelCollider.WheelCollider.suspensionDistance = distance;

        customizationData.suspensionDistanceFront = distance;

        Refresh(this);

        if (CarController.Customizer.autoSave)
            Save();

    }

    /// <summary>
    /// Set Rear Suspension Distances.
    /// </summary>
    public void SetRearSuspensionsDistances(float distance) {

        //  Make sure new distance is not close to 0.
        if (distance <= .01)
            distance = .05f;

        if (!CarController.RearAxle)
            return;

        //  Setting suspension distance of front wheelcolliders.
        if (CarController.RearAxle.leftWheelCollider)
            CarController.RearAxle.leftWheelCollider.WheelCollider.suspensionDistance = distance;

        if (CarController.RearAxle.rightWheelCollider)
            CarController.RearAxle.rightWheelCollider.WheelCollider.suspensionDistance = distance;

        customizationData.suspensionDistanceRear = distance;

        Refresh(this);

        if (CarController.Customizer.autoSave)
            Save();

    }

    /// <summary>
    /// Set Front Suspension Spring Force.
    /// </summary>
    public void SetFrontSuspensionsSpringForce(float targetValue) {

        if (!CarController.FrontAxle)
            return;

        if (!CarController.FrontAxle.leftWheelCollider || !CarController.FrontAxle.rightWheelCollider)
            return;

        JointSpring spring = CarController.FrontAxle.leftWheelCollider.WheelCollider.suspensionSpring;
        spring.spring = targetValue;
        CarController.FrontAxle.leftWheelCollider.WheelCollider.suspensionSpring = spring;
        CarController.FrontAxle.rightWheelCollider.WheelCollider.suspensionSpring = spring;

        customizationData.suspensionSpringForceFront = targetValue;

        Refresh(this);

        if (CarController.Customizer.autoSave)
            Save();

    }

    /// <summary>
    /// Set Rear Suspension Spring Force.
    /// </summary>
    public void SetRearSuspensionsSpringForce(float targetValue) {

        if (!CarController.RearAxle)
            return;

        if (!CarController.RearAxle.leftWheelCollider || !CarController.RearAxle.rightWheelCollider)
            return;

        JointSpring spring = CarController.RearAxle.leftWheelCollider.WheelCollider.suspensionSpring;
        spring.spring = targetValue;
        CarController.RearAxle.leftWheelCollider.WheelCollider.suspensionSpring = spring;
        CarController.RearAxle.rightWheelCollider.WheelCollider.suspensionSpring = spring;

        customizationData.suspensionSpringForceRear = targetValue;

        Refresh(this);

        if (CarController.Customizer.autoSave)
            Save();

    }

    /// <summary>
    /// Set Front Suspension Spring Damper.
    /// </summary>
    public void SetFrontSuspensionsSpringDamper(float targetValue) {

        if (!CarController.FrontAxle)
            return;

        if (!CarController.FrontAxle.leftWheelCollider || !CarController.FrontAxle.rightWheelCollider)
            return;

        JointSpring spring = CarController.FrontAxle.leftWheelCollider.WheelCollider.suspensionSpring;
        spring.damper = targetValue;
        CarController.FrontAxle.leftWheelCollider.WheelCollider.suspensionSpring = spring;
        CarController.FrontAxle.rightWheelCollider.WheelCollider.suspensionSpring = spring;

        customizationData.suspensionDamperFront = targetValue;

        Refresh(this);

        if (CarController.Customizer.autoSave)
            Save();

    }

    /// <summary>
    /// Set Rear Suspension Spring Damper.
    /// </summary>
    public void SetRearSuspensionsSpringDamper(float targetValue) {

        if (!CarController.RearAxle)
            return;

        if (!CarController.RearAxle.leftWheelCollider || !CarController.RearAxle.rightWheelCollider)
            return;

        JointSpring spring = CarController.RearAxle.leftWheelCollider.WheelCollider.suspensionSpring;
        spring.damper = targetValue;
        CarController.RearAxle.leftWheelCollider.WheelCollider.suspensionSpring = spring;
        CarController.RearAxle.rightWheelCollider.WheelCollider.suspensionSpring = spring;

        customizationData.suspensionDamperRear = targetValue;

        Refresh(this);

        if (CarController.Customizer.autoSave)
            Save();

    }

    /// <summary>
    /// Repair CarController.
    /// </summary>
    public void Repair() {

        if (!CarController.Damage)
            return;

        CarController.Damage.repairNow = true;

    }

    /// <summary>
    /// Restores the settings to default.
    /// </summary>
    public void Restore() {

        if (customizationDataDefault == null)
            return;

        //  Apply customization data to the vehicle.
        SetHeadlightsColor(customizationDataDefault.headlightColor);
        SetSmokeColor(customizationDataDefault.wheelSmokeColor);
        SetFrontCambers(customizationDataDefault.cambersFront);
        SetRearCambers(customizationDataDefault.cambersRear);
        SetFrontSuspensionsTargetPos(customizationDataDefault.suspensionTargetFront);
        SetRearSuspensionsTargetPos(customizationDataDefault.suspensionTargetRear);
        SetFrontSuspensionsDistances(customizationDataDefault.suspensionDistanceFront);
        SetRearSuspensionsDistances(customizationDataDefault.suspensionDistanceRear);
        SetFrontSuspensionsSpringForce(customizationDataDefault.suspensionSpringForceFront);
        SetRearSuspensionsSpringForce(customizationDataDefault.suspensionSpringForceRear);
        SetFrontSuspensionsSpringDamper(customizationDataDefault.suspensionDamperFront);
        SetRearSuspensionsSpringDamper(customizationDataDefault.suspensionDamperRear);

        customizationData = new RCCP_CustomizationData();
        customizationData = Loadout.customizationData;

        //  If data is new, get customization data from the vehicle.
        if (!customizationData.initialized) {

            if (CarController.FrontAxle) {

                customizationData.cambersFront = CarController.FrontAxle.leftWheelCollider.camber;
                customizationData.suspensionTargetFront = CarController.FrontAxle.leftWheelCollider.WheelCollider.suspensionSpring.targetPosition;
                customizationData.suspensionDistanceFront = CarController.FrontAxle.leftWheelCollider.WheelCollider.suspensionDistance;
                customizationData.suspensionSpringForceFront = CarController.FrontAxle.leftWheelCollider.WheelCollider.suspensionSpring.spring;
                customizationData.suspensionDamperFront = CarController.FrontAxle.leftWheelCollider.WheelCollider.suspensionSpring.damper;

            }

            if (CarController.RearAxle) {

                customizationData.cambersRear = CarController.RearAxle.leftWheelCollider.camber;
                customizationData.suspensionTargetRear = CarController.RearAxle.leftWheelCollider.WheelCollider.suspensionSpring.targetPosition;
                customizationData.suspensionDistanceRear = CarController.RearAxle.leftWheelCollider.WheelCollider.suspensionDistance;
                customizationData.suspensionSpringForceRear = CarController.RearAxle.leftWheelCollider.WheelCollider.suspensionSpring.spring;
                customizationData.suspensionDamperRear = CarController.RearAxle.leftWheelCollider.WheelCollider.suspensionSpring.damper;

            }

            if (CarController.Lights && CarController.Lights.Headlights != null && CarController.Lights.Headlights.Length > 0) {

                if (CarController.Lights.Headlights[0] != null) {

                    for (int i = 0; i < CarController.Lights.Headlights.Length; i++)
                        customizationData.headlightColor = CarController.Lights.Headlights[i].GetLightColor();

                }

            }

            if (CarController.Particles) {

                if (CarController.Particles.wheelParticles != null && CarController.Particles.wheelParticles.Length > 0) {

                    for (int i = 0; i < CarController.Particles.wheelParticles.Length; i++) {

                        ParticleSystem.MainModule psmain = CarController.Particles.wheelParticles[i].allWheelParticles[0].main;
                        customizationData.wheelSmokeColor = psmain.startColor.color;

                    }

                }

            }

            customizationData.initialized = true;

        }

    }

}
