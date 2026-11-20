using System.Collections.Generic;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// AMÍGDALAS, paso 1: cuando muere un germen de la lista, suelta un ANTÍGENO (una "muestra" del germen).
    /// Se recoge con solo tocarlo y da vueltas alrededor del jugador (puedes llevar hasta 'maxCarried'), sin frenarte:
    /// sigues corriendo y disparando. Si te pegan se te cae UNO.
    /// Paso 2: en el CENTRO GERMINAL se entregan todos: cada antígeno se vuelve un LINFOCITO B que te sigue y dispara
    /// anticuerpos. Biología: las amígdalas atrapan gérmenes y presentan sus antígenos para que se fabriquen anticuerpos.
    /// </summary>
    public class AntigenSystem : MonoBehaviour
    {
        [SerializeField] private EnemyData[] sources;
        [SerializeField] private int maxOnGround = 5;
        [SerializeField] private int maxCarried = 3;
        [SerializeField, TextArea] private string firstDrop = "¡Un ANTÍGENO! Es una muestra del germen. Tócalo para recogerlo (puedes llevar 3) y llévalo al CENTRO GERMINAL: cada uno = un linfocito B aliado.";

        private readonly List<Antigen> active = new List<Antigen>();
        private readonly List<Antigen> carried = new List<Antigen>();
        private bool told, toldFull;

        public static AntigenSystem Current { get; private set; }
        public int CarriedCount { get { carried.RemoveAll(a => a == null); return carried.Count; } }
        public int MaxCarried => maxCarried;

        public void Configure(EnemyData[] enemies) => sources = enemies;

        private void OnEnable()
        {
            Current = this;
            EnemyHealth.AnyDied += OnEnemyDied;
            PlayerController.Spawned += Hook;
            if (PlayerController.Current != null) Hook(PlayerController.Current);
        }

        private void OnDisable()
        {
            if (Current == this) Current = null;
            EnemyHealth.AnyDied -= OnEnemyDied;
            PlayerController.Spawned -= Hook;
            if (hooked != null) hooked.Health.Damaged -= OnPlayerHurt;
        }

        private PlayerController hooked;
        private void Hook(PlayerController p)
        {
            if (hooked != null) hooked.Health.Damaged -= OnPlayerHurt;
            hooked = p;
            if (p != null) p.Health.Damaged += OnPlayerHurt;
        }

        private void OnEnemyDied(EnemyHealth e)
        {
            if (e == null || e.Data == null || sources == null || System.Array.IndexOf(sources, e.Data) < 0) return;
            active.RemoveAll(x => x == null);
            if (active.Count >= maxOnGround) return;
            var go = new GameObject("Antigeno");
            go.transform.position = e.transform.position + Vector3.up * 0.6f;
            Antigen a = go.AddComponent<Antigen>();
            active.Add(a);
            if (!told && !string.IsNullOrEmpty(firstDrop)) { told = true; GameplayEvents.RaiseMessage(firstDrop); }
        }

        public bool IsCarried(Antigen a) => carried.Contains(a);
        public int IndexOf(Antigen a) => carried.IndexOf(a);

        public bool TryCarry(Antigen a)
        {
            carried.RemoveAll(x => x == null);
            if (carried.Count >= maxCarried)
            {
                if (!toldFull) { toldFull = true; GameplayEvents.RaiseMessage("Ya llevas 3 antígenos: entrégalos en el CENTRO GERMINAL."); }
                return false;
            }
            carried.Add(a);
            Sfx.Effect("Recoger_Capsula", 0.5f);
            return true;
        }

        /// <summary>Lo llama el centro germinal: entrega todos los que llevas. Devuelve cuántos eran.</summary>
        public int Deliver()
        {
            carried.RemoveAll(x => x == null);
            int n = carried.Count;
            foreach (Antigen a in carried)
            {
                active.Remove(a);
                if (a != null) Destroy(a.gameObject);
            }
            carried.Clear();
            return n;
        }

        private void OnPlayerHurt()
        {
            carried.RemoveAll(x => x == null);
            if (carried.Count == 0) return;
            Antigen a = carried[carried.Count - 1];
            carried.RemoveAt(carried.Count - 1);
            a.Drop();
            GameplayEvents.RaiseMessage("¡Se te cayó un antígeno! Recógelo otra vez.");
        }
    }
}
