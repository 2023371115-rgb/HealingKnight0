using System;
using System.Collections.Generic;
using HealingKnight.Core.Combat;
using HealingKnight.Core.Combat2D;
using HealingKnight.Core.Damage;
using HealingKnight.Core.Flow;
using HealingKnight.Core.Pooling;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Movement;
using UnityEngine;

namespace HealingKnight.Gameplay.Player
{
    /// <summary>
    /// Brazo-pistola (estilo Metal Slug):
    /// - Dispara cápsulas del medicamento seleccionado en la dirección a la que apuntas.
    /// - Si al disparar hay un enemigo pegado enfrente, en lugar de disparar da un cuchillazo automático.
    /// - Cambia entre medicamentos: genéricos + la cápsula regional del sistema actual.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerShooter : MonoBehaviour
    {
        [SerializeField] private Projectile capsulePrefab;
        [SerializeField] private LayerMask groundMask = 1 << 6;

        [Tooltip("Brazo-pistola: rota hacia donde apuntas y se pinta del color del medicamento.")]
        [SerializeField] private Transform armPivot;
        [SerializeField] private SpriteRenderer tintedArm;

        [Tooltip("Piezas que se pintan del color del medicamento (ojos, cruz, rueda del casco, luz del cañón...). " +
                 "Dibújalas en BLANCO o gris claro en su propia capa: el color se multiplica encima.")]
        [SerializeField] private SpriteRenderer[] medicationColorParts;

        [Tooltip("Para probar una escena sin pasar por el mapa: cápsula regional a usar si no hay sistema actual.")]
        [SerializeField] private MedicationData fallbackRegionalMedication;

        private PlayerController controller;
        private ComponentPool<Projectile> pool;
        private readonly List<MedicationData> loadout = new List<MedicationData>();
        private readonly List<Hurtbox> meleeHits = new List<Hurtbox>();
        private int selectedIndex;
        private float fireCooldown;
        private float meleeCooldown;

        public event Action<MedicationData> MedicationChanged;
        public event Action Fired;
        public event Action Stabbed;

        public MedicationData Current => loadout.Count > 0 ? loadout[selectedIndex] : null;
        public IReadOnlyList<MedicationData> Loadout => loadout;
        public int SelectedIndex => selectedIndex;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
        }

        private void Start()
        {
            BuildLoadout();

            if (capsulePrefab == null)
            {
                Debug.LogError("[PlayerShooter] Falta asignar el prefab de la cápsula.", this);
                return;
            }

            int max = Mathf.Max(1, controller.Config.maxActiveCapsules);
            pool = new ComponentPool<Projectile>(capsulePrefab, max, max * 2);
        }

        private void OnDestroy()
        {
            pool?.Clear();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (fireCooldown > 0f) fireCooldown -= dt;
            if (meleeCooldown > 0f) meleeCooldown -= dt;
            UpdateArm();
        }

        private void UpdateArm()
        {
            if (armPivot == null) return;
            // El Visual está volteado cuando mira a la izquierda: el ángulo se calcula en su espacio local.
            Vector2 aim = controller.Aim;
            float angle = Mathf.Atan2(aim.y, aim.x * controller.Motor.Facing) * Mathf.Rad2Deg;
            armPivot.localRotation = Quaternion.Euler(0f, 0f, angle);

            // El brazo cuelga del cuerpo, que ya se encoge entero: aquí el hombro va a su altura normal.
            PlayerConfig c = controller.Config;
            Vector3 p = armPivot.localPosition;
            armPivot.localPosition = new Vector3(p.x, c.muzzleHeight, p.z);
        }

        // ---------------------------------------------------------------- medicamentos

        public void BuildLoadout()
        {
            loadout.Clear();
            foreach (MedicationData m in controller.Config.genericMedications)
            {
                if (m != null && !loadout.Contains(m)) loadout.Add(m);
            }

            MedicationData regional = GameSession.CurrentSystem != null
                ? GameSession.CurrentSystem.regionalMedication
                : fallbackRegionalMedication;

            if (regional != null && !loadout.Contains(regional)) loadout.Add(regional);

            selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, loadout.Count - 1));
            NotifyChanged();
        }

        public void CycleMedication(int step)
        {
            if (loadout.Count == 0) return;
            selectedIndex = (selectedIndex + step + loadout.Count) % loadout.Count;
            NotifyChanged();
        }

        public void SelectMedication(int index)
        {
            if (index < 0 || index >= loadout.Count) return;
            selectedIndex = index;
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            if (Current != null)
            {
                if (tintedArm != null) tintedArm.color = Current.color;
                if (medicationColorParts != null)
                    foreach (SpriteRenderer part in medicationColorParts)
                        if (part != null) part.color = Current.color;
            }
            MedicationChanged?.Invoke(Current);
        }

        /// <summary>Recompensa del parry: el siguiente disparo sale al instante.</summary>
        public void ResetCooldown() => fireCooldown = 0f;

        // ---------------------------------------------------------------- ataque

        /// <summary>Dispara, o da un cuchillazo si hay un enemigo pegado. 'pressed' = se acaba de presionar.</summary>
        public void TryAttack(Vector2 aim, bool crouching, bool pressed)
        {
            if (TryStab(crouching)) return;
            TryShoot(aim, crouching);
        }

        private bool TryStab(bool crouching)
        {
            if (meleeCooldown > 0f) return false;
            PlayerConfig c = controller.Config;
            CharacterMotor2D motor = controller.Motor;

            float height = crouching ? 0.5f : 0.95f;
            Vector2 center = motor.Position + new Vector2(motor.Facing * c.meleeReach * 0.5f, height);
            Rect area = HitMath.FromCenter(center, new Vector2(c.meleeReach, crouching ? 0.9f : 1.5f));

            Hurtbox.QueryRect(Team.Enemy, area, meleeHits);
            if (meleeHits.Count == 0) return false;

            foreach (Hurtbox h in meleeHits)
            {
                DamageInfo info = DamageInfo.Create(c.meleeDamage, DamageKind.Melee, Team.Player, motor.Position);
                info.knockback = c.meleeKnockback;
                HitResult result = h.Damageable.ApplyDamage(info);
                if (result.Landed && controller.Bombs != null) controller.Bombs.OnMeleeDamage(result.damageDealt);
            }

            meleeCooldown = c.meleeCooldown;
            fireCooldown = Mathf.Max(fireCooldown, c.meleeCooldown * 0.5f);
            Stabbed?.Invoke();
            return true;
        }

        public bool TryShoot(Vector2 aim, bool crouching)
        {
            if (fireCooldown > 0f || pool == null || Current == null) return false;
            PlayerConfig c = controller.Config;
            if (pool.CountActive >= c.maxActiveCapsules) return false;

            CharacterMotor2D motor = controller.Motor;
            Vector2 shoulder = motor.Position + new Vector2(0f, controller.MuzzleHeight);
            Vector2 dir = aim.sqrMagnitude > 0.01f ? aim.normalized : new Vector2(motor.Facing, 0f);

            Projectile capsule = pool.Get();
            capsule.Launch(new ProjectileShot
            {
                origin = shoulder + dir * c.muzzleForward * controller.Size,
                direction = dir,
                speed = c.capsuleSpeed,
                range = c.capsuleRange,
                damage = c.capsuleDamage,
                knockback = c.capsuleKnockback,
                radius = 0.18f,
                sourceTeam = Team.Player,
                targetTeam = Team.Enemy,
                medication = Current,
                color = Current.color,
                groundMask = groundMask
            }, p => pool.Release(p));

            fireCooldown = c.fireInterval;
            CombatAudio sounds = CombatAudio.Instance;
            Sfx.Play(Sfx.FireSound(Current), sounds != null ? sounds.fireVolume : 0.45f, sounds != null ? sounds.pitchJitter : 0.06f);
            Fired?.Invoke();
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            if (controller == null || controller.Config == null || controller.Motor == null) return;
            PlayerConfig c = controller.Config;
            Vector2 center = controller.Motor.Position + new Vector2(controller.Motor.Facing * c.meleeReach * 0.5f, 0.95f);
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
            Gizmos.DrawWireCube(center, new Vector3(c.meleeReach, 1.5f, 0f));
        }
    }
}
