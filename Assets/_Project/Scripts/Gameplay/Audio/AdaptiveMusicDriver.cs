using System;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Audio
{
    /// <summary>Va en el objeto "MusicaAdaptativa" (lo crea AdaptiveMusic): lo actualiza cada cuadro.</summary>
    public class AdaptiveMusicDriver : MonoBehaviour
    {
        private void Update() => AdaptiveMusic.Tick();
    }
}
