using System;
using HealingKnight.Core.Damage;
using UnityEngine;

namespace HealingKnight.Gameplay.Player
{
    /// <summary>
    /// Escudo burbuja (diseño del prototipo + GDD 5.8):
    /// - Mientras se mantiene, una burbuja rodea a Healing Knight y bloquea golpes y proyectiles
    ///   que llegan dentro del arco configurado (por defecto 120° hacia donde miras).
    /// - Cada bloqueo gasta energía; si llega a 0 la burbuja se rompe y no se puede usar un rato.
    /// - PARRY: si el golpe llega justo después de levantar el escudo, no gasta energía
    ///   y el siguiente disparo sale al instante.
    /// - No bloquea el ácido ni el daño ambiental. Con el escudo arriba no se dispara, golpea ni salta.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerShield : MonoBehaviour
    {
        [Tooltip("La burbuja (se activa mientras bloquea).")]
        [SerializeField] private GameObject shieldVisual;
        [SerializeField] private Color fullColor = new Color(0.03f, 0.91f, 0.95f, 0.45f);
        [SerializeField] private Color lowColor = new Color(1f, 0.35f, 0.25f, 0.45f);
        [SerializeField] private Color parryColor = new Color(1f, 1f, 1f, 0.8f);
        [SerializeField] private float parryFlashTime = 0.15f;

        private PlayerController controller;
        private SpriteRenderer shieldRenderer;
        private Vector3 visualBaseScale = Vector3.one;
        private float energy;
        private bool wantsBlock;
        private bool wasActive;
        private bool broken;
        private float brokenTimer;
        private float raisedAt = -999f;
        private float lastHitTime = -999f;
        private float flashUntil;

        public bool IsActive => wantsBlock && !broken && energy > 0f;
        public bool IsBroken => broken;
        public float Energy01 => controller != null && controller.Config.shieldMaxEnergy > 0f ? energy / controller.Config.shieldMaxEnergy : 0f;

        public event Action Blocked;
        public event Action Parried;
        public event Action Broke;
        public event Action<float> EnergyChanged; // 0..1

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            if (shieldVisual != null)
            {
                shieldRenderer = shieldVisual.GetComponentInChildren<SpriteRenderer>(true);
                visualBaseScale = shieldVisual.transform.localScale;
                shieldVisual.SetActive(false);
            }
        }

        private void Start()
        {
            ResetEnergy();
        }

        public void SetWantsBlock(bool value) => wantsBlock = value;

        public void ResetEnergy()
        {
            energy = controller.Config.shieldMaxEnergy;
            broken = false;
            EnergyChanged?.Invoke(Energy01);
        }

        private void Update()
        {
            PlayerConfig c = controller.Config;
            float dt = Time.deltaTime;

            if (broken)
            {
                brokenTimer -= dt;
                if (brokenTimer <= 0f)
                {
                    broken = false;
                    energy = c.shieldMaxEnergy * 0.25f;
                    EnergyChanged?.Invoke(Energy01);
                }
            }
            else if (energy < c.shieldMaxEnergy && Time.time >= lastHitTime + c.shieldRegenDelay)
            {
                energy = Mathf.Min(c.shieldMaxEnergy, energy + c.shieldRegenPerSecond * dt);
                EnergyChanged?.Invoke(Energy01);
            }

            bool active = IsActive;
            if (active && !wasActive) raisedAt = Time.time; // empieza la ventana de parry
            wasActive = active;

            if (shieldVisual == null) return;
            if (shieldVisual.activeSelf != active) shieldVisual.SetActive(active);
            if (!active) return;

            if (shieldRenderer != null)
                shieldRenderer.color = Time.time < flashUntil ? parryColor : Color.Lerp(lowColor, fullColor, Energy01);

            // Pequeño "pop" al levantarla y un pulso suave mientras se mantiene.
            float sinceRaise = Time.time - raisedAt;
            float pop = sinceRaise < 0.1f ? Mathf.Lerp(0.8f, 1f, sinceRaise / 0.1f) : 1f + Mathf.Sin(Time.time * 8f) * 0.03f;
            shieldVisual.transform.localScale = visualBaseScale * pop;
        }

        /// <summary>Intenta bloquear un golpe. Devuelve true si lo detuvo.</summary>
        public bool TryBlock(DamageInfo info)
        {
            if (!IsActive || !info.blockable || info.kind == DamageKind.Environment) return false;

            PlayerConfig c = controller.Config;
            if (!IsInsideArc(info.sourcePosition, c.shieldArcDegrees)) return false;

            lastHitTime = Time.time;

            // Parry: bloqueo justo a tiempo.
            if (Time.time - raisedAt <= c.parryWindow)
            {
                flashUntil = Time.time + parryFlashTime;
                controller.Shooter?.ResetCooldown();
                Parried?.Invoke();
                Blocked?.Invoke();
                return true;
            }

            float cost = info.kind == DamageKind.Projectile ? c.shieldProjectileCost : c.shieldContactCost;
            energy = Mathf.Max(0f, energy - cost);
            Blocked?.Invoke();

            if (energy <= 0f)
            {
                broken = true;
                brokenTimer = c.shieldBreakCooldown;
                Broke?.Invoke();
            }

            EnergyChanged?.Invoke(Energy01);
            return true;
        }

        private bool IsInsideArc(Vector2 sourcePosition, float arcDegrees)
        {
            if (arcDegrees >= 359f) return true;

            Vector2 toSource = sourcePosition - controller.Motor.Center;
            if (toSource.sqrMagnitude < 0.0001f) return true;

            Vector2 facing = new Vector2(controller.Motor.Facing, 0f);
            return Vector2.Angle(facing, toSource) <= arcDegrees * 0.5f;
        }
    }
}
