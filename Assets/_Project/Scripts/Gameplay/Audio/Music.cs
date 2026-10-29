using HealingKnight.Gameplay.Level;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HealingKnight.Gameplay.Audio
{
    /// <summary>
    /// Música de cada nivel, elegida por el nombre de la escena (igual que el ambiente):
    ///   "Digestivo_Estomago" → Musica_Digestivo_Estomago si existe, si no Musica_Digestivo.
    /// Al empezar un jefe cambia (con fundido) a la misma con "_Jefe" (Musica_Digestivo_Jefe) y al terminar regresa.
    /// Los archivos van en Assets/_Project/Audio/Resources/Music. Si no hay archivo, no suena nada.
    /// Al morir y recargar la escena, la música sigue sin reiniciarse.
    /// </summary>
    public static class Music
    {
        private const string Folder = "Music";
        private const float FadeSeconds = 1.2f;

        private static AudioSource a, b;      // dos fuentes para mezclar una con otra
        private static AudioSource current;
        private static AudioClip levelTrack;
        private static string baseName;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            a = b = current = null;
            levelTrack = null;
            baseName = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            GameplayEvents.BossStarted -= OnBossStarted;
            GameplayEvents.BossStarted += OnBossStarted;
            GameplayEvents.BossEnded -= OnBossEnded;
            GameplayEvents.BossEnded += OnBossEnded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static AudioClip Load(string name) =>
            string.IsNullOrEmpty(name) ? null : Resources.Load<AudioClip>($"{Folder}/{name}");

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive) return;
            string name = scene.name ?? "";
            int cut = name.IndexOf('_');
            string system = cut > 0 ? name.Substring(0, cut) : name;

            AudioClip specific = Load("Musica_" + name);
            baseName = specific != null ? "Musica_" + name : "Musica_" + system;
            levelTrack = specific != null ? specific : Load(baseName);
            PlayTrack(levelTrack);
        }

        private static void OnBossStarted(string bossName, Enemies.EnemyHealth health)
        {
            AudioClip boss = Load(baseName + "_Jefe");
            if (boss == null && baseName != null)
            {
                // "Musica_Respiratorio_Pulmones_Jefe" no existe → prueba "Musica_Respiratorio_Jefe".
                int cut = baseName.IndexOf('_', "Musica_".Length);
                if (cut > 0) boss = Load(baseName.Substring(0, cut) + "_Jefe");
            }
            if (boss != null) PlayTrack(boss);
        }

        private static void OnBossEnded() => PlayTrack(levelTrack);

        private static void PlayTrack(AudioClip clip)
        {
            EnsureSources();
            if (clip != null && current != null && current.clip == clip && current.isPlaying) return; // ya suena

            AudioSource next = current == a ? b : a;
            if (clip != null)
            {
                next.clip = clip;
                next.volume = 0f;
                next.Play();
            }
            else next.Stop();
            current = clip != null ? next : null;
        }

        private static void EnsureSources()
        {
            if (a != null) return;
            var go = new GameObject("Musica");
            Object.DontDestroyOnLoad(go);
            a = go.AddComponent<AudioSource>();
            b = go.AddComponent<AudioSource>();
            foreach (AudioSource s in new[] { a, b })
            {
                s.loop = true;
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                s.priority = 0; // la música nunca se corta por falta de voces
            }
            go.AddComponent<MusicFader>();
        }

        /// <summary>Lo llama MusicFader cada cuadro: sube la pista actual y baja la otra.</summary>
        internal static void Tick()
        {
            if (a == null) return;
            float step = Time.unscaledDeltaTime / FadeSeconds;
            float target = CombatAudio.MusicLevel;
            foreach (AudioSource s in new[] { a, b })
            {
                float goal = s == current ? target : 0f;
                s.volume = Mathf.MoveTowards(s.volume, goal, step * Mathf.Max(target, 0.1f));
                if (s != current && s.volume <= 0f && s.isPlaying) s.Stop();
            }
        }
    }
}
