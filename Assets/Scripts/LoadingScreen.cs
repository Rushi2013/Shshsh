using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CityDrive
{
    /// <summary>
    /// Full-screen loading UI. The displayed value always comes from real asset
    /// operations or completed setup stages supplied by GameBootstrap/HomeScreen;
    /// no timer invents progress. The bar eases toward that real value and snaps to
    /// 100% only when the caller reports completion.
    /// </summary>
    public sealed class LoadingScreen : MonoBehaviour
    {
        private static readonly string[] Tips =
        {
            "TIP  •  Hold BRAKE + GAS together for a burnout",
            "TIP  •  Rotate the wheel with your finger; it self-centres on release",
            "TIP  •  Tap CAM to switch chase, wide chase and cabin view",
            "TIP  •  Lift off before a corner to let the tyres recover grip"
        };

        private GameObject _canvasObject;
        private AudioSource _audio;
        private Image _fill;
        private Text _percentage;
        private Text _status;
        private Text _detail;
        private Text _tip;
        private float _targetProgress;
        private float _displayProgress;
        private int _tipIndex = -1;

        public bool IsVisible => _canvasObject != null;

        public void Show(string backgroundResource = "UI/loading_bg")
        {
            if (_canvasObject != null) return;

            _canvasObject = new GameObject("Loading Screen", typeof(Canvas), typeof(CanvasScaler));
            _canvasObject.transform.SetParent(transform, false);
            Canvas canvas = _canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            RectTransform canvasRect = _canvasObject.GetComponent<RectTransform>();
            Rect safe = Screen.safeArea;
            canvasRect.anchorMin = new Vector2(safe.xMin / Mathf.Max(1f, Screen.width),
                safe.yMin / Mathf.Max(1f, Screen.height));
            canvasRect.anchorMax = new Vector2(safe.xMax / Mathf.Max(1f, Screen.width),
                safe.yMax / Mathf.Max(1f, Screen.height));
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;

            CanvasScaler scaler = _canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            UiTools.MakeImage(_canvasObject.transform, "Backdrop", Vector2.zero, Vector2.one,
                Color.white, UiTools.GetSprite(backgroundResource));
            UiTools.MakeImage(_canvasObject.transform, "Dim", Vector2.zero, Vector2.one,
                new Color(0.015f, 0.03f, 0.07f, 0.68f));

            UiTools.MakeText(_canvasObject.transform, "Title", "CITY DRIVE", 92,
                new Vector2(0.12f, 0.66f), new Vector2(0.88f, 0.82f), TextAnchor.MiddleCenter,
                new Color(1f, 0.84f, 0.32f));
            UiTools.MakeText(_canvasObject.transform, "Subtitle", "PREMIUM SEDAN DRIVING", 30,
                new Vector2(0.12f, 0.61f), new Vector2(0.88f, 0.66f), TextAnchor.MiddleCenter,
                new Color(0.72f, 0.90f, 1f));

            RectTransform track = UiTools.MakeImage(_canvasObject.transform, "Progress Track",
                new Vector2(0.16f, 0.30f), new Vector2(0.84f, 0.325f),
                new Color(0f, 0f, 0f, 0.72f)).rectTransform;
            _fill = UiTools.MakeImage(track, "Progress Fill", Vector2.zero, Vector2.one,
                new Color(1f, 0.78f, 0.12f, 1f));
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Horizontal;
            _fill.fillAmount = 0f;

            _percentage = UiTools.MakeText(_canvasObject.transform, "Percentage", "0%", 44,
                new Vector2(0.16f, 0.34f), new Vector2(0.84f, 0.43f), TextAnchor.MiddleRight,
                new Color(1f, 0.84f, 0.32f));
            _status = UiTools.MakeText(_canvasObject.transform, "Status", "GAME LOADING", 40,
                new Vector2(0.12f, 0.22f), new Vector2(0.88f, 0.29f), TextAnchor.MiddleLeft);
            _detail = UiTools.MakeText(_canvasObject.transform, "Detail", "Preparing city assets...", 26,
                new Vector2(0.12f, 0.15f), new Vector2(0.88f, 0.21f), TextAnchor.MiddleLeft,
                new Color(0.76f, 0.85f, 0.94f));
            _tip = UiTools.MakeText(_canvasObject.transform, "Tip", Tips[0], 24,
                new Vector2(0.12f, 0.075f), new Vector2(0.88f, 0.13f), TextAnchor.MiddleCenter,
                new Color(0.66f, 0.80f, 0.92f));

            StartLoadingAudio();
            SetProgress(0f, "GAME LOADING", "Preparing city assets...");
        }

        private void StartLoadingAudio()
        {
            if (_audio != null) return;
            GameObject audioObject = new GameObject("Loading Audio");
            audioObject.transform.SetParent(transform, false);
            _audio = audioObject.AddComponent<AudioSource>();
            _audio.clip = ProceduralAudio.LoadingAmbient();
            _audio.loop = true;
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            _audio.volume = 0f;
            _audio.Play();
            StartCoroutine(FadeLoadingAudio(0.22f, false));
        }

        /// <summary>Sets the real progress value reported by the loading operation.</summary>
        public void SetProgress(float progress, string status, string detail)
        {
            if (_canvasObject == null) Show();
            _targetProgress = Mathf.Clamp01(progress);
            if (_targetProgress >= 0.999f) _displayProgress = 1f; // never leave 100% pending
            if (!string.IsNullOrEmpty(status) && _status != null) _status.text = status;
            if (!string.IsNullOrEmpty(detail) && _detail != null) _detail.text = detail;

            int tip = Mathf.Clamp(Mathf.FloorToInt(_targetProgress * Tips.Length), 0, Tips.Length - 1);
            if (tip != _tipIndex && _tip != null)
            {
                _tipIndex = tip;
                _tip.text = Tips[tip];
            }
        }

        private void Update()
        {
            if (_canvasObject == null) return;
            if (_displayProgress < _targetProgress)
                _displayProgress = Mathf.MoveTowards(_displayProgress, _targetProgress,
                    Time.unscaledDeltaTime * 1.4f);
            if (_fill != null) _fill.fillAmount = _displayProgress;
            if (_percentage != null)
                _percentage.text = Mathf.RoundToInt(_displayProgress * 100f) + "%";
        }

        public void Hide()
        {
            if (_canvasObject != null)
            {
                Destroy(_canvasObject);
                _canvasObject = null;
            }
            if (_audio != null)
            {
                AudioSource source = _audio;
                _audio = null;
                StartCoroutine(FadeLoadingAudio(0.25f, true, source));
            }
            _targetProgress = 0f;
            _displayProgress = 0f;
            _tipIndex = -1;
        }

        private IEnumerator FadeLoadingAudio(float duration, bool fadeOut, AudioSource source = null)
        {
            AudioSource target = source != null ? source : _audio;
            if (target == null) yield break;
            float start = target.volume;
            float end = fadeOut ? 0f : 0.28f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                target.volume = Mathf.Lerp(start, end, Mathf.Clamp01(t / duration));
                yield return null;
            }
            target.volume = end;
            if (fadeOut)
            {
                target.Stop();
                Destroy(target.gameObject);
            }
        }
    }
}
