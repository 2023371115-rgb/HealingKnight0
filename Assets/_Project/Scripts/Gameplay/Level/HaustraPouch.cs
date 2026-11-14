using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// HAUSTRA: las "bolsas" que forman la pared del colon. Se contraen de vez en cuando para empujar lo que llevan.
    /// En el juego es un abultamiento del piso en el que te puedes parar: cada cierto tiempo tiembla (AVISO)
    /// y se aprieta: si estás encima te lanza hacia arriba (para llegar alto, como al apéndice).
    /// El objeto está en el piso; la parte de arriba (donde se pisa) está a 'height'.
    /// </summary>
    public class HaustraPouch : MonoBehaviour
    {
        [SerializeField] private float width = 2.6f;
        [SerializeField] private float height = 0.9f;
        [SerializeField] private float interval = 2.8f;
        [SerializeField] private float offset;
        [SerializeField] private float warning = 0.6f;
        [SerializeField] private float launchSpeed = 18f;
        [SerializeField] private Sprite art;
        [SerializeField] private Color tissue = new Color(0.78f, 0.45f, 0.62f, 1f);
        [SerializeField] private Color shade = new Color(0.6f, 0.3f, 0.48f, 1f);
        [SerializeField] private Color outline = new Color(0.22f, 0.06f, 0.16f, 1f);

        private Transform visual;
        private float timer, squeeze;
        private bool warned;

        public void Configure(float pouchWidth, float period, float startOffset, Sprite drawing)
        {
            width = pouchWidth;
            interval = period;
            offset = startOffset;
            art = drawing;
        }

        private void Start()
        {
            timer = interval + offset;
            Build();
        }

        private void FixedUpdate()
        {
            timer -= Time.fixedDeltaTime;
            if (timer <= warning && !warned)
            {
                warned = true;
                Sfx.EffectAt("Lombriz_Retumbo", transform.position, 0.35f);
            }
            if (timer > 0f) return;
            timer = interval;
            warned = false;
            squeeze = 1f;

            PlayerController player = PlayerController.Current;
            if (player == null || player.Motor == null) return;
            Vector2 feet = player.Position;
            float top = transform.position.y + height;
            bool onTop = Mathf.Abs(feet.x - transform.position.x) <= width * 0.5f + 0.1f && Mathf.Abs(feet.y - top) < 0.3f;
            if (!onTop) return;
            player.Motor.SetVelocity(new Vector2(player.Motor.Velocity.x, launchSpeed));
            Sfx.EffectAt("Alveolo_Rebote", transform.position, 0.9f);
        }

        private void Update()
        {
            if (visual == null) return;
            float t = Time.time;
            float shake = timer < warning ? Mathf.Sin(t * 40f) * 0.04f : 0f;
            squeeze = Mathf.MoveTowards(squeeze, 0f, Time.deltaTime * 3f);
            float s = Mathf.Sin(squeeze * Mathf.PI) * 0.3f;
            float breathe = Mathf.Sin(t * 2f + transform.position.x) * 0.03f;
            visual.localScale = new Vector3(1f + s * 0.6f + breathe, 1f - s + breathe, 1f);
            visual.localPosition = new Vector3(shake, 0f, 0f);
        }

        private void Build()
        {
            gameObject.layer = PhysicsLayers.Ground;
            var box = gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(width * 0.85f, height);
            box.offset = new Vector2(0f, height * 0.5f);
            box.usedByEffector = true;
            var effector = gameObject.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 160f;

            visual = new GameObject("Bolsa").transform;
            visual.SetParent(transform, false);
            if (art != null)
            {
                SpriteRenderer r = CodeSprites.Art(visual, art, width * 1.1f, -6, Vector2.zero);
                r.transform.localPosition = new Vector3(0f, 0f, 0f);
                return;
            }
            Sprite c = CodeSprites.Circle;
            CodeSprites.Add(visual, c, outline, -8, new Vector2(0f, height * 0.35f), new Vector2(width + 0.14f, height * 1.4f + 0.14f));
            CodeSprites.Add(visual, c, tissue, -7, new Vector2(0f, height * 0.35f), new Vector2(width, height * 1.4f));
            CodeSprites.Add(visual, c, shade, -6, new Vector2(width * 0.15f, height * 0.15f), new Vector2(width * 0.6f, height * 0.7f));
            CodeSprites.Add(visual, c, new Color(1f, 0.85f, 0.95f, 0.8f), -5, new Vector2(-width * 0.2f, height * 0.75f), new Vector2(width * 0.3f, height * 0.18f));
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.5f, 0.8f, 0.8f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * height * 0.5f, new Vector3(width, height, 0f));
        }
    }
}
