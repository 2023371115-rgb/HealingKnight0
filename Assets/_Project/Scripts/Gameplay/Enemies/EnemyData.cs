using System;
using System.Collections.Generic;
using HealingKnight.Core.Combat;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    public enum EnemyMovement
    {
        /// <summary>Camina por el suelo y las plataformas (con gravedad).</summary>
        Ground,
        /// <summary>Flota en el aire (virus): no le afecta la gravedad.</summary>
        Flying
    }

    public enum EnemyAttackType
    {
        /// <summary>Se acerca y embiste (parásitos, bacterias).</summary>
        Melee,
        /// <summary>Guarda distancia y dispara por su franja (virus).</summary>
        Ranged
    }

    /// <summary>
    /// Forma de pelear y moverse. Cada una se basa en la biología del microorganismo (ver 'biologia' en cada enemigo).
    /// Las de suelo bajan de las plataformas y saltan para subir; las voladoras flotan.
    /// </summary>
    public enum EnemyBehavior
    {
        // ---- De suelo ----
        /// <summary>Camina y embiste (genérico).</summary>
        Walker,
        /// <summary>Avanza a saltitos y te cae encima (neumococo: diplococo que se mueve en pareja).</summary>
        Hopper,
        /// <summary>Se detiene, avisa y cruza a toda velocidad (meningococo: invade la sangre muy rápido).</summary>
        Charger,
        /// <summary>Rueda como llanta y rebota; no se detiene (rotavirus: "rota" = rueda).</summary>
        Roller,
        /// <summary>Se mete bajo la mucosa, no lo puedes dañar, y sale debajo de ti (H. pylori: espiral que se entierra).</summary>
        Burrower,
        /// <summary>Herido, se vuelve espora: las cápsulas rebotan hasta que despierta (C. difficile).</summary>
        Sporer,
        /// <summary>Guarda distancia y lanza ácido en arco, alcanza plataformas (S. mutans: ácido láctico de la caries).</summary>
        Lobber,
        /// <summary>Camina y embiste; al morir se separa en cocos pequeños (S. aureus: racimo de uvas).</summary>
        Splitter,
        /// <summary>Ataques de tos en ráfaga que empujan lejos (B. pertussis: tos ferina).</summary>
        Cougher,
        /// <summary>Lento y duro; escupe gotitas en arco y se encierra en granuloma (M. tuberculosis).</summary>
        Tank,
        /// <summary>Salta y se te pega con su disco: te frena y te va dañando (Giardia).</summary>
        Latcher,
        /// <summary>Estira un seudópodo largo que te jala hacia él (Entamoeba: fagocitosis).</summary>
        Engulfer,
        /// <summary>Rápido y culebreando; al morir deja un huevo que eclosiona si no lo rompes (oxiuro: autoinfección).</summary>
        Breeder,
        /// <summary>Huevo: no se mueve; si no lo destruyes a tiempo, nace un enemigo ('cria').</summary>
        Egg,

        // ---- Voladores ----
        /// <summary>Flota y dispara (genérico).</summary>
        Shooter,
        /// <summary>Vuelo errático, estornuda en abanico y se cura solo con el tiempo (rinovirus: resfriado).</summary>
        Spreader,
        /// <summary>Flota alto y se lanza en picada con sus espículas (influenza).</summary>
        Diver,
        /// <summary>Sus disparos te dejan lento un momento (poliovirus: parálisis).</summary>
        Paralyzer,
        /// <summary>Se vuelve latente (invisible e inmune), se acomoda y reaparece disparando en ráfaga (herpes).</summary>
        Latent,
        /// <summary>Lo lleva el aire en ondas; cerca de ti revienta y te da alergia (te frena) (polen).</summary>
        Drifter,
        /// <summary>Muerde varias veces seguidas, muy agresivo (rabia).</summary>
        Frenzy
    }

    /// <summary>Excepción a la tabla general para UN enemigo (p. ej. tuberculosis: solo el antibiótico sirve).</summary>
    [Serializable]
    public class EnemyWeakness
    {
        public MedicationData medication;
        public Effectiveness result = Effectiveness.Effective;
    }

    /// <summary>Valores de un tipo de enemigo. Un asset por enemigo: Parásito, Bacteria, Rotavirus...</summary>
    [CreateAssetMenu(menuName = "Healing Knight/Enemigo", fileName = "Enemigo")]
    public class EnemyData : ScriptableObject
    {
        public string displayName = "Parásito";

        [Tooltip("Define qué medicamento lo trata (tabla de efectividad).")]
        public ThreatType threat = ThreatType.Parasito;

        [Tooltip("Excepciones a la tabla solo para este enemigo (ganan sobre la tabla general).")]
        public List<EnemyWeakness> weaknesses = new List<EnemyWeakness>();

        [Tooltip("En qué se basa su comportamiento (para el equipo y para documentar).")]
        [TextArea] public string biologia;

        [Header("Comportamiento")]
        public EnemyBehavior behavior = EnemyBehavior.Walker;
        [Tooltip("De suelo: qué tan alto salta para subir a una plataforma (0 = no salta).")]
        public float jumpHeight = 3.2f;
        [Tooltip("Habilidad especial (enterrarse, espora, granuloma, latencia): cada cuántos segundos.")]
        public float specialCooldown = 5f;
        [Tooltip("Habilidad especial: cuánto dura (o lo que tarda en eclosionar un huevo).")]
        public float specialDuration = 2.5f;
        [Tooltip("Se elimina solo después de estos segundos (0 = nunca). El resfriado se cura solo.")]
        public float lifetime = 0f;

        [Header("Al morir")]
        [Tooltip("Lo que suelta al morir (cocos del estafilococo, huevo del oxiuro) o lo que nace del huevo.")]
        public GameObject childPrefab;
        public EnemyData childData;
        public int childCount = 2;
        [Header("Vida")]
        public float maxHealth = 30f;
        [Tooltip("Multiplicador del daño del golpe cercano del jugador.")]
        public float meleeDamageTakenMultiplier = 1f;
        [Tooltip("0 = vuela con cualquier golpe, 1 = no se mueve.")]
        [Range(0f, 1f)] public float knockbackResistance = 0f;
        [Tooltip("Tiempo aturdido al recibir un golpe que sí hace daño.")]
        public float hurtStunTime = 0.2f;

        [Header("Movimiento")]
        public EnemyMovement movement = EnemyMovement.Ground;
        public float moveSpeed = 2.2f;
        [Tooltip("Solo voladores: altura sobre el jugador a la que intenta flotar.")]
        public float flyHeight = 2.2f;
        [Tooltip("A qué distancia empieza a perseguirte.")]
        public float aggroRange = 14f;

        [Header("Ataque")]
        public EnemyAttackType attackType = EnemyAttackType.Melee;
        [Tooltip("Cuerpo a cuerpo: distancia a la que embiste. A distancia: distancia que intenta mantener.")]
        public float attackDistance = 1.2f;
        [Tooltip("Aviso antes de atacar (se ilumina). Es lo que hace justo al combate.")]
        public float attackWindup = 0.45f;
        public float attackCooldown = 1.4f;
        public float attackDamage = 10f;
        public float attackKnockback = 4f;

        [Header("Solo a distancia")]
        public float projectileSpeed = 7f;
        public float projectileRange = 9f;
        public Color projectileColor = new Color(0.7f, 1f, 0.3f);
        [Tooltip("Proyectiles por disparo, en abanico (estornudo).")]
        public int spreadShots = 1;
        public float spreadDegrees = 24f;
        [Tooltip("Disparos seguidos por ataque (ráfaga).")]
        public int burstCount = 1;
        [Tooltip("Si es > 0, el disparo cae en arco (gravedad).")]
        public float projectileGravity = 0f;
        [Tooltip("Al pegarte te deja lento: multiplicador de velocidad (0 = no frena).")]
        [Range(0f, 1f)] public float slowOnHit = 0f;
        public float slowDuration = 1.5f;

        [Header("Recompensas")]
        [Tooltip("Carga que da a Linfocito al ser derrotado cerca de él (para cuando se implemente).")]
        public int linfocitoCharge = 1;

        [Header("Placeholder visual")]
        public Color tint = Color.white;
        public float visualScale = 1f;

        /// <summary>Efectividad de un medicamento contra ESTE enemigo: primero sus excepciones, luego la tabla.</summary>
        public Effectiveness Evaluate(EffectivenessTable table, MedicationData medication)
        {
            if (weaknesses != null)
                foreach (EnemyWeakness w in weaknesses)
                    if (w != null && w.medication == medication && medication != null) return w.result;
            return table != null ? table.Evaluate(threat, medication) : Effectiveness.Effective;
        }

        public bool IsFlying => movement == EnemyMovement.Flying;
    }
}
