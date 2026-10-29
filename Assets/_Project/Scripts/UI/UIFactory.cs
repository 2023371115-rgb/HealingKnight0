using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HealingKnight.UI
{
    /// <summary>
    /// Construye la interfaz desde código (placeholders). Así no hay que armar Canvas a mano en cada escena.
    /// Cuando tengas el arte final puedes reemplazar estas pantallas por prefabs hechos en el Editor.
    /// </summary>
    public static class UIFactory
    {
        public static readonly Color PanelColor = new Color(0.08f, 0.05f, 0.09f, 0.85f);
        public static readonly Color ButtonColor = new Color(0.85f, 0.25f, 0.35f, 1f);
        public static readonly Color ButtonDisabledColor = new Color(0.35f, 0.3f, 0.33f, 1f);
        public static readonly Color TextColor = new Color(1f, 0.96f, 0.92f, 1f);
        public static readonly Color AccentColor = new Color(0.4f, 0.9f, 1f, 1f);

        public static Canvas CreateCanvas(string name, int sortingOrder, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null) go.transform.SetParent(parent, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            EnsureEventSystem();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null || UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        public static RectTransform Rect(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            return rt != null ? rt : go.AddComponent<RectTransform>();
        }

        /// <summary>Coloca un elemento con anclas (0..1) y tamaño/posición en píxeles de referencia.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float padding = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
            return rt;
        }

        public static Image Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        public static TextMeshProUGUI Text(Transform parent, string text, float size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center, string name = "Text")
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            return tmp;
        }

        public static Button Button(Transform parent, string label, Action onClick, Vector2 size, float fontSize = 36f)
        {
            Image bg = Panel(parent, "Button_" + label, ButtonColor);
            Rect(bg.gameObject).sizeDelta = size;

            var button = bg.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.85f, 0.85f);
            colors.selectedColor = new Color(1f, 0.85f, 0.6f);
            colors.pressedColor = new Color(0.8f, 0.6f, 0.6f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(() => onClick());

            TextMeshProUGUI text = Text(bg.transform, label, fontSize, TextColor);
            Stretch(text.rectTransform, 8f);
            return button;
        }

        public static void SetButtonLabel(Button button, string label)
        {
            var tmp = button.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = label;
        }

        public static VerticalLayoutGroup VerticalLayout(GameObject go, float spacing, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static HorizontalLayoutGroup HorizontalLayout(GameObject go, float spacing, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static UIBar Bar(Transform parent, string name, Color fill, Vector2 size)
        {
            Image bg = Panel(parent, name, new Color(0f, 0f, 0f, 0.6f));
            Rect(bg.gameObject).sizeDelta = size;

            Image fillImage = Panel(bg.transform, "Fill", fill);
            RectTransform fr = fillImage.rectTransform;
            fr.anchorMin = new Vector2(0f, 0f);
            fr.anchorMax = new Vector2(1f, 1f);
            fr.pivot = new Vector2(0f, 0.5f);
            fr.offsetMin = new Vector2(4f, 4f);
            fr.offsetMax = new Vector2(-4f, -4f);

            var bar = bg.gameObject.AddComponent<UIBar>();
            bar.Init(fillImage);
            return bar;
        }

        public static void Select(Selectable selectable)
        {
            if (selectable == null || EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }
    }
}
