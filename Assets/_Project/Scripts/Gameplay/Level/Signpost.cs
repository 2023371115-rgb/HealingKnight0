using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using TMPro;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// LETRERO del nivel. Se ve desde lejos (avisa lo que viene) y, cuando llegas, NODO te explica la mecánica.
    ///  - Bienvenida: nombre del órgano al empezar.   - Info: una mecánica nueva (NODO la explica al llegar).
    ///  - Peligro: lago, hueco, jefe...              - Flecha: a dónde ir (derecha, arriba o abajo).
    ///  - Secreto: un "?" chiquito cerca de un pasaje escondido.   - Salida: el final del nivel.
    /// El dibujo depende del sistema (Art/Letreros/Digestivo, Respiratorio, Nervioso); si falta, se arma con figuras.
    /// La palabra del letrero la escribe el juego encima del dibujo. Se tambalea un poquito cuando pasas.
    /// </summary>
    public class Signpost : MonoBehaviour
    {
        public enum Kind { Welcome, Info, Warning, ArrowRight, ArrowUp, ArrowDown, Secret, Exit }

        [SerializeField] private Kind kind = Kind.Info;
        [SerializeField] private string label;
        [Tooltip("Lo que dice NODO al llegar (vacío = nada). Acepta {saltar}, {fijar}, etc.")]
        [SerializeField, TextArea] private string message;
        [Tooltip("La parte que se tambalea (letrero completo menos nada: gira desde la base).")]
        [SerializeField] private Transform board;
        [SerializeField] private Vector2 labelCenter = new Vector2(0f, 1.55f);
        [SerializeField] private Vector2 labelSize = new Vector2(1.6f, 0.6f);
        [SerializeField] private Color labelColor = new Color(0.25f, 0.07f, 0.12f);
        [SerializeField] private Font font;
        [SerializeField] private float triggerDistance = 2.6f;
        [Tooltip("Al empezar se baja (o sube) hasta el piso que tenga justo debajo.")]
        [SerializeField] private bool snapToGround = true;
        [SerializeField] private int sortingOrder = -30;

        private TextMeshPro text;
        private bool near, said;
        private float wiggle, phase;

        public Kind SignKind => kind;
        public string Label => label;

        public void Configure(Kind kind, string label, string message, Transform board, Vector2 labelCenter, Vector2 labelSize,
            Color labelColor, Font font, float triggerDistance, int sortingOrder)
        {
            this.kind = kind;
            this.label = label;
            this.message = message;
            this.board = board;
            this.labelCenter = labelCenter;
            this.labelSize = labelSize;
            this.labelColor = labelColor;
            this.font = font;
            this.triggerDistance = triggerDistance;
            this.sortingOrder = sortingOrder;
        }

        private void Awake()
        {
            if (board == null) board = transform;
            if (snapToGround) Snap();
            phase = Random.value * 10f;
            if (!string.IsNullOrEmpty(label))
                text = WorldText.Create(board, label, labelCenter, labelSize, labelColor, sortingOrder + 6, font, labelSize.y * 9f);
        }

        private void Snap()
        {
            Vector2 from = (Vector2)transform.position + Vector2.up * 1.2f;
            RaycastHit2D hit = Physics2D.Raycast(from, Vector2.down, 3f, PhysicsLayers.GroundMask);
            if (hit.collider != null && hit.distance > 0.02f)
                transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            PlayerController p = PlayerController.Current;
            if (p != null)
            {
                Vector2 d = p.Position - (Vector2)transform.position;
                bool close = Mathf.Abs(d.x) < triggerDistance && d.y > -2.5f && d.y < 5f;
                if (close && !near) wiggle = 1f;   // ¡tin! se tambalea al llegar
                near = close;

                if (!said && !string.IsNullOrEmpty(message) && (close || (d.x > 0.3f && Mathf.Abs(d.y) < 9f)))
                {
                    said = true;
                    GameplayEvents.RaiseMessage(message);
                }
            }

            wiggle = Mathf.MoveTowards(wiggle, 0f, dt * 1.4f);
            float sway = Mathf.Sin(Time.time * 1.3f + phase) * 0.7f;            // respira con el nivel
            float shake = Mathf.Sin(Time.time * 17f) * 7f * wiggle * wiggle;   // rebote al pasar
            board.localRotation = Quaternion.Euler(0f, 0f, sway + shake);
            float s = 1f + 0.06f * wiggle;
            board.localScale = new Vector3(s, s, 1f);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = kind == Kind.Warning ? new Color(1f, 0.4f, 0.2f) : new Color(1f, 0.85f, 0.3f);
            Gizmos.DrawWireCube(transform.position + Vector3.up, new Vector3(1.4f, 2f, 0f));
        }
    }
}
