using HealingKnight.Core.Flow;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HealingKnight.UI
{
    /// <summary>
    /// Menú principal y prólogo (guion técnico E01). El prólogo se muestra la primera vez que se juega
    /// y se puede saltar. Al terminar lleva al mapa del cuerpo.
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

        private GameObject menuRoot;
        private GameObject prologueRoot;
        private GameObject confirmRoot;
        private TextMeshProUGUI prologueText;
        private Button playButton;
        private Button confirmNoButton;
        private int page;
        private int prologueOpenedFrame;

        private void Awake()
        {
            Time.timeScale = 1f;
            Canvas canvas = UIFactory.CreateCanvas("Menu Canvas", 0, transform);

            Image bg = UIFactory.Panel(canvas.transform, "Background", new Color(0.35f, 0.08f, 0.14f, 1f));
            UIFactory.Stretch(bg.rectTransform);

            BuildMenu(canvas.transform);
            BuildPrologue(canvas.transform);
            BuildConfirm(canvas.transform);
        }

        private void Start()
        {
            UIFactory.Select(playButton);
        }

        private void BuildMenu(Transform parent)
        {
            menuRoot = new GameObject("Menu", typeof(RectTransform));
            menuRoot.transform.SetParent(parent, false);
            UIFactory.Stretch(UIFactory.Rect(menuRoot));

            var title = UIFactory.Text(menuRoot.transform, "HEALING KNIGHT", 130f, UIFactory.TextColor);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 280f), new Vector2(1600f, 180f));

            var subtitle = UIFactory.Text(menuRoot.transform, "Diagnostica, elige el medicamento correcto y salva al paciente desde adentro.", 34f, new Color(1f, 0.85f, 0.85f));
            UIFactory.Place(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(1600f, 60f));

            var column = new GameObject("Buttons", typeof(RectTransform));
            column.transform.SetParent(menuRoot.transform, false);
            UIFactory.Place(UIFactory.Rect(column), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(520f, 360f));
            UIFactory.VerticalLayout(column, 24f);

            Vector2 size = new Vector2(460f, 86f);
            playButton = UIFactory.Button(column.transform, "Jugar", OnPlay, size, 42f);
            UIFactory.Button(column.transform, "Borrar progreso", ShowConfirm, size);
            UIFactory.Button(column.transform, "Salir", Quit, size);
        }

        private void BuildPrologue(Transform parent)
        {
            Image panel = UIFactory.Panel(parent, "Prologue", new Color(0.02f, 0.02f, 0.04f, 1f));
            UIFactory.Stretch(panel.rectTransform);
            prologueRoot = panel.gameObject;

            prologueText = UIFactory.Text(prologueRoot.transform, "", 54f, UIFactory.TextColor);
            UIFactory.Place(prologueText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1500f, 400f));

            var hint = UIFactory.Text(prologueRoot.transform, "Espacio / Enter / A: continuar     Esc / Start: saltar", 26f, new Color(1f, 1f, 1f, 0.6f));
            UIFactory.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1400f, 40f));

            prologueRoot.SetActive(false);
        }

        private void BuildConfirm(Transform parent)
        {
            Image panel = UIFactory.Panel(parent, "Confirm", new Color(0f, 0f, 0f, 0.85f));
            UIFactory.Stretch(panel.rectTransform);
            confirmRoot = panel.gameObject;

            var text = UIFactory.Text(confirmRoot.transform, "¿Borrar todo el progreso guardado?", 48f, UIFactory.TextColor);
            UIFactory.Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1400f, 80f));

            var row = new GameObject("Row", typeof(RectTransform));
            row.transform.SetParent(confirmRoot.transform, false);
            UIFactory.Place(UIFactory.Rect(row), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(800f, 100f));
            UIFactory.HorizontalLayout(row, 40f);

            UIFactory.Button(row.transform, "Sí, borrar", () =>
            {
                GameSession.ResetProgress();
                HideConfirm();
            }, new Vector2(340f, 84f));
            confirmNoButton = UIFactory.Button(row.transform, "Cancelar", HideConfirm, new Vector2(340f, 84f));

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
            prologueText.text = prologuePages[0];
        }

        private void Update()
        {
            if (prologueRoot == null || !prologueRoot.activeSelf) return;
            if (Time.frameCount <= prologueOpenedFrame) return; // ignora la tecla que abrió el prólogo

            bool skip = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                        || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
            bool next = (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
                        || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
                        || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

            if (skip) { FinishPrologue(); return; }
            if (!next) return;

            page++;
            if (page >= prologuePages.Length) FinishPrologue();
            else prologueText.text = prologuePages[page];
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
