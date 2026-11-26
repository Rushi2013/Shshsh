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
using UnityEngine.Animations;

/// <summary>
/// Unity Parent Const component.
/// </summary>
public class RCCP_ParentConst : RCCP_GenericComponent {

    private ParentConstraint parentConstraint;

    /// <summary>
    /// Unity Parent Const component.
    /// </summary>
    public ParentConstraint ParentConstraint {

        get {

            if (parentConstraint == null)
                parentConstraint = GetComponent<ParentConstraint>();

            if (parentConstraint == null)
                parentConstraint = gameObject.AddComponent<ParentConstraint>();

            return parentConstraint;

        }

    }

    public void Restore() {

        foreach (Transform item in transform)
            item.SetParent(transform.parent, true);

#if UNITY_EDITOR

        if (UnityEditor.EditorApplication.isPlaying)
            Destroy(gameObject);
        else
            DestroyImmediate(gameObject);

#else

 Destroy(gameObject);

#endif

    }

}
