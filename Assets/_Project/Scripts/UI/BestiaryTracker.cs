using System.Collections.Generic;
using HealingKnight.Core.Flow;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Enemies;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HealingKnight.UI
{
    /// <summary>
    /// Va en cada nivel. Cuando un patógeno aparece EN PANTALLA por primera vez, se desbloquea su ficha del
    /// bestiario (deja de ser silueta) y sale un aviso arriba a la derecha. Al derrotarlo por primera vez se marca
    /// como "derrotado". Todo se guarda en la partida.
    /// </summary>
    public class BestiaryTracker : MonoBehaviour
    {
        [SerializeField] private float scanEvery = 0.3f;
        [SerializeField] private float toastTime = 3.6f;

        private Bestiary bestiary;
        private Camera cam;
        private float nextScan;
        private readonly Queue<BestiaryEntry> pending = new Queue<BestiaryEntry>();

        private RectTransform toast;
        private Image toastPicture;
        private TextMeshProUGUI toastName, toastHead;
        private float toastTimer = -1f;

        private void Awake()
        {
            bestiary = Bestiary.Load();
            if (bestiary == null) { enabled = false; return; }
            BuildToast();
        }

        private void OnEnable() => EnemyHealth.AnyDied += OnEnemyDied;
        private void OnDisable() => EnemyHealth.AnyDied -= OnEnemyDied;

        private void OnEnemyDied(EnemyHealth enemy)
        {
            if (bestiary == null || enemy == null) return;
            BestiaryEntry entry = bestiary.Find(enemy.Data);
            if (entry == null) return;
            if (!GameSession.Save.HasSeen(entry.id)) Unlock(entry);   // por si lo mataste fuera de cámara
            GameSession.DefeatPathogen(entry.id);
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + scanEvery;
                Scan();
            }
            AnimateToast();
        }

        private void Scan()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            List<EnemyHealth> active = EnemyHealth.Active;
            for (int i = 0; i < active.Count; i++)
            {
                EnemyHealth enemy = active[i];
                if (enemy == null || !enemy.IsAlive || !enemy.isActiveAndEnabled) continue;
                Vector3 v = cam.WorldToViewportPoint(enemy.transform.position);
                if (v.z < 0f || v.x < 0.03f || v.x > 0.97f || v.y < 0.03f || v.y > 0.97f) continue;
                BestiaryEntry entry = bestiary.Find(enemy.Data);
                if (entry != null && !GameSession.Save.HasSeen(entry.id)) Unlock(entry);
            }
        }

        private void Unlock(BestiaryEntry entry)
        {
            if (!GameSession.SeePathogen(entry.id)) return;
            pending.Enqueue(entry);
        }

        // ---------- Aviso ----------

        private void BuildToast()
        {
            Canvas canvas = UIFactory.CreateCanvas("Bestiario Aviso", 40, transform);
            Image body = CartoonUI.Blob(canvas.transform, "Aviso", new Color(0.32f, 0.13f, 0.27f, 1f), new Vector2(520f, 140f), 6f, 1f, true, false);   // sin brillo: tapaba el título amarillo
            toast = (RectTransform)body.transform.parent;
            UIFactory.Place(toast, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(600f, -285f), new Vector2(520f, 140f));

            Image glow = CartoonUI.Image(body.transform, "Halo", CartoonUI.Glow, new Color(1f, 0.85f, 0.5f, 0.45f));
            UIFactory.Place(glow.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(66f, 0f), new Vector2(170f, 170f));
            toastPicture = CartoonUI.Image(body.transform, "Dibujo", null, Color.white);
            toastPicture.preserveAspect = true;
            UIFactory.Place(toastPicture.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(66f, 0f), new Vector2(110f, 110f));

            toastHead = CartoonUI.Text(body.transform, "¡NUEVO EN EL BESTIARIO!", 24f, CartoonUI.Yellow, true, TextAlignmentOptions.Left, 0.15f);
            UIFactory.Place(toastHead.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(136f, -12f), new Vector2(370f, 34f));
            toastName = CartoonUI.Text(body.transform, "", 30f, CartoonUI.Cream, true, TextAlignmentOptions.Left, 0.15f);
            toastName.enableAutoSizing = true; toastName.fontSizeMin = 18f; toastName.fontSizeMax = 30f;
            UIFactory.Place(toastName.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(136f, -46f), new Vector2(370f, 44f));
            TextMeshProUGUI hint = CartoonUI.Text(body.transform, "Lee su ficha en Pausa > Bestiario", 18f, new Color(1f, 0.9f, 0.92f, 0.7f), false, TextAlignmentOptions.Left, 0f);
            UIFactory.Place(hint.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(136f, -94f), new Vector2(370f, 28f));
            toast.gameObject.SetActive(false);
        }

        private void AnimateToast()
        {
            if (toast == null) return;
            if (toastTimer < 0f)
            {
                if (pending.Count == 0) return;
                BestiaryEntry e = pending.Dequeue();
                toastName.text = e.name;
                toastHead.text = e.boss ? "¡JEFE EN EL BESTIARIO!" : e.ally ? "¡NUEVA ALIADA EN EL BESTIARIO!" : "¡NUEVO EN EL BESTIARIO!";
                toastPicture.sprite = e.portrait;
                toastPicture.enabled = e.portrait != null;
                toastTimer = 0f;
                toast.gameObject.SetActive(true);
                Sfx.Effect("Bestiario_Nuevo", 0.7f);
            }

            toastTimer += Time.unscaledDeltaTime;
            // Entra deslizándose, se queda y sale.
            float inT = Mathf.Clamp01(toastTimer / 0.35f);
            float outT = Mathf.Clamp01((toastTimer - (toastTime - 0.35f)) / 0.35f);
            float ease = 1f - Mathf.Pow(1f - inT, 3f);
            float x = Mathf.Lerp(600f, -30f, ease) + Mathf.Lerp(0f, 600f, outT * outT);
            toast.anchoredPosition = new Vector2(x, -285f);   // debajo del globo de NODO
            float pop = 1f + Mathf.Sin(Mathf.Clamp01(toastTimer / 0.5f) * Mathf.PI) * 0.06f;
            toast.localScale = new Vector3(pop, pop, 1f);

            if (toastTimer >= toastTime)
            {
                toastTimer = -1f;
                toast.gameObject.SetActive(false);
            }
        }
    }
}
