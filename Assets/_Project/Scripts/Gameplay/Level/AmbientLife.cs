using System.Collections.Generic;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// Vida de adorno en el fondo de cada nivel (no hace daño ni estorba). Va en el objeto "Fondo" (LevelBackdrop)
    /// y dibuja, detrás del juego y delante de la pintura:
    ///  - Partículas según el órgano: polvo/aire, burbujas que suben, gotas que caen, chispas eléctricas, moscas volantes.
    ///  - Un "latido" del fondo: se infla y desinfla un poco (respirar, peristalsis, pulso).
    ///  - En los Pulmones, racimos de alvéolos de fondo que se inflan al inhalar y se desinflan al exhalar.
    /// </summary>
    [DefaultExecutionOrder(1002)]
    public class AmbientLife : MonoBehaviour
    {
        public enum Theme { Air, Lungs, Acid, Gut, Colon, Mouth, Nerve, Memory, Throat, Eye }

        [SerializeField] private Theme theme = Theme.Air;
        [SerializeField] private int count = 30;

        private enum Kind { Mote, Bubble, Drip, Spark, Floater }

        private class Bit
        {
            public Transform t;
            public SpriteRenderer r;
            public Vector2 pos, vel;
            public float size, life, maxLife, seed, alpha;
            public Kind kind;
        }

        private readonly List<Bit> bits = new List<Bit>();
        private readonly List<Transform> alveoli = new List<Transform>();
        private Transform layer;
        private SpriteRenderer backdropRenderer;
        private Color backdropBase = Color.white;
        private float flash;

        public void Configure(Theme look, int amount) { theme = look; count = amount; }

        private void Start()
        {
            backdropRenderer = GetComponent<SpriteRenderer>();
            if (backdropRenderer != null) backdropBase = backdropRenderer.color;
        }

        private Camera Cam => Camera.main;

        private void Build(Camera cam)
        {
            layer = new GameObject("VidaDelFondo").transform;
            layer.SetParent(cam.transform, false);
            layer.localPosition = new Vector3(0f, 0f, 10f);
            for (int i = 0; i < count; i++) bits.Add(NewBit(cam, true));
            if (theme == Theme.Lungs) BuildAlveoli(cam);
        }

        private Kind KindFor()
        {
            switch (theme)
            {
                case Theme.Acid: case Theme.Memory: case Theme.Throat: return Random.value < 0.75f ? Kind.Bubble : Kind.Mote;
                case Theme.Mouth: return Random.value < 0.3f ? Kind.Drip : Random.value < 0.5f ? Kind.Bubble : Kind.Mote;
                case Theme.Nerve: return Random.value < 0.55f ? Kind.Spark : Kind.Mote;
                case Theme.Eye: return Random.value < 0.35f ? Kind.Floater : Kind.Mote;
                default: return Kind.Mote;
            }
        }

        private Color ColorFor(Kind k)
        {
            switch (theme)
            {
                case Theme.Air: return k == Kind.Mote ? new Color(1f, 1f, 0.85f, 1f) : Color.white;
                case Theme.Lungs: return new Color(0.85f, 0.95f, 1f, 1f);
                case Theme.Acid: return new Color(0.6f, 1f, 0.35f, 1f);
                case Theme.Gut: return new Color(1f, 0.9f, 0.55f, 1f);
                case Theme.Colon: return new Color(0.85f, 0.7f, 0.95f, 1f);
                case Theme.Mouth: return new Color(0.85f, 0.95f, 1f, 1f);
                case Theme.Nerve: return k == Kind.Spark ? new Color(1f, 0.95f, 0.5f, 1f) : new Color(0.75f, 0.6f, 1f, 1f);
                case Theme.Memory: return new Color(1f, 0.85f, 0.5f, 1f);
                case Theme.Throat: return new Color(1f, 0.92f, 0.95f, 1f);
                case Theme.Eye: return k == Kind.Floater ? new Color(0.45f, 0.55f, 0.75f, 1f) : new Color(0.7f, 0.85f, 1f, 1f);
            }
            return Color.white;
        }

        private Bit NewBit(Camera cam, bool anywhere)
        {
            var b = new Bit { kind = KindFor(), seed = Random.value * 100f };
            b.t = new GameObject("Particula").transform;
            b.t.SetParent(layer, false);
            b.r = b.t.gameObject.AddComponent<SpriteRenderer>();
            b.r.sprite = CodeSprites.Circle;
            b.r.sortingOrder = -280;
            Respawn(b, cam, anywhere);
            return b;
        }

        private void Respawn(Bit b, Camera cam, bool anywhere)
        {
            float h = cam.orthographicSize, w = h * cam.aspect;
            b.kind = KindFor();
            b.r.color = ColorFor(b.kind);
            b.life = 0f;
            switch (b.kind)
            {
                case Kind.Mote:
                    b.size = Random.Range(0.05f, 0.14f);
                    b.alpha = Random.Range(0.25f, 0.6f);
                    b.vel = theme == Theme.Air ? new Vector2(Random.Range(-0.9f, -0.3f), Random.Range(-0.15f, 0.15f))
                          : new Vector2(Random.Range(-0.25f, 0.25f), Random.Range(-0.1f, 0.25f));
                    b.maxLife = Random.Range(6f, 12f);
                    b.pos = anywhere ? new Vector2(Random.Range(-w, w), Random.Range(-h * 0.6f, h)) : new Vector2(theme == Theme.Air ? w + 0.5f : Random.Range(-w, w), Random.Range(-h * 0.6f, h));
                    break;
                case Kind.Bubble:
                    b.size = Random.Range(0.12f, 0.35f);
                    b.alpha = Random.Range(0.25f, 0.55f);
                    b.vel = new Vector2(Random.Range(-0.1f, 0.1f), Random.Range(0.3f, 0.8f));
                    b.maxLife = Random.Range(5f, 10f);
                    b.pos = new Vector2(Random.Range(-w, w), anywhere ? Random.Range(-h * 0.6f, h) : -h * 0.6f);
                    break;
                case Kind.Drip:
                    b.size = Random.Range(0.1f, 0.18f);
                    b.alpha = 0.6f;
                    b.vel = new Vector2(0f, -0.2f);
                    b.maxLife = Random.Range(3f, 5f);
                    b.pos = new Vector2(Random.Range(-w, w), h * Random.Range(0.6f, 0.9f));
                    break;
                case Kind.Spark:
                    b.size = Random.Range(0.06f, 0.1f);
                    b.alpha = 0.85f;
                    float a = Random.Range(0f, Mathf.PI * 2f);
                    b.vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.4f) * Random.Range(3f, 6f);
                    b.maxLife = Random.Range(0.5f, 1.1f);
                    b.pos = new Vector2(Random.Range(-w, w), Random.Range(-h * 0.4f, h * 0.9f));
                    break;
                case Kind.Floater:
                    b.size = Random.Range(0.35f, 0.7f);
                    b.alpha = Random.Range(0.12f, 0.25f);
                    b.vel = new Vector2(Random.Range(-0.15f, 0.15f), Random.Range(-0.1f, 0.05f));
                    b.maxLife = Random.Range(10f, 16f);
                    b.pos = new Vector2(Random.Range(-w, w), Random.Range(-h * 0.4f, h));
                    break;
            }
            if (!anywhere && b.kind == Kind.Spark) { }
            if (anywhere) b.life = Random.Range(0f, b.maxLife * 0.8f);
        }

        private void BuildAlveoli(Camera cam)
        {
            float h = cam.orthographicSize, w = h * cam.aspect;
            // Racimos de uvas (alvéolos) en el fondo, translúcidos.
            for (int c = 0; c < 6; c++)
            {
                Transform cluster = new GameObject("RacimoAlveolos").transform;
                cluster.SetParent(layer, false);
                cluster.localPosition = new Vector3(-w + (c + 0.5f) * 2f * w / 6f + Random.Range(-0.6f, 0.6f), Random.Range(-h * 0.1f, h * 0.7f), 0f);
                float s = Random.Range(0.7f, 1.2f);
                for (int i = 0; i < 7; i++)
                {
                    float ang = i * Mathf.PI * 2f / 6f;
                    Vector2 p = i == 6 ? Vector2.zero : new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 0.42f * s;
                    CodeSprites.Add(cluster, CodeSprites.Circle, new Color(0.95f, 0.55f, 0.65f, 0.22f), -285, p, Vector2.one * 0.55f * s);
                    CodeSprites.Add(cluster, CodeSprites.Circle, new Color(1f, 0.85f, 0.9f, 0.18f), -284, p + new Vector2(-0.08f, 0.08f) * s, Vector2.one * 0.2f * s);
                }
                CodeSprites.Add(cluster, CodeSprites.Square, new Color(0.9f, 0.5f, 0.6f, 0.2f), -286, new Vector2(0f, -0.6f * s), new Vector2(0.1f, 0.8f * s));
                alveoli.Add(cluster);
            }
        }

        /// <summary>0..1: qué tan "lleno" está ahora (respiración, latido o peristalsis según el tema).</summary>
        private float Pulse()
        {
            float t = Time.timeSinceLevelLoad;
            switch (theme)
            {
                case Theme.Lungs:
                {
                    // Sincronizado con BreathingCycle: inhala 3.5 s, exhala 2 s.
                    float ph = Mathf.Repeat(t, BreathingCycle.Period);
                    return ph < BreathingCycle.InhaleTime ? Mathf.SmoothStep(0f, 1f, ph / 0.8f) : 1f - Mathf.SmoothStep(0f, 1f, (ph - BreathingCycle.InhaleTime) / 1.2f);
                }
                case Theme.Air: return 0.5f + 0.5f * Mathf.Sin(t / 5.5f * Mathf.PI * 2f);   // respirar tranquilo
                case Theme.Throat: return Mathf.Pow(Mathf.Abs(Mathf.Sin(t * 1.4f)), 6f);   // pulso de inflamación
                case Theme.Gut: case Theme.Colon: case Theme.Acid: return 0.5f + 0.5f * Mathf.Sin(t * 0.7f);   // peristalsis lenta
                default: return 0f;
            }
        }

        private void LateUpdate()
        {
            Camera cam = Cam;
            if (cam == null) return;
            if (layer == null) Build(cam);
            float dt = Time.deltaTime, h = cam.orthographicSize, w = h * cam.aspect;

            // Latido / respiración del fondo (LevelBackdrop ya puso la escala base este cuadro).
            float pulse = Pulse();
            float amount = theme == Theme.Lungs ? 0.035f : theme == Theme.Throat ? 0.015f : theme == Theme.Air ? 0.012f : 0.01f;
            transform.localScale *= 1f + pulse * amount;

            // Destellos de las neuronas (fondo del sistema nervioso).
            if (theme == Theme.Nerve || theme == Theme.Memory)
            {
                flash = Mathf.MoveTowards(flash, 0f, dt * 2.5f);
                if (Random.value < dt * 0.25f) flash = 1f;
            }
            if (backdropRenderer != null)
            {
                float k = 1f + flash * 0.18f + (theme == Theme.Lungs ? pulse * 0.05f : 0f);
                backdropRenderer.color = new Color(Mathf.Min(1f, backdropBase.r * k), Mathf.Min(1f, backdropBase.g * k), Mathf.Min(1f, backdropBase.b * k), backdropBase.a);
            }

            foreach (Transform a in alveoli)
            {
                float s = 0.8f + pulse * 0.35f;
                a.localScale = new Vector3(s, s, 1f);
            }

            float time = Time.time;
            foreach (Bit b in bits)
            {
                b.life += dt;
                Vector2 v = b.vel;
                if (theme == Theme.Lungs) v.x += BreathingCycle.Flow * 0.8f;          // el aire las lleva
                if (b.kind == Kind.Drip && b.life > b.maxLife * 0.4f) v.y -= 6f * (b.life - b.maxLife * 0.4f);
                b.pos += v * dt + new Vector2(Mathf.Sin(time * 0.9f + b.seed) * 0.15f, Mathf.Cos(time * 0.7f + b.seed) * 0.1f) * dt;
                float k = b.life / b.maxLife;
                float fade = Mathf.Clamp01(k * 5f) * Mathf.Clamp01((1f - k) * 4f);
                Color c = b.r.color;
                c.a = b.alpha * fade * (b.kind == Kind.Mote ? 0.7f + 0.3f * Mathf.Sin(time * 3f + b.seed) : 1f);
                b.r.color = c;
                Vector2 scale = Vector2.one * b.size;
                if (b.kind == Kind.Spark) { scale = new Vector2(b.size * 5f, b.size); b.t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(b.vel.y, b.vel.x) * Mathf.Rad2Deg); }
                if (b.kind == Kind.Drip) scale = new Vector2(b.size, b.size * 1.5f);
                if (b.kind == Kind.Floater) scale = new Vector2(b.size * 1.8f, b.size * 0.5f);
                b.t.localScale = new Vector3(scale.x, scale.y, 1f);
                b.t.localPosition = new Vector3(b.pos.x, b.pos.y, 0f);
                bool outside = b.pos.x < -w - 1f || b.pos.x > w + 1f || b.pos.y > h + 1f || b.pos.y < -h - 1f;
                if (b.life >= b.maxLife || outside) Respawn(b, cam, false);
            }
        }
    }
}
