using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.CameraSystem;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// MOVIMIENTO DE MASA: unas pocas veces al día el colon se contrae fuerte y empuja todo su contenido de golpe.
    /// En la pelea final, cuando al jefe le queda poca vida ('fromHealth01'), cada cierto tiempo pasa una ola baja
    /// de lado a lado de la arena: si estás en el piso te pega y te avienta. Hay que estar arriba (plataformas o en el aire).
    /// Antes de cada ola retumba y se ve venir desde la orilla (aviso). Se dibuja por código.
    /// </summary>
    public class MassWave : MonoBehaviour
    {
        [SerializeField] private float fromX = 0f;
        [SerializeField] private float toX = 24f;
        [SerializeField] private float height = 1.9f;
        [SerializeField] private float speed = 10f;
        [SerializeField] private float interval = 8f;
        [SerializeField] private float warning = 1.2f;
        [SerializeField] private float damage = 15f;
        [SerializeField, Range(0f, 1f)] private float fromHealth01 = 0.35f;
        [SerializeField, TextArea] private string message = "¡MOVIMIENTO DE MASA! El colon empuja todo de golpe: ¡súbete a una plataforma!";
        [SerializeField] private Color fill = new Color(0.5f, 0.34f, 0.17f, 1f);
        [SerializeField] private Color crest = new Color(0.66f, 0.48f, 0.26f, 1f);
        [SerializeField] private Color outline = new Color(0.2f, 0.12f, 0.05f, 1f);

        private EnemyHealth boss;
        private float timer, front;
        private bool active, running, warned, hitThisWave;
        private Transform visual;

        public void Configure(float left, float right) { fromX = left; toX = right; }

        private void OnEnable() => GameplayEvents.BossStarted += OnBoss;
        private void OnDisable() => GameplayEvents.BossStarted -= OnBoss;

        private void OnBoss(string bossName, EnemyHealth health)
        {
            if (health == null) return;
            float x = health.transform.position.x;
            if (x >= fromX - 4f && x <= toX + 4f) boss = health;
        }

        private void Start()
        {
            Build();
            visual.gameObject.SetActive(false);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (!active)
            {
                if (boss != null && boss.IsAlive && boss.Health01 <= fromHealth01)
                {
                    active = true;
                    timer = 2f;
                    if (!warned) { warned = true; GameplayEvents.RaiseMessage(message); }
                }
                return;
            }
            if (boss == null || !boss.IsAlive) { active = running = false; visual.gameObject.SetActive(false); return; }

            if (!running)
            {
                timer -= dt;
                if (timer <= warning && timer + dt > warning) { Sfx.Effect("Lombriz_Retumbo", 1f); Shake(0.15f, warning); }
                if (timer > 0f) return;
                running = true;
                hitThisWave = false;
                front = fromX - 2f;
                visual.gameObject.SetActive(true);
                return;
            }

            front += speed * dt;
            visual.position = new Vector3(front, transform.position.y, 0f);
            float t = Time.time;
            visual.localScale = new Vector3(1f, 1f + Mathf.Sin(t * 12f) * 0.06f, 1f);

            PlayerController player = PlayerController.Current;
            if (!hitThisWave && player != null && player.Health.IsAlive)
            {
                Vector2 feet = player.Position;
                if (Mathf.Abs(feet.x - front) < 1.1f && feet.y < transform.position.y + height)
                {
                    hitThisWave = true;
                    DamageInfo info = DamageInfo.Create(damage, DamageKind.Contact, Team.Neutral, new Vector2(front - 1f, feet.y));
                    info.blockable = false;
                    info.knockback = 10f;
                    player.Health.ApplyDamage(info);
                    player.Motor.SetVelocity(new Vector2(9f, 7f));
                }
            }

            if (front > toX + 3f)
            {
                running = false;
                timer = interval;
                visual.gameObject.SetActive(false);
            }
        }

        private void Build()
        {
            visual = new GameObject("OlaDeMasa").transform;
            visual.SetParent(transform, false);
            Sprite c = CodeSprites.Circle, sq = CodeSprites.Square;
            CodeSprites.Add(visual, sq, outline, 33, new Vector2(-3f, height * 0.45f), new Vector2(6.2f, height * 0.9f + 0.1f));
            CodeSprites.Add(visual, sq, fill, 34, new Vector2(-3f, height * 0.45f - 0.05f), new Vector2(6f, height * 0.9f));
            CodeSprites.Add(visual, c, outline, 33, new Vector2(0f, height * 0.55f), new Vector2(height * 1.3f + 0.14f, height * 1.1f + 0.14f));
            CodeSprites.Add(visual, c, fill, 34, new Vector2(0f, height * 0.55f), new Vector2(height * 1.3f, height * 1.1f));
            for (int i = 0; i < 5; i++)
                CodeSprites.Add(visual, c, crest, 35, new Vector2(-i * 1.2f, height * 0.95f), new Vector2(1.1f, 0.5f));
            CodeSprites.Add(visual, c, new Color(1f, 0.9f, 0.75f, 0.6f), 36, new Vector2(-0.2f, height * 0.85f), new Vector2(0.5f, 0.2f));
        }

        private static void Shake(float strength, float duration)
        {
            Camera cam = Camera.main;
            if (cam != null && cam.TryGetComponent(out GameCamera gameCamera)) gameCamera.Shake(strength, duration);
        }
    }
}
