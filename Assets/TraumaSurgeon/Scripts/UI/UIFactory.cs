using System;
using TraumaSurgeon.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TraumaSurgeon.UI
{
    /// <summary>
    /// Builds uGUI hierarchies in code. Every screen in the game is assembled through these
    /// helpers, which keeps the project free of binary .prefab UI assets and makes the layout
    /// reviewable in source control.
    /// </summary>
    public static class UIFactory
    {
        private static Sprite _whiteSprite;

        /// <summary>
        /// A 4x4 white sprite. Image.fillAmount only works on a sprite-backed Image, so every
        /// progress bar needs one; Unity has no built-in runtime sprite we can rely on.
        /// </summary>
        public static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite != null)
                {
                    return _whiteSprite;
                }

                var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false) { name = "TS_White" };
                var pixels = new Color32[16];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = new Color32(255, 255, 255, 255);
                }

                texture.SetPixels32(pixels);
                texture.Apply();

                _whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f);
                _whiteSprite.name = "TS_WhiteSprite";
                return _whiteSprite;
            }
        }

        // ---- Core objects -----------------------------------------------------

        public static Canvas CreateCanvas(string name, int sortOrder, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;

            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Stretches a rect to fill its parent with the given padding.</summary>
        public static RectTransform Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
            return rect;
        }

        public static RectTransform Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        public static Image CreatePanel(string name, Transform parent, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static Text CreateText(string name, Transform parent, string content, int size,
            Color color, TextAnchor anchor = TextAnchor.UpperLeft, FontStyle style = FontStyle.Normal)
        {
            RectTransform rect = CreateRect(name, parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = UITheme.Font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
            return text;
        }

        public static Button CreateButton(string name, Transform parent, string label,
            Action onClick, int fontSize = UITheme.FontBody)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = UITheme.ButtonNormal;

            Button button = rect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            colors.pressedColor = new Color(0.8f, 0.95f, 1f, 1f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.targetGraphic = image;

            Text text = CreateText("Label", rect, label, fontSize, UITheme.TextPrimary, TextAnchor.MiddleCenter);
            Stretch((RectTransform)text.transform, 8f);

            button.onClick.AddListener(() =>
            {
                if (AudioManager.Exists)
                {
                    AudioManager.Instance.PlayUI(SoundId.UiClick, 0.6f);
                }

                onClick?.Invoke();
            });

            AddHoverSound(button);
            return button;
        }

        private static void AddHoverSound(Button button)
        {
            EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entry.callback.AddListener(_ =>
            {
                if (AudioManager.Exists)
                {
                    AudioManager.Instance.PlayUI(SoundId.UiHover, 0.25f);
                }
            });
            trigger.triggers.Add(entry);
        }

        /// <summary>Vertical layout container with automatic spacing.</summary>
        public static VerticalLayoutGroup CreateVerticalGroup(string name, Transform parent, float spacing,
            RectOffset padding = null, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            RectTransform rect = CreateRect(name, parent);
            VerticalLayoutGroup group = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = spacing;
            group.padding = padding ?? new RectOffset(0, 0, 0, 0);
            group.childAlignment = alignment;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            group.childControlWidth = true;
            group.childControlHeight = true;
            return group;
        }

        public static HorizontalLayoutGroup CreateHorizontalGroup(string name, Transform parent, float spacing,
            RectOffset padding = null, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            RectTransform rect = CreateRect(name, parent);
            HorizontalLayoutGroup group = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.spacing = spacing;
            group.padding = padding ?? new RectOffset(0, 0, 0, 0);
            group.childAlignment = alignment;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            group.childControlWidth = true;
            group.childControlHeight = true;
            return group;
        }

        public static LayoutElement SetSize(GameObject go, float minHeight, float preferredHeight = -1f,
            float minWidth = -1f, float preferredWidth = -1f)
        {
            LayoutElement element = go.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = go.AddComponent<LayoutElement>();
            }

            element.minHeight = minHeight;
            element.preferredHeight = preferredHeight >= 0f ? preferredHeight : minHeight;
            if (minWidth >= 0f)
            {
                element.minWidth = minWidth;
            }

            if (preferredWidth >= 0f)
            {
                element.preferredWidth = preferredWidth;
            }

            return element;
        }

        /// <summary>Scrollable content area. Returns the content transform to parent items to.</summary>
        public static RectTransform CreateScrollView(string name, Transform parent, out ScrollRect scrollRect)
        {
            RectTransform root = CreateRect(name, parent);
            Image bg = root.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.16f);

            scrollRect = root.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 32f;

            RectTransform viewport = CreateRect("Viewport", root);
            Stretch(viewport, 4f);

            // RectMask2D, not Mask. A Mask needs a Graphic to write the stencil, and the UI shader
            // alpha-clips that graphic with clip(a - 0.001). Canvas vertex colours are quantised to
            // bytes, so a "nearly invisible" alpha of 0.001 becomes 0, the whole mask quad is
            // discarded, the stencil is never written, and every child of the scroll view vanishes.
            // RectMask2D clips by rectangle instead - no graphic, no alpha, nothing to get wrong.
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = new Vector2(0f, 0f);
            content.offsetMax = new Vector2(0f, 0f);

            VerticalLayoutGroup group = content.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = 8f;
            group.padding = new RectOffset(10, 10, 10, 10);
            group.childForceExpandHeight = false;
            group.childControlHeight = true;
            group.childControlWidth = true;
            group.childForceExpandWidth = true;

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            return content;
        }

        /// <summary>Horizontal progress/fill bar. Returns the fill image.</summary>
        public static Image CreateBar(string name, Transform parent, Color fillColor, float height = 14f)
        {
            RectTransform root = CreateRect(name, parent);
            Image bg = root.gameObject.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.10f);
            SetSize(root.gameObject, height);

            RectTransform fillRect = CreateRect("Fill", root);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);

            Image fill = fillRect.gameObject.AddComponent<Image>();
            fill.sprite = WhiteSprite;
            fill.color = fillColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
            return fill;
        }

        public static Slider CreateSlider(string name, Transform parent, float min, float max, float value,
            Action<float> onChanged)
        {
            RectTransform root = CreateRect(name, parent);
            SetSize(root.gameObject, 24f);

            Image bg = root.gameObject.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.10f);

            Slider slider = root.gameObject.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = false;

            RectTransform fillArea = CreateRect("FillArea", root);
            Anchor(fillArea, new Vector2(0f, 0.25f), new Vector2(1f, 0.75f), new Vector2(6f, 0f), new Vector2(-6f, 0f));

            RectTransform fillRect = CreateRect("Fill", fillArea);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fill = fillRect.gameObject.AddComponent<Image>();
            fill.color = UITheme.Accent;

            RectTransform handleArea = CreateRect("HandleArea", root);
            Anchor(handleArea, Vector2.zero, Vector2.one, new Vector2(6f, 0f), new Vector2(-6f, 0f));

            RectTransform handleRect = CreateRect("Handle", handleArea);
            handleRect.sizeDelta = new Vector2(16f, 24f);
            Image handle = handleRect.gameObject.AddComponent<Image>();
            handle.color = UITheme.TextPrimary;

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.value = Mathf.Clamp(value, min, max);
            slider.onValueChanged.AddListener(v => onChanged?.Invoke(v));
            return slider;
        }

        public static Toggle CreateToggle(string name, Transform parent, string label, bool value,
            Action<bool> onChanged)
        {
            RectTransform root = CreateRect(name, parent);
            SetSize(root.gameObject, 28f);

            Toggle toggle = root.gameObject.AddComponent<Toggle>();

            RectTransform boxRect = CreateRect("Box", root);
            Anchor(boxRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            boxRect.sizeDelta = new Vector2(22f, 22f);
            boxRect.anchoredPosition = new Vector2(13f, 0f);
            Image box = boxRect.gameObject.AddComponent<Image>();
            box.color = UITheme.ButtonNormal;

            RectTransform checkRect = CreateRect("Check", boxRect);
            Stretch(checkRect, 4f);
            Image check = checkRect.gameObject.AddComponent<Image>();
            check.color = UITheme.Accent;

            Text text = CreateText("Label", root, label, UITheme.FontBody, UITheme.TextPrimary,
                TextAnchor.MiddleLeft);
            Anchor((RectTransform)text.transform, Vector2.zero, Vector2.one, new Vector2(34f, 0f), Vector2.zero);

            toggle.targetGraphic = box;
            toggle.graphic = check;
            toggle.isOn = value;
            toggle.onValueChanged.AddListener(v => onChanged?.Invoke(v));
            return toggle;
        }

        /// <summary>Label + value row used throughout the chart and report screens.</summary>
        public static Text CreateRow(Transform parent, string label, string value, Color valueColor,
            int fontSize = UITheme.FontBody)
        {
            HorizontalLayoutGroup row = CreateHorizontalGroup("Row_" + label, parent, 8f);
            SetSize(row.gameObject, fontSize + 10f);

            Text labelText = CreateText("Label", row.transform, label, fontSize, UITheme.TextMuted,
                TextAnchor.MiddleLeft);
            SetSize(labelText.gameObject, fontSize + 8f, fontSize + 8f, 140f, 220f);

            Text valueText = CreateText("Value", row.transform, value, fontSize, valueColor,
                TextAnchor.MiddleRight);
            SetSize(valueText.gameObject, fontSize + 8f);
            return valueText;
        }

        public static Image CreateDivider(Transform parent)
        {
            Image image = CreatePanel("Divider", parent, UITheme.Divider);
            SetSize(image.gameObject, 1f);
            return image;
        }

        /// <summary>Title + optional subtitle header block.</summary>
        public static void CreateHeader(Transform parent, string title, string subtitle)
        {
            Text titleText = CreateText("Title", parent, title, UITheme.FontHeading, UITheme.Accent,
                TextAnchor.UpperLeft, FontStyle.Bold);
            SetSize(titleText.gameObject, UITheme.FontHeading + 10f);

            if (!string.IsNullOrEmpty(subtitle))
            {
                Text sub = CreateText("Subtitle", parent, subtitle, UITheme.FontSmall, UITheme.TextMuted);
                SetSize(sub.gameObject, UITheme.FontSmall + 8f);
            }

            CreateDivider(parent);
        }
    }
}
