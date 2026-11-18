using HealingKnight.Core.Flow;
using HealingKnight.Core.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HealingKnight.UI
{
    /// <summary>
    /// Mapa del cuerpo: los sistemas (I–IV) y sus órganos (niveles) según el progreso guardado.
    /// Jerarquía de la pantalla: título y fórmulas, tarjetas de sistemas con el siguiente órgano resaltado,
    /// y ayuda de controles abajo. El siguiente órgano queda seleccionado para entrar directamente.
    /// Estados con palabra + icono (no solo color): Desinfectado (palomita), En curso (avance), Bloqueado (candado).
    /// Los efectos de letra (contorno y sombra) solo van en títulos y botones principales; lo demás es texto limpio.
    /// Todo sale del GameCatalog: agregar un sistema o nivel no requiere tocar esta pantalla.
    /// </summary>
    public class BodyMapScreen : MonoBehaviour
    {
        [Header("Arte (lo pone SceneSetup)")]
        [SerializeField] private Font titleFont;
        [SerializeField] private Font bodyFont;
        [SerializeField] private Sprite[] microbes;
        [Tooltip("Foto de cada nivel (su fondo pintado), en el mismo orden que 'levelIds'.")]
        [SerializeField] private string[] levelIds;
        [SerializeField] private Sprite[] levelArt;

        private static readonly Color Panel = new Color(0.1f, 0.04f, 0.08f, 1f);
        private static readonly Color TextSoft = new Color(1f, 0.9f, 0.92f, 1f);
        private static readonly Color TextDim = new Color(0.85f, 0.8f, 0.84f, 1f);

        private Button focus;
        private LevelData nextLevel;

        private void Awake()
        {
            Time.timeScale = 1f;
            CartoonUI.SetFonts(titleFont, bodyFont);
            Build();
        }

        private void Start() => UIFactory.Select(focus);

        private void Update()
        {
            if (BestiaryScreen.IsOpen) return;
            bool back = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                        || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
            if (back) GameSession.GoToMainMenu();
        }

        private Sprite ArtFor(LevelData level)
        {
            if (level == null || levelIds == null || levelArt == null) return null;
            for (int i = 0; i < levelIds.Length && i < levelArt.Length; i++)
                if (levelIds[i] == level.id) return levelArt[i];
            return null;
        }

        private void Build()
        {
            Canvas canvas = UIFactory.CreateCanvas("BodyMap Canvas", 0, transform);
            Image bg = UIFactory.Panel(canvas.transform, "Fondo", new Color(0.16f, 0.05f, 0.11f, 1f));
            UIFactory.Stretch(bg.rectTransform);
            Image glow = CartoonUI.Image(canvas.transform, "Luz", CartoonUI.Glow, new Color(0.9f, 0.3f, 0.45f, 0.18f));
            UIFactory.Place(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(2200f, 1400f));
            Image vig = CartoonUI.Image(canvas.transform, "Viñeta", CartoonUI.Vignette, new Color(0.04f, 0f, 0.02f, 0.9f));
            UIFactory.Stretch(vig.rectTransform, -200f);

            GameCatalog catalog = GameSession.Catalog;
            ProgressService progress = GameSession.Progress;
            if (catalog == null || progress == null)
            {
                var error = CartoonUI.Text(canvas.transform, "Falta el GameCatalog. Ejecuta 'Healing Knight > Crear proyecto base'.", 36f, Color.red, false, TextAlignmentOptions.Center, 0f);
                UIFactory.Place(error.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600f, 200f));
                return;
            }

            FindNextMission(catalog, progress);

            // 3) Título discreto y fórmulas (arriba).
            TextMeshProUGUI title = CartoonUI.Text(canvas.transform, "MAPA DEL CUERPO", 44f, CartoonUI.Cream, true, TextAlignmentOptions.Left, 0.18f);
            UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(250f, -30f), new Vector2(600f, 60f));
            TextMeshProUGUI patient = CartoonUI.Text(canvas.transform, "Paciente 001  ·  elige un órgano para entrar", 22f, TextDim, false, TextAlignmentOptions.Left, 0f);
            UIFactory.Place(patient.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(252f, -86f), new Vector2(700f, 34f));
            BuildFormulas(canvas.transform, GameSession.Save.formulas.Count, catalog.systems.Count);

            Button back = CartoonUI.Button(canvas.transform, "Menú", GameSession.GoToMainMenu, new Vector2(180f, 70f), new Color(0.42f, 0.25f, 0.4f), 28f);
            UIFactory.Place((RectTransform)back.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(180f, 70f));
            Button bestiary = null;
            bestiary = CartoonUI.Button(canvas.transform, "Bestiario", () => BestiaryScreen.Open(() => UIFactory.Select(bestiary)),
                new Vector2(240f, 66f), new Color(0.35f, 0.55f, 0.6f), 28f);
            UIFactory.Place((RectTransform)bestiary.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -112f), new Vector2(240f, 66f));

            // 2) Tarjetas.
            var row = new GameObject("Sistemas", typeof(RectTransform));
            row.transform.SetParent(canvas.transform, false);
            UIFactory.Place(UIFactory.Rect(row), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(1800f, 700f));
            UIFactory.HorizontalLayout(row, 28f);
            SystemData previous = null;
            foreach (SystemData system in catalog.systems)
            {
                if (system == null) continue;
                BuildSystemCard(row.transform, system, progress, previous);
                previous = system;
            }

            // Ayuda de controles.
            TextMeshProUGUI help = CartoonUI.Text(canvas.transform, "Flechas / stick: elegir     Enter / A: entrar     Esc / B: menú", 20f, new Color(1f, 1f, 1f, 0.55f), false, TextAlignmentOptions.Center, 0f);
            UIFactory.Place(help.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(1400f, 30f));

            if (focus == null) focus = back;

            Image fade = UIFactory.Panel(canvas.transform, "Fundido", Color.black);
            UIFactory.Stretch(fade.rectTransform);
            fade.raycastTarget = false;
            fade.gameObject.AddComponent<UIFadeIn>();
        }

        /// <summary>El primer órgano jugable que no se ha completado (en el orden del catálogo).</summary>
        private void FindNextMission(GameCatalog catalog, ProgressService progress)
        {
            foreach (SystemData system in catalog.systems)
            {
                if (system == null || !progress.IsSystemUnlocked(system)) continue;
                foreach (LevelData level in system.levels)
                {
                    if (level == null || level.hidden || !progress.IsLevelVisible(level)) continue;
                    if (progress.IsLevelPlayable(level) && !progress.IsLevelCompleted(level))
                    {
                        nextLevel = level;
                        return;
                    }
                }
            }
        }

        /// <summary>Contador de fórmulas: una cápsula por sistema, llena si ya obtuviste su fórmula.</summary>
        private void BuildFormulas(Transform parent, int got, int total)
        {
            var box = new GameObject("Formulas", typeof(RectTransform));
            box.transform.SetParent(parent, false);
            UIFactory.Place(UIFactory.Rect(box), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -40f), new Vector2(560f, 56f));
            UIFactory.HorizontalLayout(box, 10f);
            TextMeshProUGUI label = CartoonUI.Text(box.transform, $"Fórmulas {got}/{total}", 24f, CartoonUI.Sky, false, TextAlignmentOptions.Right, 0f);
            label.rectTransform.sizeDelta = new Vector2(190f, 56f);
            for (int i = 0; i < total; i++)
            {
                bool on = i < got;
                Image pill = CartoonUI.Blob(box.transform, "Capsula", on ? CartoonUI.Sky : new Color(0.28f, 0.2f, 0.28f), new Vector2(70f, 36f), 5f, 0.5f, false, on);
                if (!on) continue;
                Image half = CartoonUI.Image(pill.transform, "Mitad", CartoonUI.Rounded, CartoonUI.Cream, true, 0.35f);
                RectTransform hr = half.rectTransform;
                hr.anchorMin = new Vector2(0.5f, 0f); hr.anchorMax = Vector2.one; hr.offsetMin = hr.offsetMax = Vector2.zero;
            }
        }

        private void BuildSystemCard(Transform parent, SystemData system, ProgressService progress, SystemData previous)
        {
            bool unlocked = progress.IsSystemUnlocked(system);
            bool completed = progress.IsSystemCompleted(system);
            Color tone = unlocked ? system.mapColor : CartoonUI.Locked;
            Color cardColor = unlocked ? Color.Lerp(system.mapColor, Panel, 0.68f) : new Color(0.22f, 0.2f, 0.23f, 1f);

            // Cuenta de órganos (sin los secretos que aún no aparecen).
            int total = 0, done = 0;
            foreach (LevelData l in system.levels)
            {
                if (l == null || !progress.IsLevelVisible(l)) continue;
                total++;
                if (progress.IsLevelCompleted(l)) done++;
            }

            const float W = 420f, H = 700f, Pad = 18f;
            Image card = CartoonUI.Blob(parent, "Sistema_" + system.id, cardColor, new Vector2(W, H), 7f, 1.1f);
            Transform c = card.transform;

            // Pintura del primer órgano arriba.
            Sprite header = null;
            foreach (LevelData l in system.levels) { header = ArtFor(l); if (header != null) break; }
            var top = new GameObject("Portada", typeof(RectTransform));
            top.transform.SetParent(c, false);
            RectTransform tr = UIFactory.Rect(top);
            UIFactory.Place(tr, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -Pad), new Vector2(W - Pad * 2f - 14f, 150f));
            Image frame = CartoonUI.Image(top.transform, "Marco", CartoonUI.Rounded, CartoonUI.Outline, true, 0.6f);
            UIFactory.Stretch(frame.rectTransform);
            Image maskImg = CartoonUI.Image(top.transform, "Mascara", CartoonUI.Rounded, Color.Lerp(tone, Color.black, 0.5f), true, 0.5f);
            UIFactory.Stretch(maskImg.rectTransform, 5f);
            maskImg.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            if (header != null && unlocked) CartoonUI.Cover(maskImg.transform, header, new Vector2(W - Pad * 2f - 24f, 140f));
            if (!unlocked) CartoonUI.Padlock(maskImg.transform, 82f, new Color(0.75f, 0.72f, 0.76f));

            // Número del sistema en una insignia, en la esquina de la pintura.
            TextMeshProUGUI num = CartoonUI.Badge(c, system.mapNumber, tone, 70f, system.mapNumber.Length > 2 ? 28f : 34f);
            UIFactory.Place((RectTransform)num.transform.parent.parent, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(Pad + 26f, -Pad - 8f), new Vector2(70f, 70f));

            TextMeshProUGUI name = CartoonUI.Text(c, system.displayName, 32f, unlocked ? CartoonUI.Cream : TextDim, true, TextAlignmentOptions.Center, unlocked ? 0.12f : 0f);
            UIFactory.Place(name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -Pad - 162f), new Vector2(W - 30f, 44f));

            // Estado: icono + palabra + explicación corta.
            string status, detail;
            Color chip;
            if (completed) { status = "Desinfectado"; detail = "Fórmula obtenida"; chip = new Color(0.2f, 0.55f, 0.35f); }
            else if (unlocked) { status = $"En curso  {done}/{total}"; detail = "Limpia todos sus órganos"; chip = new Color(0.62f, 0.2f, 0.28f); }
            else { status = "Bloqueado"; detail = previous != null ? $"Termina: {previous.displayName}" : "Aún no disponible"; chip = new Color(0.34f, 0.31f, 0.35f); }

            Image pill = CartoonUI.Blob(c, "Estado", chip, new Vector2(W - 70f, 46f), 5f, 0.5f, false, false);
            UIFactory.Place((RectTransform)pill.transform.parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -Pad - 212f), new Vector2(W - 70f, 46f));
            var icon = new GameObject("Icono", typeof(RectTransform));
            icon.transform.SetParent(pill.transform, false);
            UIFactory.Place(UIFactory.Rect(icon), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(26f, 0f), new Vector2(30f, 30f));
            if (completed) CartoonUI.CheckMark(icon.transform, 26f);
            else if (!unlocked) CartoonUI.Padlock(icon.transform, 28f, TextDim);
            else ProgressDots(icon.transform, done, total);
            TextMeshProUGUI st = CartoonUI.Text(pill.transform, status, 24f, Color.white, false, TextAlignmentOptions.Center, 0f);
            UIFactory.Stretch(st.rectTransform, 2f);
            st.rectTransform.offsetMin = new Vector2(44f, 2f);

            TextMeshProUGUI det = CartoonUI.Text(c, detail, 19f, TextDim, false, TextAlignmentOptions.Center, 0f);
            UIFactory.Place(det.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -Pad - 262f), new Vector2(W - 40f, 26f));

            var column = new GameObject("Organos", typeof(RectTransform));
            column.transform.SetParent(c, false);
            UIFactory.Place(UIFactory.Rect(column), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -Pad - 300f), new Vector2(W - Pad * 2f, 360f));
            UIFactory.VerticalLayout(column, 10f, TextAnchor.UpperCenter);

            foreach (LevelData level in system.levels)
            {
                if (level == null || !progress.IsLevelVisible(level)) continue; // los secretos no se muestran
                BuildLevelRow(column.transform, level, progress, unlocked, tone, W - Pad * 2f - 12f);
            }
        }

        /// <summary>Bolitas de avance (llenas = órganos limpios).</summary>
        private static void ProgressDots(Transform parent, int done, int total)
        {
            Image ring = CartoonUI.Image(parent, "Aro", CartoonUI.Circle, CartoonUI.Outline);
            UIFactory.Stretch(ring.rectTransform);
            Image fill = CartoonUI.Image(parent, "Avance", CartoonUI.Circle, CartoonUI.Yellow);
            UIFactory.Stretch(fill.rectTransform, 4f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = 2;
            fill.fillAmount = total > 0 ? (float)done / total : 0f;
        }

        private void BuildLevelRow(Transform parent, LevelData level, ProgressService progress, bool systemUnlocked, Color tone, float width)
        {
            bool playable = progress.IsLevelPlayable(level);
            bool done = progress.IsLevelCompleted(level);
            bool next = level == nextLevel;

            string label = level.hidden ? "Secreto: " + level.displayName : level.displayName;
            string sub = done ? "Completado" : next ? "Siguiente" : systemUnlocked && !level.HasScene ? "Próximamente" : !playable ? "Bloqueado" : "Disponible";

            LevelData captured = level;
            Color color = done ? Color.Lerp(tone, Panel, 0.45f) : Color.Lerp(tone, CartoonUI.Red, 0.3f);
            Button button = CartoonUI.Button(parent, "", () => GameSession.StartLevel(captured), new Vector2(width, 76f), color, 26f);
            button.interactable = playable;
            if (next && playable) focus = button;
            CartoonButton juicy = button.GetComponent<CartoonButton>();
            if (next && juicy != null) juicy.Highlight = true;
            Transform body = button.transform.Find("Relleno");

            // Foto redonda del órgano (o candado si está bloqueado).
            var photo = new GameObject("FotoOrgano", typeof(RectTransform));
            photo.transform.SetParent(body, false);
            UIFactory.Place(UIFactory.Rect(photo), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(58f, 58f));
            if (playable || done) CartoonUI.RoundPicture(photo.transform, ArtFor(level), 58f, tone);
            else
            {
                Image disc = CartoonUI.Image(photo.transform, "Disco", CartoonUI.Circle, new Color(0.18f, 0.16f, 0.19f));
                UIFactory.Stretch(disc.rectTransform);
                CartoonUI.Padlock(photo.transform, 32f, TextDim);
            }

            // Nombre y estado: texto limpio (sin contorno) para que se lea.
            TextMeshProUGUI text = body.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.outlineWidth = 0f;
                text.fontMaterial.DisableKeyword(ShaderUtilities.Keyword_Underlay);
                string subColor = done ? "C9FFD8" : next ? "FFE27A" : playable ? "FFFFFF" : "DDD5DB";
                text.text = $"{label}\n<size=70%><color=#{subColor}>{sub}</color></size>";
                text.alignment = TextAlignmentOptions.MidlineLeft;
                text.color = playable || done ? Color.white : TextDim;
                text.rectTransform.offsetMin = new Vector2(78f, 4f);
                text.rectTransform.offsetMax = new Vector2(done ? -48f : -10f, -4f);
                text.fontSize = label.Length > 16 ? 23f : 26f;
            }

            if (done)
            {
                TextMeshProUGUI check = CartoonUI.Badge(body, "", CartoonUI.Mint, 36f, 20f);
                UIFactory.Place((RectTransform)check.transform.parent.parent, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-26f, 0f), new Vector2(36f, 36f));
                CartoonUI.CheckMark(check.transform.parent, 22f);
            }
        }
    }
}
