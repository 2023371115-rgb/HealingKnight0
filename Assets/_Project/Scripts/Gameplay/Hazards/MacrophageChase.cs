using System.Collections.Generic;
using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// MACRÓFAGO DEL BAZO que te persigue: cuando pasas 'startX' aparece atrás de ti y avanza (más lento que corriendo).
    /// Si te alcanza te muerde y te avienta hacia adelante. Se va al llegar a 'endX'.
    /// Biología: los macrófagos del bazo se comen a los glóbulos rojos viejos y a lo que se queda atorado.
    /// </summary>
    public class MacrophageChase : MonoBehaviour
    {
        [SerializeField] private float startX, endX;
        [SerializeField] private float speed = 4.2f;
        [SerializeField] private float damage = 14f;
        [SerializeField] private float floorY;

        private bool running, done;
        private float x, hurtCooldown;
        private Transform art, mouth;

        public void Configure(float fromX, float toX, float groundY, float chaseSpeed)
        {
            startX = fromX; endX = toX; floorY = groundY; speed = chaseSpeed;
        }

        private void Start()
        {
            art = new GameObject("Macrofago").transform;
            art.SetParent(transform, false);
            Color edge = new Color(0.4f, 0.3f, 0.2f), body = new Color(0.95f, 0.85f, 0.7f);
            CodeSprites.Add(art, CodeSprites.Circle, edge, 40, new Vector2(0f, 2.2f), new Vector2(4.6f, 4.4f), "Contorno");
            CodeSprites.Add(art, CodeSprites.Circle, body, 41, new Vector2(0f, 2.2f), new Vector2(4.3f, 4.1f), "Cuerpo");
            for (int i = 0; i < 5; i++)
            {
                float a = (i * 72f + 30f) * Mathf.Deg2Rad;
                Vector2 p = new Vector2(Mathf.Cos(a) * 2f, 2.2f + Mathf.Sin(a) * 1.9f);
                CodeSprites.Add(art, CodeSprites.Circle, edge, 40, p, Vector2.one * 1.5f, "PataContorno");
                CodeSprites.Add(art, CodeSprites.Circle, body, 41, p, Vector2.one * 1.3f, "Pata");
            }
            CodeSprites.Add(art, CodeSprites.Circle, new Color(0.6f, 0.4f, 0.7f), 42, new Vector2(-0.8f, 1.6f), new Vector2(1.3f, 1f), "Nucleo");
            // Glóbulos viejos que ya se comió (adentro).
            for (int i = 0; i < 3; i++)
                CodeSprites.Add(art, CodeSprites.Circle, new Color(0.6f, 0.25f, 0.2f, 0.8f), 42, new Vector2(-0.4f + i * 0.6f, 2.8f - i * 0.4f), new Vector2(0.7f, 0.3f), "GloboViejo");
            foreach (float ex in new[] { 0.6f, 1.4f })
            {
                CodeSprites.Add(art, CodeSprites.Circle, Color.white, 43, new Vector2(ex, 3.1f), new Vector2(0.55f, 0.65f), "Ojo");
                CodeSprites.Add(art, CodeSprites.Circle, Color.black, 44, new Vector2(ex + 0.1f, 3.05f), Vector2.one * 0.25f, "Pupila");
            }
            mouth = CodeSprites.Add(art, CodeSprites.Circle, new Color(0.35f, 0.05f, 0.1f), 43, new Vector2(1.3f, 2f), new Vector2(1.1f, 0.5f), "Boca").transform;
            art.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (done || art == null) return;
            float dt = Time.deltaTime;
            PlayerController player = PlayerController.Current;
            if (player == null) return;
            if (!running)
            {
                if (player.Position.x < startX) return;
                running = true;
                x = startX - 9f;
                art.gameObject.SetActive(true);
                GameplayEvents.RaiseMessage("¡Un MACRÓFAGO del bazo! Se come a los glóbulos viejos... y a lo que se atora. ¡CORRE!");
                Sfx.Effect("Colmena_Enjambre", 0.6f);
            }
            // Si te quedas muy atrás (reapareciste), espera.
            if (player.Position.x < x - 3f) x = player.Position.x - 9f;
            x += speed * dt;
            art.position = new Vector3(x, floorY, 0f);
            art.localScale = new Vector3(1f + Mathf.Sin(Time.time * 5f) * 0.04f, 1f + Mathf.Cos(Time.time * 5f) * 0.04f, 1f);
            if (mouth != null) mouth.localScale = new Vector3(1.1f, 0.5f + Mathf.Abs(Mathf.Sin(Time.time * 6f)) * 0.6f, 1f);
            if (x > endX)
            {
                done = true;
                art.gameObject.SetActive(false);
                GameplayEvents.RaiseMessage("Se quedó atrás, comiéndose glóbulos viejos. ¡Uf!");
                return;
            }
            if (hurtCooldown > 0f) { hurtCooldown -= dt; return; }
            if (!player.Health.IsAlive) return;
            if (player.Position.x < x + 2.2f && player.Position.x > x - 2.4f && player.Position.y < floorY + 4.4f)
            {
                DamageInfo info = DamageInfo.Create(damage, DamageKind.Contact, Team.Enemy, new Vector2(x, floorY + 2f));
                info.knockback = 9f;
                player.Health.ApplyDamage(info);
                hurtCooldown = 1f;
            }
        }
    }
}
