using System.Collections.Generic;
using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// LLUVIA DE AZÚCAR (Boca): caen dulces en [minX, maxX]. Si un S. mutans se come uno, CRECE: se cura y se hace
    /// más grande (biología: fermenta el azúcar y suelta el ácido que hace la caries). Dispárale a los dulces para
    /// romperlos antes de que lleguen a ellas. Los dulces en el piso duran unos segundos.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class SugarRain : MonoBehaviour
    {
        [SerializeField] private float minX;
        [SerializeField] private float maxX = 20f;
        [SerializeField] private float topY = 9f;
        [SerializeField] private float interval = 1.6f;
        [SerializeField] private EnemyData eater;
        [SerializeField] private Sprite art;
        [SerializeField, TextArea] private string firstFeed = "¡El S. mutans se comió un dulce y creció! Rompe los dulces antes de que lleguen a él.";

        private class Candy { public Transform root; public float vy, life; public bool alive, landed; public Color color; }
        private readonly List<Candy> candies = new List<Candy>();
        private float timer;
        private bool told;
        private static readonly Color[] Colors =
        {
            new Color(1f, 0.4f, 0.6f), new Color(0.5f, 0.85f, 1f), new Color(1f, 0.85f, 0.3f), new Color(0.6f, 1f, 0.5f)
        };

        public void Configure(float from, float to, float top, float period, EnemyData enemy, Sprite drawing)
        {
            minX = from; maxX = to; topY = top; interval = period; eater = enemy; art = drawing;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            PlayerController player = PlayerController.Current;
            bool near = player != null && player.Position.x > minX - 6f && player.Position.x < maxX + 6f;
            timer -= dt;
            if (near && timer <= 0f) { timer = interval * Random.Range(0.7f, 1.3f); Drop(); }

            var shots = Projectile.Flying;
            foreach (Candy c in candies)
            {
                if (!c.alive) continue;
                Vector3 p = c.root.position;
                if (!c.landed)
                {
                    c.vy -= 14f * dt;
                    p.y += c.vy * dt;
                    c.root.Rotate(0f, 0f, 200f * dt);
                    if (p.y <= transform.position.y + 0.25f) { p.y = transform.position.y + 0.25f; c.landed = true; c.life = 6f; }
                    c.root.position = p;
                }
                else
                {
                    c.life -= dt;
                    if (c.life <= 0f) { Kill(c); continue; }
                }

                // Disparos del jugador lo rompen.
                for (int i = shots.Count - 1; i >= 0; i--)
                {
                    Projectile s = shots[i];
                    if (s == null || s.SourceTeam != Team.Player || Vector2.Distance(s.Position, p) > 0.55f) continue;
                    s.Consume();
                    Sfx.EffectAt("Azucar_Rompe", p, 0.6f);
                    Kill(c);
                    break;
                }
                if (!c.alive) continue;

                // Un S. mutans cerca se lo come.
                if (eater == null) continue;
                foreach (EnemyHealth e in EnemyHealth.Active)
                {
                    if (e == null || !e.IsAlive || e.Data != eater) continue;
                    Vector2 ep = e.transform.position;
                    if (Mathf.Abs(ep.x - p.x) > 0.9f || Mathf.Abs(ep.y + 0.5f - p.y) > 1.3f) continue;
                    e.Heal(25f);
                    float s = Mathf.Min(1.6f, e.transform.localScale.y * 1.15f);
                    e.transform.localScale = new Vector3(Mathf.Sign(e.transform.localScale.x) * s, s, 1f);
                    Sfx.EffectAt("Acido_Burbujeo", p, 0.8f);
                    if (!told && !string.IsNullOrEmpty(firstFeed)) { told = true; GameplayEvents.RaiseMessage(firstFeed); }
                    Kill(c);
                    break;
                }
            }
        }

        private void Drop()
        {
            Candy c = candies.Find(x => !x.alive);
            if (c == null)
            {
                c = new Candy { root = new GameObject("Dulce").transform };
                c.root.SetParent(transform, true);
                c.color = Colors[candies.Count % Colors.Length];
                if (art != null) CodeSprites.Art(c.root, art, 0.7f, 22, Vector2.zero);
                else
                {
                    Sprite circle = CodeSprites.Circle;
                    CodeSprites.Add(c.root, circle, new Color(0.25f, 0.1f, 0.15f), 20, Vector2.zero, new Vector2(0.6f, 0.6f));
                    CodeSprites.Add(c.root, circle, c.color, 21, Vector2.zero, new Vector2(0.5f, 0.5f));
                    CodeSprites.Add(c.root, CodeSprites.Square, Color.white, 22, Vector2.zero, new Vector2(0.5f, 0.1f));
                    for (int s = -1; s <= 1; s += 2)   // envoltura
                        CodeSprites.Add(c.root, circle, Color.Lerp(c.color, Color.white, 0.4f), 20, new Vector2(s * 0.38f, 0f), new Vector2(0.28f, 0.4f));
                }
                candies.Add(c);
            }
            c.alive = true;
            c.landed = false;
            c.vy = 0f;
            c.root.position = new Vector3(Random.Range(minX, maxX), topY, 0f);
            c.root.gameObject.SetActive(true);
        }

        private static void Kill(Candy c)
        {
            c.alive = false;
            c.root.gameObject.SetActive(false);
        }
    }
}
