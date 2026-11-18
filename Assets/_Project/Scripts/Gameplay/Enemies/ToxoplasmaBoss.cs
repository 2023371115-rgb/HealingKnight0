using HealingKnight.Core.Damage;
using HealingKnight.Core.Flow;
using HealingKnight.Core.Pooling;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>
    /// JEFE del Ojo: TOXOPLASMA (Toxoplasma gondii). Biología: un parásito que se esconde en QUISTES dentro de la retina
    /// y puede reactivarse (retinocoroiditis). Patrón de sigilo:
    ///  - Se esconde en uno de los quistes (no le puedes hacer daño).
    ///  - Si llegas pegado a su quiste SIN que te detecten (barra baja), lo SORPRENDES: sale mareado y le puedes pegar.
    ///  - Después de un rato dispara y se cambia a otro quiste. Si te ve acercarte (barra alta) huye antes.
    /// Al derrotarlo ganas el premio: el ESCÁNER de NODO (ves la debilidad de los enemigos en todos los niveles).
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class ToxoplasmaBoss : MonoBehaviour
    {
        private enum State { Hidden, Stunned, Attack, Dead }

        [SerializeField] private string bossName = "TOXOPLASMA";
        [SerializeField, TextArea] private string introMessage = "NODO: Toxoplasma está escondido en un quiste de la retina. Acércate SIN que te vean y sorpréndelo.";
        [SerializeField, TextArea] private string defeatMessage = "NODO: Toxoplasma eliminado. PREMIO: aprendí a ESCANEAR. Verás la debilidad de cada enemigo en todos los niveles.";
        [SerializeField] private string rewardSecret = "premio_escaneo";
        [SerializeField] private Vector2[] cysts;
        [SerializeField] private Transform visual;
        [SerializeField] private SpriteRenderer art;
        [SerializeField] private Sprite idleSprite, stunnedSprite, attackSprite, deadSprite;
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private LayerMask groundMask = 1 << 6;
        [SerializeField] private float stunTime = 4f;

        private EnemyHealth health;
        private ComponentPool<Projectile> pool;
        private State state = State.Hidden;
        private int cyst;
        private float timer, shootTimer;
        private Transform[] cystVisuals;

        public void Configure(Vector2[] spots) => cysts = spots;

        private void Awake() => health = GetComponent<EnemyHealth>();
        private void OnEnable() => health.Died += OnDied;
        private void OnDisable() => health.Died -= OnDied;

        private void OnDestroy()
        {
            if (state != State.Dead) GameplayEvents.RaiseBossEnded();
            pool?.Clear();
            if (cystVisuals != null) foreach (Transform t in cystVisuals) if (t != null) Destroy(t.gameObject);
        }

        private void Start()
        {
            if (projectilePrefab != null) pool = new ComponentPool<Projectile>(projectilePrefab, 8, 24);
            if (cysts == null || cysts.Length == 0)
            {
                CystLayout layout = FindFirstObjectByType<CystLayout>();
                if (layout != null) cysts = layout.Spots;
            }
            if (cysts == null || cysts.Length == 0) cysts = new[] { (Vector2)transform.position };
            cystVisuals = new Transform[cysts.Length];
            for (int i = 0; i < cysts.Length; i++)
            {
                Transform t = new GameObject("Quiste").transform;
                t.position = cysts[i];
                Sprite c = CodeSprites.Circle;
                CodeSprites.Add(t, c, new Color(0.25f, 0.15f, 0.3f, 0.9f), 3, Vector2.zero, new Vector2(2.3f, 2.1f));
                CodeSprites.Add(t, c, new Color(0.8f, 0.7f, 0.95f, 0.75f), 4, Vector2.zero, new Vector2(2.1f, 1.9f));
                for (int k = 0; k < 6; k++)
                    CodeSprites.Add(t, c, new Color(0.55f, 0.45f, 0.8f, 0.8f), 5, Random.insideUnitCircle * 0.6f, new Vector2(0.25f, 0.25f));
                CodeSprites.Add(t, c, new Color(1f, 1f, 1f, 0.6f), 6, new Vector2(-0.5f, 0.5f), new Vector2(0.4f, 0.25f));
                cystVisuals[i] = t;
            }
            cyst = 0;
            Hide();
            GameplayEvents.RaiseBossStarted(bossName, health);
            if (!string.IsNullOrEmpty(introMessage)) GameplayEvents.RaiseMessage(introMessage);
        }

        private void Hide()
        {
            state = State.Hidden;
            health.Invulnerable = true;
            transform.position = cysts[cyst];
            if (visual != null) visual.gameObject.SetActive(false);
            timer = 0f;
        }

        private void Update()
        {
            UpdateArt();
            if (state == State.Dead) return;
            float dt = Time.deltaTime;
            PlayerController p = PlayerController.Current;
            StealthMeter meter = StealthMeter.Current;
            timer += dt;

            // El quiste ocupado late un poquito (pista).
            for (int i = 0; i < cystVisuals.Length; i++)
                cystVisuals[i].localScale = Vector3.one * (i == cyst && state == State.Hidden ? 1f + Mathf.Sin(Time.time * 3f) * 0.04f : 1f);

            // Mira hacia el jugador (el dibujo mira a la izquierda).
            if (visual != null && p != null)
            {
                float sx = p.Position.x < transform.position.x ? 1f : -1f;
                visual.localScale = new Vector3(sx, 1f, 1f);
            }

            switch (state)
            {
                case State.Hidden:
                    if (p == null || !p.Health.IsAlive) break;
                    float d = Vector2.Distance(p.Position + Vector2.up * 0.9f, cysts[cyst]);
                    bool noticed = meter != null && (meter.Level > 0.55f || meter.Alerted);
                    if (d < 2.6f && !noticed) Surprise();
                    else if (d < 6f && noticed) { Pop(); state = State.Attack; timer = 0f; shootTimer = 0f; }
                    break;
                case State.Stunned:
                    if (visual != null) visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 10f) * 15f);
                    if (timer > stunTime) { state = State.Attack; timer = 0f; shootTimer = 0.3f; }
                    break;
                case State.Attack:
                    if (visual != null) visual.localRotation = Quaternion.identity;
                    shootTimer -= dt;
                    if (shootTimer <= 0f && p != null) { shootTimer = 0.7f; Shoot(p); }
                    if (timer > 2.4f) Relocate();
                    break;
            }
        }

        private void Pop()
        {
            if (visual != null) { visual.gameObject.SetActive(true); visual.localPosition = Vector3.zero; }
            Sfx.EffectAt("Acido_Burbujeo", transform.position, 0.9f);
        }

        private void Surprise()
        {
            Pop();
            state = State.Stunned;
            timer = 0f;
            health.Invulnerable = false;
            Sfx.Effect("Escudo_Parry", 0.8f);
            GameplayEvents.RaiseMessage("¡Lo sorprendiste! Está mareado: ¡ANTIPARASITARIO!");
        }

        private void Relocate()
        {
            int next = cyst;
            if (cysts.Length > 1) while (next == cyst) next = Random.Range(0, cysts.Length);
            cyst = next;
            Sfx.EffectAt("Alveolo_Desinfla", transform.position, 0.7f);
            Hide();
        }

        private void Shoot(PlayerController p)
        {
            if (pool == null) return;
            Vector2 from = transform.position;
            Vector2 dir = (p.Position + Vector2.up * 0.9f - from).normalized;
            Projectile shot = pool.Get();
            shot.Launch(new ProjectileShot
            {
                origin = from, direction = dir, speed = 8f, range = 20f, damage = 9f, knockback = 3f, radius = 0.3f,
                sourceTeam = Team.Enemy, targetTeam = Team.Player, color = new Color(0.7f, 1f, 0.6f), groundMask = groundMask
            }, done => pool.Release(done));
        }

        private void UpdateArt()
        {
            if (art == null) return;
            Sprite s = state == State.Dead ? deadSprite : state == State.Stunned ? stunnedSprite : state == State.Attack ? attackSprite : idleSprite;
            if (s == null) s = idleSprite;
            if (s != null && art.sprite != s) art.sprite = s;
        }

        private void OnDied(EnemyHealth _)
        {
            state = State.Dead;
            if (visual != null) visual.gameObject.SetActive(true);
            GameSession.FindSecret(rewardSecret);
            GameplayEvents.RaiseBossEnded();
            if (!string.IsNullOrEmpty(defeatMessage)) GameplayEvents.RaiseMessage(defeatMessage);
        }
    }

    /// <summary>Dónde están los quistes de la retina en esta escena (el jefe Toxoplasma los usa al aparecer).</summary>
    public class CystLayout : MonoBehaviour
    {
        [SerializeField] private Vector2[] spots;
        public Vector2[] Spots => spots;
        public void Configure(Vector2[] positions) => spots = positions;
    }
}
