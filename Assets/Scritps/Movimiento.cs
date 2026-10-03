    using UnityEngine;

    public class Movimiento : MonoBehaviour
    {
        [Header("Ajustes de Movimiento")]
        public float velocidad = 5f;
        public float fuerzaSalto = 10f;

        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        private bool estaEnSuelo;

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            // 1. Movimiento horizontal (Teclas A/D o Flechas Izquierda/Derecha)
            float entradaX = Input.GetAxisRaw("Horizontal");
            rb.linearVelocity = new Vector2(entradaX * velocidad, rb.linearVelocity.y);

            // Voltear el sprite si va a la izquierda o derecha
            if (entradaX > 0)
            {
                spriteRenderer.flipX = false;
            }
            else if (entradaX < 0)
            {
                spriteRenderer.flipX = true;
            }

            // 2. Saltar (Barra Espaciadora) solo si está tocando el suelo
            if (Input.GetKeyDown(KeyCode.Space) && estaEnSuelo)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x,fuerzaSalto);
                estaEnSuelo = false;
            }
        }

        // Detectar cuando toca el suelo
        private void OnCollisionEnter2D(Collision2D colision)
        {
            // Si choca por encima de una superficie, puede volver a saltar
            if (colision.contacts[0].normal.y > 0.5f)
            {
                estaEnSuelo = true;
            }
        }

        // Detectar cuando se despega del suelo
        private void OnCollisionExit2D(Collision2D colision)
        {
            estaEnSuelo = false;
        }
    }