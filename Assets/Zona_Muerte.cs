using UnityEngine;

public class ZonaMuerte : MonoBehaviour
{
    [SerializeField] private Transform puntoInicio;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Si lo que cayó tiene la etiqueta Player o es el personaje de prueba
        if (collision.CompareTag("Player") || collision.name.Contains("Player"))
        {
            // Lo devuelve inmediatamente al inicio
            collision.transform.position = puntoInicio.position;

            // Si tiene físicas, detiene su velocidad de caída
            Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
        }
    }
}
