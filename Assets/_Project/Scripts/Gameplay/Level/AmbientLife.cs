using System.Collections.Generic;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// Vida de adorno en el fondo de cada nivel (no hace daño ni estorba). Va en el objeto "Fondo" (LevelBackdrop)
    /// y dibuja, detrás del juego y delante de la pintura:
    ///  - PARTÍCULAS con profundidad: las lejanas son chicas, tenues y casi no se mueven al caminar; las cercanas son
    ///    más grandes y se deslizan más (parallax). Cada órgano tiene las suyas:
    ///      Nariz: polvo, corrientes de aire que cambian de sentido al respirar y gotas de moco.
    ///      Tráquea: corrientes de aire y bolitas de moco que los cilios suben hacia la garganta.
    ///      Pulmones: glóbulos rojos que se ponen rojo vivo al inhalar (se oxigenan) y racimos de alvéolos.
    ///      Estómago: burbujas de ácido que revientan y vapor verde.  Delgado: nutrientes dorados que suben (absorción).
    ///      Grueso: bacterias de la flora meneándose, burbujas de gas y gotas de moco.
    ///      Boca: gotas de saliva que cuelgan, caen y salpican, burbujas y destellos en los dientes.
    ///      Amígdalas: gotas, linfocitos flotando y el pulso de la inflamación.
    ///      Médula: impulsos que SALTAN de nodo en nodo por el axón (conducción saltatoria).
    ///      Cerebro / Hipocampo: impulsos que viajan por las neuronas y "sinapsis" que se encienden; burbujas de memoria.
    ///      Ojo: moscas volantes y rayos de luz que salen de la pupila.
    ///  - Un "latido" del fondo (respirar, peristalsis, pulso) y una viñeta suave que oscurece las orillas.
    ///  - Unas pocas manchas de luz MUY tenues por delante (desenfocadas) que dan sensación de profundidad.
    /// </summary>
    [DefaultExecutionOrder(1002)]
    public class AmbientLife : MonoBehaviour
    {
        // Nuevos temas siempre al final (las escenas guardan el número).
        public enum Theme { Air, Lungs, Acid, Gut, Colon, Mouth, Nerve, Memory, Throat, Eye, Trachea, Spine }

        [SerializeField] private Theme theme = Theme.Air;
        [SerializeField] private int count = 30;

        private enum Kind { Mote, Bubble, Drip, Spark, Floater, Nutrient, Rod, Cell, Streak, Glob, Lymph, Glint }

        private class Bit
        {
            public Transform t;
            public SpriteRenderer r, rim, extra;   // 'extra' solo lo usa el destello (la otra aspa de la estrellita)
            public Vector2 pos, vel;
            public float size, life, maxLife, seed, alpha, depth, hang, floorY;
            public Kind kind;
            public bool falling;
        }

        private class Impulse
        {
            public SpriteRenderer head, core;
            public SpriteRenderer[] trail;
            public Vector2[] history;
            public Vector2 from, to;
            public float t, duration, wobble, seed, sample, wait;
            public bool jumps;
        }

        private class Pop { public SpriteRenderer r; public float t, size, alpha; public bool ring; }

        private class Bokeh { public SpriteRenderer r; public Vector2 pos, vel; public float size, alpha, seed; }

        private const int Trail = 7;
        private const int OrderFar = -288, OrderBits = -280, OrderVignette = -270, OrderFront = 330;

        private readonly List<Bit> bits = new List<Bit>();
        private readonly List<Transform> alveoli = new List<Transform>();
        private readonly List<Impulse> impulses = new List<Impulse>();
        private readonly List<Pop> pops = new List<Pop>();
        private readonly List<Bokeh> bokeh = new List<Bokeh>();
        private readonly List<SpriteRenderer> rays = new List<SpriteRenderer>();
        private Transform layer, front, vignette, rayRoot;
        private SpriteRenderer backdropRenderer;
        private Color backdropBase = Color.white;
        private float flash, lastCamX, lastCamY;

        public void Configure(Theme look, int amount) { theme = look; count = amount; }

        private void Start()
        {
            backdropRenderer = GetComponent<SpriteRenderer>();
            if (backdropRenderer != null) backdropBase = backdropRenderer.color;
        }

        private Camera Cam => Camera.main;

        // ───────────────────────────── Armado ─────────────────────────────

        private void Build(Camera cam)
        {
            float h = cam.orthographicSize, w = h * cam.aspect;
            layer = new GameObject("VidaDelFondo").transform;
            layer.SetParent(cam.transform, false);
            layer.localPosition = new Vector3(0f, 0f, 10f);
            for (int i = 0; i < count; i++) bits.Add(NewBit(cam, true));
            if (theme == Theme.Lungs) BuildAlveoli(cam);

            // Viñeta: oscurece un poco las orillas de la pintura (no el juego).
            float dark = theme == Theme.Eye ? 0.4f : theme == Theme.Air || theme == Theme.Trachea ? 0.28f : 0.38f;
            vignette = CodeSprites.Add(layer, CodeSprites.Vignette, new Color(0.04f, 0f, 0.05f, dark), OrderVignette, Vector2.zero, new Vector2(w * 2.1f, h * 2.1f), "Vineta").transform;

            // Manchas de luz desenfocadas por delante (muy tenues). En el Ojo no: ahí hay que ver todo claro.
            if (theme != Theme.Eye)
            {
                front = new GameObject("LucesCercanas").transform;
                front.SetParent(cam.transform, false);
                front.localPosition = new Vector3(0f, 0f, 5f);
                for (int i = 0; i < 6; i++)
                {
                    var b = new Bokeh { seed = Random.value * 50f, size = Random.Range(1.6f, 3.4f), alpha = Random.Range(0.03f, 0.065f) };
                    b.pos = new Vector2(Random.Range(-w, w), Random.Range(-h * 0.8f, h * 0.9f));
                    b.vel = new Vector2(Random.Range(-0.12f, 0.12f), Random.Range(-0.05f, 0.08f));
                    b.r = CodeSprites.Add(front, CodeSprites.Glow, FrontColor(), OrderFront, b.pos, Vector2.one * b.size, "Desenfoque");
                    bokeh.Add(b);
                }
            }

            // Impulsos nerviosos: van pegados a la pintura (siguen sus axones), no a la cámara.
            int n = theme == Theme.Spine ? 4 : theme == Theme.Nerve ? 5 : theme == Theme.Memory ? 3 : 0;
            for (int i = 0; i < n; i++) impulses.Add(NewImpulse(i));

            if (theme == Theme.Eye) BuildRays();
            lastCamX = cam.transform.position.x;
            lastCamY = cam.transform.position.y;
        }

        private Color FrontColor()
        {
            switch (theme)
            {
                case Theme.Acid: return new Color(0.7f, 1f, 0.5f, 1f);
                case Theme.Gut: return new Color(1f, 0.85f, 0.5f, 1f);
                case Theme.Nerve: case Theme.Spine: case Theme.Memory: return new Color(0.7f, 0.6f, 1f, 1f);
                case Theme.Lungs: case Theme.Air: case Theme.Trachea: return new Color(0.85f, 0.95f, 1f, 1f);
                default: return new Color(1f, 0.85f, 0.88f, 1f);
            }
        }

        private Kind KindFor()
        {
            float v = Random.value;
            switch (theme)
            {
                case Theme.Air: return v < 0.5f ? Kind.Mote : v < 0.85f ? Kind.Streak : Kind.Drip;
                case Theme.Trachea: return v < 0.35f ? Kind.Mote : v < 0.75f ? Kind.Streak : Kind.Glob;
                case Theme.Lungs: return v < 0.45f ? Kind.Mote : Kind.Cell;
                case Theme.Acid: return v < 0.72f ? Kind.Bubble : Kind.Mote;
                case Theme.Gut: return v < 0.5f ? Kind.Nutrient : v < 0.8f ? Kind.Mote : Kind.Bubble;
                case Theme.Colon: return v < 0.35f ? Kind.Rod : v < 0.7f ? Kind.Bubble : v < 0.85f ? Kind.Drip : Kind.Mote;
                case Theme.Mouth: return v < 0.3f ? Kind.Drip : v < 0.62f ? Kind.Bubble : v < 0.8f ? Kind.Glint : Kind.Mote;
                case Theme.Nerve: return v < 0.5f ? Kind.Spark : Kind.Mote;
                case Theme.Spine: return v < 0.35f ? Kind.Spark : Kind.Mote;
                case Theme.Memory: return v < 0.5f ? Kind.Bubble : v < 0.75f ? Kind.Mote : Kind.Spark;
                case Theme.Throat: return v < 0.3f ? Kind.Drip : v < 0.6f ? Kind.Lymph : v < 0.8f ? Kind.Bubble : Kind.Mote;
                case Theme.Eye: return v < 0.35f ? Kind.Floater : Kind.Mote;
                default: return Kind.Mote;
            }
        }

        private Color ColorFor(Kind k)
        {
            switch (k)
            {
                case Kind.Drip:
                    if (theme == Theme.Air) return new Color(0.82f, 0.88f, 0.4f, 1f);      // moco verdoso
                    if (theme == Theme.Colon) return new Color(0.9f, 0.78f, 0.38f, 1f);    // moco amarillento
                    return new Color(0.85f, 0.95f, 1f, 1f);                                // saliva
                case Kind.Bubble:
                    if (theme == Theme.Acid) return new Color(0.6f, 1f, 0.35f, 1f);
                    if (theme == Theme.Colon) return new Color(0.85f, 0.75f, 0.95f, 1f);
                    if (theme == Theme.Memory) return new Color(0.75f, 0.85f, 1f, 1f);
                    return new Color(0.92f, 0.97f, 1f, 1f);
                case Kind.Spark: return new Color(1f, 0.95f, 0.5f, 1f);
                case Kind.Floater: return new Color(0.45f, 0.55f, 0.75f, 1f);
                case Kind.Nutrient: return new Color(1f, 0.85f, 0.35f, 1f);
                case Kind.Rod: return Random.value < 0.5f ? new Color(0.55f, 0.8f, 0.45f, 1f) : new Color(0.75f, 0.55f, 0.85f, 1f);
                case Kind.Cell: return new Color(0.6f, 0.15f, 0.3f, 1f);
                case Kind.Streak: return new Color(0.92f, 0.97f, 1f, 1f);
                case Kind.Glob: return new Color(0.82f, 0.86f, 0.45f, 1f);
                case Kind.Lymph: return new Color(0.78f, 0.62f, 0.95f, 1f);
                case Kind.Glint: return new Color(1f, 1f, 0.95f, 1f);
            }
            switch (theme)
            {
                case Theme.Air: case Theme.Trachea: return new Color(1f, 1f, 0.85f, 1f);
                case Theme.Lungs: return new Color(0.85f, 0.95f, 1f, 1f);
                case Theme.Acid: return new Color(0.6f, 1f, 0.35f, 1f);
                case Theme.Gut: return new Color(1f, 0.9f, 0.55f, 1f);
                case Theme.Colon: return new Color(0.85f, 0.7f, 0.95f, 1f);
                case Theme.Mouth: return new Color(0.85f, 0.95f, 1f, 1f);
                case Theme.Nerve: case Theme.Spine: return new Color(0.75f, 0.6f, 1f, 1f);
                case Theme.Memory: return new Color(1f, 0.85f, 0.5f, 1f);
                case Theme.Throat: return new Color(1f, 0.92f, 0.95f, 1f);
                case Theme.Eye: return new Color(0.7f, 0.85f, 1f, 1f);
            }
            return Color.white;
        }

        private Bit NewBit(Camera cam, bool anywhere)
        {
            var b = new Bit { seed = Random.value * 100f };
            b.t = new GameObject("Particula").transform;
            b.t.SetParent(layer, false);
            b.r = b.t.gameObject.AddComponent<SpriteRenderer>();
            var rimGo = new GameObject("Orilla");
            rimGo.transform.SetParent(b.t, false);
            b.rim = rimGo.AddComponent<SpriteRenderer>();
            Respawn(b, cam, anywhere);
            return b;
        }

        private void Respawn(Bit b, Camera cam, bool anywhere)
        {
            float h = cam.orthographicSize, w = h * cam.aspect;
            b.kind = KindFor();
            b.depth = Random.Range(0.2f, 1f);
            b.life = 0f;
            b.falling = false;
            b.r.sprite = CodeSprites.Circle;
            b.r.sortingOrder = OrderBits - Mathf.RoundToInt((1f - b.depth) * 6f);
            b.r.color = ColorFor(b.kind);
            b.rim.enabled = false;
            b.rim.sortingOrder = b.r.sortingOrder + 1;
            b.rim.transform.localPosition = Vector3.zero;
            b.rim.transform.localScale = Vector3.one;
            b.rim.transform.localRotation = Quaternion.identity;
            b.t.localRotation = Quaternion.identity;
            if (b.extra != null) b.extra.enabled = false;
            float near = Mathf.Lerp(0.6f, 1.2f, b.depth);

            switch (b.kind)
            {
                case Kind.Mote:
                    b.size = Random.Range(0.05f, 0.13f) * near;
                    b.alpha = Random.Range(0.25f, 0.6f);
                    b.vel = theme == Theme.Air || theme == Theme.Trachea ? new Vector2(Random.Range(-0.5f, -0.15f), Random.Range(-0.12f, 0.12f))
                          : new Vector2(Random.Range(-0.25f, 0.25f), Random.Range(-0.1f, 0.25f));
                    b.maxLife = Random.Range(6f, 12f);
                    b.pos = new Vector2(Random.Range(-w, w), Random.Range(-h * 0.6f, h));
                    break;

                case Kind.Bubble:
                    bool memory = theme == Theme.Memory;
                    b.size = (memory ? Random.Range(0.35f, 0.8f) : theme == Theme.Colon || theme == Theme.Acid ? Random.Range(0.2f, 0.55f) : Random.Range(0.14f, 0.38f)) * near;
                    b.alpha = Random.Range(0.18f, 0.35f);
                    b.vel = new Vector2(Random.Range(-0.1f, 0.1f), memory ? Random.Range(0.08f, 0.25f) : Random.Range(0.3f, 0.8f));
                    b.maxLife = memory ? Random.Range(9f, 15f) : Random.Range(4f, 9f);
                    b.pos = new Vector2(Random.Range(-w, w), anywhere ? Random.Range(-h * 0.6f, h) : -h * Random.Range(0.5f, 0.75f));
                    b.r.sprite = CodeSprites.Glow;
                    b.rim.enabled = true;
                    b.rim.sprite = CodeSprites.Ring;
                    b.rim.color = Color.Lerp(b.r.color, Color.white, theme == Theme.Acid ? 0.3f : 0.6f);
                    break;

                case Kind.Drip:
                    b.size = Random.Range(0.1f, 0.18f) * near;
                    b.alpha = 0.65f;
                    b.vel = Vector2.zero;
                    b.hang = Random.Range(0.8f, 2f);
                    b.maxLife = b.hang + 3f;
                    b.floorY = -h * Mathf.Lerp(0.3f, 0.6f, b.depth);
                    b.pos = new Vector2(Random.Range(-w, w), h * Random.Range(0.72f, 0.95f));
                    b.rim.enabled = true;                         // brillo de la gota
                    b.rim.sprite = CodeSprites.Circle;
                    b.rim.color = new Color(1f, 1f, 1f, 0.8f);
                    b.rim.transform.localPosition = new Vector3(-0.18f, 0.15f, 0f);
                    b.rim.transform.localScale = Vector3.one * 0.3f;
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
                    // Moscas volantes: hilitos transparentes con orilla (como los que ves al mirar el cielo).
                    b.size = Random.Range(0.35f, 0.7f) * near;
                    b.alpha = Random.Range(0.12f, 0.25f);
                    b.vel = new Vector2(Random.Range(-0.15f, 0.15f), Random.Range(-0.1f, 0.05f));
                    b.maxLife = Random.Range(10f, 16f);
                    b.pos = new Vector2(Random.Range(-w, w), Random.Range(-h * 0.4f, h));
                    b.r.sprite = CodeSprites.Glow;
                    b.rim.enabled = true;
                    b.rim.sprite = CodeSprites.Ring;
                    b.rim.color = new Color(0.6f, 0.75f, 0.95f, 1f);
                    break;

                case Kind.Nutrient:
                    // Nutrientes ya digeridos: puntitos dorados que suben hacia las vellosidades (absorción).
                    b.size = Random.Range(0.22f, 0.45f) * near;
                    b.alpha = Random.Range(0.55f, 0.9f);
                    b.vel = new Vector2(Random.Range(-0.08f, 0.08f), Random.Range(0.2f, 0.45f));
                    b.maxLife = Random.Range(6f, 11f);
                    b.pos = new Vector2(Random.Range(-w, w), anywhere ? Random.Range(-h * 0.7f, h * 0.6f) : -h * Random.Range(0.25f, 0.75f));
                    b.r.sprite = CodeSprites.Glow;
                    b.r.color = new Color(1f, 0.97f, 0.65f, 1f);
                    b.rim.enabled = true;
                    b.rim.sprite = CodeSprites.Circle;
                    b.rim.color = new Color(1f, 1f, 0.92f, 1f);
                    b.rim.transform.localScale = Vector3.one * 0.32f;
                    break;

                case Kind.Rod:
                    // Bacterias de la flora: bastoncitos que se menean.
                    b.size = Random.Range(0.1f, 0.18f) * near;
                    b.alpha = Random.Range(0.25f, 0.45f);
                    b.vel = new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(-0.08f, 0.08f));
                    b.maxLife = Random.Range(8f, 14f);
                    b.pos = new Vector2(Random.Range(-w, w), Random.Range(-h * 0.5f, h * 0.8f));
                    b.rim.enabled = true;
                    b.rim.sprite = CodeSprites.Circle;
                    b.rim.color = Color.Lerp(b.r.color, Color.black, 0.5f);
                    b.rim.sortingOrder = b.r.sortingOrder - 1;
                    b.rim.transform.localScale = new Vector3(1.18f, 1.45f, 1f);
                    break;

                case Kind.Cell:
                    // Glóbulos rojos por los capilares: entran por la izquierda y se van con la corriente.
                    b.size = Random.Range(0.18f, 0.32f) * near;
                    b.alpha = Random.Range(0.35f, 0.6f);
                    b.vel = new Vector2(Random.Range(0.5f, 1.1f), Random.Range(-0.1f, 0.1f));
                    b.maxLife = Random.Range(10f, 16f);
                    b.pos = new Vector2(anywhere ? Random.Range(-w, w) : -w - 0.5f, Random.Range(-h * 0.5f, h * 0.85f));
                    b.rim.enabled = true;                         // el centro hundido del glóbulo
                    b.rim.sprite = CodeSprites.Circle;
                    b.rim.transform.localScale = Vector3.one * 0.45f;
                    break;

                case Kind.Streak:
                    // Corrientes de aire: rayitas rápidas que van con la respiración.
                    b.size = Random.Range(0.025f, 0.05f);
                    b.alpha = Random.Range(0.1f, 0.22f) * Mathf.Lerp(0.6f, 1f, b.depth);
                    b.hang = Random.Range(1.2f, 3f);              // largo de la rayita
                    b.vel = new Vector2(Random.Range(5f, 9f) * Mathf.Lerp(0.5f, 1f, b.depth), 0f);
                    b.maxLife = Random.Range(0.7f, 1.4f);
                    b.pos = new Vector2(Random.Range(-w, w), Random.Range(-h * 0.7f, h * 0.9f));
                    b.r.sprite = CodeSprites.Square;
                    break;

                case Kind.Glob:
                    // Moco que los cilios suben hacia la garganta (hacia la izquierda), pegado arriba o abajo.
                    b.size = Random.Range(0.3f, 0.6f) * near;
                    b.alpha = Random.Range(0.3f, 0.5f);
                    b.vel = new Vector2(-Random.Range(0.25f, 0.6f), 0f);
                    b.maxLife = Random.Range(12f, 18f);
                    b.pos = new Vector2(anywhere ? Random.Range(-w, w) : w + 0.6f, Random.value < 0.5f ? h * Random.Range(0.6f, 0.85f) : -h * Random.Range(0.45f, 0.65f));
                    b.rim.enabled = true;
                    b.rim.sprite = CodeSprites.Circle;
                    b.rim.color = new Color(1f, 1f, 0.9f, 0.7f);
                    b.rim.transform.localPosition = new Vector3(-0.2f, 0.18f, 0f);
                    b.rim.transform.localScale = Vector3.one * 0.3f;
                    break;

                case Kind.Lymph:
                    // Linfocitos de la amígdala: célula redonda con su núcleo grande.
                    b.size = Random.Range(0.35f, 0.6f) * near;
                    b.alpha = Random.Range(0.2f, 0.35f);
                    b.vel = new Vector2(Random.Range(-0.2f, 0.2f), Random.Range(-0.05f, 0.12f));
                    b.maxLife = Random.Range(10f, 16f);
                    b.pos = new Vector2(Random.Range(-w, w), Random.Range(-h * 0.4f, h * 0.7f));
                    b.rim.enabled = true;
                    b.rim.sprite = CodeSprites.Circle;
                    b.rim.color = new Color(0.45f, 0.25f, 0.65f, 1f);
                    b.rim.transform.localScale = Vector3.one * 0.65f;
                    b.rim.transform.localPosition = new Vector3(0.08f, 0.05f, 0f);
                    break;

                case Kind.Glint:
                    // Destello en los dientes (arriba o abajo de la pintura): una estrellita que se prende y se apaga.
                    b.size = Random.Range(0.35f, 0.7f);
                    b.alpha = Random.Range(0.5f, 0.85f);
                    b.vel = Vector2.zero;
                    b.maxLife = Random.Range(0.6f, 1f);
                    b.pos = new Vector2(Random.Range(-w, w), Random.value < 0.5f ? h * Random.Range(0.72f, 0.88f) : -h * Random.Range(0.6f, 0.72f));
                    // La estrella son dos aspas (hijas) sobre un brillito; el objeto se escala parejo.
                    b.r.sprite = CodeSprites.Glow;
                    b.rim.enabled = true;
                    b.rim.sprite = CodeSprites.Square;
                    b.rim.color = b.r.color;
                    b.rim.transform.localScale = new Vector3(1f, 0.07f, 1f);
                    if (b.extra == null)
                    {
                        var go = new GameObject("Aspa");
                        go.transform.SetParent(b.t, false);
                        b.extra = go.AddComponent<SpriteRenderer>();
                        b.extra.sprite = CodeSprites.Square;
                    }
                    b.extra.enabled = true;
                    b.extra.sortingOrder = b.rim.sortingOrder;
                    b.extra.transform.localScale = new Vector3(0.07f, 1f, 1f);
                    break;
            }
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

        private Impulse NewImpulse(int index)
        {
            var im = new Impulse { seed = Random.value * 30f, jumps = theme == Theme.Spine, history = new Vector2[Trail], trail = new SpriteRenderer[Trail] };
            Color c = theme == Theme.Memory ? new Color(1f, 0.85f, 0.5f, 1f) : theme == Theme.Spine ? new Color(0.55f, 0.95f, 1f, 1f) : new Color(1f, 0.9f, 0.55f, 1f);
            im.head = CodeSprites.Add(transform, CodeSprites.Glow, c, OrderFar + 2, Vector2.zero, Vector2.one, "Impulso");
            im.core = CodeSprites.Add(im.head.transform, CodeSprites.Circle, new Color(1f, 1f, 0.95f, 0.95f), OrderFar + 3, Vector2.zero, Vector2.one * 0.16f, "Nucleo");
            for (int i = 0; i < Trail; i++)
                im.trail[i] = CodeSprites.Add(transform, CodeSprites.Glow, c, OrderFar + 1, Vector2.zero, Vector2.one * 0.5f, "Estela");
            im.wait = index * 1.1f + Random.Range(0f, 1f);
            Restart(im);
            return im;
        }

        private void Restart(Impulse im)
        {
            // Coordenadas de la pintura (centro = 0; mide ~26 x 13 unidades).
            if (im.jumps)
            {
                // Médula: por los axones mielinizados (la cadena del centro y otras más delgadas).
                float[] lanes = { -0.15f, 1.25f, -1.7f, 2.4f };
                float y = lanes[Random.Range(0, lanes.Length)] + Random.Range(-0.1f, 0.1f);
                bool right = Random.value < 0.7f;
                im.from = new Vector2(right ? -13f : 13f, y);
                im.to = new Vector2(right ? 13f : -13f, y);
                im.duration = Random.Range(1.6f, 2.4f);
            }
            else
            {
                im.from = new Vector2(Random.Range(-12f, 4f), Random.Range(-3.5f, 4f));
                im.to = im.from + new Vector2(Random.Range(5f, 10f), Random.Range(-2.5f, 2.5f));
                im.duration = Random.Range(1.2f, 2.2f);
            }
            im.wobble = Random.Range(0.3f, 0.8f);
            im.t = 0f;
            im.sample = 0f;
            for (int i = 0; i < Trail; i++) im.history[i] = im.from;
            SetImpulseVisible(im, false);
        }

        private static void SetImpulseVisible(Impulse im, bool on)
        {
            im.head.enabled = on;
            im.core.enabled = on;
            foreach (SpriteRenderer r in im.trail) r.enabled = on;
        }

        private void BuildRays()
        {
            // Los rayos salen de la pupila (el iris es hijo del fondo); si no hay iris, desde arriba al centro.
            Transform iris = transform.Find("Iris");
            rayRoot = new GameObject("RayosDeLuz").transform;
            rayRoot.SetParent(iris != null ? iris : transform, false);
            rayRoot.localPosition = iris != null ? Vector3.zero : new Vector3(0f, 4.6f, 0f);
            float[] angles = { -30f, -14f, 2f, 17f, 31f };
            for (int i = 0; i < angles.Length; i++)
            {
                SpriteRenderer r = CodeSprites.Add(rayRoot, CodeSprites.Beam, new Color(0.85f, 0.95f, 1f, 0.1f), -298, Vector2.zero, new Vector2(Random.Range(1.2f, 2.4f), Random.Range(9f, 12f)), "Rayo");
                r.transform.localRotation = Quaternion.Euler(0f, 0f, angles[i]);
                rays.Add(r);
            }
        }

        // ───────────────────────────── Cada cuadro ─────────────────────────────

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
                case Theme.Air: case Theme.Trachea: return 0.5f + 0.5f * Mathf.Sin(t / 5.5f * Mathf.PI * 2f);   // respirar tranquilo
                case Theme.Throat: return Mathf.Pow(Mathf.Abs(Mathf.Sin(t * 1.4f)), 6f);   // pulso de inflamación
                case Theme.Gut: case Theme.Colon: case Theme.Acid: return 0.5f + 0.5f * Mathf.Sin(t * 0.7f);   // peristalsis lenta
                case Theme.Spine: return Mathf.Pow(Mathf.Abs(Mathf.Sin(t * 1.2f)), 8f);    // pulso del líquido cefalorraquídeo
                default: return 0f;
            }
        }

        /// <summary>Sentido del aire en la nariz/tráquea: +1 entra (inhalar, hacia la derecha), -1 sale.</summary>
        private float AirDirection()
        {
            float c = Mathf.Cos(Time.timeSinceLevelLoad / 5.5f * Mathf.PI * 2f);
            return Mathf.Abs(c) < 0.15f ? c / 0.15f : Mathf.Sign(c);
        }

        private void LateUpdate()
        {
            Camera cam = Cam;
            if (cam == null) return;
            if (layer == null) Build(cam);
            float dt = Time.deltaTime, h = cam.orthographicSize, w = h * cam.aspect;
            Vector3 cp = cam.transform.position;
            float camDx = cp.x - lastCamX, camDy = cp.y - lastCamY;
            if (Mathf.Abs(camDx) > 6f || Mathf.Abs(camDy) > 6f) { camDx = 0f; camDy = 0f; }   // reaparecer: no arrastrar todo
            lastCamX = cp.x;
            lastCamY = cp.y;
            float time = Time.time;

            // Latido / respiración del fondo (LevelBackdrop ya puso la escala base este cuadro).
            float pulse = Pulse();
            float amount = theme == Theme.Lungs ? 0.035f : theme == Theme.Throat ? 0.015f : theme == Theme.Air || theme == Theme.Trachea ? 0.012f : 0.01f;
            Vector3 s = transform.localScale * (1f + pulse * amount);
            if (theme == Theme.Gut || theme == Theme.Colon) s.y *= 1f + Mathf.Sin(time * 0.7f + 1.2f) * 0.008f;   // apretón de la peristalsis
            transform.localScale = s;
            if (theme == Theme.Acid) transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 0.35f) * 0.45f);   // el estómago revuelve

            // Destellos de las neuronas (fondo del sistema nervioso).
            if (theme == Theme.Nerve || theme == Theme.Memory || theme == Theme.Spine)
            {
                flash = Mathf.MoveTowards(flash, 0f, dt * 2.5f);
                if (Random.value < dt * 0.25f) flash = 1f;
            }
            if (backdropRenderer != null)
            {
                float k = 1f + flash * 0.18f + (theme == Theme.Lungs ? pulse * 0.05f : 0f) + (theme == Theme.Throat ? pulse * 0.04f : 0f);
                Color tint = backdropBase;
                if (theme == Theme.Throat) tint = Color.Lerp(backdropBase, new Color(1f, 0.85f, 0.85f, backdropBase.a), pulse * 0.5f);
                backdropRenderer.color = new Color(Mathf.Min(1f, tint.r * k), Mathf.Min(1f, tint.g * k), Mathf.Min(1f, tint.b * k), tint.a);
            }

            if (vignette != null) vignette.localScale = new Vector3(w * 2.1f, h * 2.1f, 1f);

            foreach (Transform a in alveoli)
            {
                float sc = 0.8f + pulse * 0.35f;
                a.localScale = new Vector3(sc, sc, 1f);
            }

            UpdateBits(cam, dt, w, h, camDx, camDy, pulse, time);
            UpdateImpulses(dt);
            UpdatePops(dt);
            UpdateBokeh(dt, w, h, camDx, camDy, time);
            UpdateRays(time);
        }

        private void UpdateBits(Camera cam, float dt, float w, float h, float camDx, float camDy, float pulse, float time)
        {
            float airDir = AirDirection();
            foreach (Bit b in bits)
            {
                b.life += dt;
                Vector2 v = b.vel;
                float parallax = Mathf.Lerp(0.08f, 0.4f, b.depth);
                switch (b.kind)
                {
                    case Kind.Streak: v.x = Mathf.Abs(b.vel.x) * airDir; break;
                    case Kind.Mote:
                        if (theme == Theme.Lungs) v.x += BreathingCycle.Flow * 0.8f;          // el aire las lleva
                        if (theme == Theme.Air || theme == Theme.Trachea) v.x = b.vel.x * 0.3f + airDir * 0.6f;
                        break;
                    case Kind.Cell: v.x += BreathingCycle.Flow * 0.4f; break;
                    case Kind.Drip:
                        if (b.life < b.hang) v = Vector2.zero;                                // colgando: se va formando
                        else { b.falling = true; v.y = b.vel.y - 9f * Mathf.Lerp(0.5f, 1f, b.depth) * (b.life - b.hang); }
                        break;
                }

                Vector2 wander = b.kind == Kind.Drip || b.kind == Kind.Streak || b.kind == Kind.Glint ? Vector2.zero
                    : new Vector2(Mathf.Sin(time * 0.9f + b.seed) * 0.15f, Mathf.Cos(time * 0.7f + b.seed) * 0.1f);
                b.pos += v * dt + wander * dt - new Vector2(camDx, camDy * 0.5f) * parallax;

                float k = b.life / b.maxLife;
                float fade = Mathf.Clamp01(k * 5f) * Mathf.Clamp01((1f - k) * 4f);
                float a = b.alpha * fade;
                Vector2 scale = Vector2.one * b.size;
                switch (b.kind)
                {
                    case Kind.Mote: a *= 0.7f + 0.3f * Mathf.Sin(time * 3f + b.seed); break;
                    case Kind.Spark:
                        scale = new Vector2(b.size * 5f, b.size);
                        b.t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(b.vel.y, b.vel.x) * Mathf.Rad2Deg);
                        break;
                    case Kind.Drip:
                    {
                        a = b.alpha * Mathf.Clamp01(b.life * 3f);
                        float grow = Mathf.Clamp01(b.life / b.hang);
                        // Colgando se estira poco a poco; cayendo es una gota alargada.
                        scale = b.falling ? new Vector2(b.size * 0.9f, b.size * 1.7f) : new Vector2(b.size * (0.5f + 0.5f * grow), b.size * (0.6f + 1.2f * grow));
                        break;
                    }
                    case Kind.Floater:
                        scale = new Vector2(b.size * 1.8f, b.size * 0.5f);
                        b.t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 0.2f + b.seed) * 25f);
                        break;
                    case Kind.Bubble:
                    {
                        float wob = 1f + Mathf.Sin(time * 4f + b.seed) * 0.06f;
                        scale = new Vector2(b.size * wob, b.size / wob);
                        if (theme == Theme.Memory)   // orilla tornasol (como pompa de jabón)
                        {
                            Color iri = Color.HSVToRGB(Mathf.Repeat(time * 0.08f + b.seed, 1f), 0.45f, 1f);
                            b.rim.color = new Color(iri.r, iri.g, iri.b, Mathf.Min(1f, a * 2.6f));
                        }
                        else b.rim.color = new Color(b.rim.color.r, b.rim.color.g, b.rim.color.b, Mathf.Min(1f, a * 2.2f));
                        a *= 0.45f;
                        break;
                    }
                    case Kind.Nutrient: a *= 0.65f + 0.35f * Mathf.Sin(time * 4f + b.seed); break;
                    case Kind.Rod:
                        scale = new Vector2(b.size * 3f, b.size);
                        b.t.localRotation = Quaternion.Euler(0f, 0f, b.seed * 3f + Mathf.Sin(time * 2.4f + b.seed) * 20f);
                        b.rim.color = new Color(b.rim.color.r, b.rim.color.g, b.rim.color.b, a);
                        break;
                    case Kind.Cell:
                    {
                        // Oscuro (sin oxígeno) → rojo vivo cuando el pulmón se llena de aire.
                        Color body = Color.Lerp(new Color(0.55f, 0.15f, 0.32f), new Color(0.95f, 0.18f, 0.2f), pulse);
                        b.r.color = new Color(body.r, body.g, body.b, a);
                        b.rim.color = new Color(body.r * 0.7f, body.g * 0.7f, body.b * 0.7f, a);
                        b.t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time + b.seed) * 30f);
                        scale = new Vector2(b.size, b.size * (0.75f + 0.25f * Mathf.Abs(Mathf.Sin(time * 0.8f + b.seed))));
                        break;
                    }
                    case Kind.Streak:
                        scale = new Vector2(b.hang, b.size);
                        a *= Mathf.Abs(airDir);
                        break;
                    case Kind.Glob:
                    {
                        float sq = 1f + Mathf.Sin(time * 3f + b.seed) * 0.1f;
                        scale = new Vector2(b.size * 1.3f * sq, b.size / sq);
                        break;
                    }
                    case Kind.Lymph:
                        b.rim.color = new Color(b.rim.color.r, b.rim.color.g, b.rim.color.b, a * 1.3f);
                        break;
                    case Kind.Glint:
                    {
                        float tw = Mathf.Sin(k * Mathf.PI);
                        a = b.alpha * tw * 0.5f;
                        scale = Vector2.one * Mathf.Max(0.01f, b.size * tw);
                        b.t.localRotation = Quaternion.Euler(0f, 0f, 45f + k * 40f);
                        b.rim.color = new Color(1f, 1f, 0.95f, b.alpha * tw);
                        if (b.extra != null) b.extra.color = b.rim.color;
                        break;
                    }
                }
                if (b.kind != Kind.Bubble && b.kind != Kind.Rod && b.kind != Kind.Cell && b.kind != Kind.Lymph && b.kind != Kind.Glint && b.rim.enabled)
                    b.rim.color = new Color(b.rim.color.r, b.rim.color.g, b.rim.color.b, a * 1.2f);
                if (b.kind != Kind.Cell)
                {
                    Color c = b.r.color;
                    c.a = a;
                    b.r.color = c;
                }
                b.t.localScale = new Vector3(scale.x, scale.y, 1f);
                b.t.localPosition = new Vector3(b.pos.x, b.pos.y, 0f);

                // Las que flotan dan la vuelta por los lados (así no "se acaban" al caminar).
                bool wraps = b.kind == Kind.Mote || b.kind == Kind.Floater || b.kind == Kind.Rod || b.kind == Kind.Lymph || b.kind == Kind.Nutrient || b.kind == Kind.Bubble;
                if (wraps)
                {
                    if (b.pos.x < -w - 1f) b.pos.x += 2f * w + 2f;
                    else if (b.pos.x > w + 1f) b.pos.x -= 2f * w + 2f;
                }

                // Gota que llega al "piso" del fondo: salpica.
                if (b.kind == Kind.Drip && b.falling && b.pos.y <= b.floorY)
                {
                    SpawnPop(b.pos, b.size * 3.5f, 0.5f, true, b.r.color, OrderBits);
                    Respawn(b, cam, false);
                    continue;
                }
                bool outside = b.pos.x < -w - 3f || b.pos.x > w + 3f || b.pos.y > h + 1f || b.pos.y < -h - 1f;
                if (b.life >= b.maxLife || outside)
                {
                    // Las burbujas revientan con un aro (las de memoria sueltan un destello).
                    if (b.kind == Kind.Bubble && !outside && theme != Theme.Gut)
                        SpawnPop(b.pos, b.size * 2.2f, theme == Theme.Memory ? 0.6f : 0.4f, true, b.rim.color, OrderBits);
                    Respawn(b, cam, false);
                }
            }
        }

        private void UpdateImpulses(float dt)
        {
            foreach (Impulse im in impulses)
            {
                if (im.wait > 0f) { im.wait -= dt; continue; }
                im.t += dt;
                float k = Mathf.Clamp01(im.t / im.duration);
                Vector2 p;
                if (im.jumps)
                {
                    // Conducción saltatoria: el impulso brinca de un nodo de Ranvier al siguiente (cada 1.3 u).
                    float total = Vector2.Distance(im.from, im.to);
                    int nodes = Mathf.Max(1, Mathf.RoundToInt(total / 1.3f));
                    int node = Mathf.Min(nodes, Mathf.FloorToInt(k * nodes));
                    p = Vector2.Lerp(im.from, im.to, (float)node / nodes);
                    float sub = k * nodes - node;
                    im.head.transform.localScale = Vector3.one * (0.9f + 1f * Mathf.Clamp01(1f - sub * 2.5f));
                    if (node != Mathf.RoundToInt(im.sample))
                    {
                        im.sample = node;
                        for (int i = Trail - 1; i > 0; i--) im.history[i] = im.history[i - 1];
                        im.history[0] = p;
                    }
                }
                else
                {
                    Vector2 d = im.to - im.from;
                    Vector2 n = new Vector2(-d.y, d.x).normalized;
                    p = Vector2.Lerp(im.from, im.to, k) + n * Mathf.Sin(k * Mathf.PI * 3f + im.seed) * im.wobble;
                    im.head.transform.localScale = Vector3.one * (1.1f + Mathf.Sin(Time.time * 20f) * 0.1f);
                    im.sample += dt;
                    if (im.sample >= 0.035f)
                    {
                        im.sample = 0f;
                        for (int i = Trail - 1; i > 0; i--) im.history[i] = im.history[i - 1];
                        im.history[0] = p;
                    }
                }
                SetImpulseVisible(im, true);
                im.head.transform.localPosition = new Vector3(p.x, p.y, 0f);
                Color hc = im.head.color;
                im.head.color = new Color(hc.r, hc.g, hc.b, 0.85f);
                for (int i = 0; i < Trail; i++)
                {
                    SpriteRenderer r = im.trail[i];
                    r.transform.localPosition = new Vector3(im.history[i].x, im.history[i].y, 0f);
                    float f = 1f - (i + 1f) / (Trail + 1f);
                    r.transform.localScale = Vector3.one * (im.jumps ? 0.9f : 0.6f) * (0.5f + f * 0.5f);
                    Color c = r.color;
                    r.color = new Color(c.r, c.g, c.b, 0.65f * f);
                }
                if (k >= 1f)
                {
                    // Llegó a la sinapsis: se enciende un momento.
                    Vector3 world = transform.TransformPoint(new Vector3(p.x, p.y, 0f));
                    Vector3 local = layer.InverseTransformPoint(world);
                    SpawnPop(new Vector2(local.x, local.y), im.jumps ? 1.2f : 1.8f, 0.45f, false, im.head.color, OrderFar + 3);
                    Restart(im);
                    im.wait = Random.Range(0.4f, 2.2f);
                }
            }
        }

        private void SpawnPop(Vector2 pos, float size, float alpha, bool ring, Color color, int order)
        {
            Pop p = null;
            foreach (Pop q in pops) if (q.t < 0f) { p = q; break; }
            if (p == null)
            {
                if (pops.Count >= 16) return;
                p = new Pop { r = CodeSprites.Add(layer, CodeSprites.Ring, Color.white, order, pos, Vector2.one) };
                p.r.name = "Plop";
                pops.Add(p);
            }
            p.t = 0f;
            p.size = size;
            p.alpha = alpha;
            p.ring = ring;
            p.r.sprite = ring ? CodeSprites.Ring : CodeSprites.Glow;
            p.r.sortingOrder = order;
            p.r.color = new Color(color.r, color.g, color.b, alpha);
            p.r.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            p.r.enabled = true;
        }

        private void UpdatePops(float dt)
        {
            foreach (Pop p in pops)
            {
                if (p.t < 0f) continue;
                p.t += dt / 0.35f;
                if (p.t >= 1f) { p.t = -1f; p.r.enabled = false; continue; }
                float e = 1f - (1f - p.t) * (1f - p.t);
                float sx = p.size * (0.4f + 0.8f * e);
                p.r.transform.localScale = new Vector3(sx, p.ring ? sx * 0.55f : sx, 1f);   // las salpicaduras son planas
                Color c = p.r.color;
                p.r.color = new Color(c.r, c.g, c.b, p.alpha * (1f - p.t));
            }
        }

        private void UpdateBokeh(float dt, float w, float h, float camDx, float camDy, float time)
        {
            foreach (Bokeh b in bokeh)
            {
                // Están "más cerca" que el juego: se deslizan más rápido que el piso al caminar.
                b.pos += b.vel * dt - new Vector2(camDx, camDy * 0.6f) * 1.35f;
                if (b.pos.x < -w - b.size) b.pos.x += 2f * w + 2f * b.size;
                else if (b.pos.x > w + b.size) b.pos.x -= 2f * w + 2f * b.size;
                if (b.pos.y < -h - b.size) b.pos.y += 2f * h + b.size;
                else if (b.pos.y > h + b.size) b.pos.y -= 2f * h + b.size;
                b.r.transform.localPosition = new Vector3(b.pos.x, b.pos.y, 0f);
                Color c = b.r.color;
                b.r.color = new Color(c.r, c.g, c.b, b.alpha * (0.7f + 0.3f * Mathf.Sin(time * 0.6f + b.seed)));
            }
        }

        private void UpdateRays(float time)
        {
            if (rays.Count == 0) return;
            StealthMeter m = StealthMeter.Current;
            float alarm = m != null ? Mathf.Clamp01(m.Level) : 0f;
            for (int i = 0; i < rays.Count; i++)
            {
                SpriteRenderer r = rays[i];
                // Brillan como luz entre polvo: suben y bajan despacio, y más fuerte si el ojo está alerta.
                float a = 0.05f + 0.06f * (0.5f + 0.5f * Mathf.Sin(time * (0.5f + i * 0.13f) + i * 1.7f)) + alarm * 0.05f;
                r.color = new Color(0.85f, 0.95f, 1f, a);
                float baseAngle = -30f + i * 15.25f;
                r.transform.localRotation = Quaternion.Euler(0f, 0f, baseAngle + Mathf.Sin(time * 0.3f + i) * 2.5f);
            }
        }

        private void OnDestroy()
        {
            if (layer != null) Destroy(layer.gameObject);
            if (front != null) Destroy(front.gameObject);
        }
    }
}
