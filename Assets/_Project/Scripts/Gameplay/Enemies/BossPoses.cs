using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>
    /// DIBUJO de un jefe con sus poses: Normal, Ataca, Enojado y Derrotado. Solo existe si hay PNG en
    /// Art/Jefes/&lt;Jefe&gt;/&lt;Jefe&gt;_&lt;Pose&gt;.png: lo agregan PrefabSetup / SceneSetup al regenerar (PrefabSetup.AddBossPoses).
    /// Si no hay dibujos, el jefe se arma con sus figuras de siempre.
    /// - Los jefes que dibujan sus figuras en código (Start) preguntan <see cref="HasArt"/> y, si hay, llaman
    ///   <see cref="Build"/> en lugar de armar las figuras.
    /// - Las poses que falten usan la más parecida (Ataca → Enojado → Normal).
    /// </summary>
    public class BossPoses : MonoBehaviour
    {
        public enum Pose { Normal, Attack, Angry, Defeated }

        [SerializeField] private Sprite normal, attack, angry, defeated;
        [SerializeField] private SpriteRenderer target;
        [SerializeField] private int sortingOrder = 10;

        private float attackUntil;
        private bool angryBase;

        public bool HasArt => normal != null;
        public SpriteRenderer Target => target;
        public Pose Current { get; private set; }

        public void Configure(Sprite normalPose, Sprite attackPose, Sprite angryPose, Sprite defeatedPose, SpriteRenderer renderer, int order)
        {
            normal = normalPose;
            attack = attackPose;
            angry = angryPose;
            defeated = defeatedPose;
            target = renderer;
            sortingOrder = order;
        }

        /// <summary>Crea el dibujo (si todavía no existe) como hijo de 'parent'. Devuelve su SpriteRenderer.</summary>
        public SpriteRenderer Build(Transform parent, Vector2 localPosition)
        {
            if (!HasArt) return null;
            if (target == null)
            {
                var go = new GameObject("DibujoJefe");
                go.transform.SetParent(parent, false);
                go.transform.localPosition = localPosition;
                target = go.AddComponent<SpriteRenderer>();
                target.sortingOrder = sortingOrder;
            }
            target.sprite = normal;
            return target;
        }

        /// <summary>Cambia de pose. Normal y Enojado son "de fondo"; Ataca dura 'attackSeconds' y regresa sola.</summary>
        public void Show(Pose pose, float attackSeconds = 0.6f)
        {
            if (pose == Pose.Attack) attackUntil = Time.time + attackSeconds;
            Current = pose;
            Apply();
        }

        private void Update()
        {
            if (Current == Pose.Attack && Time.time > attackUntil)
            {
                Current = angryBase ? Pose.Angry : Pose.Normal;
                Apply();
            }
        }

        /// <summary>La pose "de fondo" (a la que se regresa después de atacar): enojado desde la mitad de la pelea, por ejemplo.</summary>
        public void SetAngry(bool value)
        {
            angryBase = value;
            if (Current != Pose.Attack && Current != Pose.Defeated) { Current = value ? Pose.Angry : Pose.Normal; Apply(); }
        }

        private void Apply()
        {
            if (target == null || !HasArt) return;
            Sprite s;
            switch (Current)
            {
                case Pose.Attack: s = attack != null ? attack : angry; break;
                case Pose.Angry: s = angry; break;
                case Pose.Defeated: s = defeated != null ? defeated : angry; break;
                default: s = normal; break;
            }
            if (s == null) s = normal;
            if (target.sprite != s) target.sprite = s;
        }
    }
}
