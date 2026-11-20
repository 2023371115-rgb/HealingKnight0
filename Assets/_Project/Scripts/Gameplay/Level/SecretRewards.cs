using System.Collections.Generic;
using HealingKnight.Core.Combat;
using HealingKnight.Core.Flow;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// Premios de los niveles secretos, activos en TODOS los niveles (va uno en cada escena):
    ///  - "premio_linfocito" (Amígdalas): un linfocito B te sigue y dispara anticuerpos.
    ///  - "premio_escaneo" (Ojo): sobre cada enemigo aparece un puntito con el color del medicamento que le gana.
    /// </summary>
    public class SecretRewards : MonoBehaviour
    {
        public const string Companion = "premio_linfocito";
        public const string Scanner = "premio_escaneo";

        private bool scanner;
        private float scanTimer;
        private readonly Dictionary<EnemyHealth, SpriteRenderer> labels = new Dictionary<EnemyHealth, SpriteRenderer>();

        private void Start()
        {
            var save = GameSession.Save;
            if (save == null) return;
            scanner = save.HasSecret(Scanner);
            if (save.HasSecret(Companion))
            {
                PlayerController p = PlayerController.Current;
                var go = new GameObject("LinfocitoAliado");
                go.transform.position = p != null ? (Vector3)(p.Position + Vector2.up * 2.4f) : transform.position;
                go.AddComponent<AllyLymphocyte>().Configure(null, 0f, 2.6f, 4f, true);
            }
            if (scanner) GameplayEvents.RaiseMessage("NODO: Escáner activo. El puntito sobre cada enemigo es el color del medicamento que le gana.");
        }

        private void Update()
        {
            if (!scanner) return;
            scanTimer -= Time.deltaTime;
            if (scanTimer > 0f) return;
            scanTimer = 0.5f;
            PlayerController p = PlayerController.Current;
            if (p == null || p.Shooter == null) return;

            foreach (EnemyHealth e in EnemyHealth.Active)
            {
                if (e == null || !e.IsAlive || labels.ContainsKey(e) || e.Data == null || e.Table == null) continue;
                MedicationData best = null;
                foreach (MedicationData m in p.Shooter.Loadout)
                    if (m != null && e.Data.Evaluate(e.Table, m) == Effectiveness.Effective) { best = m; break; }
                if (best == null) continue;
                Transform t = new GameObject("Escaneo").transform;
                t.SetParent(e.transform, false);
                t.localPosition = new Vector3(0f, 2.3f, 0f);
                CodeSprites.Add(t, CodeSprites.Circle, new Color(0.05f, 0.05f, 0.1f), 60, Vector2.zero, new Vector2(0.42f, 0.42f));
                labels[e] = CodeSprites.Add(t, CodeSprites.Circle, best.color, 61, Vector2.zero, new Vector2(0.32f, 0.32f));
            }
        }
    }
}
