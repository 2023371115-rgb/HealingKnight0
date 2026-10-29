using System.Collections.Generic;
using HealingKnight.Core.Flow;
using HealingKnight.Core.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HealingKnight.UI
{
    /// <summary>
    /// Mapa del cuerpo: muestra los sistemas (I–IV) y sus niveles según el progreso guardado.
    /// - Sistema bloqueado: gris con candado.
    /// - Nivel sin escena todavía: "Próximamente".
    /// - Nivel completado: ✓ y se puede rejugar.
    /// Todo sale del GameCatalog: agregar un sistema o nivel no requiere tocar esta pantalla.
    /// </summary>
    public class BodyMapScreen : MonoBehaviour
    {
        private Button firstPlayable;

        private void Awake()
        {
            Time.timeScale = 1f;
            Build();
        }

        private void Start()
        {
            UIFactory.Select(firstPlayable);
        }

        private void Build()
        {
            Canvas canvas = UIFactory.CreateCanvas("BodyMap Canvas", 0, transform);
            Image bg = UIFactory.Panel(canvas.transform, "Background", new Color(0.12f, 0.06f, 0.1f, 1f));
            UIFactory.Stretch(bg.rectTransform);

            var title = UIFactory.Text(canvas.transform, "MAPA DEL CUERPO · PACIENTE 001", 64f, UIFactory.TextColor);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1600f, 90f));

            GameCatalog catalog = GameSession.Catalog;
            ProgressService progress = GameSession.Progress;
            if (catalog == null || progress == null)
            {
                var error = UIFactory.Text(canvas.transform, "Falta el GameCatalog. Ejecuta 'Healing Knight > Crear proyecto base'.", 36f, Color.red);
                UIFactory.Place(error.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600f, 200f));
                return;
            }

            var formulas = UIFactory.Text(canvas.transform, $"Fórmulas obtenidas: {GameSession.Save.formulas.Count} / {catalog.systems.Count}",
                30f, UIFactory.AccentColor);
            UIFactory.Place(formulas.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(1000f, 50f));

            var row = new GameObject("Systems", typeof(RectTransform));
            row.transform.SetParent(canvas.transform, false);
            UIFactory.Place(UIFactory.Rect(row), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(1820f, 700f));
            UIFactory.HorizontalLayout(row, 24f);

            foreach (SystemData system in catalog.systems)
            {
                if (system != null) BuildSystemCard(row.transform, system, progress);
            }

            Button back = UIFactory.Button(canvas.transform, "← Menú principal", GameSession.GoToMainMenu, new Vector2(420f, 76f), 32f);
            UIFactory.Place((RectTransform)back.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(420f, 76f));
            if (firstPlayable == null) firstPlayable = back;
        }

        private void BuildSystemCard(Transform parent, SystemData system, ProgressService progress)
        {
            bool unlocked = progress.IsSystemUnlocked(system);
            bool completed = progress.IsSystemCompleted(system);

            Color cardColor = unlocked ? Color.Lerp(system.mapColor, Color.black, 0.55f) : new Color(0.18f, 0.16f, 0.18f, 1f);
            Image card = UIFactory.Panel(parent, "System_" + system.id, cardColor);
            card.rectTransform.sizeDelta = new Vector2(430f, 680f);

            var header = UIFactory.Text(card.transform, $"{system.mapNumber}", 90f, unlocked ? system.mapColor : Color.gray);
            UIFactory.Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(400f, 110f));

            var name = UIFactory.Text(card.transform, system.displayName, 34f, unlocked ? UIFactory.TextColor : Color.gray);
            UIFactory.Place(name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(410f, 50f));

            string status = completed ? "Desinfectado" : unlocked ? "Infección activa" : "Bloqueado";
            var statusText = UIFactory.Text(card.transform, status, 26f, completed ? new Color(0.5f, 1f, 0.6f) : unlocked ? new Color(1f, 0.6f, 0.6f) : Color.gray);
            UIFactory.Place(statusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(410f, 40f));

            var column = new GameObject("Levels", typeof(RectTransform));
            column.transform.SetParent(card.transform, false);
            UIFactory.Place(UIFactory.Rect(column), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -230f), new Vector2(400f, 430f));
            UIFactory.VerticalLayout(column, 14f, TextAnchor.UpperCenter);

            foreach (LevelData level in system.levels)
            {
                if (level == null || !progress.IsLevelVisible(level)) continue; // los secretos no se muestran
                BuildLevelButton(column.transform, level, progress, unlocked);
            }
        }

        private void BuildLevelButton(Transform parent, LevelData level, ProgressService progress, bool systemUnlocked)
        {
            bool playable = progress.IsLevelPlayable(level);
            bool done = progress.IsLevelCompleted(level);

            string label = level.hidden ? "Secreto: " + level.displayName : level.displayName;
            if (done) label += "\n<size=70%>Completado</size>";
            else if (systemUnlocked && !level.HasScene) label += "\n<size=70%>Próximamente</size>";
            else if (!playable) label += "\n<size=70%>Bloqueado</size>";

            LevelData captured = level;
            Button button = UIFactory.Button(parent, label, () => GameSession.StartLevel(captured), new Vector2(380f, 96f), 28f);
            button.interactable = playable;
            if (!playable) button.GetComponent<Image>().color = UIFactory.ButtonDisabledColor;

            // Selecciona por defecto el primer nivel jugable que no se haya completado.
            if (playable && !done && firstPlayable == null) firstPlayable = button;
        }
    }
}
