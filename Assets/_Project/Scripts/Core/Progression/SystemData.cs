using System.Collections.Generic;
using HealingKnight.Core.Combat;
using UnityEngine;

namespace HealingKnight.Core.Progression
{
    /// <summary>
    /// Un sistema del cuerpo (un "mundo" del mapa): Digestivo, Respiratorio, Nervioso, Circulatorio.
    /// Agregar un sistema nuevo = crear este asset, sus LevelData y sus escenas. No hay que tocar código.
    /// </summary>
    [CreateAssetMenu(menuName = "Healing Knight/Sistema del cuerpo", fileName = "Sistema")]
    public class SystemData : ScriptableObject
    {
        [Tooltip("Identificador único y estable (se usa en el guardado). No lo cambies después de publicar.")]
        public string id = "digestivo";

        public string displayName = "Sistema Digestivo";

        [Tooltip("Número romano que aparece en el mapa (I, II, III, IV).")]
        public string mapNumber = "I";

        [TextArea] public string description;

        [Tooltip("Cápsula que solo existe en este sistema (se agrega al ciclo de medicamentos).")]
        public MedicationData regionalMedication;

        [Header("Identidad visual y sonora")]
        public Color mapColor = new Color(0.9f, 0.45f, 0.55f);
        public Color backgroundTint = Color.white;
        public AudioClip music;

        [Header("Mapa del cuerpo")]
        [Tooltip("Posición del nodo en el mapa (0 a 1 sobre la ilustración del cuerpo).")]
        public Vector2 mapPosition = new Vector2(0.5f, 0.3f);

        [Header("Niveles en orden")]
        public List<LevelData> levels = new List<LevelData>();
    }
}
