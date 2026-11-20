using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// DIURÉTICO (cápsula azul que flota): unos segundos nadas más rápido y las corrientes de lado te llevan hacia
    /// adelante. Biología: un diurético hace que el riñón saque más agua y sal en la orina (se usa, por ejemplo, para
    /// la presión alta).
    /// </summary>
    public class DiureticPickup : MonoBehaviour
    {
        [SerializeField] private float seconds = 8f;

        private static float until;
        private Transform visual;

        public static bool Active => Time.time < until;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => until = 0f;

        private void Start()
        {
            visual = new GameObject("Capsula").transform;
            visual.SetParent(transform, false);
            CodeSprites.Add(visual, CodeSprites.Glow, new Color(0.4f, 0.75f, 1f, 0.6f), 34, Vector2.zero, Vector2.one * 1.8f, "Aura");
            CodeSprites.Add(visual, CodeSprites.Circle, new Color(0.1f, 0.15f, 0.3f), 35, Vector2.zero, new Vector2(1f, 0.5f), "Contorno");
            CodeSprites.Add(visual, CodeSprites.Circle, new Color(0.35f, 0.65f, 1f), 36, new Vector2(-0.2f, 0f), new Vector2(0.5f, 0.38f), "Mitad");
            CodeSprites.Add(visual, CodeSprites.Circle, Color.white, 36, new Vector2(0.2f, 0f), new Vector2(0.5f, 0.38f), "MitadBlanca");
            CodeSprites.Add(visual, CodeSprites.Circle, new Color(1f, 1f, 1f, 0.7f), 37, new Vector2(-0.15f, 0.08f), new Vector2(0.4f, 0.08f), "Brillo");
            WorldText.Create(transform, "DIURÉTICO", new Vector2(0f, 0.85f), new Vector2(3f, 0.5f), new Color(0.7f, 0.9f, 1f), 38, null, 2.4f);
        }

        private void Update()
        {
            if (visual == null) return;
            visual.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 2f) * 0.2f, 0f);
            visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 1.3f) * 15f);
            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive) return;
            if (((Vector2)visual.position - player.Motor.Center).sqrMagnitude > 1f) return;
            until = Time.time + seconds;
            Sfx.EffectAt("Recoger_Capsula", visual.position);
            Flight.SpeedBanner.Show("¡DIURÉTICO!", new Color(0.5f, 0.8f, 1f), 1.2f);
            GameplayEvents.RaiseMessage($"¡DIURÉTICO! Hace que el riñón saque más agua y sal en la orina: por {seconds:0} segundos todo FLUYE " +
                "hacia adelante y nadas más rápido. (Los médicos lo usan, por ejemplo, para la presión alta.)");
            Destroy(gameObject);
        }
    }
}
