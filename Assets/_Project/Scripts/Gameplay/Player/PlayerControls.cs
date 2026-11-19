using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HealingKnight.Gameplay.Player
{
    /// <summary>
    /// Todos los controles del juego en un solo lugar (estilo Cuphead). Los usa el jugador (PlayerInputReader) y
    /// el menú de Controles, que puede cambiar cualquier tecla o botón. Los cambios se guardan en PlayerPrefs.
    ///
    ///   Acción                  Cuphead (teclado)   Clásico (teclado)   Control (Xbox)
    ///   Mover / apuntar         Flechas             W A S D             Stick izq. / cruceta
    ///   Saltar (x2, planear)    Z                   Espacio             A
    ///   Disparar (mantener)     X                   J                   X
    ///   Fijar y apuntar         C                   U                   RB
    ///   Dash                    Shift izq.          Shift izq.          Y
    ///   Bomba (EX)              V                   L                   B
    ///   Escudo (mantener)       A                   K                   LT
    ///   Cambiar medicamento     Tab                 E                   LB
    ///   Medicamento anterior    Q                   Q                   Select
    ///   Herramienta (hilo)      S                   R                   RT
    ///   Pausa                   Esc                 Esc                 Start
    /// Siempre: 1 a 5 = medicamento directo; clic izq. disparar, clic der. escudo, clic central herramienta, rueda
    /// cambia de medicamento; stick derecho = apuntar libre. Apuntar con el mouse solo si lo activas en Controles.
    /// </summary>
    public static class PlayerControls
    {
        public const string Move = "Move", Jump = "Jump", Shoot = "Shoot", Lock = "Lock", Dash = "Dash", Bomb = "Bomb",
            Shield = "Shield", NextMed = "NextMedication", PrevMed = "PrevMedication", Gadget = "Gadget", Pause = "Pause";

        /// <summary>Índice de cada binding dentro de su acción (fijo, así el menú sabe cuál cambiar).</summary>
        public const int KeyboardIndex = 0, GamepadIndex = 1, MouseIndex = 2;
        public const int MoveUp = 1, MoveDown = 2, MoveLeft = 3, MoveRight = 4;

        /// <summary>Una fila del menú de Controles.</summary>
        public class Row
        {
            public string label, action, token;
            public int keyboard = KeyboardIndex, gamepad = GamepadIndex;   // -1 = no se cambia en esa columna
            public string fixedGamepad;                                     // texto si el control no se cambia
            public string cuphead, classic, pad;                            // valores de fábrica
        }

        public static readonly Row[] Rows =
        {
            new Row { label = "Arriba (apuntar)", action = Move, token = "arriba", keyboard = MoveUp, gamepad = -1, fixedGamepad = "Stick / cruceta", cuphead = "<Keyboard>/upArrow", classic = "<Keyboard>/w" },
            new Row { label = "Abajo (encogerse)", action = Move, token = "abajo", keyboard = MoveDown, gamepad = -1, fixedGamepad = "Stick / cruceta", cuphead = "<Keyboard>/downArrow", classic = "<Keyboard>/s" },
            new Row { label = "Izquierda", action = Move, token = "izquierda", keyboard = MoveLeft, gamepad = -1, fixedGamepad = "Stick / cruceta", cuphead = "<Keyboard>/leftArrow", classic = "<Keyboard>/a" },
            new Row { label = "Derecha", action = Move, token = "derecha", keyboard = MoveRight, gamepad = -1, fixedGamepad = "Stick / cruceta", cuphead = "<Keyboard>/rightArrow", classic = "<Keyboard>/d" },
            new Row { label = "Saltar", action = Jump, token = "saltar", cuphead = "<Keyboard>/z", classic = "<Keyboard>/space", pad = "<Gamepad>/buttonSouth" },
            new Row { label = "Disparar", action = Shoot, token = "disparar", cuphead = "<Keyboard>/x", classic = "<Keyboard>/j", pad = "<Gamepad>/buttonWest" },
            new Row { label = "Fijar y apuntar", action = Lock, token = "fijar", cuphead = "<Keyboard>/c", classic = "<Keyboard>/u", pad = "<Gamepad>/rightShoulder" },
            new Row { label = "Dash", action = Dash, token = "dash", cuphead = "<Keyboard>/leftShift", classic = "<Keyboard>/leftShift", pad = "<Gamepad>/buttonNorth" },
            new Row { label = "Bomba (EX)", action = Bomb, token = "bomba", cuphead = "<Keyboard>/v", classic = "<Keyboard>/l", pad = "<Gamepad>/buttonEast" },
            new Row { label = "Escudo", action = Shield, token = "escudo", cuphead = "<Keyboard>/a", classic = "<Keyboard>/k", pad = "<Gamepad>/leftTrigger" },
            new Row { label = "Cambiar medicamento", action = NextMed, token = "medicamento", cuphead = "<Keyboard>/tab", classic = "<Keyboard>/e", pad = "<Gamepad>/leftShoulder" },
            new Row { label = "Medicamento anterior", action = PrevMed, token = "medicamento_anterior", cuphead = "<Keyboard>/q", classic = "<Keyboard>/q", pad = "<Gamepad>/select" },
            new Row { label = "Herramienta (hilo dental)", action = Gadget, token = "herramienta", cuphead = "<Keyboard>/s", classic = "<Keyboard>/r", pad = "<Gamepad>/rightTrigger" },
        };

        private const string BindingsKey = "hk.controles";
        private const string MouseKey = "hk.apuntarConMouse";
        private const string PresetKey = "hk.esquema";

        private static InputActionMap map;
        private static IDisposable buttonWatch;
        private static bool? mouseAim;

        /// <summary>Se llama cuando cambia una tecla, el esquema o el apuntado con mouse (para refrescar textos).</summary>
        public static event Action Changed;

        /// <summary>true si lo último que se usó fue un control (los textos de ayuda muestran botones del control).</summary>
        public static bool UsingGamepad
        {
            get => usingGamepad;
            set
            {
                if (usingGamepad == value) return;
                usingGamepad = value;
                Changed?.Invoke();
            }
        }
        private static bool usingGamepad;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            buttonWatch?.Dispose();
            buttonWatch = null;
            map?.Dispose();
            map = null;
            mouseAim = null;
            Changed = null;
            usingGamepad = false;
        }

        public static InputActionMap Map
        {
            get
            {
                if (map == null) Build();
                return map;
            }
        }

        public static InputAction Action(string name) => Map.FindAction(name, true);

        /// <summary>Apuntar con el mouse (apagado = como Cuphead, solo 8 direcciones y stick derecho).</summary>
        public static bool MouseAim
        {
            get
            {
                if (mouseAim == null) mouseAim = PlayerPrefs.GetInt(MouseKey, 0) == 1;
                return mouseAim.Value;
            }
            set
            {
                mouseAim = value;
                PlayerPrefs.SetInt(MouseKey, value ? 1 : 0);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        /// <summary>Esquema de teclado elegido la última vez ("cuphead" o "clasico"; "personal" si cambiaste teclas a mano).</summary>
        public static string Preset => PlayerPrefs.GetString(PresetKey, "cuphead");

        // ---------- Construcción ----------

        private static void Build()
        {
            map = new InputActionMap("Gameplay");

            InputAction move = map.AddAction(Move, InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", Rows[0].cuphead).With("Down", Rows[1].cuphead)
                .With("Left", Rows[2].cuphead).With("Right", Rows[3].cuphead);
            move.AddBinding("<Gamepad>/leftStick");
            move.AddBinding("<Gamepad>/dpad");

            Button(Jump, Rows[4], null);
            Button(Shoot, Rows[5], "<Mouse>/leftButton");
            Button(Lock, Rows[6], null);
            Button(Dash, Rows[7], null);
            Button(Bomb, Rows[8], null);
            Button(Shield, Rows[9], "<Mouse>/rightButton");
            Button(NextMed, Rows[10], null);
            Button(PrevMed, Rows[11], null);
            Button(Gadget, Rows[12], "<Mouse>/middleButton");

            InputAction pause = map.AddAction(Pause, InputActionType.Button);
            pause.AddBinding("<Keyboard>/escape");
            pause.AddBinding("<Gamepad>/start");

            map.AddAction("Pointer", InputActionType.Value, "<Mouse>/position");
            map.AddAction("PointerDelta", InputActionType.Value, "<Mouse>/delta");
            map.AddAction("AimStick", InputActionType.Value, "<Gamepad>/rightStick");
            map.AddAction("GamepadMove", InputActionType.Value, "<Gamepad>/leftStick");
            map.AddAction("Scroll", InputActionType.Value, "<Mouse>/scroll");
            for (int i = 0; i < 5; i++)
                map.AddAction("Medication" + (i + 1), InputActionType.Button, "<Keyboard>/" + (i + 1));

            string saved = PlayerPrefs.GetString(BindingsKey, "");
            if (!string.IsNullOrEmpty(saved))
            {
                try { map.LoadBindingOverridesFromJson(saved); }
                catch (Exception e) { Debug.LogWarning("[Controles] No se pudieron leer los controles guardados: " + e.Message); }
            }

            // Para que los textos de ayuda muestren teclas o botones según lo último que tocaste.
            if (buttonWatch == null) buttonWatch = InputSystem.onAnyButtonPress.Subscribe(new DeviceWatcher());
        }

        private static void Button(string name, Row row, string mouse)
        {
            InputAction a = map.AddAction(name, InputActionType.Button);
            a.AddBinding(row.cuphead);                              // 0: teclado
            a.AddBinding(row.pad);                                  // 1: control
            if (mouse != null) a.AddBinding(mouse);                 // 2: mouse (fijo)
        }

        private class DeviceWatcher : IObserver<InputControl>
        {
            public void OnNext(InputControl control)
            {
                if (control.device is Gamepad) UsingGamepad = true;
                else if (control.device is Keyboard) UsingGamepad = false;
            }
            public void OnError(Exception error) { }
            public void OnCompleted() { }
        }

        // ---------- Guardar / esquemas ----------

        public static void Save(bool custom = true)
        {
            PlayerPrefs.SetString(BindingsKey, Map.SaveBindingOverridesAsJson());
            if (custom) PlayerPrefs.SetString(PresetKey, "personal");
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>Esquema de teclado: "cuphead" (flechas + Z X C V) o "clasico" (W A S D + J K L). El control vuelve a lo de fábrica.</summary>
        public static void ApplyPreset(string preset)
        {
            Map.RemoveAllBindingOverrides();
            if (preset == "clasico")
            {
                foreach (Row r in Rows)
                    Action(r.action).ApplyBindingOverride(r.keyboard, r.classic);
            }
            PlayerPrefs.SetString(PresetKey, preset);
            Save(false);
        }

        public static void ResetAll() => ApplyPreset("cuphead");

        // ---------- Nombres para mostrar ----------

        public static string BindingPath(Row row, bool gamepad)
        {
            int index = gamepad ? row.gamepad : row.keyboard;
            if (index < 0) return null;
            InputAction a = Action(row.action);
            return index < a.bindings.Count ? a.bindings[index].effectivePath : null;
        }

        /// <summary>Nombre corto en español de una tecla o botón ("Z", "Shift izq.", "Flecha arriba", "RB"...).</summary>
        public static string DisplayName(string path)
        {
            if (string.IsNullOrEmpty(path)) return "—";
            int slash = path.LastIndexOf('/');
            string control = slash >= 0 ? path.Substring(slash + 1) : path;
            bool pad = path.StartsWith("<Gamepad>") || path.StartsWith("<XInputController>") || path.StartsWith("<DualShockGamepad>");
            if (pad)
            {
                switch (control)
                {
                    case "buttonSouth": return "A";
                    case "buttonEast": return "B";
                    case "buttonWest": return "X";
                    case "buttonNorth": return "Y";
                    case "leftShoulder": return "LB";
                    case "rightShoulder": return "RB";
                    case "leftTrigger": return "LT";
                    case "rightTrigger": return "RT";
                    case "leftStickPress": return "L3";
                    case "rightStickPress": return "R3";
                    case "select": return "Select";
                    case "start": return "Start";
                    case "up": return "Cruceta arriba";
                    case "down": return "Cruceta abajo";
                    case "left": return "Cruceta izq.";
                    case "right": return "Cruceta der.";
                }
                return control;
            }
            switch (control)
            {
                case "upArrow": return "Flecha arriba";
                case "downArrow": return "Flecha abajo";
                case "leftArrow": return "Flecha izq.";
                case "rightArrow": return "Flecha der.";
                case "space": return "Espacio";
                case "leftShift": return "Shift izq.";
                case "rightShift": return "Shift der.";
                case "leftCtrl": return "Ctrl izq.";
                case "rightCtrl": return "Ctrl der.";
                case "leftAlt": return "Alt izq.";
                case "rightAlt": return "Alt der.";
                case "tab": return "Tab";
                case "enter": return "Enter";
                case "numpadEnter": return "Enter (num.)";
                case "backspace": return "Retroceso";
                case "capsLock": return "Bloq Mayús";
                case "escape": return "Esc";
                case "leftButton": return "Clic izq.";
                case "rightButton": return "Clic der.";
                case "middleButton": return "Clic central";
            }
            // Letras y símbolos: el nombre que tiene la tecla en TU teclado (así sale la Ñ en un teclado en español).
            InputControl c = InputSystem.FindControl(path);
            string name = c != null && !string.IsNullOrEmpty(c.displayName) ? c.displayName : control;
            return name.Length == 1 ? name.ToUpperInvariant() : name;
        }

        public static string DisplayName(Row row, bool gamepad)
        {
            if (gamepad && row.gamepad < 0) return row.fixedGamepad;
            return DisplayName(BindingPath(row, gamepad));
        }

        public static Row FindRow(string token)
        {
            foreach (Row r in Rows) if (r.token == token) return r;
            return null;
        }

        /// <summary>Texto de una acción para la ayuda, con el dispositivo que estás usando ahora.</summary>
        public static string Name(string token)
        {
            bool pad = UsingGamepad;
            switch (token)
            {
                case "mover":
                    if (pad) return "Stick izq.";
                    string u = BindingPath(Rows[0], false), d = BindingPath(Rows[1], false), l = BindingPath(Rows[2], false), r = BindingPath(Rows[3], false);
                    if (u == "<Keyboard>/upArrow" && d == "<Keyboard>/downArrow" && l == "<Keyboard>/leftArrow" && r == "<Keyboard>/rightArrow") return "Flechas";
                    return $"{DisplayName(l)}/{DisplayName(r)}";
                case "apuntar":
                    return pad ? "Stick izq." : (MouseAim ? "el mouse" : Name("arriba"));
                case "pausa":
                    return pad ? "Start" : "Esc";
            }
            Row row = FindRow(token);
            return row != null ? DisplayName(row, pad) : token;
        }

        /// <summary>
        /// Cambia las marcas {saltar}, {disparar}, {fijar}, {dash}, {bomba}, {escudo}, {medicamento}, {herramienta},
        /// {mover}, {arriba}, {abajo}, {izquierda}, {derecha} y {pausa} por la tecla o el botón que tengas puesto.
        /// Así los mensajes de NODO siempre dicen el control correcto aunque lo cambies en el menú.
        /// </summary>
        public static string Format(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text;
            var sb = new System.Text.StringBuilder(text.Length + 16);
            int i = 0;
            while (i < text.Length)
            {
                int open = text.IndexOf('{', i);
                if (open < 0) { sb.Append(text, i, text.Length - i); break; }
                int close = text.IndexOf('}', open + 1);
                if (close < 0) { sb.Append(text, i, text.Length - i); break; }
                sb.Append(text, i, open - i);
                string token = text.Substring(open + 1, close - open - 1);
                bool known = token == "mover" || token == "apuntar" || token == "pausa" || FindRow(token) != null;
                sb.Append(known ? Name(token) : text.Substring(open, close - open + 1));
                i = close + 1;
            }
            return sb.ToString();
        }

        /// <summary>Línea de ayuda con todos los controles (la usa el HUD al empezar un nivel).</summary>
        public static string HelpLine()
        {
            string aim = UsingGamepad ? "Stick der.: apuntar libre" : (MouseAim ? "Mouse: apuntar" : null);
            var parts = new List<string>
            {
                $"Mover {Name("mover")}",
                $"Saltar {Name("saltar")} (x2, mantener = planear)",
                $"Disparar {Name("disparar")}",
                $"Fijar {Name("fijar")}",
                $"Dash {Name("dash")}",
                $"Bomba {Name("bomba")}",
                $"Escudo {Name("escudo")}",
                $"Medicamento {Name("medicamento")}" + (UsingGamepad ? "" : " o 1-5"),
                $"Pausa {Name("pausa")}"
            };
            if (aim != null) parts.Insert(4, aim);
            return string.Join("  ·  ", parts);
        }
    }
}
