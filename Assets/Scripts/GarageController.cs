using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CityDrive
{
    /// <summary>
    /// Car Menu UI v2 garage. Shows nine cars per page with real next/previous
    /// navigation, live thumbnails, selected state and persistent car choice.
    /// All cars are available locally; no fake purchase/locked state is shown.
    /// </summary>
    public sealed class GarageController : MonoBehaviour
    {
        private const string MenuRoot = "UI/CarMenu/";
        private const int PageSize = 9;

        private readonly List<GameObject> _prefabs = new List<GameObject>();
        private readonly List<GameObject> _cards = new List<GameObject>();
        private readonly Dictionary<GameObject, Sprite> _thumbnails = new Dictionary<GameObject, Sprite>();
        private Action<GameObject, string> _onSelect;
        private GameObject _panel;
        private Text _coinsText;
        private Text _pageText;
        private int _page;
        private Coroutine _thumbnailJob;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        public void Build(Transform parent, Action<GameObject, string> onSelect)
        {
            _onSelect = onSelect;
            GameObject[] garage = Resources.LoadAll<GameObject>("GarageCars");
            GameObject[] traffic = Resources.LoadAll<GameObject>("TrafficCars");
            if (garage != null) _prefabs.AddRange(garage);
            if (traffic != null) _prefabs.AddRange(traffic);
            _prefabs.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));

            _panel = UiTools.MakeImage(parent, "Garage Panel", new Vector2(0.04f, 0.07f),
                new Vector2(0.92f, 0.92f), new Color(0.02f, 0.04f, 0.09f, 0.97f)).gameObject;
            _panel.GetComponent<Image>().raycastTarget = true;

            Image carsBadge = UiTools.MakeImage(_panel.transform, "Cars Badge",
                new Vector2(0.02f, 0.86f), new Vector2(0.22f, 0.99f), Color.white,
                UiTools.GetSprite(MenuRoot + "Main_Buttons/Button_CARS"));
            carsBadge.preserveAspect = true;
            UiTools.MakeText(_panel.transform, "Garage Title", "CAR GARAGE", 44,
                new Vector2(0.20f, 0.86f), new Vector2(0.60f, 0.99f), TextAnchor.MiddleLeft,
                new Color(1f, 0.84f, 0.32f));
            _coinsText = UiTools.MakeText(_panel.transform, "Garage Coins", "COINS  " + MenuSettings.Coins.ToString("N0"), 30,
                new Vector2(0.60f, 0.89f), new Vector2(0.78f, 0.99f), TextAnchor.MiddleRight,
                new Color(1f, 0.90f, 0.42f));
            UiTools.MakeAssetButton(_panel.transform, "Close Garage", MenuRoot + "Sub_Buttons/Button_CLOSE",
                new Vector2(0.80f, 0.87f), new Vector2(0.98f, 0.99f), Close);

            for (int i = 0; i < PageSize; i++)
            {
                int col = i % 3;
                int row = i / 3;
                Vector2 min = new Vector2(0.03f + col * 0.31f, 0.54f - row * 0.19f);
                Vector2 max = new Vector2(min.x + 0.29f, min.y + 0.18f);
                GameObject card = UiTools.MakeImage(_panel.transform, "Card " + i, min, max,
                    new Color(0.09f, 0.13f, 0.20f, 0.98f)).gameObject;
                _cards.Add(card);
            }

            UiTools.MakeAssetButton(_panel.transform, "Previous Page", MenuRoot + "Sub_Buttons/Button_PREVIOUS",
                new Vector2(0.05f, 0.02f), new Vector2(0.25f, 0.14f), () => ChangePage(-1));
            _pageText = UiTools.MakeText(_panel.transform, "Page Label", "", 28,
                new Vector2(0.28f, 0.02f), new Vector2(0.72f, 0.14f), TextAnchor.MiddleCenter,
                new Color(0.86f, 0.92f, 1f));
            UiTools.MakeAssetButton(_panel.transform, "Next Page", MenuRoot + "Sub_Buttons/Button_NEXT",
                new Vector2(0.75f, 0.02f), new Vector2(0.95f, 0.14f), () => ChangePage(1));

            _panel.SetActive(false);
        }

        private void ChangePage(int delta)
        {
            int pages = Mathf.Max(1, Mathf.CeilToInt(_prefabs.Count / (float)PageSize));
            _page = (_page + delta + pages) % pages;
            RenderPage();
        }

        private void RenderPage()
        {
            if (_thumbnailJob != null) StopCoroutine(_thumbnailJob);
            for (int i = 0; i < _cards.Count; i++)
            {
                GameObject card = _cards[i];
                if (card == null) continue;
                for (int c = card.transform.childCount - 1; c >= 0; c--)
                    Destroy(card.transform.GetChild(c).gameObject);

                int index = _page * PageSize + i;
                if (index >= _prefabs.Count)
                {
                    card.SetActive(false);
                    continue;
                }
                card.SetActive(true);
                GameObject prefab = _prefabs[index];
                bool selected = prefab.name == MenuSettings.SelectedCar;
                card.GetComponent<Image>().color = selected
                    ? new Color(0.12f, 0.34f, 0.24f, 0.98f)
                    : new Color(0.09f, 0.13f, 0.20f, 0.98f);

                _thumbnails.TryGetValue(prefab, out Sprite thumbnail);
                Image preview = UiTools.MakeImage(card.transform, "Preview",
                    new Vector2(0.02f, 0.24f), new Vector2(0.98f, 0.98f), Color.white, thumbnail);
                preview.preserveAspect = true;
                UiTools.MakeText(card.transform, "Name", CarNames.Display(prefab.name), 20,
                    new Vector2(0.02f, 0.02f), new Vector2(0.52f, 0.22f), TextAnchor.MiddleLeft,
                    new Color(0.86f, 0.91f, 0.98f));
                string sprite = selected ? "Sub_Buttons/Button_SELECTED" : "Sub_Buttons/Button_SELECT";
                UiTools.MakeAssetButton(card.transform, "Select " + prefab.name, MenuRoot + sprite,
                    new Vector2(0.54f, 0.02f), new Vector2(0.98f, 0.22f),
                    selected ? (Action)null : () => Select(prefab), !selected);
            }

            int pages = Mathf.Max(1, Mathf.CeilToInt(_prefabs.Count / (float)PageSize));
            if (_pageText != null) _pageText.text = "PAGE " + (_page + 1) + " / " + pages;
            _thumbnailJob = StartCoroutine(RenderPageThumbnails(_page));
        }

        private IEnumerator RenderPageThumbnails(int page)
        {
            yield return null; // Show the garage before rendering expensive previews.
            for (int i = 0; i < _cards.Count && page == _page; i++)
            {
                int index = page * PageSize + i;
                if (index >= _prefabs.Count) break;
                GameObject prefab = _prefabs[index];
                if (!_thumbnails.TryGetValue(prefab, out Sprite thumbnail) || thumbnail == null)
                {
                    try { thumbnail = RenderThumbnail(prefab); }
                    catch (Exception ex) { Debug.LogWarning("[CityDrive] Garage preview unavailable: " + ex.Message); }
                    _thumbnails[prefab] = thumbnail;
                }
                Transform child = _cards[i].transform.Find("Preview");
                if (child != null && thumbnail != null) child.GetComponent<Image>().sprite = thumbnail;
                yield return null;
            }
            _thumbnailJob = null;
        }

        private static Sprite RenderThumbnail(GameObject prefab)
        {
            const int width = 256;
            const int height = 144;
            GameObject temp = UnityEngine.Object.Instantiate(prefab, new Vector3(1000f, 0f, 0f), Quaternion.identity);
            Bounds bounds = new Bounds(temp.transform.position, Vector3.one);
            bool found = false;
            List<Material> temporaryMaterials = new List<Material>();
            foreach (Renderer renderer in temp.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || renderer.bounds.size == Vector3.zero) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);

                // glTFast materials can render dark in an off-screen preview camera.
                // Use the built-in Standard shader with the original texture and colour.
                Shader standard = Shader.Find("Standard");
                if (standard == null) continue;
                Material[] materials = renderer.sharedMaterials;
                for (int m = 0; m < materials.Length; m++)
                {
                    Material source = materials[m];
                    if (source == null || source.shader == null || !source.shader.isSupported) continue;
                    Material promoted = new Material(standard);
                    temporaryMaterials.Add(promoted);
                    if (source.HasProperty("_MainTex")) promoted.mainTexture = source.mainTexture;
                    if (source.HasProperty("_Color")) promoted.color = source.color;
                    materials[m] = promoted;
                }
                renderer.sharedMaterials = materials;
            }
            if (!found)
            {
                UnityEngine.Object.Destroy(temp);
                foreach (Material material in temporaryMaterials) UnityEngine.Object.Destroy(material);
                return null;
            }

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
            Light fill = cameraObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.9f;
            fill.color = new Color(0.72f, 0.84f, 1f);
            fill.transform.rotation = Quaternion.Euler(25f, 145f, 0f);

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
            foreach (Material material in temporaryMaterials) UnityEngine.Object.Destroy(material);
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private void OnDestroy()
        {
            foreach (Sprite sprite in _thumbnails.Values)
            {
                if (sprite == null) continue;
                Texture texture = sprite.texture;
                Destroy(sprite);
                if (texture != null) Destroy(texture);
            }
            _thumbnails.Clear();
        }

        public void Open()
        {
            if (_panel == null) return;
            RefreshCoins();
            int pages = Mathf.Max(1, Mathf.CeilToInt(_prefabs.Count / (float)PageSize));
            if (_page >= pages) _page = 0;
            _panel.SetActive(true);
            _panel.transform.SetAsLastSibling();
            RenderPage();
        }

        public void Close()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        public void RefreshCoins()
        {
            if (_coinsText != null) _coinsText.text = "COINS  " + MenuSettings.Coins.ToString("N0");
        }

        private void Select(GameObject prefab)
        {
            MenuSettings.SelectCar(prefab.name);
            if (_onSelect != null) _onSelect(prefab, CarNames.Display(prefab.name));
            Close();
        }
    }
}
