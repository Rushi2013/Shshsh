using UnityEngine;
using UnityEngine.UI;

namespace CityDrive
{
    /// <summary>
    /// Full-screen loading experience: AI-generated city backdrop, status lines,
    /// a yellow progress line that advances in real time and a percentage counter.
    /// </summary>
    public sealed class LoadingScreen : MonoBehaviour
    {
        private GameObject _canvasObject;
        private Image _fill;
        private Text _percentage;
        private Text _status;
        private Text _detail;

        public void Show()
        {
            if (_canvasObject != null) return;

            _canvasObject = new GameObject("Loading Screen", typeof(Canvas), typeof(CanvasScaler));
            _canvasObject.transform.SetParent(transform, false);
            Canvas canvas = _canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = _canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            UiTools.MakeImage(_canvasObject.transform, "Backdrop", Vector2.zero, Vector2.one,
                new Color(1f, 1f, 1f, 0.72f), UiTools.GetSprite("UI/loading_bg"));
            UiTools.MakeImage(_canvasObject.transform, "Dim", Vector2.zero, Vector2.one,
                new Color(0.02f, 0.04f, 0.09f, 0.55f));

            UiTools.MakeText(_canvasObject.transform, "Title", "CITY DRIVE", 92,
                new Vector2(0.12f, 0.64f), new Vector2(0.88f, 0.80f), TextAnchor.MiddleCenter,
                new Color(1f, 0.84f, 0.32f));
            UiTools.MakeText(_canvasObject.transform, "Subtitle", "OPEN WORLD BMW DRIVING", 30,
                new Vector2(0.12f, 0.59f), new Vector2(0.88f, 0.65f), TextAnchor.MiddleCenter,
                new Color(0.72f, 0.90f, 1f));

            RectTransform track = UiTools.MakeImage(_canvasObject.transform, "Progress Track",
                new Vector2(0.16f, 0.30f), new Vector2(0.84f, 0.33f),
                new Color(0f, 0f, 0f, 0.65f)).rectTransform;
            _fill = UiTools.MakeImage(track, "Progress Fill", Vector2.zero, Vector2.one,
                new Color(1f, 0.78f, 0.12f, 1f));
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Horizontal;
            _fill.fillAmount = 0f;

            _percentage = UiTools.MakeText(_canvasObject.transform, "Percentage", "0%", 44,
                new Vector2(0.16f, 0.34f), new Vector2(0.84f, 0.42f), TextAnchor.MiddleRight,
                new Color(1f, 0.84f, 0.32f));
            _status = UiTools.MakeText(_canvasObject.transform, "Status", "GAME LOADING", 40,
                new Vector2(0.12f, 0.22f), new Vector2(0.88f, 0.29f), TextAnchor.MiddleLeft);
            _detail = UiTools.MakeText(_canvasObject.transform, "Detail", "Connecting to assets...", 26,
                new Vector2(0.12f, 0.15f), new Vector2(0.88f, 0.21f), TextAnchor.MiddleLeft,
                new Color(0.76f, 0.85f, 0.94f));
            SetProgress(0f, "GAME LOADING", "Preparing city assets...");
        }

        public void SetProgress(float progress, string status, string detail)
        {
            if (_canvasObject == null) Show();
            float value = Mathf.Clamp01(progress);
            if (_fill != null) _fill.fillAmount = value;
            if (_percentage != null) _percentage.text = Mathf.RoundToInt(value * 100f) + "%";
            if (_status != null && !string.IsNullOrEmpty(status)) _status.text = status;
            if (_detail != null && !string.IsNullOrEmpty(detail)) _detail.text = detail;
        }

        public void Hide()
        {
            if (_canvasObject != null) Destroy(_canvasObject);
            _canvasObject = null;
        }
    }
}
