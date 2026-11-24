using UnityEngine;
using UnityEngine.UI;

namespace CityDrive
{
    /// <summary>
    /// Touch HUD with a real circular steering wheel: drag your finger around the
    /// wheel and the front wheels follow that angle. Gas/brake/drift icons grow while
    /// held; releasing a button immediately removes its input.
    /// </summary>
    public sealed class MobileControls : MonoBehaviour
    {
        public ArcadeCarController Car;
        public ChaseCamera Chase;

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

        private bool _gasPressed;
        private bool _brakePressed;
        private bool _driftPressed;
        private bool _cameraPressed;
        private bool _resetPressed;

        private const float WheelX = 0.17f;
        private const float WheelY = 0.24f;
        private const float GasMinX = 0.70f;
        private const float GasMinY = 0.27f;
        private const float GasMaxY = 0.48f;
        private const float BrakeMinY = 0.06f;
        private const float BrakeMaxY = 0.26f;
        private const float DriftMinX = 0.38f;
        private const float DriftMaxX = 0.62f;
        private const float DriftMaxY = 0.18f;

        private void Awake()
        {
            BuildHud();
        }

        private void BuildHud()
        {
            Font font = null;
            try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (font == null)
            {
                try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
            }

            GameObject canvasObject = new GameObject("Touch HUD", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Circular steering wheel — the sprite itself rotates with the finger.
            Image wheelImage = UiTools.MakeImage(canvasObject.transform, "Steering Wheel",
                new Vector2(WheelX - 0.14f, WheelY - 0.16f),
                new Vector2(WheelX + 0.14f, WheelY + 0.16f),
                Color.white, UiTools.GetSprite("UI/icon_steer"));
            _wheel = wheelImage.rectTransform;

            Image gas = UiTools.MakeImage(canvasObject.transform, "Gas Icon",
                new Vector2(0.70f, 0.30f), new Vector2(0.98f, 0.47f),
                Color.white, UiTools.GetSprite("UI/icon_gas"));
            _gasIcon = gas.rectTransform;

            Image brake = UiTools.MakeImage(canvasObject.transform, "Brake Icon",
                new Vector2(0.70f, 0.08f), new Vector2(0.98f, 0.25f),
                Color.white, UiTools.GetSprite("UI/icon_brake"));
            _brakeIcon = brake.rectTransform;

            Image drift = UiTools.MakeImage(canvasObject.transform, "Drift Icon",
                new Vector2(0.40f, 0.015f), new Vector2(0.60f, 0.15f),
                Color.white, UiTools.GetSprite("UI/icon_drift"));
            _driftIcon = drift.rectTransform;

            UiTools.MakeImage(canvasObject.transform, "Camera Icon",
                new Vector2(0.86f, 0.86f), new Vector2(0.98f, 0.97f),
                Color.white, UiTools.GetSprite("UI/icon_camera"));
            UiTools.MakeImage(canvasObject.transform, "Reset Icon",
                new Vector2(0.02f, 0.86f), new Vector2(0.14f, 0.97f),
                Color.white, UiTools.GetSprite("UI/icon_reset"));

            UiTools.MakeImage(canvasObject.transform, "Speedometer",
                new Vector2(0.39f, 0.82f), new Vector2(0.61f, 0.98f),
                Color.white, UiTools.GetSprite("UI/speed_gauge"));
            _speedText = UiTools.MakeText(canvasObject.transform, "Speed", "0 km/h", 34,
                new Vector2(0.41f, 0.84f), new Vector2(0.59f, 0.92f),
                TextAnchor.MiddleCenter);
        }

        private static float AngleOf(Vector2 point, Vector2 centre)
        {
            return Mathf.Atan2(point.y - centre.y, point.x - centre.x) * Mathf.Rad2Deg;
        }

        private static bool InWheelZone(Vector2 normalized)
        {
            float dx = (normalized.x - WheelX) * Screen.width;
            float dy = (normalized.y - WheelY) * Screen.height;
            float radius = Mathf.Min(Screen.width, Screen.height) * 0.18f;
            return dx * dx + dy * dy <= radius * radius;
        }

        private void HandleSteerTouch(Touch touch, Vector2 normalized)
        {
            if (touch.fingerId != _steerTouchId) return;
            float angle = AngleOf(touch.position,
                new Vector2(WheelX * Screen.width, WheelY * Screen.height));
            float delta = Mathf.DeltaAngle(_lastSteerAngle, angle);
            _steerTarget = Mathf.Clamp(_steerTarget + delta / 85f, -1f, 1f);
            _lastSteerAngle = angle;
        }

        private void Update()
        {
            _gasPressed = false;
            _brakePressed = false;
            _driftPressed = false;
            bool keyboardSteer = false;
            float keyboardSteerValue = 0f;

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                Vector2 n = new Vector2(touch.position.x / Screen.width,
                    touch.position.y / Screen.height);

                if (InWheelZone(n))
                {
                    if (touch.phase == TouchPhase.Began)
                    {
                        _steerTouchId = touch.fingerId;
                        _lastSteerAngle = AngleOf(touch.position,
                            new Vector2(WheelX * Screen.width, WheelY * Screen.height));
                    }
                    HandleSteerTouch(touch, n);
                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                        _steerTouchId = -1;
                    continue;
                }

                if (n.x >= GasMinX && n.y >= GasMinY && n.y <= GasMaxY) _gasPressed = true;
                if (n.x >= GasMinX && n.y >= BrakeMinY && n.y <= BrakeMaxY) _brakePressed = true;
                if (n.x >= DriftMinX && n.x <= DriftMaxX && n.y <= DriftMaxY) _driftPressed = true;
                if (touch.phase == TouchPhase.Began && n.x >= 0.84f && n.y >= 0.85f && Chase != null)
                    Chase.CycleMode();
                if (touch.phase == TouchPhase.Began && n.x <= 0.16f && n.y >= 0.85f && Car != null)
                    Car.ResetCar();
            }

            if (Input.touchCount == 0)
            {
                if (Input.GetMouseButton(0))
                {
                    Vector2 n = new Vector2(Input.mousePosition.x / Screen.width,
                        Input.mousePosition.y / Screen.height);
                    if (InWheelZone(n))
                    {
                        float angle = AngleOf(Input.mousePosition,
                            new Vector2(WheelX * Screen.width, WheelY * Screen.height));
                        if (Input.GetMouseButtonDown(0)) _lastSteerAngle = angle;
                        _steerTarget = Mathf.Clamp(_steerTarget +
                            Mathf.DeltaAngle(_lastSteerAngle, angle) / 85f, -1f, 1f);
                        _lastSteerAngle = angle;
                    }
                    else
                    {
                        if (n.x >= GasMinX && n.y >= GasMinY && n.y <= GasMaxY) _gasPressed = true;
                        if (n.x >= GasMinX && n.y >= BrakeMinY && n.y <= BrakeMaxY) _brakePressed = true;
                        if (n.x >= DriftMinX && n.x <= DriftMaxX && n.y <= DriftMaxY) _driftPressed = true;
                    }
                    if (Input.GetMouseButtonDown(0))
                    {
                        if (n.x >= 0.84f && n.y >= 0.85f && Chase != null) Chase.CycleMode();
                        if (n.x <= 0.16f && n.y >= 0.85f && Car != null) Car.ResetCar();
                    }
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
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) _gasPressed = true;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) _brakePressed = true;
                if (Input.GetKey(KeyCode.Space)) _driftPressed = true;
                if (Input.GetKeyDown(KeyCode.C) && Chase != null) Chase.CycleMode();
                if (Input.GetKeyDown(KeyCode.R) && Car != null) Car.ResetCar();
            }

            if (keyboardSteer)
                _steerTarget = Mathf.MoveTowards(_steerTarget, keyboardSteerValue, Time.unscaledDeltaTime * 3f);
            else if (_steerTouchId < 0 && !Input.GetMouseButton(0))
                _steerTarget = Mathf.MoveTowards(_steerTarget, 0f, Time.unscaledDeltaTime * 0.85f);

            Steer = Mathf.MoveTowards(Steer, _steerTarget, Time.unscaledDeltaTime * 12f);

            // Brake always wins if both pedals are held.
            Throttle = _gasPressed && !_brakePressed ? 1f : 0f;
            Brake = _brakePressed ? 1f : 0f;
            Handbrake = _driftPressed ? 1f : 0f;

            if (Car != null)
            {
                Car.SteerInput = Steer;
                Car.ThrottleInput = Throttle;
                Car.BrakeInput = Brake;
                Car.HandbrakeInput = Handbrake;
            }

            if (_wheel != null)
            {
                _wheel.localEulerAngles = new Vector3(0f, 0f, -Steer * 135f);
                float wheelScale = _steerTouchId >= 0 ? 1.08f : 1f;
                _wheel.localScale = Vector3.Lerp(_wheel.localScale,
                    Vector3.one * wheelScale, Time.unscaledDeltaTime * 10f);
            }
            ScaleIcon(_gasIcon, _gasPressed && !_brakePressed);
            ScaleIcon(_brakeIcon, _brakePressed);
            ScaleIcon(_driftIcon, _driftPressed);

            if (_speedText != null && Car != null)
            {
                int kph = Mathf.RoundToInt(Car.SpeedKph);
                float forward = Vector3.Dot(Car.GetComponent<Rigidbody>().velocity, Car.transform.forward);
                string gear = Brake > 0f && forward < 1f ? "R" : "D";
                _speedText.text = kph + " km/h  " + gear;
            }
        }

        private static void ScaleIcon(RectTransform icon, bool pressed)
        {
            if (icon == null) return;
            float target = pressed ? 1.28f : 1f;
            icon.localScale = Vector3.Lerp(icon.localScale,
                Vector3.one * target, Time.unscaledDeltaTime * 16f);
        }
    }
}
