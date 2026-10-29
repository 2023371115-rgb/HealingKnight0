using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>
    /// Un virión de la cadena de rabia (forma de bala). Tiene su propia vida (EnemyHealth + Hurtbox).
    /// Cuando es la cabeza de su cadena se ven los ojos y brilla más.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class RabiesSegment : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [Tooltip("Partes que solo se ven en la cabeza (ojos, punta brillante).")]
        [SerializeField] private GameObject[] headParts;
        [SerializeField] private float contactRadius = 0.5f;

        private RabiesBoss boss;
        private EnemyHealth health;
        private Vector3 visualBaseScale = Vector3.one;

        public EnemyHealth Health => health != null ? health : health = GetComponent<EnemyHealth>();
        public float ContactRadius => contactRadius;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            if (visual != null) visualBaseScale = new Vector3(Mathf.Abs(visual.localScale.x), visual.localScale.y, visual.localScale.z);
        }

        private void OnEnable() => Health.Died += OnDied;
        private void OnDisable() => Health.Died -= OnDied;

        public void Init(RabiesBoss owner) => boss = owner;

        public void SetHead(bool isHead)
        {
            if (headParts == null) return;
            foreach (GameObject g in headParts)
                if (g != null) g.SetActive(isHead);
        }

        /// <summary>Mira hacia donde avanza; al cambiar de riel se inclina hacia abajo/arriba.</summary>
        public void SetFacing(int dir, bool changingRail)
        {
            if (visual == null || dir == 0) return;
            visual.localScale = new Vector3(visualBaseScale.x * dir, visualBaseScale.y, visualBaseScale.z);
            float z = changingRail ? -60f * dir : 0f;
            visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(visual.localEulerAngles.z, z, 10f * Time.deltaTime));
        }

        private void OnDied(EnemyHealth _)
        {
            if (boss != null) boss.OnSegmentDied(this);
        }
    }
}
