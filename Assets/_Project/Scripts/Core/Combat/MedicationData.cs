using UnityEngine;

namespace HealingKnight.Core.Combat
{
    /// <summary>
    /// Un medicamento (tipo de cápsula). Cada asset es una cápsula distinta:
    /// Antibiótico, Antiviral, Antiinflamatorio, Antihistamínico y las regionales
    /// (Antiparasitario, Mucolítico...).
    /// </summary>
    [CreateAssetMenu(menuName = "Healing Knight/Medicamento", fileName = "Medicamento")]
    public class MedicationData : ScriptableObject
    {
        [Tooltip("Nombre que se muestra en el HUD.")]
        public string displayName = "Medicamento";

        [Tooltip("Color de la cápsula y del brazo-pistola.")]
        public Color color = Color.white;

        [Tooltip("Ícono para el HUD (opcional).")]
        public Sprite icon;

        [Tooltip("Sprite de la cápsula en vuelo (opcional; si está vacío se usa el del prefab).")]
        public Sprite capsuleSprite;

        [Tooltip("Marca las cápsulas que solo existen en un sistema (p. ej. Antiparasitario en el Digestivo).")]
        public bool isRegional;

        [Tooltip("Anticonvulsivo: no daña a las neuronas sanas (las cápsulas las atraviesan) y su bomba deja un campo neuroprotector.")]
        public bool calmsNeurons;

        [TextArea] public string description;

        [Header("Sonido")]
        [Tooltip("Al disparar esta cápsula (corto, menos de 0.2 s).")]
        public AudioClip fireSound;
        [Tooltip("Cuando la cápsula hace daño (efectiva o parcial). Si es ineficaz suena el rebote general.")]
        public AudioClip hitSound;
    }
}
