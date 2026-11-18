using System;
using System.Collections.Generic;
using HealingKnight.Core.Combat;
using HealingKnight.Core.Flow;
using HealingKnight.Core.Progression;
using HealingKnight.Gameplay.Enemies;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HealingKnight.UI
{
    /// <summary>
    /// BESTIARIO (tipo Pokédex). Pantalla encima de todo (se abre desde el menú, el mapa o la pausa).
    ///  - Pestañas por sistema; cuadrícula con cada patógeno: SILUETA negra y "???" hasta que lo ves en un nivel.
    ///  - Ficha: dibujo grande, nombre y nombre científico, tipo, dónde vive, enfermedad, datos reales, cómo se trata
    ///    de verdad, cómo se previene y qué medicamento del juego le gana.
    ///  - Controles: flechas / stick para elegir, Q/E o LB/RB para cambiar de sistema, Esc / B para salir.
    /// Funciona con el tiempo detenido (pausa).
    /// </summary>
    public class BestiaryScreen : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        private static readonly string[] Systems = { "Digestivo", "Respiratorio", "Nervioso" };
        private static readonly Color[] SystemColors =
        {
            new Color(0.95f, 0.55f, 0.3f), new Color(0.4f, 0.75f, 1f), new Color(0.7f, 0.5f, 1f)
        };
        private static readonly Color Panel = new Color(0.27f, 0.12f, 0.22f, 1f);
        private static readonly Color Dim = new Color(1f, 0.9f, 0.92f, 0.65f);

        private Action onClosed;
        private GameObject previousSelection;
        private Bestiary bestiary;
        private SaveData save;
        private int tab;

        private RectTransform grid;
        private readonly List<Button> tabs = new List<Button>();
        private readonly Dictionary<GameObject, BestiaryEntry> cards = new Dictionary<GameObject, BestiaryEntry>();
        private GameObject lastShown;
        private Image picture, pictureGlow;
        private TextMeshProUGUI title, scientific, kind, where, body, counter, missing, partial;
        private RectTransform chips;
        private ScrollRect scroll;
        private int openedFrame;

        /// <summary>Abre el bestiario. 'closed' se llama al cerrarlo (para regresar la selección al menú).</summary>
        public static BestiaryScreen Open(Action closed = null)
        {
            if (IsOpen) return null;
            var go = new GameObject("Bestiario");
            var screen = go.AddComponent<BestiaryScreen>();
            screen.onClosed = closed;
            return screen;
        }

        private void Awake()
        {
            IsOpen = true;
            openedFrame = Time.frameCount;
            previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            bestiary = Bestiary.Load();
            save = GameSession.Save;
            FreezeOtherMenus();
            Build();
        }

        private void OnDestroy()
        {
            IsOpen = false;
            foreach (KeyValuePair<Selectable, Navigation> pair in frozen)
                if (pair.Key != null) pair.Key.navigation = pair.Value;
            frozen.Clear();
        }

        // Para que el stick / las flechas no se escapen a los botones del menú que quedó detrás.
        private readonly Dictionary<Selectable, Navigation> frozen = new Dictionary<Selectable, Navigation>();

        private void FreezeOtherMenus()
        {
            foreach (Selectable s in Selectable.allSelectablesArray)
            {
                if (s == null || s.transform.IsChildOf(transform) || frozen.ContainsKey(s)) continue;
                frozen.Add(s, s.navigation);
                s.navigation = new Navigation { mode = Navigation.Mode.None };
            }
        }

        private void Close()
        {
            Destroy(gameObject);
            if (previousSelection != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(previousSelection);
            onClosed?.Invoke();
        }

        // ---------- Construcción ----------

        private void Build()
        {
            Canvas canvas = UIFactory.CreateCanvas("Bestiario Canvas", 60, transform);
            Image bg = UIFactory.Panel(canvas.transform, "Fondo", new Color(0.1f, 0.03f, 0.08f, 0.97f));
            UIFactory.Stretch(bg.rectTransform);
            Image glow = CartoonUI.Image(canvas.transform, "Luz", CartoonUI.Glow, new Color(0.9f, 0.35f, 0.5f, 0.15f));
            UIFactory.Place(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 100f), new Vector2(2400f, 1400f));
            Image vig = CartoonUI.Image(canvas.transform, "Viñeta", CartoonUI.Vignette, new Color(0.03f, 0f, 0.02f, 0.85f));
            UIFactory.Stretch(vig.rectTransform, -200f);

            if (bestiary == null)
            {
                TextMeshProUGUI err = CartoonUI.Text(canvas.transform, "Falta el bestiario. Ejecuta 'Healing Knight > Crear proyecto base'.", 34f, Color.red, false, TextAlignmentOptions.Center, 0f);
                UIFactory.Place(err.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600f, 200f));
                Button b = CartoonUI.Button(canvas.transform, "Volver", Close, new Vector2(220f, 76f), new Color(0.42f, 0.25f, 0.4f), 30f);
                UIFactory.Place((RectTransform)b.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(220f, 76f));
                UIFactory.Select(b);
                return;
            }

            TextMeshProUGUI head = CartoonUI.Text(canvas.transform, "BESTIARIO", 60f, CartoonUI.Cream, true, TextAlignmentOptions.Left, 0.2f);
            UIFactory.Place(head.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -24f), new Vector2(600f, 80f));
            counter = CartoonUI.Text(canvas.transform, "", 24f, Dim, false, TextAlignmentOptions.Left, 0f);
            UIFactory.Place(counter.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, -100f), new Vector2(900f, 34f));

            Button back = CartoonUI.Button(canvas.transform, "Volver", Close, new Vector2(220f, 76f), new Color(0.42f, 0.25f, 0.4f), 30f);
            UIFactory.Place((RectTransform)back.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(220f, 76f));

            // Pestañas de sistema.
            var tabRow = new GameObject("Sistemas", typeof(RectTransform));
            tabRow.transform.SetParent(canvas.transform, false);
            UIFactory.Place(UIFactory.Rect(tabRow), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -150f), new Vector2(1000f, 80f));
            UIFactory.HorizontalLayout(tabRow, 18f, TextAnchor.MiddleLeft);
            for (int i = 0; i < Systems.Length; i++)
            {
                int captured = i;
                tabs.Add(CartoonUI.Button(tabRow.transform, Systems[i], () => ShowTab(captured), new Vector2(300f, 72f), SystemColors[i] * 0.75f, 30f));
            }

            // Cuadrícula de fichas.
            var gridGo = new GameObject("Fichas", typeof(RectTransform));
            gridGo.transform.SetParent(canvas.transform, false);
            grid = UIFactory.Place(UIFactory.Rect(gridGo), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -250f), new Vector2(1000f, 700f));
            var layout = gridGo.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(180f, 206f);
            layout.spacing = new Vector2(16f, 18f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 5;
            layout.childAlignment = TextAnchor.UpperLeft;

            BuildDetail(canvas.transform);

            TextMeshProUGUI note = CartoonUI.Text(canvas.transform,
                "Información educativa. Solo un médico puede diagnosticar y recetar medicamentos: nunca te automediques.", 20f, Dim, false, TextAlignmentOptions.Center, 0f);
            note.enableAutoSizing = true; note.fontSizeMin = 14f; note.fontSizeMax = 20f;
            UIFactory.Place(note.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(60f, 54f), new Vector2(1000f, 30f));
            TextMeshProUGUI help = CartoonUI.Text(canvas.transform,
                "Flechas / stick: elegir     Q / E  o  LB / RB: sistema     Rueda / stick derecho: leer más     Esc / B: volver", 18f, new Color(1f, 1f, 1f, 0.45f), false, TextAlignmentOptions.Center, 0f);
            help.enableAutoSizing = true; help.fontSizeMin = 12f; help.fontSizeMax = 18f;
            UIFactory.Place(help.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(60f, 20f), new Vector2(1000f, 28f));

            UpdateCounter();
            ShowTab(TabOfCurrentLevel());
        }

        private void BuildDetail(Transform parent)
        {
            Image panel = CartoonUI.Blob(parent, "Ficha", Panel, new Vector2(780f, 820f), 7f, 1f, true, false);
            RectTransform root = (RectTransform)panel.transform.parent;
            UIFactory.Place(root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-50f, -130f), new Vector2(780f, 820f));
            Transform p = panel.transform;

            pictureGlow = CartoonUI.Image(p, "Halo", CartoonUI.Glow, new Color(1f, 0.8f, 0.85f, 0.35f));
            UIFactory.Place(pictureGlow.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(170f, -160f), new Vector2(330f, 330f));
            picture = CartoonUI.Image(p, "Dibujo", null, Color.white);
            picture.preserveAspect = true;
            UIFactory.Place(picture.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(170f, -160f), new Vector2(260f, 260f));
            missing = CartoonUI.Text(p, "?", 160f, new Color(0.05f, 0.02f, 0.05f, 0.9f), true, TextAlignmentOptions.Center, 0f);
            UIFactory.Place(missing.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(170f, -160f), new Vector2(260f, 260f));

            title = CartoonUI.Text(p, "", 42f, CartoonUI.Cream, true, TextAlignmentOptions.TopLeft, 0.18f);
            title.enableAutoSizing = true; title.fontSizeMin = 26f; title.fontSizeMax = 42f;
            UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(330f, -34f), new Vector2(420f, 110f));
            scientific = CartoonUI.Text(p, "", 24f, Dim, false, TextAlignmentOptions.TopLeft, 0f);
            UIFactory.Place(scientific.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(330f, -148f), new Vector2(420f, 64f));
            kind = CartoonUI.Text(p, "", 24f, CartoonUI.Yellow, false, TextAlignmentOptions.TopLeft, 0f);
            kind.enableAutoSizing = true; kind.fontSizeMin = 16f; kind.fontSizeMax = 24f; kind.textWrappingMode = TextWrappingModes.NoWrap;
            UIFactory.Place(kind.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(330f, -214f), new Vector2(420f, 34f));
            where = CartoonUI.Text(p, "", 22f, Dim, false, TextAlignmentOptions.TopLeft, 0f);
            UIFactory.Place(where.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(330f, -252f), new Vector2(420f, 60f));

            // Medicamentos del juego que le ganan.
            var chipRow = new GameObject("EnElJuego", typeof(RectTransform));
            chipRow.transform.SetParent(p, false);
            chips = UIFactory.Place(UIFactory.Rect(chipRow), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -320f), new Vector2(720f, 56f));
            UIFactory.HorizontalLayout(chipRow, 10f, TextAnchor.MiddleLeft);
            partial = CartoonUI.Text(p, "", 20f, Dim, false, TextAlignmentOptions.TopLeft, 0f);
            UIFactory.Place(partial.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -382f), new Vector2(716f, 30f));

            // Texto con barra para leer más.
            var view = new GameObject("Lectura", typeof(RectTransform));
            view.transform.SetParent(p, false);
            RectTransform vr = UIFactory.Place(UIFactory.Rect(view), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -424f), new Vector2(720f, 370f));
            view.AddComponent<RectMask2D>();
            Image hit = view.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            scroll = view.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            var content = new GameObject("Contenido", typeof(RectTransform));
            content.transform.SetParent(view.transform, false);
            RectTransform cr = UIFactory.Rect(content);
            cr.anchorMin = new Vector2(0f, 1f); cr.anchorMax = new Vector2(1f, 1f); cr.pivot = new Vector2(0.5f, 1f);
            cr.offsetMin = cr.offsetMax = Vector2.zero;
            body = CartoonUI.Text(content.transform, "", 23f, CartoonUI.Cream, false, TextAlignmentOptions.TopLeft, 0f);
            body.richText = true;
            RectTransform br = body.rectTransform;
            br.anchorMin = new Vector2(0f, 1f); br.anchorMax = new Vector2(1f, 1f); br.pivot = new Vector2(0.5f, 1f);
            br.offsetMin = br.offsetMax = Vector2.zero;
            var fit = content.AddComponent<VerticalLayoutGroup>();
            fit.childControlHeight = true; fit.childControlWidth = true; fit.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = vr;
            scroll.content = cr;
        }

        private int TabOfCurrentLevel()
        {
            SystemData sys = GameSession.CurrentSystem;
            if (sys == null) return 0;
            for (int i = 0; i < Systems.Length; i++)
                if (!string.IsNullOrEmpty(sys.displayName) && sys.displayName.Contains(Systems[i])) return i;
            return 0;
        }

        private void UpdateCounter()
        {
            int seen = 0, beaten = 0;
            foreach (BestiaryEntry e in bestiary.entries)
            {
                if (save.HasSeen(e.id)) seen++;
                if (save.HasDefeated(e.id)) beaten++;
            }
            counter.text = $"Descubiertos: {seen} / {bestiary.entries.Count}     Derrotados: {beaten}";
        }

        private void ShowTab(int index)
        {
            tab = (index + Systems.Length) % Systems.Length;
            for (int i = 0; i < tabs.Count; i++)
            {
                CartoonButton juicy = tabs[i].GetComponent<CartoonButton>();
                if (juicy != null) { juicy.SetColor(i == tab ? SystemColors[i] : SystemColors[i] * 0.55f); juicy.Highlight = i == tab; }
            }

            foreach (Transform child in grid) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            cards.Clear();
            Button first = null;
            int number = 0;
            foreach (BestiaryEntry e in bestiary.entries)
            {
                number++;
                if (e.system != Systems[tab]) continue;
                Button card = BuildCard(e, number);
                if (first == null) first = card;
            }
            if (first != null) { UIFactory.Select(first); ShowEntry(cards[first.gameObject]); lastShown = first.gameObject; }
        }

        private Button BuildCard(BestiaryEntry e, int number)
        {
            bool seen = save.HasSeen(e.id);
            Color fill = seen ? (e.ally ? new Color(0.35f, 0.6f, 0.45f) : e.boss ? new Color(0.55f, 0.25f, 0.3f) : new Color(0.38f, 0.2f, 0.35f)) : new Color(0.22f, 0.15f, 0.2f);
            Button card = CartoonUI.Button(grid, "", () => ShowEntry(e), new Vector2(180f, 206f), fill, 20f);
            Transform body = card.transform.Find("Relleno");
            Transform parent = body != null ? body : card.transform;

            Image pic = CartoonUI.Image(parent, "Dibujo", e.portrait, seen ? Color.white : new Color(0.04f, 0.02f, 0.05f, 0.95f));
            pic.preserveAspect = true;
            UIFactory.Place(pic.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(130f, 120f));
            if (e.portrait == null)
            {
                TextMeshProUGUI q = CartoonUI.Text(parent, "?", 80f, new Color(0.05f, 0.02f, 0.05f, 0.9f), true, TextAlignmentOptions.Center, 0f);
                UIFactory.Place(q.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(130f, 120f));
            }

            TextMeshProUGUI label = CartoonUI.Text(parent, seen ? e.name : "???", 19f, CartoonUI.Cream, false, TextAlignmentOptions.Center, 0f);
            label.enableAutoSizing = true; label.fontSizeMin = 13f; label.fontSizeMax = 19f;
            UIFactory.Place(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(166f, 50f));
            TextMeshProUGUI num = CartoonUI.Text(parent, $"#{number:00}", 16f, new Color(1f, 1f, 1f, 0.5f), false, TextAlignmentOptions.TopLeft, 0f);
            UIFactory.Place(num.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -6f), new Vector2(60f, 24f));
            if (save.HasDefeated(e.id))
            {
                var check = new GameObject("Derrotado", typeof(RectTransform));
                check.transform.SetParent(parent, false);
                UIFactory.Place(UIFactory.Rect(check), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-8f, -8f), new Vector2(34f, 34f));
                CartoonUI.CheckMark(check.transform, 34f);
            }
            cards[card.gameObject] = e;
            return card;
        }

        // ---------- Ficha ----------

        private void ShowEntry(BestiaryEntry e)
        {
            if (e == null) return;
            bool seen = save.HasSeen(e.id);
            picture.sprite = e.portrait;
            picture.enabled = e.portrait != null;
            picture.color = seen ? Color.white : new Color(0.04f, 0.02f, 0.05f, 0.95f);
            missing.gameObject.SetActive(e.portrait == null);
            pictureGlow.color = seen ? new Color(1f, 0.8f, 0.85f, 0.35f) : new Color(0.6f, 0.5f, 0.6f, 0.15f);
            foreach (Transform c in chips) { c.gameObject.SetActive(false); Destroy(c.gameObject); }
            partial.text = "";
            scroll.verticalNormalizedPosition = 1f;

            if (!seen)
            {
                title.text = "???";
                scientific.text = "";
                kind.text = "Sin descubrir";
                where.text = $"Pista: búscalo en {e.foundIn}.";
                body.text = "Todavía no lo encuentras. Cuando lo veas en un nivel se desbloquea su ficha completa: qué enfermedad causa, datos reales, cómo se trata de verdad y qué medicamento del juego le gana.";
                return;
            }

            title.text = e.name;
            scientific.text = $"<i>{e.scientific}</i>";
            kind.text = e.kind + (save.HasDefeated(e.id) ? "  ·  ¡Derrotado!" : "");
            where.text = $"Vive en: {e.foundIn}";
            BuildChips(e);

            string tip = !string.IsNullOrEmpty(e.gameTip) ? e.gameTip
                : (e.enemies != null && e.enemies.Length > 0 && e.enemies[0] != null ? e.enemies[0].biologia : "");
            body.text =
                Section("Qué causa", e.disease) +
                Section("En la vida real", e.facts) +
                Section("Cómo se trata de verdad", e.treatment) +
                Section("Cómo prevenirlo", e.prevention) +
                Section("En el juego", tip);
        }

        private static string Section(string head, string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return $"<color=#FFD24D><b>{head}</b></color>\n{text}\n\n";
        }

        private void BuildChips(BestiaryEntry e)
        {
            TextMeshProUGUI label = CartoonUI.Text(chips, "Le gana:", 22f, Dim, false, TextAlignmentOptions.MidlineLeft, 0f);
            label.rectTransform.sizeDelta = new Vector2(100f, 50f);
            if (e.ally)
            {
                Chip("¡Es tu aliada! No le dispares", CartoonUI.Mint, 1f);
                return;
            }
            int shown = 0;
            var weak = new List<string>();
            foreach (MedicationData m in bestiary.medications)
            {
                if (m == null) continue;
                Effectiveness r = bestiary.Evaluate(e, m);
                if (r == Effectiveness.Effective && shown < 3) { Chip(m.displayName, m.color, 1f); shown++; }
                else if (r == Effectiveness.Partial) weak.Add(m.displayName);
            }
            if (shown == 0) Chip("Lee la pista de abajo", CartoonUI.Locked, 1f);
            if (weak.Count > 0) partial.text = "Le hace un poco de daño: " + string.Join(", ", weak);
        }

        private void Chip(string text, Color color, float strength)
        {
            float w = Mathf.Clamp(text.Length * 13f + 44f, 120f, 400f);
            Image blob = CartoonUI.Blob(chips, "Medicamento", Color.Lerp(new Color(0.2f, 0.1f, 0.15f), color, strength), new Vector2(w, 48f), 5f, 1f, false, true);
            TextMeshProUGUI t = CartoonUI.Text(blob.transform, text, 20f, strength > 0.9f && color.grayscale > 0.6f ? CartoonUI.Outline : CartoonUI.Cream, false, TextAlignmentOptions.Center, 0f);
            UIFactory.Stretch(t.rectTransform, 4f);
        }

        // ---------- Controles ----------

        private void Update()
        {
            if (bestiary == null)
            {
                if (Time.frameCount > openedFrame && BackPressed()) Close();
                return;
            }
            if (Time.frameCount > openedFrame && BackPressed()) { Close(); return; }

            bool prev = (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.leftShoulder.wasPressedThisFrame);
            bool next = (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame);
            if (prev) ShowTab(tab - 1);
            else if (next) ShowTab(tab + 1);

            // Si te mueves con el teclado o el control, la ficha sigue a la selección.
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected != null && selected != lastShown && cards.TryGetValue(selected, out BestiaryEntry entry))
            {
                lastShown = selected;
                ShowEntry(entry);
            }

            // Leer más con el stick derecho o AvPág / RePág.
            float s = 0f;
            if (Gamepad.current != null) s = Gamepad.current.rightStick.ReadValue().y;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.pageDownKey.isPressed) s = -1f;
                if (Keyboard.current.pageUpKey.isPressed) s = 1f;
            }
            if (Mathf.Abs(s) > 0.2f && scroll != null)
            {
                float h = Mathf.Max(1f, scroll.content.rect.height - scroll.viewport.rect.height);
                scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition + s * 600f * Time.unscaledDeltaTime / h);
            }
        }

        private static bool BackPressed()
        {
            return (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                || (Gamepad.current != null && (Gamepad.current.buttonEast.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame));
        }
    }
}
