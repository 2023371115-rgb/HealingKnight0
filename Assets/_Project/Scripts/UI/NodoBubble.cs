using System.Collections.Generic;
using HealingKnight.Gameplay.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HealingKnight.UI
{
    /// <summary>
    /// Globo de NODO (la IA del Programa Centinela) para las indicaciones del nivel.
    ///  - Baja desde arriba, escribe letra por letra y dura según lo largo del texto (da tiempo de leer).
    ///  - Si llegan varios mensajes juntos, hace fila (no se tapan).
    ///  - El color va con el sistema (Digestivo, Respiratorio, Nervioso) y la cara de NODO con el tono:
    ///    normal, ALERTA (empieza con "¡" o habla de peligro) o FELIZ (premios, zona limpia).
    /// Caras opcionales: Art/UI/Resources/Nodo/Nodo_Normal.png, Nodo_Alerta.png y Nodo_Feliz.png
    /// (sin ellas se dibuja la insignia "N").
    /// </summary>
    public class NodoBubble : MonoBehaviour
    {
        private enum Mood { Normal, Alert, Happy }

        private struct Theme { public Color panel, accent, name; }

        private const float Width = 1100f, Height = 150f, Top = -106f, OffsetX = 200f;   // a la derecha del panel de vida
        private const float CharsPerSecond = 55f;

        private readonly Queue<string> queue = new Queue<string>();
        private RectTransform root;
        private Image panel, portrait, portraitRing;
        private GameObject badge;
        private TextMeshProUGUI text, nameTag;
        private Sprite normalFace, alertFace, happyFace;
        private Theme theme;

        private float slide;          // 0 = escondido, 1 = abajo
        private bool showing;
        private float typed, hold, shake;
        private int totalChars;

        public static NodoBubble Create(Transform canvas, string system)
        {
            var go = new GameObject("NODO", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            var bubble = go.AddComponent<NodoBubble>();
            bubble.Build(system);
            return bubble;
        }

        private static Theme ThemeFor(string system)
        {
            string s = (system ?? "").ToLowerInvariant();
            if (s.Contains("resp"))
                return new Theme { panel = new Color(0.13f, 0.2f, 0.42f), accent = new Color(0.45f, 0.85f, 1f), name = new Color(0.25f, 0.55f, 0.9f) };
            if (s.Contains("nerv"))
                return new Theme { panel = new Color(0.24f, 0.11f, 0.4f), accent = new Color(0.78f, 0.6f, 1f), name = new Color(0.55f, 0.32f, 0.9f) };
            return new Theme { panel = new Color(0.4f, 0.1f, 0.18f), accent = new Color(1f, 0.6f, 0.65f), name = new Color(0.9f, 0.3f, 0.4f) };
        }

        private void Build(string system)
        {
            theme = ThemeFor(system);
            normalFace = Resources.Load<Sprite>("Nodo/Nodo_Normal");
            alertFace = Resources.Load<Sprite>("Nodo/Nodo_Alerta");
            happyFace = Resources.Load<Sprite>("Nodo/Nodo_Feliz");

            root = (RectTransform)transform;
            UIFactory.Place(root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 200f), new Vector2(Width, Height));

            panel = CartoonUI.Blob(root, "Globo", theme.panel, new Vector2(Width - 120f, Height - 20f), 6f, 1f, true, false);   // sin brillo: tapaba la 1a línea
            UIFactory.Place((RectTransform)panel.transform.parent, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(110f, 0f), new Vector2(Width - 120f, Height - 20f));

            // Cara de NODO (dibujo o insignia "N").
            portraitRing = CartoonUI.Image(root, "Halo", CartoonUI.Glow, new Color(theme.accent.r, theme.accent.g, theme.accent.b, 0.45f));
            UIFactory.Place(portraitRing.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(80f, 0f), new Vector2(210f, 210f));
            if (normalFace != null)
            {
                portrait = CartoonUI.Image(root, "Cara", normalFace, Color.white);
                portrait.preserveAspect = true;
                UIFactory.Place(portrait.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(80f, 0f), new Vector2(150f, 150f));
            }
            else
            {
                TextMeshProUGUI n = CartoonUI.Badge(root, "N", theme.accent, 120f, 64f);
                badge = n.transform.parent.parent.gameObject;
                UIFactory.Place((RectTransform)badge.transform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(80f, 0f), new Vector2(120f, 120f));
            }

            Image tag = CartoonUI.Blob(root, "Nombre", theme.name, new Vector2(120f, 38f), 4f, 1f, false, false);
            UIFactory.Place((RectTransform)tag.transform.parent, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(150f, -6f), new Vector2(120f, 38f));
            nameTag = CartoonUI.Text(tag.transform, "NODO", 24f, CartoonUI.Cream, true, TextAlignmentOptions.Center, 0.15f);
            UIFactory.Stretch(nameTag.rectTransform, 2f);

            text = CartoonUI.Text(panel.transform, "", 30f, CartoonUI.Cream, false, TextAlignmentOptions.MidlineLeft, 0.1f);
            text.richText = true;
            text.enableAutoSizing = true;
            text.fontSizeMin = 20f;
            text.fontSizeMax = 30f;
            UIFactory.Stretch(text.rectTransform, 0f);
            text.rectTransform.offsetMin = new Vector2(70f, 14f);
            text.rectTransform.offsetMax = new Vector2(-30f, -16f);

            gameObject.SetActive(true);
            slide = 0f;
            Apply();
        }

        /// <summary>Pone un mensaje en la fila. Quita el "NODO:" del principio (ya lo dice la etiqueta).</summary>
        public void Show(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            string m = message.Trim();
            if (m.StartsWith("NODO:")) m = m.Substring(5).TrimStart();
            if (queue.Count >= 3) queue.Dequeue();   // demasiados juntos: el más viejo se salta
            queue.Enqueue(m);
            if (!showing) Next();
        }

        private void Next()
        {
            if (queue.Count == 0) { showing = false; return; }
            string m = queue.Dequeue();
            showing = true;
            Mood mood = MoodOf(m);
            text.text = m;
            text.ForceMeshUpdate();
            totalChars = Mathf.Max(1, text.textInfo.characterCount);
            text.maxVisibleCharacters = 0;
            typed = 0f;
            // Tiempo para leer: ~16 letras por segundo + un respiro.
            hold = Mathf.Clamp(1.8f + totalChars / 16f, 3.2f, 11f);

            Color panelColor = theme.panel;
            if (mood == Mood.Alert) panelColor = Color.Lerp(theme.panel, new Color(0.62f, 0.12f, 0.1f), 0.55f);
            if (mood == Mood.Happy) panelColor = Color.Lerp(theme.panel, new Color(0.15f, 0.45f, 0.25f), 0.5f);
            panel.color = panelColor;
            Sfx.Effect(mood == Mood.Alert ? "Nodo_Alerta" : "Nodo_Mensaje", 0.6f);
            if (portrait != null)
                portrait.sprite = mood == Mood.Alert && alertFace != null ? alertFace : mood == Mood.Happy && happyFace != null ? happyFace : normalFace;
            shake = mood == Mood.Alert ? 0.35f : 0f;
        }

        private static Mood MoodOf(string m)
        {
            string l = m.ToLowerInvariant();
            if (l.Contains("ganaste") || l.Contains("+1") || l.Contains("gracias") || l.Contains("desinfectad") || l.Contains("¡bien")
                || l.Contains("¡excelente") || l.Contains("salvaste") || l.Contains("¡lo lograste")) return Mood.Happy;
            if (m.StartsWith("¡") || l.Contains("cuidado") || l.Contains("peligro") || l.Contains("te detectaron") || l.Contains("¡corre"))
                return Mood.Alert;
            return Mood.Normal;
        }

        private void Update()
        {
            float dt = Time.deltaTime;   // se congela en la pausa
            if (showing)
            {
                if (typed < totalChars)
                {
                    typed += CharsPerSecond * dt;
                    text.maxVisibleCharacters = Mathf.Min(totalChars, Mathf.FloorToInt(typed));
                }
                else
                {
                    // Si hay más mensajes esperando, este dura un poco menos.
                    hold -= dt * (queue.Count > 0 ? 1.5f : 1f);
                    if (hold <= 0f)
                    {
                        if (queue.Count > 0) Next();
                        else showing = false;
                    }
                }
            }

            float target = showing ? 1f : 0f;
            slide = Mathf.MoveTowards(slide, target, dt * (showing ? 5f : 3.5f));
            shake = Mathf.MoveTowards(shake, 0f, dt);
            Apply();
        }

        private void Apply()
        {
            // Entra con rebote desde arriba de la pantalla.
            float e = slide < 1f ? 1f - Mathf.Pow(1f - slide, 3f) : 1f;
            float over = Mathf.Sin(slide * Mathf.PI) * 14f;
            float x = shake > 0f ? Mathf.Sin(Time.time * 60f) * 10f * shake : 0f;
            root.anchoredPosition = new Vector2(OffsetX + x, Mathf.Lerp(Height + 40f, Top, e) - over * (showing ? 1f : 0f));
            bool visible = slide > 0.001f;
            if (panel.transform.parent.gameObject.activeSelf != visible)
                foreach (Transform child in root) child.gameObject.SetActive(visible);
            if (badge != null) badge.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 2f) * 4f);
        }
    }
}
