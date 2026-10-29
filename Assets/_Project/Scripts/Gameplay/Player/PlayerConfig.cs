using System.Collections.Generic;
using HealingKnight.Core.Combat;
using UnityEngine;

namespace HealingKnight.Gameplay.Player
{
    /// <summary>
    /// Todos los valores de balance de Healing Knight en un asset (estilo Metal Slug).
    /// Se ajustan desde el Inspector, incluso en Play Mode, sin tocar código.
    /// </summary>
    [CreateAssetMenu(menuName = "Healing Knight/Configuración del jugador", fileName = "PlayerConfig")]
    public class PlayerConfig : ScriptableObject
    {
        [Header("Movimiento (Metal Slug: velocidad constante, sin inercia)")]
        public float runSpeed = 5f;
        [Tooltip("Velocidad al avanzar encogido.")]
        public float crouchSpeed = 1.8f;
        [Tooltip("Control en el aire (0 a 1).")]
        [Range(0f, 1f)] public float airControl = 0.9f;

        [Header("Salto (altura fija)")]
        public float jumpHeight = 2.6f;
        public float timeToApex = 0.38f;
        [Tooltip("Saltos extra en el aire (1 = doble salto, 0 = sin doble salto).")]
        [Min(0)] public int airJumps = 1;
        [Tooltip("Altura del salto en el aire (un poco menor que el normal).")]
        public float airJumpHeight = 2f;
        public float airJumpTimeToApex = 0.32f;

        [Header("Caída lenta (mantener saltar mientras caes; como el prototipo)")]
        [Tooltip("Segundos de planeo por cada vez que pisas el suelo.")]
        public float slowFallDuration = 1.5f;
        [Tooltip("Gravedad al planear (0.25 = cuatro veces más suave).")]
        public float slowFallGravityScale = 0.25f;
        public float slowFallMaxSpeed = 2.2f;
        [Tooltip("Disparar hacia abajo en el aire frena un poco la caída (como el retroceso).")]
        public float downShotMaxFallSpeed = 5f;

        [Header("Fijar (botón de Cuphead: quieto y apuntando en 8 direcciones)")]
        [Tooltip("Velocidad horizontal en el aire mientras fijas (0 = te quedas en tu X).")]
        public float lockedAirSpeed = 0f;

        [Header("Encogerse (abajo)")]
        [Tooltip("Tamaño al encogerse (abajo): 0.55 = a poco más de la mitad. También baja el colisionador.")]
        [Range(0.3f, 1f)] public float crouchHeightFactor = 0.55f;
        [Tooltip("Qué tan rápido se encoge y vuelve a crecer (tamaño por segundo).")]
        public float shrinkSpeed = 5f;
        public float standingColliderHeight = 1.8f;

        [Header("Salud")]
        public float maxHealth = 100f;
        public float invulnerabilityTime = 1.2f;
        public float hurtStunTime = 0.2f;
        public float hurtKnockback = 4f;
        [Tooltip("Daño por caer al vacío.")]
        public float fallDamage = 20f;

        [Header("Disparo de cápsulas")]
        [Tooltip("Segundos entre disparos al mantener el botón.")]
        public float fireInterval = 0.18f;
        public float capsuleSpeed = 16f;
        public float capsuleRange = 12f;
        public float capsuleDamage = 10f;
        public float capsuleKnockback = 1.5f;
        public int maxActiveCapsules = 8;
        [Tooltip("Altura del hombro (de ahí sale el disparo). Encogido se multiplica por el tamaño.")]
        public float muzzleHeight = 0.85f;
        [Tooltip("Ya no se usa: al encogerse, la altura del hombro es muzzleHeight × tamaño.")]
        public float crouchMuzzleHeight = 0.5f;
        public float muzzleForward = 0.7f;

        [Header("Cuchillazo automático (al disparar con un enemigo pegado)")]
        public float meleeDamage = 14f;
        public float meleeReach = 1.1f;
        public float meleeCooldown = 0.3f;
        public float meleeKnockback = 5f;

        [Header("Bomba de medicamento (granada)")]
        [Tooltip("Bombas al empezar el nivel o al reaparecer.")]
        public int bombsPerLevel = 5;
        public int maxBombs = 10;
        [Tooltip("Carga de bomba por punto de daño EFECTIVO hecho (como las cartas de Cuphead). 1/150 = una bomba cada 150 de daño.")]
        public float bombChargePerDamage = 1f / 150f;
        [Tooltip("Carga extra por cada parry con el escudo.")]
        public float bombChargePerParry = 0.35f;
        public float bombDamage = 30f;
        public float bombBlastRadius = 1.8f;
        public Vector2 bombThrowVelocity = new Vector2(7f, 9f);
        public float bombCooldown = 0.4f;

        [Header("Escudo burbuja (GDD 5.8)")]
        public float shieldMaxEnergy = 100f;
        public float shieldProjectileCost = 20f;
        public float shieldContactCost = 30f;
        public float shieldRegenDelay = 1.5f;
        public float shieldRegenPerSecond = 15f;
        public float shieldBreakCooldown = 2.5f;
        [Range(0f, 1f)] public float shieldMoveSpeedMultiplier = 0.4f;
        [Tooltip("Ángulo que cubre el escudo hacia donde miras. 360 = bloquea por todos lados.")]
        [Range(30f, 360f)] public float shieldArcDegrees = 160f;
        [Tooltip("Parry: si el golpe llega en estos primeros segundos después de levantar el escudo, no gasta energía y recarga el disparo.")]
        public float parryWindow = 0.2f;

        [Header("Medicamentos genéricos (la cápsula regional la agrega el sistema)")]
        public List<MedicationData> genericMedications = new List<MedicationData>();
    }
}
