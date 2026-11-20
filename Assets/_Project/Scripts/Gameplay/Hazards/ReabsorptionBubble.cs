using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// REABSORCIÓN: burbujas de GLUCOSA, AMINOÁCIDOS o AGUA que flotan en el túbulo. El riñón las regresa a la sangre
    /// (no se desperdician en la orina): al tocarlas te curan un poquito. La primera vez NODO explica qué es.
    /// </summary>
    public class ReabsorptionBubble : MonoBehaviour
    {
        public enum Kind { Glucosa, Aminoacido, Agua }

        [SerializeField] private Kind kind;
        [SerializeField] private float heal = 4f;

        private static bool told;
        private Transform visual;
        private float seed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => told = false;

        public void Configure(Kind what, float amount) { kind = what; heal = amount; }

        private void Start()
        {
            seed = transform.position.x;
            visual = new GameObject("Dibujo").transform;
            visual.SetParent(transform, false);
            Color c = kind == Kind.Glucosa ? new Color(1f, 0.9f, 0.4f) : kind == Kind.Aminoacido ? new Color(0.55f, 0.95f, 0.6f) : new Color(0.55f, 0.85f, 1f);
            CodeSprites.Add(visual, CodeSprites.Glow, new Color(c.r, c.g, c.b, 0.5f), 34, Vector2.zero, Vector2.one * 1.3f, "Aura");
            CodeSprites.Add(visual, CodeSprites.Circle, new Color(1f, 1f, 1f, 0.35f), 35, Vector2.zero, Vector2.one * 0.75f, "Burbuja");
            CodeSprites.Add(visual, CodeSprites.Ring, Color.white, 36, Vector2.zero, Vector2.one * 0.78f, "Borde");
            if (kind == Kind.Glucosa)
            {
                // Glucosa: un anillo de 6 lados.
                for (int i = 0; i < 6; i++)
                {
                    float a = i * Mathf.PI / 3f;
                    CodeSprites.Add(visual, CodeSprites.Circle, c, 37, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.18f, Vector2.one * 0.13f, "Atomo");
                }
            }
            else if (kind == Kind.Aminoacido)
            {
                // Aminoácidos: cadenita de cuentas.
                for (int i = 0; i < 4; i++)
                    CodeSprites.Add(visual, CodeSprites.Circle, c, 37, new Vector2(-0.21f + i * 0.14f, Mathf.Sin(i * 1.7f) * 0.07f), Vector2.one * 0.14f, "Cuenta");
            }
            else CodeSprites.Add(visual, CodeSprites.Circle, c, 37, new Vector2(0f, -0.03f), new Vector2(0.24f, 0.3f), "Gota");
        }

        private void Update()
        {
            if (visual == null) return;
            visual.localPosition = new Vector3(Mathf.Sin(Time.time * 1.3f + seed) * 0.2f, Mathf.Sin(Time.time * 1.9f + seed) * 0.25f, 0f);
            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive) return;
            if (((Vector2)visual.position - player.Motor.Center).sqrMagnitude > 0.9f * 0.9f) return;
            player.Health.Heal(heal);
            Sfx.EffectAt("Recoger_Vida", visual.position, 0.5f);
            if (!told)
            {
                told = true;
                GameplayEvents.RaiseMessage("¡REABSORCIÓN! El riñón filtra la sangre y luego RECUPERA lo que sí sirve: glucosa, aminoácidos y " +
                    "casi toda el agua. Solo lo que sobra se va en la orina. Cada burbuja te cura un poquito.");
            }
            Destroy(gameObject);
        }
    }
}
