using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>Muestra un mensaje de NODO una sola vez cuando el jugador pasa por esta X (tutoriales fuera de arenas).</summary>
    public class MessageTrigger : MonoBehaviour
    {
        [SerializeField, TextArea] private string message;

        private bool shown;

        private void Update()
        {
            if (shown || string.IsNullOrEmpty(message)) return;
            PlayerController player = PlayerController.Current;
            if (player == null || player.Position.x < transform.position.x) return;

            shown = true;
            GameplayEvents.RaiseMessage(message);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.8f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 3f);
        }
    }
}
