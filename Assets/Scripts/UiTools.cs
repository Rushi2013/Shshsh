using System;
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

        public static Sprite GetSprite(string resource)
        {
            Sprite cached;
            if (Sprites.TryGetValue(resource, out cached) && cached != null) return cached;

            Sprite imported = Resources.Load<Sprite>(resource);
            if (imported != null)
            {
                Sprites[resource] = imported;
                return imported;
            }

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

        public static Image MakeImage(Transform parent, string name, Vector2 min, Vector2 max, Color color, Sprite sprite = null)
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

        public static Text MakeText(Transform parent, string name, string content, int size, Vector2 min, Vector2 max,
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

        /// <summary>
        /// Asset-styled menu button. The sprite comes from the user's RCC Pro UI
        /// texture set staged under Resources/UI/Menu; a plain tinted button is used
        /// only if that texture is unavailable.
        /// </summary>
        /// <summary>Button whose artwork already contains its label (Car Menu UI kit).</summary>
        public static Button MakeAssetButton(Transform parent, string name, string resource,
            Vector2 min, Vector2 max, Action onClick, bool interactable = true)
        {
            Image image = MakeImage(parent, name, min, max, Color.white, GetSprite(resource));
            image.preserveAspect = true;
            image.type = Image.Type.Simple;
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.interactable = interactable;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.96f, 0.86f, 1f);
            colors.pressedColor = new Color(0.82f, 0.90f, 1f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.48f, 0.55f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(() => onClick());
            return button;
        }

        public static Button MakeButton(Transform parent, string name, string label, Vector2 min, Vector2 max,
            Action onClick, int fontSize = 30, Sprite sprite = null, Color? tint = null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = go.GetComponent<Image>();
            image.sprite = sprite != null ? sprite : GetSprite("UI/Menu/ButtonSprite");
            image.color = tint ?? new Color(0.10f, 0.17f, 0.26f, 0.94f);
            image.raycastTarget = true;
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;

            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.86f);
            colors.pressedColor = new Color(0.72f, 0.86f, 1f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.45f, 0.48f, 0.52f, 0.6f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(() => onClick());

            if (!string.IsNullOrEmpty(label))
                MakeText(go.transform, "Label", label, fontSize, Vector2.zero, Vector2.one,
                    TextAnchor.MiddleCenter, new Color(0.96f, 0.98f, 1f, 0.98f));
            return button;
        }
    }
}
