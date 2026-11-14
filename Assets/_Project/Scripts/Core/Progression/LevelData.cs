using UnityEngine;

namespace HealingKnight.Core.Progression
{
    /// <summary>Un nivel dentro de un sistema (un órgano o zona: Boca y esófago, Estómago, Intestino...).</summary>
    [CreateAssetMenu(menuName = "Healing Knight/Nivel", fileName = "Nivel")]
    public class LevelData : ScriptableObject
    {
        [Tooltip("Identificador único y estable (se usa en el guardado). No lo cambies después de publicar.")]
        public string id = "digestivo_boca";

        public string displayName = "Boca y esófago";

        [Tooltip("Nombre exacto de la escena (debe estar en Build Settings). Vacío = 'Próximamente'.")]
        public string sceneName;

        [TextArea] public string objective;

        [Tooltip("Si al completarlo se obtiene una fórmula (normalmente el último nivel del sistema).")]
        public bool grantsFormula;

        [Header("Nivel oculto")]
        [Tooltip("No aparece en el mapa hasta encontrar su secreto.")]
        public bool hidden;
        [Tooltip("Id del secreto que lo desbloquea (lo registra un SecretPassage en algún nivel).")]
        public string secretId;

        public bool HasScene => !string.IsNullOrEmpty(sceneName);
    }
}
