using System.Collections.Generic;
using HealingKnight.Core.Combat;
using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Flight
{
    /// <summary>
    /// GRAVEDAD en las venas de la pierna (tramo 5): mientras vas por aquí, algo te jala hacia ABAJO (hacia atrás del
    /// camino). La sangre sube contra la gravedad; por eso las venas tienen válvulas.
    /// </summary>
    public class GravityPull : MonoBehaviour
    {
        [SerializeField] private float fromX, toX;
        [SerializeField] private float pull = 1.3f;

        public void Configure(float startX, float endX, float strength)
        {
            fromX = startX;
            toX = endX;
            pull = strength;
        }

        private void Update()
        {
            PlayerController player = PlayerController.Current;
            if (player == null || player.Flight == null || !player.Health.IsAlive) return;
            float x = player.Position.x;
            if (x < fromX || x > toX) return;
            player.Flight.AddPush(new Vector2(-pull, 0f));
        }
    }
}
