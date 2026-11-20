using System.Collections.Generic;
using HealingKnight.Core.Combat;
using HealingKnight.Core.Damage;
using HealingKnight.Core.Pooling;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>
    /// LINFOCITO B de la arena del Epstein-Barr. Sano: flota en su lugar y le dispara anticuerpos al virus. Infectado:
    /// se pone morado, te persigue un poco y te dispara; tiene una "capa" de infección (EnemyHealth) que hay que bajarle
    /// a balazos para curarlo.
    /// </summary>
    public class InfectableLymphocyte : MonoBehaviour
    {
        private EpsteinBarrBoss boss;
        private Vector2 home;
        private SpriteRenderer body, nucleus;
        private Sprite healthyArt, infectedArt;   // dibujos (Art/Aliados/Resources/Aliados/LinfocitoB*.png), si existen
        private Transform eyes, angry;
        private EnemyHealth infection;
        private float timer, seed;

        public bool Infected => infection != null;

        public void Setup(EpsteinBarrBoss owner, Vector2 homePos)
        {
            boss = owner;
            home = homePos;
        }

        private void Start()
        {
            seed = Random.value * 10f;
            timer = 1.6f + seed * 0.1f;
            Sprite c = CodeSprites.Circle;
            healthyArt = CodeSprites.ResourceArt("Aliados/LinfocitoB");
            if (healthyArt != null)
            {
                infectedArt = CodeSprites.ResourceArt("Aliados/LinfocitoB_Infectado");
                body = CodeSprites.AddArt(transform, healthyArt, 1.6f, 31, Vector2.zero, "Cuerpo");   // con sus anticuerpos alrededor
                eyes = new GameObject("Ojos").transform;
                eyes.SetParent(transform, false);
                angry = new GameObject("Enojado").transform;
                angry.SetParent(transform, false);
                return;
            }
            CodeSprites.Add(transform, c, new Color(0.2f, 0.1f, 0.35f), 30, Vector2.zero, Vector2.one * 1.05f, "Contorno");
            body = CodeSprites.Add(transform, c, new Color(0.65f, 0.75f, 1f), 31, Vector2.zero, Vector2.one * 0.92f, "Cuerpo");
            nucleus = CodeSprites.Add(transform, c, new Color(0.45f, 0.4f, 0.9f), 32, new Vector2(0.05f, -0.04f), Vector2.one * 0.6f, "Nucleo");
            eyes = new GameObject("Ojos").transform;
            eyes.SetParent(transform, false);
            foreach (float ex in new[] { -0.13f, 0.15f })
            {
                CodeSprites.Add(eyes, c, Color.white, 33, new Vector2(ex, 0.08f), new Vector2(0.15f, 0.18f), "Ojo");
                CodeSprites.Add(eyes, c, new Color(0.15f, 0.1f, 0.3f), 34, new Vector2(ex + 0.01f, 0.06f), new Vector2(0.07f, 0.1f), "Pupila");
            }
            angry = new GameObject("Enojado").transform;
            angry.SetParent(transform, false);
            foreach (float ex in new[] { -0.13f, 0.15f })
                CodeSprites.Add(angry, c, new Color(1f, 0.2f, 0.25f), 35, new Vector2(ex, 0.07f), new Vector2(0.12f, 0.12f), "OjoRojo");
            angry.gameObject.SetActive(false);
        }

        private void Update()
        {
            float dt = Time.deltaTime, tt = Time.time + seed;
            PlayerController player = PlayerController.Current;
            Vector2 target = home + new Vector2(Mathf.Sin(tt * 0.8f) * 0.8f, Mathf.Sin(tt * 1.7f) * 0.3f);
            if (Infected && player != null)
                target = Vector2.Lerp(home, player.Position + new Vector2(Mathf.Sin(tt) * 3f, 2.5f), 0.35f);
            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-3f * dt));
            if (infection != null) infection.transform.position = transform.position;

            timer -= dt;
            if (timer > 0f) return;
            if (!Infected)
            {
                timer = 1.6f;
                if (boss == null) return;
                EnemyHealth h = boss.GetComponent<EnemyHealth>();
                if (h == null || !h.IsAlive) return;
                var go = new GameObject("Anticuerpo");
                go.transform.position = transform.position;
                go.AddComponent<Antibody>().Launch(h, 4f);
            }
            else if (player != null && player.Health.IsAlive)
            {
                timer = 1.7f;
                Vector2 dir = (player.Position + Vector2.up * 0.8f - (Vector2)transform.position).normalized;
                var go = new GameObject("Particula");
                go.transform.position = transform.position;
                go.AddComponent<SimpleShot>().Launch(dir * 6f, 8f);
            }
        }

        /// <summary>El virus lo infectó: se pone morado y le sale una capa de infección que hay que bajarle.</summary>
        public void Infect()
        {
            if (Infected || boss == null || boss.InfectedData == null) return;
            var go = new GameObject("Infeccion");
            go.transform.position = transform.position;
            go.layer = PhysicsLayers.Enemy;
            // (un dibujo vacío para el destello: el dibujo de verdad es el del linfocito)
            infection = go.AddComponent<EnemyHealth>();
            infection.Configure(boss.InfectedData, boss.Table);
            go.AddComponent<Hurtbox>().SetBox(Vector2.zero, new Vector2(1.1f, 1.1f));
            infection.Died += OnCured;
            if (healthyArt != null)
            {
                if (infectedArt != null) body.sprite = infectedArt;
                else body.color = new Color(0.8f, 0.55f, 0.95f);   // sin dibujo de infectado: el sano pintado de morado
            }
            else
            {
                body.color = new Color(0.7f, 0.4f, 0.85f);
                nucleus.color = new Color(0.4f, 0.1f, 0.5f);
            }
            eyes.gameObject.SetActive(false);
            angry.gameObject.SetActive(true);
            timer = 1.2f;
        }

        private void OnCured(EnemyHealth _)
        {
            infection = null;
            Heal();
            if (boss != null) boss.Cured(this);
        }

        public void CureSilently()
        {
            if (infection != null)
            {
                infection.Died -= OnCured;
                Destroy(infection.gameObject);
                infection = null;
            }
            Heal();
        }

        private void Heal()
        {
            if (body == null) return;
            if (healthyArt != null)
            {
                body.sprite = healthyArt;
                body.color = Color.white;
            }
            else
            {
                body.color = new Color(0.65f, 0.75f, 1f);
                nucleus.color = new Color(0.45f, 0.4f, 0.9f);
            }
            eyes.gameObject.SetActive(true);
            angry.gameObject.SetActive(false);
            timer = 1.6f;
        }
    }
}
