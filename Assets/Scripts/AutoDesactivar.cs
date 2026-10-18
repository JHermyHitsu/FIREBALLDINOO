using System.Collections;
using UnityEngine;

public class AutoDesactivar : MonoBehaviour
{
    [Header("Configuración de Tiempo")]
    [Tooltip("Tiempo en segundos antes de que desaparezca")]
    public float duracion = 1f;

    [Tooltip("Si está marcado se destruye el objeto por completo; si no, solo se oculta (SetActive false)")]
    public bool destruir = false;

    private void Start()
    {
        StartCoroutine(DesaparecerRutina());
    }

    private IEnumerator DesaparecerRutina()
    {
        yield return new WaitForSeconds(duracion);

        if (destruir)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
