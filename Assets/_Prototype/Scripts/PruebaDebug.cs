using UnityEngine;

// Script TEMPORAL para medir las pruebas P01 a P13. Bórralo al terminar.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerMovement))]
public class PruebaDebug : MonoBehaviour
{
    private Rigidbody2D rb;
    private PlayerMovement pm;

    private float maxVx;
    private float fps;

    // Salto
    private bool midiendoSalto;
    private float takeoffY, alturaMax, ultimaAltura, tiempoSalto, ultimoSaltoTiempo = -999f;

    // Aceleración y frenado
    private float inicioAcel = -1f, tAcel, inicioFreno = -1f, tFreno;
    private float inputPrevio;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        pm = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        float vx = rb.linearVelocity.x;
        float vy = rb.linearVelocity.y;
        float input = Input.GetAxisRaw("Horizontal");

        fps = Mathf.Lerp(fps, 1f / Time.unscaledDeltaTime, 0.05f);
        maxVx = Mathf.Max(maxVx, Mathf.Abs(vx));

        if (Input.GetKeyDown(KeyCode.R)) { maxVx = 0f; tAcel = 0f; tFreno = 0f; }

        bool suelo = pm.groundCheck != null &&
            Physics2D.OverlapCircle(pm.groundCheck.position, pm.groundCheckRadius, pm.groundLayer);

        // ---- Salto ----
        if (Input.GetButtonDown("Jump"))
        {
            ultimoSaltoTiempo = Time.time;
            if (suelo)
            {
                midiendoSalto = true;
                takeoffY = transform.position.y;
                alturaMax = 0f;
                tiempoSalto = Time.time;
            }
        }

        if (midiendoSalto)
        {
            alturaMax = Mathf.Max(alturaMax, transform.position.y - takeoffY);

            if (suelo && vy <= 0.01f && Time.time - tiempoSalto > 0.15f)
            {
                midiendoSalto = false;
                ultimaAltura = alturaMax;
            }
        }

        // ---- Aceleración (de 0 a velocidad máxima) ----
        if (Mathf.Abs(vx) < 0.01f && Mathf.Abs(input) > 0.01f && inicioAcel < 0f) inicioAcel = Time.time;
        if (inicioAcel >= 0f && Mathf.Abs(vx) >= pm.moveSpeed * 0.99f)
        {
            tAcel = Time.time - inicioAcel;
            inicioAcel = -1f;
        }
        if (Mathf.Abs(input) < 0.01f) inicioAcel = -1f;

        // ---- Frenado (de velocidad máxima a 0) ----
        if (Mathf.Abs(inputPrevio) > 0.01f && Mathf.Abs(input) < 0.01f && Mathf.Abs(vx) > pm.moveSpeed * 0.9f)
            inicioFreno = Time.time;
        if (inicioFreno >= 0f && Mathf.Abs(vx) < 0.01f)
        {
            tFreno = Time.time - inicioFreno;
            inicioFreno = -1f;
        }

        inputPrevio = input;
    }

    void OnGUI()
    {
        GUIStyle estilo = new GUIStyle(GUI.skin.label) { fontSize = 16 };
        estilo.normal.textColor = Color.white;

        int proyectiles = FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length;

        string texto =
            "Vel. X: " + rb.linearVelocity.x.ToString("F2") + "\n" +
            "Vel. Y: " + rb.linearVelocity.y.ToString("F2") + "\n" +
            "Vel. máx: " + maxVx.ToString("F2") + "   (R = reiniciar)\n" +
            "t. aceleración: " + tAcel.ToString("F2") + " s\n" +
            "t. frenado: " + tFreno.ToString("F2") + " s\n" +
            "Altura último salto: " + ultimaAltura.ToString("F2") + " u\n" +
            "t. desde salto: " + (Time.time - ultimoSaltoTiempo).ToString("F2") + " s\n" +
            "Cápsulas activas: " + proyectiles + "\n" +
            "FPS: " + fps.ToString("F0");

        GUI.Label(new Rect(10, 10, 420, 260), texto, estilo);
    }
}