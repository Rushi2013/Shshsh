using UnityEngine;
using UnityEngine.UI;

namespace CityDrive
{
    /// <summary>
    /// Touch controls that work on any phone resolution, plus keyboard/mouse fallback.
    /// The visual pads are created in code, so no scene authoring or prefabs are needed.
    /// </summary>
    public sealed class MobileControls : MonoBehaviour
    {
        public ArcadeCarController Car;
        public ChaseCamera Chase;

        public float Steer { get; private set; }
        public float Throttle { get; private set; }
        public float Brake { get; private set; }
        public float Handbrake { get; private set; }

        private Image _steerPad;
        private Image _gasPad;
        private Image _brakePad;
        private Image _driftPad;
        private RectTransform _steerKnob;
        private Text _speedText;

        private static readonly Color PadColor = new Color(0.06f, 0.10f, 0.16f, 0.66f);
        private static readonly Color ActiveColor = new Color(0.20f, 0.75f, 0.95f, 0.85f);

        private void Awake()
        {
            BuildHud();
        }

        private void BuildHud()
        {
            Font font = null;
            try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { /* older Unity */ }
            if (font == null)
            {
                try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { /* no font available */ }
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

            _steerPad = Panel(canvasObject.transform, "Steer Pad",
                new Vector2(0.01f, 0.02f), new Vector2(0.34f, 0.44f), PadColor);
            Icon(_steerPad.transform, "UI/icon_steer", new Vector2(0.20f, 0.22f), new Vector2(0.80f, 0.90f));
            Label(_steerPad.transform, "STEER", font, 26);

            GameObject knob = new GameObject("Steer Knob", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            knob.transform.SetParent(_steerPad.transform, false);
            RectTransform knobRect = knob.GetComponent<RectTransform>();
            knobRect.anchorMin = knobRect.anchorMax = new Vector2(0.5f, 0.5f);
            knobRect.sizeDelta = new Vector2(110f, 110f);
            knob.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.35f);
            knob.GetComponent<Image>().raycastTarget = false;
            _steerKnob = knobRect;

            _gasPad = Panel(canvasObject.transform, "Gas Pad",
                new Vector2(0.68f, 0.25f), new Vector2(0.99f, 0.45f), PadColor);
            Icon(_gasPad.transform, "UI/icon_gas", new Vector2(0.18f, 0.30f), new Vector2(0.60f, 0.94f));
            Label(_gasPad.transform, "GAS", font, 30);

            _brakePad = Panel(canvasObject.transform, "Brake Pad",
                new Vector2(0.68f, 0.04f), new Vector2(0.99f, 0.24f), PadColor);
            Icon(_brakePad.transform, "UI/icon_brake", new Vector2(0.18f, 0.30f), new Vector2(0.60f, 0.94f));
            Label(_brakePad.transform, "BRAKE", font, 30);

            _driftPad = Panel(canvasObject.transform, "Handbrake Pad",
                new Vector2(0.37f, 0.02f), new Vector2(0.62f, 0.17f), PadColor);
            Icon(_driftPad.transform, "UI/icon_drift", new Vector2(0.30f, 0.18f), new Vector2(0.70f, 0.90f));
            Label(_driftPad.transform, "DRIFT", font, 24);

            Image cameraPad = Panel(canvasObject.transform, "Camera Button",
                new Vector2(0.84f, 0.85f), new Vector2(0.99f, 0.98f), PadColor);
            Icon(cameraPad.transform, "UI/icon_camera", new Vector2(0.16f, 0.20f), new Vector2(0.84f, 0.86f));
            Label(cameraPad.transform, "CAM", font, 22);

            Image resetPad = Panel(canvasObject.transform, "Reset Button",
                new Vector2(0.01f, 0.85f), new Vector2(0.16f, 0.98f), PadColor);
            Icon(resetPad.transform, "UI/icon_reset", new Vector2(0.16f, 0.20f), new Vector2(0.84f, 0.86f));
            Label(resetPad.transform, "RESET", font, 22);

            UiTools.Image(canvasObject.transform, "Speedometer", new Vector2(0.39f, 0.82f),
                new Vector2(0.61f, 0.98f), Color.white, UiTools.Sprite("UI/speed_gauge"));
            _speedText = Label(canvasObject.transform, "Speed", font, 34,
                new Vector2(0.41f, 0.84f), new Vector2(0.59f, 0.92f), TextAnchor.MiddleCenter);
            _speedText.text = "0 km/h";
        }

        private static Image Panel(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text Label(Transform parent, string text, Font font, int size)
        {
            return Label(parent, text, font, size, Vector2.zero, Vector2.one, TextAnchor.MiddleCenter);
        }

        private static Text Label(Transform parent, string text, Font font, int size,
            Vector2 min, Vector2 max, TextAnchor alignment)
        {
            GameObject go = new GameObject(text + " Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Text label = go.GetComponent<Text>();
            if (font != null) label.font = font;
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = new Color(1f, 1f, 1f, 0.92f);
            label.raycastTarget = false;
            return label;
        }

        private static Image Icon(Transform parent, string resource, Vector2 min, Vector2 max)
        {
            Image image = UiTools.Image(parent, resource, min, max, Color.white, UiTools.Sprite(resource));
            return image;
        }

        private static string ZoneOf(Vector2 normalized, bool began)
        {
            float x = normalized.x;
            float y = normalized.y;

            if (began && x >= 0.84f && y >= 0.85f) return "camera";
            if (began && x <= 0.16f && y >= 0.85f) return "reset";

            if (y <= 0.45f && x <= 0.34f) return "steer";
            if (y >= 0.25f && y <= 0.45f && x >= 0.68f) return "gas";
            if (y >= 0.04f && y < 0.25f && x >= 0.68f) return "brake";
            if (y <= 0.17f && x >= 0.37f && x <= 0.62f) return "drift";
            return "none";
        }

        private void Update()
        {
            float steerWanted = 0f;
            bool gas = false;
            bool brake = false;
            bool drift = false;

            int touches = Input.touchCount;
            for (int i = 0; i < touches; i++)
            {
                Touch touch = Input.GetTouch(i);
                Vector2 normalized = new Vector2(touch.position.x / Screen.width,
                    touch.position.y / Screen.height);
                bool began = touch.phase == TouchPhase.Began;
                string zone = ZoneOf(normalized, began);

                if (began && zone == "camera" && Chase != null) Chase.CycleMode();
                if (began && zone == "reset" && Car != null) Car.ResetCar();

                if (zone == "steer") steerWanted = Mathf.Clamp((normalized.x - 0.17f) / 0.17f, -1f, 1f);
                else if (zone == "gas") gas = true;
                else if (zone == "brake") brake = true;
                else if (zone == "drift") drift = true;
            }

            // Mouse / keyboard fallback (handy in the Editor and on laptops).
            if (touches == 0)
            {
                if (Input.GetMouseButton(0))
                {
                    Vector2 normalized = new Vector2(Input.mousePosition.x / Screen.width,
                        Input.mousePosition.y / Screen.height);
                    string zone = ZoneOf(normalized, Input.GetMouseButtonDown(0));
                    if (zone == "steer") steerWanted = Mathf.Clamp((normalized.x - 0.17f) / 0.17f, -1f, 1f);
                    else if (zone == "gas") gas = true;
                    else if (zone == "brake") brake = true;
                    else if (zone == "drift") drift = true;
                }

                if (Input.GetKeyDown(KeyCode.C) && Chase != null) Chase.CycleMode();
                if (Input.GetKeyDown(KeyCode.R) && Car != null) Car.ResetCar();

                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) steerWanted = -1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) steerWanted = 1f;
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) gas = true;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) brake = true;
                if (Input.GetKey(KeyCode.Space)) drift = true;
            }

            Steer = Mathf.MoveTowards(Steer, steerWanted, Time.unscaledDeltaTime * 5f);
            Throttle = gas ? 1f : 0f;
            Brake = brake ? 1f : 0f;
            Handbrake = drift ? 1f : 0f;

            if (Car != null)
            {
                Car.SteerInput = Steer;
                Car.ThrottleInput = Throttle;
                Car.BrakeInput = Brake;
                Car.HandbrakeInput = Handbrake;
            }

            if (_steerPad != null)
            {
                _steerPad.color = Mathf.Abs(Steer) > 0.1f ? ActiveColor : PadColor;
                _gasPad.color = gas ? ActiveColor : PadColor;
                _brakePad.color = brake ? ActiveColor : PadColor;
                _driftPad.color = drift ? ActiveColor : PadColor;
                _steerKnob.anchoredPosition = new Vector2(Steer * 105f, 0f);
            }

            if (_speedText != null && Car != null)
            {
                int kph = Mathf.RoundToInt(Car.SpeedKph);
                float forward = Vector3.Dot(Car.GetComponent<Rigidbody>().velocity, Car.transform.forward);
                string gear = BrakeInput > 0f && forward < 1f ? "R" : "D";
                _speedText.text = kph + " km/h  " + gear;
            }
        }
    }
}
