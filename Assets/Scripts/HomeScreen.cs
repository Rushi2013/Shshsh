using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CityDrive
{
    /// <summary>
    /// Car Menu UI v2 based main screen. The supplied full-screen artwork and
    /// baked-label buttons are used directly from Resources; the 3D showroom car
    /// is rendered into a dedicated RenderTexture so it can sit on the right of
    /// the menu without being covered by the background image. All existing
    /// gameplay transitions, car selection and save data are preserved.
    /// </summary>
    public sealed class HomeScreen : MonoBehaviour
    {
        private const string MenuRoot = "UI/CarMenu/";

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
        private Text _walletBalance;
        private Text _bonusText;
        private GameObject _settingsPanel;
        private GameObject _customizePanel;
        private GameObject _creditsPanel;
        private GameObject _quitPanel;
        private GameObject _walletPanel;
        private Text _masterValue;
        private Text _sfxValue;
        private Text _paintName;
        private Camera _previewCamera;
        private RenderTexture _previewTexture;
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
            if (_camera != null) _camera.enabled = false;

            BuildShowroom();
            BuildPreviewCamera();
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

            AddStudioLight("Key Light", new Vector3(4f, 7f, -5f), new Color(1f, 0.94f, 0.84f), 4.2f, true);
            AddStudioLight("Fill Light", new Vector3(-5f, 4f, 2f), new Color(0.60f, 0.76f, 1f), 2.1f, false);
            AddStudioLight("Rim Light", new Vector3(1f, 5f, 7f), new Color(0.86f, 0.92f, 1f), 2.6f, false);
        }

        private void BuildPreviewCamera()
        {
            _previewTexture = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32)
            {
                name = "Menu Car Preview",
                useMipMap = false,
                filterMode = FilterMode.Bilinear
            };
            _previewTexture.Create();

            GameObject cameraObject = new GameObject("Menu Car Preview Camera", typeof(Camera));
            cameraObject.transform.SetParent(transform, false);
            _previewCamera = cameraObject.GetComponent<Camera>();
            _previewCamera.clearFlags = CameraClearFlags.SolidColor;
            _previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _previewCamera.fieldOfView = 52f;
            _previewCamera.nearClipPlane = 0.1f;
            _previewCamera.farClipPlane = 200f;
            _previewCamera.allowHDR = true;
            _previewCamera.targetTexture = _previewTexture;
            cameraObject.transform.position = new Vector3(0f, 2.25f, -7.6f);
            cameraObject.transform.LookAt(new Vector3(0f, 0.88f, 0f));
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

            _canvasObject = new GameObject("Car Menu UI v2", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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

            Image background = UiTools.MakeImage(_canvasObject.transform, "Menu Background", Vector2.zero, Vector2.one,
                Color.white, UiTools.GetSprite(MenuRoot + "Background/MainMenu_Background"));
            AspectRatioFitter fitter = background.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 1672f / 941f;

            UiTools.MakeImage(_canvasObject.transform, "Left Shade", new Vector2(0f, 0f), new Vector2(0.38f, 1f),
                new Color(0.02f, 0.04f, 0.08f, 0.52f));

            RawImage preview = new GameObject("3D Car Preview", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
            preview.transform.SetParent(_canvasObject.transform, false);
            RectTransform previewRect = preview.rectTransform;
            previewRect.anchorMin = new Vector2(0.40f, 0.09f);
            previewRect.anchorMax = new Vector2(0.99f, 0.90f);
            previewRect.offsetMin = Vector2.zero;
            previewRect.offsetMax = Vector2.zero;
            preview.texture = _previewTexture;
            preview.color = Color.white;
            preview.raycastTarget = false;

            UiTools.MakeText(_canvasObject.transform, "Game Title", "CITY DRIVE", 74,
                new Vector2(0.03f, 0.88f), new Vector2(0.38f, 0.98f), TextAnchor.MiddleLeft,
                new Color(1f, 0.86f, 0.36f));
            _carNameText = UiTools.MakeText(_canvasObject.transform, "Selected Car", GameBootstrap.SelectedCarName, 30,
                new Vector2(0.03f, 0.82f), new Vector2(0.38f, 0.89f), TextAnchor.MiddleLeft,
                new Color(0.78f, 0.91f, 1f));

            UiTools.MakeAssetButton(_canvasObject.transform, "Play Button", MenuRoot + "Main_Buttons/Button_PLAY",
                new Vector2(0.03f, 0.64f), new Vector2(0.23f, 0.74f), Play);
            UiTools.MakeAssetButton(_canvasObject.transform, "Garage Button", MenuRoot + "Main_Buttons/Button_GARAGE",
                new Vector2(0.03f, 0.54f), new Vector2(0.23f, 0.64f), OpenGarage);
            UiTools.MakeAssetButton(_canvasObject.transform, "Customize Button", MenuRoot + "Main_Buttons/Button_CUSTOMIZE",
                new Vector2(0.03f, 0.44f), new Vector2(0.23f, 0.54f), OpenCustomize);
            UiTools.MakeAssetButton(_canvasObject.transform, "Settings Button", MenuRoot + "Main_Buttons/Button_SETTINGS",
                new Vector2(0.03f, 0.34f), new Vector2(0.23f, 0.44f), OpenSettings);
            UiTools.MakeAssetButton(_canvasObject.transform, "Credits Button", MenuRoot + "Main_Buttons/Button_CREDITS",
                new Vector2(0.03f, 0.24f), new Vector2(0.23f, 0.34f), OpenCredits);
            UiTools.MakeAssetButton(_canvasObject.transform, "Exit Button", MenuRoot + "Main_Buttons/Button_EXIT",
                new Vector2(0.03f, 0.14f), new Vector2(0.23f, 0.24f), AskQuit);
            UiTools.MakeAssetButton(_canvasObject.transform, "Store Button (disabled)", MenuRoot + "Main_Buttons/Button_STORE",
                new Vector2(0.03f, 0.04f), new Vector2(0.23f, 0.14f), null, false);
            UiTools.MakeText(_canvasObject.transform, "Store Note", "STORE — COMING SOON", 22,
                new Vector2(0.24f, 0.05f), new Vector2(0.42f, 0.13f), TextAnchor.MiddleLeft,
                new Color(0.78f, 0.84f, 0.92f));

            BuildCoinBadge();

            _garage = gameObject.AddComponent<GarageController>();
            _garage.Build(_canvasObject.transform, OnCarSelected);
            BuildSettingsPanel();
            BuildCustomizePanel();
            BuildCreditsPanel();
            BuildQuitPanel();
            BuildWalletPanel();
        }

        private void BuildCoinBadge()
        {
            Image coinIcon = UiTools.MakeImage(_canvasObject.transform, "Coin Icon",
                new Vector2(0.82f, 0.89f), new Vector2(0.87f, 0.98f), Color.white,
                UiTools.GetSprite(MenuRoot + "Icons/Icon_Coins"));
            coinIcon.preserveAspect = true;

            UiTools.MakeAssetButton(_canvasObject.transform, "Coins Button", MenuRoot + "Main_Buttons/Button_COINS",
                new Vector2(0.72f, 0.80f), new Vector2(0.92f, 0.90f), OpenWallet);
            _coinsText = UiTools.MakeText(_canvasObject.transform, "Coins Value", MenuSettings.Coins.ToString("N0"), 34,
                new Vector2(0.76f, 0.90f), new Vector2(0.94f, 0.98f), TextAnchor.MiddleRight,
                new Color(1f, 0.90f, 0.42f));
        }

        private GameObject MakePanel(string name, string title)
        {
            GameObject panel = UiTools.MakeImage(_canvasObject.transform, name,
                new Vector2(0.30f, 0.10f), new Vector2(0.90f, 0.88f),
                new Color(0.02f, 0.04f, 0.09f, 0.96f)).gameObject;
            panel.GetComponent<Image>().raycastTarget = true;
            UiTools.MakeText(panel.transform, "Panel Title", title, 46,
                new Vector2(0.03f, 0.86f), new Vector2(0.70f, 0.99f), TextAnchor.MiddleLeft,
                new Color(1f, 0.84f, 0.32f));
            UiTools.MakeAssetButton(panel.transform, "Back", MenuRoot + "Sub_Buttons/Button_BACK",
                new Vector2(0.76f, 0.88f), new Vector2(0.96f, 0.99f), ClosePanels);
            panel.SetActive(false);
            return panel;
        }

        private static Text Header(Transform parent, string resource, float y)
        {
            Image image = UiTools.MakeImage(parent, resource, new Vector2(0.05f, y), new Vector2(0.25f, y + 0.08f),
                Color.white, UiTools.GetSprite(MenuRoot + resource));
            image.preserveAspect = true;
            return UiTools.MakeText(parent, resource + " Caption", "", 24,
                new Vector2(0.27f, y), new Vector2(0.95f, y + 0.08f), TextAnchor.MiddleLeft,
                new Color(0.86f, 0.92f, 1f));
        }

        private void BuildSettingsPanel()
        {
            _settingsPanel = MakePanel("Settings Panel", "SETTINGS");
            Transform panel = _settingsPanel.transform;

            _masterValue = Header(panel, "Sub_Buttons/Button_MUSIC", 0.62f);
            Slider master = MakeSlider(panel, "Master Slider", new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.62f));
            master.SetValueWithoutNotify(MenuSettings.MasterVolume);
            master.onValueChanged.AddListener(value =>
            {
                MenuSettings.MasterVolume = value;
                MenuSettings.Apply(_audio);
                RefreshSettingsLabels();
            });

            _sfxValue = Header(panel, "Sub_Buttons/Button_SOUND", 0.42f);
            Slider sfx = MakeSlider(panel, "SFX Slider", new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.42f));
            sfx.SetValueWithoutNotify(MenuSettings.SfxVolume);
            sfx.onValueChanged.AddListener(value =>
            {
                MenuSettings.SfxVolume = value;
                MenuSettings.Apply(_audio);
                RefreshSettingsLabels();
            });

            Header(panel, "Sub_Buttons/Button_GRAPHICS", 0.22f);
            UiTools.MakeAssetButton(panel, "Low Quality", MenuRoot + "Sub_Buttons/Button_LOW",
                new Vector2(0.05f, 0.10f), new Vector2(0.25f, 0.21f), () => SetQuality(0));
            UiTools.MakeAssetButton(panel, "Medium Quality", MenuRoot + "Sub_Buttons/Button_MEDIUM",
                new Vector2(0.27f, 0.10f), new Vector2(0.47f, 0.21f), () => SetQuality(1));
            UiTools.MakeAssetButton(panel, "High Quality", MenuRoot + "Sub_Buttons/Button_HIGH",
                new Vector2(0.49f, 0.10f), new Vector2(0.69f, 0.21f), () => SetQuality(2));
            UiTools.MakeAssetButton(panel, "Save Settings", MenuRoot + "Sub_Buttons/Button_SAVE",
                new Vector2(0.71f, 0.10f), new Vector2(0.95f, 0.21f), () => { MenuSettings.Save(); ClosePanels(); });
            RefreshSettingsLabels();
        }

        private static void SetQuality(int level)
        {
            MenuSettings.QualityLevel = level;
            MenuSettings.Apply(null);
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
                _masterValue.text = Mathf.RoundToInt(MenuSettings.MasterVolume * 100f) + "%";
            if (_sfxValue != null)
                _sfxValue.text = Mathf.RoundToInt(MenuSettings.SfxVolume * 100f) + "%";
        }

        private void BuildCustomizePanel()
        {
            _customizePanel = MakePanel("Customize Panel", "CUSTOMIZE");
            Transform panel = _customizePanel.transform;
            Header(panel, "Sub_Buttons/Button_PAINT", 0.62f);
            _paintName = UiTools.MakeText(panel, "Paint Name", "", 32,
                new Vector2(0.05f, 0.50f), new Vector2(0.95f, 0.62f), TextAnchor.MiddleCenter,
                new Color(0.96f, 0.86f, 0.40f));
            UiTools.MakeAssetButton(panel, "Previous Paint", MenuRoot + "Sub_Buttons/Button_PREVIOUS",
                new Vector2(0.10f, 0.34f), new Vector2(0.34f, 0.48f), () => ChangePaint(-1));
            UiTools.MakeAssetButton(panel, "Next Paint", MenuRoot + "Sub_Buttons/Button_NEXT",
                new Vector2(0.66f, 0.34f), new Vector2(0.90f, 0.48f), () => ChangePaint(1));

            UiTools.MakeAssetButton(panel, "Wheels (coming soon)", MenuRoot + "Sub_Buttons/Button_WHEELS",
                new Vector2(0.10f, 0.18f), new Vector2(0.34f, 0.31f), null, false);
            UiTools.MakeAssetButton(panel, "Upgrade (coming soon)", MenuRoot + "Sub_Buttons/Button_UPGRADE",
                new Vector2(0.66f, 0.18f), new Vector2(0.90f, 0.31f), null, false);
            UiTools.MakeText(panel, "Coming Soon", "Wheels and upgrades: coming soon.", 22,
                new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.17f), TextAnchor.MiddleCenter,
                new Color(0.78f, 0.84f, 0.92f));
            RefreshPaintLabel();
        }

        private static readonly string[] PaintNames =
        {
            "RACING YELLOW", "SKY BLUE", "CRIMSON", "EMERALD", "SILVER", "MIDNIGHT"
        };

        private void ChangePaint(int delta)
        {
            int index = (MenuSettings.BodyColor + delta + PaintNames.Length) % PaintNames.Length;
            MenuSettings.BodyColor = index;
            GameBootstrap.ApplyBodyColor(_car, index);
            MenuSettings.Save();
            RefreshPaintLabel();
        }

        private void RefreshPaintLabel()
        {
            if (_paintName != null)
                _paintName.text = PaintNames[Mathf.Clamp(MenuSettings.BodyColor, 0, PaintNames.Length - 1)];
        }

        private void BuildCreditsPanel()
        {
            _creditsPanel = MakePanel("Credits Panel", "CREDITS");
            UiTools.MakeText(_creditsPanel.transform, "Credits Text",
                "CITY DRIVE — Unity 3D open-world driving\n\n" +
                "City GLB, BMW GLB, Complete Vehicle Pack V2, garage GLBs: supplied by the project owner.\n" +
                "Car Menu UI v2: original generated kit supplied by the project owner.\n" +
                "UniStorm Weather System and RCC Pro v1.44: supplied by the project owner.\n\n" +
                "Audio is generated procedurally at runtime; no external audio is redistributed.",
                26, new Vector2(0.05f, 0.10f), new Vector2(0.95f, 0.82f), TextAnchor.UpperLeft,
                new Color(0.86f, 0.91f, 0.98f));
        }

        private void BuildQuitPanel()
        {
            _quitPanel = MakePanel("Quit Panel", "QUIT GAME?");
            UiTools.MakeText(_quitPanel.transform, "Quit Message",
                Application.platform == RuntimePlatform.IPhonePlayer
                    ? "Use the device home gesture to close this app."
                    : "Your car, paint and settings are already saved.",
                26, new Vector2(0.05f, 0.42f), new Vector2(0.95f, 0.72f), TextAnchor.MiddleCenter);
            UiTools.MakeAssetButton(_quitPanel.transform, "Confirm Quit", MenuRoot + "Sub_Buttons/Button_YES",
                new Vector2(0.10f, 0.14f), new Vector2(0.44f, 0.32f), ConfirmQuit);
            UiTools.MakeAssetButton(_quitPanel.transform, "Cancel Quit", MenuRoot + "Sub_Buttons/Button_NO",
                new Vector2(0.56f, 0.14f), new Vector2(0.90f, 0.32f), ClosePanels);
        }

        private void BuildWalletPanel()
        {
            _walletPanel = MakePanel("Wallet Panel", "WALLET");
            _walletBalance = UiTools.MakeText(_walletPanel.transform, "Wallet Balance", MenuSettings.Coins.ToString("N0"), 72,
                new Vector2(0.05f, 0.42f), new Vector2(0.95f, 0.68f), TextAnchor.MiddleCenter,
                new Color(1f, 0.90f, 0.42f));
            _bonusText = UiTools.MakeText(_walletPanel.transform, "Bonus Status",
                MenuSettings.BonusAvailable ? "DAILY BONUS READY" : "BONUS CLAIMED TODAY", 26,
                new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.42f), TextAnchor.MiddleCenter,
                new Color(0.70f, 0.94f, 0.74f));
            UiTools.MakeButton(_walletPanel.transform, "Claim Bonus", "CLAIM +250",
                new Vector2(0.25f, 0.12f), new Vector2(0.75f, 0.28f), ClaimBonus, 30,
                UiTools.GetSprite("UI/Menu/ButtonSprite"), new Color(0.18f, 0.40f, 0.24f, 0.98f));
        }

        private void ClaimBonus()
        {
            bool claimed = MenuSettings.ClaimDailyBonus(250);
            if (_coinsText != null) _coinsText.text = MenuSettings.Coins.ToString("N0");
            if (_walletBalance != null) _walletBalance.text = MenuSettings.Coins.ToString("N0");
            if (_bonusText != null)
                _bonusText.text = claimed ? "BONUS CLAIMED TODAY" : "ALREADY CLAIMED";
            if (_garage != null) _garage.RefreshCoins();
        }

        private void OpenGarage()
        {
            ClosePanels();
            if (_garage != null) _garage.Open();
        }

        private void OpenCustomize()
        {
            ClosePanels();
            RefreshPaintLabel();
            _customizePanel.SetActive(true);
        }

        private void OpenSettings()
        {
            ClosePanels();
            RefreshSettingsLabels();
            _settingsPanel.SetActive(true);
        }

        private void OpenCredits()
        {
            ClosePanels();
            _creditsPanel.SetActive(true);
        }

        private void OpenWallet()
        {
            ClosePanels();
            if (_walletBalance != null) _walletBalance.text = MenuSettings.Coins.ToString("N0");
            if (_bonusText != null) _bonusText.text = MenuSettings.BonusAvailable ? "DAILY BONUS READY" : "BONUS CLAIMED TODAY";
            _walletPanel.SetActive(true);
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
            if (_walletPanel != null) _walletPanel.SetActive(false);
        }

        private void OnCarSelected(GameObject prefab, string displayName)
        {
            GameObject visual = GameBootstrap.SetPlayerVisual(_car, prefab, true);
            if (visual == null) return;
            VehicleVisuals visuals = _car.GetComponent<VehicleVisuals>();
            if (visuals != null)
            {
                visuals.FindWheels(visual.transform);
                visuals.FindOriginalInteriorWheel(visual.transform);
                visuals.SetSpinEnabled(false); // freeze skinned wheel bones in showroom
            }
            GameBootstrap.ApplyBodyColor(_car, MenuSettings.BodyColor);
            MenuSettings.SelectCar(prefab.name);
            if (_carNameText != null) _carNameText.text = displayName;
            ClosePanels();
        }

        private void Update()
        {
            if (_playing || _car == null) return;
            // Selected cars stay straight; the rotating showcase platform was removed.
            _car.transform.SetPositionAndRotation(_showroomPosition,
                Quaternion.Euler(0f, _showroomYaw, 0f));
        }

        public void Play()
        {
            if (_playing) return;
            if (_city == null || !_city.HasSafeSpawn)
            {
                if (_carNameText != null) _carNameText.text = "CITY ROAD NOT READY - RESTART GAME";
                Debug.LogError("[CityDrive] Cannot start: no verified road collider/spawn.");
                return;
            }
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
                    "Clearing menu and preparing the road...");
            }
            if (_showroom != null) Destroy(_showroom);
            if (_canvasObject != null) Destroy(_canvasObject);
            if (_previewCamera != null) Destroy(_previewCamera.gameObject);
            if (_previewTexture != null) _previewTexture.Release();
            yield return null;

            if (_loading != null)
                _loading.SetProgress(0.34f, "LOADING CITY AND CAR MODEL",
                    "Placing the selected car on the city road...");
            // Restore city colliders before enabling gravity on the player.
            if (_city != null && _city.Root != null) _city.Root.SetActive(true);
            Physics.SyncTransforms();
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
            if (_camera != null) _camera.enabled = true;
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

        public void ResumeShowroom()
        {
            _playing = false;
            _car.transform.SetPositionAndRotation(_showroomPosition,
                Quaternion.Euler(0f, _showroomYaw, 0f));
        }
    }
}
