using System.Collections.Generic;
using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// OLA DE SALIVA (Boca): cada cierto tiempo una ola de saliva barre [minX, maxX] de izquierda a derecha.
    /// Al pasar te CURA y les hace daño a las bacterias (la saliva neutraliza el ácido y arrastra a los microbios).
    /// </summary>
    public class SalivaWave : MonoBehaviour
    {
        [SerializeField] private float minX;
        [SerializeField] private float maxX = 30f;
        [SerializeField] private float period = 14f;
        [SerializeField] private float speed = 9f;
        [SerializeField] private float heal = 12f;
        [SerializeField] private float enemyDamage = 20f;
        [SerializeField, TextArea] private string message = "¡Ola de SALIVA! Te cura y debilita a las bacterias: la saliva neutraliza el ácido.";

        private Transform wave;
        private float timer, x;
        private bool running, said, healedPlayer;
        private readonly HashSet<EnemyHealth> hit = new HashSet<EnemyHealth>();

        public void Configure(float from, float to, float cycle) { minX = from; maxX = to; period = cycle; }

        private void Start()
        {
            timer = period * 0.5f;
            wave = new GameObject("Ola").transform;
            wave.SetParent(transform, false);
            Sprite c = CodeSprites.Circle, sq = CodeSprites.Square;
            CodeSprites.Add(wave, sq, new Color(0.7f, 0.9f, 1f, 0.45f), 35, new Vector2(-1.5f, 1.3f), new Vector2(3f, 2.6f));
            CodeSprites.Add(wave, c, new Color(0.75f, 0.93f, 1f, 0.6f), 36, new Vector2(0f, 1.6f), new Vector2(2.2f, 3.2f));
            CodeSprites.Add(wave, c, new Color(1f, 1f, 1f, 0.85f), 37, new Vector2(0.3f, 3f), new Vector2(1.4f, 0.6f));
            for (int i = 0; i < 4; i++)
                CodeSprites.Add(wave, c, new Color(1f, 1f, 1f, 0.8f), 37, new Vector2(-0.5f - i * 0.6f, 0.4f + (i % 2) * 1.2f), new Vector2(0.25f, 0.25f));
            wave.gameObject.SetActive(false);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            PlayerController p = PlayerController.Current;
            if (!running)
            {
                bool near = p != null && p.Position.x > minX - 4f && p.Position.x < maxX + 4f;
                if (near) timer -= dt;
                if (timer <= 0f)
                {
                    running = true;
                    x = minX - 2f;
                    hit.Clear();
                    healedPlayer = false;
                    wave.gameObject.SetActive(true);
                    Sfx.Effect("Saliva_Ola", 0.8f);
                    if (!said && !string.IsNullOrEmpty(message)) { said = true; GameplayEvents.RaiseMessage(message); }
                }
                return;
            }

            x += speed * dt;
            wave.position = new Vector3(x, transform.position.y, 0f);
            wave.localScale = new Vector3(1f, 1f + Mathf.Sin(Time.time * 8f) * 0.06f, 1f);

            if (p != null && p.Health.IsAlive && !healedPlayer && Mathf.Abs(p.Position.x - x) < 1.2f && p.Position.y < transform.position.y + 3.5f)
            {
                healedPlayer = true;
                p.Health.Heal(heal);
            }
            foreach (EnemyHealth e in EnemyHealth.Active)
            {
                if (e == null || !e.IsAlive || hit.Contains(e)) continue;
                Vector2 ep = e.transform.position;
                if (Mathf.Abs(ep.x - x) > 1.2f || ep.y > transform.position.y + 3.5f) continue;
                hit.Add(e);
                if (e.Data != null && (e.Data.threat == Core.Combat.ThreatType.Bacteria))
                {
                    DamageInfo info = DamageInfo.Create(enemyDamage, DamageKind.Environment, Team.Neutral, new Vector2(x, ep.y));
                    e.ApplyDamage(info);
                }
            }

            if (x > maxX + 2f)
            {
                running = false;
                timer = period;
                wave.gameObject.SetActive(false);
            }
        }
    }
}
