using UnityEngine;

public class LombrizBoss : MonoBehaviour
{
    // ---------------- ESTADOS ----------------

    public enum EstadoLombriz { Oculta, Aviso, Emergida, Sumergiendo }

    public enum TipoAtaque { SaltoLargo, SaltoCorto, Furia, Ballena }

    // ---------------- CICLO DEL BOSS ----------------

    [Header("=== CICLO DEL BOSS ===")]
    [Tooltip("Tiempo que permanece completamente bajo tierra.")]
    public float tiempoOculta = 2.5f;

    [Tooltip("Tiempo del aviso antes de emerger.")]
    public float tiempoAviso = 1.0f;

    [Tooltip("Duración del movimiento por el aire.")]
    public float duracionArco = 2.6f; // margen extra para que la cadena más larga/gruesa alcance a salir completa

    [Tooltip("Tiempo mínimo de seguridad para terminar de enterrarse.")]
    public float tiempoMaximoSumersion = 2.5f;

    // ---------------- ATAQUES ----------------

    [Header("=== ATAQUES ===")]
    public float anchoArco = 12f;
    public float alturaArco = 19f; // calculado para que el pico llegue cerca de Y=18 partiendo de un sueloY=-12

    [Tooltip("Ataque corto: menor distancia y mayor velocidad.")]
    public float anchoArcoCorto = 7f;

    [Tooltip("Altura del salto corto.")]
    public float alturaArcoCorto = 5f;

    [Tooltip("Velocidad al enterrarse.")]
    public float velocidadHundir = 10f;

    [Tooltip("Probabilidad de elegir ataque corto.")]
    [Range(0f, 1f)]
    public float probabilidadSaltoCorto = 0.35f;

    // ---------------- ATAQUE BALLENA (salto completo) ----------------

    [Header("=== ATAQUE BALLENA (sale completa del ácido) ===")]
    [Tooltip("Altura del salto especial donde TODO el cuerpo sale del ácido, como una ballena breaching.")]
    public float alturaArcoBallena = 38f; // pico en Y ≈ 26 partiendo de sueloY=-12, bien por encima de la plataforma más alta (Y=18)

    [Tooltip("Ancho del salto ballena (normalmente más vertical que horizontal).")]
    public float anchoArcoBallena = 6f;

    [Tooltip("Probabilidad de que el siguiente ataque sea el salto ballena.")]
    [Range(0f, 1f)]
    public float probabilidadBallena = 0.25f;

    // ---------------- MOVIMIENTO ORGÁNICO ----------------

    [Header("=== MOVIMIENTO ORGÁNICO ===")]
    [Tooltip("Movimiento lateral de la cabeza.")]
    public float ondulacion = 0.25f;

    [Tooltip("Frecuencia de la ondulación.")]
    public float frecuenciaOndulacion = 6f;

    [Tooltip("La cabeza gira siguiendo la trayectoria.")]
    public bool orientarCabeza = true;

    [Tooltip("Suavizado de la rotación.")]
    public float suavizadoRotacion = 12f;

    [Header("=== SQUASH & STRETCH (sensación orgánica al salir/entrar) ===")]
    [Tooltip("Qué tanto se alarga y adelgaza justo al salir disparada del ácido.")]
    public float estiramientoMax = 1.35f;

    [Tooltip("Qué tanto se aplasta y ensancha justo al volver a entrar al ácido.")]
    [Range(0.4f, 1f)]
    public float compresionMax = 0.75f;

    [Tooltip("Fracción del salto (0-1) que dura el efecto de estiramiento al inicio.")]
    [Range(0.05f, 0.4f)]
    public float duracionEstiramiento = 0.15f;

    [Tooltip("Fracción del salto (0-1) que dura el efecto de aplastamiento al final.")]
    [Range(0.05f, 0.4f)]
    public float duracionCompresion = 0.15f;

    // ---------------- CUERPO ----------------

    [Header("=== CUERPO ===")]
    public int numSegmentos = 50;
    public float tamanoCabeza = 1.8f;
    public float tamanoCuerpo = 1.3f; // más grueso = más intimidante sin depender solo del largo
    public float tamanoCola = 0.4f;

    [Range(0.4f, 1f)]
    public float solapamiento = 0.65f;

    public Color colorCuerpo = new Color(0.8f, 0.35f, 0.05f);

    [Tooltip("Separación temporal entre segmentos. Mayor = más ondulación.")]
    public float retrasoCuerpo = 0.035f;

    [Tooltip("Qué tan rápido cada segmento persigue al anterior. Súbelo si al aumentar Num Segmentos la cola deja de alcanzar a salir a tiempo.")]
    public float velocidadSeguimientoCuerpo = 32f;

    // ---------------- DAÑO ----------------

    [Header("=== DAÑO ===")]
    public float danioContacto = 10f;
    public float cooldownDanio = 1f;

    [Tooltip("Radio extra utilizado para detectar al jugador.")]
    public float margenDanio = 0.1f;

    // ---------------- FASES ----------------

    [Header("=== FASES DEL BOSS ===")]
    [Tooltip("Porcentaje de vida donde comienza la fase 2.")]
    [Range(0f, 1f)]
    public float fase2Vida = 0.65f;

    [Tooltip("Porcentaje de vida donde comienza la fase 3.")]
    [Range(0f, 1f)]
    public float fase3Vida = 0.30f;

    [Tooltip("Reduce el tiempo entre ataques en fase 2.")]
    [Range(0.1f, 1f)]
    public float multiplicadorFase2 = 0.75f;

    [Tooltip("Reduce el tiempo entre ataques en fase 3.")]
    [Range(0.1f, 1f)]
    public float multiplicadorFase3 = 0.55f;

    // ---------------- REFERENCIAS ----------------

    [Header("=== REFERENCIAS ===")]
    public Transform suelo;
    public Transform jugador;
    public float margenSuelo = 0.2f;
    public SpriteRenderer sr;
    public Collider2D colliderLombriz;
    public Enemy enemyScript;

    // ---------------- VARIABLES INTERNAS ----------------

    private EstadoLombriz estadoActual;
    private TipoAtaque ataqueActual;
    private float temporizador;
    private float sueloY;
    private Vector2 inicioArco;
    private Vector2 finArco;
    private Vector2 ultimaDireccion = Vector2.down;
    private float dirArco = 1f;
    private float tiempoEnArco;
    private float ultimoGolpe = -999f;
    private bool colaEmergio;
    private int faseActual = 1;
    private Vector3 escalaBase;

    // Cuerpo
    private Transform contenedor;
    private Transform[] segmentos;
    private SpriteRenderer[] segSr;
    private float[] distancias;
    private float[] radios;

    // Historial de posiciones de la cabeza
    private Vector3[] historialCabeza;
    private int historialIndex;
    private int pasosPorSegmento;

    // ---------------- UNITY ----------------

    private void Awake()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (colliderLombriz == null) colliderLombriz = GetComponent<Collider2D>();
        if (enemyScript == null) enemyScript = GetComponent<Enemy>();
    }

    private void Start()
    {
        if (sr == null)
        {
            Debug.LogError("LombrizBoss necesita un SpriteRenderer en el mismo GameObject.");
            enabled = false;
            return;
        }

        BuscarJugador();
        sueloY = ObtenerAlturaSuelo();
        escalaBase = Vector3.one * tamanoCabeza;
        transform.localScale = escalaBase;

        CrearCuerpo();
        InicializarHistorial();
        EntrarAOculta();
    }

    private void OnDestroy()
    {
        if (contenedor != null)
            Destroy(contenedor.gameObject);
    }

    private void Update()
    {
        ActualizarFase();

        temporizador -= Time.deltaTime;

        switch (estadoActual)
        {
            case EstadoLombriz.Oculta: ActualizarOculta(); break;
            case EstadoLombriz.Aviso: ActualizarAviso(); break;
            case EstadoLombriz.Emergida: ActualizarEmergida(); break;
            case EstadoLombriz.Sumergiendo: ActualizarSumergiendo(); break;
        }

        if (estadoActual != EstadoLombriz.Oculta)
        {
            ActualizarCuerpo();
            ActualizarVisibilidad();
            ActualizarDanio();
        }
    }

    // ---------------- BUSCAR JUGADOR / SUELO ----------------

    private void BuscarJugador()
    {
        if (jugador != null) return;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) jugador = p.transform;
    }

    private float ObtenerAlturaSuelo()
    {
        if (suelo != null)
        {
            Collider2D c = suelo.GetComponent<Collider2D>();
            return c != null ? c.bounds.max.y : suelo.position.y;
        }

        return transform.position.y;
    }

    // ---------------- CREAR CUERPO ----------------

    private void CrearCuerpo()
    {
        contenedor = new GameObject("LombrizCuerpo").transform;
        contenedor.SetParent(transform.parent);

        segmentos = new Transform[numSegmentos];
        segSr = new SpriteRenderer[numSegmentos];
        distancias = new float[numSegmentos];
        radios = new float[numSegmentos];

        float radioAnterior = tamanoCabeza * 0.5f;

        for (int i = 0; i < numSegmentos; i++)
        {
            float t = numSegmentos > 1 ? (float)i / (numSegmentos - 1) : 0f;
            float tam = Mathf.Lerp(tamanoCuerpo, tamanoCola, t);

            GameObject go = new GameObject("Segmento_" + i);
            go.transform.SetParent(contenedor);
            go.transform.localScale = Vector3.one * tam;

            SpriteRenderer s = go.AddComponent<SpriteRenderer>();
            s.sprite = sr.sprite;
            s.sharedMaterial = sr.sharedMaterial;
            s.color = colorCuerpo;
            s.sortingLayerID = sr.sortingLayerID;
            s.sortingOrder = sr.sortingOrder - 1 - i;

            float radio = tam * 0.5f;

            distancias[i] = (radioAnterior + radio) * solapamiento;
            radios[i] = radio;
            radioAnterior = radio;

            segmentos[i] = go.transform;
            segSr[i] = s;
        }
    }

    // ---------------- HISTORIAL DE LA CABEZA ----------------

    private void InicializarHistorial()
    {
        pasosPorSegmento = Mathf.Max(1, Mathf.RoundToInt(retrasoCuerpo / Time.fixedDeltaTime));

        int longitud = Mathf.Max(100, pasosPorSegmento * Mathf.Max(1, numSegmentos) + 20);

        historialCabeza = new Vector3[longitud];

        for (int i = 0; i < historialCabeza.Length; i++)
            historialCabeza[i] = transform.position;
    }

    private void GuardarPosicionCabeza()
    {
        if (historialCabeza == null) return;

        historialIndex++;

        if (historialIndex >= historialCabeza.Length)
            historialIndex = 0;

        historialCabeza[historialIndex] = transform.position;
    }

    // ---------------- ESTADO: OCULTA ----------------

    private void ActualizarOculta()
    {
        if (temporizador <= 0f)
            EntrarAAviso();
    }

    private void EntrarAOculta()
    {
        estadoActual = EstadoLombriz.Oculta;
        temporizador = ObtenerTiempoOculta();

        if (sr != null)
        {
            sr.enabled = false;
            transform.localScale = escalaBase; // resetea cualquier squash/stretch pendiente
        }
        if (colliderLombriz != null) colliderLombriz.enabled = false;
        if (enemyScript != null) enemyScript.isInvulnerable = true;

        if (segSr != null)
        {
            for (int i = 0; i < segSr.Length; i++)
                segSr[i].enabled = false;
        }

        colaEmergio = false;
    }

    // ---------------- ESTADO: AVISO ----------------

    private void EntrarAAviso()
    {
        estadoActual = EstadoLombriz.Aviso;
        temporizador = tiempoAviso;
        colaEmergio = false;

        SeleccionarAtaque();

        float centroX = jugador != null ? jugador.position.x : transform.position.x;

        dirArco = Random.value > 0.5f ? 1f : -1f;

        float ancho = ObtenerAnchoAtaque();

        inicioArco = new Vector2(centroX - dirArco * ancho * 0.5f, sueloY);
        finArco = new Vector2(centroX + dirArco * ancho * 0.5f, sueloY);

        transform.position = inicioArco;

        InicializarCuerpoBajoTierra();
    }

    private void ActualizarAviso()
    {
        // Temblor progresivo, un poco más fuerte si viene un ataque Ballena (anticipación mayor).
        float multiplicadorTemblor = ataqueActual == TipoAtaque.Ballena ? 1.6f : 1f;
        float progreso = 1f - Mathf.Clamp01(temporizador / Mathf.Max(0.01f, tiempoAviso));
        float intensidad = Mathf.Lerp(0.03f, 0.12f, progreso) * multiplicadorTemblor;

        transform.position = inicioArco + Random.insideUnitCircle * intensidad;

        if (temporizador <= 0f)
            EntrarAEmergida();
    }

    // ---------------- SELECCIÓN DE ATAQUE ----------------

    private void SeleccionarAtaque()
    {
        float roll = Random.value;

        if (faseActual >= 3)
        {
            if (roll < probabilidadBallena) ataqueActual = TipoAtaque.Ballena;
            else if (roll < probabilidadBallena + 0.4f) ataqueActual = TipoAtaque.Furia;
            else ataqueActual = TipoAtaque.SaltoLargo;
            return;
        }

        if (roll < probabilidadBallena)
            ataqueActual = TipoAtaque.Ballena;
        else if (roll < probabilidadBallena + probabilidadSaltoCorto)
            ataqueActual = TipoAtaque.SaltoCorto;
        else
            ataqueActual = TipoAtaque.SaltoLargo;
    }

    private float ObtenerAnchoAtaque()
    {
        switch (ataqueActual)
        {
            case TipoAtaque.SaltoCorto: return anchoArcoCorto;
            case TipoAtaque.Ballena: return anchoArcoBallena;
            default: return anchoArco;
        }
    }

    private float ObtenerAlturaAtaque()
    {
        switch (ataqueActual)
        {
            case TipoAtaque.SaltoCorto: return alturaArcoCorto;
            case TipoAtaque.Ballena: return alturaArcoBallena;
            default: return alturaArco;
        }
    }

    // ---------------- EMERGER ----------------

    private void EntrarAEmergida()
    {
        estadoActual = EstadoLombriz.Emergida;
        temporizador = ObtenerDuracionArco();
        tiempoEnArco = 0f;

        if (enemyScript != null)
            enemyScript.isInvulnerable = false;
    }

    private void ActualizarEmergida()
    {
        MoverEnArco();

        if (temporizador <= 0f)
            EntrarASumergiendo();
    }

    // ---------------- ARCO ----------------

    private void MoverEnArco()
    {
        tiempoEnArco += Time.deltaTime;

        float duracion = Mathf.Max(0.01f, ObtenerDuracionArco());
        float t = Mathf.Clamp01(tiempoEnArco / duracion);

        float altura = ObtenerAlturaAtaque();
        float ancho = ObtenerAnchoAtaque();

        float x = Mathf.Lerp(inicioArco.x, finArco.x, t);
        float y = sueloY + 4f * altura * t * (1f - t);

        Vector2 posicion = new Vector2(x, y);

        // Tangente de la parábola.
        Vector2 tangente = new Vector2(dirArco * ancho, 4f * altura * (1f - 2f * t)).normalized;
        Vector2 normal = new Vector2(-tangente.y, tangente.x);

        // Ondulación orgánica.
        float onda = Mathf.Sin(tiempoEnArco * frecuenciaOndulacion) * ondulacion;
        posicion += normal * onda;

        Vector2 posicionAnterior = transform.position;
        transform.position = posicion;

        // Orientar la cabeza hacia el movimiento.
        if (orientarCabeza)
        {
            Vector2 direccion = posicion - posicionAnterior;

            if (direccion.sqrMagnitude > 0.0001f)
            {
                float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
                Quaternion objetivo = Quaternion.Euler(0f, 0f, angulo);

                transform.rotation = Quaternion.Lerp(
                    transform.rotation,
                    objetivo,
                    Time.deltaTime * suavizadoRotacion
                );
            }
        }

        AplicarSquashStretch(t);
        GuardarPosicionCabeza();
    }

    // Estira la cabeza al salir disparada y la aplasta justo antes de volver a entrar.
    private void AplicarSquashStretch(float t)
    {
        float factor = 1f;

        if (t < duracionEstiramiento)
        {
            float progreso = 1f - (t / duracionEstiramiento); // 1 al salir, 0 cuando termina el efecto
            factor = Mathf.Lerp(1f, estiramientoMax, progreso);
        }
        else if (t > 1f - duracionCompresion)
        {
            float progreso = (t - (1f - duracionCompresion)) / duracionCompresion; // 0 a 1 al acercarse a entrar
            factor = Mathf.Lerp(1f, compresionMax, progreso);
        }

        // factor > 1 = estirado (más alto, más delgado); factor < 1 = aplastado (más ancho, más bajo)
        transform.localScale = new Vector3(escalaBase.x / factor, escalaBase.y * factor, escalaBase.z);
    }

    // ---------------- SUMERSIÓN ----------------

    private void EntrarASumergiendo()
    {
        estadoActual = EstadoLombriz.Sumergiendo;
        temporizador = tiempoMaximoSumersion;

        if (enemyScript != null)
            enemyScript.isInvulnerable = true;

        float altura = ObtenerAlturaAtaque();
        float ancho = ObtenerAnchoAtaque();

        // Tangente aproximada al final del arco.
        ultimaDireccion = new Vector2(dirArco * ancho, -4f * altura).normalized;
    }

    private void ActualizarSumergiendo()
    {
        transform.position += (Vector3)(ultimaDireccion * velocidadHundir * Time.deltaTime);

        if (temporizador <= 0f)
        {
            EntrarAOculta();
            return;
        }

        if (colaEmergio && ColaBajoElSuelo())
            EntrarAOculta();
    }

    // Comprueba que la cola ya esté bajo la superficie.
    private bool ColaBajoElSuelo()
    {
        if (segmentos == null || segmentos.Length == 0)
            return true;

        return segmentos[segmentos.Length - 1].position.y < sueloY - margenSuelo;
    }

    // ---------------- CUERPO ----------------

    private void InicializarCuerpoBajoTierra()
    {
        Vector3 posicion = inicioArco;

        for (int i = 0; i < segmentos.Length; i++)
        {
            posicion.y -= distancias[i];
            segmentos[i].position = posicion;
        }
    }

    private void ActualizarCuerpo()
    {
        if (segmentos == null) return;

        Vector3 anterior = transform.position;

        for (int i = 0; i < segmentos.Length; i++)
        {
            Vector3 direccion = segmentos[i].position - anterior;
            direccion.z = 0f;

            if (direccion.sqrMagnitude < 0.0001f)
                direccion = Vector3.down;

            Vector3 objetivo = anterior + direccion.normalized * distancias[i];

            segmentos[i].position = Vector3.Lerp(
                segmentos[i].position,
                objetivo,
                Time.deltaTime * velocidadSeguimientoCuerpo
            );

            anterior = segmentos[i].position;
        }
    }

    // ---------------- VISIBILIDAD ----------------

    private void ActualizarVisibilidad()
    {
        bool peligroso = EsPeligroso();
        bool cabezaVisible = transform.position.y > sueloY - margenSuelo;

        sr.enabled = cabezaVisible;

        if (colliderLombriz != null)
            colliderLombriz.enabled = cabezaVisible && peligroso;

        for (int i = 0; i < segmentos.Length; i++)
            segSr[i].enabled = segmentos[i].position.y > sueloY - margenSuelo;

        Transform cola = segmentos.Length > 0 ? segmentos[segmentos.Length - 1] : transform;

        if (cola.position.y > sueloY)
            colaEmergio = true;
    }

    private bool EsPeligroso()
    {
        return estadoActual == EstadoLombriz.Emergida
            || estadoActual == EstadoLombriz.Sumergiendo;
    }

    // ---------------- DAÑO ----------------

    private void ActualizarDanio()
    {
        if (!EsPeligroso()) return;
        if (Time.time - ultimoGolpe < cooldownDanio) return;

        // Cabeza.
        if (transform.position.y > sueloY - margenSuelo)
        {
            if (RevisarContacto(transform.position, tamanoCabeza * 0.5f))
                return;
        }

        // Cuerpo.
        for (int i = 0; i < segmentos.Length; i++)
        {
            if (!segSr[i].enabled) continue;

            if (RevisarContacto(segmentos[i].position, radios[i]))
                return;
        }
    }

    private bool RevisarContacto(Vector2 centro, float radio)
    {
        if (jugador != null)
        {
            float distancia = Vector2.Distance(centro, jugador.position);

            if (distancia > radio + margenDanio + 1.5f)
                return false;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(centro, radio + margenDanio);

        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            PlayerHealth ph = hit.GetComponent<PlayerHealth>();

            if (ph != null)
            {
                ph.TakeDamage(danioContacto);
                ultimoGolpe = Time.time;
                return true;
            }
        }

        return false;
    }

    // ---------------- FASES ----------------

    private void ActualizarFase()
    {
        if (enemyScript == null) return;

        float vidaMaxima = ObtenerVidaMaximaEnemy();
        if (vidaMaxima <= 0f) return;

        float porcentaje = ObtenerVidaEnemy() / vidaMaxima;

        int nuevaFase = 1;

        if (porcentaje <= fase3Vida) nuevaFase = 3;
        else if (porcentaje <= fase2Vida) nuevaFase = 2;

        faseActual = nuevaFase;
    }

    // Si tu clase Enemy usa otros nombres, cambia solo estos dos métodos.
    private float ObtenerVidaEnemy()
    {
        return enemyScript.CurrentHealth;
    }

    private float ObtenerVidaMaximaEnemy()
    {
        return enemyScript.maxHealth;
    }

    // ---------------- TIEMPOS SEGÚN FASE ----------------

    private float ObtenerTiempoOculta()
    {
        switch (faseActual)
        {
            case 2: return tiempoOculta * multiplicadorFase2;
            case 3: return tiempoOculta * multiplicadorFase3;
            default: return tiempoOculta;
        }
    }

    private float ObtenerDuracionArco()
    {
        switch (ataqueActual)
        {
            case TipoAtaque.SaltoCorto: return duracionArco * 0.75f;
            case TipoAtaque.Furia: return duracionArco * 0.70f;
            case TipoAtaque.Ballena: return duracionArco * 1.3f; // más tiempo en el aire por la altura extra
            default: return duracionArco;
        }
    }

    // ---------------- DEBUG VISUAL ----------------

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // Suelo.
        Gizmos.DrawLine(
            new Vector3(transform.position.x - 20f, sueloY, 0f),
            new Vector3(transform.position.x + 20f, sueloY, 0f)
        );

        // Trayectoria.
        if (estadoActual == EstadoLombriz.Aviso || estadoActual == EstadoLombriz.Emergida)
        {
            Gizmos.DrawLine(inicioArco, finArco);

            const int muestras = 20;
            Vector3 anterior = inicioArco;

            for (int i = 1; i <= muestras; i++)
            {
                float t = (float)i / muestras;
                float x = Mathf.Lerp(inicioArco.x, finArco.x, t);
                float y = sueloY + 4f * ObtenerAlturaAtaque() * t * (1f - t);

                Vector3 actual = new Vector3(x, y, 0f);
                Gizmos.DrawLine(anterior, actual);
                anterior = actual;
            }
        }
    }
}