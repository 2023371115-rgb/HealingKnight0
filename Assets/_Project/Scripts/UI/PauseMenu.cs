using HealingKnight.Core.Flow;
using HealingKnight.Gameplay.Player;
using UnityEngine;
using UnityEngine.UI;

namespace HealingKnight.UI
{
    /// <summary>Menú de pausa (Esc / Start): Reanudar, Reiniciar nivel, Mapa del cuerpo, Menú principal.</summary>
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
            root = UIFactory.Panel(canvas.transform, "Overlay", new Color(0f, 0f, 0f, 0.7f)).gameObject;
            UIFactory.Stretch(UIFactory.Rect(root));

            var title = UIFactory.Text(root.transform, "PAUSA", 90f, UIFactory.TextColor);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(800f, 120f));

            var column = new GameObject("Buttons", typeof(RectTransform));
            column.transform.SetParent(root.transform, false);
            UIFactory.Place(UIFactory.Rect(column), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(520f, 460f));
            UIFactory.VerticalLayout(column, 24f);

            Vector2 size = new Vector2(480f, 84f);
            resumeButton = UIFactory.Button(column.transform, "Reanudar", Resume, size);
            UIFactory.Button(column.transform, "Reiniciar nivel", GameSession.ReloadCurrentScene, size);
            UIFactory.Button(column.transform, "Mapa del cuerpo", GameSession.GoToBodyMap, size);
            UIFactory.Button(column.transform, "Menú principal", GameSession.GoToMainMenu, size);

            root.SetActive(false);
        }

        private void Update()
        {
            PlayerController player = PlayerController.Current;
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
