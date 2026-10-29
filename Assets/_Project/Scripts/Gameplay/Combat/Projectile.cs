using System;
using HealingKnight.Core.Combat;
using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Movement;
using UnityEngine;

namespace HealingKnight.Gameplay.Combat
{
    /// <summary>Parámetros de un disparo.</summary>
    public struct ProjectileShot
    {
        public Vector2 origin;
        public Vector2 direction;       // normalizada (8 direcciones para el jugador)
        public float speed;
        public float range;
        public float damage;
        public float knockback;
        public float radius;
        public Team sourceTeam;
        public Team targetTeam;
        public MedicationData medication; // null en proyectiles enemigos
        public Color color;
        public LayerMask groundMask;     // se destruye al tocar el escenario
        public float gravity;            // > 0 = cae en arco (ácido, gotitas)
        public float slowMultiplier;     // > 0 = al pegarle al jugador lo frena (poliovirus)
        public float slowDuration;
    }

    /// <summary>
    /// Cápsula de medicamento o proyectil enemigo: viaja en línea recta en la dirección del disparo.
    /// Si el medicamento es ineficaz, rebota en gris (GDD 5.7). Lo administra un pool.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Rebote (medicamento ineficaz)")]
        [SerializeField] private float bounceDuration = 0.3f;
        [SerializeField] private float bounceSpeedFactor = 0.4f;
        [SerializeField] private Color bounceColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        private ProjectileShot shot;
        private float travelled;
        private bool bouncing;
        private float bounceTimer;
        private Vector2 bounceVelocity;
        private Action<Projectile> onFinished;
        private bool finished;
        private Vector2 velocity;

        public static event Action<Projectile, HitResult> AnyHit;

        /// <summary>De quién es el disparo (para cargar las bombas solo con disparos del jugador).</summary>
        public Team SourceTeam => shot.sourceTeam;

        /// <summary>Sonido del impacto de las cápsulas del jugador: el del medicamento o el rebote si fue ineficaz.</summary>
        private void PlayHitSound(HitResult result)
        {
            if (shot.sourceTeam != Team.Player || result.outcome == HitOutcome.Ignored) return;
            Audio.CombatAudio sounds = Audio.CombatAudio.Instance;
            float jitter = sounds != null ? sounds.pitchJitter : 0.06f;
            if (result.outcome == HitOutcome.Resisted)
            {
                AudioClip resist = sounds != null && sounds.resistSound != null ? sounds.resistSound : Audio.Sfx.Load("Rebote_Ineficaz");
                Audio.Sfx.Play(resist, sounds != null ? sounds.resistVolume : 0.55f, jitter);
            }
            else if (shot.medication != null)
            {
                Audio.Sfx.Play(Audio.Sfx.HitSound(shot.medication), sounds != null ? sounds.hitVolume : 0.6f, jitter);
            }
        }

        public void Launch(ProjectileShot data, Action<Projectile> finishedCallback)
        {
            shot = data;
            onFinished = finishedCallback;
            travelled = 0f;
            velocity = data.direction * data.speed;
            bouncing = false;
            finished = false;

            transform.position = new Vector3(data.origin.x, data.origin.y, 0f);
            float angle = Mathf.Atan2(data.direction.y, data.direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = data.color;
                if (data.medication != null && data.medication.capsuleSprite != null)
                    spriteRenderer.sprite = data.medication.capsuleSprite;
            }
        }

        private void Update()
        {
            if (finished) return;
            float dt = Time.deltaTime;

            if (bouncing)
            {
                bounceTimer += dt;
                bounceVelocity += Vector2.down * 12f * dt;
                transform.position += (Vector3)(bounceVelocity * dt);
                transform.Rotate(0f, 0f, 720f * dt);
                if (spriteRenderer != null)
                {
                    Color c = bounceColor;
                    c.a = Mathf.Clamp01(1f - bounceTimer / bounceDuration);
                    spriteRenderer.color = c;
                }
                if (bounceTimer >= bounceDuration) Finish();
                return;
            }

            if (shot.gravity > 0f)
            {
                // En arco: cae y gira hacia donde va.
                velocity += Vector2.down * shot.gravity * dt;
                shot.direction = velocity.normalized;
                transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
            }
            float step = (shot.gravity > 0f ? velocity.magnitude : shot.speed) * dt;
            Vector2 previous = transform.position;
            Vector2 next = previous + shot.direction * step;
            transform.position = next;
            travelled += step;

            Hurtbox target = Hurtbox.FirstInCircle(shot.targetTeam, next, shot.radius);
            // Los proyectiles ENEMIGOS también pegan en lo neutral (neuronas sanas). Los del jugador las atraviesan.
            if (target == null && shot.sourceTeam == Team.Enemy) target = Hurtbox.FirstInCircle(Team.Neutral, next, shot.radius);
            if (target != null)
            {
                DamageInfo info = DamageInfo.Create(shot.damage, DamageKind.Projectile, shot.sourceTeam, previous);
                info.knockback = shot.knockback;
                info.medication = shot.medication;

                HitResult result = target.Damageable.ApplyDamage(info);
                AnyHit?.Invoke(this, result);
                PlayHitSound(result);
                if (shot.slowMultiplier > 0f && result.Landed)
                {
                    Player.PlayerController pc = target.GetComponent<Player.PlayerController>();
                    if (pc != null) pc.ApplySlow(shot.slowMultiplier, shot.slowDuration);
                }

                if (result.outcome == HitOutcome.Resisted) StartBounce();
                else if (result.outcome != HitOutcome.Ignored) Finish();
                return;
            }

            if (shot.groundMask.value != 0 && Physics2D.OverlapCircle(next, shot.radius * 0.6f, shot.groundMask) != null)
            {
                Finish();
                return;
            }

            Rect b = WalkableArea.LevelBounds;
            bool outOfLevel = next.x < b.xMin - 2f || next.x > b.xMax + 2f || next.y < b.yMin - 2f || next.y > b.yMax + 10f;
            if (travelled >= shot.range || outOfLevel) Finish();
        }

        private void StartBounce()
        {
            bouncing = true;
            bounceTimer = 0f;
            bounceVelocity = (-shot.direction + Vector2.up * 0.8f) * shot.speed * bounceSpeedFactor;
        }

        private void Finish()
        {
            if (finished) return;
            finished = true;
            if (onFinished != null) onFinished(this);
            else gameObject.SetActive(false);
        }
    }
}
