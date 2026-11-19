using HealingKnight.Gameplay.Level;
using UnityEngine;

namespace HealingKnight.Gameplay.Flight
{
    /// <summary>
    /// Mensaje de NODO en un nivel de vuelo: sale una vez cuando el centro de la pantalla llega a esta X.
    /// Con 'branch' (1 arriba, 2 abajo) solo sale si vas por esa rama de la bifurcación.
    /// </summary>
    public class FlightMessage : MonoBehaviour
    {
        [SerializeField, TextArea] private string message;
        [SerializeField] private int branch;

        private bool shown;

        public void Configure(string text, int routeBranch = 0)
        {
            message = text;
            branch = routeBranch;
        }

        private void Update()
        {
            if (shown || string.IsNullOrEmpty(message)) return;
            FlightRoute route = FlightRoute.Current;
            if (route == null || !route.InBranch(branch) || route.CameraPosition.x < transform.position.x) return;
            shown = true;
            GameplayEvents.RaiseMessage(message);
        }
    }
}
