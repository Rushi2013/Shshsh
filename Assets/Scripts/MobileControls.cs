using UnityEngine;
using UnityEngine.UI;

namespace CityDrive
{
    /// <summary>
    /// Professional mobile driving HUD: a real circular steering wheel (angle based,
    /// self-centering), large independent gas/brake/drift buttons with smooth press
    /// scaling and brake priority, and a safe-area-aware responsive canvas.
    /// </summary>
    public sealed class MobileControls : MonoBehaviour
    {
        public ArcadeCarController Car;
        public ChaseCamera Chase;

        [Header("Steering wheel")]
        [SerializeField] private float maxWheelVisualDegrees = 135f;
        [SerializeField] private float steerReturnSpeed = 2.4f;
        [SerializeField] private float steerResponse = 12f;
        [SerializeField] private float gestureSensitivity = 85f;

        [Header("Pedals")]
        [SerializeField] private float pedalResponse = 7f;
        [SerializeField] private float pressedScale = 1.26f;

        public float Steer { get; private set; }
        public float Throttle { get; private set; }
        public float Brake { get; private set; }
        public float Handbrake { get; private set; }

        private RectTransform _wheel;
        private RectTransform _gasIcon;
        private RectTransform _brakeIcon;
        private RectTransform _driftIcon;
        private Text _speedText;

        private int _steerTouchId = -1;
        private float _lastSteerAngle;
        private float _steerTarget;
        private float _throttleSmoothed;
        private float _brakeSmoothed;
        private float _handbrakeSmoothed;

        private const float GasMinX = 0.70f;
        private const float GasMinY = 0.29f;
        private const float GasMaxY = 0.48f;
        private const float BrakeMinY = 0.07f;
        private const float BrakeMaxY = 0.27f;
        private const float DriftMinX = 0.38f;
        private const float DriftMaxX = 0.62f;
        private const float DriftMaxY = 0.18f;

        private void Awake()
        {
            BuildHud();
        }

        private void BuildHud()
        {
            GameObject canvasObject = new GameObject("Touch HUD", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Safe area: keep every control away from notches and rounded corners.
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            Rect safe = Screen.safeArea;
            canvasRect.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            canvasRect.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;

            Image wheelImage = UiTools.MakeImage(canvasObject.transform, "Steering Wheel",
                new Vector2(0.17f, 0.24f), new Vector2(0.17f, 0.24f),
                Color.white, UiTools.GetSprite("UI/icon_steer"));
            _wheel = wheelImage.rectTransform;
            _wheel.sizeDelta = new Vector2(340f, 340f); // square: never stretched into an oval

            Image gas = UiTools.MakeImage(canvasObject.transform, "Gas Pedal",
                new Vector2(0.70f, 0.31f), new Vector2(0.98f, 0.48f),
                Color.white, UiTools.GetSprite("UI/icon_gas"));
            _gasIcon = gas.rectTransform;

            Image brake = UiTools.MakeImage(canvasObject.transform, "Brake Pedal",
                new Vector2(0.70f, 0.09f), new Vector2(0.98f, 0.26f),
                Color.white, UiTools.GetSprite("UI/icon_brake"));
            _brakeIcon = brake.rectTransform;

            Image drift = UiTools.MakeImage(canvasObject.transform, "Drift Button",
                new Vector2(0.40f, 0.015f), new Vector2(0.60f, 0.15f),
                Color.white, UiTools.GetSprite("UI/icon_drift"));
            _driftIcon = drift.rectTransform;

            UiTools.MakeImage(canvasObject.transform, "Camera Button",
                new Vector2(0.86f, 0.86f), new Vector2(0.98f, 0.97f),
                Color.white, UiTools.GetSprite("UI/icon_camera"));
            UiTools.MakeImage(canvasObject.transform, "Reset Button",
                new Vector2(0.02f, 0.86f), new Vector2(0.14f, 0.97f),
                Color.white, UiTools.GetSprite("UI/icon_reset"));

            UiTools.MakeImage(canvasObject.transform, "Speedometer",
                new Vector2(0.39f, 0.82f), new Vector2(0.61f, 0.98f),
                Color.white, UiTools.GetSprite("UI/speed_gauge"));
            _speedText = UiTools.MakeText(canvasObject.transform, "Speed", "0 km/h", 34,
                new Vector2(0.41f, 0.84f), new Vector2(0.59f, 0.92f),
                TextAnchor.MiddleCenter);
        }

        private Vector2 WheelCentreScreen()
        {
            if (_wheel == null) return Vector2.zero;
            return RectTransformUtility.WorldToScreenPoint(null, _wheel.position);
        }

        private float WheelRadiusScreen()
        {
            if (_wheel == null) return 0f;
            return _wheel.rect.width * 0.5f * Mathf.Max(0.01f, _wheel.lossyScale.x);
        }

        private bool InWheelZone(Vector2 screenPoint)
        {
            Vector2 centre = WheelCentreScreen();
            Vector2 delta = screenPoint - centre;
            float radius = WheelRadiusScreen() * 1.15f;
            return delta.x * delta.x + delta.y * delta.y <= radius * radius;
        }

        private float AngleAroundWheel(Vector2 screenPoint)
        {
            Vector2 delta = screenPoint - WheelCentreScreen();
            return Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        }

        private void Update()
        {
            bool gasPressed = false;
            bool brakePressed = false;
            bool driftPressed = false;
            bool keyboardSteer = false;
            float keyboardSteerValue = 0f;

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                Vector2 n = new Vector2(touch.position.x / Screen.width,
                    touch.position.y / Screen.height);

                if (InWheelZone(touch.position))
                {
                    if (touch.phase == TouchPhase.Began || touch.fingerId != _steerTouchId)
                    {
                        _steerTouchId = touch.fingerId;
                        _lastSteerAngle = AngleAroundWheel(touch.position);
                    }
                    else if (touch.fingerId == _steerTouchId)
                    {
                        float angle = AngleAroundWheel(touch.position);
                        _steerTarget = Mathf.Clamp(
                            _steerTarget + Mathf.DeltaAngle(_lastSteerAngle, angle) / gestureSensitivity,
                            -1f, 1f);
                        _lastSteerAngle = angle;
                    }
                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                        _steerTouchId = -1;
                    continue;
                }

                if (n.x >= GasMinX && n.y >= GasMinY && n.y <= GasMaxY) gasPressed = true;
                if (n.x >= GasMinX && n.y >= BrakeMinY && n.y <= BrakeMaxY) brakePressed = true;
                if (n.x >= DriftMinX && n.x <= DriftMaxX && n.y <= DriftMaxY) driftPressed = true;
                if (touch.phase == TouchPhase.Began && n.x >= 0.84f && n.y >= 0.85f && Chase != null)
                    Chase.CycleMode();
                if (touch.phase == TouchPhase.Began && n.x <= 0.16f && n.y >= 0.85f && Car != null)
                    Car.ResetCar();
            }

            if (Input.touchCount == 0)
            {
                Vector2 mouse = Input.mousePosition;
                Vector2 n = new Vector2(mouse.x / Screen.width, mouse.y / Screen.height);
                if (Input.GetMouseButton(0))
                {
                    if (InWheelZone(mouse))
                    {
                        float angle = AngleAroundWheel(mouse);
                        if (Input.GetMouseButtonDown(0)) _lastSteerAngle = angle;
                        _steerTarget = Mathf.Clamp(
                            _steerTarget + Mathf.DeltaAngle(_lastSteerAngle, angle) / gestureSensitivity,
                            -1f, 1f);
                        _lastSteerAngle = angle;
                    }
                    else
                    {
                        if (n.x >= GasMinX && n.y >= GasMinY && n.y <= GasMaxY) gasPressed = true;
                        if (n.x >= GasMinX && n.y >= BrakeMinY && n.y <= BrakeMaxY) brakePressed = true;
                        if (n.x >= DriftMinX && n.x <= DriftMaxX && n.y <= DriftMaxY) driftPressed = true;
                    }
                }
                if (Input.GetMouseButtonDown(0))
                {
                    if (n.x >= 0.84f && n.y >= 0.85f && Chase != null) Chase.CycleMode();
                    if (n.x <= 0.16f && n.y >= 0.85f && Car != null) Car.ResetCar();
                }

                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                {
                    keyboardSteer = true;
                    keyboardSteerValue = -1f;
                }
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                {
                    keyboardSteer = true;
                    keyboardSteerValue = 1f;
                }
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) gasPressed = true;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) brakePressed = true;
                if (Input.GetKey(KeyCode.Space)) driftPressed = true;
                if (Input.GetKeyDown(KeyCode.C) && Chase != null) Chase.CycleMode();
                if (Input.GetKeyDown(KeyCode.R) && Car != null) Car.ResetCar();
            }

            if (keyboardSteer)
                _steerTarget = Mathf.MoveTowards(_steerTarget, keyboardSteerValue, Time.unscaledDeltaTime * 3f);
            else if (_steerTouchId < 0)
                _steerTarget = Mathf.MoveTowards(_steerTarget, 0f, Time.unscaledDeltaTime * steerReturnSpeed);

            Steer = Mathf.MoveTowards(Steer, _steerTarget, Time.unscaledDeltaTime * steerResponse);

            // Brake always overrides the accelerator.
            float throttleTarget = gasPressed && !brakePressed ? 1f : 0f;
            float brakeTarget = brakePressed ? 1f : 0f;
            float driftTarget = driftPressed ? 1f : 0f;
            _throttleSmoothed = Mathf.MoveTowards(_throttleSmoothed, throttleTarget, Time.unscaledDeltaTime * pedalResponse);
            _brakeSmoothed = Mathf.MoveTowards(_brakeSmoothed, brakeTarget, Time.unscaledDeltaTime * pedalResponse);
            _handbrakeSmoothed = Mathf.MoveTowards(_handbrakeSmoothed, driftTarget, Time.unscaledDeltaTime * pedalResponse);

            Throttle = _throttleSmoothed;
            Brake = _brakeSmoothed;
            Handbrake = _handbrakeSmoothed;

            if (Car != null)
            {
                Car.SteerInput = Steer;
                Car.ThrottleInput = Throttle;
                Car.BrakeInput = Brake;
                Car.HandbrakeInput = Handbrake;
            }

            if (_wheel != null)
            {
                _wheel.localEulerAngles = new Vector3(0f, 0f, -Steer * maxWheelVisualDegrees);
                float targetScale = _steerTouchId >= 0 ? 1.06f : 1f;
                _wheel.localScale = Vector3.Lerp(_wheel.localScale, Vector3.one * targetScale,
                    Time.unscaledDeltaTime * 10f);
            }
            ScaleIcon(_gasIcon, throttleTarget > 0f);
            ScaleIcon(_brakeIcon, brakeTarget > 0f);
            ScaleIcon(_driftIcon, driftTarget > 0f);

            if (_speedText != null && Car != null)
            {
                int kph = Mathf.RoundToInt(Car.SpeedKph);
                string gear = Car.CurrentGear + (Brake > 0.2f && Car.SpeedKph < 5f ? "R" : "");
                _speedText.text = kph + " km/h  " + gear;
            }
        }

        private void ScaleIcon(RectTransform icon, bool pressed)
        {
            if (icon == null) return;
            float target = pressed ? pressedScale : 1f;
            icon.localScale = Vector3.Lerp(icon.localScale, Vector3.one * target,
                Time.unscaledDeltaTime * 14f);
        }
    }
}
