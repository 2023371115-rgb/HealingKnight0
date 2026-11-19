using System;
using System.Collections;
using System.Collections.Generic;
using HealingKnight.Gameplay.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HealingKnight.UI
{
    /// <summary>
    /// Menú de CONTROLES (se abre desde el menú principal o la pausa).
    ///  - Esquema de teclado: Cuphead (flechas + Z X C V) o Clásico (W A S D + J K L).
    ///  - Cualquier acción se puede cambiar: clic (o Enter / A) en la tecla o el botón y presiona el nuevo.
    ///    Si ya lo usaba otra acción, se intercambian. Esc cancela.
    ///  - Apuntar con el mouse (apagado = como Cuphead) y Restablecer.
    /// Todo se guarda solo (PlayerPrefs) y se aplica al momento, también a media partida.
    /// </summary>
    public class ControlsScreen : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        private static readonly Color Panel = new Color(0.27f, 0.12f, 0.22f, 1f);
        private static readonly Color Dim = new Color(1f, 0.9f, 0.92f, 0.7f);
        private static readonly Color KeyColor = new Color(0.42f, 0.25f, 0.45f);
        private static readonly Color PadColor = new Color(0.25f, 0.38f, 0.55f);

        private Action onClosed;
        private GameObject previousSelection;
        private int openedFrame;
        private readonly Dictionary<Selectable, Navigation> frozen = new Dictionary<Selectable, Navigation>();

        private struct Cell { public Button button; public PlayerControls.Row row; public bool gamepad; }
        private readonly List<Cell> cells = new List<Cell>();
        private Button cupheadButton, classicButton, mouseButton, firstCell;
        private TextMeshProUGUI status;

        private InputActionRebindingExtensions.RebindingOperation operation;
        private bool listening, waitRelease;
        private bool mapWasEnabled;
        private Button listeningButton;
        private float listenUntil;

        public static ControlsScreen Open(Action closed = null)
        {
            if (IsOpen) return null;
            var go = new GameObject("Controles");
            var screen = go.AddComponent<ControlsScreen>();
            screen.onClosed = closed;
            return screen;
        }

        private void Awake()
        {
            IsOpen = true;
            openedFrame = Time.frameCount;
            previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            FreezeOtherMenus();
            Build();
            Refresh();
            PlayerControls.Changed += Refresh;
        }

        private void OnDestroy()
        {
            IsOpen = false;
            PlayerControls.Changed -= Refresh;
            StopListening(false);
            if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = true;
            foreach (KeyValuePair<Selectable, Navigation> pair in frozen)
                if (pair.Key != null) pair.Key.navigation = pair.Value;
            frozen.Clear();
        }

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
            if (listening) return;
            Destroy(gameObject);
            if (previousSelection != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(previousSelection);
            onClosed?.Invoke();
        }

        // ---------- Construcción ----------

        private void Build()
        {
            Canvas canvas = UIFactory.CreateCanvas("Controles Canvas", 60, transform);
            Image bg = UIFactory.Panel(canvas.transform, "Fondo", new Color(0.1f, 0.03f, 0.08f, 0.97f));
            UIFactory.Stretch(bg.rectTransform);
            Image glow = CartoonUI.Image(canvas.transform, "Luz", CartoonUI.Glow, new Color(0.9f, 0.35f, 0.5f, 0.15f));
            UIFactory.Place(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 100f), new Vector2(2400f, 1400f));

            TextMeshProUGUI head = CartoonUI.Text(canvas.transform, "CONTROLES", 60f, CartoonUI.Cream, true, TextAlignmentOptions.Left, 0.2f);
            UIFactory.Place(head.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -22f), new Vector2(700f, 80f));
            TextMeshProUGUI sub = CartoonUI.Text(canvas.transform,
                "Como en Cuphead: mantén FIJAR para quedarte quieto y apuntar en 8 direcciones.", 24f, Dim, false, TextAlignmentOptions.Left, 0f);
            UIFactory.Place(sub.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, -98f), new Vector2(1300f, 34f));

            Button back = CartoonUI.Button(canvas.transform, "Volver", Close, new Vector2(220f, 76f), new Color(0.42f, 0.25f, 0.4f), 30f);
            UIFactory.Place((RectTransform)back.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(220f, 76f));

            // Esquemas y opciones.
            var options = new GameObject("Opciones", typeof(RectTransform));
            options.transform.SetParent(canvas.transform, false);
            UIFactory.Place(UIFactory.Rect(options), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -146f), new Vector2(1800f, 70f));
            UIFactory.HorizontalLayout(options, 16f, TextAnchor.MiddleCenter);
            TextMeshProUGUI label = CartoonUI.Text(options.transform, "Teclado:", 26f, Dim, false, TextAlignmentOptions.MidlineRight, 0f);
            label.rectTransform.sizeDelta = new Vector2(130f, 60f);
            cupheadButton = CartoonUI.Button(options.transform, "Cuphead (flechas + Z X C V)", () => PlayerControls.ApplyPreset("cuphead"), new Vector2(460f, 64f), CartoonUI.Red, 24f);
            classicButton = CartoonUI.Button(options.transform, "Clásico (W A S D + J K L)", () => PlayerControls.ApplyPreset("clasico"), new Vector2(420f, 64f), new Color(0.35f, 0.55f, 0.6f), 24f);
            mouseButton = CartoonUI.Button(options.transform, "Apuntar con mouse: NO", () => PlayerControls.MouseAim = !PlayerControls.MouseAim, new Vector2(370f, 64f), new Color(0.57f, 0.34f, 0.61f), 24f);
            CartoonUI.Button(options.transform, "Restablecer", () => { PlayerControls.MouseAim = false; PlayerControls.ResetAll(); }, new Vector2(240f, 64f), new Color(0.45f, 0.35f, 0.4f), 24f);

            // Tabla.
            Image table = CartoonUI.Blob(canvas.transform, "Tabla", Panel, new Vector2(1500f, 760f), 7f, 1f, true, false);
            RectTransform tableRoot = (RectTransform)table.transform.parent;
            UIFactory.Place(tableRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -236f), new Vector2(1500f, 760f));
            Transform t = table.transform;

            Header(t, "Acción", 60f, TextAlignmentOptions.Left, 560f);
            Header(t, "Teclado", 840f, TextAlignmentOptions.Center, 320f);
            Header(t, "Control", 1220f, TextAlignmentOptions.Center, 320f);

            float y = -76f;
            const float rowH = 48f;
            for (int i = 0; i < PlayerControls.Rows.Length; i++)
            {
                PlayerControls.Row row = PlayerControls.Rows[i];
                if (i % 2 == 0)
                {
                    Image stripe = UIFactory.Panel(t, "Franja", new Color(1f, 1f, 1f, 0.04f));
                    stripe.raycastTarget = false;
                    UIFactory.Place(stripe.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, y + 3f), new Vector2(1452f, rowH));
                }
                TextMeshProUGUI name = CartoonUI.Text(t, row.label, 25f, CartoonUI.Cream, false, TextAlignmentOptions.MidlineLeft, 0f);
                UIFactory.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, y), new Vector2(620f, rowH - 6f));

                Cell kb = MakeCell(t, row, false, new Vector2(840f, y));
                if (firstCell == null) firstCell = kb.button;
                if (row.gamepad >= 0) MakeCell(t, row, true, new Vector2(1220f, y));
                else
                {
                    TextMeshProUGUI fixedText = CartoonUI.Text(t, row.fixedGamepad, 22f, Dim, false, TextAlignmentOptions.Center, 0f);
                    UIFactory.Place(fixedText.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(1220f, y), new Vector2(320f, rowH - 6f));
                }
                y -= rowH;
            }

            TextMeshProUGUI always = CartoonUI.Text(t,
                "Siempre: 1-5 medicamento directo  ·  Esc / Start pausa  ·  Stick derecho apuntar libre  ·  Mouse: clic izq. disparar, clic der. escudo, rueda medicamento",
                19f, Dim, false, TextAlignmentOptions.Center, 0f);
            always.enableAutoSizing = true; always.fontSizeMin = 13f; always.fontSizeMax = 19f;
            UIFactory.Place(always.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(1420f, 30f));

            status = CartoonUI.Text(canvas.transform, "", 24f, CartoonUI.Yellow, false, TextAlignmentOptions.Center, 0.1f);
            UIFactory.Place(status.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(1700f, 34f));
            TextMeshProUGUI help = CartoonUI.Text(canvas.transform,
                "Elige una tecla o botón y presiona el nuevo  ·  Esc: cancelar  ·  Esc / B: volver", 18f, new Color(1f, 1f, 1f, 0.45f), false, TextAlignmentOptions.Center, 0f);
            UIFactory.Place(help.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(1700f, 28f));

            UIFactory.Select(firstCell);
        }

        private static void Header(Transform parent, string text, float x, TextAlignmentOptions align, float width)
        {
            TextMeshProUGUI h = CartoonUI.Text(parent, text, 28f, CartoonUI.Yellow, true, align, 0.12f);
            Vector2 pivot = align == TextAlignmentOptions.Left ? new Vector2(0f, 1f) : new Vector2(0.5f, 1f);
            UIFactory.Place(h.rectTransform, new Vector2(0f, 1f), pivot, new Vector2(x, -22f), new Vector2(width, 44f));
        }

        private Cell MakeCell(Transform parent, PlayerControls.Row row, bool gamepad, Vector2 pos)
        {
            var cell = new Cell { row = row, gamepad = gamepad };
            Button b = null;
            b = CartoonUI.Button(parent, "", () => StartListening(cell, b), new Vector2(300f, 40f), gamepad ? PadColor : KeyColor, 21f);
            UIFactory.Place((RectTransform)b.transform, new Vector2(0f, 1f), new Vector2(0.5f, 1f), pos - new Vector2(0f, 3f), new Vector2(300f, 40f));
            cell.button = b;
            cells.Add(cell);
            return cell;
        }

        private void Refresh()
        {
            foreach (Cell c in cells)
                if (c.button != listeningButton) UIFactory.SetButtonLabel(c.button, PlayerControls.DisplayName(c.row, c.gamepad));
            string preset = PlayerControls.Preset;
            Mark(cupheadButton, preset == "cuphead");
            Mark(classicButton, preset == "clasico");
            UIFactory.SetButtonLabel(mouseButton, PlayerControls.MouseAim ? "Apuntar con mouse: SÍ" : "Apuntar con mouse: NO");
            Mark(mouseButton, PlayerControls.MouseAim);
            if (!listening && status != null)
                status.text = preset == "personal" ? "Esquema personalizado (guardado)." : "";
        }

        private static void Mark(Button b, bool on)
        {
            CartoonButton juicy = b != null ? b.GetComponent<CartoonButton>() : null;
            if (juicy != null) juicy.Highlight = on;
        }

        // ---------- Cambiar una tecla ----------

        private void StartListening(Cell cell, Button button)
        {
            if (listening || waitRelease) return;
            StartCoroutine(Listen(cell, button));
        }

        private IEnumerator Listen(Cell cell, Button button)
        {
            listening = true;
            listeningButton = button;
            UIFactory.SetButtonLabel(button, cell.gamepad ? "Presiona un botón..." : "Presiona una tecla...");
            status.text = $"{cell.row.label}: " + (cell.gamepad ? "presiona el botón del control que quieras (Esc cancela)." : "presiona la tecla que quieras (Esc cancela).");
            if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = false;

            // Espera a que sueltes Enter / A (si no, esa misma tecla se tomaría como la nueva).
            while (AnyPressed()) yield return null;

            InputActionMap map = PlayerControls.Map;
            mapWasEnabled = map.enabled;
            map.Disable();

            InputAction action = PlayerControls.Action(cell.row.action);
            int index = cell.gamepad ? cell.row.gamepad : cell.row.keyboard;
            string oldPath = action.bindings[index].effectivePath;

            operation = action.PerformInteractiveRebinding(index)
                .WithControlsHavingToMatchPath(cell.gamepad ? "<Gamepad>" : "<Keyboard>")
                .WithExpectedControlType("Button")
                .WithControlsExcluding("<Keyboard>/escape")
                .WithControlsExcluding("<Keyboard>/anyKey")
                .WithControlsExcluding("<Gamepad>/start")
                .WithControlsExcluding("<Gamepad>/leftStick/*")
                .WithControlsExcluding("<Gamepad>/rightStick/*")
                .WithControlsExcluding("<Gamepad>/dpad/*")
                .OnMatchWaitForAnother(0.05f)
                .OnComplete(op => Finish(cell, oldPath, true))
                .OnCancel(op => Finish(cell, oldPath, false));
            for (int i = 1; i <= 5; i++) operation.WithControlsExcluding("<Keyboard>/" + i);   // 1-5: medicamento directo
            listenUntil = Time.unscaledTime + 8f;
            operation.Start();
        }

        private void Finish(Cell cell, string oldPath, bool done)
        {
            InputAction action = PlayerControls.Action(cell.row.action);
            int index = cell.gamepad ? cell.row.gamepad : cell.row.keyboard;
            string newPath = action.bindings[index].effectivePath;
            string message = "";
            if (done && newPath != oldPath)
            {
                // Si otra acción ya usaba esa tecla, se queda con la vieja (se intercambian).
                foreach (PlayerControls.Row other in PlayerControls.Rows)
                {
                    int otherIndex = cell.gamepad ? other.gamepad : other.keyboard;
                    if (otherIndex < 0 || (other.action == cell.row.action && otherIndex == index)) continue;
                    InputAction a = PlayerControls.Action(other.action);
                    if (a.bindings[otherIndex].effectivePath != newPath) continue;
                    a.ApplyBindingOverride(otherIndex, oldPath);
                    message = $"  ({other.label} ahora usa {PlayerControls.DisplayName(oldPath)})";
                }
                message = $"{cell.row.label}: {PlayerControls.DisplayName(newPath)}" + message;
            }
            else if (!done) message = "Cancelado.";

            StopListening(true);
            if (done) PlayerControls.Save();
            Refresh();
            status.text = message;
        }

        private void StopListening(bool reselect)
        {
            if (operation != null)
            {
                InputActionRebindingExtensions.RebindingOperation op = operation;
                operation = null;
                op.Dispose();
            }
            if (!listening) return;
            listening = false;
            if (mapWasEnabled) PlayerControls.Map.Enable();
            mapWasEnabled = false;
            Button b = listeningButton;
            listeningButton = null;
            waitRelease = true;   // la navegación vuelve cuando sueltes la tecla que acabas de poner
            if (reselect && b != null) UIFactory.Select(b);
        }

        private static bool AnyPressed()
        {
            Keyboard k = Keyboard.current;
            if (k != null && k.anyKey.isPressed) return true;
            Mouse m = Mouse.current;
            if (m != null && m.leftButton.isPressed) return true;
            Gamepad g = Gamepad.current;
            if (g == null) return false;
            return g.buttonSouth.isPressed || g.buttonEast.isPressed || g.buttonWest.isPressed || g.buttonNorth.isPressed
                || g.leftShoulder.isPressed || g.rightShoulder.isPressed || g.leftTrigger.isPressed || g.rightTrigger.isPressed
                || g.startButton.isPressed || g.selectButton.isPressed || g.leftStickButton.isPressed || g.rightStickButton.isPressed;
        }

        private void Update()
        {
            if (listening)
            {
                if (operation != null)
                {
                    bool escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
                    bool start = Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;
                    if (escape || start || Time.unscaledTime > listenUntil) operation.Cancel();
                }
                return;
            }
            if (waitRelease)
            {
                if (AnyPressed()) return;
                waitRelease = false;
                if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = true;
                return;
            }
            if (Time.frameCount <= openedFrame) return;
            bool back = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                || (Gamepad.current != null && (Gamepad.current.buttonEast.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame));
            if (back) Close();
        }
    }
}
