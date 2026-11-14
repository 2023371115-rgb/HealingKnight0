using HealingKnight.Gameplay.Audio;
using System;
using System.Collections.Generic;
using HealingKnight.Core.Combat;
using HealingKnight.Core.Damage;
using UnityEngine;

namespace HealingKnight.Gameplay.Combat
{
    /// <summary>
    /// Bomba de medicamento (la "granada" de Metal Slug): sale en arco, rebota una vez en el piso
    /// y explota liberando el medicamento seleccionado en un área. Cada enemigo del área recibe daño
    /// según la tabla de efectividad, así que también hay que elegir bien qué medicamento lanzar.
    /// </summary>
    public class MedicineBomb : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private SpriteRenderer blastRenderer;
        [SerializeField] private float gravity = 30f;
        [SerializeField] private float radius = 0.25f;
        [SerializeField] private float fuseTime = 1.6f;
        [SerializeField] private float blastDuration = 0.25f;
        [SerializeField] private float neuroprotectionRadius = 3.5f;
        [SerializeField] private float neuroprotectionSeconds = 8f;

        private Vector2 velocity;
        private float damage;
        private float blastRadius;
        private MedicationData medication;
        private LayerMask groundMask;
        private Action<MedicineBomb> onFinished;
        private int bounces;
        private float timer;
        private float blastTimer = -1f;
        private readonly List<Hurtbox> hits = new List<Hurtbox>();

        public void Launch(Vector2 origin, Vector2 initialVelocity, float bombDamage, float bombBlastRadius,
            MedicationData med, LayerMask ground, Action<MedicineBomb> finished)
        {
            transform.position = origin;
            velocity = initialVelocity;
            damage = bombDamage;
            blastRadius = bombBlastRadius;
            medication = med;
            groundMask = ground;
            onFinished = finished;
            bounces = 0;
            timer = fuseTime;
            blastTimer = -1f;

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.color = med != null ? med.color : Color.white;
            }
            if (blastRenderer != null) blastRenderer.enabled = false;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (blastTimer >= 0f)
            {
                blastTimer += dt;
                if (blastRenderer != null)
                {
                    float k = blastTimer / blastDuration;
                    blastRenderer.transform.localScale = Vector3.one * (blastRadius * 2f * Mathf.Lerp(0.6f, 1.1f, k));
                    Color c = blastRenderer.color;
                    c.a = Mathf.Clamp01(1f - k);
                    blastRenderer.color = c;
                }
                if (blastTimer >= blastDuration) Finish();
                return;
            }

            timer -= dt;
            velocity += Vector2.down * gravity * dt;
            Vector2 next = (Vector2)transform.position + velocity * dt;
            transform.position = next;
            transform.Rotate(0f, 0f, -540f * dt);

            // Explota al tocar a un enemigo.
            if (Hurtbox.FirstInCircle(Team.Enemy, next, radius) != null)
            {
                Explode();
                return;
            }

            // Rebota una vez en el piso y explota al segundo contacto.
            if (velocity.y < 0f && Physics2D.OverlapCircle(next, radius, groundMask) != null)
            {
                if (bounces == 0)
                {
                    bounces++;
                    velocity = new Vector2(velocity.x * 0.5f, -velocity.y * 0.45f);
                }
                else
                {
                    Explode();
                    return;
                }
            }

            if (timer <= 0f) Explode();
        }

        private void Explode()
        {
            blastTimer = 0f;
            Sfx.EffectAt("Bomba_Explota", transform.position);
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            if (blastRenderer != null)
            {
                blastRenderer.enabled = true;
                Color c = medication != null ? medication.color : Color.white;
                c.a = 0.8f;
                blastRenderer.color = c;
            }

            // Bomba de anticonvulsivo: además deja un campo neuroprotector (las neuronas sanas no reciben daño).
            if (medication != null && medication.calmsNeurons)
                Level.NeuroprotectionField.Create(transform.position, neuroprotectionRadius, neuroprotectionSeconds,
                    blastRenderer != null ? blastRenderer.sprite : null, medication.color);

            Hurtbox.QueryCircle(Team.Enemy, transform.position, blastRadius, hits);
            foreach (Hurtbox h in hits)
            {
                DamageInfo info = DamageInfo.Create(damage, DamageKind.Projectile, Team.Player, transform.position);
                info.knockback = 5f;
                info.medication = medication;
                h.Damageable.ApplyDamage(info);
            }
        }

        private void Finish()
        {
            if (onFinished != null) onFinished(this);
            else gameObject.SetActive(false);
        }
    }
}
