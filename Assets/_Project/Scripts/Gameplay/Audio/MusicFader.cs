using UnityEngine;

namespace HealingKnight.Gameplay.Audio
{
    /// <summary>Va en el objeto "Musica" (lo crea Music): hace los fundidos entre pistas.</summary>
    public class MusicFader : MonoBehaviour
    {
        private void Update() => Music.Tick();
    }
}
