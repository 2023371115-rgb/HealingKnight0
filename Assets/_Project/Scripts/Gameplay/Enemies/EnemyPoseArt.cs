using HealingKnight.Core.Damage;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>
    /// Cambia el dibujo de un enemigo según lo que está haciendo (quieto, caminando, atacando, lastimado, muerto).
    /// Los dibujos son PNG sueltos (uno por pose) en Art/Enemigos; los pone PrefabSetup.
    /// Si falta una pose usa la más parecida (por ejemplo, sin "lastimado" usa "normal").
    /// También se puede forzar una pose un rato con Hold (la colonia madre lo usa al soltar bacterias).
    /// </summary>
    public class EnemyPoseArt : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer target;
        [SerializeField] private Sprite idle;
        [SerializeField] private Sprite move;
        [SerializeField] private Sprite attack;
        [SerializeField] private Sprite hurt;
        [SerializeField] private Sprite dead;
        [Tooltip("Cuánto se ve la pose de 'lastimado' cuando le pegas.")]
        [SerializeField] private float hurtFlash = 0.25f;

        private EnemyBrain brain;
        private EnemyHealth health;
        private float hurtTimer, holdTimer;
        private EnemyBrain.Pose held;
        private bool isDead;

        private void Awake()
        {
            brain = GetComponent<EnemyBrain>();
            health = GetComponent<EnemyHealth>();
        }

        private void OnEnable()
        {
            isDead = false;
            hurtTimer = holdTimer = 0f;
            if (health != null)
            {
                health.Hit += OnHit;
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Hit -= OnHit;
                health.Died -= OnDied;
            }
        }

        private void OnHit(DamageInfo info, HitResult result) => hurtTimer = hurtFlash;

        private void OnDied(EnemyHealth _) => isDead = true;

        /// <summary>Muestra una pose durante unos segundos (ignora lo que esté haciendo).</summary>
        public void Hold(EnemyBrain.Pose pose, float seconds)
        {
            held = pose;
            holdTimer = seconds;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.deltaTime;
            if (hurtTimer > 0f) hurtTimer -= dt;
            if (holdTimer > 0f) holdTimer -= dt;

            EnemyBrain.Pose pose;
            if (isDead) pose = EnemyBrain.Pose.Dead;
            else if (holdTimer > 0f) pose = held;
            else if (hurtTimer > 0f) pose = EnemyBrain.Pose.Hurt;
            else pose = brain != null ? brain.CurrentPose : EnemyBrain.Pose.Idle;

            Sprite s = Pick(pose);
            if (s != null && target.sprite != s) target.sprite = s;
        }

        private Sprite Pick(EnemyBrain.Pose pose)
        {
            switch (pose)
            {
                case EnemyBrain.Pose.Dead: return Or(dead, Or(hurt, idle));
                case EnemyBrain.Pose.Hurt: return Or(hurt, idle);
                case EnemyBrain.Pose.Attack: return Or(attack, Or(move, idle));
                case EnemyBrain.Pose.Move: return Or(move, idle);
                default: return Or(idle, move);
            }
        }

        // (Los objetos de Unity no se llevan bien con '??': por eso se compara con != null.)
        private static Sprite Or(Sprite a, Sprite b) => a != null ? a : b;
    }
}
