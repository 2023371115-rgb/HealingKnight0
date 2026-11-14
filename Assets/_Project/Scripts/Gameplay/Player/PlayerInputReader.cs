using UnityEngine;
using UnityEngine.InputSystem;

namespace HealingKnight.Gameplay.Player
{
    /// <summary>
    /// Controles (teclado + mouse y gamepad). Las acciones se crean aquí en código
    /// para que se vean todos los controles en un solo lugar.
    ///
    ///   Acción                         Teclado / mouse              Gamepad
    ///   Mover                          A / D, flechas               Stick izq. / cruceta
    ///   Apuntar libre                  MOVER EL MOUSE               Stick der.
    ///   Apuntar en 8 direcciones*      W (+ A o D), S en el aire    Stick izq. (si no usas el der.)
    ///   Encogerse                      S                            Abajo
    ///   Disparar (mantener)            J / clic izquierdo           X / gatillo der.
    ///   Escudo burbuja (mantener)      K / clic derecho             Gatillo izq.
    ///   Saltar / doble salto           Espacio                      A / LB
    ///   Caída lenta                    mantener Espacio al caer     mantener A / LB
    ///   Bomba de medicamento           L / F                        B / RB
    ///   Fijar (quieto)                 U / Shift izq.               Clic del stick der.
    ///   Medicamento anterior/siguiente Q / E, rueda del mouse       — / Y
    ///   Medicamento directo            1 a 5                        —
    ///   Pausa                          Esc                          Start
    ///
    ///   * Si mueves el mouse apuntas con el mouse; si presionas W vuelves a apuntar con teclas.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlayerInputReader : MonoBehaviour
    {
        private InputActionMap map;
        private InputAction move, jump, shoot, aimLock, bomb, shield, prevMed, nextMed, pause;
        private InputAction pointer, pointerDelta, aimStick, gamepadMove, scroll;
        private bool mouseAim;
        private InputAction[] directMed;

        /// <summary>Dirección (incluye arriba/abajo para apuntar y encogerse).</summary>
        public Vector2 Move { get; private set; }
        public bool JumpHeld { get; private set; }
        /// <summary>Botón de fijar: quieto y apuntando en 8 direcciones.</summary>
        public bool LockHeld { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool ShootPressed { get; private set; }
        public bool ShootHeld { get; private set; }
        public bool BombPressed { get; private set; }
        public bool ShieldHeld { get; private set; }
        public int MedicationStep { get; private set; }   // -1, 0 o 1
        public int DirectMedicationIndex { get; private set; } = -1;
        public bool PausePressed { get; private set; }

        /// <summary>Cómo se está apuntando ahora mismo.</summary>
        public AimSource AimSource { get; private set; } = AimSource.Directional;
        /// <summary>Posición del mouse en pantalla (solo si AimSource = Mouse).</summary>
        public Vector2 PointerScreenPosition { get; private set; }
        /// <summary>Dirección del stick derecho (solo si AimSource = Stick).</summary>
        public Vector2 AimStick { get; private set; }

        /// <summary>Para cinemáticas, pausa o muerte: el personaje ignora los controles.</summary>
        public bool Blocked { get; set; }

        private void Awake()
        {
            map = new InputActionMap("Gameplay");

            move = map.AddAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");
            move.AddBinding("<Gamepad>/dpad");

            shoot = map.AddAction("Shoot", InputActionType.Button);
            shoot.AddBinding("<Keyboard>/j");
            shoot.AddBinding("<Mouse>/leftButton");
            shoot.AddBinding("<Gamepad>/buttonWest");
            shoot.AddBinding("<Gamepad>/rightTrigger");

            aimLock = map.AddAction("Lock", InputActionType.Button);
            aimLock.AddBinding("<Keyboard>/u");
            aimLock.AddBinding("<Keyboard>/leftShift");
            aimLock.AddBinding("<Gamepad>/rightStickPress");

            jump = map.AddAction("Jump", InputActionType.Button);
            jump.AddBinding("<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");
            jump.AddBinding("<Gamepad>/leftShoulder");

            bomb = map.AddAction("Bomb", InputActionType.Button);
            bomb.AddBinding("<Keyboard>/l");
            bomb.AddBinding("<Keyboard>/f");
            bomb.AddBinding("<Gamepad>/buttonEast");
            bomb.AddBinding("<Gamepad>/rightShoulder");

            shield = map.AddAction("Shield", InputActionType.Button);
            shield.AddBinding("<Keyboard>/k");
            shield.AddBinding("<Mouse>/rightButton");
            shield.AddBinding("<Gamepad>/leftTrigger");

            prevMed = map.AddAction("PrevMedication", InputActionType.Button);
            prevMed.AddBinding("<Keyboard>/q");

            nextMed = map.AddAction("NextMedication", InputActionType.Button);
            nextMed.AddBinding("<Keyboard>/e");
            nextMed.AddBinding("<Gamepad>/buttonNorth");

            // Apuntado libre.
            pointer = map.AddAction("Pointer", InputActionType.Value, "<Mouse>/position");
            pointerDelta = map.AddAction("PointerDelta", InputActionType.Value, "<Mouse>/delta");
            aimStick = map.AddAction("AimStick", InputActionType.Value, "<Gamepad>/rightStick");
            gamepadMove = map.AddAction("GamepadMove", InputActionType.Value, "<Gamepad>/leftStick");
            scroll = map.AddAction("Scroll", InputActionType.Value, "<Mouse>/scroll");

            pause = map.AddAction("Pause", InputActionType.Button);
            pause.AddBinding("<Keyboard>/escape");
            pause.AddBinding("<Gamepad>/start");

            directMed = new InputAction[5];
            for (int i = 0; i < directMed.Length; i++)
            {
                directMed[i] = map.AddAction("Medication" + (i + 1), InputActionType.Button);
                directMed[i].AddBinding("<Keyboard>/" + (i + 1));
            }
        }

        private void UpdateAimSource()
        {
            // Mover el mouse activa el apuntado con mouse; presionar arriba (W) o usar el stick
            // izquierdo del control regresa al apuntado de 8 direcciones.
            if (pointerDelta.ReadValue<Vector2>().sqrMagnitude > 4f) mouseAim = true;
            else if (Move.y > 0.5f || gamepadMove.ReadValue<Vector2>().sqrMagnitude > 0.25f) mouseAim = false;

            Vector2 stick = aimStick.ReadValue<Vector2>();
            if (stick.sqrMagnitude > 0.3f)
            {
                mouseAim = false;
                AimSource = AimSource.Stick;
                AimStick = stick.normalized;
            }
            else if (mouseAim)
            {
                AimSource = AimSource.Mouse;
                PointerScreenPosition = pointer.ReadValue<Vector2>();
            }
            else AimSource = AimSource.Directional;
        }

        private void OnEnable() => map?.Enable();
        private void OnDisable() => map?.Disable();
        private void OnDestroy() => map?.Dispose();

        private void Update()
        {
            // La pausa se lee siempre, incluso con el personaje bloqueado.
            PausePressed = pause.WasPressedThisFrame();

            if (Blocked)
            {
                Move = Vector2.zero;
                JumpHeld = LockHeld = false;
                JumpPressed = ShootPressed = ShootHeld = BombPressed = ShieldHeld = false;
                MedicationStep = 0;
                DirectMedicationIndex = -1;
                return;
            }

            Vector2 m = move.ReadValue<Vector2>();
            Move = m.sqrMagnitude > 1f ? m.normalized : m;
            JumpPressed = jump.WasPressedThisFrame();
            JumpHeld = jump.IsPressed();
            LockHeld = aimLock.IsPressed();
            ShootPressed = shoot.WasPressedThisFrame();
            ShootHeld = shoot.IsPressed();
            BombPressed = bomb.WasPressedThisFrame();
            ShieldHeld = shield.IsPressed();
            UpdateAimSource();

            MedicationStep = 0;
            if (prevMed.WasPressedThisFrame()) MedicationStep = -1;
            else if (nextMed.WasPressedThisFrame()) MedicationStep = 1;
            else
            {
                // Rueda del mouse: abajo = siguiente, arriba = anterior.
                float wheel = scroll.ReadValue<Vector2>().y;
                if (wheel < -0.1f) MedicationStep = 1;
                else if (wheel > 0.1f) MedicationStep = -1;
            }

            DirectMedicationIndex = -1;
            for (int i = 0; i < directMed.Length; i++)
            {
                if (directMed[i].WasPressedThisFrame())
                {
                    DirectMedicationIndex = i;
                    break;
                }
            }
        }
    }

    public enum AimSource
    {
        /// <summary>8 direcciones con W/A/S/D o el stick izquierdo (como Metal Slug).</summary>
        Directional,
        /// <summary>Libre hacia el mouse.</summary>
        Mouse,
        /// <summary>Libre con el stick derecho del control.</summary>
        Stick
    }
}
