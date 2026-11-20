using UnityEngine;

namespace HealingKnight.Gameplay.Audio
{
    /// <summary>
    /// Sonidos generales del combate (los de cada medicamento van en su MedicationData).
    /// Vive en Data/Resources/CombatAudio para cargarse solo.
    /// </summary>
    [CreateAssetMenu(menuName = "Healing Knight/Sonidos de combate", fileName = "CombatAudio")]
    public class CombatAudio : ScriptableObject
    {
        public const string ResourcePath = "CombatAudio";

        [Tooltip("Cápsula ineficaz que rebota en gris.")]
        public AudioClip resistSound;

        [Header("Volumen")]
        [Range(0f, 1f)] public float fireVolume = 0.45f;
        [Range(0f, 1f)] public float hitVolume = 0.6f;
        [Range(0f, 1f)] public float resistVolume = 0.55f;
        [Tooltip("Efectos del nivel: ácido, rayos, alvéolos, neuronas, jefes, salto, escudo, bomba...")]
        [Range(0f, 1f)] public float effectsVolume = 0.7f;
        [Tooltip("Disparos y muertes de los enemigos (son muchos: mejor más bajito).")]
        [Range(0f, 1f)] public float enemyVolume = 0.4f;
        [Tooltip("Sonido de fondo de cada sistema (Ambiente_Digestivo, _Respiratorio, _Nervioso).")]
        [Range(0f, 1f)] public float ambienceVolume = 0.3f;
        [Tooltip("Música de cada nivel (Audio/Resources/Music).")]
        [Range(0f, 1f)] public float musicVolume = 0.45f;
        [Tooltip("Variación de tono al azar para que no suene repetitivo (0.06 = ±6 %).")]
        [Range(0f, 0.3f)] public float pitchJitter = 0.06f;

        private static CombatAudio cached;
        private static bool loaded;

        public static CombatAudio Instance
        {
            get
            {
                if (!loaded)
                {
                    cached = Resources.Load<CombatAudio>(ResourcePath);
                    loaded = true;
                }
                return cached;
            }
        }

        public static float Effects => Instance != null ? Instance.effectsVolume : 0.7f;
        public static float Enemies => Instance != null ? Instance.enemyVolume : 0.4f;
        public static float MusicLevel => Instance != null ? Instance.musicVolume : 0.45f;
        public static float Ambience => Instance != null ? Instance.ambienceVolume : 0.3f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            cached = null;
            loaded = false;
        }
    }
}
