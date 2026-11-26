using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CityDrive
{
    /// <summary>
    /// Runtime garage screen. Loads every supplied car prefab from Resources,
    /// renders a live thumbnail for each one, and calls back when SELECT is
    /// tapped so the showroom and the gameplay car both change immediately.
    /// </summary>
    public sealed class GarageController : MonoBehaviour
    {
        private readonly List<GameObject> _prefabs = new List<GameObject>();
        private Action<GameObject, string> _onSelect;
        private GameObject _panel;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        public void Build(Transform parent, Action<GameObject, string> onSelect)
        {
            _onSelect = onSelect;
            GameObject[] garage = Resources.LoadAll<GameObject>("GarageCars");
            GameObject[] traffic = Resources.LoadAll<GameObject>("TrafficCars");
            if (garage != null) _prefabs.AddRange(garage);
            if (traffic != null) _prefabs.AddRange(traffic);

            _panel = new GameObject("Garage Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _panel.transform.SetParent(parent, false);
            RectTransform panelRect = _panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.06f, 0.08f);
            panelRect.anchorMax = new Vector2(0.94f, 0.90f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            Image panelImage = _panel.GetComponent<Image>();
            panelImage.color = new Color(0.03f, 0.05f, 0.09f, 0.95f);

            UiTools.MakeText(_panel.transform, "Garage Title", "CAR GARAGE", 52,
                new Vector2(0f, 0.88f), new Vector2(1f, 1f), TextAnchor.MiddleCenter,
                new Color(1f, 0.84f, 0.32f));
            MakeButton(_panel.transform, "Close Garage", "CLOSE", new Vector2(0.84f, 0.90f),
                new Vector2(0.98f, 0.98f), Close);

            int columns = 3;
            for (int i = 0; i < _prefabs.Count; i++)
            {
                GameObject prefab = _prefabs[i];
                int col = i % columns;
                int row = i / columns;
                Vector2 min = new Vector2(0.04f + col * 0.31f, 0.62f - row * 0.20f);
                Vector2 max = new Vector2(min.x + 0.28f, min.y + 0.18f);
                BuildCard(prefab, min, max);
            }

            _panel.SetActive(false);
        }

        private void BuildCard(GameObject prefab, Vector2 min, Vector2 max)
        {
            GameObject card = new GameObject("Card " + prefab.name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            card.transform.SetParent(_panel.transform, false);
            RectTransform rect = card.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            card.GetComponent<Image>().color = new Color(0.10f, 0.14f, 0.21f, 0.96f);

            Sprite thumbnail = RenderThumbnail(prefab);
            Image preview = UiTools.MakeImage(card.transform, "Preview",
                new Vector2(0.02f, 0.22f), new Vector2(0.98f, 0.98f), Color.white, thumbnail);
            preview.preserveAspect = true;

            UiTools.MakeText(card.transform, "Name", prefab.name, 20,
                new Vector2(0.02f, 0.02f), new Vector2(0.62f, 0.22f), TextAnchor.MiddleLeft,
                new Color(0.86f, 0.91f, 0.98f));
            MakeButton(card.transform, "Select " + prefab.name, "SELECT",
                new Vector2(0.64f, 0.02f), new Vector2(0.98f, 0.22f),
                () => Select(prefab));
        }

        private static Sprite RenderThumbnail(GameObject prefab)
        {
            const int width = 256;
            const int height = 144;
            GameObject temp = Instantiate(prefab, new Vector3(1000f, 0f, 0f), Quaternion.identity);
            Bounds bounds = new Bounds(temp.transform.position, Vector3.one);
            foreach (Renderer renderer in temp.GetComponentsInChildren<Renderer>())
                bounds.Encapsulate(renderer.bounds);

            float radius = Mathf.Max(2f, bounds.size.magnitude * 0.5f);
            Vector3 centre = bounds.center;
            GameObject cameraObject = new GameObject("Thumbnail Camera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.transform.position = centre + new Vector3(1f, 0.55f, 1f) * radius * 2.1f;
            camera.transform.LookAt(centre);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.08f, 0.13f, 1f);
            camera.fieldOfView = 38f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = radius * 12f;
            Light light = cameraObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
            light.transform.rotation = Quaternion.Euler(40f, -35f, 0f);

            RenderTexture rt = RenderTexture.GetTemporary(width, height, 24);
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.Destroy(cameraObject);
            UnityEngine.Object.Destroy(temp);
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static void MakeButton(Transform parent, string name, string label, Vector2 min, Vector2 max, Action action)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.12f, 0.62f, 0.86f, 0.96f);
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => action());
            UiTools.MakeText(go.transform, "Label", label, 20, Vector2.zero, Vector2.one,
                TextAnchor.MiddleCenter, new Color(0.03f, 0.05f, 0.08f));
        }

        public void Open()
        {
            if (_panel != null) _panel.SetActive(true);
        }

        private void Close()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        private void Select(GameObject prefab)
        {
            if (_onSelect != null) _onSelect(prefab, prefab.name);
            Close();
        }
    }
}
