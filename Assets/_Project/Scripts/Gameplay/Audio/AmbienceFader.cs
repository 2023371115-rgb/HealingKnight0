using UnityEngine;

namespace HealingKnight.Gameplay.Audio
{
    /// <summary>Va en el objeto "Ambiente" (lo crea Ambience): hace los fundidos del sonido de fondo.</summary>
    public class AmbienceFader : MonoBehaviour
    {
        private void Update() => Ambience.Tick();
    }
}
