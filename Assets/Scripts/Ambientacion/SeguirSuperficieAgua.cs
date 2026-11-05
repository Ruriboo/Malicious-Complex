using UnityEngine;

// Mantiene el objeto (niebla baja, ondas) pegado a la superficie del FloodWater asignado.
// Se oculta mientras el agua esta por debajo de 'alturaMinimaVisible'.
public class SeguirSuperficieAgua : MonoBehaviour
{
    [SerializeField] private FloodWater agua;
    [SerializeField] private float offset = 0.15f;

    public FloodWater Agua { get => agua; set => agua = value; }

    private void LateUpdate()
    {
        if (agua == null) return;
        Vector3 p = transform.position;
        p.y = agua.AlturaSuperficie + offset;
        transform.position = p;
    }
}
