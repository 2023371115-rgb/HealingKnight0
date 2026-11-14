using HealingKnight.Core.Flow;
using HealingKnight.Core.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HealingKnight.UI
{
    /// <summary>
    /// Mapa del cuerpo: los sistemas (I–IV) y sus niveles según el progreso guardado, con el estilo de caricatura
    /// del juego. Cada sistema es una tarjeta con la pintura de su primer órgano arriba, su número en una insignia
    /// y su estado; cada nivel es un botón con una "foto" redonda de su fondo y una palomita si ya lo completaste.
    /// - Sistema bloqueado: gris con candado. Nivel sin escena todavía: "Próximamente". Secretos: no se muestran.
    /// Todo sale del GameCatalog: agregar un sistema o nivel no requiere tocar esta pantalla.
    /// Los dibujos (fondos de nivel, microbios) y la letra los asigna SceneSetup.
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

        private Button firstPlayable;

        private void Awake()
        {
            Time.timeScale = 1f;
            CartoonUI.SetFonts(titleFont, bodyFont);
            Build();
        }

        private void Start() => UIFactory.Select(firstPlayable);

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
            Image bg = UIFactory.Panel(canvas.transform, "Fondo", new Color(0.14f, 0.05f, 0.1f, 1f));
            UIFactory.Stretch(bg.rectTransform);
            var bugs = new GameObject("Microbios", typeof(RectTransform));
            bugs.transform.SetParent(canvas.transform, false);
            UIFactory.Stretch(UIFactory.Rect(bugs));
            bugs.AddComponent<FloatingMicrobes>().Init(microbes, 14);
            Image vig = CartoonUI.Image(canvas.transform, "Viñeta", CartoonUI.Vignette, new Color(0.05f, 0f, 0.03f, 0.9f));
            UIFactory.Stretch(vig.rectTransform, -200f);

            // Título en una cinta.
            Image ribbon = CartoonUI.Blob(canvas.transform, "Cinta", CartoonUI.Red, new Vector2(1180f, 104f), 7f, 1f);
            RectTransform rr = (RectTransform)ribbon.transform.parent;
            UIFactory.Place(rr, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(1180f, 104f));
            rr.gameObject.AddComponent<UIBob>().height = 4f;
            TextMeshProUGUI title = CartoonUI.Text(ribbon.transform, "MAPA DEL CUERPO  ·  PACIENTE 001", 54f, CartoonUI.Cream, true);
            UIFactory.Stretch(title.rectTransform, 6f);

            GameCatalog catalog = GameSession.Catalog;
            ProgressService progress = GameSession.Progress;
            if (catalog == null || progress == null)
            {
                var error = CartoonUI.Text(canvas.transform, "Falta el GameCatalog. Ejecuta 'Healing Knight > Crear proyecto base'.", 36f, Color.red);
                UIFactory.Place(error.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600f, 200f));
                return;
            }

            BuildFormulas(canvas.transform, GameSession.Save.formulas.Count, catalog.systems.Count);

            var row = new GameObject("Sistemas", typeof(RectTransform));
            row.transform.SetParent(canvas.transform, false);
            UIFactory.Place(UIFactory.Rect(row), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(1840f, 800f));
            UIFactory.HorizontalLayout(row, 26f);

            foreach (SystemData system in catalog.systems)
                if (system != null) BuildSystemCard(row.transform, system, progress);

            Button back = CartoonUI.Button(canvas.transform, "Menú", GameSession.GoToMainMenu, new Vector2(260f, 82f), new Color(0.55f, 0.3f, 0.5f), 32f);
            UIFactory.Place((RectTransform)back.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -36f), new Vector2(260f, 82f));
            if (firstPlayable == null) firstPlayable = back;

            Image fade = UIFactory.Panel(canvas.transform, "Fundido", Color.black);
            UIFactory.Stretch(fade.rectTransform);
            fade.raycastTarget = false;
            fade.gameObject.AddComponent<UIFadeIn>();
        }

        /// <summary>Contador de fórmulas: una cápsula por sistema, llena si ya obtuviste su fórmula.</summary>
        private void BuildFormulas(Transform parent, int got, int total)
        {
            var box = new GameObject("Formulas", typeof(RectTransform));
            box.transform.SetParent(parent, false);
            UIFactory.Place(UIFactory.Rect(box), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(900f, 60f));
            UIFactory.HorizontalLayout(box, 14f);
            TextMeshProUGUI label = CartoonUI.Text(box.transform, "FÓRMULAS", 30f, CartoonUI.Sky, true);
            label.rectTransform.sizeDelta = new Vector2(220f, 60f);
            for (int i = 0; i < total; i++)
            {
                bool on = i < got;
                Image pill = CartoonUI.Blob(box.transform, "Capsula", on ? CartoonUI.Sky : new Color(0.25f, 0.18f, 0.25f), new Vector2(84f, 42f), 5f, 0.6f, true, on);
                if (on)
                {
                    Image half = CartoonUI.Image(pill.transform, "Mitad", CartoonUI.Rounded, CartoonUI.Cream, true, 0.4f);
                    RectTransform hr = half.rectTransform;
                    hr.anchorMin = new Vector2(0.5f, 0f); hr.anchorMax = Vector2.one; hr.offsetMin = hr.offsetMax = Vector2.zero;
                }
            }
            TextMeshProUGUI count = CartoonUI.Text(box.transform, $"{got} / {total}", 30f, CartoonUI.Cream, true);
            count.rectTransform.sizeDelta = new Vector2(110f, 60f);
        }

        private void BuildSystemCard(Transform parent, SystemData system, ProgressService progress)
        {
            bool unlocked = progress.IsSystemUnlocked(system);
            bool completed = progress.IsSystemCompleted(system);
            Color tone = unlocked ? system.mapColor : CartoonUI.Locked;
            Color cardColor = unlocked ? Color.Lerp(system.mapColor, new Color(0.12f, 0.03f, 0.08f), 0.62f) : new Color(0.2f, 0.18f, 0.2f, 1f);

            Image card = CartoonUI.Blob(parent, "Sistema_" + system.id, cardColor, new Vector2(438f, 780f), 8f, 1.3f);
            Transform c = card.transform;

            // Pintura del primer órgano del sistema arriba (recortada en la tarjeta).
            Sprite header = null;
            foreach (LevelData l in system.levels) { header = ArtFor(l); if (header != null) break; }
            var top = new GameObject("Portada", typeof(RectTransform));
            top.transform.SetParent(c, false);
            RectTransform tr = UIFactory.Rect(top);
            tr.anchorMin = new Vector2(0f, 1f); tr.anchorMax = new Vector2(1f, 1f); tr.pivot = new Vector2(0.5f, 1f);
            tr.sizeDelta = new Vector2(-28f, 180f); tr.anchoredPosition = new Vector2(0f, -14f);
            Image frame = CartoonUI.Image(top.transform, "Marco", CartoonUI.Rounded, CartoonUI.Outline, true, 0.7f);
            UIFactory.Stretch(frame.rectTransform);
            Image maskImg = CartoonUI.Image(top.transform, "Mascara", CartoonUI.Rounded, Color.white, true, 0.6f);
            UIFactory.Stretch(maskImg.rectTransform, 5f);
            maskImg.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            maskImg.color = Color.Lerp(tone, Color.black, 0.4f);
            if (header != null)
            {
                Image pic = CartoonUI.Cover(maskImg.transform, header, new Vector2(410f, 172f));
                pic.color = unlocked ? Color.white : new Color(0.35f, 0.35f, 0.35f, 1f);
            }

            // Número del sistema en una insignia, montada sobre la portada.
            TextMeshProUGUI num = CartoonUI.Badge(c, system.mapNumber, unlocked ? tone : CartoonUI.Locked, 104f, system.mapNumber.Length > 2 ? 40f : 50f);
            RectTransform nb = (RectTransform)num.transform.parent.parent;
            UIFactory.Place(nb, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -194f), new Vector2(104f, 104f));
            if (unlocked && !completed) nb.gameObject.AddComponent<UIBob>().height = 5f;

            TextMeshProUGUI name = CartoonUI.Text(c, system.displayName, 34f, unlocked ? CartoonUI.Cream : Color.gray, true);
            UIFactory.Place(name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(410f, 50f));

            // Estado en una pastilla de color.
            string status = completed ? "Desinfectado" : unlocked ? "¡Infección activa!" : "Bloqueado";
            Color statusColor = completed ? CartoonUI.Mint : unlocked ? CartoonUI.Red : CartoonUI.Locked;
            Image pill = CartoonUI.Blob(c, "Estado", statusColor, new Vector2(300f, 50f), 5f, 0.6f, false, true);
            UIFactory.Place((RectTransform)pill.transform.parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -304f), new Vector2(300f, 50f));
            TextMeshProUGUI st = CartoonUI.Text(pill.transform, status, 26f, CartoonUI.Cream, true, TextAlignmentOptions.Center, 0.18f);
            UIFactory.Stretch(st.rectTransform, 2f);
            if (unlocked && !completed) pill.transform.parent.gameObject.AddComponent<UIBob>().height = 3f;

            var column = new GameObject("Niveles", typeof(RectTransform));
            column.transform.SetParent(c, false);
            UIFactory.Place(UIFactory.Rect(column), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -366f), new Vector2(410f, 370f));
            UIFactory.VerticalLayout(column, 8f, TextAnchor.UpperCenter);

            foreach (LevelData level in system.levels)
            {
                if (level == null || !progress.IsLevelVisible(level)) continue; // los secretos no se muestran
                BuildLevelButton(column.transform, level, progress, unlocked, tone);
            }

            if (!unlocked)
            {
                TextMeshProUGUI lockText = CartoonUI.Text(c, "Completa los sistemas\nanteriores para entrar", 24f, new Color(1f, 1f, 1f, 0.6f), false);
                UIFactory.Place(lockText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(400f, 70f));
            }
        }

        private void BuildLevelButton(Transform parent, LevelData level, ProgressService progress, bool systemUnlocked, Color tone)
        {
            bool playable = progress.IsLevelPlayable(level);
            bool done = progress.IsLevelCompleted(level);

            string label = level.hidden ? "Secreto: " + level.displayName : level.displayName;
            string sub = done ? "Completado" : systemUnlocked && !level.HasScene ? "Próximamente" : !playable ? "Bloqueado" : "¡Entra!";

            LevelData captured = level;
            Color color = done ? Color.Lerp(tone, new Color(0.25f, 0.1f, 0.15f), 0.35f) : Color.Lerp(tone, CartoonUI.Red, 0.35f);
            Button button = CartoonUI.Button(parent, "", () => GameSession.StartLevel(captured), new Vector2(392f, 84f), color, 28f);
            button.interactable = playable;
            Transform body = button.GetComponent<CartoonButton>() != null ? button.transform.Find("Relleno") : button.transform;

            // Foto redonda del nivel a la izquierda.
            var photoHolder = new GameObject("FotoNivel", typeof(RectTransform));
            photoHolder.transform.SetParent(body, false);
            UIFactory.Place(UIFactory.Rect(photoHolder), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(6f, 0f), new Vector2(66f, 66f));
            CartoonUI.RoundPicture(photoHolder.transform, ArtFor(level), 66f, tone);

            // Nombre y estado.
            TextMeshProUGUI text = body.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = $"{label}\n<size=62%><color=#{(done ? "B8FFC8" : playable ? "FFE9A0" : "CCCCCC")}>{sub}</color></size>";
                text.alignment = TextAlignmentOptions.MidlineLeft;
                text.rectTransform.offsetMin = new Vector2(84f, 4f);
                text.fontSize = label.Length > 16 ? 25f : 28f;
            }

            if (done)
            {
                TextMeshProUGUI check = CartoonUI.Badge(button.transform, "", CartoonUI.Mint, 46f, 28f);
                UIFactory.Place((RectTransform)check.transform.parent.parent, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-14f, -14f), new Vector2(46f, 46f));
                CartoonUI.CheckMark(check.transform.parent, 30f);
            }

            // Selecciona por defecto el primer nivel jugable que no se haya completado.
            if (playable && !done && firstPlayable == null) firstPlayable = button;
        }
    }
}
