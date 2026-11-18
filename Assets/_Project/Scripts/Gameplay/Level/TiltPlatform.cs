using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// CEREBELO (el centro del equilibrio): plataformas tipo SUBIBAJA. Se ladean hacia el lado donde estás parado
    /// y, mientras más ladeadas, más te resbalas hacia esa orilla. Quédate cerca del centro o cruza rápido.
    /// Sin jugador encima regresan derechas.
    /// </summary>
    public class TiltPlatform : MonoBehaviour
    {
        [SerializeField] private float width = 4f;
        [SerializeField] private float maxAngle = 14f;
        [SerializeField] private float slide = 4.5f;
        [SerializeField] private Color fill = new Color(0.45f, 0.75f, 0.55f, 1f);
        [SerializeField] private Color outline = new Color(0.06f, 0.15f, 0.1f, 1f);

        private float angle;

        public void Configure(float platformWidth, float tilt) { width = platformWidth; maxAngle = tilt; }

        private void Start()
        {
            // El pivote (el punto de apoyo) queda abajo, en un "tallo" de adorno.
            var board = new GameObject("Tabla");
            board.transform.SetParent(transform, false);
            CodeSprites.OneWay(board, width);
            Sprite c = CodeSprites.Circle, sq = CodeSprites.Square;
            CodeSprites.Add(board.transform, sq, outline, -11, new Vector2(0f, -0.2f), new Vector2(width + 0.14f, 0.5f));
            CodeSprites.Add(board.transform, sq, fill, -10, new Vector2(0f, -0.2f), new Vector2(width, 0.36f));
            for (int i = -1; i <= 1; i += 2)
                CodeSprites.Add(board.transform, c, fill, -10, new Vector2(i * width * 0.5f, -0.2f), new Vector2(0.5f, 0.5f));
            CodeSprites.Add(board.transform, sq, new Color(1f, 1f, 1f, 0.35f), -9, new Vector2(0f, -0.08f), new Vector2(width * 0.9f, 0.07f));
            CodeSprites.Add(transform, c, outline, -12, new Vector2(0f, -0.55f), new Vector2(0.9f, 0.9f));
            CodeSprites.Add(transform, c, new Color(0.9f, 0.85f, 0.5f, 1f), -11, new Vector2(0f, -0.55f), new Vector2(0.7f, 0.7f));
        }


        private void FixedUpdate()
        {
            Transform board = transform.Find("Tabla");
            if (board == null) return;
            float dt = Time.fixedDeltaTime;
            PlayerController player = PlayerController.Current;
            Vector2 pivot = transform.position;

            // Altura de la tabla justo debajo del jugador (para saber si está encima).
            bool on = false;
            float offset = 0f;
            if (player != null && player.Motor != null && player.Health.IsAlive)
            {
                offset = player.Position.x - pivot.x;
                float surface = pivot.y + Mathf.Tan(angle * Mathf.Deg2Rad) * offset;
                on = Mathf.Abs(offset) <= width * 0.5f + 0.1f && Mathf.Abs(player.Position.y - surface) < 0.3f && player.Motor.Velocity.y <= 0.5f;
            }

            float target = on ? -Mathf.Clamp(offset / (width * 0.5f), -1f, 1f) * maxAngle : 0f;
            angle = Mathf.MoveTowards(angle, target, (on ? 30f : 20f) * dt);
            board.localRotation = Quaternion.Euler(0f, 0f, angle);

            // Te resbalas hacia el lado bajo.
            if (on) player.Motor.Body.position += new Vector2(-Mathf.Sin(angle * Mathf.Deg2Rad) * slide * dt * 3f, 0f);
        }
    }
}
