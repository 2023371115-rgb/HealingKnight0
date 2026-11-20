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
    /// CÁMARA QUE SE APRIETA (ventrículo): el techo de músculo BAJA en cada latido (sístole) y sube después. Si en ese
    /// momento estás arriba (en una repisa alta o saltando), te aplasta. Quédate abajo en el latido y sube entre latidos.
    /// </summary>
    public class SqueezingChamber : MonoBehaviour
    {
        [SerializeField] private float fromX, toX, ceilingY = 8f, drop = 3.4f, floorY;
        [SerializeField] private float damage = 10f;

        private Transform ceiling;
        private float hurtCooldown;
        private bool told;

        public void Configure(float startX, float endX, float groundY, float topY, float squeeze)
        {
            fromX = startX; toX = endX; floorY = groundY; ceilingY = topY; drop = squeeze;
        }

        private void Start()
        {
            ceiling = new GameObject("TechoMusculo").transform;
            ceiling.SetParent(transform, false);
            float w = toX - fromX;
            CodeSprites.Add(ceiling, CodeSprites.Square, new Color(0.28f, 0.03f, 0.07f), 8, new Vector2(w * 0.5f, 3f), new Vector2(w, 6f), "Musculo");
            for (float x = 0.6f; x < w; x += 1.3f)
            {
                CodeSprites.Add(ceiling, CodeSprites.Circle, new Color(0.25f, 0.02f, 0.06f), 9, new Vector2(x, 0.05f), new Vector2(1.45f, 1f), "Contorno");
                CodeSprites.Add(ceiling, CodeSprites.Circle, new Color(0.75f, 0.15f, 0.2f), 10, new Vector2(x, 0.1f), new Vector2(1.3f, 0.85f), "Fibra");
            }
            // Rayas de aviso en la pared del fondo: hasta dónde baja el techo.
            for (float x = 1f; x < w; x += 3f)
                CodeSprites.Add(transform, CodeSprites.Square, new Color(1f, 0.6f, 0.5f, 0.25f), -40, new Vector2(x, ceilingY - drop - floorY - transform.position.y + floorY), new Vector2(1.4f, 0.08f), "Marca");
        }

        private void Update()
        {
            if (ceiling == null) return;
            float dt = Time.deltaTime;
            float surge = Heartbeat.SurgeNow;
            float bottom = ceilingY - drop * surge;
            ceiling.position = new Vector3(fromX, bottom, 0f);
            if (hurtCooldown > 0f) hurtCooldown -= dt;

            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive) return;
            Vector2 p = player.Position;
            if (p.x < fromX || p.x > toX) return;
            if (!told)
            {
                told = true;
                GameplayEvents.RaiseMessage("¡El VENTRÍCULO se aprieta en cada latido! El techo baja: en el 'lub' quédate ABAJO y salta entre latidos.");
            }
            float head = p.y + 1.8f * player.Size;
            if (head > bottom && hurtCooldown <= 0f && surge > 0.3f)
            {
                hurtCooldown = 0.9f;
                DamageInfo info = DamageInfo.Create(damage, DamageKind.Environment, Team.Neutral, new Vector2(p.x, bottom));
                info.blockable = false;
                player.Health.ApplyDamage(info);
                player.Motor.SetVelocity(new Vector2(player.Motor.Velocity.x, -8f));
            }
        }
    }
}
