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
        [Tooltip("Letrero grande arriba de la pantalla (opcional), además del mensaje de NODO.")]
        [SerializeField] private string banner;
        [SerializeField] private Color bannerColor = Color.white;

        private bool shown;

        public void Configure(string text, int routeBranch = 0)
        {
            message = text;
            branch = routeBranch;
        }

        public void ConfigureBanner(string bannerText, Color color)
        {
            banner = bannerText;
            bannerColor = color;
        }

        private void Update()
        {
            if (shown || (string.IsNullOrEmpty(message) && string.IsNullOrEmpty(banner))) return;
            FlightRoute route = FlightRoute.Current;
            if (route == null || !route.InBranch(branch) || route.CameraPosition.x < transform.position.x) return;
            shown = true;
            if (!string.IsNullOrEmpty(message)) GameplayEvents.RaiseMessage(message);
            if (!string.IsNullOrEmpty(banner)) SpeedBanner.Show(banner, bannerColor, 1.8f);
        }
    }
}
