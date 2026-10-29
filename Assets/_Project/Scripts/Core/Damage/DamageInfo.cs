using HealingKnight.Core.Combat;
using UnityEngine;

namespace HealingKnight.Core.Damage
{
    public enum Team
    {
        Player,
        Enemy,
        Neutral
    }

    public enum DamageKind
    {
        /// <summary>Choque o embestida cuerpo a cuerpo de un enemigo.</summary>
        Contact,
        /// <summary>Golpe cercano del jugador.</summary>
        Melee,
        /// <summary>Cápsula del jugador o proyectil enemigo.</summary>
        Projectile,
        /// <summary>Ácido, tormentas y demás peligros del escenario. El escudo no lo bloquea.</summary>
        Environment
    }

    /// <summary>Todo lo que hace falta saber sobre un golpe. Una sola forma de hacer daño en todo el juego.</summary>
    public struct DamageInfo
    {
        public float amount;
        public DamageKind kind;
        public Team sourceTeam;

        /// <summary>Posición en el piso de quien golpea (para el arco del escudo y el empuje).</summary>
        public Vector2 sourcePosition;

        /// <summary>Si el escudo puede bloquearlo.</summary>
        public bool blockable;

        /// <summary>Fuerza del empuje horizontal (unidades por segundo).</summary>
        public float knockback;

        /// <summary>Medicamento de la cápsula; null si el golpe no es de medicamento.</summary>
        public MedicationData medication;

        public static DamageInfo Create(float amount, DamageKind kind, Team sourceTeam, Vector2 sourcePosition)
        {
            return new DamageInfo
            {
                amount = amount,
                kind = kind,
                sourceTeam = sourceTeam,
                sourcePosition = sourcePosition,
                blockable = kind != DamageKind.Environment,
                knockback = 0f,
                medication = null
            };
        }
    }

    public enum HitOutcome
    {
        /// <summary>No hizo nada (objetivo muerto, invulnerable o del mismo equipo).</summary>
        Ignored,
        /// <summary>Lo detuvo el escudo.</summary>
        Blocked,
        /// <summary>El medicamento no sirve contra esta amenaza: la cápsula rebota.</summary>
        Resisted,
        Damaged,
        Killed
    }

    public struct HitResult
    {
        public HitOutcome outcome;
        public Effectiveness effectiveness;
        public float damageDealt;

        public bool Landed => outcome == HitOutcome.Damaged || outcome == HitOutcome.Killed;

        public static HitResult Ignored => new HitResult { outcome = HitOutcome.Ignored, effectiveness = Effectiveness.Ineffective };
    }

    /// <summary>Cualquier cosa que puede recibir daño: jugador, enemigos, obstáculos tratables.</summary>
    public interface IDamageable
    {
        Team Team { get; }
        bool IsAlive { get; }
        HitResult ApplyDamage(DamageInfo info);
    }
}
