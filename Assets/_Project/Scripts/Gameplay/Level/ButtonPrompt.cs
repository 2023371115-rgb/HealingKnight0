using HealingKnight.Gameplay.Player;
using TMPro;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// Globito con el botón que hay que presionar ("[S] Hilo dental", "[Flecha abajo] Esconderse") que aparece
    /// cuando estás cerca. Siempre dice la tecla o el botón que tienes puesto (cambia si usas control).
    /// </summary>
    public class ButtonPrompt : MonoBehaviour
    {
        public enum HideWhen { Never, Swinging, Crouching }

        [Tooltip("Acción de PlayerControls: herramienta, abajo, saltar, fijar...")]
        [SerializeField] private string token = "herramienta";
        [SerializeField] private string verb = "Usar";
        [SerializeField] private Vector2 offset = new Vector2(0f, 1.6f);
        [SerializeField] private float radius = 3f;
        [SerializeField] private HideWhen hideWhen = HideWhen.Never;
        [SerializeField] private Font font;

        private Transform root;
        private TextMeshPro text;
        private SpriteRenderer fill, edge, capL, capR, edgeL, edgeR;
        private SpriteRenderer[] outlines, insides;
        private float alpha, bob;
        private string shown;

        public void Configure(string token, string verb, Vector2 offset, float radius, HideWhen hideWhen, Font font)
        {
            this.token = token;
            this.verb = verb;
            this.offset = offset;
            this.radius = radius;
            this.hideWhen = hideWhen;
            this.font = font;
        }

        private void Awake()
        {
            root = new GameObject("AvisoBoton").transform;
            root.SetParent(transform, false);
            root.localPosition = offset;
            Color line = new Color(0.16f, 0.05f, 0.1f), inside = new Color(0.32f, 0.12f, 0.24f);
            edge = CodeSprites.Add(root, CodeSprites.Square, line, 60, Vector2.zero, new Vector2(2f, 0.62f), "Borde");
            edgeL = CodeSprites.Add(root, CodeSprites.Circle, line, 60, Vector2.zero, new Vector2(0.62f, 0.62f), "BordeIzq");
            edgeR = CodeSprites.Add(root, CodeSprites.Circle, line, 60, Vector2.zero, new Vector2(0.62f, 0.62f), "BordeDer");
            fill = CodeSprites.Add(root, CodeSprites.Square, inside, 61, Vector2.zero, new Vector2(2f, 0.5f), "Fondo");
            capL = CodeSprites.Add(root, CodeSprites.Circle, inside, 61, Vector2.zero, new Vector2(0.5f, 0.5f), "FondoIzq");
            capR = CodeSprites.Add(root, CodeSprites.Circle, inside, 61, Vector2.zero, new Vector2(0.5f, 0.5f), "FondoDer");
            outlines = new[] { edge, edgeL, edgeR };
            insides = new[] { fill, capL, capR };
            text = WorldText.Create(root, "", Vector2.zero, new Vector2(6f, 0.46f), new Color(1f, 0.96f, 0.9f), 62, font, 3.4f);
            text.enableAutoSizing = false;
            text.fontSize = 3f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            Refresh();
            SetAlpha(0f);
        }

        private void OnEnable() => PlayerControls.Changed += Refresh;
        private void OnDisable() => PlayerControls.Changed -= Refresh;

        private void Refresh()
        {
            if (text == null) return;
            string key = PlayerControls.Name(token);
            string now = $"<color=#FFD24D>{key}</color>  {verb}";
            if (now == shown) return;
            shown = now;
            text.text = now;
            text.ForceMeshUpdate();
            float w = Mathf.Max(1.2f, text.preferredWidth + 0.3f);
            edge.transform.localScale = new Vector3(w, 0.62f, 1f);
            fill.transform.localScale = new Vector3(w, 0.5f, 1f);
            edgeL.transform.localPosition = capL.transform.localPosition = new Vector3(-w * 0.5f, 0f, 0f);
            edgeR.transform.localPosition = capR.transform.localPosition = new Vector3(w * 0.5f, 0f, 0f);
        }

        private void Update()
        {
            PlayerController p = PlayerController.Current;
            bool show = false;
            if (p != null && p.CurrentState == PlayerController.State.Normal)
            {
                Vector2 d = p.Position - (Vector2)transform.position;
                show = d.sqrMagnitude < radius * radius;
                if (hideWhen == HideWhen.Swinging && p.Swinging) show = false;
                if (hideWhen == HideWhen.Crouching && p.IsCrouching) show = false;
            }
            alpha = Mathf.MoveTowards(alpha, show ? 1f : 0f, Time.deltaTime * 6f);
            SetAlpha(alpha);
            bob += Time.deltaTime;
            root.localPosition = offset + new Vector2(0f, Mathf.Sin(bob * 3f) * 0.06f);
            float s = 0.85f + 0.15f * alpha;
            root.localScale = new Vector3(s, s, 1f);
        }

        private void SetAlpha(float a)
        {
            if (root == null) return;
            root.gameObject.SetActive(a > 0.01f);
            foreach (SpriteRenderer r in outlines)
            {
                Color c = r.color; c.a = a; r.color = c;
            }
            foreach (SpriteRenderer r in insides)
            {
                Color c = r.color; c.a = a * 0.95f; r.color = c;
            }
            if (text != null) text.alpha = a;
        }
    }
}
