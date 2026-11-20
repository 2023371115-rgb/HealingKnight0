using System.Collections.Generic;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Flight
{
    /// <summary>Qué da un power-up de la carrera.</summary>
    public enum FlightPowerKind
    {
        /// <summary>TRIPLE DISPARO: tres cápsulas en abanico por un rato.</summary>
        TripleShot,
        /// <summary>+MISILES (aunque pases del máximo normal).</summary>
        Missiles,
        /// <summary>GLÓBULO BLANCO: lo atrapas y se vuelve un LINFOCITO que te escolta y dispara toda la carrera.</summary>
        WhiteCell
    }
}
