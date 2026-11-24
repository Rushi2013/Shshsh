using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CityDrive
{
    /// <summary>Small shared helpers for the runtime-generated UI (no scene authoring needed).</summary>
    public static class UiTools
    {
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        private static Font _font;

        public static Sprite Sprite(string resource)
        {
            Sprite cached;
            if (Sprites.TryGetValue(resource, out cached) && cached != null) return cached;

            Texture2D texture = Resources.Load<Texture2D>(resource);
            if (texture == null) return null;

            Sprite sprite = UnityEngine.Sprite.Create(texture,
                new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            Sprites[resource] = sprite;
            return sprite;
        }

        public static Font Font()
        {
            if (_font != null) return _font;
            try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (_font == null)
            {
                try { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
            }
            return _font;
        }

        public static Image Image(Transform parent, string name, Vector2 min, Vector2 max, Color color, Sprite sprite = null)
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
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        public static Text Text(Transform parent, string name, string content, int size, Vector2 min, Vector2 max,
            TextAnchor alignment, Color? color = null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Text text = go.GetComponent<Text>();
            Font font = Font();
            if (font != null) text.font = font;
            text.text = content;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color ?? new Color(1f, 1f, 1f, 0.94f);
            text.raycastTarget = false;
            return text;
        }
    }
}
