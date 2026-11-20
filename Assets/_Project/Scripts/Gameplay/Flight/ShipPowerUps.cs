using System.Collections.Generic;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Flight
{
    /// <summary>
    /// Los power-ups que trae la nave en este nivel: el TRIPLE DISPARO (con su tiempo) y los LINFOCITOS que la escoltan.
    /// Lo agrega FlightPowerUp al jugador la primera vez que agarras algo. Duran todo el nivel (también al reaparecer).
    /// </summary>
    public class ShipPowerUps : MonoBehaviour
    {
        public const float TripleSeconds = 15f;
        public const int MaxAllies = 3, MaxMissiles = 15;

        private PlayerController player;
        private float tripleLeft;
        private readonly List<FlightAlly> allies = new List<FlightAlly>();
        private SpriteRenderer tripleAura;

        public int Allies => allies.Count;
        public bool TripleShot => tripleLeft > 0f;

        public static ShipPowerUps For(PlayerController p)
        {
            ShipPowerUps s = p.GetComponent<ShipPowerUps>();
            if (s == null) { s = p.gameObject.AddComponent<ShipPowerUps>(); s.player = p; }
            return s;
        }

        private void Awake() => player = GetComponent<PlayerController>();

        public void GiveTripleShot(float seconds)
        {
            tripleLeft = Mathf.Min(tripleLeft + seconds, seconds * 2f);
            if (player.Shooter != null) player.Shooter.ExtraShots = 2;
            if (tripleAura == null)
                tripleAura = CodeSprites.Add(transform, CodeSprites.Ring, new Color(1f, 0.85f, 0.3f, 0.6f), 30, new Vector2(0.1f, 0.65f), Vector2.one * 2.3f, "AuraTriple");
            tripleAura.enabled = true;
        }

        public void GiveMissiles(int amount)
        {
            if (player.Bombs != null) player.Bombs.AddBonus(amount, MaxMissiles);
        }

        public bool AddAlly()
        {
            if (allies.Count >= MaxAllies) return false;
            var go = new GameObject("LinfocitoEscolta");
            go.transform.position = transform.position;
            FlightAlly ally = go.AddComponent<FlightAlly>();
            ally.Begin(player, allies.Count);
            allies.Add(ally);
            return true;
        }

        private void Update()
        {
            if (tripleLeft <= 0f) return;
            tripleLeft -= Time.deltaTime;
            if (tripleAura != null)
            {
                tripleAura.transform.localScale = Vector3.one * player.Size * (2.3f + Mathf.Sin(Time.time * 5f) * 0.1f);
                // Parpadea los últimos 3 segundos: ya se va a acabar.
                tripleAura.enabled = tripleLeft > 3f || Mathf.Repeat(tripleLeft * 4f, 1f) > 0.4f;
            }
            if (tripleLeft <= 0f)
            {
                if (player.Shooter != null) player.Shooter.ExtraShots = 0;
                if (tripleAura != null) tripleAura.enabled = false;
                GameplayEvents.RaiseMessage("NODO: Se acabó el TRIPLE DISPARO.");
            }
        }

        private void OnDestroy()
        {
            foreach (FlightAlly a in allies) if (a != null) Destroy(a.gameObject);
        }
    }
}
