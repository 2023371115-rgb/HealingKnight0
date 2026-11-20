using System;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// Avisos globales del nivel. La interfaz (HUD) y el audio se suscriben aquí
    /// sin que el gameplay tenga que conocerlos.
    /// </summary>
    public static class GameplayEvents
    {
        public static event Action<ArenaZone> ArenaStarted;
        public static event Action<ArenaZone, int, int> WaveStarted;   // arena, oleada (1..n), total
        public static event Action<ArenaZone> ArenaCleared;
        public static event Action LevelCompleted;
        public static event Action PlayerDefeated;
        public static event Action<string> Message;                     // textos breves de NODO / tutorial
        public static event Action<string, Enemies.EnemyHealth> BossStarted; // nombre, vida del jefe
        public static event Action BossEnded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ArenaStarted = null;
            WaveStarted = null;
            ArenaCleared = null;
            LevelCompleted = null;
            PlayerDefeated = null;
            Message = null;
            BossStarted = null;
            BossEnded = null;
        }

        public static void RaiseArenaStarted(ArenaZone arena) => ArenaStarted?.Invoke(arena);
        public static void RaiseWaveStarted(ArenaZone arena, int wave, int total) => WaveStarted?.Invoke(arena, wave, total);
        public static void RaiseArenaCleared(ArenaZone arena) => ArenaCleared?.Invoke(arena);
        public static void RaiseLevelCompleted() => LevelCompleted?.Invoke();
        public static void RaisePlayerDefeated() => PlayerDefeated?.Invoke();
        public static void RaiseMessage(string text) => Message?.Invoke(text);
        public static void RaiseBossStarted(string name, Enemies.EnemyHealth health) => BossStarted?.Invoke(name, health);
        public static void RaiseBossEnded() => BossEnded?.Invoke();
    }
}
