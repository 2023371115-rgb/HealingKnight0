using HealingKnight.Gameplay.Audio;
using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Enemies;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// Neurona sana. Tus cápsulas la ATRAVIESAN (disparas libre, como en todo el juego).
    /// El peligro viene de los enemigos: cada proyectil enemigo que la toca resta 3 a la salud del cerebro,
    /// y si recibe varios se INFECTA y se vuelve neurona hiperexcitada (roja, suelta descargas).
    /// El campo neuroprotector (bomba de anticonvulsivo) la vuelve inmune un rato.
    /// Necesita un Hurtbox en el mismo objeto.
    /// </summary>
    public class HealthyNeuron : MonoBehaviour, IDamageable
    {
        [SerializeField] private float enemyShotDamage = 3f;
        [Tooltip("Golpes de enemigos que aguanta antes de infectarse (0 = nunca se infecta así).")]
        [SerializeField] private int hitsToInfect = 3;
        [SerializeField] private EnemyHealth infectedPrefab;
        [SerializeField] private SpriteRenderer[] renderers;
        [SerializeField] private Color hurtColor = new Color(1f, 0.25f, 0.3f);
        [SerializeField] private float dimPerHit = 0.15f;

        private Color[] baseColors;
        private float flashTimer;
        private float dim;
        private int hits;

        public Team Team => Team.Neutral;
        public bool IsAlive => true;

        private void Awake()
        {
            if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<SpriteRenderer>();
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i] != null ? renderers[i].color : Color.white;
        }

        public HitResult ApplyDamage(DamageInfo info)
        {
            // Solo le hacen daño los proyectiles enemigos (los tuyos la atraviesan).
            if (info.kind != DamageKind.Projectile || info.sourceTeam != Team.Enemy) return HitResult.Ignored;
            if (NeuroprotectionField.Protects(transform.position)) return HitResult.Ignored;

            if (BrainHealth.Current != null) BrainHealth.Current.Damage(enemyShotDamage);
            hits++;
            Sfx.EffectAt("Neurona_Golpe", transform.position, 0.7f);
            dim = Mathf.Min(0.6f, dim + dimPerHit);
            flashTimer = 0.2f;

            if (hitsToInfect > 0 && hits >= hitsToInfect) Infect();
            return new HitResult { outcome = HitOutcome.Damaged, effectiveness = Core.Combat.Effectiveness.Ineffective, damageDealt = 0f };
        }

        /// <summary>La infección la vuelve neurona hiperexcitada (la usan los enemigos y el jefe rabia).</summary>
        public void Infect()
        {
            Sfx.EffectAt("Neurona_Infecta", transform.position);
            if (infectedPrefab != null) Instantiate(infectedPrefab, transform.position, Quaternion.identity);
            GameplayEvents.RaiseMessage("¡Infectaron una neurona! Cálmala con Anticonvulsivo.");
            Destroy(gameObject);
        }

        private void Update()
        {
            // Late suave (neurona viva) y destello rojo al recibir daño.
            float pulse = 1f + Mathf.Sin(Time.time * 3f + transform.position.x) * 0.06f;
            if (flashTimer > 0f) flashTimer -= Time.deltaTime;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Color baseC = Color.Lerp(baseColors[i], new Color(0.25f, 0.25f, 0.35f, baseColors[i].a), dim);
                Color c = flashTimer > 0f ? hurtColor : baseC * pulse;
                c.a = baseColors[i].a;
                renderers[i].color = c;
            }
        }
    }
}
