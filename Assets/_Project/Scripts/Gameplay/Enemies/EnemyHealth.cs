using HealingKnight.Gameplay.Audio;
using System;
using System.Collections;
using HealingKnight.Core.Combat;
using HealingKnight.Core.Damage;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>
    /// Vida del enemigo. Las cápsulas pasan por la tabla de efectividad:
    /// Efectivo = daño completo, Parcial = daño reducido, Ineficaz = la cápsula rebota (GDD 5.7).
    /// </summary>
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private EnemyData data;
        [SerializeField] private EffectivenessTable effectivenessTable;

        [Tooltip("Sprites que destellan al recibir daño.")]
        [SerializeField] private SpriteRenderer[] flashRenderers;

        private float current;
        private bool dead;
        private Coroutine flashRoutine;
        private Color[] baseColors;

        public EnemyData Data => data;

        /// <summary>Si es true, ignora todo el daño (jefe bajo tierra, por ejemplo). Las cápsulas lo atraviesan.</summary>
        public bool Invulnerable { get; set; }

        /// <summary>Encerrado (espora, granuloma): todo rebota en gris como si fuera ineficaz.</summary>
        public bool Shelled { get; set; }

        /// <summary>La vida no baja de este valor (0..1 de la vida máxima). Lo usa un jefe que huye en vez de morir.</summary>
        public float HealthFloor01 { get; set; }
        public Team Team => Team.Enemy;
        public bool IsAlive => !dead;
        public float Health01 => data != null && data.maxHealth > 0f ? current / data.maxHealth : 0f;

        /// <summary>Se dispara con cada golpe recibido (aunque no haga daño).</summary>
        public event Action<DamageInfo, HitResult> Hit;
        public event Action<EnemyHealth> Died;

        /// <summary>Cualquier enemigo derrotado (para Linfocito, arenas, estadísticas).</summary>
        public static event Action<EnemyHealth> AnyDied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => AnyDied = null;

        private void Awake()
        {
            if (flashRenderers == null || flashRenderers.Length == 0)
                flashRenderers = GetComponentsInChildren<SpriteRenderer>(true);

            baseColors = new Color[flashRenderers.Length];
            for (int i = 0; i < flashRenderers.Length; i++)
                baseColors[i] = flashRenderers[i] != null ? flashRenderers[i].color : Color.white;

            current = data != null ? data.maxHealth : 1f;
        }

        public void Configure(EnemyData enemyData, EffectivenessTable table)
        {
            if (enemyData != null) data = enemyData;
            if (table != null) effectivenessTable = table;
            current = data != null ? data.maxHealth : current;
            dead = false;
        }

        public HitResult ApplyDamage(DamageInfo info)
        {
            if (dead || Invulnerable || info.sourceTeam == Team.Enemy || data == null) return HitResult.Ignored;

            Effectiveness effectiveness = Effectiveness.Effective;
            float amount = info.amount;

            if (info.medication != null)
            {
                if (effectivenessTable == null)
                {
                    Debug.LogWarning($"[EnemyHealth] {name} no tiene tabla de efectividad asignada.", this);
                }
                else
                {
                    effectiveness = Shelled ? Effectiveness.Ineffective : data.Evaluate(effectivenessTable, info.medication);
                    amount *= effectivenessTable.GetMultiplier(effectiveness);
                }
            }
            else if (info.kind == DamageKind.Melee)
            {
                amount *= Shelled ? 0f : data.meleeDamageTakenMultiplier;
            }

            if (amount <= 0f)
            {
                var resisted = new HitResult { outcome = HitOutcome.Resisted, effectiveness = effectiveness };
                Flash(new Color(0.55f, 0.55f, 0.55f), 0.12f);
                Hit?.Invoke(info, resisted);
                return resisted;
            }

            current -= amount;
            if (HealthFloor01 > 0f) current = Mathf.Max(current, data.maxHealth * HealthFloor01);
            Flash(effectiveness == Effectiveness.Partial ? new Color(1f, 0.9f, 0.4f) : Color.white, 0.08f);

            HitResult result = new HitResult
            {
                outcome = current <= 0f ? HitOutcome.Killed : HitOutcome.Damaged,
                effectiveness = effectiveness,
                damageDealt = amount
            };

            Hit?.Invoke(info, result);

            if (current <= 0f) Die();
            return result;
        }

        /// <summary>
        /// Sale del combate sin morir (un jefe que huye). Cuenta como "ya no está vivo" para las arenas,
        /// pero no dispara AnyDied ni la animación de muerte. El que llama se encarga de destruirlo.
        /// </summary>
        /// <summary>Para jefes compuestos (el ciempiés de rabia): la barra refleja la vida de todas sus partes.</summary>
        public void SetHealth01(float value01)
        {
            if (data == null || dead) return;
            current = Mathf.Clamp01(value01) * data.maxHealth;
        }

        /// <summary>Recupera vida (la espora de C. difficile al despertar).</summary>
        public void Heal(float amount)
        {
            if (data == null || dead) return;
            current = Mathf.Min(data.maxHealth, current + amount);
        }

        /// <summary>Muerte forzada (cuando el jefe compuesto ya no tiene partes).</summary>
        public void Kill()
        {
            if (dead) return;
            current = 0f;
            Die();
        }

        public void Retreat()
        {
            dead = true;
        }

        private void Die()
        {
            if (dead) return;
            dead = true;
            Sfx.PlayAt(Sfx.EnemySound(data, "Muere"), transform.position,
                data != null && data.name.StartsWith("Jefe") ? CombatAudio.Effects : CombatAudio.Enemies);
            Died?.Invoke(this);
            AnyDied?.Invoke(this);
            StartCoroutine(DissolveAndDestroy());
        }

        private IEnumerator DissolveAndDestroy()
        {
            const float duration = 0.4f;
            float t = 0f;
            Vector3 startScale = transform.localScale;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = 1f - t / duration;
                transform.localScale = new Vector3(startScale.x * (1f + (1f - k) * 0.3f), startScale.y * k, startScale.z);
                SetAlpha(k);
                yield return null;
            }
            Destroy(gameObject);
        }

        private void Flash(Color color, float duration)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine(color, duration));
        }

        private IEnumerator FlashRoutine(Color color, float duration)
        {
            for (int i = 0; i < flashRenderers.Length; i++)
                if (flashRenderers[i] != null) flashRenderers[i].color = color;

            yield return new WaitForSeconds(duration);
            RestoreColors();
            flashRoutine = null;
        }

        /// <summary>Tiñe el cuerpo de forma duradera (espora pálida, granuloma oscuro). amount 0 = color normal.</summary>
        public void SetTint(Color tint, float amount)
        {
            if (originalColors == null) originalColors = (Color[])baseColors.Clone();
            for (int i = 0; i < flashRenderers.Length; i++)
            {
                Color c = Color.Lerp(originalColors[i], tint, amount);
                c.a = originalColors[i].a;
                baseColors[i] = c;
            }
            if (flashRoutine == null) RestoreColors();
        }

        private Color[] originalColors;

        /// <summary>Lo usa la IA para el aviso antes de atacar.</summary>
        public void SetTelegraph(bool on)
        {
            if (flashRoutine != null) return;
            for (int i = 0; i < flashRenderers.Length; i++)
            {
                if (flashRenderers[i] == null) continue;
                flashRenderers[i].color = on ? Color.Lerp(baseColors[i], new Color(1f, 0.3f, 0.3f), 0.6f) : baseColors[i];
            }
        }

        private void RestoreColors()
        {
            for (int i = 0; i < flashRenderers.Length; i++)
                if (flashRenderers[i] != null) flashRenderers[i].color = baseColors[i];
        }

        private void SetAlpha(float a)
        {
            for (int i = 0; i < flashRenderers.Length; i++)
            {
                if (flashRenderers[i] == null) continue;
                Color c = flashRenderers[i].color;
                c.a = a;
                flashRenderers[i].color = c;
            }
        }

        /// <summary>Actualiza los colores base (por ejemplo si se aplica un tinte después de Awake).</summary>
        public void RefreshBaseColors()
        {
            for (int i = 0; i < flashRenderers.Length; i++)
                if (flashRenderers[i] != null) baseColors[i] = flashRenderers[i].color;
        }
    }
}
