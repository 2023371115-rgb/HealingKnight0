using HealingKnight.Gameplay.Audio;
using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// Charco de ácido en el piso (GDD 5.12): daña cada cierto tiempo mientras lo pisas.
    /// Se evita saltando por encima. El escudo no protege.
    /// La zona es un rectángulo centrado en el objeto: dañan los pies del jugador dentro de él.
    /// </summary>
    public class AcidPool : MonoBehaviour
    {
        [SerializeField] private Vector2 size = new Vector2(2.5f, 0.4f);
        [SerializeField] private float damagePerTick = 5f;
        [SerializeField] private float tickInterval = 0.5f;

        private float timer;

        public Vector2 Size => size;

        private void Start() => SfxLoop.Attach(gameObject, "Acido_Burbujeo", 0.5f, size.x * 0.5f, size.x * 0.5f + 7f);

        private void Update()
        {
            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive) return;

            Vector2 p = player.Position;
            Vector2 c = transform.position;
            bool inside = Mathf.Abs(p.x - c.x) <= size.x * 0.5f && Mathf.Abs(p.y - c.y) <= size.y * 0.5f
                          && player.Motor.IsGrounded;

            if (!inside)
            {
                timer = 0f;
                return;
            }

            timer -= Time.deltaTime;
            if (timer > 0f) return;

            timer = tickInterval;
            DamageInfo info = DamageInfo.Create(damagePerTick, DamageKind.Environment, Team.Neutral, c);
            info.blockable = false;
            player.Health.ApplyDamage(info);
            Sfx.Effect("Acido_Quema", 0.8f);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.9f, 0.9f, 0.2f, 0.8f);
            Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 0f));
        }
    }
}
