using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Control de la fila de seguidores: alta, baja y reindexado.
/// No sabe de input, fisica ni muerte.
/// </summary>
public class ControladorFila : MonoBehaviour
{
    [Header("Sistema de Vidas Extra")]
    public int maxSeguidores = 4;
    public List<DinoSeguidor> seguidores = new List<DinoSeguidor>();

    public bool AgregarSeguidor(DinoSeguidor amigo)
    {
        if (seguidores.Count >= maxSeguidores) return false;
        seguidores.Add(amigo);
        amigo.AsignarIndice(seguidores.Count - 1);
        return true;
    }

    public void RemoverSeguidor(DinoSeguidor amigo)
    {
        if (seguidores.Contains(amigo))
        {
            seguidores.Remove(amigo);
            for (int i = 0; i < seguidores.Count; i++)
            {
                if (seguidores[i] != null)
                    seguidores[i].AsignarIndice(i);
            }
        }
    }
}