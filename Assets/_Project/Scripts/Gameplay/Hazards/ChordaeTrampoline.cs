using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Flight;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// CUERDAS TENDINOSAS: los hilos que sostienen las válvulas. Son elásticos: si caes encima te lanzan muy alto.
    /// </summary>
    public class ChordaeTrampoline : MonoBehaviour
    {
        [SerializeField] private float width = 2.4f;
        [SerializeField] private float launch = 17f;

        private Transform cords;
        private float bounce;

        public void Configure(float padWidth, float launchSpeed)
        {
            width = padWidth; launch = launchSpeed;
        }

        private void Start()
        {
            cords = new GameObject("Cuerdas").transform;
            cords.SetParent(transform, false);
            // Músculo papilar (el poste) y las cuerdas blancas en abanico.
            CodeSprites.Add(transform, CodeSprites.Circle, new Color(0.3f, 0.03f, 0.08f), 4, new Vector2(0f, -1.2f), new Vector2(1.5f, 2.6f), "PapilarContorno");
            CodeSprites.Add(transform, CodeSprites.Circle, new Color(0.75f, 0.16f, 0.22f), 5, new Vector2(0f, -1.2f), new Vector2(1.3f, 2.4f), "Papilar");
            for (int i = 0; i < 5; i++)
            {
                float x = -width * 0.5f + width * i / 4f;
                SpriteRenderer c = CodeSprites.Add(cords, CodeSprites.Square, new Color(1f, 0.95f, 0.88f), 6, new Vector2(x * 0.5f, -0.3f), new Vector2(0.07f, 1f), "Cuerda");
                c.transform.localRotation = Quaternion.Euler(0f, 0f, -x * 18f);
            }
            CodeSprites.Add(cords, CodeSprites.Square, new Color(1f, 0.92f, 0.85f), 7, new Vector2(0f, 0.15f), new Vector2(width, 0.16f), "Borde");
        }

        private void Update()
        {
            if (cords == null) return;
            float dt = Time.deltaTime;
            bounce = Mathf.MoveTowards(bounce, 0f, dt * 3f);
            cords.localScale = new Vector3(1f, 1f - bounce * 0.35f, 1f);
            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive) return;
            Vector2 p = player.Position;
            float top = transform.position.y + 0.25f;
            if (Mathf.Abs(p.x - transform.position.x) > width * 0.5f || p.y < top - 0.4f || p.y > top + 0.35f) return;
            if (player.Motor.Velocity.y > 0.5f) return;
            player.Motor.SetVelocity(new Vector2(player.Motor.Velocity.x, launch));
            bounce = 1f;
            Sfx.EffectAt("Jugador_Salto", transform.position, 0.8f);
        }
    }
}
