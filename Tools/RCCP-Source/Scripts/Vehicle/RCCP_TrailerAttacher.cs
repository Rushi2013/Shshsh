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
/// Vehicle and trailer must have trailer attachers. Must be added to the box collider with trigger enabled.
/// When two trailer attachers triggers each other, attachment will be processed.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Other Addons/RCCP Trailer Attacher")]
public class RCCP_TrailerAttacher : RCCP_Component {

    private RCCP_TruckTrailer _trailer;

    /// <summary>
    /// This trailer is connected to this attacher.
    /// </summary>
    public RCCP_TruckTrailer Trailer {

        get {

            if (_trailer == null)
                _trailer = GetComponentInParent<RCCP_TruckTrailer>(true);

            return _trailer;

        }

    }

    /// <summary>
    /// Attached trailer.
    /// </summary>
    public RCCP_TruckTrailer attachedTrailer;

    /// <summary>
    /// This triggger collider.
    /// </summary>
    private BoxCollider trigger;

    private void OnTriggerEnter(Collider col) {

        //  Getting other attacher.
        RCCP_TrailerAttacher otherAttacher = col.gameObject.GetComponent<RCCP_TrailerAttacher>();

        //  If no attacher found, return.
        if (!otherAttacher)
            return;

        //  If no trailer found on the other side, return.
        if (!otherAttacher.Trailer)
            return;

        //  Assigning the attached trailer with the other side.
        attachedTrailer = otherAttacher.Trailer;
        RCCP_CarController carController = GetComponentInParent<RCCP_CarController>();

        //  If this attacher belongs to the vehicle, not trailer, let the other trailer attach to this vehicle.
        if (carController)
            otherAttacher.Trailer.AttachTrailer(carController);

    }

    private void Reset() {

        //  Resetting.
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        transform.localPosition = new Vector3(0f, 0f, -2.75f);
        trigger = GetComponent<BoxCollider>();

        if (!trigger)
            trigger = gameObject.AddComponent<BoxCollider>();

        trigger.isTrigger = true;
        trigger.size = new Vector3(.1f, 1f, .1f);

    }

}
