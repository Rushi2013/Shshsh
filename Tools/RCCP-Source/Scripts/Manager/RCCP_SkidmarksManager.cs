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
/// Skidmarks manager all all kind of skidmarks.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Misc/RCCP Skidmarks Manager")]
public class RCCP_SkidmarksManager : RCCP_Singleton<RCCP_SkidmarksManager> {

    /// <summary>
    /// All skidmarks.
    /// </summary>
    private RCCP_Skidmarks[] skidmarks;

    /// <summary>
    /// Index of the skidmarks.
    /// </summary>
    private int[] skidmarksIndexes;

    /// <summary>
    /// Last index of the ground.
    /// </summary>
    private int _lastGroundIndex = 0;

    private void Awake() {

        //  Creating new skidmarks and initializing them with given ground materials in RCCP Ground Materials.
        skidmarks = new RCCP_Skidmarks[RCCPGroundMaterials.frictions.Length];
        skidmarksIndexes = new int[skidmarks.Length];

        for (int i = 0; i < skidmarks.Length; i++) {

            skidmarks[i] = Instantiate(RCCPGroundMaterials.frictions[i].skidmark, Vector3.zero, Quaternion.identity);
            skidmarks[i].transform.name = skidmarks[i].transform.name + "_" + RCCPGroundMaterials.frictions[i].groundMaterial.name;
            skidmarks[i].transform.SetParent(transform, true);

        }

    }

    // Function called by the wheels that is skidding. Gathers all the information needed to
    // create the mesh later. Sets the intensity of the skidmark section b setting the alpha
    // of the vertex color.
    public int AddSkidMark(Vector3 pos, Vector3 normal, float intensity, float width, int lastIndex, int groundIndex) {

        if (_lastGroundIndex != groundIndex) {

            _lastGroundIndex = groundIndex;
            return -1;

        }

        skidmarksIndexes[groundIndex] = skidmarks[groundIndex].AddSkidMark(pos, normal, intensity, width, lastIndex);

        return skidmarksIndexes[groundIndex];

    }

    /// <summary>
    /// Cleans all skidmarks.
    /// </summary>
    public void CleanSkidmarks() {

        for (int i = 0; i < skidmarks.Length; i++)
            skidmarks[i].Clean();

    }

    /// <summary>
    /// Cleans target skidmarks.
    /// </summary>
    /// <param name="index"></param>
    public void CleanSkidmarks(int index) {

        skidmarks[index].Clean();

    }

}
