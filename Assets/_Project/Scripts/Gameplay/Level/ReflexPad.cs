using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.CameraSystem;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// ARCO REFLEJO: el reflejo rotuliano (el martillito en la rodilla). El estímulo llega a la médula y ella
    /// responde SOLA, sin preguntarle al cerebro: por eso es tan rápido. Pisa el receptor y, un instante después,
    /// la médula te lanza muy alto. Se dibuja por código.
    /// </summary>
    public class ReflexPad : MonoBehaviour
    {
        [SerializeField] private float width = 1.8f;
        [SerializeField] private float height = 7f;
        [SerializeField] private float delay = 0.25f;
        [SerializeField, TextArea] private string firstMessage = "¡Arco reflejo! La médula responde sola, sin esperar al cerebro.";

        private float armed = -1f, cooldown;
        private Transform pad;
        private static bool explained;

        public void Configure(float padWidth, float launch) { width = padWidth; height = launch; }

        private void Start()
        {
            CodeSprites.OneWay(gameObject, width);
            pad = new GameObject("Receptor").transform;
            pad.SetParent(transform, false);
            Sprite c = CodeSprites.Circle;
            CodeSprites.Add(pad, c, new Color(0.08f, 0.05f, 0.2f, 1f), -10, new Vector2(0f, -0.2f), new Vector2(width + 0.2f, 0.6f));
            CodeSprites.Add(pad, c, new Color(0.95f, 0.45f, 0.7f, 1f), -9, new Vector2(0f, -0.18f), new Vector2(width, 0.46f));
            CodeSprites.Add(pad, c, new Color(1f, 0.85f, 0.95f, 0.8f), -8, new Vector2(-width * 0.2f, -0.08f), new Vector2(width * 0.35f, 0.12f));
            // Flecha de "¡arriba!".
            CodeSprites.Add(pad, CodeSprites.Square, new Color(1f, 0.9f, 0.35f, 0.8f), -8, new Vector2(0f, 0.55f), new Vector2(0.14f, 0.5f));
            CodeSprites.Add(pad, c, new Color(1f, 0.9f, 0.35f, 0.8f), -8, new Vector2(0f, 0.85f), new Vector2(0.4f, 0.2f));
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (cooldown > 0f) cooldown -= dt;
            PlayerController player = PlayerController.Current;
            bool on = CodeSprites.Standing(player, transform.position, width);
            if (armed < 0f && on && cooldown <= 0f) { armed = delay; Sfx.EffectAt("Neurona_Carga", transform.position, 0.6f); }
            if (armed >= 0f)
            {
                armed -= dt;
                pad.localScale = new Vector3(1.1f, 0.7f, 1f);
                if (armed < 0f)
                {
                    pad.localScale = Vector3.one;
                    cooldown = 0.4f;
                    if (CodeSprites.Standing(player, transform.position, width + 0.4f))
                    {
                        player.Bounce(height, 15.8f * Mathf.Sqrt(height / 4f));
                        Sfx.Effect("Neurona_Descarga", 0.8f);
                        Camera cam = Camera.main;
                        if (cam != null && cam.TryGetComponent(out GameCamera gc)) gc.Shake(0.12f, 0.15f);
                        if (!explained && !string.IsNullOrEmpty(firstMessage)) { explained = true; GameplayEvents.RaiseMessage(firstMessage); }
                    }
                }
            }
            else pad.localScale = Vector3.Lerp(pad.localScale, Vector3.one, 1f - Mathf.Exp(-12f * dt));
        }
    }
}
