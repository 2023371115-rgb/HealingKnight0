using System;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.CameraSystem;
using UnityEngine;

namespace HealingKnight.Gameplay.Flight
{
    /// <summary>
    /// El LATIDO del sistema circulatorio: el reloj de todo el nivel.
    /// - Cada latido (sístole) el corazón empuja la sangre: la pantalla avanza de golpe y tiembla un poco, y luego
    ///   (diástole) todo va lento hasta el siguiente.
    /// - Suena "lub" al latir y "dub" un momento después (los dos ruidos reales del corazón: se cierran las válvulas).
    /// - Los enemigos pueden disparar al ritmo (evento Beat).
    /// - BPM sube con 'Tachycardia' (para jefes o tramos de tensión).
    /// Biología: en reposo el corazón late 60–100 veces por minuto.
    /// </summary>
    [DefaultExecutionOrder(-60)]
    public class Heartbeat : MonoBehaviour
    {
        [SerializeField] private float bpm = 84f;
        [Tooltip("Segundos entre el 'lub' y el 'dub'.")]
        [SerializeField] private float dubDelay = 0.28f;
        [SerializeField] private float shake = 0.05f;
        [SerializeField] private float volume = 0.55f;

        public static Heartbeat Current { get; private set; }

        /// <summary>Cada latido (número de latido). Para disparar o cambiar algo al ritmo.</summary>
        public static event Action<int> Beat;

        /// <summary>0..1: qué tan fuerte empuja la sangre ahora (1 justo al latir, baja hasta el siguiente).</summary>
        public static float SurgeNow => Current != null ? Current.surge : 0f;
        /// <summary>Fracción del latido actual (0 = acaba de latir, 1 = va a latir).</summary>
        public static float PhaseNow => Current != null ? Current.phase : 0f;

        /// <summary>Latidos por minuto extra (taquicardia). 0 = normal.</summary>
        public float Tachycardia { get; set; }
        public float Bpm => bpm + Tachycardia;
        public float Period => 60f / Mathf.Max(30f, Bpm);
        public int Count => count;

        private float sinceBeat, surge, phase;
        private bool dubPlayed;
        private int count;
        private bool running = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = null;
            Beat = null;
        }

        public void Configure(float beatsPerMinute) => bpm = beatsPerMinute;

        /// <summary>Detiene o reanuda el latido (al morir el jugador, el mundo "se queda quieto").</summary>
        public void SetRunning(bool on) => running = on;

        private void OnEnable() => Current = this;
        private void OnDisable() { if (Current == this) Current = null; }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (!running) { surge = Mathf.MoveTowards(surge, 0f, dt * 2f); return; }
            sinceBeat += dt;
            float period = Period;
            if (sinceBeat >= period)
            {
                sinceBeat -= period;
                count++;
                dubPlayed = false;
                Sfx.Effect("Latido_Lub", volume);
                Camera cam = Camera.main;
                if (cam != null && cam.TryGetComponent(out GameCamera gc)) gc.Shake(shake, 0.12f);
                Beat?.Invoke(count);
            }
            if (!dubPlayed && sinceBeat >= dubDelay)
            {
                dubPlayed = true;
                Sfx.Effect("Latido_Dub", volume * 0.7f);
            }
            phase = sinceBeat / period;
            // Sube rápido (contracción) y baja suave: así se siente el empujón.
            const float rise = 0.06f;
            surge = sinceBeat < rise ? sinceBeat / rise : Mathf.Exp(-(sinceBeat - rise) / (period * 0.3f));
        }
    }
}
