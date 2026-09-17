using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ZonaInundacion
{
    public string nombreZona;

    public List<FloodWater> aguas;

    public Transform puntoEntrada;

    public float alturaInicial;

    /// <summary>
    /// Valida que la zona tenga al menos un punto de entrada y al menos un agua asignada.
    /// </summary>
    public bool EsValida()
    {
        if (puntoEntrada == null || aguas == null || aguas.Count == 0)
            return false;

        foreach (var agua in aguas)
        {
            if (agua != null) return true;
        }

        return false;
    }
}