using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CityDrive
{
    /// <summary>
    /// Main menu shown before the open-world drive. The 3D showroom and the real
    /// PLAY transition are preserved; the old flat Play/Garage UI was replaced by
    /// an asset-styled menu with working Play, Garage, Customize, Settings,
    /// Credits, Quit and a local coins wallet.
    /// </summary>
    public sealed class HomeScreen : MonoBehaviour
    {
        private CityBuilder _city;
        private GameObject _car;
        private ArcadeCarController _controller;
        private ChaseCamera _chase;
        private Camera _camera;
        private LoadingScreen _loading;
        private VehicleAudio _audio;
        private Vector3 _spawn;
        private Quaternion _spawnRotation;
        private Action _onPlay;
        private GameObject _showroom;
        private GameObject _canvasObject;
        private GarageController _garage;
        private Text _carNameText;
        private Text _coinsText;
        private Text _bonusText;
        private GameObject _settingsPanel;
        private GameObject _customizePanel;
        private GameObject _creditsPanel;
        private GameObject _quitPanel;
        private Text _masterValue;
        private Text _sfxValue;
        private Text _qualityValue;
        private bool _playing;
        private float _showroomYaw;
        private readonly Vector3 _showroomPosition = new Vector3(0f, 0.72f, 0f);

        public bool PlayPressed => _playing;

        public void Show(CityBuilder city, GameObject car, ArcadeCarController controller,
            ChaseCamera chase, Camera camera, Vector3 spawn, Quaternion spawnRotation,
            LoadingScreen loading, VehicleAudio audio, Action onPlay)
        {
            _city = city;
            _car = car;
            _controller = controller;
            _chase = chase;
            _camera = camera;
            _spawn = spawn;
            _spawnRotation = spawnRotation;
            _loading = loading;
            _audio = audio;
            _onPlay = onPlay;

            if (_city != null && _city.Root != null) _city.Root.SetActive(false);
            Rigidbody body = _car.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = true;
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            _controller.enabled = false;
            _chase.enabled = false;
            if (_audio != null) _audio.SetActive(false);
            MenuSettings.Apply(_audio);
            GameBootstrap.ApplyBodyColor(_car, MenuSettings.BodyColor);

            _showroomYaw = 180f;
            _car.transform.SetPositionAndRotation(_showroomPosition, Quaternion.Euler(0f, _showroomYaw, 0f));
            _camera.transform.position = new Vector3(0f, 2.25f, -7.6f);
            _camera.transform.LookAt(new Vector3(0f, 0.88f, 0f));
            _camera.fieldOfView = 52f;
            _camera.farClipPlane = 200f;

            BuildShowroom();
            BuildUi();
        }

        private void BuildShowroom()
        {
            _showroom = new GameObject("Home Showroom");

            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            platform.name = "Circular Stand";
            platform.transform.SetParent(_showroom.transform, false);
            platform.transform.localScale = new Vector3(6f, 0.12f, 6f);
            platform.transform.position = new Vector3(0f, -0.12f, 0f);
            SetMaterial(platform, new Color(0.10f, 0.13f, 0.18f), null);

            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Stand Glow";
            ring.transform.SetParent(_showroom.transform, false);
            ring.transform.localScale = new Vector3(6.25f, 0.045f, 6.25f);
            ring.transform.position = new Vector3(0f, -0.24f, 0f);
            SetMaterial(ring, new Color(0.10f, 0.58f, 0.82f), null);

            Texture2D showroomTexture = Resources.Load<Texture2D>("UI/showroom_bg");
            if (showroomTexture != null)
            {
                GameObject backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
                backdrop.name = "Showroom Backdrop";
                backdrop.transform.SetParent(_showroom.transform, false);
                backdrop.transform.position = new Vector3(0f, 2.6f, 11f);
                backdrop.transform.localScale = new Vector3(26f, 13f, 1f);
                backdrop.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                SetMaterial(backdrop, Color.white, showroomTexture);
            }

            AddStudioLight("Key Light", new Vector3(4f, 7f, -5f), new Color(1f, 0.94f, 0.84f), 4.2f, true);
            AddStudioLight("Fill Light", new Vector3(-5f, 4f, 2f), new Color(0.60f, 0.76f, 1f), 2.1f, false);
            AddStudioLight("Rim Light", new Vector3(1f, 5f, 7f), new Color(0.86f, 0.92f, 1f), 2.6f, false);
        }

        private void AddStudioLight(string name, Vector3 position, Color color, float intensity, bool shadows)
        {
            GameObject lamp = new GameObject(name, typeof(Light));
            lamp.transform.SetParent(_showroom.transform, false);
            lamp.transform.position = position;
            lamp.transform.LookAt(new Vector3(0f, 0.7f, 0f));
            Light light = lamp.GetComponent<Light>();
            light.type = LightType.Spot;
            light.color = color;
            light.intensity = intensity;
            light.range = 30f;
            light.spotAngle = 68f;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
        }

        private static void SetMaterial(GameObject go, Color color, Texture texture)
        {
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            Shader shader = Shader.Find("Standard");
            if (shader == null) return;
            Material material = new Material(shader);
            material.color = color;
            if (texture != null) material.mainTexture = texture;
            renderer.material = material;
        }

        private void BuildUi()
        {
            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            _canvasObject = new GameObject("Main Menu UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvasObject.transform.SetParent(transform, false);
            Canvas canvas = _canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            CanvasScaler scaler = _canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform rect = _canvasObject.GetComponent<RectTransform>();
            Rect safe = Screen.safeArea;
            rect.anchorMin = new Vector2(safe.xMin / Mathf.Max(1f, Screen.width),
                safe.yMin / Mathf.Max(1f, Screen.height));
            rect.anchorMax = new Vector2(safe.xMax / Mathf.Max(1f, Screen.width),
                safe.yMax / Mathf.Max(1f, Screen.height));
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Sprite buttonSprite = UiTools.GetSprite("UI/Menu/ButtonSprite");
            UiTools.MakeImage(_canvasObject.transform, "Top Fade", new Vector2(0f, 0.86f), new Vector2(1f, 1f),
                new Color(0.02f, 0.04f, 0.08f, 0.72f));
            UiTools.MakeText(_canvasObject.transform, "Game Title", "CITY DRIVE", 78,
                new Vector2(0.03f, 0.89f), new Vector2(0.62f, 0.98f), TextAnchor.MiddleLeft,
                new Color(1f, 0.84f, 0.30f));
            _carNameText = UiTools.MakeText(_canvasObject.transform, "Selected Car", GameBootstrap.SelectedCarName, 32,
                new Vector2(0.03f, 0.81f), new Vector2(0.62f, 0.89f), TextAnchor.MiddleLeft,
                new Color(0.72f, 0.90f, 1f));

            BuildCoinBadge();

            UiTools.MakeImage(_canvasObject.transform, "Button Rail", new Vector2(0.70f, 0.06f),
                new Vector2(0.97f, 0.78f), new Color(0.03f, 0.06f, 0.11f, 0.68f));

            UiTools.MakeButton(_canvasObject.transform, "Play Button", "PLAY / DRIVE",
                new Vector2(0.72f, 0.60f), new Vector2(0.95f, 0.74f), Play, 36, buttonSprite,
                new Color(0.95f, 0.68f, 0.20f, 0.98f));
            UiTools.MakeButton(_canvasObject.transform, "Garage Button", "GARAGE",
                new Vector2(0.72f, 0.47f), new Vector2(0.95f, 0.58f), OpenGarage, 28, buttonSprite);
            UiTools.MakeButton(_canvasObject.transform, "Customize Button", "CUSTOMIZE",
                new Vector2(0.72f, 0.35f), new Vector2(0.95f, 0.46f), OpenCustomize, 28, buttonSprite);
            UiTools.MakeButton(_canvasObject.transform, "Settings Button", "SETTINGS",
                new Vector2(0.72f, 0.23f), new Vector2(0.83f, 0.34f), OpenSettings, 24, buttonSprite);
            UiTools.MakeButton(_canvasObject.transform, "Credits Button", "CREDITS",
                new Vector2(0.84f, 0.23f), new Vector2(0.95f, 0.34f), OpenCredits, 24, buttonSprite);
            UiTools.MakeButton(_canvasObject.transform, "Quit Button", "QUIT",
                new Vector2(0.72f, 0.10f), new Vector2(0.95f, 0.21f), AskQuit, 24, buttonSprite,
                new Color(0.32f, 0.12f, 0.14f, 0.96f));

            UiTools.MakeText(_canvasObject.transform, "Menu Hint",
                "OPEN WORLD   •   DRIFT SMOKE   •   3 CAMERAS", 26,
                new Vector2(0.03f, 0.05f), new Vector2(0.68f, 0.12f), TextAnchor.MiddleLeft,
                new Color(0.84f, 0.88f, 0.94f));

            _garage = gameObject.AddComponent<GarageController>();
            _garage.Build(_canvasObject.transform, OnCarSelected);
            BuildSettingsPanel(buttonSprite);
            BuildCustomizePanel(buttonSprite);
            BuildCreditsPanel(buttonSprite);
            BuildQuitPanel(buttonSprite);
        }

        private void BuildCoinBadge()
        {
            GameObject badge = UiTools.MakeImage(_canvasObject.transform, "Coins Panel",
                new Vector2(0.78f, 0.88f), new Vector2(0.97f, 0.98f),
                new Color(0.08f, 0.07f, 0.04f, 0.92f)).gameObject;
            UiTools.MakeText(badge.transform, "Coins Title", "COINS", 22,
                new Vector2(0.02f, 0.52f), new Vector2(0.48f, 0.96f), TextAnchor.MiddleLeft,
                new Color(0.91f, 0.78f, 0.36f));
            _coinsText = UiTools.MakeText(badge.transform, "Coins Value", MenuSettings.Coins.ToString("N0"), 38,
                new Vector2(0.48f, 0.08f), new Vector2(0.96f, 0.92f), TextAnchor.MiddleRight,
                new Color(1f, 0.90f, 0.42f));

            UiTools.MakeButton(_canvasObject.transform, "Daily Bonus Button", "DAILY +250",
                new Vector2(0.70f, 0.79f), new Vector2(0.90f, 0.87f), ClaimBonus, 22,
                UiTools.GetSprite("UI/Menu/ButtonFade"), new Color(0.18f, 0.34f, 0.22f, 0.96f));
            _bonusText = UiTools.MakeText(_canvasObject.transform, "Bonus Status",
                MenuSettings.BonusAvailable ? "READY" : "CLAIMED TODAY", 20,
                new Vector2(0.70f, 0.75f), new Vector2(0.90f, 0.80f), TextAnchor.MiddleLeft,
                new Color(0.64f, 0.94f, 0.70f));
        }

        private void ClaimBonus()
        {
            bool claimed = MenuSettings.ClaimDailyBonus(250);
            _coinsText.text = MenuSettings.Coins.ToString("N0");
            _bonusText.text = claimed ? "CLAIMED TODAY" : "ALREADY CLAIMED";
        }

        private GameObject MakePanel(string name, string title)
        {
            GameObject panel = UiTools.MakeImage(_canvasObject.transform, name,
                new Vector2(0.14f, 0.12f), new Vector2(0.68f, 0.84f),
                new Color(0.02f, 0.04f, 0.09f, 0.96f)).gameObject;
            panel.GetComponent<Image>().raycastTarget = true; // block clicks reaching the menu behind it
            UiTools.MakeText(panel.transform, "Panel Title", title, 46,
                new Vector2(0.03f, 0.86f), new Vector2(0.80f, 0.99f), TextAnchor.MiddleLeft,
                new Color(1f, 0.84f, 0.32f));
            UiTools.MakeButton(panel.transform, "Close " + name, "BACK",
                new Vector2(0.80f, 0.88f), new Vector2(0.98f, 0.99f), ClosePanels, 24,
                UiTools.GetSprite("UI/Menu/ButtonSprite"), new Color(0.18f, 0.25f, 0.36f, 0.96f));
            panel.SetActive(false);
            return panel;
        }

        private void BuildSettingsPanel(Sprite buttonSprite)
        {
            _settingsPanel = MakePanel("Settings Panel", "SETTINGS");
            Transform panel = _settingsPanel.transform;
            _masterValue = UiTools.MakeText(panel, "Master Label", "", 26,
                new Vector2(0.05f, 0.62f), new Vector2(0.95f, 0.72f), TextAnchor.MiddleLeft);
            Slider master = MakeSlider(panel, "Master Slider", new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.62f));
            master.wholeNumbers = false;
            master.SetValueWithoutNotify(MenuSettings.MasterVolume);
            master.onValueChanged.AddListener(value =>
            {
                MenuSettings.MasterVolume = value;
                MenuSettings.Apply(_audio);
                RefreshSettingsLabels();
            });

            _sfxValue = UiTools.MakeText(panel, "SFX Label", "", 26,
                new Vector2(0.05f, 0.42f), new Vector2(0.95f, 0.52f), TextAnchor.MiddleLeft);
            Slider sfx = MakeSlider(panel, "SFX Slider", new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.42f));
            sfx.wholeNumbers = false;
            sfx.SetValueWithoutNotify(MenuSettings.SfxVolume);
            sfx.onValueChanged.AddListener(value =>
            {
                MenuSettings.SfxVolume = value;
                MenuSettings.Apply(_audio);
                RefreshSettingsLabels();
            });

            _qualityValue = UiTools.MakeText(panel, "Quality Label", "", 26,
                new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.32f), TextAnchor.MiddleLeft);
            Slider quality = MakeSlider(panel, "Quality Slider", new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.22f));
            quality.wholeNumbers = true;
            quality.minValue = 0;
            quality.maxValue = Mathf.Max(0, QualitySettings.names.Length - 1);
            quality.SetValueWithoutNotify(MenuSettings.QualityLevel);
            quality.onValueChanged.AddListener(value =>
            {
                MenuSettings.QualityLevel = Mathf.RoundToInt(value);
                MenuSettings.Apply(_audio);
                RefreshSettingsLabels();
            });

            UiTools.MakeText(panel, "Settings Note", "Changes save locally and apply immediately.", 22,
                new Vector2(0.05f, 0.03f), new Vector2(0.95f, 0.12f), TextAnchor.MiddleLeft,
                new Color(0.70f, 0.78f, 0.88f));
            RefreshSettingsLabels();
        }

        private static Slider MakeSlider(Transform parent, string name, Vector2 min, Vector2 max)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Slider));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.10f, 0.15f, 0.22f, 0.98f);

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            RectTransform fillRect = fillArea.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0.02f, 0.25f);
            fillRect.anchorMax = new Vector2(0.98f, 0.75f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillImageRect = fill.GetComponent<RectTransform>();
            fillImageRect.anchorMin = Vector2.zero;
            fillImageRect.anchorMax = Vector2.one;
            fillImageRect.offsetMin = Vector2.zero;
            fillImageRect.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().color = new Color(0.12f, 0.62f, 0.86f, 1f);

            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            handle.transform.SetParent(go.transform, false);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0f, 0f);
            handleRect.anchorMax = new Vector2(0f, 1f);
            handleRect.sizeDelta = new Vector2(26f, 0f);
            handle.GetComponent<Image>().color = new Color(1f, 0.84f, 0.30f, 1f);

            Slider slider = go.GetComponent<Slider>();
            slider.fillRect = fillImageRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        private void RefreshSettingsLabels()
        {
            if (_masterValue != null)
                _masterValue.text = "MASTER VOLUME    " + Mathf.RoundToInt(MenuSettings.MasterVolume * 100f) + "%";
            if (_sfxValue != null)
                _sfxValue.text = "ENGINE / SFX     " + Mathf.RoundToInt(MenuSettings.SfxVolume * 100f) + "%";
            if (_qualityValue != null)
            {
                string[] names = QualitySettings.names;
                int index = MenuSettings.QualityLevel;
                _qualityValue.text = "GRAPHICS         " + (index < names.Length ? names[index] : index.ToString());
            }
        }

        private void BuildCustomizePanel(Sprite buttonSprite)
        {
            _customizePanel = MakePanel("Customize Panel", "CUSTOMIZE");
            string[] names = { "RACING YELLOW", "SKY BLUE", "CRIMSON", "EMERALD", "SILVER", "MIDNIGHT" };
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                float y = 0.72f - i * 0.10f;
                UiTools.MakeButton(_customizePanel.transform, "Paint " + i, names[i],
                    new Vector2(0.05f, y), new Vector2(0.55f, y + 0.085f),
                    () => SetPaint(index), 22, buttonSprite, PaintColor(i));
            }
            UiTools.MakeText(_customizePanel.transform, "Customize Note",
                "Wheel and body-kit options: coming soon.", 24,
                new Vector2(0.05f, 0.10f), new Vector2(0.95f, 0.22f), TextAnchor.MiddleLeft,
                new Color(0.78f, 0.84f, 0.92f));
        }

        private static Color PaintColor(int index)
        {
            Color[] colors =
            {
                new Color(0.95f, 0.72f, 0.16f, 1f), new Color(0.16f, 0.55f, 0.92f, 1f),
                new Color(0.78f, 0.14f, 0.18f, 1f), new Color(0.12f, 0.62f, 0.38f, 1f),
                new Color(0.72f, 0.75f, 0.80f, 1f), new Color(0.09f, 0.11f, 0.18f, 1f)
            };
            return colors[Mathf.Clamp(index, 0, colors.Length - 1)];
        }

        private void SetPaint(int index)
        {
            MenuSettings.BodyColor = index;
            GameBootstrap.ApplyBodyColor(_car, index);
            MenuSettings.Save();
        }

        private void BuildCreditsPanel(Sprite buttonSprite)
        {
            _creditsPanel = MakePanel("Credits Panel", "CREDITS");
            UiTools.MakeText(_creditsPanel.transform, "Credits Text",
                "CITY DRIVE — Unity 3D open-world driving\n\n" +
                "City GLB, BMW 330i xDrive GLB, Complete Vehicle Pack V2: supplied by the project owner.\n" +
                "UniStorm Weather System: supplied by the project owner.\n" +
                "Realistic Car Controller Pro v1.44.0: BoneCracker Games, supplied by the project owner.\n" +
                "UI button art: staged from the supplied RCC Pro UI texture set.\n\n" +
                "Audio is generated procedurally at runtime; no external audio files are redistributed.",
                26, new Vector2(0.05f, 0.10f), new Vector2(0.95f, 0.82f), TextAnchor.UpperLeft,
                new Color(0.86f, 0.91f, 0.98f));
        }

        private void BuildQuitPanel(Sprite buttonSprite)
        {
            _quitPanel = MakePanel("Quit Panel", "QUIT GAME?");
            UiTools.MakeText(_quitPanel.transform, "Quit Message",
                Application.platform == RuntimePlatform.IPhonePlayer
                    ? "iOS apps should be closed with the device home gesture."
                    : "Your car choice, paint and settings are already saved.",
                26, new Vector2(0.05f, 0.42f), new Vector2(0.95f, 0.72f), TextAnchor.MiddleCenter);
            UiTools.MakeButton(_quitPanel.transform, "Confirm Quit", "QUIT",
                new Vector2(0.10f, 0.14f), new Vector2(0.46f, 0.32f), ConfirmQuit, 28, buttonSprite,
                new Color(0.35f, 0.12f, 0.14f, 0.98f));
            UiTools.MakeButton(_quitPanel.transform, "Cancel Quit", "CANCEL",
                new Vector2(0.54f, 0.14f), new Vector2(0.90f, 0.32f), ClosePanels, 28, buttonSprite);
        }

        private void OpenGarage()
        {
            ClosePanels();
            if (_garage != null) _garage.Open();
        }

        private void OpenCustomize()
        {
            ClosePanels();
            _customizePanel.SetActive(true);
        }

        private void OpenSettings()
        {
            ClosePanels();
            _settingsPanel.SetActive(true);
        }

        private void OpenCredits()
        {
            ClosePanels();
            _creditsPanel.SetActive(true);
        }

        private void AskQuit()
        {
            ClosePanels();
            _quitPanel.SetActive(true);
        }

        private void ConfirmQuit()
        {
            MenuSettings.Save();
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID
            Application.Quit();
#endif
        }

        private void ClosePanels()
        {
            if (_garage != null) _garage.Close();
            if (_settingsPanel != null) _settingsPanel.SetActive(false);
            if (_customizePanel != null) _customizePanel.SetActive(false);
            if (_creditsPanel != null) _creditsPanel.SetActive(false);
            if (_quitPanel != null) _quitPanel.SetActive(false);
        }

        private void OnCarSelected(GameObject prefab, string displayName)
        {
            GameObject visual = GameBootstrap.SetPlayerVisual(_car, prefab);
            if (visual == null) return;
            VehicleVisuals visuals = _car.GetComponent<VehicleVisuals>();
            if (visuals != null)
            {
                visuals.FindWheels(_car.transform);
                visuals.FindOriginalInteriorWheel(visual.transform);
            }
            GameBootstrap.ApplyBodyColor(_car, MenuSettings.BodyColor);
            MenuSettings.SelectCar(prefab.name);
            if (_carNameText != null) _carNameText.text = displayName;
            ClosePanels();
        }

        private void Update()
        {
            if (_playing || _car == null) return;
            _showroomYaw = (_showroomYaw + 26f * Time.unscaledDeltaTime) % 360f;
            _car.transform.SetPositionAndRotation(_showroomPosition,
                Quaternion.Euler(0f, _showroomYaw, 0f));
        }

        public void Play()
        {
            if (_playing) return;
            _playing = true;
            ClosePanels();
            StartCoroutine(StartGameplay());
        }

        private IEnumerator StartGameplay()
        {
            if (_loading != null)
            {
                _loading.Show("UI/loading_bg_2");
                _loading.SetProgress(0.08f, "LOADING CITY AND CAR MODEL",
                    "Clearing showroom and preparing the road...");
            }
            if (_showroom != null) Destroy(_showroom);
            if (_canvasObject != null) Destroy(_canvasObject);
            yield return null;

            if (_loading != null)
                _loading.SetProgress(0.34f, "LOADING CITY AND CAR MODEL",
                    "Placing the selected car on the city road...");
            Rigidbody body = _car.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = false;
                body.position = _spawn;
                body.rotation = _spawnRotation;
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            _car.transform.SetPositionAndRotation(_spawn, _spawnRotation);
            _controller.ConfigureSpawn(_spawn, _spawnRotation);
            Physics.SyncTransforms();
            yield return null;

            if (_loading != null)
                _loading.SetProgress(0.62f, "STARTING ENGINE",
                    "Bringing the drivetrain and cameras online...");
            if (_city != null && _city.Root != null) _city.Root.SetActive(true);
            _controller.enabled = true;
            _chase.enabled = true;
            if (_audio != null)
            {
                _audio.SetMasterVolume(MenuSettings.SfxVolume);
                _audio.SetActive(true);
            }
            MenuSettings.Apply(_audio);
            _camera.fieldOfView = 64f;
            _camera.farClipPlane = 1200f;
            _camera.transform.position = _spawn + new Vector3(0f, 3.6f, -7.6f);
            _camera.transform.LookAt(_spawn + Vector3.up);
            yield return null;

            if (_loading != null)
            {
                _loading.SetProgress(1f, "READY", "City Drive is ready");
                yield return null;
                _loading.Hide();
            }

            if (_onPlay != null) _onPlay();
        }

        /// <summary>Allows a future main-menu return to resume the same smooth rotation.</summary>
        public void ResumeShowroom()
        {
            _playing = false;
            _car.transform.SetPositionAndRotation(_showroomPosition,
                Quaternion.Euler(0f, _showroomYaw, 0f));
        }
    }
}
