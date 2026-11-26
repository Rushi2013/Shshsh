//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright © 2014 - 2024 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// All demo materials.
/// </summary>
[Icon("Assets/Realistic Car Controller Pro/Resources/Editor Icons/RCCP_EditorIcon_Manager.png")]
public class RCCP_DemoMaterials : ScriptableObject {

    public int instanceId = 0;

    #region singleton
    private static RCCP_DemoMaterials instance;
    public static RCCP_DemoMaterials Instance { get { if (instance == null) instance = Resources.Load("RCCP_DemoMaterials") as RCCP_DemoMaterials; return instance; } }
    #endregion

    [System.Serializable]
    public class MaterialStructure {

        public Material material;

        public string defaultShader;
        public string DefaultShader {

            get {

                if (defaultShader == "" && material)
                    defaultShader = material.shader.name;

                return defaultShader;

            }
            set {
                defaultShader = value;
            }

        }
    }

    public MaterialStructure[] demoMaterials;

    public Material[] vehicleBodyMaterials;
    public Material[] wheelBlurMaterials;

}
