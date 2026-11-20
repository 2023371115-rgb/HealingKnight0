using UnityEngine;
using UnityEngine.InputSystem;

namespace HealingKnight.Gameplay.Player
{
    /// <summary>
    /// Lee los controles del jugador (estilo Cuphead). Las teclas y botones viven en PlayerControls, que también
    /// usa el menú de Controles para cambiarlos. La tabla completa está en PlayerControls.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlayerInputReader : MonoBehaviour
    {
        private InputActionMap map;
        private InputAction move, jump, shoot, aimLock, dash, bomb, shield, prevMed, nextMed, pause, gadget;
        private InputAction pointer, pointerDelta, aimStick, gamepadMove, scroll;
        private bool mouseAim;
        private InputAction[] directMed;

        /// <summary>Dirección (incluye arriba/abajo para apuntar y encogerse).</summary>
        public Vector2 Move { get; private set; }
        public bool JumpHeld { get; private set; }
        /// <summary>Botón de fijar (Cuphead): quieto y apuntando en 8 direcciones.</summary>
        public bool LockHeld { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool ShootPressed { get; private set; }
        public bool ShootHeld { get; private set; }
        /// <summary>Dash de Cuphead.</summary>
        public bool DashPressed { get; private set; }
        public bool BombPressed { get; private set; }
        public bool ShieldHeld { get; private set; }
        public int MedicationStep { get; private set; }   // -1, 0 o 1
        public int DirectMedicationIndex { get; private set; } = -1;
        public bool PausePressed { get; private set; }
        /// <summary>Botón de herramienta del nivel (hilo dental en la Boca).</summary>
        public bool GadgetPressed { get; private set; }
        public bool GadgetHeld { get; private set; }

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
            map = PlayerControls.Map;
            move = map.FindAction(PlayerControls.Move, true);
            jump = map.FindAction(PlayerControls.Jump, true);
            shoot = map.FindAction(PlayerControls.Shoot, true);
            aimLock = map.FindAction(PlayerControls.Lock, true);
            dash = map.FindAction(PlayerControls.Dash, true);
            bomb = map.FindAction(PlayerControls.Bomb, true);
            shield = map.FindAction(PlayerControls.Shield, true);
            prevMed = map.FindAction(PlayerControls.PrevMed, true);
            nextMed = map.FindAction(PlayerControls.NextMed, true);
            gadget = map.FindAction(PlayerControls.Gadget, true);
            pause = map.FindAction(PlayerControls.Pause, true);
            pointer = map.FindAction("Pointer", true);
            pointerDelta = map.FindAction("PointerDelta", true);
            aimStick = map.FindAction("AimStick", true);
            gamepadMove = map.FindAction("GamepadMove", true);
            scroll = map.FindAction("Scroll", true);
            directMed = new InputAction[5];
            for (int i = 0; i < directMed.Length; i++) directMed[i] = map.FindAction("Medication" + (i + 1), true);
        }

        private void UpdateAimSource()
        {
            // Con "Apuntar con el mouse" activado en Controles, mover el mouse apunta con él; presionar arriba o
            // usar el stick izquierdo del control regresa al apuntado de 8 direcciones (como Cuphead).
            if (!PlayerControls.MouseAim) mouseAim = false;
            else if (pointerDelta.ReadValue<Vector2>().sqrMagnitude > 4f) mouseAim = true;
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

        // El mapa es compartido (PlayerControls): no se destruye con el jugador.
        private void OnEnable() => map?.Enable();
        private void OnDisable() => map?.Disable();

        private void Update()
        {
            // La pausa se lee siempre, incluso con el personaje bloqueado.
            PausePressed = pause.WasPressedThisFrame();

            if (Blocked)
            {
                Move = Vector2.zero;
                JumpHeld = LockHeld = false;
                JumpPressed = ShootPressed = ShootHeld = BombPressed = ShieldHeld = DashPressed = false;
                GadgetPressed = GadgetHeld = false;
                MedicationStep = 0;
                DirectMedicationIndex = -1;
                return;
            }

            // Mientras el menú de Controles escucha una tecla nueva, el mapa está apagado.
            if (!map.enabled) { Move = Vector2.zero; JumpPressed = ShootPressed = BombPressed = DashPressed = GadgetPressed = false; return; }

            Vector2 m = move.ReadValue<Vector2>();
            if (gamepadMove.ReadValue<Vector2>().sqrMagnitude > 0.3f && !PlayerControls.UsingGamepad) PlayerControls.UsingGamepad = true;
            Move = m.sqrMagnitude > 1f ? m.normalized : m;
            JumpPressed = jump.WasPressedThisFrame();
            JumpHeld = jump.IsPressed();
            LockHeld = aimLock.IsPressed();
            ShootPressed = shoot.WasPressedThisFrame();
            ShootHeld = shoot.IsPressed();
            BombPressed = bomb.WasPressedThisFrame();
            DashPressed = dash.WasPressedThisFrame();
            ShieldHeld = shield.IsPressed();
            GadgetPressed = gadget.WasPressedThisFrame();
            GadgetHeld = gadget.IsPressed();
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
        /// <summary>8 direcciones con las flechas o el stick izquierdo (como Cuphead).</summary>
        Directional,
        /// <summary>Libre hacia el mouse.</summary>
        Mouse,
        /// <summary>Libre con el stick derecho del control.</summary>
        Stick
    }
}
