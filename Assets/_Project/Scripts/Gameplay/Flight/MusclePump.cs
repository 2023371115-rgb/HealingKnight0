using System.Collections.Generic;
using HealingKnight.Core.Combat;
using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Flight
{
    /// <summary>
    /// BOMBA MUSCULAR de la pantorrilla (tramo 5): mientras la pantalla va por aquí, el músculo aprieta la vena cada
    /// 'period' segundos. Al apretar, las VÁLVULAS se abren y la sangre (y tú) sale disparada hacia arriba; al soltar,
    /// las válvulas se cierran para que la sangre no se regrese.
    /// Biología: por eso caminar ayuda a la circulación de las piernas.
    /// </summary>
    public class MusclePump : MonoBehaviour
    {
        [SerializeField] private float fromX, toX;
        [SerializeField] private float period = 4.2f;
        [Tooltip("Parte del ciclo en la que las válvulas están abiertas (0..1).")]
        [SerializeField] private float openPart = 0.58f;

        public static MusclePump Current { get; private set; }
        private float time;
        private int squeezes;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Current = null;

        public void Configure(float startX, float endX, float seconds)
        {
            fromX = startX;
            toX = endX;
            period = seconds;
        }

        /// <summary>Qué tan abierta está una válvula con ese retraso (0 cerrada, 1 abierta). Sin bomba: abierta.</summary>
        public static float Open01(float delay, float x)
        {
            MusclePump p = Current;
            if (p == null || x < p.fromX || x > p.toX) return 1f;
            float phase = Mathf.Repeat((p.time - delay) / p.period, 1f);
            // Abre rápido, se queda abierta, cierra rápido.
            const float edge = 0.08f;
            if (phase < edge) return phase / edge;
            if (phase < p.openPart) return 1f;
            if (phase < p.openPart + edge) return 1f - (phase - p.openPart) / edge;
            return 0f;
        }

        private void Update()
        {
            FlightRoute route = FlightRoute.Current;
            if (route == null) return;
            float cx = route.CameraPosition.x;
            bool inside = cx > fromX - 6f && cx < toX;
            if (!inside) { if (Current == this) Current = null; return; }
            if (Current != this) { Current = this; time = 0f; }
            float before = Mathf.Repeat(time / period, 1f);
            time += Time.deltaTime;
            float after = Mathf.Repeat(time / period, 1f);
            if (after < before) Squeeze(route);
        }

        /// <summary>El músculo aprieta: empujón hacia arriba y la pantalla avanza un rato más rápido.</summary>
        private void Squeeze(FlightRoute route)
        {
            squeezes++;
            route.ApplySpeedEffect(1.6f, period * openPart * 0.8f, 0f);
            PlayerController player = PlayerController.Current;
            if (player != null && player.Flight != null && player.Health.IsAlive) player.Flight.Kick(new Vector2(4.5f, 0f));
            Sfx.Effect("Latido_Lub", 0.9f);
            if (squeezes <= 3) SpeedBanner.Show("¡BOMBA MUSCULAR!", new Color(1f, 0.7f, 0.6f), 1f);
        }
    }
}
