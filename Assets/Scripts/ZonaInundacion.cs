using UnityEngine;

[System.Serializable]
public struct ZonaInundacion
{
    [Tooltip("Nombre identificador de la zona (ej: 'Zona 1 - Tutorial')")]
    public string nombreZona;

    [Tooltip("Referencia al FloodWater de esta zona")]
    public FloodWater agua;

    [Tooltip("Punto donde aparece el jugador al entrar/respawnear en esta zona")]
    public Transform puntoEntrada;

    [Tooltip("Altura Y inicial del agua al empezar esta zona")]
    public float alturaInicial;

    public bool EsValida()
    {
        return agua != null && puntoEntrada != null;
    }
}