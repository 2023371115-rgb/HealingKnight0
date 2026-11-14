using HealingKnight.Gameplay.Audio;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// El PÍLORO: el músculo en forma de anillo que separa el estómago del intestino delgado.
    /// Es la salida del nivel: se queda cerrado mientras la arena del jefe no esté limpia y se abre
    /// (con un brillo y un sonido) en cuanto vences al Bezoar.
    /// 'closed' y 'open' son los dos dibujos (placeholder o los sprites de la IA).
    /// </summary>
    public class PyloricDoor : MonoBehaviour
    {
        [SerializeField] private ArenaZone arena;
        [SerializeField] private GameObject closed;
        [SerializeField] private GameObject open;
        [SerializeField, TextArea] private string openMessage = "NODO: El píloro se abrió. ¡Pasa al intestino delgado!";

        private bool isOpen;
        private float pop;

        public void Configure(ArenaZone bossArena, GameObject closedArt, GameObject openArt)
        {
            arena = bossArena;
            closed = closedArt;
            open = openArt;
        }

        private void Start() => Show(false);

        private void Update()
        {
            if (!isOpen && arena != null && arena.IsCleared)
            {
                isOpen = true;
                Show(true);
                pop = 1f;
                Sfx.Effect("Diente_Secreto", 0.8f);
                if (!string.IsNullOrEmpty(openMessage)) GameplayEvents.RaiseMessage(openMessage);
            }

            // Al abrirse "late" una vez; cerrado respira despacio.
            pop = Mathf.MoveTowards(pop, 0f, Time.deltaTime * 2f);
            float breathe = 1f + Mathf.Sin(Time.time * (isOpen ? 3f : 1.5f)) * 0.02f + Mathf.Sin(pop * Mathf.PI) * 0.12f;
            transform.localScale = new Vector3(breathe, breathe, 1f);
        }

        private void Show(bool opened)
        {
            if (closed != null) closed.SetActive(!opened);
            if (open != null) open.SetActive(opened);
        }
    }
}
