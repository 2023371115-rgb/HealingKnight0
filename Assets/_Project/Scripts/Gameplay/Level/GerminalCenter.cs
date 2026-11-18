using System.Collections.Generic;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// CENTRO GERMINAL (amígdalas): aquí los linfocitos B aprenden a reconocer al germen. Entrega un antígeno y
    /// sale un LINFOCITO B aliado que dispara anticuerpos un rato. Máximo 'maxAllies' a la vez.
    /// </summary>
    public class GerminalCenter : MonoBehaviour
    {
        [SerializeField] private float radius = 1.8f;
        [SerializeField] private int maxAllies = 4;
        [SerializeField] private float allyLife = 30f;
        [SerializeField, TextArea] private string message = "Centro germinal: aquí se fabrican los anticuerpos. Trae antígenos.";

        private readonly List<AllyLymphocyte> allies = new List<AllyLymphocyte>();
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
            if (sys.Carried == null || d > radius + 0.6f) return;

            sys.Deliver();
            pulse = 1f;
            Sfx.EffectAt("Bomba_Ganada", center, 0.9f);
            if (allies.Count >= maxAllies && allies.Count > 0) { Destroy(allies[0].gameObject); allies.RemoveAt(0); }
            var go = new GameObject("LinfocitoB");
            go.transform.position = center + Random.insideUnitCircle * 0.8f;
            AllyLymphocyte ally = go.AddComponent<AllyLymphocyte>();
            ally.Configure(transform, allyLife, 1.4f, 9f, false);
            allies.Add(ally);
            GameplayEvents.RaiseMessage("¡Linfocito B listo! Dispara ANTICUERPOS a los gérmenes.");
        }
    }
}
