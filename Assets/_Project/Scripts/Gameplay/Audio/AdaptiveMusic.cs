using System;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Audio
{
    /// <summary>
    /// Música ADAPTATIVA: cada nivel tiene su canción separada en 6 capas que suenan a la vez, sincronizadas al
    /// milisegundo, y el juego sube o baja cada capa según lo que pasa:
    ///
    ///   Intensidad 0   (explorando)         → Ambiente + Armonía + Melodía suave
    ///   ~0.35          (enemigos cerca)      → entra el Bajo
    ///   ~0.7           (pelea en una arena)  → entra la Percusión y la Melodía completa
    ///   1              (muchos enemigos/jefe) → entra la capa de Combate
    ///
    ///   - Al limpiar una arena suena el remate "Victoria" justo en el siguiente tiempo del compás.
    ///   - Con poca vida todo se escucha "bajo el agua" (filtro) y suena un latido de corazón.
    ///   - Con un jefe cambia a la canción "Sistema_Jefe" justo al empezar un compás, con una subida ("Entrada").
    ///
    /// Archivos: Assets/_Project/Audio/Resources/MusicAdaptive/&lt;NombreDeLaEscena&gt;/ (Ambiente, Armonia, Melodia,
    /// Bajo, Percusion, Combate, Victoria, manifest.json). Los genera Tools/Audio/generar_musica_adaptativa.py.
    /// Si un nivel no tiene carpeta, Music usa la pista normal de siempre.
    /// </summary>
    public static class AdaptiveMusic
    {
        private const string Folder = "MusicAdaptive";
        private static readonly string[] Layers = { "Ambiente", "Armonia", "Melodia", "Bajo", "Percusion", "Combate" };

        [Serializable]
        private class Manifest
        {
            public string level;
            public float bpm;
            public int bars;
            public int sampleRate;
            public int samples;
            public string[] layers;
        }

        /// <summary>Una canción: sus 6 capas, cuándo empezó (en el reloj de audio) y su volumen general.</summary>
        private class Song
        {
            public string key;
            public double bar;              // duración de un compás (segundos)
            public double startDsp;         // cuándo empezó a sonar (AudioSettings.dspTime)
            public double fadeOutAt = double.MaxValue;
            public float gain;              // 0..1, sube al empezar y baja al reemplazarla
            public AudioSource[] sources;
            public AudioLowPassFilter[] filters;
            public AudioClip victory, entrance;
            public GameObject root;
        }

        private static GameObject root;
        private static Song levelSong, bossSong, current;
        private static AudioSource stinger, heart;
        private static float intensity, holdUntil, danger;
        private static bool bossOn;

        /// <summary>Intensidad actual (0 calma … 1 pelea fuerte). Útil para depurar.</summary>
        public static float Intensity => intensity;
        public static bool IsActive => levelSong != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            root = null;
            levelSong = bossSong = current = null;
            stinger = heart = null;
            intensity = holdUntil = danger = 0f;
            bossOn = false;
        }

        // ------------------------------------------------------------------ lo que llama Music

        /// <summary>Al cargar una escena. Devuelve true si ese nivel tiene música adaptativa.</summary>
        public static bool TryPlayLevel(string sceneName)
        {
            // Misma escena (por ejemplo, reaparecer tras morir): la música sigue; si estaba el jefe, vuelve la normal.
            if (levelSong != null && levelSong.key == sceneName)
            {
                if (current == bossSong && bossSong != null) EndBoss();
                return true;
            }

            Song song = LoadSong(sceneName);
            Stop();
            if (song == null) return false;

            EnsureRoot();
            levelSong = song;
            Start(song, AudioSettings.dspTime + 0.15);
            current = song;
            return true;
        }

        /// <summary>Empezó un jefe: canción de jefe del sistema, justo al empezar un compás. True si se encargó.</summary>
        public static bool TryBoss()
        {
            if (levelSong == null) return false;
            bossOn = true;
            if (current == bossSong && bossSong != null) return true;

            int cut = levelSong.key.IndexOf('_');
            string system = cut > 0 ? levelSong.key.Substring(0, cut) : levelSong.key;
            if (bossSong == null) bossSong = LoadSong(system + "_Jefe");
            if (bossSong == null) return true; // sin canción de jefe: la del nivel sube a intensidad máxima

            // La subida dura 1 compás y su golpe cae justo cuando empieza la canción del jefe.
            double start = NextBar(current) + current.bar;
            if (bossSong.entrance != null)
            {
                stinger.clip = bossSong.entrance;
                stinger.PlayScheduled(start - current.bar);
            }
            SwitchTo(bossSong, start);
            return true;
        }

        public static void EndBoss()
        {
            bossOn = false;
            if (levelSong == null || current == levelSong) return;
            SwitchTo(levelSong, NextBar(current));
        }

        public static void Stop()
        {
            foreach (Song s in new[] { levelSong, bossSong })
                if (s != null && s.root != null) UnityEngine.Object.Destroy(s.root);
            levelSong = bossSong = current = null;
            bossOn = false;
            if (heart != null) heart.volume = 0f;
        }

        // ------------------------------------------------------------------ cargar y tocar

        private static Song LoadSong(string key)
        {
            var text = Resources.Load<TextAsset>($"{Folder}/{key}/manifest");
            if (text == null) return null;
            Manifest m = JsonUtility.FromJson<Manifest>(text.text);
            if (m == null || m.bpm <= 0f) return null;

            EnsureRoot();
            var song = new Song { key = key, bar = 60.0 / m.bpm * 4.0, sources = new AudioSource[Layers.Length], filters = new AudioLowPassFilter[Layers.Length] };
            song.root = new GameObject("Cancion_" + key);
            song.root.transform.SetParent(root.transform, false);
            for (int i = 0; i < Layers.Length; i++)
            {
                AudioClip clip = Resources.Load<AudioClip>($"{Folder}/{key}/{Layers[i]}");
                var go = new GameObject(Layers[i]);
                go.transform.SetParent(song.root.transform, false);
                AudioSource s = go.AddComponent<AudioSource>();
                s.clip = clip;
                s.loop = true;
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                s.priority = 0;
                s.volume = 0f;
                song.sources[i] = s;
                song.filters[i] = go.AddComponent<AudioLowPassFilter>();
                song.filters[i].cutoffFrequency = 22000f;
            }
            song.victory = Resources.Load<AudioClip>($"{Folder}/{key}/Victoria");
            song.entrance = Resources.Load<AudioClip>($"{Folder}/{key}/Entrada");
            return song;
        }

        /// <summary>Todas las capas empiezan en el MISMO instante del reloj de audio (así nunca se desfasan).</summary>
        private static void Start(Song song, double dspTime)
        {
            song.startDsp = dspTime;
            song.fadeOutAt = double.MaxValue;
            foreach (AudioSource s in song.sources)
            {
                if (s.clip == null) continue;
                s.Stop();
                s.timeSamples = 0;
                s.PlayScheduled(dspTime);
            }
        }

        private static void SwitchTo(Song next, double dspTime)
        {
            Song previous = current;
            Start(next, dspTime);
            next.gain = 0f;
            if (previous != null) previous.fadeOutAt = dspTime;
            current = next;
        }

        /// <summary>Cuándo empieza el siguiente compás de esa canción (en el reloj de audio).</summary>
        private static double NextBar(Song song)
        {
            double now = AudioSettings.dspTime;
            if (song == null) return now + 0.1;
            double t = Math.Max(0.0, now - song.startDsp);
            return song.startDsp + Math.Ceiling(t / song.bar + 1e-6) * song.bar;
        }

        private static double NextBeat(Song song)
        {
            double now = AudioSettings.dspTime;
            if (song == null) return now + 0.05;
            double beat = song.bar / 4.0;
            double t = Math.Max(0.0, now - song.startDsp);
            return song.startDsp + Math.Ceiling(t / beat + 1e-6) * beat;
        }

        private static void EnsureRoot()
        {
            if (root != null) return;
            root = new GameObject("MusicaAdaptativa");
            UnityEngine.Object.DontDestroyOnLoad(root);
            stinger = MakeSource("Remate", false);
            heart = MakeSource("Latido", true);
            heart.clip = Resources.Load<AudioClip>($"{Folder}/Latido");
            if (heart.clip != null) heart.Play();
            root.AddComponent<AdaptiveMusicDriver>();

            GameplayEvents.ArenaCleared -= OnArenaCleared;
            GameplayEvents.ArenaCleared += OnArenaCleared;
        }

        private static AudioSource MakeSource(string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            AudioSource s = go.AddComponent<AudioSource>();
            s.loop = loop;
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.priority = 0;
            s.volume = 0f;
            return s;
        }

        private static void OnArenaCleared(ArenaZone arena)
        {
            if (current == null || current.victory == null || stinger == null) return;
            stinger.clip = current.victory;
            stinger.volume = CombatAudio.MusicLevel;
            stinger.PlayScheduled(NextBeat(current));
            holdUntil = Time.time + 1f; // después del remate se calma pronto
        }

        // ------------------------------------------------------------------ cada cuadro

        /// <summary>Lo llama AdaptiveMusicDriver: calcula la intensidad y mueve los volúmenes.</summary>
        internal static void Tick()
        {
            if (levelSong == null) return;
            float dt = Time.unscaledDeltaTime;
            UpdateIntensity(dt);
            UpdateDanger(dt);

            float master = CombatAudio.MusicLevel;
            double now = AudioSettings.dspTime;
            float cutoff = Mathf.Lerp(22000f, 900f, danger);

            foreach (Song song in new[] { levelSong, bossSong })
            {
                if (song == null) continue;
                bool fading = now >= song.fadeOutAt;
                bool started = now >= song.startDsp;
                // La actual sube en cuanto empieza; la anterior sigue sonando completa hasta el compás del cambio.
                bool waitingToLeave = song != current && song.fadeOutAt < double.MaxValue && !fading;
                float goal = (song == current && started) || waitingToLeave ? 1f : 0f;
                song.gain = Mathf.MoveTowards(song.gain, goal, dt / (fading ? 0.5f : 0.3f));
                if (fading && song.gain <= 0f && song != current)
                {
                    foreach (AudioSource s in song.sources) s.Stop();
                    song.fadeOutAt = double.MaxValue;
                }
                for (int i = 0; i < song.sources.Length; i++)
                {
                    song.sources[i].volume = LayerWeight(i, intensity) * song.gain * master;
                    song.filters[i].cutoffFrequency = cutoff;
                }
            }

            if (heart != null) heart.volume = Mathf.Lerp(heart.volume, danger * 0.8f * master, dt * 4f);
            if (stinger != null) stinger.volume = master;
        }

        private static void UpdateIntensity(float dt)
        {
            PlayerController player = PlayerController.Current;
            int near = 0;
            if (player != null)
            {
                Vector2 p = player.Position;
                foreach (EnemyHealth e in EnemyHealth.Active)
                {
                    if (e != null && e.IsAlive && ((Vector2)e.transform.position - p).sqrMagnitude < 11f * 11f) near++;
                }
            }

            float target = 0f;
            if (near > 0) target = 0.35f + 0.1f * (near - 1);
            if (ArenaZone.AnyRunning) target = Mathf.Max(target, 0.7f + 0.06f * near);
            if (bossOn) target = 1f;
            target = Mathf.Clamp01(target);

            // Sube rápido (1.2 s), baja lento (4 s) y espera 3 s antes de bajar: así no "parpadea".
            if (target >= intensity)
            {
                intensity = Mathf.MoveTowards(intensity, target, dt / 1.2f);
                if (target > 0.05f) holdUntil = Mathf.Max(holdUntil, Time.time + 3f);
            }
            else if (Time.time > holdUntil)
            {
                intensity = Mathf.MoveTowards(intensity, target, dt / 4f);
            }
        }

        private static void UpdateDanger(float dt)
        {
            PlayerController player = PlayerController.Current;
            float goal = 0f;
            if (player != null && player.Health != null && player.Health.IsAlive && player.Health.Max > 0f)
            {
                float health = player.Health.Current / player.Health.Max;
                goal = 1f - Mathf.InverseLerp(0.15f, 0.35f, health); // 0 con más de 35 % de vida, 1 con 15 % o menos
            }
            danger = Mathf.MoveTowards(danger, goal, dt / 0.8f);
        }

        /// <summary>Qué tan fuerte suena cada capa según la intensidad (0..1).</summary>
        private static float LayerWeight(int layer, float x)
        {
            switch (layer)
            {
                case 0: return 1f - 0.3f * Ramp(0.6f, 1f, x);           // Ambiente
                case 1: return 0.7f + 0.3f * Ramp(0f, 0.3f, x);         // Armonía
                case 2: return 0.45f + 0.55f * Ramp(0.3f, 0.7f, x);     // Melodía
                case 3: return Ramp(0.15f, 0.45f, x);                    // Bajo
                case 4: return Ramp(0.3f, 0.6f, x);                      // Percusión
                default: return Ramp(0.65f, 0.95f, x);                   // Combate
            }
        }

        private static float Ramp(float from, float to, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, x));
    }

    /// <summary>Va en el objeto "MusicaAdaptativa" (lo crea AdaptiveMusic): lo actualiza cada cuadro.</summary>
    public class AdaptiveMusicDriver : MonoBehaviour
    {
        private void Update() => AdaptiveMusic.Tick();
    }
}
