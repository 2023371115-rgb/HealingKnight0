using System.Collections.Generic;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Flight
{
    /// <summary>
    /// GLÓBULOS ROJOS que viajan contigo por la arteria (tráfico): van un poco más lento que la pantalla, así que
    /// los vas rebasando. No hacen daño, pero estorban:
    /// - Te empujan suave si te les pegas.
    /// - Detienen las balas, las tuyas Y las de los enemigos: úsalos de escudo, pero no dispares a ciegas.
    /// Giran como discos (son discos con el centro hundido). Entre 'fromX' y 'toX'.
    /// Biología: son las células más abundantes de la sangre; llevan el oxígeno pegado a la hemoglobina.
    /// </summary>
    public class BloodStream : MonoBehaviour
    {
        [SerializeField] private float fromX, toX = 100f;
        [SerializeField] private int cells = 8;
        [SerializeField] private Vector2 sizeRange = new Vector2(1.1f, 1.7f);
        [Tooltip("Qué tan lento van respecto a la pantalla (u/s hacia la izquierda).")]
        [SerializeField] private Vector2 lagRange = new Vector2(0.6f, 1.8f);
        [Tooltip("Banda vertical donde viajan (respecto al centro de la pantalla).")]
        [SerializeField] private float band = 3.2f;
        [SerializeField] private Sprite art;
        [Tooltip("0 = siempre; 1/2 = solo en esa rama de la bifurcación.")]
        [SerializeField] private int branch;

        private class Cell
        {
            public Transform t;
            public SpriteRenderer body, dimple;
            public Vector2 screen;
            public float lag, size, spin, phase, wait;
            public bool live;
        }

        private readonly List<Cell> pool = new List<Cell>();
        private float aspect = 1f;   // alto / ancho del dibujo (el de arte es un disco visto de lado)

        public void Configure(float startX, float endX, int count, float verticalBand, Sprite sprite, int routeBranch = 0)
        {
            branch = routeBranch;
            fromX = startX;
            toX = endX;
            cells = count;
            band = verticalBand;
            art = sprite;
        }

        private void Start()
        {
            if (art != null && art.bounds.size.x > 0f) aspect = art.bounds.size.y / art.bounds.size.x;
            for (int i = 0; i < cells; i++)
            {
                var c = new Cell { wait = i * 1.1f + Random.value };
                c.t = new GameObject("GloboRojo").transform;
                c.t.SetParent(transform, false);
                if (art != null)
                {
                    c.body = c.t.gameObject.AddComponent<SpriteRenderer>();
                    c.body.sprite = art;
                    c.body.sortingOrder = 2;
                }
                else
                {
                    // Dibujo de relleno: disco rojo con el centro hundido más oscuro y su contorno.
                    CodeSprites.Add(c.t, CodeSprites.Circle, new Color(0.35f, 0.02f, 0.06f), 1, Vector2.zero, Vector2.one * 1.08f, "Contorno");
                    c.body = CodeSprites.Add(c.t, CodeSprites.Circle, new Color(0.88f, 0.16f, 0.2f), 2, Vector2.zero, Vector2.one, "Disco");
                    c.dimple = CodeSprites.Add(c.t, CodeSprites.Circle, new Color(0.68f, 0.08f, 0.12f), 3, Vector2.zero, Vector2.one * 0.5f, "Centro");
                    CodeSprites.Add(c.t, CodeSprites.Circle, new Color(1f, 0.6f, 0.6f, 0.6f), 4, new Vector2(-0.18f, 0.22f), new Vector2(0.28f, 0.14f), "Brillo");
                }
                c.t.gameObject.SetActive(false);
                pool.Add(c);
            }
        }

        private void Update()
        {
            FlightRoute route = FlightRoute.Current;
            if (route == null) return;
            float dt = Time.deltaTime;
            float hw = route.HalfWidth;
            bool inRange = route.View.xMax > fromX && route.View.xMin < toX && route.Launched && route.InBranch(branch);
            PlayerController player = PlayerController.Current;
            ShipFlight ship = player != null ? player.Flight : null;

            foreach (Cell c in pool)
            {
                if (!c.live)
                {
                    if (!inRange) continue;
                    c.wait -= dt;
                    if (c.wait > 0f) continue;
                    c.live = true;
                    c.size = Random.Range(sizeRange.x, sizeRange.y);
                    c.lag = Random.Range(lagRange.x, lagRange.y);
                    c.spin = Random.Range(0.6f, 1.4f) * (Random.value < 0.5f ? -1f : 1f);
                    c.phase = Random.value * 6f;
                    c.screen = new Vector2(hw + c.size, Random.Range(-band, band));
                    c.t.gameObject.SetActive(true);
                }

                // Avanzan con la sangre, un poco más lento que tú; con cada latido se empujan un poco.
                c.screen.x -= (c.lag - Heartbeat.SurgeNow * 0.6f) * dt;
                c.phase += c.spin * dt;
                c.screen.y += Mathf.Sin(c.phase * 0.7f) * 0.25f * dt;
                Vector2 world = route.CameraPosition + c.screen;
                c.t.position = new Vector3(world.x, world.y, 0f);
                // Disco que gira: se ve delgado de canto y redondo de frente.
                float face = 0.45f + 0.55f * Mathf.Abs(Mathf.Cos(c.phase));
                c.t.localScale = new Vector3(c.size * face, c.size, 1f);
                c.t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(c.phase * 0.5f) * 25f);

                float r = c.size * 0.46f;
                // Atrapan balas (de los dos lados). El disco es más ancho que alto: se mide como óvalo.
                for (int i = Projectile.Flying.Count - 1; i >= 0; i--)
                {
                    Projectile p = Projectile.Flying[i];
                    if (p == null) continue;
                    Vector2 d = (Vector2)p.Position - world;
                    d.y /= Mathf.Max(0.3f, aspect);
                    if (d.sqrMagnitude < r * r) p.Consume();
                }
                // Te empujan suave si te les encimas.
                if (ship != null && player.Health.IsAlive)
                {
                    Vector2 d = (player.Position + new Vector2(0f, 0.55f)) - world;
                    float reach = r + 0.55f;
                    if (d.sqrMagnitude < reach * reach && d.sqrMagnitude > 0.0001f)
                        ship.AddPush(d.normalized * 9f * (1f - d.magnitude / reach));
                }

                if (c.screen.x < -hw - c.size * 1.2f)
                {
                    c.live = false;
                    c.wait = Random.Range(0.6f, 2.2f);
                    c.t.gameObject.SetActive(false);
                }
            }
        }
    }
}
