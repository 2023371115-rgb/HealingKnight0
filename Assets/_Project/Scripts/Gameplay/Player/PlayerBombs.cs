using System;
using HealingKnight.Core.Damage;
using HealingKnight.Core.Pooling;
using HealingKnight.Gameplay.Combat;
using UnityEngine;

namespace HealingKnight.Gameplay.Player
{
    /// <summary>
    /// Bombas de medicamento (las granadas de Metal Slug): se lanzan en arco hacia donde miras
    /// y liberan el medicamento seleccionado en un área.
    /// Empiezas con unas cuantas y ganas más peleando bien, como las cartas de Cuphead:
    /// el daño EFECTIVO (medicamento correcto), los cuchillazos y los parry llenan una barra;
    /// al llenarse ganas una bomba.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerBombs : MonoBehaviour
    {
        [SerializeField] private MedicineBomb bombPrefab;
        [SerializeField] private LayerMask groundMask = 1 << 6;

        private PlayerController controller;
        private ComponentPool<MedicineBomb> pool;
        private PlayerShield shield;
        private int count;
        private float charge;
        private float cooldown;

        public int Count => count;
        /// <summary>Barra de la siguiente bomba (0..1).</summary>
        public float Charge01 => charge;
        public event Action<int> CountChanged;
        public event Action<float> ChargeChanged;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            shield = GetComponent<PlayerShield>();
        }

        private void OnEnable()
        {
            Projectile.AnyHit += OnProjectileHit;
            if (shield != null) shield.Parried += OnParried;
        }

        private void OnDisable()
        {
            Projectile.AnyHit -= OnProjectileHit;
            if (shield != null) shield.Parried -= OnParried;
        }

        private void OnProjectileHit(Projectile projectile, HitResult result)
        {
            if (projectile.SourceTeam != Team.Player || !result.Landed) return;
            AddCharge(result.damageDealt * controller.Config.bombChargePerDamage);
        }

        private void OnParried() => AddCharge(controller.Config.bombChargePerParry);

        /// <summary>Daño hecho con el cuchillo (lo avisa PlayerShooter).</summary>
        public void OnMeleeDamage(float damage) => AddCharge(damage * controller.Config.bombChargePerDamage);

        public void AddCharge(float amount)
        {
            if (amount <= 0f) return;
            PlayerConfig c = controller.Config;
            if (count >= c.maxBombs)
            {
                charge = 1f; // barra llena esperando a que gastes una
                ChargeChanged?.Invoke(charge);
                return;
            }

            charge += amount;
            while (charge >= 1f && count < c.maxBombs)
            {
                charge -= 1f;
                Add(1);
            }
            if (count >= c.maxBombs) charge = Mathf.Min(charge, 1f);
            ChargeChanged?.Invoke(charge);
        }

        private void Start()
        {
            Refill();
            if (bombPrefab != null) pool = new ComponentPool<MedicineBomb>(bombPrefab, 3, 6);
        }

        private void OnDestroy() => pool?.Clear();

        private void Update()
        {
            if (cooldown > 0f) cooldown -= Time.deltaTime;
        }

        /// <summary>Al empezar o reaparecer: al menos las bombas iniciales (no te quita las que ganaste).</summary>
        public void Refill()
        {
            count = Mathf.Max(count, controller.Config.bombsPerLevel);
            CountChanged?.Invoke(count);
            ChargeChanged?.Invoke(charge);
        }

        public void Add(int amount)
        {
            count = Mathf.Min(count + amount, controller.Config.maxBombs);
            CountChanged?.Invoke(count);
        }

        public bool TryThrow()
        {
            if (count <= 0 || cooldown > 0f || pool == null) return false;
            PlayerConfig c = controller.Config;
            int facing = controller.Motor.Facing;

            MedicineBomb bomb = pool.Get();
            Vector2 origin = controller.Motor.Position + new Vector2(facing * 0.4f, 1.3f);
            Vector2 velocity = new Vector2(facing * c.bombThrowVelocity.x + controller.Motor.Velocity.x * 0.5f, c.bombThrowVelocity.y);
            bomb.Launch(origin, velocity, c.bombDamage, c.bombBlastRadius, controller.Shooter != null ? controller.Shooter.Current : null,
                groundMask, b => pool.Release(b));

            count--;
            cooldown = c.bombCooldown;
            CountChanged?.Invoke(count);
            // Si la barra estaba llena esperando lugar, ahora sí se convierte en bomba.
            if (charge >= 1f) { charge = 0f; Add(1); ChargeChanged?.Invoke(charge); }
            return true;
        }
    }
}
