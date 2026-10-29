using System;
using System.Collections;
using HealingKnight.Core.Combat;
using HealingKnight.Core.Damage;
using UnityEngine;

namespace HealingKnight.Gameplay.Player
{
    /// <summary>
    /// Salud de Healing Knight (GDD 5.9): 100 puntos, invulnerabilidad breve con parpadeo,
    /// empuje al recibir golpes. Al morir avisa; el LevelManager decide dónde reaparece.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Tooltip("Sprites que parpadean al ser invulnerable. Vacío = todos los hijos.")]
        [SerializeField] private SpriteRenderer[] blinkRenderers;

        private PlayerController controller;
        private PlayerShield shield;
        private float current;
        private float invulnerableUntil;
        private Coroutine blinkRoutine;
        private float[] baseAlphas;

        public Team Team => Team.Player;
        public bool IsAlive => current > 0f;
        public float Current => current;
        public float Max => controller.Config.maxHealth;
        public float Health01 => Max > 0f ? current / Max : 0f;

        public event Action<float, float> HealthChanged; // (actual, máxima)
        public event Action Damaged;
        public event Action Died;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            shield = GetComponent<PlayerShield>();
            if (blinkRenderers == null || blinkRenderers.Length == 0)
                blinkRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            baseAlphas = new float[blinkRenderers.Length];
            for (int i = 0; i < blinkRenderers.Length; i++)
                baseAlphas[i] = blinkRenderers[i] != null ? blinkRenderers[i].color.a : 1f;
            current = controller.Config != null ? controller.Config.maxHealth : 100f;
        }

        public void ResetHealth()
        {
            current = Max;
            invulnerableUntil = Time.time + 1f;
            StartBlink(1f);
            HealthChanged?.Invoke(current, Max);
        }

        public HitResult ApplyDamage(DamageInfo info)
        {
            if (!IsAlive || info.sourceTeam == Team.Player || Time.time < invulnerableUntil)
                return HitResult.Ignored;

            if (shield != null && shield.TryBlock(info))
                return new HitResult { outcome = HitOutcome.Blocked, effectiveness = Effectiveness.Ineffective };

            current = Mathf.Max(0f, current - info.amount);
            HealthChanged?.Invoke(current, Max);
            Damaged?.Invoke();

            if (current <= 0f)
            {
                controller.OnDied();
                Died?.Invoke();
                return new HitResult { outcome = HitOutcome.Killed, effectiveness = Effectiveness.Effective, damageDealt = info.amount };
            }

            // El daño ambiental (ácido) no aturde ni da invulnerabilidad: castiga quedarse encima.
            if (info.kind == DamageKind.Environment)
            {
                StartBlink(0.15f);
                return new HitResult { outcome = HitOutcome.Damaged, effectiveness = Effectiveness.Effective, damageDealt = info.amount };
            }

            float dir = controller.Position.x >= info.sourcePosition.x ? 1f : -1f;
            controller.OnHurt(dir, Mathf.Max(info.knockback, controller.Config.hurtKnockback));

            invulnerableUntil = Time.time + controller.Config.invulnerabilityTime;
            StartBlink(controller.Config.invulnerabilityTime);

            return new HitResult { outcome = HitOutcome.Damaged, effectiveness = Effectiveness.Effective, damageDealt = info.amount };
        }

        private void StartBlink(float duration)
        {
            if (blinkRoutine != null) StopCoroutine(blinkRoutine);
            blinkRoutine = StartCoroutine(Blink(duration));
        }

        private IEnumerator Blink(float duration)
        {
            float end = Time.time + duration;
            bool visible = true;
            while (Time.time < end)
            {
                visible = !visible;
                SetVisible(visible);
                yield return new WaitForSeconds(0.08f);
            }
            SetVisible(true);
            blinkRoutine = null;
        }

        private void SetVisible(bool visible)
        {
            // Respeta la transparencia original de cada pieza (sombra, burbuja del escudo...).
            for (int i = 0; i < blinkRenderers.Length; i++)
            {
                SpriteRenderer r = blinkRenderers[i];
                if (r == null) continue;
                Color c = r.color;
                c.a = visible ? baseAlphas[i] : baseAlphas[i] * 0.25f;
                r.color = c;
            }
        }
    }
}
