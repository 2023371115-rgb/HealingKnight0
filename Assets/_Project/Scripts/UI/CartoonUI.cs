using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HealingKnight.UI
{
    /// <summary>
    /// Piezas de interfaz con el estilo del juego (caricatura tipo Hora de Aventura): formas redondas, contorno grueso
    /// café oscuro, colores planos, brillo ovalado arriba y sombra abajo. Todo se dibuja por código (no hay que armar
    /// nada a mano en el Editor). La letra es Titan One (Art/UI/Fuentes); si no está, usa la de TextMeshPro.
    /// </summary>
    public static class CartoonUI
    {
        public static readonly Color Outline = new Color(0.2f, 0.05f, 0.09f, 1f);
        public static readonly Color Cream = new Color(1f, 0.96f, 0.9f, 1f);
        public static readonly Color Red = new Color(0.93f, 0.27f, 0.33f, 1f);
        public static readonly Color Pink = new Color(0.98f, 0.55f, 0.62f, 1f);
        public static readonly Color Mint = new Color(0.45f, 0.88f, 0.6f, 1f);
        public static readonly Color Sky = new Color(0.4f, 0.85f, 1f, 1f);
        public static readonly Color Yellow = new Color(1f, 0.83f, 0.3f, 1f);
        public static readonly Color Locked = new Color(0.45f, 0.4f, 0.44f, 1f);

        private static TMP_FontAsset titleFont, bodyFont;
        private static Sprite rounded, circle, glow, vignette;

        /// <summary>Fuentes del juego (las pone el menú al abrir). Se convierten a fuentes de TextMeshPro al vuelo.</summary>
        public static void SetFonts(Font title, Font body)
        {
            if (title != null && titleFont == null) titleFont = TMP_FontAsset.CreateFontAsset(title);
            if (body != null && bodyFont == null) bodyFont = TMP_FontAsset.CreateFontAsset(body);
            if (bodyFont == null) bodyFont = titleFont;
        }

        // ---------- Sprites generados ----------

        /// <summary>Rectángulo redondeado blanco para estirar (9-slice). Las esquinas miden 48 px de la textura.</summary>
        public static Sprite Rounded
        {
            get
            {
                if (rounded != null) return rounded;
                const int n = 128, r = 48;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float cx = Mathf.Clamp(x + 0.5f, r, n - r), cy = Mathf.Clamp(y + 0.5f, r, n - r);
                        float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                        byte a = (byte)(Mathf.Clamp01(r - d + 0.5f) * 255f);
                        px[y * n + x] = new Color32(255, 255, 255, a);
                    }
                tex.SetPixels32(px);
                tex.Apply();
                rounded = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
                return rounded;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (circle != null) return circle;
                circle = Radial(128, d => Mathf.Clamp01((1f - d) * 64f));
                return circle;
            }
        }

        /// <summary>Brillo suave (de blanco al centro a transparente).</summary>
        public static Sprite Glow
        {
            get
            {
                if (glow != null) return glow;
                glow = Radial(128, d => Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d));
                return glow;
            }
        }

        /// <summary>Viñeta: transparente al centro y oscura en las orillas.</summary>
        public static Sprite Vignette
        {
            get
            {
                if (vignette != null) return vignette;
                vignette = Radial(128, d => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.45f) / 0.6f)));
                return vignette;
            }
        }

        private static Sprite Radial(int n, Func<float, float> alpha)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(alpha(Mathf.Sqrt(dx * dx + dy * dy)) * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        // ---------- Piezas ----------

        public static Image Image(Transform parent, string name, Sprite sprite, Color color, bool sliced = false, float cornerScale = 1f)
        {
            Image img = UIFactory.Panel(parent, name, color);
            img.sprite = sprite;
            if (sliced)
            {
                img.type = UnityEngine.UI.Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.05f, cornerScale);
            }
            img.raycastTarget = false;
            return img;
        }

        /// <summary>
        /// "Burbuja" con el estilo del juego: sombra abajo, contorno grueso, relleno y brillo arriba.
        /// Devuelve el relleno (para cambiarle el color); el objeto raíz es el contorno.
        /// </summary>
        public static Image Blob(Transform parent, string name, Color fill, Vector2 size, float outline = 7f, float corner = 1f, bool shadow = true, bool shine = true)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rt = UIFactory.Rect(root);
            rt.sizeDelta = size;

            if (shadow)
            {
                Image sh = Image(root.transform, "Sombra", Rounded, new Color(0f, 0f, 0f, 0.35f), true, corner);
                UIFactory.Stretch(sh.rectTransform);
                sh.rectTransform.offsetMin += new Vector2(4f, -10f);
                sh.rectTransform.offsetMax += new Vector2(4f, -10f);
            }
            Image line = Image(root.transform, "Contorno", Rounded, Outline, true, corner);
            UIFactory.Stretch(line.rectTransform);
            Image body = Image(root.transform, "Relleno", Rounded, fill, true, corner * 0.85f);
            UIFactory.Stretch(body.rectTransform, outline);
            if (shine)
            {
                Image s = Image(body.transform, "Brillo", Rounded, new Color(1f, 1f, 1f, 0.28f), true, corner * 0.5f);
                RectTransform sr = s.rectTransform;
                sr.anchorMin = new Vector2(0.04f, 0.56f);
                sr.anchorMax = new Vector2(0.96f, 0.93f);
                sr.offsetMin = sr.offsetMax = Vector2.zero;
            }
            return body;
        }

        /// <summary>Texto con letra del juego, contorno oscuro y sombra.</summary>
        public static TextMeshProUGUI Text(Transform parent, string text, float size, Color color, bool title = false,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center, float outline = 0.22f)
        {
            TextMeshProUGUI tmp = UIFactory.Text(parent, text, size, color, alignment);
            TMP_FontAsset font = title ? titleFont : bodyFont;
            if (font != null) tmp.font = font;
            if (outline > 0f)
            {
                tmp.outlineWidth = outline;
                tmp.outlineColor = Outline;
                Material m = tmp.fontMaterial;
                m.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                m.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.45f));
                m.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.35f);
                m.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.6f);
                m.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.3f);
            }
            return tmp;
        }

        /// <summary>Botón redondo de caricatura: se infla al pasar o seleccionar, se aplasta al presionar.</summary>
        public static Button Button(Transform parent, string label, Action onClick, Vector2 size, Color color, float fontSize = 40f)
        {
            Image body = Blob(parent, "Boton_" + label, color, size, 7f, 1f);
            GameObject root = body.transform.parent.gameObject;
            Image hit = root.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);          // zona para el mouse

            var button = root.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = hit;
            if (onClick != null) button.onClick.AddListener(() => onClick());

            TextMeshProUGUI text = Text(body.transform, label, fontSize, Cream, true);
            UIFactory.Stretch(text.rectTransform, 10f);

            var juicy = root.AddComponent<CartoonButton>();
            juicy.Init(body, color);
            return button;
        }

        /// <summary>Insignia redonda con texto adentro (números romanos, palomita, etc.).</summary>
        public static TextMeshProUGUI Badge(Transform parent, string text, Color fill, float diameter, float fontSize)
        {
            var root = new GameObject("Insignia", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            UIFactory.Rect(root).sizeDelta = new Vector2(diameter, diameter);
            Image sh = Image(root.transform, "Sombra", Circle, new Color(0f, 0f, 0f, 0.35f));
            UIFactory.Stretch(sh.rectTransform);
            sh.rectTransform.anchoredPosition = new Vector2(3f, -6f);
            Image line = Image(root.transform, "Contorno", Circle, Outline);
            UIFactory.Stretch(line.rectTransform);
            Image body = Image(root.transform, "Relleno", Circle, fill);
            UIFactory.Stretch(body.rectTransform, diameter * 0.07f);
            Image shine = Image(body.transform, "Brillo", Circle, new Color(1f, 1f, 1f, 0.35f));
            UIFactory.Place(shine.rectTransform, new Vector2(0.35f, 0.7f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(diameter * 0.35f, diameter * 0.18f));
            TextMeshProUGUI t = Text(body.transform, text, fontSize, Cream, true);
            UIFactory.Stretch(t.rectTransform);
            return t;
        }

        /// <summary>Palomita dibujada con dos barras redondas (la letra del juego no trae el símbolo ✓).</summary>
        public static void CheckMark(Transform parent, float size)
        {
            var root = new GameObject("Palomita", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            UIFactory.Place(UIFactory.Rect(root), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            Bar(root.transform, new Vector2(-size * 0.2f, -size * 0.05f), new Vector2(size * 0.48f, size * 0.2f), -45f);
            Bar(root.transform, new Vector2(size * 0.12f, size * 0.08f), new Vector2(size * 0.8f, size * 0.2f), 55f);
        }

        private static void Bar(Transform parent, Vector2 pos, Vector2 size, float angle)
        {
            Image line = Image(parent, "Contorno", Rounded, Outline, true, 0.12f);
            UIFactory.Place(line.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size + new Vector2(6f, 6f));
            line.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            Image fill = Image(parent, "Barra", Rounded, Cream, true, 0.1f);
            UIFactory.Place(fill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            fill.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        /// <summary>Recorte redondo de una imagen (como "foto" del nivel), cubriendo todo el círculo.</summary>
        public static void RoundPicture(Transform parent, Sprite picture, float diameter, Color ring)
        {
            var root = new GameObject("Foto", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            UIFactory.Rect(root).sizeDelta = new Vector2(diameter, diameter);
            Image line = Image(root.transform, "Contorno", Circle, Outline);
            UIFactory.Stretch(line.rectTransform);
            Image mask = Image(root.transform, "Mascara", Circle, Color.white);
            UIFactory.Stretch(mask.rectTransform, 5f);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            if (picture != null) Cover(mask.transform, picture, diameter);
            Image rim = Image(root.transform, "Aro", Circle, new Color(ring.r, ring.g, ring.b, 0f));
            UIFactory.Stretch(rim.rectTransform);
        }

        /// <summary>Pone una imagen que llena (sin deformarse) un cuadro de 'boxSize'.</summary>
        public static Image Cover(Transform parent, Sprite picture, float boxSize) => Cover(parent, picture, new Vector2(boxSize, boxSize));

        public static Image Cover(Transform parent, Sprite picture, Vector2 box)
        {
            Image pic = Image(parent, "Imagen", picture, Color.white);
            Vector2 s = picture.rect.size;
            float k = Mathf.Max(box.x / s.x, box.y / s.y);
            pic.rectTransform.sizeDelta = s * k;
            return pic;
        }
    }
}
