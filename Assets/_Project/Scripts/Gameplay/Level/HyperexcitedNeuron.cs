using HealingKnight.Gameplay.Audio;
using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// Neurona hiperexcitada (roja): dañada por la infección, suelta descargas eléctricas sin control,
    /// como en una convulsión. Cada pocos segundos avisa (brilla y crece el anillo) y descarga en círculo:
    /// si estás cerca, te daña (el escudo no protege).
    /// Se SANA con el anticonvulsivo (la vida y la efectividad las maneja EnemyHealth con threat = Hiperexcitacion):
    /// al sanarla se convierte en neurona sana y devuelve salud al cerebro.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class HyperexcitedNeuron : MonoBehaviour
    {
        [SerializeField] private HealthyNeuron healedPrefab;
        [SerializeField] private float brainHealOnCure = 5f;
        [Tooltip("Mientras siga roja, el cerebro pierde salud poco a poco (te apura a calmarla).")]
        [SerializeField] private float brainDrainPerSecond = 0.25f;

        [Header("Descarga")]
        [SerializeField] private float interval = 4.5f;
        [SerializeField] private float warningTime = 0.9f;
        [SerializeField] private float radius = 2.3f;
        [SerializeField] private float damage = 15f;
        [SerializeField] private SpriteRenderer ring;
        [SerializeField] private SpriteRenderer core;

        private EnemyHealth health;
        private float timer;
        private float flash;
        private Color coreColor = Color.white;
        private bool warned;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            timer = Random.Range(1.5f, interval);
            if (core != null) coreColor = core.color;
            if (ring != null) ring.enabled = false;
        }

        private void OnEnable() => health.Died += OnCured;
        private void OnDisable() => health.Died -= OnCured;

        private void Update()
        {
            if (!health.IsAlive) return;
            timer -= Time.deltaTime;
            if (BrainHealth.Current != null) BrainHealth.Current.Damage(brainDrainPerSecond * Time.deltaTime);

            if (timer <= warningTime && ring != null)
            {
                // Aviso: el anillo crece hasta el radio de la descarga y el núcleo parpadea.
                float k = 1f - Mathf.Clamp01(timer / warningTime);
                if (!warned)
                {
                    warned = true;
                    Sfx.EffectAt("Neurona_Carga", transform.position, 0.7f);
                }
                ring.enabled = true;
                ring.transform.localScale = Vector3.one * radius * 2f * Mathf.Lerp(0.3f, 1f, k);
                Color c = ring.color;
                c.a = 0.2f + k * 0.35f;
                ring.color = c;
                if (core != null) core.color = Mathf.Sin(Time.time * 30f) > 0f ? Color.white : coreColor;
            }

            if (timer <= 0f)
            {
                warned = false;
                Sfx.EffectAt("Neurona_Descarga", transform.position);
                Discharge();
                timer = interval;
                if (ring != null) ring.enabled = false;
                if (core != null) core.color = coreColor;
            }

            if (flash > 0f)
            {
                flash -= Time.deltaTime;
                if (ring != null)
                {
                    ring.enabled = true;
                    ring.transform.localScale = Vector3.one * radius * 2f;
                    ring.color = new Color(1f, 0.9f, 1f, flash * 3f);
                }
            }
        }

        private void Discharge()
        {
            flash = 0.25f;
            PlayerController player = PlayerController.Current;
            if (player == null) return;
            Hurtbox box = player.GetComponent<Hurtbox>();
            if (box == null || !box.IsAlive || !box.OverlapsCircle(transform.position, radius)) return;

            DamageInfo info = DamageInfo.Create(damage, DamageKind.Contact, Team.Neutral, transform.position);
            info.blockable = false;
            info.knockback = 5f;
            box.Damageable.ApplyDamage(info);
        }

        private void OnCured(EnemyHealth _)
        {
            if (ring != null) ring.enabled = false;
            if (healedPrefab != null) Instantiate(healedPrefab, transform.position, Quaternion.identity);
            if (BrainHealth.Current != null) BrainHealth.Current.Heal(brainHealOnCure);
            GameplayEvents.RaiseMessage("Neurona calmada: vuelve a estar sana (+5 salud del cerebro).");
        }
    }
}
