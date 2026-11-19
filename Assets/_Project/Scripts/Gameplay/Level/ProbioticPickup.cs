using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// PROBIÓTICOS: el yogur que comiste llega hasta el colon. La botella quedó en el piso, hundida en el moco,
    /// y alrededor nadan sus LACTOBACILOS (bacterias buenas, las cápsulas verdes).
    /// Al tocarla, los lactobacilos se van a reforzar la flora: unos segundos la flora del colon se pone brava
    /// y ataca a los enemigos que tenga cerca. Después las bacterias se reproducen y la colonia vuelve a llenarse.
    /// Nada flota: todo está apoyado en el piso (la posición del objeto es el piso).
    /// </summary>
    public class ProbioticPickup : MonoBehaviour
    {
        [SerializeField] private float duration = 10f;
        [SerializeField] private float radius = 0.9f;
        [SerializeField] private float respawnTime = 20f;
        [SerializeField, TextArea] private string message = "NODO: ¡Probióticos! Los lactobacilos del yogur se unen a tu flora y atacan a los enemigos cercanos.";
        [SerializeField] private Transform visual;
        [SerializeField] private int bacteria = 6;

        private Transform[] bugs;
        private Vector2[] home;
        private SpriteRenderer[] bottleParts;
        private float[] leaving;           // >0: ese lactobacilo va saliendo (segundos que lleva)
        private bool empty;
        private float refillAt;

        public void Configure(Transform art) => visual = art;

        private void Start()
        {
            bottleParts = visual != null ? visual.GetComponentsInChildren<SpriteRenderer>(true) : new SpriteRenderer[0];
            // Charco de yogur donde está hundida la botella.
            CodeSprites.Add(transform, CodeSprites.Circle, new Color(0.35f, 0.4f, 0.3f, 0.6f), 18, new Vector2(0f, 0.04f), new Vector2(2.3f, 0.42f), "Sombra");
            CodeSprites.Add(transform, CodeSprites.Circle, new Color(0.97f, 0.97f, 0.9f, 0.95f), 19, new Vector2(0f, 0.08f), new Vector2(2f, 0.32f), "Yogur");
            bugs = new Transform[bacteria];
            home = new Vector2[bacteria];
            leaving = new float[bacteria];
            for (int i = 0; i < bacteria; i++)
            {
                var bug = new GameObject("Lactobacilo").transform;
                bug.SetParent(transform, false);
                // Lactobacilo: bastoncito verde con contorno.
                CodeSprites.Add(bug, CodeSprites.Circle, new Color(0.1f, 0.3f, 0.12f), 22, Vector2.zero, new Vector2(0.38f, 0.18f), "Contorno");
                CodeSprites.Add(bug, CodeSprites.Circle, new Color(0.45f, 0.9f, 0.4f), 23, Vector2.zero, new Vector2(0.32f, 0.12f), "Cuerpo");
                float side = i % 2 == 0 ? -1f : 1f;
                home[i] = new Vector2(side * (0.45f + 0.18f * (i / 2)), 0.16f + 0.1f * (i % 3));
                bugs[i] = bug;
            }
        }

        private void Update()
        {
            float t = Time.time;
            for (int i = 0; i < bugs.Length; i++)
            {
                if (bugs[i] == null) continue;
                if (leaving[i] > 0f)
                {
                    // Se van volando hacia adelante y hacia la flora (desaparecen).
                    leaving[i] += Time.deltaTime;
                    float k = leaving[i];
                    bugs[i].localPosition = new Vector3(home[i].x + k * (4f + i), home[i].y + Mathf.Sin(k * 9f + i) * 0.3f + k * 1.5f, 0f);
                    if (k > 1.2f) { bugs[i].gameObject.SetActive(false); leaving[i] = 0f; }
                    continue;
                }
                if (!bugs[i].gameObject.activeSelf) continue;
                // Nadan en el charco, pegados al piso, meneándose.
                float a = t * (1.4f + i * 0.17f) + i;
                bugs[i].localPosition = new Vector3(home[i].x + Mathf.Sin(a) * 0.12f, home[i].y + Mathf.Abs(Mathf.Sin(a * 1.7f)) * 0.08f, 0f);
                bugs[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(a * 2f) * 25f);
            }
            // La botella se mece apenitas (está hundida en el moco, no flota).
            if (visual != null) visual.localRotation = Quaternion.Euler(0f, 0f, -8f + Mathf.Sin(t * 1.3f) * 2f);

            if (empty)
            {
                // Las bacterias se reproducen: vuelven una por una.
                float span = Mathf.Max(1f, respawnTime);
                int back = Mathf.Clamp(Mathf.FloorToInt((t - (refillAt - span)) / span * bugs.Length), 0, bugs.Length);
                for (int i = 0; i < back; i++)
                    if (bugs[i] != null && !bugs[i].gameObject.activeSelf && leaving[i] <= 0f) bugs[i].gameObject.SetActive(true);
                if (t < refillAt) return;
                empty = false;
                foreach (SpriteRenderer r in bottleParts) if (r != null) r.color = Color.white;
            }

            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive) return;
            if (Vector2.Distance(player.Position + Vector2.up * 0.6f, (Vector2)transform.position + Vector2.up * 0.5f) > radius + 0.6f) return;
            FriendlyBacteria.Rally(duration);
            GameplayEvents.RaiseMessage(message);
            Sfx.Effect("Recoger_Capsula", 0.9f);
            empty = true;
            refillAt = t + respawnTime;
            for (int i = 0; i < bugs.Length; i++) if (bugs[i] != null && bugs[i].gameObject.activeSelf) leaving[i] = 0.01f;
            foreach (SpriteRenderer r in bottleParts) if (r != null) r.color = new Color(0.65f, 0.65f, 0.65f, 0.9f);   // botella vacía
        }
    }
}
