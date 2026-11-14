using HealingKnight.Core.Flow;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// Pasaje secreto escondido en un nivel. Al tocarlo se registra el secreto en el guardado
    /// y aparece en el mapa el nivel oculto que lo usa (LevelData.secretId).
    /// Escóndelo en una plataforma alta, detrás de algo, o en un tramo que no sea obvio.
    /// </summary>
    public class SecretPassage : MonoBehaviour
    {
        [SerializeField] private string secretId = "digestivo_boca";
        [SerializeField, TextArea] private string foundMessage = "¡Pasaje secreto! Nuevo nivel en el mapa del cuerpo.";
        [SerializeField] private float radius = 1f;
        [SerializeField] private SpriteRenderer glow;

        private bool found;
        private Vector3 baseScale = Vector3.one;

        private void Awake()
        {
            if (glow != null) baseScale = glow.transform.localScale;
        }

        private void Start()
        {
            // Si ya se encontró antes, se muestra apagado.
            if (GameSession.Save.HasSecret(secretId)) SetFound(false);
        }

        private void Update()
        {
            if (glow != null && !found)
                glow.transform.localScale = baseScale * (1f + Mathf.Sin(Time.time * 4f) * 0.12f);

            if (found) return;
            PlayerController player = PlayerController.Current;
            if (player == null) return;

            if (Vector2.Distance(player.Motor.Center, transform.position) <= radius)
                SetFound(GameSession.FindSecret(secretId));
        }

        private void SetFound(bool announce)
        {
            found = true;
            if (glow != null)
            {
                Color c = glow.color;
                c.a = 0.25f;
                glow.color = c;
                glow.transform.localScale = baseScale;
            }
            if (announce && !string.IsNullOrEmpty(foundMessage)) GameplayEvents.RaiseMessage(foundMessage);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
