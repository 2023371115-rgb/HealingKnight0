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

        [Header("Dibujo (opcional): si hay arte, cambia de pose")]
        [SerializeField] private SpriteRenderer art;
        [SerializeField] private bool artFacesLeft = true;
        [SerializeField] private Sprite headIdle, headAttack, headScared, headDead;
        [SerializeField] private Sprite bodySprite, bodyHurt, tailSprite;

        private bool isHead, isTail;
        private float attackTimer, scaredTimer, hurtTimer;

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

        private void OnEnable() { Health.Died += OnDied; Health.Hit += OnHit; }
        private void OnDisable() { Health.Died -= OnDied; Health.Hit -= OnHit; }

        private void OnHit(HealingKnight.Core.Damage.DamageInfo info, HealingKnight.Core.Damage.HitResult result) => hurtTimer = 0.3f;

        /// <summary>Abre la boca un momento (dispara o muerde).</summary>
        public void Attack(float seconds) => attackTimer = Mathf.Max(attackTimer, seconds);

        /// <summary>Pánico al agua (hidrofobia).</summary>
        public void Scare(float seconds) => scaredTimer = Mathf.Max(scaredTimer, seconds);

        public void SetTail(bool tail) => isTail = tail;

        private void Update()
        {
            if (art == null) return;
            float dt = Time.deltaTime;
            attackTimer -= dt; scaredTimer -= dt; hurtTimer -= dt;
            Sprite s;
            if (isHead)
            {
                if (!Health.IsAlive) s = headDead;
                else if (scaredTimer > 0f) s = headScared;
                else if (attackTimer > 0f) s = headAttack;
                else s = headIdle;
            }
            else if (isTail && hurtTimer <= 0f) s = tailSprite;
            else s = hurtTimer > 0f || (Health.IsAlive && Health.Health01 < 0.5f) ? bodyHurt : bodySprite;
            if (s == null) s = bodySprite;
            if (s != null && art.sprite != s) art.sprite = s;
        }

        public void Init(RabiesBoss owner) => boss = owner;

        public void SetHead(bool head)
        {
            isHead = head;
            if (headParts == null) return;
            foreach (GameObject g in headParts)
                if (g != null) g.SetActive(head && art == null);
        }

        /// <summary>Mira hacia donde avanza; al cambiar de riel se inclina hacia abajo/arriba.</summary>
        public void SetFacing(int dir, bool changingRail)
        {
            if (visual == null || dir == 0) return;
            int look = art != null && artFacesLeft ? -dir : dir;
            visual.localScale = new Vector3(visualBaseScale.x * look, visualBaseScale.y, visualBaseScale.z);
            float z = changingRail ? -60f * dir : 0f;
            visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(visual.localEulerAngles.z, z, 10f * Time.deltaTime));
        }

        private void OnDied(EnemyHealth _)
        {
            if (boss != null) boss.OnSegmentDied(this);
        }
    }
}
