using System.Collections.Generic;
using UnityEngine;

// Trigger grande alrededor del enemigo (hijo "ZonaAtaque"). Igual que FloodWater, detecta
// al jugador por el componente Oxigeno del collider que entra (ColliderOxigeno) o por su
// CharacterController, y avisa a Enemigo si el jugador esta dentro.
[RequireComponent(typeof(Collider))]
public class ZonaAtaqueEnemigo : MonoBehaviour
{
    // Colliders del jugador que estan dentro ahora mismo (puede tener varios).
    private readonly HashSet<Collider> dentro = new HashSet<Collider>();

    public bool JugadorDentro
    {
        get
        {
            dentro.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
            return dentro.Count > 0;
        }
    }

    public Oxigeno OxigenoJugador { get; private set; }

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other) => Registrar(other);

    private void OnTriggerStay(Collider other) => Registrar(other);

    private void OnTriggerExit(Collider other) => dentro.Remove(other);

    private void OnDisable() => Vaciar();

    public void Vaciar() => dentro.Clear();

    private void Registrar(Collider other)
    {
        Oxigeno oxigeno = BuscarOxigeno(other);
        if (oxigeno == null) return;

        OxigenoJugador = oxigeno;
        dentro.Add(other);
    }

    private static Oxigeno BuscarOxigeno(Collider other)
    {
        Oxigeno oxigeno = other.GetComponent<Oxigeno>(); // ColliderOxigeno (como en FloodWater)
        if (oxigeno == null && other is CharacterController) oxigeno = other.GetComponentInChildren<Oxigeno>();
        return oxigeno;
    }
}
