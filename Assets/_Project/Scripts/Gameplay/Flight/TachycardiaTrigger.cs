using HealingKnight.Gameplay.Level;
using UnityEngine;

namespace HealingKnight.Gameplay.Flight
{
    /// <summary>
    /// Al pasar la pantalla por esta X, el corazón late más rápido (o vuelve a su ritmo con 0): la sangre empuja más
    /// seguido y los enemigos disparan más rápido (disparan al ritmo). Biología: TAQUICARDIA = más de 100 latidos por
    /// minuto; pasa con el ejercicio, el miedo... o una infección con fiebre como la sepsis.
    /// </summary>
    public class TachycardiaTrigger : MonoBehaviour
    {
        [SerializeField] private float extraBpm = 18f;
        [SerializeField, TextArea] private string message;

        private bool done;

        public void Configure(float bpm, string text)
        {
            extraBpm = bpm;
            message = text;
        }

        private void Update()
        {
            if (done) return;
            FlightRoute route = FlightRoute.Current;
            if (route == null || route.View.xMax < transform.position.x || Heartbeat.Current == null) return;
            done = true;
            Heartbeat.Current.Tachycardia = extraBpm;
            if (!string.IsNullOrEmpty(message)) GameplayEvents.RaiseMessage(message);
        }
    }
}
