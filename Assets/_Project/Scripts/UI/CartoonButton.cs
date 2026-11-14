using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HealingKnight.UI
{
    /// <summary>
    /// Hace que un botón se sienta "de caricatura": se infla y se ilumina al pasar el mouse o seleccionarlo
    /// con teclado/control, se aplasta al presionarlo y se pone gris si está desactivado.
    /// </summary>
    public class CartoonButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        private Image fill;
        private Color baseColor;
        private Button button;
        private bool hover, selected, pressed;
        private float squash, seed;

        public void Init(Image fillImage, Color color)
        {
            fill = fillImage;
            baseColor = color;
        }

        /// <summary>Cambia el color base (por ejemplo, para un nivel completado).</summary>
        public void SetColor(Color color)
        {
            baseColor = color;
            if (fill != null) fill.color = color;
        }

        private void Awake()
        {
            button = GetComponent<Button>();
            seed = Random.value * 10f;
        }

        public void OnPointerEnter(PointerEventData e) { hover = true; if (Interactable) UIFactory.Select(button); }
        public void OnPointerExit(PointerEventData e) { hover = false; pressed = false; }
        public void OnPointerDown(PointerEventData e) { pressed = true; }
        public void OnPointerUp(PointerEventData e) { pressed = false; if (Interactable) squash = 1f; }
        public void OnSelect(BaseEventData e) => selected = true;
        public void OnDeselect(BaseEventData e) => selected = false;
        public void OnSubmit(BaseEventData e) { if (Interactable) squash = 1f; }

        private bool Interactable => button == null || button.interactable;

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            bool active = Interactable && (hover || selected);
            squash = Mathf.MoveTowards(squash, 0f, dt * 4f);
            float target = !Interactable ? 0.97f : pressed ? 0.93f : active ? 1.08f : 1f;
            float s = Mathf.Lerp(transform.localScale.x, target, 1f - Mathf.Exp(-14f * dt));
            float wob = Mathf.Sin(squash * Mathf.PI * 2f) * 0.08f * squash;
            transform.localScale = new Vector3(s + wob, s - wob, 1f);
            float tilt = active ? Mathf.Sin(Time.unscaledTime * 3f + seed) * 1.5f : 0f;
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(transform.localEulerAngles.z > 180f ? transform.localEulerAngles.z - 360f : transform.localEulerAngles.z, tilt, 1f - Mathf.Exp(-10f * dt)));

            if (fill == null) return;
            Color c = !Interactable ? CartoonUI.Locked : active ? Color.Lerp(baseColor, Color.white, 0.18f) : baseColor;
            fill.color = Color.Lerp(fill.color, c, 1f - Mathf.Exp(-12f * dt));
        }
    }
}
