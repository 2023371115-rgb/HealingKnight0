using UnityEngine;

namespace HealingKnight.Gameplay.Movement
{
    /// <summary>
    /// Capas de física del juego (definidas en Project Settings → Tags and Layers).
    /// Los personajes solo chocan contra el piso y las plataformas: entre ellos se atraviesan
    /// (como en Metal Slug); el daño se resuelve con Hurtbox, no con colisiones.
    /// </summary>
    public static class PhysicsLayers
    {
        public const string GroundName = "Ground";
        public const string PlayerProjectileName = "PlayerProjectile";
        public const string PlayerName = "Player";
        public const string EnemyName = "Enemy";

        public static int Ground => Resolve(GroundName, 6);
        public static int PlayerProjectile => Resolve(PlayerProjectileName, 7);
        public static int Player => Resolve(PlayerName, 8);
        public static int Enemy => Resolve(EnemyName, 9);

        public static LayerMask GroundMask => 1 << Ground;

        private static int Resolve(string layerName, int fallback)
        {
            int layer = LayerMask.NameToLayer(layerName);
            return layer >= 0 ? layer : fallback;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            Physics2D.IgnoreLayerCollision(Player, Enemy, true);
            Physics2D.IgnoreLayerCollision(Enemy, Enemy, true);
            Physics2D.IgnoreLayerCollision(Player, Player, true);
            Physics2D.IgnoreLayerCollision(PlayerProjectile, Player, true);
            Physics2D.IgnoreLayerCollision(PlayerProjectile, Enemy, true);
        }
    }
}
