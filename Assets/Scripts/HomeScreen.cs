using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CityDrive
{
    /// <summary>
    /// Showroom screen. The same car model rotates at a fixed position using
    /// frame-rate-independent yaw. PLAY performs real setup stages (showroom teardown,
    /// car placement, city activation, engine start) and reports each as loading
    /// progress instead of using a decorative timer.
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

            _canvasObject = new GameObject("Home UI", typeof(Canvas), typeof(CanvasScaler));
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

            UiTools.MakeText(_canvasObject.transform, "Home Title", "CITY DRIVE", 80,
                new Vector2(0.1f, 0.82f), new Vector2(0.9f, 0.94f), TextAnchor.MiddleCenter,
                new Color(1f, 0.84f, 0.30f));
            _carNameText = UiTools.MakeText(_canvasObject.transform, "Home Car", "BMW 330i xDrive", 36,
                new Vector2(0.1f, 0.75f), new Vector2(0.9f, 0.82f), TextAnchor.MiddleCenter,
                new Color(0.70f, 0.90f, 1f));
            UiTools.MakeText(_canvasObject.transform, "Home Hint",
                "OPEN WORLD  •  BURNOUTS  •  DRIFT  •  3 CAMERAS", 26,
                new Vector2(0.1f, 0.16f), new Vector2(0.9f, 0.22f), TextAnchor.MiddleCenter,
                new Color(0.84f, 0.88f, 0.94f));

            GameObject buttonObject = new GameObject("Play Button", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(_canvasObject.transform, false);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.40f, 0.28f);
            buttonRect.anchorMax = new Vector2(0.60f, 0.47f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.sprite = UiTools.GetSprite("UI/icon_play");
            buttonImage.color = new Color(1f, 0.92f, 0.66f, 1f);
            buttonImage.raycastTarget = true;
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(Play);
            UiTools.MakeText(buttonObject.transform, "Play Label", "PLAY", 32,
                new Vector2(0f, -0.35f), new Vector2(1f, 0.12f), TextAnchor.MiddleCenter,
                new Color(0.06f, 0.09f, 0.13f));

            GameObject garageObject = new GameObject("Garage Button", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Button));
            garageObject.transform.SetParent(_canvasObject.transform, false);
            RectTransform garageRect = garageObject.GetComponent<RectTransform>();
            garageRect.anchorMin = new Vector2(0.27f, 0.28f);
            garageRect.anchorMax = new Vector2(0.38f, 0.46f);
            garageRect.offsetMin = Vector2.zero;
            garageRect.offsetMax = Vector2.zero;
            Image garageImage = garageObject.GetComponent<Image>();
            garageImage.color = new Color(0.12f, 0.62f, 0.86f, 0.96f);
            Button garageButton = garageObject.GetComponent<Button>();
            garageButton.targetGraphic = garageImage;
            garageButton.onClick.AddListener(OpenGarage);
            UiTools.MakeText(garageObject.transform, "Garage Label", "GARAGE", 24,
                new Vector2(0f, -0.35f), new Vector2(1f, 0.12f), TextAnchor.MiddleCenter,
                new Color(0.03f, 0.05f, 0.08f));

            _garage = gameObject.AddComponent<GarageController>();
            _garage.Build(_canvasObject.transform, OnCarSelected);
        }

        private void OpenGarage()
        {
            if (_garage != null) _garage.Open();
        }

        private void OnCarSelected(GameObject prefab, string displayName)
        {
            GameObject visual = GameBootstrap.SetPlayerVisual(_car, prefab);
            if (visual == null) return;
            VehicleVisuals visuals = _car.GetComponent<VehicleVisuals>();
            if (visuals != null) visuals.FindWheels(_car.transform);
            if (_carNameText != null) _carNameText.text = displayName;
        }

        private void Update()
        {
            if (_playing || _car == null) return;

            // Frame-rate-independent, fixed-centre yaw. No transform drift or stutter.
            _showroomYaw = (_showroomYaw + 26f * Time.unscaledDeltaTime) % 360f;
            _car.transform.SetPositionAndRotation(_showroomPosition,
                Quaternion.Euler(0f, _showroomYaw, 0f));

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began) CheckPlayTap(touch.position);
            }
            else if (Input.GetMouseButtonDown(0))
            {
                CheckPlayTap(Input.mousePosition);
            }
        }

        private void CheckPlayTap(Vector2 pixel)
        {
            float x = pixel.x / Mathf.Max(1f, Screen.width);
            float y = pixel.y / Mathf.Max(1f, Screen.height);
            if (x >= 0.36f && x <= 0.64f && y >= 0.25f && y <= 0.50f) Play();
        }

        public void Play()
        {
            if (_playing) return;
            _playing = true; // pauses showroom rotation immediately
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
                    "Placing the sedan on the city road...");
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
            if (_audio != null) _audio.SetActive(true);
            _camera.fieldOfView = 64f;
            _camera.farClipPlane = 1200f;
            _camera.transform.position = _spawn + new Vector3(0f, 3.6f, -7.6f);
            _camera.transform.LookAt(_spawn + Vector3.up);
            yield return null;

            // All real work is finished before reporting 100%.
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
