using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>Punto de regreso invisible: al pasar por aquí, si te caes o pierdes, vuelves a partir de esta X.</summary>
    public class CheckpointTrigger : MonoBehaviour
    {
        private bool done;

        private void Update()
        {
            if (done) return;
            PlayerController p = PlayerController.Current;
            if (p == null || p.Position.x < transform.position.x || LevelManager.Current == null) return;
            done = true;
            LevelManager.Current.SetCheckpoint(transform.position.x);
        }
    }
}
