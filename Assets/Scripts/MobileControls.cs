using UnityEngine;
using UnityEngine.UI;

namespace CityDrive
{
    /// <summary>
    /// Clean mobile racing HUD: true angular steering wheel on the left, separate
    /// gas/brake pedals on the right, drift button, live Rigidbody speedometer and
    /// camera/reset icons. Safe-area aware; no joystick, no text on the pedals.
    /// </summary>
    public sealed class MobileControls : MonoBehaviour
    {
        public ArcadeCarController Car;
        public ChaseCamera Chase;
        public VehicleAudio Audio;

        [Header("Steering wheel")]
        [SerializeField] private float maxWheelVisualDegrees = 200f;
        [SerializeField] private float steerReturnSpeed = 2.2f;
        [SerializeField] private float steerResponse = 14f;
        [SerializeField] private float gestureSensitivity = 200f;

        [Header("Pedals")]
        [SerializeField] private float pedalResponse = 7f;
        [SerializeField] private float pressedScale = 1.26f;

        [Header("Speedometer")]
        [SerializeField] private float speedometerMaxKph = 240f;
        [SerializeField] private float needleMinAngle = -125f;
        [SerializeField] private float needleMaxAngle = 125f;

        public float Steer { get; private set; }
        public float Throttle { get; private set; }
        public float Brake { get; private set; }
        public float Handbrake { get; private set; }

        private RectTransform _wheel;
        private RectTransform _gasIcon;
        private RectTransform _brakeIcon;
        private RectTransform _driftIcon;
        private RectTransform _needle;
        private Text _speedText;
        private Text _gearText;

        private int _steerTouchId = -1;
        private float _lastSteerAngle;
        private float _steerTarget;
        private float _throttleSmoothed;
        private float _brakeSmoothed;
        private float _handbrakeSmoothed;
        private float _displaySpeed;

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

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            Rect safe = Screen.safeArea;
            canvasRect.anchorMin = new Vector2(safe.xMin / Mathf.Max(1f, Screen.width),
                safe.yMin / Mathf.Max(1f, Screen.height));
            canvasRect.anchorMax = new Vector2(safe.xMax / Mathf.Max(1f, Screen.width),
                safe.yMax / Mathf.Max(1f, Screen.height));
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

            BuildSpeedometer(canvasObject.transform);
        }

        private void BuildSpeedometer(Transform parent)
        {
            // Old speed_gauge.png is gone. A clean square gauge is generated at runtime:
            // no stretched PNG, no white rectangle, no duplicate overlay.
            // Prefer the supplied Realistic Car Controller speedometer artwork;
            // the generated gauge remains a fallback if that asset is unavailable.
            Sprite gaugeFace = UiTools.GetSprite("UI/RCC/Speedometer") ?? CreateGaugeSprite();
            Image gauge = UiTools.MakeImage(parent, "Speedometer",
                new Vector2(0.5f, 0.895f), new Vector2(0.5f, 0.895f),
                Color.white, gaugeFace);
            gauge.rectTransform.sizeDelta = new Vector2(220f, 220f);

            Sprite needleSprite = UiTools.GetSprite("UI/RCC/Needle");
            Image needle = UiTools.MakeImage(gauge.rectTransform, "Speed Needle",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                needleSprite != null ? Color.white : new Color(1f, 0.30f, 0.20f, 0.96f),
                needleSprite);
            _needle = needle.rectTransform;
            _needle.sizeDelta = new Vector2(8f, 88f);
            _needle.pivot = new Vector2(0.5f, 0.08f);
            _needle.anchoredPosition = new Vector2(0f, -18f);
            _needle.localEulerAngles = new Vector3(0f, 0f, needleMinAngle);

            _speedText = UiTools.MakeText(gauge.rectTransform, "Digital Speed", "0", 46,
                new Vector2(0.14f, 0.10f), new Vector2(0.86f, 0.42f),
                TextAnchor.MiddleCenter, new Color(0.96f, 0.99f, 1f));
            UiTools.MakeText(gauge.rectTransform, "Speed Unit", "km/h", 20,
                new Vector2(0.14f, 0.02f), new Vector2(0.86f, 0.15f),
                TextAnchor.MiddleCenter, new Color(0.68f, 0.82f, 0.94f));
            _gearText = UiTools.MakeText(gauge.rectTransform, "Gear", "D1", 20,
                new Vector2(0.01f, 0.72f), new Vector2(0.26f, 0.90f),
                TextAnchor.MiddleCenter, new Color(1f, 0.84f, 0.32f));
        }

        /// <summary>Procedural dark gauge face with a subtle tick ring; replaces the old PNG.</summary>
        private static Sprite CreateGaugeSprite()
        {
            const int size = 256;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Live Speedometer Face",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] pixels = new Color[size * size];
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color face = new Color(0.035f, 0.055f, 0.085f, 0.86f);
            Color ring = new Color(0.65f, 0.82f, 0.95f, 0.95f);
            Color tick = new Color(1f, 0.86f, 0.36f, 0.95f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f;
                    float dy = (y + 0.5f) / size * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    Color c = clear;

                    if (r < 0.70f)
                    {
                        c = face;
                    }
                    else if (r < 0.80f)
                    {
                        c = Color.Lerp(face, ring, Mathf.SmoothStep(0.70f, 0.80f, r));
                    }
                    else if (r < 0.84f)
                    {
                        c = ring;
                    }
                    else if (r < 0.94f)
                    {
                        c = new Color(ring.r, ring.g, ring.b,
                            ring.a * (1f - Mathf.SmoothStep(0.84f, 0.94f, r)) * 0.35f);
                    }

                    float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    float sweep = Mathf.DeltaAngle(angle, 90f); // 0 at bottom, ±125 across the arc
                    if (r > 0.57f && r < 0.68f && Mathf.Abs(sweep) <= 125f)
                    {
                        float nearest = Mathf.Round(sweep / 25f) * 25f;
                        if (Mathf.Abs(sweep - nearest) < 2.2f) c = tick;
                    }

                    pixels[y * size + x] = c;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private Vector2 WheelCentreScreen()
        {
            if (_wheel == null) return Vector2.zero;
            return RectTransformUtility.WorldToScreenPoint(null, _wheel.position);
        }

        private float WheelRadiusScreen()
        {
            if (_wheel == null) return 0f;
            return _wheel.rect.width * 0.5f * Mathf.Max(0.01f, _wheel.lossyScale.x) * 1.15f;
        }

        private bool InWheelZone(Vector2 screenPoint)
        {
            if (_wheel == null) return false;
            Vector2 centre = WheelCentreScreen();
            Vector2 delta = screenPoint - centre;
            float radius = WheelRadiusScreen();
            return delta.x * delta.x + delta.y * delta.y <= radius * radius;
        }

        private float AngleAroundWheel(Vector2 screenPoint)
        {
            Vector2 delta = screenPoint - WheelCentreScreen();
            return Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        }

        private bool IsNearWheelHub(Vector2 screenPoint)
        {
            Vector2 delta = screenPoint - WheelCentreScreen();
            float radius = WheelRadiusScreen();
            return delta.sqrMagnitude < (radius * 0.18f) * (radius * 0.18f);
        }

        /// <summary>
        /// The ONLY steering inversion in the project. Screen angle grows counter-clockwise,
        /// so clockwise finger motion yields a negative delta; right turn must be positive.
        /// </summary>
        private void ApplyAngularDelta(float deltaDegrees)
        {
            _steerTarget = Mathf.Clamp(
                _steerTarget - deltaDegrees / Mathf.Max(20f, gestureSensitivity), -1f, 1f);
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
                Vector2 n = new Vector2(touch.position.x / Mathf.Max(1f, Screen.width),
                    touch.position.y / Mathf.Max(1f, Screen.height));

                if (InWheelZone(touch.position))
                {
                    if (_steerTouchId < 0 && touch.phase != TouchPhase.Ended &&
                        touch.phase != TouchPhase.Canceled)
                    {
                        _steerTouchId = touch.fingerId;
                        _lastSteerAngle = AngleAroundWheel(touch.position);
                    }
                    else if (touch.fingerId == _steerTouchId)
                    {
                        float angle = AngleAroundWheel(touch.position);
                        if (!IsNearWheelHub(touch.position))
                            ApplyAngularDelta(Mathf.DeltaAngle(_lastSteerAngle, angle));
                        _lastSteerAngle = angle;
                        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                            _steerTouchId = -1;
                    }
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
                Vector2 n = new Vector2(mouse.x / Mathf.Max(1f, Screen.width),
                    mouse.y / Mathf.Max(1f, Screen.height));
                if (Input.GetMouseButton(0))
                {
                    if (InWheelZone(mouse))
                    {
                        float angle = AngleAroundWheel(mouse);
                        if (Input.GetMouseButtonDown(0)) _lastSteerAngle = angle;
                        if (!IsNearWheelHub(mouse))
                            ApplyAngularDelta(Mathf.DeltaAngle(_lastSteerAngle, angle));
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
                if (Input.GetKeyDown(KeyCode.H) && Car != null) Car.Honk();
            }

            float speedRatio = Car != null ? Mathf.Clamp01(Car.SpeedKph / speedometerMaxKph) : 0f;
            float response = steerResponse * Mathf.Lerp(1f, 0.75f, speedRatio);

            if (keyboardSteer)
                _steerTarget = Mathf.MoveTowards(_steerTarget, keyboardSteerValue,
                    Time.unscaledDeltaTime * 3f);
            else if (_steerTouchId < 0)
                _steerTarget = Mathf.MoveTowards(_steerTarget, 0f,
                    Time.unscaledDeltaTime * steerReturnSpeed);

            Steer = Mathf.MoveTowards(Steer, _steerTarget, Time.unscaledDeltaTime * response);

            // Both pedals remain active. Brake priority and burnout torque are handled
            // by the physics controller, not by deleting the throttle input here.
            float throttleTarget = gasPressed ? 1f : 0f;
            float brakeTarget = brakePressed ? 1f : 0f;
            float driftTarget = driftPressed ? 1f : 0f;
            _throttleSmoothed = Mathf.MoveTowards(_throttleSmoothed, throttleTarget,
                Time.unscaledDeltaTime * pedalResponse);
            _brakeSmoothed = Mathf.MoveTowards(_brakeSmoothed, brakeTarget,
                Time.unscaledDeltaTime * pedalResponse);
            _handbrakeSmoothed = Mathf.MoveTowards(_handbrakeSmoothed, driftTarget,
                Time.unscaledDeltaTime * pedalResponse);

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

            UpdateSpeedometer();
        }

        private void UpdateSpeedometer()
        {
            // Never derived from input: the only source is the player's Rigidbody velocity.
            float actual = Car != null ? Car.SpeedKph : 0f;
            if (actual < 0.35f && Car != null && Car.GroundedWheels > 0) actual = 0f;
            _displaySpeed = Mathf.MoveTowards(_displaySpeed, actual, Time.unscaledDeltaTime * 55f);

            if (_needle != null)
            {
                float t = Mathf.Clamp01(_displaySpeed / speedometerMaxKph);
                _needle.localEulerAngles = new Vector3(0f, 0f,
                    Mathf.Lerp(needleMinAngle, needleMaxAngle, t));
            }
            if (_speedText != null)
                _speedText.text = Mathf.RoundToInt(_displaySpeed).ToString();
            if (_gearText != null && Car != null)
                _gearText.text = Car.IsReversing ? "R" : "D" + Car.CurrentGear;
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
