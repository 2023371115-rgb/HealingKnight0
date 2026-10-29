using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 7f;
    public float acceleration = 60f;   // qué tan rápido alcanza la velocidad máxima
    public float deceleration = 80f;   // qué tan rápido frena al soltar la tecla

    [Header("Salto")]
    public float jumpForce = 14f;
    public int maxJumps = 2; // doble salto (botas jet)

    [Header("Botas jet - caída suave")]
    public float slowFallGravityScale = 0.4f;
    public float normalGravityScale = 3f;
    public float jetFuelMax = 1.5f; // segundos de combustible disponible
    private float jetFuelRemaining;

    [Header("Detección de suelo")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private int jumpsRemaining;
    private float moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Configuración que evita el movimiento "trabado"
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;

        jetFuelRemaining = jetFuelMax;
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        bool isGrounded = groundCheck != null &&
            Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Solo recarga si no va subiendo (evita saltos extra justo al despegar)
        if (isGrounded && rb.linearVelocity.y <= 0.01f)
        {
            jumpsRemaining = maxJumps;
            jetFuelRemaining = jetFuelMax; // recarga de combustible al tocar el suelo
        }

        if (Input.GetButtonDown("Jump") && jumpsRemaining > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpsRemaining--;
        }

        // Caída suave: solo mientras cae, manteniendo el botón y con combustible disponible
        bool wantsSlowFall = !isGrounded
            && rb.linearVelocity.y < 0f
            && Input.GetButton("Jump")
            && jetFuelRemaining > 0f;

        if (wantsSlowFall)
        {
            rb.gravityScale = slowFallGravityScale;
            jetFuelRemaining -= Time.deltaTime;
        }
        else
        {
            rb.gravityScale = normalGravityScale;
        }
    }

    void FixedUpdate()
    {
        float targetSpeed = moveInput * moveSpeed;
        float rate = Mathf.Abs(moveInput) > 0.01f ? acceleration : deceleration;

        float newX = Mathf.MoveTowards(
            rb.linearVelocity.x,
            targetSpeed,
            rate * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}