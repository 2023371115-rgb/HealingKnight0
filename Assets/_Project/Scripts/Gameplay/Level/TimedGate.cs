using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// COMPUERTA que se abre y se cierra sola, a ritmo (la EPIGLOTIS: la tapita que cierra la tráquea
    /// cada vez que tragas, para que la comida no se vaya a los pulmones).
    /// Cerrada es una pared; abierta se puede pasar. Medio segundo antes de cerrarse tiembla (aviso).
    /// Si se cierra contigo debajo, te avienta hacia atrás.
    /// </summary>
    public class TimedGate : MonoBehaviour
    {
        [SerializeField] private float openTime = 2.4f;
        [SerializeField] private float closedTime = 2.2f;
        [SerializeField] private float offset;
        [SerializeField] private float height = 8f;
        [SerializeField] private float thickness = 0.9f;
        [SerializeField] private Sprite art;
        [SerializeField] private Color fill = new Color(0.92f, 0.55f, 0.68f, 1f);
        [SerializeField] private Color outline = new Color(0.3f, 0.08f, 0.2f, 1f);

        private BoxCollider2D wall;
        private Transform flap;
        private float time;
        private bool open;

        public void Configure(float opened, float closed, float phase, float gateHeight, Sprite drawing)
        {
            openTime = opened;
            closedTime = closed;
            offset = phase;
            height = gateHeight;
            art = drawing;
        }

        private float Period => openTime + closedTime;

        private void Start()
        {
            time = offset;
            gameObject.layer = PhysicsLayers.Ground;
            wall = gameObject.AddComponent<BoxCollider2D>();
            wall.size = new Vector2(thickness, height);
            wall.offset = new Vector2(0f, height * 0.5f);

            // La tapa cuelga desde arriba (su pivote es el techo) y se levanta al abrirse.
            flap = new GameObject("Epiglotis").transform;
            flap.SetParent(transform, false);
            flap.localPosition = new Vector3(0f, height, 0f);
            if (art != null) CodeSprites.Art(flap, art, thickness * 2.2f, 4, new Vector2(0f, -height * 0.5f));
            else
            {
                Sprite c = CodeSprites.Circle, sq = CodeSprites.Square;
                CodeSprites.Add(flap, sq, outline, 3, new Vector2(0f, -height * 0.5f), new Vector2(thickness + 0.16f, height));
                CodeSprites.Add(flap, sq, fill, 4, new Vector2(0f, -height * 0.5f + 0.08f), new Vector2(thickness, height - 0.16f));
                CodeSprites.Add(flap, c, outline, 3, new Vector2(0f, -height), new Vector2(thickness + 0.5f, 0.9f));
                CodeSprites.Add(flap, c, fill, 4, new Vector2(0f, -height), new Vector2(thickness + 0.34f, 0.74f));
                for (int i = 0; i < 4; i++)
                    CodeSprites.Add(flap, c, new Color(1f, 0.8f, 0.86f, 0.6f), 5, new Vector2(-thickness * 0.2f, -height * (0.2f + i * 0.2f)), new Vector2(0.18f, 0.5f));
            }
        }

        private void Update()
        {
            time += Time.deltaTime;
            float t = Mathf.Repeat(time, Period);
            bool nowOpen = t < openTime;
            float lift;
            if (nowOpen)
            {
                float k = Mathf.Clamp01(t / 0.3f) * Mathf.Clamp01((openTime - t) / 0.3f);
                lift = Mathf.SmoothStep(0f, 1f, k);
            }
            else lift = 0f;

            // Aviso: tiembla antes de cerrarse.
            float shake = nowOpen && openTime - t < 0.6f ? Mathf.Sin(time * 50f) * 0.06f : 0f;
            flap.localPosition = new Vector3(shake, height, 0f);
            flap.localScale = new Vector3(1f, Mathf.Lerp(1f, 0.12f, lift), 1f);

            if (nowOpen != open)
            {
                open = nowOpen;
                wall.enabled = !open;
                Sfx.EffectAt(open ? "Alveolo_Infla" : "Alveolo_Desinfla", transform.position, 0.6f);
                if (!open) PushOut();
            }
        }

        private void PushOut()
        {
            PlayerController player = PlayerController.Current;
            if (player == null || player.Motor == null) return;
            Vector2 f = player.Position;
            if (Mathf.Abs(f.x - transform.position.x) > thickness * 0.5f + 0.5f || f.y > transform.position.y + height) return;
            const float side = -1f;   // siempre hacia atrás (hacia la entrada del nivel)
            player.Motor.Teleport(new Vector2(transform.position.x + side * (thickness * 0.5f + 0.6f), f.y));
            player.OnHurt(side, 6f);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.7f, 0.7f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * height * 0.5f, new Vector3(thickness, height, 0f));
        }
    }
}
