using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CityDrive
{
    /// <summary>
    /// Home screen: the actual BMW model rotates on a circular stand in 3D.
    /// Tapping PLAY shows a second fast loading screen, then the same car is placed
    /// on a city road and the chase camera + touch controls take over.
    /// </summary>
    public sealed class HomeScreen : MonoBehaviour
    {
        private CityBuilder _city;
        private GameObject _car;
        private ArcadeCarController _controller;
        private ChaseCamera _chase;
        private Camera _camera;
        private LoadingScreen _loading;
        private Vector3 _spawn;
        private Quaternion _spawnRotation;
        private Action _onPlay;
        private GameObject _showroom;
        private GameObject _canvasObject;
        private bool _playing;

        public bool PlayPressed => _playing;

        public void Show(CityBuilder city, GameObject car, ArcadeCarController controller,
            ChaseCamera chase, Camera camera, Vector3 spawn, Quaternion spawnRotation,
            LoadingScreen loading, Action onPlay)
        {
            _city = city;
            _car = car;
            _controller = controller;
            _chase = chase;
            _camera = camera;
            _spawn = spawn;
            _spawnRotation = spawnRotation;
            _loading = loading;
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

            _car.transform.position = new Vector3(0f, 0.72f, 0f);
            _car.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            _camera.transform.position = new Vector3(0f, 2.1f, -7.2f);
            _camera.transform.LookAt(new Vector3(0f, 0.85f, 0f));
            _camera.fieldOfView = 54f;
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
            SetMaterial(platform, new Color(0.10f, 0.14f, 0.19f), null);

            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Stand Glow";
            ring.transform.SetParent(_showroom.transform, false);
            ring.transform.localScale = new Vector3(6.25f, 0.045f, 6.25f);
            ring.transform.position = new Vector3(0f, -0.24f, 0f);
            SetMaterial(ring, new Color(0.10f, 0.72f, 0.95f), null);

            Texture2D showroomTexture = Resources.Load<Texture2D>("UI/showroom_bg");
            if (showroomTexture != null)
            {
                GameObject backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
                backdrop.name = "Showroom Backdrop";
                backdrop.transform.SetParent(_showroom.transform, false);
                backdrop.transform.position = new Vector3(0f, 2.5f, 10f);
                backdrop.transform.localScale = new Vector3(24f, 12f, 1f);
                backdrop.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                SetMaterial(backdrop, Color.white, showroomTexture);
            }

            GameObject lamp = new GameObject("Showroom Light", typeof(Light));
            lamp.transform.SetParent(_showroom.transform, false);
            lamp.transform.position = new Vector3(0f, 5f, -3f);
            Light light = lamp.GetComponent<Light>();
            light.type = LightType.Point;
            light.intensity = 3f;
            light.range = 22f;
            light.color = new Color(0.62f, 0.86f, 1f);
        }

        private static void SetMaterial(GameObject go, Color color, Texture texture)
        {
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            Shader shader = Shader.Find("Unlit/Texture");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return;
            Material material = new Material(shader);
            material.color = color;
            if (texture != null) material.mainTexture = texture;
            renderer.material = material;
        }

        private void BuildUi()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            _canvasObject = new GameObject("Home UI", typeof(Canvas), typeof(CanvasScaler));
            _canvasObject.transform.SetParent(transform, false);
            Canvas canvas = _canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            CanvasScaler scaler = _canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            UiTools.MakeText(_canvasObject.transform, "Home Title", "CITY DRIVE", 80,
                new Vector2(0.1f, 0.82f), new Vector2(0.9f, 0.94f), TextAnchor.MiddleCenter,
                new Color(1f, 0.84f, 0.30f));
            UiTools.MakeText(_canvasObject.transform, "Home Car", "BMW 330i xDrive", 36,
                new Vector2(0.1f, 0.75f), new Vector2(0.9f, 0.82f), TextAnchor.MiddleCenter,
                new Color(0.70f, 0.90f, 1f));
            UiTools.MakeText(_canvasObject.transform, "Home Hint", "DRIVE ANYWHERE  •  OPEN WORLD  •  DRIFT", 26,
                new Vector2(0.1f, 0.16f), new Vector2(0.9f, 0.22f), TextAnchor.MiddleCenter,
                new Color(0.84f, 0.88f, 0.94f));

            GameObject buttonObject = new GameObject("Play Button", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(_canvasObject.transform, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.40f, 0.28f);
            rect.anchorMax = new Vector2(0.60f, 0.47f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
        }

        private void Update()
        {
            if (_playing) return;
            if (_car != null)
                _car.transform.Rotate(0f, 26f * Time.unscaledDeltaTime, 0f, Space.World);

            // Direct touch/mouse hit test as a reliable fallback to the UI Button.
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
            float x = pixel.x / Screen.width;
            float y = pixel.y / Screen.height;
            if (x >= 0.36f && x <= 0.64f && y >= 0.25f && y <= 0.50f) Play();
        }

        public void Play()
        {
            if (_playing) return;
            _playing = true;
            StartCoroutine(StartGameplay());
        }

        private IEnumerator StartGameplay()
        {
            if (_loading != null)
            {
                _loading.Show("UI/loading_bg_2");
                _loading.SetProgress(0.10f, "LOADING CITY AND CAR MODEL", "Preparing roads, buildings and BMW 330i...");
            }
            yield return new WaitForSecondsRealtime(0.15f);

            if (_loading != null)
                _loading.SetProgress(0.55f, "LOADING CITY AND CAR MODEL", "Textures and physics ready");
            yield return new WaitForSecondsRealtime(0.15f);

            if (_loading != null)
                _loading.SetProgress(0.85f, "STARTING ENGINE", "Getting behind the wheel...");
            yield return new WaitForSecondsRealtime(0.15f);

            if (_showroom != null) Destroy(_showroom);
            if (_canvasObject != null) Destroy(_canvasObject);

            if (_city != null && _city.Root != null) _city.Root.SetActive(true);

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
            _controller.enabled = true;
            _chase.enabled = true;
            _camera.fieldOfView = 65f;
            _camera.farClipPlane = 1200f;
            _camera.transform.position = _spawn + new Vector3(0f, 4f, -8f);
            _camera.transform.LookAt(_spawn + Vector3.up);

            if (_loading != null)
            {
                _loading.SetProgress(1f, "READY", "City Drive is ready");
                yield return new WaitForSecondsRealtime(0.2f);
                _loading.Hide();
            }

            if (_onPlay != null) _onPlay();
        }
    }
}
