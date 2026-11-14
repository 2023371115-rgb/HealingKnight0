using HealingKnight.Core.Flow;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HealingKnight.UI
{
    /// <summary>
    /// Menú principal y prólogo (guion técnico E01), con el estilo de caricatura del juego:
    /// fondo pintado de un órgano, microbios flotando, título con contorno grueso y botones que rebotan.
    /// El prólogo se muestra la primera vez que se juega (se puede saltar) y escribe el texto letra por letra.
    /// Si existe Art/UI/Logo.png se usa en lugar del título de texto; si existe Art/UI/Portada.png, de fondo.
    /// Los dibujos y la letra los asigna SceneSetup al crear la escena.
    /// </summary>
    public class MainMenuScreen : MonoBehaviour
    {
        [SerializeField, TextArea]
        private string[] prologuePages =
        {
            "AÑO 2060 · AURELIS NANOMEDICINA",
            "NODO: Programa Centinela. Nanobots que destruyen enfermedades desde dentro del cuerpo.",
            "CONTENCIÓN ROTA. Las enfermedades de prueba mutaron. Los medicamentos ya no las detienen.",
            "PACIENTE 001 · ESTADO CRÍTICO",
            "NODO: Healing Knight, trata cada amenaza con la medicina correcta."
        };

        [Header("Arte (lo pone SceneSetup)")]
        [SerializeField] private Font titleFont;
        [SerializeField] private Font bodyFont;
        [SerializeField] private Sprite background;
        [SerializeField] private Sprite logo;
        [SerializeField] private Sprite[] microbes;
        [SerializeField] private float charactersPerSecond = 40f;

        private GameObject menuRoot;
        private GameObject prologueRoot;
        private GameObject confirmRoot;
        private TextMeshProUGUI prologueText;
        private TextMeshProUGUI pageCounter;
        private Button playButton;
        private Button confirmNoButton;
        private int page;
        private int prologueOpenedFrame;
        private float typed;

        private void Awake()
        {
            Time.timeScale = 1f;
            CartoonUI.SetFonts(titleFont, bodyFont);
            Canvas canvas = UIFactory.CreateCanvas("Menu Canvas", 0, transform);

            BuildBackground(canvas.transform);
            BuildMenu(canvas.transform);
            BuildPrologue(canvas.transform);
            BuildConfirm(canvas.transform);

            Image fade = UIFactory.Panel(canvas.transform, "Fundido", Color.black);
            UIFactory.Stretch(fade.rectTransform);
            fade.raycastTarget = false;
            fade.gameObject.AddComponent<UIFadeIn>();
        }

        private void Start() => UIFactory.Select(playButton);

        private void BuildBackground(Transform parent)
        {
            Image bg = UIFactory.Panel(parent, "Fondo", new Color(0.35f, 0.08f, 0.14f, 1f));
            UIFactory.Stretch(bg.rectTransform);
            if (background != null)
            {
                var holder = new GameObject("Pintura", typeof(RectTransform));
                holder.transform.SetParent(parent, false);
                UIFactory.Stretch(UIFactory.Rect(holder));
                holder.AddComponent<RectMask2D>();
                CartoonUI.Cover(holder.transform, background, new Vector2(1920f, 1080f) * 1.05f);
            }
            // Oscurece un poco para que se lea el texto y viñeta en las orillas.
            Image tint = UIFactory.Panel(parent, "Oscurecer", new Color(0.18f, 0.02f, 0.08f, 0.45f));
            UIFactory.Stretch(tint.rectTransform);
            tint.raycastTarget = false;

            var bugs = new GameObject("Microbios", typeof(RectTransform));
            bugs.transform.SetParent(parent, false);
            UIFactory.Stretch(UIFactory.Rect(bugs));
            bugs.AddComponent<FloatingMicrobes>().Init(microbes, 18);

            Image vig = CartoonUI.Image(parent, "Viñeta", CartoonUI.Vignette, new Color(0.1f, 0f, 0.04f, 0.85f));
            UIFactory.Stretch(vig.rectTransform, -200f);
        }

        private void BuildMenu(Transform parent)
        {
            menuRoot = new GameObject("Menu", typeof(RectTransform));
            menuRoot.transform.SetParent(parent, false);
            UIFactory.Stretch(UIFactory.Rect(menuRoot));

            // Título (o logo) que flota.
            var titleBox = new GameObject("Titulo", typeof(RectTransform));
            titleBox.transform.SetParent(menuRoot.transform, false);
            UIFactory.Place(UIFactory.Rect(titleBox), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 270f), new Vector2(1500f, 300f));
            titleBox.AddComponent<UIBob>().height = 8f;
            Image halo = CartoonUI.Image(titleBox.transform, "Halo", CartoonUI.Glow, new Color(1f, 0.5f, 0.6f, 0.35f));
            UIFactory.Place(halo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500f, 420f));
            if (logo != null)
            {
                Image l = CartoonUI.Image(titleBox.transform, "Logo", logo, Color.white);
                l.preserveAspect = true;
                UIFactory.Stretch(l.rectTransform);
            }
            else
            {
                TextMeshProUGUI t1 = CartoonUI.Text(titleBox.transform, "HEALING", 150f, CartoonUI.Cream, true, TextAlignmentOptions.Center, 0.28f);
                UIFactory.Place(t1.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-10f, 62f), new Vector2(1400f, 170f));
                TextMeshProUGUI t2 = CartoonUI.Text(titleBox.transform, "KNIGHT", 150f, CartoonUI.Pink, true, TextAlignmentOptions.Center, 0.28f);
                UIFactory.Place(t2.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(30f, -78f), new Vector2(1400f, 170f));
                t2.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 2.5f);
            }

            // Subtítulo en una cinta.
            Image ribbon = CartoonUI.Blob(menuRoot.transform, "Cinta", new Color(0.35f, 0.08f, 0.16f, 1f), new Vector2(1180f, 76f), 6f, 1f, true, false);
            UIFactory.Place((RectTransform)ribbon.transform.parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 75f), new Vector2(1180f, 76f));
            TextMeshProUGUI sub = CartoonUI.Text(ribbon.transform, "Diagnostica, elige el medicamento correcto y salva al paciente desde adentro.", 30f, CartoonUI.Cream, false, TextAlignmentOptions.Center, 0.15f);
            UIFactory.Stretch(sub.rectTransform, 8f);

            var column = new GameObject("Botones", typeof(RectTransform));
            column.transform.SetParent(menuRoot.transform, false);
            UIFactory.Place(UIFactory.Rect(column), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -190f), new Vector2(560f, 380f));
            UIFactory.VerticalLayout(column, 26f);

            playButton = CartoonUI.Button(column.transform, "¡JUGAR!", OnPlay, new Vector2(500f, 120f), CartoonUI.Red, 58f);
            CartoonUI.Button(column.transform, "Borrar progreso", ShowConfirm, new Vector2(420f, 88f), new Color(0.75f, 0.35f, 0.55f), 34f);
            CartoonUI.Button(column.transform, "Salir", Quit, new Vector2(420f, 88f), new Color(0.55f, 0.3f, 0.5f), 34f);

            TextMeshProUGUI footer = CartoonUI.Text(menuRoot.transform, "PACIENTE 001  ·  AURELIS NANOMEDICINA  ·  PROGRAMA CENTINELA", 22f, new Color(1f, 0.85f, 0.88f, 0.75f), false, TextAlignmentOptions.Center, 0.12f);
            UIFactory.Place(footer.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(1600f, 40f));
        }

        private void BuildPrologue(Transform parent)
        {
            Image panel = UIFactory.Panel(parent, "Prologo", new Color(0.04f, 0.01f, 0.04f, 0.93f));
            UIFactory.Stretch(panel.rectTransform);
            prologueRoot = panel.gameObject;

            // Pantalla de NODO: tarjeta redonda con la insignia de la IA.
            Image card = CartoonUI.Blob(prologueRoot.transform, "Tarjeta", new Color(0.12f, 0.06f, 0.14f, 1f), new Vector2(1500f, 520f), 8f, 1.2f);
            UIFactory.Place((RectTransform)card.transform.parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(1500f, 520f));
            TextMeshProUGUI badge = CartoonUI.Badge(card.transform, "N", CartoonUI.Sky, 120f, 64f);
            UIFactory.Place((RectTransform)badge.transform.parent.parent, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(110f, -20f), new Vector2(120f, 120f));
            badge.transform.parent.parent.gameObject.AddComponent<UIBob>().height = 5f;

            prologueText = CartoonUI.Text(card.transform, "", 52f, CartoonUI.Cream, false, TextAlignmentOptions.Center, 0.12f);
            UIFactory.Stretch(prologueText.rectTransform, 70f);

            pageCounter = CartoonUI.Text(card.transform, "", 24f, CartoonUI.Sky, false, TextAlignmentOptions.BottomRight, 0.1f);
            UIFactory.Stretch(pageCounter.rectTransform, 28f);

            var hint = CartoonUI.Text(prologueRoot.transform, "Espacio / Enter / A: continuar     Esc / Start: saltar", 26f, new Color(1f, 1f, 1f, 0.6f), false, TextAlignmentOptions.Center, 0.1f);
            UIFactory.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1400f, 40f));

            prologueRoot.SetActive(false);
        }

        private void BuildConfirm(Transform parent)
        {
            Image panel = UIFactory.Panel(parent, "Confirmar", new Color(0f, 0f, 0f, 0.75f));
            UIFactory.Stretch(panel.rectTransform);
            confirmRoot = panel.gameObject;

            Image card = CartoonUI.Blob(confirmRoot.transform, "Tarjeta", new Color(0.3f, 0.07f, 0.14f, 1f), new Vector2(1000f, 400f), 8f, 1.2f);
            UIFactory.Place((RectTransform)card.transform.parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 400f));

            var text = CartoonUI.Text(card.transform, "¿Borrar todo el progreso guardado?", 46f, CartoonUI.Cream, true);
            UIFactory.Place(text.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(900f, 140f));

            var row = new GameObject("Fila", typeof(RectTransform));
            row.transform.SetParent(card.transform, false);
            UIFactory.Place(UIFactory.Rect(row), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(860f, 110f));
            UIFactory.HorizontalLayout(row, 40f);

            CartoonUI.Button(row.transform, "Sí, borrar", () =>
            {
                GameSession.ResetProgress();
                HideConfirm();
            }, new Vector2(360f, 96f), CartoonUI.Red, 36f);
            confirmNoButton = CartoonUI.Button(row.transform, "Cancelar", HideConfirm, new Vector2(360f, 96f), new Color(0.4f, 0.65f, 0.55f), 36f);

            confirmRoot.SetActive(false);
        }

        private void OnPlay()
        {
            if (GameSession.Save.prologueSeen || prologuePages == null || prologuePages.Length == 0)
            {
                GameSession.GoToBodyMap();
                return;
            }

            menuRoot.SetActive(false);
            prologueRoot.SetActive(true);
            page = 0;
            prologueOpenedFrame = Time.frameCount;
            ShowPage();
        }

        private void ShowPage()
        {
            typed = 0f;
            prologueText.text = prologuePages[page];
            prologueText.maxVisibleCharacters = 0;
            pageCounter.text = $"{page + 1} / {prologuePages.Length}";
        }

        private void Update()
        {
            if (prologueRoot == null || !prologueRoot.activeSelf) return;

            // Escribe letra por letra.
            int total = prologuePages[page].Length;
            if (prologueText.maxVisibleCharacters < total)
            {
                typed += Time.unscaledDeltaTime * charactersPerSecond;
                prologueText.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(typed));
            }

            if (Time.frameCount <= prologueOpenedFrame) return; // ignora la tecla que abrió el prólogo

            bool skip = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                        || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
            bool next = (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
                        || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
                        || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

            if (skip) { FinishPrologue(); return; }
            if (!next) return;

            // Si todavía se está escribiendo, la primera tecla muestra todo el texto.
            if (prologueText.maxVisibleCharacters < total)
            {
                prologueText.maxVisibleCharacters = total;
                return;
            }

            page++;
            if (page >= prologuePages.Length) FinishPrologue();
            else ShowPage();
        }

        private void FinishPrologue()
        {
            GameSession.MarkPrologueSeen();
            GameSession.GoToBodyMap();
        }

        private void ShowConfirm()
        {
            confirmRoot.SetActive(true);
            UIFactory.Select(confirmNoButton);
        }

        private void HideConfirm()
        {
            confirmRoot.SetActive(false);
            UIFactory.Select(playButton);
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
