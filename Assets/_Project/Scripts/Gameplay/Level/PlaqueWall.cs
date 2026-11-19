using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Movement;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// PLACA ENTRE DIENTES (Boca): un tapón de placa bacteriana pegado entre dos dientes que cierra el paso.
    /// Las balas no lo quitan; engánchale el HILO DENTAL y mantente colgado para arrancarlo de un jalón.
    /// </summary>
    public class PlaqueWall : MonoBehaviour
    {
        [SerializeField] private float height = 6f;
        [SerializeField] private float width = 1.4f;
        [SerializeField] private Sprite art;
        [SerializeField, TextArea] private string message = "Placa pegada entre los dientes: las balas no la quitan. ¡Engánchale el HILO DENTAL y jala!";

        private bool said;

        public void Configure(float h, Sprite drawing) { height = h; art = drawing; }

        private void Start()
        {
            gameObject.layer = PhysicsLayers.Ground;
            var box = gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(width, height);
            box.offset = new Vector2(0f, height * 0.5f);

            if (art != null)
            {
                // Que el dibujo ocupe toda la altura del tapón (un poco más angosto para que no tape de más).
                float k = art.bounds.size.y > 0.01f ? height / art.bounds.size.y : 1f;
                CodeSprites.Add(transform, art, Color.white, 4, new Vector2(0f, height * 0.5f), new Vector2(k * 0.7f, k));
            }
            else
            {
                Color line = new Color(0.35f, 0.28f, 0.08f), fill = new Color(0.92f, 0.86f, 0.5f);
                Sprite c = CodeSprites.Circle;
                for (float y = 0.5f; y < height; y += 0.9f)
                {
                    float x = Mathf.Sin(y * 2.3f) * 0.15f;
                    CodeSprites.Add(transform, c, line, 3, new Vector2(x, y), new Vector2(width + 0.35f, 1.15f));
                    CodeSprites.Add(transform, c, fill, 4, new Vector2(x, y), new Vector2(width + 0.2f, 1f));
                }
                // Caritas de S. mutans atrapadas en la placa.
                for (int i = 0; i < 3; i++)
                {
                    float y = 1.2f + i * height * 0.28f;
                    CodeSprites.Add(transform, c, new Color(0.95f, 0.8f, 0.35f), 5, new Vector2(0f, y), new Vector2(0.55f, 0.55f));
                    CodeSprites.Add(transform, c, line, 6, new Vector2(-0.1f, y + 0.05f), new Vector2(0.08f, 0.1f));
                    CodeSprites.Add(transform, c, line, 6, new Vector2(0.1f, y + 0.05f), new Vector2(0.08f, 0.1f));
                }
            }

            var hookGo = new GameObject("GanchoPlaca");
            hookGo.transform.SetParent(transform, false);
            hookGo.transform.localPosition = new Vector3(0f, Mathf.Min(height - 0.6f, 4.2f), 0f);
            var hook = hookGo.AddComponent<FlossHook>();
            hook.Configure(0.8f, true, 3.5f);
            hook.Ripped += _ =>
            {
                Sfx.EffectAt("Placa_Rompe", transform.position, 0.8f);
                GameplayEvents.RaiseMessage("¡Placa arrancada! Así limpia el hilo dental.");
                Destroy(gameObject);
            };
        }

        private void Update()
        {
            if (said) return;
            var p = Player.PlayerController.Current;
            if (p != null && Mathf.Abs(p.Position.x - transform.position.x) < 7f) { said = true; GameplayEvents.RaiseMessage(message); }
        }
    }
}
