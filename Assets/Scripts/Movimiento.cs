using UnityEngine;

public class Movimiento : MonoBehaviour
{
    [Header("Ajustes de Movimiento")]
    public float velocidad = 5f;
    public float fuerzaSalto = 14f;
    [Header("Feel del salto")]
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.15f;
    [Header("Better Jump - para que no planee")]
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;
    public float maxFallSpeed = -18f;
    [Header("Animación")]
    public Animator animator;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float entradaX;
    private bool estaEnSuelo;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private bool saltoMantenido;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Update()
    {
        entradaX = Input.GetAxisRaw("Horizontal");
        if (entradaX > 0) spriteRenderer.flipX = false;
        else if (entradaX < 0) spriteRenderer.flipX = true;

        if (Input.GetKeyDown(KeyCode.Space))
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;

        if (estaEnSuelo)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.deltaTime;

        saltoMantenido = Input.GetKey(KeyCode.Space);

        if (animator != null)
        {
            animator.SetFloat("Velocidad", Mathf.Abs(entradaX));
            animator.SetBool("EnSuelo", estaEnSuelo);
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(entradaX * velocidad, rb.linearVelocity.y);

        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
            estaEnSuelo = false;
        }

        if (rb.linearVelocity.y < 0)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        else if (rb.linearVelocity.y > 0 && !saltoMantenido)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;

        if (rb.linearVelocity.y < maxFallSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("SUELO") && TocaDesdeArriba(col))
            estaEnSuelo = true;
    }
    private void OnCollisionStay2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("SUELO") && TocaDesdeArriba(col))
            estaEnSuelo = true;
    }
    private void OnCollisionExit2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("SUELO"))
            estaEnSuelo = false;
    }
    private bool TocaDesdeArriba(Collision2D col)
    {
        foreach (var c in col.contacts)
            if (c.normal.y > 0.5f) return true;
        return false;
    }
}