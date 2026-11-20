using HealingKnight.Core.Damage;
using HealingKnight.Core.Flow;
using HealingKnight.Core.Pooling;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.CameraSystem;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>Dónde están los quistes de la retina y el cristalino en esta escena (el jefe Toxoplasma los usa al aparecer).</summary>
    public class CystLayout : MonoBehaviour
    {
        [SerializeField] private Vector2[] spots;
        [SerializeField] private Vector2 lens;
        [SerializeField] private Sprite lensArt;
        public Vector2[] Spots => spots;
        public Vector2 Lens => lens;
        public Sprite LensArt => lensArt;
        public void Configure(Vector2[] positions) => spots = positions;
        public void Configure(Vector2[] positions, Vector2 lensPosition, Sprite lensDrawing)
        {
            spots = positions;
            lens = lensPosition;
            lensArt = lensDrawing;
        }
    }
}
