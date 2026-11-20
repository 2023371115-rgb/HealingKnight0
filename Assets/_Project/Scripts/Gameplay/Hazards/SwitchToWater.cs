using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>Une un botón con un agua: prendido sube, apagado baja (la hormona ADH en el túbulo colector).</summary>
    public class SwitchToWater : MonoBehaviour
    {
        [SerializeField] private ShootSwitch button;
        [SerializeField] private BuoyancyZone water;

        public void Configure(ShootSwitch sw, BuoyancyZone zone) { button = sw; water = zone; }

        private void Start()
        {
            if (button != null) button.Changed += OnChanged;
        }

        private void OnDestroy()
        {
            if (button != null) button.Changed -= OnChanged;
        }

        private void OnChanged(bool on)
        {
            if (water != null) water.SetHigh(on);
            GameplayEvents.RaiseMessage(on ? "¡ADH! El cuerpo GUARDA agua: el nivel sube." : "Sin ADH el agua se va (por eso orinas más si tomas mucha agua).");
        }
    }
}
