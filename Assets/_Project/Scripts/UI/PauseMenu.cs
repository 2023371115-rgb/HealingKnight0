using HealingKnight.Core.Flow;
using HealingKnight.Gameplay.Player;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HealingKnight.UI
{
    /// <summary>Menú de pausa (Esc / Start): Reanudar, Bestiario, Reiniciar nivel, Mapa del cuerpo, Menú principal.</summary>
    public class PauseMenu : MonoBehaviour
    {
        private GameObject root;
        private Button resumeButton;
        private bool paused;

        public static bool IsPaused { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => IsPaused = false;

        private void Awake()
        {
            Canvas canvas = UIFactory.CreateCanvas("Pause Canvas", 50, transform);
            Image overlay = UIFactory.Panel(canvas.transform, "Overlay", new Color(0.03f, 0.01f, 0.04f, 0.72f));
            UIFactory.Stretch(overlay.rectTransform);
            root = overlay.gameObject;

            Image card = CartoonUI.Blob(root.transform, "TarjetaPausa", new Color(0.28f, 0.08f, 0.17f, 0.98f), new Vector2(900f, 840f), 10f, 1.2f);
            Transform content = card.transform;
            TextMeshProUGUI title = CartoonUI.Text(content, "PAUSA", 78f, CartoonUI.Cream, true, TextAlignmentOptions.Center, 0.24f);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -65f), new Vector2(760f, 110f));

            var column = new GameObject("Buttons", typeof(RectTransform));
            column.transform.SetParent(content, false);
            UIFactory.Place(UIFactory.Rect(column), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(700f, 560f));
            UIFactory.VerticalLayout(column, 16f);

            Vector2 size = new Vector2(650f, 96f);
            resumeButton = CartoonUI.Button(column.transform, "Reanudar", Resume, size, CartoonUI.Mint, 36f);
            Button bestiary = null;
            bestiary = CartoonUI.Button(column.transform, "Bestiario", () => BestiaryScreen.Open(() => UIFactory.Select(bestiary)),
                size, new Color(0.95f, 0.6f, 0.25f), 34f);
            CartoonUI.Button(column.transform, "Reiniciar nivel", GameSession.ReloadCurrentScene, size, CartoonUI.Red, 34f);
            CartoonUI.Button(column.transform, "Mapa del cuerpo", GameSession.GoToBodyMap, size, new Color(0.35f, 0.62f, 0.82f), 34f);
            CartoonUI.Button(column.transform, "Menú principal", GameSession.GoToMainMenu, size, new Color(0.57f, 0.34f, 0.61f), 34f);

            root.SetActive(false);
        }

        private void Update()
        {
            PlayerController player = PlayerController.Current;
            if (BestiaryScreen.IsOpen) return;        // Esc / B / Start cierran el bestiario, no la pausa
            if (player != null && player.Input.PausePressed) Toggle();
        }

        private void OnDestroy()
        {
            if (paused)
            {
                Time.timeScale = 1f;
                IsPaused = false;
            }
        }

        public void Toggle()
        {
            if (paused) Resume();
            else Pause();
        }

        public void Pause()
        {
            paused = IsPaused = true;
            Time.timeScale = 0f;
            root.SetActive(true);
            if (PlayerController.Current != null) PlayerController.Current.Input.Blocked = true;
            UIFactory.Select(resumeButton);
        }

        public void Resume()
        {
            paused = IsPaused = false;
            Time.timeScale = 1f;
            root.SetActive(false);
            if (PlayerController.Current != null && PlayerController.Current.CurrentState != PlayerController.State.Dead)
                PlayerController.Current.Input.Blocked = false;
        }
    }
}
