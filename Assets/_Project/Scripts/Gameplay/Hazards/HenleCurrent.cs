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
    /// Corriente lateral del asa de Henle (idea del equipo): empuja al jugador y pulsa, para crear ventanas de cruce.
    /// Para corrientes con dibujo y en cualquier dirección, ver <see cref="WaterCurrent"/>.
    /// </summary>
    public class HenleCurrent : MonoBehaviour
    {
        [SerializeField] private float fromX, toX, minY, maxY;
        [SerializeField] private float direction = 1f, strength = 2f, pulseSpeed = 1.7f;
        private float phase;

        public void Configure(float startX, float endX, float bottom, float top, float push, float pulseSpeed)
        {
            fromX = startX; toX = endX; minY = bottom; maxY = top;
            direction = Mathf.Sign(push); strength = Mathf.Abs(push); this.pulseSpeed = pulseSpeed; phase = startX * 0.31f;
        }

        private void FixedUpdate()
        {
            PlayerController player = PlayerController.Current;
            if (player == null || player.Motor == null || player.IsFlying || !player.Health.IsAlive) return;
            Vector2 center = player.Motor.Center;
            if (center.x < fromX || center.x > toX || center.y < minY || center.y > maxY) return;

            // La corriente alterna entre empujones y pausas para que el jugador pueda planear el cruce.
            float pulse = 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed + phase));
            Vector2 velocity = player.Motor.Velocity;
            velocity.x = Mathf.Clamp(velocity.x + direction * strength * pulse * Time.fixedDeltaTime, -8f, 8f);
            player.Motor.SetVelocity(velocity);
        }
    }
}
