using System;
using System.Collections;
using HealingKnight.Core.Flow;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// Salud del cerebro (GDD 5.14): segunda barra, independiente de la del jugador.
    /// No se recupera y su daño es irreversible durante el nivel; morir no la restaura.
    /// Cápsula del jugador que pega en una neurona sana: −5. Proyectil enemigo: −3.
    /// Si llega a 0: desenlace negativo y el nivel se reinicia.
    /// </summary>
    public class BrainHealth : MonoBehaviour
    {
        [SerializeField] private float max = 100f;
        [SerializeField, TextArea] private string defeatMessage =
            "NODO: La salud del cerebro llegó a 0. El daño es irreversible... Reiniciando el tratamiento.";
        [SerializeField] private float restartDelay = 3.5f;

        private float current;
        private bool depleted;

        public static BrainHealth Current { get; private set; }
        public float Value => current;
        public float Max => max;
        public float Health01 => max > 0f ? current / max : 0f;

        public event Action<float, float> Changed; // (actual, máxima)
        public event Action Depleted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Current = null;

        private void Awake()
        {
            Current = this;
            current = max;
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        /// <summary>Sanar una neurona hiperexcitada devuelve un poco (te perdona un error).</summary>
        public void Heal(float amount)
        {
            if (depleted || amount <= 0f) return;
            current = Mathf.Min(max, current + amount);
            Changed?.Invoke(current, max);
        }

        public void Damage(float amount)
        {
            if (depleted || amount <= 0f) return;
            current = Mathf.Max(0f, current - amount);
            Changed?.Invoke(current, max);
            if (current <= 0f) StartCoroutine(NegativeEnding());
        }

        private IEnumerator NegativeEnding()
        {
            depleted = true;
            Depleted?.Invoke();
            if (!string.IsNullOrEmpty(defeatMessage)) GameplayEvents.RaiseMessage(defeatMessage);
            PlayerController player = PlayerController.Current;
            if (player != null && player.Input != null) player.Input.Blocked = true;

            yield return new WaitForSeconds(restartDelay);
            GameSession.ReloadCurrentScene();
        }
    }
}
