using System.Collections.Generic;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// CENTRO GERMINAL (amígdalas): aquí los linfocitos B aprenden a reconocer al germen. Pasa por él con antígenos y
    /// se entregan todos: cada uno se vuelve un LINFOCITO B aliado que TE SIGUE y dispara anticuerpos un rato.
    /// Máximo 'maxAllies' a la vez (todos los centros comparten la cuenta).
    /// </summary>
    public class GerminalCenter : MonoBehaviour
    {
        [SerializeField] private float radius = 2f;
        [SerializeField] private int maxAllies = 3;
        [SerializeField] private float allyLife = 35f;
        [SerializeField, TextArea] private string message = "CENTRO GERMINAL: aquí se fabrican anticuerpos. Mata gérmenes, recoge sus ANTÍGENOS y pasa por aquí: cada antígeno = un linfocito B que te sigue.";

        private static readonly List<AllyLymphocyte> allies = new List<AllyLymphocyte>();
        private Transform visual;
        private bool said;
        private float pulse;

        private void Start()
        {
            visual = new GameObject("Centro").transform;
            visual.SetParent(transform, false);
            Sprite c = CodeSprites.Circle;
            CodeSprites.Add(visual, c, new Color(0.7f, 0.5f, 1f, 0.25f), -20, new Vector2(0f, 1.6f), new Vector2(4.2f, 4.2f));
            CodeSprites.Add(visual, c, new Color(0.2f, 0.1f, 0.35f), -19, new Vector2(0f, 1.6f), new Vector2(3f, 3f));
            CodeSprites.Add(visual, c, new Color(0.75f, 0.6f, 1f), -18, new Vector2(0f, 1.6f), new Vector2(2.8f, 2.8f));
            for (int i = 0; i < 7; i++)
            {
                float a = i * Mathf.PI * 2f / 7f;
                CodeSprites.Add(visual, c, new Color(0.9f, 0.85f, 1f), -17, new Vector2(Mathf.Cos(a) * 0.9f, 1.6f + Mathf.Sin(a) * 0.9f), new Vector2(0.6f, 0.6f));
            }
            CodeSprites.Add(visual, c, new Color(1f, 0.9f, 0.5f), -16, new Vector2(0f, 1.6f), new Vector2(0.8f, 0.8f));
        }

        private void Update()
        {
            pulse = Mathf.MoveTowards(pulse, 0f, Time.deltaTime * 2f);
            visual.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 2f) * 0.03f + pulse * 0.2f);
            allies.RemoveAll(a => a == null);

            PlayerController p = PlayerController.Current;
            AntigenSystem sys = AntigenSystem.Current;
            if (p == null || sys == null) return;
            Vector2 center = (Vector2)transform.position + Vector2.up * 1.6f;
            float d = Vector2.Distance(p.Position + Vector2.up * 0.9f, center);
            if (!said && d < 8f && !string.IsNullOrEmpty(message)) { said = true; GameplayEvents.RaiseMessage(message); }
            // Brilla más cuando llegas con antígenos (para que se note que ahí se entregan).
            if (sys.CarriedCount > 0) visual.localScale *= 1f + 0.08f * Mathf.Abs(Mathf.Sin(Time.time * 6f));
            if (sys.CarriedCount == 0 || d > radius + 0.6f) return;

            int n = sys.Deliver();
            pulse = 1f;
            Sfx.EffectAt("Inmune_Activa", center, 0.9f);
            for (int i = 0; i < n; i++)
            {
                if (allies.Count >= maxAllies && allies.Count > 0) { if (allies[0] != null) Destroy(allies[0].gameObject); allies.RemoveAt(0); }
                var go = new GameObject("LinfocitoB");
                go.transform.position = center + Random.insideUnitCircle * 0.8f;
                AllyLymphocyte ally = go.AddComponent<AllyLymphocyte>();
                ally.Configure(transform, allyLife, 1.2f, 9f, true);
                allies.Add(ally);
            }
            GameplayEvents.RaiseMessage(n == 1 ? "¡Un LINFOCITO B! Te sigue y dispara ANTICUERPOS a los gérmenes."
                                               : $"¡{n} LINFOCITOS B! Te siguen y disparan ANTICUERPOS a los gérmenes.");
        }
    }
}
