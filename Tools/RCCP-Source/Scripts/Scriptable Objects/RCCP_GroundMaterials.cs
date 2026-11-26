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
/// Ground materials for variable ground physics.
/// </summary>
[Icon("Assets/Realistic Car Controller Pro/Resources/Editor Icons/RCCP_EditorIcon_Manager.png")]
[System.Serializable]
public class RCCP_GroundMaterials : ScriptableObject {

    #region singleton
    private static RCCP_GroundMaterials instance;
    public static RCCP_GroundMaterials Instance { get { if (instance == null) instance = Resources.Load("RCCP_GroundMaterials") as RCCP_GroundMaterials; return instance; } }
    #endregion

    [System.Serializable]
    public class GroundMaterialFrictions {

        /// <summary>
        /// Physic material.
        /// </summary>
        public PhysicMaterial groundMaterial;

        /// <summary>
        /// Forward stiffness.
        /// </summary>
        public float forwardStiffness = 1f;

        /// <summary>
        /// Sideways stiffness.
        /// </summary>
        public float sidewaysStiffness = 1f;

        /// <summary>
        /// Target slip limit.
        /// </summary>
        public float slip = .25f;

        /// <summary>
        /// Damp force.
        /// </summary>
        public float damp = 1f;

        /// <summary>
        /// Volume of the ground sound.
        /// </summary>
        [Range(0f, 1f)] public float volume = 1f;

        /// <summary>
        /// Ground particles.
        /// </summary>
        public GameObject groundParticles;

        /// <summary>
        /// Ground audio clip.
        /// </summary>
        public AudioClip groundSound;

        /// <summary>
        /// Skidmarks.
        /// </summary>
        public RCCP_Skidmarks skidmark;

    }

    /// <summary>
    /// Ground materials.
    /// </summary>
    public GroundMaterialFrictions[] frictions;

    /// <summary>
    /// Terrain ground materials.
    /// </summary>
    [System.Serializable]
    public class TerrainFrictions {

        /// <summary>
        /// Physic material.
        /// </summary>
        public PhysicMaterial groundMaterial;

        [System.Serializable]
        public class SplatmapIndexes {

            public int index = 0;

        }

        /// <summary>
        /// Splatmap indexes.
        /// </summary>
        public SplatmapIndexes[] splatmapIndexes;

    }

    /// <summary>
    /// Terrain ground materials.
    /// </summary>
    public TerrainFrictions[] terrainFrictions;

}


