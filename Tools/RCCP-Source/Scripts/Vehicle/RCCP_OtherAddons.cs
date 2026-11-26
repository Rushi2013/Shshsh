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
/// Other addons belongs to the vehicle, such as nos, dashboard, interior, cameras, exhaust, AI, recorder, attacher, etc...
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Other Addons/RCCP Other Addons")]
public class RCCP_OtherAddons : RCCP_Component {

    /// <summary>
    /// NOS.
    /// </summary>
    public RCCP_Nos Nos {

        get {

            if (_nos == null)
                _nos = RCCP_TryGetComponentInChildren.Get<RCCP_Nos>(transform);

            return _nos;

        }
        set {

            _nos = value;

        }

    }

    /// <summary>
    /// Visual dashboard.
    /// </summary>
    public RCCP_Visual_Dashboard Dashboard {

        get {

            if (_dashboard == null)
                _dashboard = RCCP_TryGetComponentInChildren.Get<RCCP_Visual_Dashboard>(transform);

            return _dashboard;

        }
        set {

            _dashboard = value;

        }

    }

    /// <summary>
    /// Hood and wheel cameras.
    /// </summary>
    public RCCP_Exterior_Cameras ExteriorCameras {

        get {

            if (_exteriorCameras == null)
                _exteriorCameras = RCCP_TryGetComponentInChildren.Get<RCCP_Exterior_Cameras>(transform);

            return _exteriorCameras;

        }
        set {

            _exteriorCameras = value;

        }

    }

    /// <summary>
    /// Exhausts.
    /// </summary>
    public RCCP_Exhausts Exhausts {

        get {

            if (_exhausts == null)
                _exhausts = RCCP_TryGetComponentInChildren.Get<RCCP_Exhausts>(transform);

            return _exhausts;

        }
        set {

            _exhausts = value;

        }

    }

    /// <summary>
    /// AI.
    /// </summary>
    public RCCP_AI AI {

        get {

            if (_AI == null)
                _AI = RCCP_TryGetComponentInChildren.Get<RCCP_AI>(transform);

            return _AI;

        }
        set {

            _AI = value;

        }

    }

    /// <summary>
    /// Recorder.
    /// </summary>
    public RCCP_Recorder Recorder {

        get {

            if (_recorder == null)
                _recorder = RCCP_TryGetComponentInChildren.Get<RCCP_Recorder>(transform);

            return _recorder;

        }
        set {

            _recorder = value;

        }

    }

    /// <summary>
    /// Trail attacher.
    /// </summary>
    public RCCP_TrailerAttacher TrailAttacher {

        get {

            if (_trailerAttacher == null)
                _trailerAttacher = RCCP_TryGetComponentInChildren.Get<RCCP_TrailerAttacher>(transform);

            return _trailerAttacher;

        }
        set {

            _trailerAttacher = value;

        }

    }

    /// <summary>
    /// Limiter.
    /// </summary>
    public RCCP_Limiter Limiter {

        get {

            if (_limiter == null)
                _limiter = RCCP_TryGetComponentInChildren.Get<RCCP_Limiter>(transform);

            return _limiter;

        }
        set {

            _limiter = value;

        }

    }

    /// <summary>
    /// Wheel Blur.
    /// </summary>
    public RCCP_WheelBlur WheelBlur {

        get {

            if (_wheelBlur == null)
                _wheelBlur = RCCP_TryGetComponentInChildren.Get<RCCP_WheelBlur>(transform);

            return _wheelBlur;

        }
        set {

            _wheelBlur = value;

        }

    }

    /// <summary>
    /// Fuel Tank.
    /// </summary>
    public RCCP_FuelTank FuelTank {

        get {

            if (_fuelTank == null)
                _fuelTank = RCCP_TryGetComponentInChildren.Get<RCCP_FuelTank>(transform);

            return _fuelTank;

        }
        set {

            _fuelTank = value;

        }

    }

    /// <summary>
    /// Body Tilt.
    /// </summary>
    public RCCP_BodyTilt BodyTilt {

        get {

            if (_bodyTilt == null)
                _bodyTilt = RCCP_TryGetComponentInChildren.Get<RCCP_BodyTilt>(transform);

            return _bodyTilt;

        }
        set {

            _bodyTilt = value;

        }

    }

    private RCCP_Nos _nos;
    private RCCP_Visual_Dashboard _dashboard;
    private RCCP_Exterior_Cameras _exteriorCameras;
    private RCCP_Exhausts _exhausts;
    private RCCP_AI _AI;
    private RCCP_Recorder _recorder;
    private RCCP_TrailerAttacher _trailerAttacher;
    private RCCP_Limiter _limiter;
    private RCCP_WheelBlur _wheelBlur;
    private RCCP_FuelTank _fuelTank;
    private RCCP_BodyTilt _bodyTilt;

}
