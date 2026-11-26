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
using UnityEngine.Rendering;
using System.Threading.Tasks;

/// <summary>
/// Light of the vehicle with different types such as headlight, brakelight, turnlight, reverselight, taillight, etc...
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Addons/RCCP Light")]
public class RCCP_Light : RCCP_Component {

    private Light _lightSource;

    /// <summary>
    /// Light source.
    /// </summary>
    private Light LightSource {

        get {

            if (_lightSource == null)
                _lightSource = GetComponent<Light>();

            return _lightSource;

        }

    }

    /// <summary>
    /// Light types and renderer mode.
    /// </summary>
    public enum LightType { Headlight_LowBeam, Headlight_HighBeam, Brakelight, Taillight, Reverselight, IndicatorLeftLight, IndicatorRightLight }
    public LightType lightType = LightType.Headlight_LowBeam;

    /// <summary>
    /// Pixel light on auto / force pixel mode. Vertex light mode on force vertex.
    /// </summary>
    public LightRenderMode lightRendererMode = LightRenderMode.Auto;
    public bool overrideRenderMode = false;
    [Space()]
    /// <summary>
    /// Intensity of the light.
    /// </summary>
    [Space()]
    [Range(.1f, 10f)] public float intensity = 1f;

    /// <summary>
    /// Smoothness of the intensity.
    /// </summary>
    [Range(.1f, 1f)] public float smoothness = .5f;

    /// <summary>
    /// Emissive renderer.
    /// </summary>
    [Space()] public MeshRenderer emissiveRenderer;

    /// <summary>
    /// Target material index in the emissive renderer.
    /// </summary>
    [Min(0)] public int emissiveMaterialIndex = 0;

    /// <summary>
    /// Emissive material.
    /// </summary>
    private Material emissiveMaterial;

    /// <summary>
    /// Target color for the emissive.
    /// </summary>
    [Space()] public Color emissiveColor = Color.white;

    /// <summary>
    /// Shader keyword to enable emissive.
    /// </summary>
    public string shaderKeywordEmissionEnable = "_EMISSION";

    /// <summary>
    /// Shader keyword to set color of the emissive.
    /// </summary>
    public string shaderKeywordEmissionColor = "_EmissionColor";

    [System.Serializable]
    public class RCCP_EmissiveRenderer {

        /// <summary>
        /// Emissive renderer.
        /// </summary>
        public MeshRenderer emissiveRenderer;

        /// <summary>
        /// Target material index in the emissive renderer.
        /// </summary>
        [Min(0)] public int emissiveMaterialIndex = 0;

        /// <summary>
        /// Emissive material.
        /// </summary>
        [Space()] public Material emissiveMaterial;

        /// <summary>
        /// Target color for the emissive.
        /// </summary>
        public Color emissiveColor = Color.white;

        /// <summary>
        /// Shader keyword to enable emissive.
        /// </summary>
        public string shaderKeywordEmissionEnable = "_EMISSION";

        /// <summary>
        /// Shader keyword to set color of the emissive.
        /// </summary>
        public string shaderKeywordEmissionColor = "_EmissionColor";

    }

    /// <summary>
    /// Additional emissive renderers.
    /// </summary>
    public RCCP_EmissiveRenderer[] additionalEmissiveRenderers = new RCCP_EmissiveRenderer[0];

    /// <summary>
    /// Use the lens flares. Builtin pipeline will be using "LensFlares" component, and other pipelines will be using "LensFlare (SRP)" component.
    /// </summary>
    [Space()] public bool useLensFlares = true;

#if !BCG_URP

    /// <summary>
    /// Lens flare.
    /// </summary>
    private LensFlare lensFlare;

#else

    /// <summary>
    /// SRP Lens flare for URP / HDRP.
    /// </summary>
    private LensFlareComponentSRP lensFlare_SRP;

#endif

    /// <summary>
    /// Max flare brigthness of the light.
    /// </summary>
    [Range(0f, 10f)] public float flareBrightness = 1.5f;

    /// <summary>
    /// Calculated final flare brightness of the light.
    /// </summary>
    private float finalFlareBrightness = 0f;

    /// <summary>
    /// Can it break at certain damage?
    /// </summary>
    [Space()] public bool isBreakable = true;

    /// <summary>
    /// Strength of the light. 
    /// </summary>
    public float strength = 100f;

    /// <summary>
    /// Light will be broken at this point.
    /// </summary>
    public int breakPoint = 35;

    /// <summary>
    /// Original strength of the light. We will be using this original value while restoring the light.
    /// </summary>
    private float orgStrength = 100f;

    /// <summary>
    /// Is this light broken currently?
    /// </summary>
    public bool broken = false;

    /// <summary>
    /// Main camera will be used to calculate the lens flare strength and intensity.
    /// </summary>
    private Camera mainCam;

    public override void Start() {

        base.Start();

        if (useLensFlares) {

#if !BCG_URP

            //  Getting light and lens flare components.
            lensFlare = GetComponent<LensFlare>();

#else

            lensFlare_SRP = GetComponent<LensFlareComponentSRP>();

#endif

        }

        orgStrength = strength;

        //  Setting render mode of the light.
        if (!overrideRenderMode) {

            switch (lightType) {

                case LightType.Headlight_LowBeam:

                    if (RCCPSettings.useHeadLightsAsVertexLights)
                        lightRendererMode = LightRenderMode.ForceVertex;
                    else
                        lightRendererMode = LightRenderMode.ForcePixel;

                    break;

                case LightType.Brakelight:
                case LightType.Taillight:

                    if (RCCPSettings.useBrakeLightsAsVertexLights)
                        lightRendererMode = LightRenderMode.ForceVertex;
                    else
                        lightRendererMode = LightRenderMode.ForcePixel;

                    break;

                case LightType.Reverselight:

                    if (RCCPSettings.useReverseLightsAsVertexLights)
                        lightRendererMode = LightRenderMode.ForceVertex;
                    else
                        lightRendererMode = LightRenderMode.ForcePixel;

                    break;

                case LightType.IndicatorLeftLight:
                case LightType.IndicatorRightLight:

                    if (RCCPSettings.useIndicatorLightsAsVertexLights)
                        lightRendererMode = LightRenderMode.ForceVertex;
                    else
                        lightRendererMode = LightRenderMode.ForcePixel;

                    break;

            }

        }

        InvokeRepeating(nameof(FindMainCamera), 0f, 1f);

    }

    public void FindMainCamera() {

        mainCam = Camera.main;

    }

    private void Update() {

        //  If no light manager found, return.
        if (!CarController.Lights)
            return;

        //  Light types. Intensity of the light will be adjusted with bools. If corresponding bool is enabled,  intensity of the light will be set to target intensity value.
        switch (lightType) {

            case LightType.Headlight_LowBeam:

                if (CarController.Lights.lowBeamHeadlights)
                    Lighting(intensity);
                else
                    Lighting(0f);

                break;

            case LightType.Headlight_HighBeam:

                if (CarController.Lights.highBeamHeadlights) {

                    if (!CarController.Lights.highBeamWithLowBeamOnly)
                        Lighting(intensity);
                    else if (CarController.Lights.highBeamWithLowBeamOnly && CarController.Lights.lowBeamHeadlights)
                        Lighting(intensity);
                    else
                        Lighting(0f);

                } else {

                    Lighting(0f);

                }

                break;

            case LightType.Brakelight:

                float tailIntensity = 0f;

                if (!CarController.Lights.tailLightFound) {

                    if (CarController.Lights.lowBeamHeadlights)
                        tailIntensity = .3f;
                    else
                        tailIntensity = 0f;

                }

                if (CarController.Lights.brakeLights)
                    Lighting(Mathf.Clamp(intensity + tailIntensity, 0f, intensity));
                else
                    Lighting(tailIntensity);

                break;

            case LightType.Taillight:

                if (CarController.Lights.lowBeamHeadlights)
                    Lighting(intensity);
                else
                    Lighting(0f);

                break;

            case LightType.Reverselight:

                if (CarController.Lights.reverseLights)
                    Lighting(intensity);
                else
                    Lighting(0f);

                break;

            case LightType.IndicatorLeftLight:

                if ((CarController.Lights.indicatorsLeft || CarController.Lights.indicatorsAll) && CarController.Lights.indicatorTimer < .5f)
                    Lighting(intensity);
                else
                    Lighting(0f);

                break;

            case LightType.IndicatorRightLight:

                if ((CarController.Lights.indicatorsRight || CarController.Lights.indicatorsAll) && CarController.Lights.indicatorTimer < .5f)
                    Lighting(intensity);
                else
                    Lighting(0f);

                break;

        }

        //  If emissive renderer selected, enable the necessary keyword for the emissive and set color of it.
        if (emissiveRenderer) {

            if (emissiveMaterial == null)
                emissiveMaterial = emissiveRenderer.materials[emissiveMaterialIndex];

            if (emissiveMaterial && shaderKeywordEmissionEnable != "")
                emissiveMaterial.EnableKeyword(shaderKeywordEmissionEnable);        //  Enabling keyword of the material for emission.

            if (emissiveMaterial && shaderKeywordEmissionColor != "")
                emissiveMaterial.SetColor(shaderKeywordEmissionColor, emissiveColor * LightSource.intensity);

        }

        if (additionalEmissiveRenderers != null && additionalEmissiveRenderers.Length >= 1) {

            for (int i = 0; i < additionalEmissiveRenderers.Length; i++) {

                if (additionalEmissiveRenderers[i] != null && additionalEmissiveRenderers[i].emissiveRenderer != null && additionalEmissiveRenderers[i].emissiveMaterialIndex >= 0) {

                    if (additionalEmissiveRenderers[i].emissiveMaterial == null)
                        additionalEmissiveRenderers[i].emissiveMaterial = additionalEmissiveRenderers[i].emissiveRenderer.materials[additionalEmissiveRenderers[i].emissiveMaterialIndex];

                    if (additionalEmissiveRenderers[i].emissiveMaterial && additionalEmissiveRenderers[i].shaderKeywordEmissionEnable != "")
                        additionalEmissiveRenderers[i].emissiveMaterial.EnableKeyword(additionalEmissiveRenderers[i].shaderKeywordEmissionEnable);        //  Enabling keyword of the material for emission.

                    if (additionalEmissiveRenderers[i].emissiveMaterial && additionalEmissiveRenderers[i].shaderKeywordEmissionColor != "")
                        additionalEmissiveRenderers[i].emissiveMaterial.SetColor(additionalEmissiveRenderers[i].shaderKeywordEmissionColor, additionalEmissiveRenderers[i].emissiveColor * LightSource.intensity);

                }

            }

        }

        //  Setting render mode of the light.
        LightSource.renderMode = lightRendererMode;

#if !BCG_URP

        if (lensFlare)
            LensFlare();

#else

        if (lensFlare_SRP)
            LensFlare_SRP();

#endif


    }

    /// <summary>
    /// Adjusts intensity of the light smoothly.
    /// </summary>
    /// <param name="_intensity"></param>
    private void Lighting(float _intensity) {

        //  If light is broken, set it to 0. Otherwise, set it to target intensity.
        if (!broken)
            LightSource.intensity = Mathf.Lerp(LightSource.intensity, _intensity, Time.deltaTime * smoothness * 100f);
        else
            LightSource.intensity = Mathf.Lerp(LightSource.intensity, 0f, Time.deltaTime * smoothness * 100f);

    }

#if !BCG_URP

    /// <summary>
    /// Operating lensflares related to camera angle.
    /// </summary>
    private void LensFlare() {

        //  Return if lens flares are disabled for this light.
        if (!useLensFlares)
            return;

        //  If no main camera found, return.
        if (!mainCam)
            return;

        Vector3 transformPos = transform.position;
        Vector3 transformDir = transform.forward;
        Vector3 camPos = mainCam.transform.position;

        //  Lensflares are not affected by collider of the vehicle. They will ignore it. Below code will calculate the angle of the light-camera, and sets intensity of the lensflare.
        float distanceTocam = Vector3.Distance(transformPos, camPos);
        float angle = Vector3.Angle(transformDir, camPos - transformPos);

        if (!Mathf.Approximately(angle, 0f))
            finalFlareBrightness = flareBrightness * (4f / distanceTocam) * ((300f - (3f * angle)) / 300f) / 3f;
        else
            finalFlareBrightness = flareBrightness;

        if (finalFlareBrightness < 0)
            finalFlareBrightness = 0f;

        lensFlare.brightness = finalFlareBrightness * LightSource.intensity;
        lensFlare.color = LightSource.color;

    }

#else

    /// <summary>
    /// Operating SRP lensflares related to camera angle.
    /// </summary>
    private void LensFlare_SRP() {

        //  Return if lens flares are disabled for this light.
        if (!useLensFlares)
            return;

        //  If no main camera found, return.
        if (!mainCam)
            return;

        //  Lensflares are not affected by collider of the vehicle. They will ignore it. Below code will calculate the angle of the light-camera, and sets intensity of the lensflare.
        float distanceTocam = Vector3.Distance(transform.position, mainCam.transform.position);
        float angle = Vector3.Angle(transform.forward, mainCam.transform.position - transform.position);

        if (!Mathf.Approximately(angle, 0f))
            finalFlareBrightness = flareBrightness * (8f / distanceTocam) * ((300f - (3f * angle)) / 300f) / 3f;
        else
            finalFlareBrightness = flareBrightness;

        if (finalFlareBrightness < 0)
            finalFlareBrightness = 0f;

        lensFlare_SRP.attenuationByLightShape = false;
        lensFlare_SRP.intensity = finalFlareBrightness * LightSource.intensity;

    }

#endif

    /// <summary>
    /// Listening vehicle collisions for braking the light.
    /// </summary>
    /// <param name="impulse"></param>
    public void OnCollision(float impulse) {

        //  Return if component is disabled.
        if (!enabled)
            return;

        // If light is broken, return.
        if (broken)
            return;

        //	Decreasing strength of the light related to collision impulse.
        strength -= impulse * 20f;
        strength = Mathf.Clamp(strength, 0f, Mathf.Infinity);

        //	Check joint of the part based on strength.
        if (strength <= breakPoint)
            broken = true;

    }

    /// <summary>
    /// Repairs, and restores the light.
    /// </summary>
    public void OnRepair() {

        //  Return if component is disabled.
        if (!enabled)
            return;

        strength = orgStrength;
        broken = false;

    }

    /// <summary>
    /// Gets the color of the light.
    /// </summary>
    /// <param name="color"></param>
    public Color GetLightColor() {

        return LightSource.color;

    }

    /// <summary>
    /// Changes the color of the light.
    /// </summary>
    /// <param name="color"></param>
    public void SetLightColor(Color color) {

        LightSource.color = color;

    }

    public void Reload() {

        LightSource.intensity = 0f;
        finalFlareBrightness = 0f;

    }

    private void OnValidate() {

        if (additionalEmissiveRenderers != null && additionalEmissiveRenderers.Length >= 1) {

            for (int i = 0; i < additionalEmissiveRenderers.Length; i++) {

                if (additionalEmissiveRenderers[i] != null) {

                    if (additionalEmissiveRenderers[i].emissiveColor == new Color(0f, 0f, 0f, 0f))
                        additionalEmissiveRenderers[i].emissiveColor = Color.white;

                    if (additionalEmissiveRenderers[i].shaderKeywordEmissionEnable == "")
                        additionalEmissiveRenderers[i].shaderKeywordEmissionEnable = "_EMISSION";

                    if (additionalEmissiveRenderers[i].shaderKeywordEmissionColor == "")
                        additionalEmissiveRenderers[i].shaderKeywordEmissionColor = "_EmissionColor";

                }

            }

        }

    }

}
