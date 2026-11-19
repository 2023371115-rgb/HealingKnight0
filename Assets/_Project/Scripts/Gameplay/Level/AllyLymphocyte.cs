using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// LINFOCITO B aliado: flota cerca de su casa (el centro germinal) o te sigue (premio de las amígdalas) y dispara
    /// ANTICUERPOS (en forma de Y) al enemigo más cercano. Los anticuerpos son lo único que le hace daño al Absceso.
    /// </summary>
    public class AllyLymphocyte : MonoBehaviour
    {
        [SerializeField] private float life = 30f;          // <= 0: para siempre
        [SerializeField] private float interval = 1.4f;
        [SerializeField] private float damage = 9f;
        [SerializeField] private float range = 10f;
        [SerializeField] private bool followPlayer;

        private Transform home, visual;
        private float timer, seed;
        private Antibody shot;

        public void Configure(Transform homeTransform, float lifetime, float fireInterval, float dmg, bool follow)
        {
            home = homeTransform; life = lifetime; interval = fireInterval; damage = dmg; followPlayer = follow;
        }

        private void Start()
        {
            seed = Random.value * 10f;
            timer = interval;
            visual = new GameObject("Dibujo").transform;
            visual.SetParent(transform, false);
            Sprite c = CodeSprites.Circle;
            CodeSprites.Add(visual, c, new Color(0.2f, 0.1f, 0.35f), 30, Vector2.zero, new Vector2(0.86f, 0.86f));
            CodeSprites.Add(visual, c, new Color(0.65f, 0.75f, 1f), 31, Vector2.zero, new Vector2(0.74f, 0.74f));
            CodeSprites.Add(visual, c, new Color(0.45f, 0.4f, 0.9f), 32, new Vector2(0.05f, -0.03f), new Vector2(0.48f, 0.48f));   // núcleo grande
            CodeSprites.Add(visual, c, Color.white, 33, new Vector2(-0.1f, 0.06f), new Vector2(0.12f, 0.14f));
            CodeSprites.Add(visual, c, Color.white, 33, new Vector2(0.12f, 0.06f), new Vector2(0.12f, 0.14f));
            CodeSprites.Add(visual, c, new Color(0.15f, 0.1f, 0.3f), 34, new Vector2(-0.09f, 0.05f), new Vector2(0.06f, 0.08f));
            CodeSprites.Add(visual, c, new Color(0.15f, 0.1f, 0.3f), 34, new Vector2(0.13f, 0.05f), new Vector2(0.06f, 0.08f));
            CodeSprites.Add(visual, c, new Color(1f, 1f, 1f, 0.6f), 33, new Vector2(-0.18f, 0.22f), new Vector2(0.16f, 0.1f));
        }

        private void Update()
        {
            float dt = Time.deltaTime, t = Time.time + seed;
            if (life > 0f)
            {
                life -= dt;
                if (life <= 0f) { Destroy(gameObject); return; }
                if (life < 3f) visual.gameObject.SetActive(Mathf.Sin(Time.time * 20f) > -0.2f);
            }

            Vector2 anchor;
            PlayerController p = PlayerController.Current;
            if (followPlayer && p != null) anchor = p.Position + new Vector2(-p.Motor.Facing * (1.2f + Mathf.Repeat(seed, 1f) * 1.1f), 2.2f + Mathf.Repeat(seed * 3.7f, 1f) * 0.9f);
            else if (home != null) anchor = (Vector2)home.position + new Vector2(Mathf.Sin(t * 0.7f) * 2.2f, 2.6f + Mathf.Sin(t * 1.3f) * 0.6f);
            else anchor = transform.position;
            anchor += new Vector2(0f, Mathf.Sin(t * 2.4f) * 0.15f);
            transform.position = Vector3.Lerp(transform.position, anchor, 1f - Mathf.Exp(-4f * dt));

            timer -= dt;
            if (timer > 0f) return;
            EnemyHealth target = Nearest();
            if (target == null) { timer = 0.3f; return; }
            timer = interval;
            var go = new GameObject("Anticuerpo");
            go.transform.position = transform.position;
            go.AddComponent<Antibody>().Launch(target, damage);
        }

        private EnemyHealth Nearest()
        {
            EnemyHealth best = null;
            float bestD = range;
            foreach (EnemyHealth e in EnemyHealth.Active)
            {
                if (e == null || !e.IsAlive) continue;
                if (e.GetComponent<Hazards.TreatableObstacle>() != null) continue;
                float d = Vector2.Distance(e.transform.position, transform.position);
                if (d < bestD) { bestD = d; best = e; }
            }
            return best;
        }
    }

    /// <summary>Anticuerpo en forma de Y que vuela al enemigo y le hace daño al llegar.</summary>
    public class Antibody : MonoBehaviour
    {
        private EnemyHealth target;
        private float damage, life = 2.5f;
        private Vector2 lastTargetPos;

        public void Launch(EnemyHealth enemy, float amount)
        {
            target = enemy; damage = amount;
            lastTargetPos = enemy.transform.position;
            Sprite sq = CodeSprites.Square;
            Color col = new Color(1f, 0.92f, 0.4f);
            CodeSprites.Add(transform, sq, col, 35, new Vector2(0f, -0.12f), new Vector2(0.08f, 0.28f));
            SpriteRenderer a = CodeSprites.Add(transform, sq, col, 35, new Vector2(-0.08f, 0.1f), new Vector2(0.07f, 0.24f));
            a.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
            SpriteRenderer b = CodeSprites.Add(transform, sq, col, 35, new Vector2(0.08f, 0.1f), new Vector2(0.07f, 0.24f));
            b.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            life -= dt;
            if (life <= 0f) { Destroy(gameObject); return; }
            if (target != null && target.IsAlive) lastTargetPos = (Vector2)target.transform.position + Vector2.up * 0.4f;
            Vector2 p = transform.position;
            Vector2 d = lastTargetPos - p;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
            if (d.magnitude < 0.5f)
            {
                if (target != null && target.IsAlive)
                {
                    AbscessBoss abscess = target.GetComponent<AbscessBoss>();
                    if (abscess != null) abscess.AntibodyHit(damage, p);
                    else target.ApplyDamage(DamageInfo.Create(damage, DamageKind.Projectile, Team.Player, p));
                }
                Destroy(gameObject);
                return;
            }
            transform.position = p + d.normalized * 13f * dt;
        }
    }
}
