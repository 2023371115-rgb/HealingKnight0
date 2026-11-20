using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using TMPro;
using UnityEngine;

namespace HealingKnight.Gameplay.Flight
{
    /// <summary>
    /// Letrero grande que aparece un momento arriba de la pantalla ("¡MÁS RÁPIDO!", "¡ADRENALINA!"...). Va pegado a
    /// la cámara; se crea solo la primera vez.
    /// </summary>
    public class SpeedBanner : MonoBehaviour
    {
        private static SpeedBanner instance;
        private TextMeshPro text;
        private float timer, life;
        private Color color;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        public static void Show(string message, Color tint, float seconds = 1.6f)
        {
            if (instance == null)
            {
                Camera cam = Camera.main;
                if (cam == null) return;
                var go = new GameObject("LetreroVelocidad");
                go.transform.SetParent(cam.transform, false);
                go.transform.localPosition = new Vector3(0f, cam.orthographicSize * 0.62f, 5f);
                instance = go.AddComponent<SpeedBanner>();
                instance.text = WorldText.Create(go.transform, "", Vector2.zero, new Vector2(12f, 1.6f), Color.white, 950, null, 9f);
                instance.text.outlineWidth = 0.25f;
                instance.text.outlineColor = new Color32(20, 6, 12, 255);
            }
            instance.text.text = message;
            instance.color = tint;
            instance.timer = instance.life = seconds;
            instance.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (text == null) return;
            timer -= Time.deltaTime;
            if (timer <= 0f) { gameObject.SetActive(false); return; }
            float age = life - timer;
            // Entra con un "pop" y se desvanece al final.
            float pop = age < 0.15f ? Mathf.Lerp(1.5f, 1f, age / 0.15f) : 1f;
            text.transform.localScale = Vector3.one * pop;
            Color c = color;
            c.a = Mathf.Clamp01(timer / 0.35f);
            text.color = c;
        }
    }
}
