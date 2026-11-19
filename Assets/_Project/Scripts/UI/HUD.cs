using HealingKnight.Core.Combat;
using HealingKnight.Core.Flow;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HealingKnight.UI
{
    /// <summary>
    /// HUD del nivel: salud, energía del escudo, medicamento seleccionado, nombre del nivel, el globo de NODO
    /// con las indicaciones (NodoBubble), "¡AVANZA!" y la ayuda de controles. Se construye solo al iniciar la escena.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        [Tooltip("Letra del juego (Titan One). La asigna SceneSetup; también la usan los letreros del nivel.")]
        [SerializeField] private Font titleFont;
        [Tooltip("Sistema del nivel (Digestivo, Respiratorio, Nervioso): da el color del globo de NODO.")]
        [SerializeField] private string theme = "Digestivo";

        private NodoBubble nodo;
        private UIBar healthBar;
        private UIBar shieldBar;
        private TextMeshProUGUI medicationText;
        private Image medicationSwatch;
        private TextMeshProUGUI goText;
        private TextMeshProUGUI levelText;
        private TextMeshProUGUI controlsText;
        private TextMeshProUGUI bombsText;
        private UIBar bombChargeBar;
        private GameObject brainRoot;
        private UIBar brainBar;
        private BrainHealth brain;
        private GameObject bossRoot;
        private UIBar bossBar;
        private TextMeshProUGUI bossNameText;
        private EnemyHealth boss;

        private PlayerController player;
        private float goTimer;
        private float controlsTimer = 12f;

        private void Awake()
        {
            if (titleFont != null)
            {
                CartoonUI.SetFonts(titleFont, titleFont);
                if (WorldText.DefaultFont == null) WorldText.DefaultFont = titleFont;
            }
            Build();
        }

        private void OnEnable()
        {
            PlayerController.Spawned += Bind;
            GameplayEvents.Message += ShowMessage;
            GameplayEvents.ArenaCleared += OnArenaCleared;
            GameplayEvents.ArenaStarted += OnArenaStarted;
            GameplayEvents.BossStarted += OnBossStarted;
            GameplayEvents.BossEnded += OnBossEnded;
            PlayerControls.Changed += RefreshControlsHint;
            if (PlayerController.Current != null) Bind(PlayerController.Current);
        }

        private void OnDisable()
        {
            PlayerController.Spawned -= Bind;
            GameplayEvents.Message -= ShowMessage;
            GameplayEvents.ArenaCleared -= OnArenaCleared;
            GameplayEvents.ArenaStarted -= OnArenaStarted;
            GameplayEvents.BossStarted -= OnBossStarted;
            GameplayEvents.BossEnded -= OnBossEnded;
            PlayerControls.Changed -= RefreshControlsHint;
            Unbind();
            if (brain != null) brain.Changed -= OnBrainChanged;
        }

        private void Start()
        {
            brain = BrainHealth.Current;
            if (brain != null)
            {
                brainRoot.SetActive(true);
                brainBar.SetValue(brain.Health01, true);
                brain.Changed += OnBrainChanged;
            }

            var level = GameSession.CurrentLevel;
            var system = GameSession.CurrentSystem;
            levelText.text = level != null
                ? $"{(system != null ? system.displayName.ToUpperInvariant() + "  ·  " : "")}{level.displayName}"
                : "SANDBOX";
        }

        private void Build()
        {
            Canvas canvas = UIFactory.CreateCanvas("HUD Canvas", 10, transform);
            Transform root = canvas.transform;

            // Panel superior izquierdo
            Image panel = UIFactory.Panel(root, "PlayerPanel", new Color(0f, 0f, 0f, 0.35f));
            UIFactory.Place(panel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(560f, 170f));

            TextMeshProUGUI name = UIFactory.Text(panel.transform, "HEALING KNIGHT", 30f, UIFactory.TextColor, TextAlignmentOptions.Left);
            UIFactory.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -10f), new Vector2(520f, 40f));

            healthBar = UIFactory.Bar(panel.transform, "Health", new Color(0.9f, 0.2f, 0.3f), new Vector2(520f, 34f));
            UIFactory.Place((RectTransform)healthBar.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -55f), new Vector2(520f, 34f));

            shieldBar = UIFactory.Bar(panel.transform, "Shield", UIFactory.AccentColor, new Vector2(360f, 18f));
            UIFactory.Place((RectTransform)shieldBar.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -95f), new Vector2(360f, 18f));

            // Bombas de medicamento (como el contador de granadas de Metal Slug)
            bombsText = UIFactory.Text(panel.transform, "BOMBAS 0", 24f, new Color(1f, 0.85f, 0.4f), TextAlignmentOptions.Right);
            UIFactory.Place(bombsText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(390f, -86f), new Vector2(150f, 34f));
            // Barra de la siguiente bomba: se llena pegando con el medicamento correcto y con parry.
            bombChargeBar = UIFactory.Bar(panel.transform, "BombCharge", new Color(1f, 0.85f, 0.4f), new Vector2(150f, 10f));
            UIFactory.Place((RectTransform)bombChargeBar.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(390f, -122f), new Vector2(150f, 10f));

            medicationSwatch = UIFactory.Panel(panel.transform, "MedicationSwatch", Color.white);
            UIFactory.Place(medicationSwatch.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -122f), new Vector2(34f, 34f));

            medicationText = UIFactory.Text(panel.transform, "", 28f, UIFactory.TextColor, TextAlignmentOptions.Left);
            UIFactory.Place(medicationText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(66f, -120f), new Vector2(470f, 40f));

            // Salud del cerebro (solo en niveles con BrainHealth, GDD 5.14)
            Image brainPanel = UIFactory.Panel(root, "BrainPanel", new Color(0.1f, 0f, 0.15f, 0.45f));
            UIFactory.Place(brainPanel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -210f), new Vector2(560f, 70f));
            TextMeshProUGUI brainLabel = UIFactory.Text(brainPanel.transform, "SALUD DEL CEREBRO", 22f, new Color(0.85f, 0.7f, 1f), TextAlignmentOptions.Left);
            UIFactory.Place(brainLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -4f), new Vector2(520f, 28f));
            brainBar = UIFactory.Bar(brainPanel.transform, "Brain", new Color(0.7f, 0.4f, 1f), new Vector2(520f, 24f));
            UIFactory.Place((RectTransform)brainBar.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -36f), new Vector2(520f, 24f));
            brainRoot = brainPanel.gameObject;
            brainRoot.SetActive(false);

            // Nombre del nivel (arriba al centro)
            levelText = UIFactory.Text(root, "", 30f, new Color(1f, 1f, 1f, 0.85f));
            UIFactory.Place(levelText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(900f, 50f));

            // Indicaciones: el globo de NODO (arriba, con su cara y el color del sistema).
            nodo = NodoBubble.Create(root, theme);
            // "¡AVANZA! →"
            goText = UIFactory.Text(root, "¡AVANZA!  →", 64f, new Color(1f, 0.85f, 0.2f), TextAlignmentOptions.Right);
            UIFactory.Place(goText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-60f, 120f), new Vector2(700f, 100f));
            goText.gameObject.SetActive(false);

            // Barra del jefe (abajo al centro)
            Image bossPanel = UIFactory.Panel(root, "BossPanel", new Color(0f, 0f, 0f, 0.45f));
            UIFactory.Place(bossPanel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(1100f, 90f));
            bossRoot = bossPanel.gameObject;
            bossNameText = UIFactory.Text(bossPanel.transform, "", 30f, new Color(1f, 0.8f, 0.6f));
            UIFactory.Place(bossNameText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(1000f, 36f));
            bossBar = UIFactory.Bar(bossPanel.transform, "BossHealth", new Color(0.95f, 0.45f, 0.2f), new Vector2(1060f, 30f));
            UIFactory.Place((RectTransform)bossBar.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(1060f, 30f));
            bossRoot.SetActive(false);

            // Controles (abajo, desaparece solo)
            controlsText = UIFactory.Text(root,
                PlayerControls.HelpLine(), 24f, new Color(1f, 1f, 1f, 0.7f));
            controlsText.enableAutoSizing = true;
            controlsText.fontSizeMin = 14f;
            controlsText.fontSizeMax = 24f;
            UIFactory.Place(controlsText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 25f), new Vector2(1800f, 40f));
        }

        private void Bind(PlayerController p)
        {
            Unbind();
            player = p;
            if (player == null) return;

            player.Health.HealthChanged += OnHealthChanged;
            if (player.Shield != null) player.Shield.EnergyChanged += OnShieldChanged;
            if (player.Shooter != null) player.Shooter.MedicationChanged += OnMedicationChanged;
            if (player.Bombs != null) player.Bombs.CountChanged += OnBombsChanged;
            if (player.Bombs != null) player.Bombs.ChargeChanged += OnBombChargeChanged;

            healthBar.SetValue(player.Health.Health01, true);
            if (player.Shield != null) shieldBar.SetValue(player.Shield.Energy01, true);
            if (player.Shooter != null) OnMedicationChanged(player.Shooter.Current);
            if (player.Bombs != null) OnBombsChanged(player.Bombs.Count);
            if (player.Bombs != null) bombChargeBar.SetValue(player.Bombs.Charge01, true);
        }

        private void Unbind()
        {
            if (player == null) return;
            player.Health.HealthChanged -= OnHealthChanged;
            if (player.Shield != null) player.Shield.EnergyChanged -= OnShieldChanged;
            if (player.Shooter != null) player.Shooter.MedicationChanged -= OnMedicationChanged;
            if (player.Bombs != null) player.Bombs.CountChanged -= OnBombsChanged;
            if (player.Bombs != null) player.Bombs.ChargeChanged -= OnBombChargeChanged;
            player = null;
        }

        private void OnHealthChanged(float current, float max) => healthBar.SetValue(max > 0f ? current / max : 0f);

        private void OnShieldChanged(float value01) => shieldBar.SetValue(value01);

        private void OnBrainChanged(float current, float max) => brainBar.SetValue(max > 0f ? current / max : 0f);

        private void OnBombsChanged(int count) => bombsText.text = $"BOMBAS {count}";

        private void OnBombChargeChanged(float value01) => bombChargeBar.SetValue(value01);

        private void OnMedicationChanged(MedicationData med)
        {
            if (med == null)
            {
                medicationText.text = "Sin medicamentos (revisa PlayerConfig)";
                medicationSwatch.color = Color.gray;
                return;
            }

            int index = player != null && player.Shooter != null ? player.Shooter.SelectedIndex + 1 : 0;
            medicationText.text = $"[{index}] {med.displayName}{(med.isRegional ? "  (regional)" : "")}";
            medicationSwatch.color = med.color;
        }

        // Si cambias de teclado a control (o cambias una tecla en el menú), la ayuda dice los botones nuevos.
        private void RefreshControlsHint()
        {
            if (controlsText != null) controlsText.text = PlayerControls.HelpLine();
        }

        private void ShowMessage(string text)
        {
            nodo.Show(PlayerControls.Format(text));   // {saltar}, {disparar}... = tu tecla o botón
        }

        private void OnBossStarted(string bossName, EnemyHealth health)
        {
            boss = health;
            bossNameText.text = bossName;
            bossBar.SetValue(1f, true);
            bossRoot.SetActive(true);
        }

        private void OnBossEnded()
        {
            boss = null;
            bossRoot.SetActive(false);
        }

        private void OnArenaStarted(ArenaZone arena)
        {
            goText.gameObject.SetActive(false);
        }

        private void OnArenaCleared(ArenaZone arena)
        {
            goText.gameObject.SetActive(true);
            goTimer = 3f;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (boss != null) bossBar.SetValue(boss.Health01);

            if (goTimer > 0f)
            {
                goTimer -= dt;
                float blink = Mathf.PingPong(Time.unscaledTime * 3f, 1f);
                goText.alpha = 0.4f + 0.6f * blink;
                if (goTimer <= 0f) goText.gameObject.SetActive(false);
            }

            if (controlsTimer > 0f)
            {
                controlsTimer -= dt;
                if (controlsTimer < 1f) controlsText.alpha = Mathf.Clamp01(controlsTimer);
            }
        }
    }
}
